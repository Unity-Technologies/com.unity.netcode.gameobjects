using System;

namespace Unity.Netcode
{
    /// <summary>
    /// Marks a generic parameter in this class as a type that should be serialized through
    /// <see cref="NetworkVariableSerialization{T}"/>. This enables the use of the following methods to support
    /// serialization within a Network Variable type:
    /// <br/>
    /// <br/>
    /// <see cref="NetworkVariableSerialization{T}"/>.<see cref="NetworkVariableSerialization{T}.Read"/>
    /// <br/>
    /// <see cref="NetworkVariableSerialization{T}"/>.<see cref="NetworkVariableSerialization{T}.Write"/>
    /// <br/>
    /// <see cref="NetworkVariableSerialization{T}"/>.<see cref="NetworkVariableSerialization{T}.AreEqual"/>
    /// <br/>
    /// <see cref="NetworkVariableSerialization{T}"/>.<see cref="NetworkVariableSerialization{T}.Duplicate"/>
    /// <br/>
    /// <br/>
    /// The parameter is indicated by index (and is 0-indexed); for example:
    /// <br/>
    /// <c>
    /// [SerializesGenericParameter(1)]
    /// public class MyClass&lt;TTypeOne, TTypeTwo&gt;
    /// {
    /// }
    /// </c>
    /// <br/>
    /// This tells the code generation for <see cref="NetworkVariableSerialization{T}"/> to generate
    /// serialized code for <b>TTypeTwo</b> (generic parameter 1).
    /// <br/>
    /// <br/>
    /// Note that this is primarily intended to support subtypes of <see cref="NetworkVariableBase"/>,
    /// and as such, the type resolution is done by examining fields of <see cref="NetworkBehaviour"/>
    /// subclasses. If your type is not used in a <see cref="NetworkBehaviour"/>, the codegen will
    /// not find the types, even with this attribute.
    /// <br/>
    /// <br/>
    /// This attribute is properly inherited by subclasses. For example:
    /// <br/>
    /// <c>
    /// [SerializesGenericParameter(0)]
    /// public class MyClass&lt;T&gt;
    /// {
    /// }
    ///
    /// public class MySubclass1 : MyClass&lt;Foo&gt;
    /// {
    /// }
    ///
    /// public class MySubclass2&lt;T&gt; : MyClass&lt;T&gt;
    /// {
    /// }
    ///
    /// [SerializesGenericParameter(1)]
    /// public class MySubclass3&lt;TTypeOne, TTypeTwo&gt; : MyClass&lt;TTypeOne&gt;
    /// {
    /// }
    ///
    /// public class MyBehaviour : NetworkBehaviour
    /// {
    ///     public MySubclass1 TheValue;
    ///     public MySubclass2&lt;Bar&gt; TheValue;
    ///     public MySubclass3&lt;Baz, Qux&gt; TheValue;
    /// }
    /// </c>
    /// <br/>
    /// The above code will trigger generation of serialization code for <b>Foo</b> (passed directly to the
    /// base class), <b>Bar</b> (passed indirectly to the base class), <b>Baz</b> (passed indirectly to the base class),
    /// and <b>Qux</b> (marked as serializable in the subclass).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class GenerateSerializationForGenericParameterAttribute : Attribute
    {
        internal int ParameterIndex;

        /// <summary>
        /// Initializes a new instance of the attribute.
        /// </summary>
        /// <param name="parameterIndex">The zero-based index of the generic parameter that should be serialized.</param>
        public GenerateSerializationForGenericParameterAttribute(int parameterIndex)
        {
            ParameterIndex = parameterIndex;
        }
    }
}
