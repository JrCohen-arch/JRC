// Licensed under MIT license.
// Author: JRC
//
// Based on Microsoft's RBTree<K> from System.Data (Copyright Microsoft Corporation).
// Improvements: faster list enumeration, optimizations, simplified API.

using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace JRC.Collections.RedBlackTree
{
    public class RedBlackTypeSerializationInfo
    {
        /// <summary>
        /// Indicates if the comparer or sortkeyprovider of a RedBlackStructure might be binary serialized (obsolete(true) from microsoft from net80). Default : true.
        /// </summary>
        public bool SubObjectsBinarySerializationAllowed = true;

        private static Type SerializableAttributeType = typeof(SerializableAttribute);
        private static Type DataContractAttributeType = typeof(DataContractAttribute);

        private readonly object obj;
        private readonly Type type;
        private ComparerConstructorInfo comparerConstructorInfo;
        private bool? hasSerializableAttribute;
        private bool? hasMessagePackObjectAttribute;
        private bool? hasProtoContractAttribute;
        private bool? hasDataContractAttribute;

        public RedBlackTypeSerializationInfo(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            this.obj = obj;
            this.type = obj.GetType();            
        }

        /// <summary>
        /// Gets the obj
        /// </summary>
        public object Obj
        {
            get
            {
                return obj;
            }
        }

        /// <summary>
        /// gets the type
        /// </summary>
        public Type Type
        {
            get
            {
                return type;
            }
        }

        /// <summary>
        /// true if type type has a [Serializable] attribute
        /// </summary>
        public bool HasSerializableAttribute
        {
            get
            {
                return hasSerializableAttribute ?? (hasSerializableAttribute = type.GetCustomAttributes(SerializableAttributeType, false).Length > 0).Value;
            }
        }

        /// <summary>
        /// true if type type has a [MessagePackObject] attribute
        /// </summary>
        public bool HasMessagePackObjectAttribute
        {
            get
            {
                return hasMessagePackObjectAttribute ?? (hasMessagePackObjectAttribute = type.GetCustomAttributes(false).Any(a => a.GetType().FullName == "MessagePack.MessagePackObjectAttribute")).Value;
            }
        }

        /// <summary>
        /// true if type  type has a [ProtoContract] attribute
        /// </summary>
        public bool HasProtoContractAttribute
        {
            get
            {
                return hasProtoContractAttribute ?? (hasProtoContractAttribute = type.GetCustomAttributes(false).Any(a => a.GetType().FullName == "ProtoBuf.ProtoContractAttribute")).Value;
            }
        }

        /// <summary>
        /// true if type  type has a [DataContract] attribute
        /// </summary>
        public bool HasDataContractAttribute
        {
            get
            {
                return hasDataContractAttribute ?? (hasDataContractAttribute = type.GetCustomAttributes(DataContractAttributeType, false).Length > 0).Value;
            }
        }

        /// <summary>
        /// true if type  has a default public constructor
        /// </summary>
        public bool HasDefaultPublicConstructor
        {
            get
            {
                return (this.comparerConstructorInfo ?? new ComparerConstructorInfo(this.type)).HasDefaultPublicConstructor;
            }
        }

        /// <summary>
        /// true if type  has a dedicated System.Text.Json constructor
        /// </summary>
        public bool HasJsonTextConstructor
        {
            get
            {
                return (this.comparerConstructorInfo ?? new ComparerConstructorInfo(this.type)).HasJsonTextConstructor;
            }
        }

        /// <summary>
        /// true if type  has a dedicated Newtonsoft.Json constructor
        /// </summary>
        public bool HasJsonNewtonConstructor
        {
            get
            {
                return (this.comparerConstructorInfo ?? new ComparerConstructorInfo(this.type)).HasJsonNewtonConstructor;
            }
        }

        /// <summary>
        /// true if type  has a dedicated message pack constructor
        /// </summary>
        public bool HasMessagePackConstructor
        {
            get
            {
                return (this.comparerConstructorInfo ?? new ComparerConstructorInfo(this.type)).HasMessagePackConstructor;
            }
        }

        /// <summary>
        /// true if type  type is public or nested public.
        /// </summary>
        public bool IsPublic
        {
            get
            {
                return this.type.IsPublic || this.type.IsNestedPublic;
            }
        }

        /// <summary>
        /// Gets type simple name (without key and version)
        /// </summary>
        public string SimpleTypeName
        {
            get
            {
                return $"{this.type.FullName}, {this.type.Assembly.GetName().Name}";
            }
        }

        /// <summary>
        /// Gets type qualified name
        /// </summary>
        public string QualifiedName
        {
            get
            {
                return this.type.AssemblyQualifiedName;
            }
        }

        /// <summary>
        /// Returns known type containing the binary serialization of the comparer - if <see cref="ComparerBinarySerializationAllowed"/> is true.
        /// </summary>
        /// <returns></returns>
        public string GetBinaryKnowType()
        {
            if (SubObjectsBinarySerializationAllowed && this.HasSerializableAttribute)
            {
                var formatter = new BinaryFormatter { AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple };
                using (var mem = new MemoryStream())
                {
                    formatter.Serialize(mem, this.Obj);
                    return "Binary:" + Convert.ToBase64String(mem.ToArray());
                }
            }
            return null;
        }

        public static T GetObjFromKnownText<T>(string knownType)
        {
            int twoDotIndex;
            if (knownType.StartsWith("Binary") && (twoDotIndex = knownType.IndexOf(':')) == "Binary".Length)
            {
                var formatter = new BinaryFormatter { AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple };
                using (var mem = new MemoryStream(Convert.FromBase64String(knownType.Substring(twoDotIndex + 1))))
                {
                    return (T)formatter.Deserialize(mem);
                }
            }
            return default(T);
        }

        #region inner types
        sealed class ComparerConstructorInfo
        {
            public readonly bool HasDefaultPublicConstructor;
            public readonly bool HasJsonTextConstructor;
            public readonly bool HasJsonNewtonConstructor;
            public readonly bool HasMessagePackConstructor;

            public ComparerConstructorInfo(Type comparerType)
            {
                foreach (var ctor in comparerType.GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                {
                    if (ctor.IsPublic && ctor.GetParameters().Length == 0)
                    {
                        HasDefaultPublicConstructor = true;
                    }
                    var attributeNames = ctor.GetCustomAttributes(false).Select(a => a.GetType().FullName);
                    foreach (var attributeName in attributeNames)
                    {
                        if (attributeName == "System.Text.Json.Serialization.JsonConstructorAttribute")
                        {
                            HasJsonTextConstructor = true;
                        }
                        else if (attributeName == "Newtonsoft.Json.JsonConstructorAttribute")
                        {
                            HasJsonNewtonConstructor = true;
                        }
                        else if (attributeName == "MessagePack.SerializationConstructorAttribute")
                        {
                            HasMessagePackConstructor = true;
                        }
                    }
                }
            }
        }
        #endregion
    }
}
