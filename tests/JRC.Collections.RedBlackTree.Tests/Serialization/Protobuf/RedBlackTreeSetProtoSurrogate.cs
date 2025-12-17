using Newtonsoft.Json.Linq;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JRC.Collections.RedBlackTree.Tests.Serialization.Protobuf
{
    [ProtoContract]
    public class RedBlackTreeSetProtoSurrogate<K>
    {
        [ProtoMember(1)]
        public bool AllowDuplicates { get; set; }

        [ProtoMember(2)]
        public string ComparerKnownType { get; set; }

        [ProtoMember(3)]
        public byte[] ComparerData { get; set; }

        [ProtoMember(4)]
        public string SatelliteComparerKnownType { get; set; }

        [ProtoMember(5)]
        public byte[] SatelliteComparerData { get; set; }

        [ProtoMember(6)]
        public K[] Items { get; set; }

        // dict → proto
        [ProtoConverter]
        public static RedBlackTreeSetProtoSurrogate<K> Convert(RedBlackTreeSet<K> set)
        {
            if (set == null)
            {
                return new RedBlackTreeSetProtoSurrogate<K>(); // protobuf strange behavior... called on deserialization in order to get empty surrogate
            }

            string knownType;
            byte[] comparerData;
            ComparerToData(set.Comparer, out knownType, out comparerData);

            string satelliteKnownType;
            byte[] satelliteComparerData;
            ComparerToData(set.SatelliteComparer, out satelliteKnownType, out satelliteComparerData);

            return new RedBlackTreeSetProtoSurrogate<K>
            {
                AllowDuplicates = set.AllowDuplicates,
                ComparerKnownType = knownType,
                ComparerData = comparerData,
                SatelliteComparerKnownType = satelliteKnownType,
                SatelliteComparerData = satelliteComparerData,
                Items = set.ToArray()
            };
        }

        private static void ComparerToData(IComparer<K> comparer, out string knownType, out byte[] comparerData)
        {
            var comparerInfo = new RedBlackComparerSerializationInfo<K>(comparer);
            knownType = comparerInfo.GetKnownType();
            comparerData = null;
            if (knownType == null)
            {
                if (comparerInfo.IsPublic && comparerInfo.HasDefaultPublicConstructor && (comparerInfo.HasProtoContractAttribute || comparerInfo.HasDataContractAttribute))
                {
                    try
                    {
                        using (var mem = new MemoryStream())
                        {
                            Serializer.Serialize(mem, comparer);
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
        }

        // proto → dict
        [ProtoConverter]
        public static RedBlackTreeSet<K> Convert(RedBlackTreeSetProtoSurrogate<K> proto)
        {
            IComparer<K> comparer = DataToComparer(proto.ComparerData, proto.ComparerKnownType);
            IComparer<K> satelliteComparer = DataToComparer(proto.SatelliteComparerData, proto.SatelliteComparerKnownType);

            var dict = new RedBlackTreeSet<K>(proto.AllowDuplicates, comparer, satelliteComparer);
            if (proto.Items != null)
            {
                foreach (var key in proto.Items)
                {
                    dict.Add(key);
                }
            }

            return dict;
        }

        private static IComparer<K> DataToComparer(byte[] comparerData, string comparerKnownType)
        {
            IComparer<K> comparer;
            if (comparerData != null)
            {
                var type = Type.GetType(comparerKnownType, true, true);
                using (var mem = new MemoryStream(comparerData))
                {
                    comparer = (IComparer<K>)Serializer.Deserialize(type, mem);
                }
            }
            else
            {
                comparer = RedBlackComparerSerializationInfo<K>.GetComparerFromKnownText(comparerKnownType);
            }
            if (comparer == null)
            {
                throw new InvalidOperationException($"Cannot restore comparer from {comparerKnownType}");
            }

            return comparer;
        }
    }
}
