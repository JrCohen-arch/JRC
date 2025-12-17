using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JRC.Collections.RedBlackTree.Tests.Serialization.Protobuf
{
    [ProtoContract]
    public class RedBlackTreeSetSortedKeyProtoSurrogate<TItem, TSortedKey>
    {
        [ProtoMember(1)]
        public bool AllowDuplicates { get; set; }

        [ProtoMember(2)]
        public string ProviderKnownType { get; set; }

        [ProtoMember(3)]
        public byte[] ProviderData { get; set; }

        [ProtoMember(4)]
        public string ComparerKnownType { get; set; }

        [ProtoMember(5)]
        public byte[] ComparerData { get; set; }

        [ProtoMember(6)]
        public string SatelliteComparerKnownType { get; set; }

        [ProtoMember(7)]
        public byte[] SatelliteComparerData { get; set; }

        [ProtoMember(8)]
        public TItem[] Items { get; set; }

        // dict → proto
        [ProtoConverter]
        public static RedBlackTreeSetSortedKeyProtoSurrogate<TItem, TSortedKey> Convert(RedBlackTreeSet<TItem, TSortedKey> set)
        {
            if (set == null)
            {
                return new RedBlackTreeSetSortedKeyProtoSurrogate<TItem, TSortedKey>(); // protobuf strange behavior... called on deserialization in order to get empty surrogate
            }

            string providerKnownType;
            byte[] providerData;
            ProviderToData(set.SortKeyProvider, out providerKnownType, out providerData);

            string comparerKnownType;
            byte[] comparerData;
            ComparerToData(set.SortKeyComparer, out comparerKnownType, out comparerData);

            string satelliteKnownType;
            byte[] satelliteComparerData;
            ComparerToData(set.SatelliteComparer, out satelliteKnownType, out satelliteComparerData);

            return new RedBlackTreeSetSortedKeyProtoSurrogate<TItem, TSortedKey>
            {
                AllowDuplicates = set.AllowDuplicates,
                ProviderKnownType = providerKnownType,
                ProviderData = providerData,
                ComparerKnownType = comparerKnownType,
                ComparerData = comparerData,
                SatelliteComparerKnownType = satelliteKnownType,
                SatelliteComparerData = satelliteComparerData,
                Items = set.ToArray()
            };
        }

        private static void ProviderToData(RedBlackTreeSet<TItem, TSortedKey>.ISortKeyProvider provider, out string knownType, out byte[] providerData)
        {
            var providerInfo = new RedBlackTypeSerializationInfo(provider);
            knownType = null;
            providerData = null;
            if (providerInfo.IsPublic && providerInfo.HasDefaultPublicConstructor && (providerInfo.HasProtoContractAttribute || providerInfo.HasDataContractAttribute))
            {
                try
                {
                    using (var mem = new MemoryStream())
                    {
                        Serializer.Serialize(mem, provider);
                        providerData = mem.ToArray();
                        knownType = providerInfo.SimpleTypeName;
                    }
                }
                catch
                {
                    // fallback
                }
            }
            if (knownType == null)
            {
                // RedBlackTypeSerializationInfo.SubObjectsBinarySerializationAllowed has to be true else GetBinaryKnowType() returns null
                knownType = providerInfo.GetBinaryKnowType();
            }

            if (knownType == null)
            {
                throw new InvalidOperationException($"Sort key provider {providerInfo.Type.Name} cannot be serialized");
            }
        }

        private static void ComparerToData<T>(IComparer<T> comparer, out string knownType, out byte[] comparerData)
        {
            var comparerInfo = new RedBlackComparerSerializationInfo<T>(comparer);
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
        public static RedBlackTreeSet<TItem, TSortedKey> Convert(RedBlackTreeSetSortedKeyProtoSurrogate<TItem, TSortedKey> proto)
        {
            RedBlackTreeSet<TItem, TSortedKey>.ISortKeyProvider provider = DataToProvider(proto.ProviderData, proto.ProviderKnownType);
            IComparer<TSortedKey> comparer = DataToComparer<TSortedKey>(proto.ComparerData, proto.ComparerKnownType);
            IComparer<TItem> satelliteComparer = DataToComparer<TItem>(proto.SatelliteComparerData, proto.SatelliteComparerKnownType);

            var dict = new RedBlackTreeSet<TItem, TSortedKey>(proto.AllowDuplicates, provider, comparer, satelliteComparer);
            if (proto.Items != null)
            {
                foreach (var key in proto.Items)
                {
                    dict.Add(key);
                }
            }

            return dict;
        }

        private static RedBlackTreeSet<TItem, TSortedKey>.ISortKeyProvider DataToProvider(byte[] providerData, string providerKnownType)
        {
            RedBlackTreeSet<TItem, TSortedKey>.ISortKeyProvider provider;
            if (providerData != null)
            {
                var type = Type.GetType(providerKnownType, true, true);
                using (var mem = new MemoryStream(providerData))
                {
                    provider = (RedBlackTreeSet<TItem, TSortedKey>.ISortKeyProvider)Serializer.Deserialize(type, mem);
                }
            }
            else
            {
                provider = RedBlackTypeSerializationInfo.GetObjFromKnownText<RedBlackTreeSet<TItem, TSortedKey>.ISortKeyProvider>(providerKnownType);
            }
            if (provider == null)
            {
                throw new InvalidOperationException($"Cannot restore sort key provider from {providerKnownType}");
            }

            return provider;
        }

        private static IComparer<T> DataToComparer<T>(byte[] comparerData, string comparerKnownType)
        {
            IComparer<T> comparer;
            if (comparerData != null)
            {
                var type = Type.GetType(comparerKnownType, true, true);
                using (var mem = new MemoryStream(comparerData))
                {
                    comparer = (IComparer<T>)Serializer.Deserialize(type, mem);
                }
            }
            else
            {
                comparer = RedBlackComparerSerializationInfo<T>.GetComparerFromKnownText(comparerKnownType);
            }
            if (comparer == null)
            {
                throw new InvalidOperationException($"Cannot restore comparer from {comparerKnownType}");
            }

            return comparer;
        }
    }
}
