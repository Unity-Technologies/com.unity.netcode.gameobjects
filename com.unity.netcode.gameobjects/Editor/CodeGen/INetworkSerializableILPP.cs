using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.Collections;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using ILPPInterface = Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Unity.Netcode.Editor.CodeGen
{

    internal sealed class INetworkSerializableILPP : ILPPInterface
    {
        public override ILPPInterface GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly) =>
            compiledAssembly.Name == CodeGenHelpers.RuntimeAssemblyName ||
            compiledAssembly.References.Any(filePath => Path.GetFileNameWithoutExtension(filePath) == CodeGenHelpers.RuntimeAssemblyName);

        private readonly List<DiagnosticMessage> m_Diagnostics = new List<DiagnosticMessage>();

        private TypeReference ResolveGenericType(TypeReference type, List<TypeReference> typeStack)
        {
            var genericName = type.Name;
            var lastType = (GenericInstanceType)typeStack[typeStack.Count - 1];
            var resolvedType = lastType.Resolve();
            typeStack.RemoveAt(typeStack.Count - 1);
            for (var i = 0; i < resolvedType.GenericParameters.Count; ++i)
            {
                var parameter = resolvedType.GenericParameters[i];
                if (parameter.Name == genericName)
                {
                    var underlyingType = lastType.GenericArguments[i];
                    if (underlyingType.Resolve() == null)
                    {
                        return ResolveGenericType(underlyingType, typeStack);
                    }

                    return underlyingType;
                }
            }

            return null;
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly))
            {
                return null;
            }

            m_Diagnostics.Clear();

            // read
            var assemblyDefinition = CodeGenHelpers.AssemblyDefinitionFor(compiledAssembly, out var resolver);
            if (assemblyDefinition == null)
            {
                m_Diagnostics.AddError($"Cannot read assembly definition: {compiledAssembly.Name}");
                return null;
            }

            // process
            var mainModule = assemblyDefinition.MainModule;
            if (mainModule != null)
            {
                try
                {
                    if (ImportReferences(mainModule))
                    {
                        // Initialize all the delegates for various NetworkVariable types to ensure they can be serailized

                        // Find all types we know we're going to want to serialize.
                        // The list of these types includes:
                        // - Non-generic INetworkSerializable types
                        // - Non-Generic INetworkSerializeByMemcpy types
                        // - Enums that are not declared within generic types
                        // We can't process generic types because, to initialize a generic, we need a value
                        // for `T` to initialize it with.
                        var networkSerializableTypes = mainModule.GetTypes()
                            .Where(t => t.Resolve().HasInterface(CodeGenHelpers.INetworkSerializable_FullName) && !t.Resolve().IsAbstract && !t.Resolve().HasGenericParameters && t.Resolve().IsValueType)
                            .ToList();
                        var structTypes = mainModule.GetTypes()
                            .Where(t => t.Resolve().HasInterface(CodeGenHelpers.INetworkSerializeByMemcpy_FullName) && !t.Resolve().IsAbstract && !t.Resolve().HasGenericParameters && t.Resolve().IsValueType)
                            .ToList();
                        // There are many FixedString types, but all of them share the interfaces INativeList<bool> and IUTF8Bytes.
                        // INativeList<bool> provides the Length property
                        // IUTF8Bytes provides GetUnsafePtr()
                        // Those two are necessary to serialize FixedStrings efficiently
                        // - otherwise we'd just be memcpying the whole thing even if
                        // most of it isn't used.
                        var fixedStringTypes = mainModule.GetTypes()
                            .Where(t => t.Resolve().HasInterface(CodeGenHelpers.IUTF8Bytes_FullName) && t.HasInterface(m_INativeListBool_TypeRef.FullName) && !t.Resolve().IsAbstract && !t.Resolve().HasGenericParameters && t.Resolve().IsValueType)
                            .ToList();
                        var enumTypes = mainModule.GetTypes()
                            .Where(t => t.Resolve().IsEnum && !t.Resolve().IsAbstract && !t.Resolve().HasGenericParameters && t.Resolve().IsValueType)
                            .ToList();

                        // Now, to support generics, we have to do an extra pass.
                        // We look for any type that's a NetworkBehaviour type
                        // Then we look through all the fields in that type, finding any field whose type is
                        // descended from `NetworkVariableSerialization`. Then we check `NetworkVariableSerialization`'s
                        // `T` value, and if it's a generic, then we know it was missed in the above sweep and needs
                        // to be initialized. Now we have a full generic instance rather than a generic definition,
                        // so we can validly generate an initializer for that particular instance of the generic type.
                        var networkSerializableTypesSet = new HashSet<TypeReference>(networkSerializableTypes);
                        var structTypesSet = new HashSet<TypeReference>(structTypes);
                        var enumTypesSet = new HashSet<TypeReference>(enumTypes);
                        var fixedStringTypesSet = new HashSet<TypeReference>(fixedStringTypes);
                        var typeStack = new List<TypeReference>();
                        foreach (var type in mainModule.GetTypes())
                        {
                            // Check if it's a NetworkBehaviour
                            if (type.IsSubclassOf(CodeGenHelpers.NetworkBehaviour_FullName))
                            {
                                // Iterate fields looking for NetworkVariableSerialization fields
                                foreach (var field in type.Fields)
                                {
                                    // Get the field type and its base type
                                    var fieldType = field.FieldType;
                                    var baseType = fieldType.Resolve().BaseType;
                                    if (baseType == null)
                                    {
                                        continue;
                                    }
                                    // This type stack is used for resolving NetworkVariableSerialization's T value
                                    // When looking at base types, we get the type definition rather than the
                                    // type reference... which means that we get the generic definition with an
                                    // undefined T rather than the instance with the type filled in.
                                    // We then have to walk backward back down the type stack to resolve what T
                                    // is.
                                    typeStack.Clear();
                                    typeStack.Add(fieldType);
                                    // Iterate through the base types until we get to Object.
                                    // Object is the base for everything so we'll stop when we hit that.
                                    while (baseType.Name != mainModule.TypeSystem.Object.Name)
                                    {
                                        // If we've found a NetworkVariableSerialization type...
                                        if (baseType.IsGenericInstance && baseType.Resolve() == m_NetworkVariableSerializationType)
                                        {
                                            // Then we need to figure out what T is
                                            var genericType = (GenericInstanceType)baseType;
                                            var underlyingType = genericType.GenericArguments[0];
                                            if (underlyingType.Resolve() == null)
                                            {
                                                underlyingType = ResolveGenericType(underlyingType, typeStack);
                                            }

                                            // Then we pick the correct set to add it to and set it up
                                            // for initialization, if it's generic. We'll also use this moment to catch
                                            // any NetworkVariables with invalid T types at compile time.
                                            if (underlyingType.HasInterface(CodeGenHelpers.INetworkSerializable_FullName))
                                            {
                                                if (underlyingType.IsGenericInstance)
                                                {
                                                    networkSerializableTypesSet.Add(underlyingType);
                                                }
                                            }

                                            else if (underlyingType.HasInterface(CodeGenHelpers.INetworkSerializeByMemcpy_FullName))
                                            {
                                                if (underlyingType.IsGenericInstance)
                                                {
                                                    structTypesSet.Add(underlyingType);
                                                }
                                            }
                                            else if (underlyingType.HasInterface(m_INativeListBool_TypeRef.FullName) && underlyingType.HasInterface(CodeGenHelpers.IUTF8Bytes_FullName))
                                            {
                                                if (underlyingType.IsGenericInstance)
                                                {
                                                    fixedStringTypesSet.Add(underlyingType);
                                                }
                                            }

                                            else if (underlyingType.Resolve().IsEnum)
                                            {
                                                if (underlyingType.IsGenericInstance)
                                                {
                                                    enumTypesSet.Add(underlyingType);
                                                }
                                            }
                                            else if (!underlyingType.Resolve().IsPrimitive)
                                            {
                                                bool methodExists = false;
                                                foreach (var method in m_FastBufferWriterType.Methods)
                                                {
                                                    if (!method.HasGenericParameters && method.Parameters.Count == 1 && method.Parameters[0].ParameterType.Resolve() == underlyingType.Resolve())
                                                    {
                                                        methodExists = true;
                                                        break;
                                                    }
                                                }

                                                if (!methodExists)
                                                {
                                                    m_Diagnostics.AddError($"{type}.{field.Name}: {underlyingType} is not valid for use in {typeof(NetworkVariable<>).Name} types. Types must either implement {nameof(INetworkSerializeByMemcpy)} or {nameof(INetworkSerializable)}. If this type is external and you are sure its memory layout makes it serializable by memcpy, you can replace {underlyingType} with {typeof(ForceNetworkSerializeByMemcpy<>).Name}<{underlyingType}>, or you can create extension methods for {nameof(FastBufferReader)}.{nameof(FastBufferReader.ReadValueSafe)}(this {nameof(FastBufferReader)}, out {underlyingType}) and {nameof(FastBufferWriter)}.{nameof(FastBufferWriter.WriteValueSafe)}(this {nameof(FastBufferWriter)}, in {underlyingType}) to define serialization for this type.");
                                                }
                                            }

                                            break;
                                        }

                                        typeStack.Add(baseType);
                                        baseType = baseType.Resolve().BaseType;
                                    }
                                }
                            }
                            // We'll also avoid some confusion by ensuring users only choose one of the two
                            // serialization schemes - by method OR by memcpy, not both. We'll also do a cursory
                            // check that INetworkSerializeByMemcpy types are unmanaged.
                            else if (type.HasInterface(CodeGenHelpers.INetworkSerializeByMemcpy_FullName))
                            {
                                if (type.HasInterface(CodeGenHelpers.INetworkSerializable_FullName))
                                {
                                    m_Diagnostics.AddError($"{nameof(INetworkSerializeByMemcpy)} types may not implement {nameof(INetworkSerializable)} - choose one or the other.");
                                }
                                if (!type.IsValueType)
                                {
                                    m_Diagnostics.AddError($"{nameof(INetworkSerializeByMemcpy)} types must be unmanaged types.");
                                }
                            }
                        }

                        if (networkSerializableTypes.Count + structTypes.Count + enumTypes.Count == 0)
                        {
                            return null;
                        }

                        // Finally we add to the module initializer some code to initialize the delegates in
                        // NetworkVariableSerialization<T> for all necessary values of T, by calling initialization
                        // methods in NetworkVariableHelpers.
                        CreateModuleInitializer(assemblyDefinition, networkSerializableTypesSet.ToList(), structTypesSet.ToList(), enumTypesSet.ToList(), fixedStringTypesSet.ToList());
                    }
                    else
                    {
                        m_Diagnostics.AddError($"Cannot import references into main module: {mainModule.Name}");
                    }
                }
                catch (Exception e)
                {
                    m_Diagnostics.AddError((e.ToString() + e.StackTrace.ToString()).Replace("\n", "|").Replace("\r", "|"));
                }
            }
            else
            {
                m_Diagnostics.AddError($"Cannot get main module from assembly definition: {compiledAssembly.Name}");
            }

            mainModule.RemoveRecursiveReferences();

            // write
            var pe = new MemoryStream();
            var pdb = new MemoryStream();

            var writerParameters = new WriterParameters
            {
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                SymbolStream = pdb,
                WriteSymbols = true
            };

            assemblyDefinition.Write(pe, writerParameters);

            return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), m_Diagnostics);
        }

        private MethodReference m_InitializeDelegatesNetworkSerializable_MethodRef;
        private MethodReference m_InitializeDelegatesStruct_MethodRef;
        private MethodReference m_InitializeDelegatesEnum_MethodRef;
        private MethodReference m_InitializeDelegatesFixedString_MethodRef;

        private TypeDefinition m_NetworkVariableSerializationType;
        private TypeDefinition m_FastBufferWriterType;

        private TypeReference m_INativeListBool_TypeRef;

        private const string k_InitializeNetworkSerializableMethodName = nameof(NetworkVariableHelper.InitializeDelegatesNetworkSerializable);
        private const string k_InitializeStructMethodName = nameof(NetworkVariableHelper.InitializeDelegatesStruct);
        private const string k_InitializeEnumMethodName = nameof(NetworkVariableHelper.InitializeDelegatesEnum);
        private const string k_InitializeFixedStringMethodName = nameof(NetworkVariableHelper.InitializeDelegatesFixedString);

        private bool ImportReferences(ModuleDefinition moduleDefinition)
        {

            var helperType = typeof(NetworkVariableHelper);
            foreach (var methodInfo in helperType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                switch (methodInfo.Name)
                {
                    case k_InitializeNetworkSerializableMethodName:
                        m_InitializeDelegatesNetworkSerializable_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case k_InitializeStructMethodName:
                        m_InitializeDelegatesStruct_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case k_InitializeEnumMethodName:
                        m_InitializeDelegatesEnum_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case k_InitializeFixedStringMethodName:
                        m_InitializeDelegatesFixedString_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                }
            }
            m_NetworkVariableSerializationType = moduleDefinition.ImportReference(typeof(NetworkVariableSerialization<>)).Resolve();
            m_NetworkVariableSerializationType = moduleDefinition.ImportReference(typeof(FastBufferWriter)).Resolve();
            m_INativeListBool_TypeRef = moduleDefinition.ImportReference(typeof(INativeList<bool>));
            return true;
        }

        private MethodDefinition GetOrCreateStaticConstructor(TypeDefinition typeDefinition)
        {
            var staticCtorMethodDef = typeDefinition.GetStaticConstructor();
            if (staticCtorMethodDef == null)
            {
                staticCtorMethodDef = new MethodDefinition(
                    ".cctor", // Static Constructor (constant-constructor)
                    MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName |
                    MethodAttributes.RTSpecialName |
                    MethodAttributes.Static,
                    typeDefinition.Module.TypeSystem.Void);
                staticCtorMethodDef.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                typeDefinition.Methods.Add(staticCtorMethodDef);
            }

            return staticCtorMethodDef;
        }

        // Creates a static module constructor (which is executed when the module is loaded) that registers all the
        // message types in the assembly with MessagingSystem.
        // This is the same behavior as annotating a static method with [ModuleInitializer] in standardized
        // C# (that attribute doesn't exist in Unity, but the static module constructor still works)
        // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.moduleinitializerattribute?view=net-5.0
        // https://web.archive.org/web/20100212140402/http://blogs.msdn.com/junfeng/archive/2005/11/19/494914.aspx
        private void CreateModuleInitializer(AssemblyDefinition assembly, List<TypeReference> networkSerializableTypes, List<TypeReference> structTypes, List<TypeReference> enumTypes, List<TypeReference> fixedStringTypes)
        {
            foreach (var typeDefinition in assembly.MainModule.Types)
            {
                if (typeDefinition.FullName == "<Module>")
                {
                    var staticCtorMethodDef = GetOrCreateStaticConstructor(typeDefinition);

                    var processor = staticCtorMethodDef.Body.GetILProcessor();

                    var instructions = new List<Instruction>();

                    foreach (var type in structTypes)
                    {
                        var method = new GenericInstanceMethod(m_InitializeDelegatesStruct_MethodRef);
                        method.GenericArguments.Add(type);
                        instructions.Add(processor.Create(OpCodes.Call, method));
                    }

                    foreach (var type in networkSerializableTypes)
                    {
                        var method = new GenericInstanceMethod(m_InitializeDelegatesNetworkSerializable_MethodRef);
                        method.GenericArguments.Add(type);
                        instructions.Add(processor.Create(OpCodes.Call, method));
                    }

                    foreach (var type in enumTypes)
                    {
                        var method = new GenericInstanceMethod(m_InitializeDelegatesEnum_MethodRef);
                        method.GenericArguments.Add(type);
                        instructions.Add(processor.Create(OpCodes.Call, method));
                    }

                    foreach (var type in fixedStringTypes)
                    {
                        var method = new GenericInstanceMethod(m_InitializeDelegatesFixedString_MethodRef);
                        method.GenericArguments.Add(type);
                        instructions.Add(processor.Create(OpCodes.Call, method));
                    }

                    instructions.ForEach(instruction => processor.Body.Instructions.Insert(processor.Body.Instructions.Count - 1, instruction));
                    break;
                }
            }
        }
    }
}
