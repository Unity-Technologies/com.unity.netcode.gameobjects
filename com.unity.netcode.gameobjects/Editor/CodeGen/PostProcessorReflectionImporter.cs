using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace Unity.Netcode.GameObjects.Editor.CodeGen
{
    internal class PostProcessorReflectionImporter : DefaultReflectionImporter
    {
        private const string k_SystemPrivateCoreLib = "System.Private.CoreLib";
        private readonly AssemblyNameReference m_CorrectCorlib;
        private readonly Dictionary<string, AssemblyNameReference> m_CorlibTypeScopes = new Dictionary<string, AssemblyNameReference>();

        public PostProcessorReflectionImporter(ModuleDefinition module) : base(module)
        {
            m_CorrectCorlib = module.AssemblyReferences.FirstOrDefault(a => a.Name == "mscorlib" || a.Name == "netstandard" || a.Name == k_SystemPrivateCoreLib);
        }

        // .NET reference packs split System.Private.CoreLib across System.Runtime, System.Collections and others,
        // so a module that references none of the assemblies above needs the one that defines or forwards the type.
        protected override IMetadataScope ImportScope(Type type)
        {
            if (m_CorrectCorlib == null && type.Assembly.GetName().Name == k_SystemPrivateCoreLib)
            {
                var outermost = type;
                while (outermost.DeclaringType != null)
                {
                    outermost = outermost.DeclaringType;
                }

                var reference = FindReferenceDefining(outermost.FullName);
                if (reference != null)
                {
                    return reference;
                }
            }

            return base.ImportScope(type);
        }

        public override AssemblyNameReference ImportReference(AssemblyName reference)
        {
            return m_CorrectCorlib != null && reference.Name == k_SystemPrivateCoreLib ? m_CorrectCorlib : base.ImportReference(reference);
        }

        private AssemblyNameReference FindReferenceDefining(string fullName)
        {
            if (m_CorlibTypeScopes.TryGetValue(fullName, out var cached))
            {
                return cached;
            }

            AssemblyNameReference found = null;
            foreach (var reference in module.AssemblyReferences)
            {
                AssemblyDefinition assembly;
                try
                {
                    assembly = module.AssemblyResolver.Resolve(reference);
                }
                catch (AssemblyResolutionException)
                {
                    continue;
                }

                if (assembly == null)
                {
                    continue;
                }

                if (assembly.MainModule.GetType(fullName) != null || assembly.MainModule.ExportedTypes.Any(t => t.FullName == fullName))
                {
                    found = reference;
                    break;
                }
            }

            m_CorlibTypeScopes[fullName] = found;
            return found;
        }
    }
}
