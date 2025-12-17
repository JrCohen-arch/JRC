using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JRC.Collections.RedBlackTree.Tests.Serialization.Protobuf
{
    [ProtoContract]
    public class RedBlackTreeDictionaryProtoSurrogate<TKey, TValue>
    {
        [ProtoMember(1)]
        public string ComparerKnownType { get; set; }

        [ProtoMember(2)]
        public byte[] ComparerData { get; set; }

        [ProtoMember(3)]
        public KeyValuePair<TKey, TValue>[] Items { get; set; }

        // dict → proto
        [ProtoConverter]
        public static RedBlackTreeDictionaryProtoSurrogate<TKey, TValue> Convert(RedBlackTreeDictionary<TKey, TValue> dict)
        {
            if (dict == null)  
            {
                return new RedBlackTreeDictionaryProtoSurrogate<TKey, TValue>(); // protobuf strange behavior... called on deserialization in order to get empty surrogate
            }

            var comparerInfo = new RedBlackComparerSerializationInfo<TKey>(dict.Comparer);
            var knownType = comparerInfo.GetKnownType();
            byte[] comparerData = null;

            if (knownType == null)
            {
                if (comparerInfo.IsPublic && comparerInfo.HasDefaultPublicConstructor && (comparerInfo.HasProtoContractAttribute || comparerInfo.HasDataContractAttribute))
                {
                    try
                    {
                        using (var mem = new MemoryStream())
                        {
                            Serializer.Serialize(mem, dict.Comparer);
                            comparerData = mem.ToArray();
                            knownType = comparerInfo.SimpleTypeName;
                        }
                    }
                    catch
                    {
                        // fallback
                    }
                }
            }
            if (knownType == null)
            {
                // RedBlackTypeSerializationInfo.SubObjectsBinarySerializationAllowed has to be true else GetBinaryKnowType() returns null
                knownType = comparerInfo.GetBinaryKnowType();                
            }

            if (knownType == null)
            {
                throw new InvalidOperationException($"Comparer {comparerInfo.Type.Name} cannot be serialized");
            }

            return new RedBlackTreeDictionaryProtoSurrogate<TKey, TValue>
            {
                ComparerKnownType = knownType,
                ComparerData = comparerData,
                Items = dict.ToArray()
            };
        }

        // proto → dict
        [ProtoConverter]
        public static RedBlackTreeDictionary<TKey, TValue> Convert(RedBlackTreeDictionaryProtoSurrogate<TKey, TValue> proto)
        {
            IComparer<TKey> comparer = null;
            if (proto.ComparerData != null)
            {
                var type = Type.GetType(proto.ComparerKnownType, true, true);
                using (var mem = new MemoryStream(proto.ComparerData))
                {
                    comparer = (IComparer<TKey>)Serializer.Deserialize(type, mem);
                }
            }
            else
            {
                comparer = RedBlackComparerSerializationInfo<TKey>.GetComparerFromKnownText(proto.ComparerKnownType);
            }
            if (comparer == null)
            {
                throw new InvalidOperationException($"Cannot restore comparer from {proto.ComparerKnownType}");
            }

            var dict = new RedBlackTreeDictionary<TKey, TValue>(comparer);
            if (proto.Items != null)
            {
                foreach (var kvp in proto.Items)
                {
                    dict.Add(kvp.Key, kvp.Value);
                }
            }

            return dict;
        }
    }
}
