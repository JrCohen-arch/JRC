using MessagePack;
using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.IO;

namespace JRC.Collections.RedBlackTree.Tests.Serialization.MessagePack
{
    public class RedBlackTreeSetSortedKeyMessagePackFormatter<TItem, TSortedKey> : IMessagePackFormatter<RedBlackTreeSet<TItem, TSortedKey>>
    {
        public void Serialize(ref MessagePackWriter writer, RedBlackTreeSet<TItem, TSortedKey> value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(5); // allowDuplicates + provider + comparer + satelliteComparer + items
            // dupes
            writer.Write("allowDuplicates");
            writer.Write(value.AllowDuplicates);
            // Write provide
            WriteProvider(ref writer, options, value.SortKeyProvider);
            // Write comparers            
            WriteComparer(ref writer, options, "comparer", value.SortKeyComparer);
            WriteComparer(ref writer, options, "satelliteComparer", value.SatelliteComparer);
            // Write items
            writer.Write("items");
            writer.WriteMapHeader(value.Count);
            foreach (TItem key in value)
            {
                var keyFormatter = options.Resolver.GetFormatterWithVerify<TItem>();
                keyFormatter.Serialize(ref writer, key, options);
            }
        }

        private static void WriteProvider(ref MessagePackWriter writer, MessagePackSerializerOptions options, RedBlackTreeSet<TItem, TSortedKey>.ISortKeyProvider provider)
        {
            writer.Write("provider");
            writer.WriteMapHeader(2); // knownType + data?

            var providerInfo = new RedBlackTypeSerializationInfo(provider);
            string knownType = null;
            byte[] providerPackData = null;
            try
            {
                using (var mem = new MemoryStream())
                {
                    MessagePackSerializer.Serialize(provider.GetType(), mem, provider, options);
                    providerPackData = mem.ToArray();
                }
                knownType = providerInfo.SimpleTypeName;
            }
            catch { /*oops we'll need fallback */ }

            // RedBlackTypeSerializationInfo.SubObjectsBinarySerializationAllowed has to be true else GetBinaryKnowType() returns null
            knownType = knownType ?? providerInfo.GetBinaryKnowType(); // fallback binary
            if (knownType == null)
            {
                throw new InvalidOperationException($"Sort Key Provider of type {providerInfo.Type.Name} cannot be serialized");
            }
            writer.Write("knownType");
            writer.Write(knownType);
            writer.Write("data");
            if (providerPackData == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(providerPackData);
            }

        }

        private static void WriteComparer<T>(ref MessagePackWriter writer, MessagePackSerializerOptions options, string propName, IComparer<T> comparer)
        {
            writer.Write(propName);
            writer.WriteMapHeader(2); // knownType + data?

            var comparerInfo = new RedBlackComparerSerializationInfo<T>(comparer);
            string knownType = comparerInfo.GetKnownType();
            byte[] comparerPackData = null;
            if (knownType == null)
            {
                try
                {
                    using (var mem = new MemoryStream())
                    {
                        MessagePackSerializer.Serialize(comparer.GetType(), mem, comparer, options);
                        comparerPackData = mem.ToArray();
                    }
                    knownType = comparerInfo.SimpleTypeName;
                }
                catch { /*oops we'll need fallback */ }
            }

            // RedBlackTypeSerializationInfo.SubObjectsBinarySerializationAllowed has to be true else GetBinaryKnowType() returns null
            knownType = knownType ?? comparerInfo.GetBinaryKnowType(); // fallback binary
            if (knownType == null)
            {
                throw new InvalidOperationException($"KeyComparer of type {comparerInfo.Type.Name} cannot be serialized");
            }
            writer.Write("knownType");
            writer.Write(knownType);
            writer.Write("data");
            if (comparerPackData == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(comparerPackData);
            }

        }

        public RedBlackTreeSet<TItem, TSortedKey> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            string key;
            int count;
            var treeSet = new RedBlackTreeSet<TItem, TSortedKey>();

            count = reader.ReadMapHeader();
            if (count != 5)
            {
                throw new InvalidOperationException("MapHeader of size 5 (root) is expected in order to deserialize RedBlackTreeSet<K>.");
            }

            key = reader.ReadString();
            if (key != "allowDuplicates")
            {
                throw new InvalidOperationException($"Key 'allowDuplicates' is expected in order to deserialize RedBlackTreeSet<K>.");
            }
            treeSet.AllowDuplicates = reader.ReadBoolean();
            treeSet.SortKeyProvider = ReadProvider(ref reader, options);
            treeSet.SortKeyComparer = ReadComparer<TSortedKey>(ref reader, options, "comparer");
            treeSet.SatelliteComparer = ReadComparer<TItem>(ref reader, options, "satelliteComparer");

            #region items
            key = reader.ReadString();
            if (key != "items")
            {
                throw new InvalidOperationException("Key 'items' is expected in order to deserialize ReadBlackTreeDictionary comparer.");
            }
            count = reader.ReadMapHeader();
            var keyFormatter = options.Resolver.GetFormatterWithVerify<TItem>();
            for (int i = 0; i < count; i++)
            {
                var keyItem = keyFormatter.Deserialize(ref reader, options);
                treeSet.Add(keyItem);
            }
            #endregion

            return treeSet;
        }

        private static RedBlackTreeSet<TItem, TSortedKey>.ISortKeyProvider ReadProvider(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            string key = reader.ReadString();
            if (key != "provider")
            {
                throw new InvalidOperationException($"Key 'provider' is expected in order to deserialize RedBlackTreeSet<TItem, TSortKey>.");
            }
            int count = reader.ReadMapHeader();
            if (count != 2)
            {
                throw new InvalidOperationException($"MapHeader of size 2 (provider) is expected in order to deserialize RedBlackTreeSet<TItem, TSortKey>.");
            }
            key = reader.ReadString();
            if (key != "knownType")
            {
                throw new InvalidOperationException($"Key 'knownType' is expected in order to deserialize RedBlackTreeSet<TItem, TSortKey> provider.");
            }
            var knownType = reader.ReadString();
            if (string.IsNullOrEmpty(knownType))
            {
                throw new InvalidOperationException($"Data 'knownType' is expected in order to deserialize RedBlackTreeSet<TItem, TSortKey> provider.");
            }
            var comparer = RedBlackTypeSerializationInfo.GetObjFromKnownText<RedBlackTreeSet<TItem, TSortedKey>.ISortKeyProvider>(knownType);
            key = reader.ReadString();
            if (key != "data")
            {
                throw new InvalidOperationException($"Key 'data' is expected in order to deserialize RedBlackTreeSet<TItem, TSortKey> provider.");
            }
            if (comparer != null)
            {
                if (!reader.TryReadNil())
                {
                    throw new InvalidOperationException($"Nil 'data' is expected in order to deserialize RedBlackTreeSet<TItem, TSortKey> provider.");
                }
            }
            else
            {
                var providerType = Type.GetType(knownType, true, true);
                var providerBytes = reader.ReadBytes();
                if (!providerBytes.HasValue)
                {
                    throw new InvalidOperationException($"Non Nil 'data' is expected in order to deserialize RedBlackTreeSet<TItem, TSortKey> provider.");
                }
                comparer = (RedBlackTreeSet<TItem, TSortedKey>.ISortKeyProvider)MessagePackSerializer.Deserialize(providerType, providerBytes.Value, options);
            }

            return comparer;
        }

        private static IComparer<T> ReadComparer<T>(ref MessagePackReader reader, MessagePackSerializerOptions options, string propName)
        {
            string key = reader.ReadString();
            if (key != propName)
            {
                throw new InvalidOperationException($"Key '{propName}' is expected in order to deserialize RedBlackTreeSet<TItem, TSortKey>.");
            }
            int count = reader.ReadMapHeader();
            if (count != 2)
            {
                throw new InvalidOperationException($"MapHeader of size 2 ({propName}) is expected in order to deserialize RedBlackTreeSet<TItem, TSortKey>.");
            }
            key = reader.ReadString();
            if (key != "knownType")
            {
                throw new InvalidOperationException($"Key 'knownType' is expected in order to deserialize RedBlackTreeSet<TItem, TSortKey> {propName}.");
            }
            var knownType = reader.ReadString();
            if (string.IsNullOrEmpty(knownType))
            {
                throw new InvalidOperationException($"Data 'knownType' is expected in order to deserialize RedBlackTreeSet<TItem, TSortKey> {propName}.");
            }
            var comparer = RedBlackComparerSerializationInfo<T>.GetComparerFromKnownText(knownType);
            key = reader.ReadString();
            if (key != "data")
            {
                throw new InvalidOperationException($"Key 'data' is expected in order to deserialize RedBlackTreeSet<TItem, TSortKey> {propName}.");
            }
            if (comparer != null)
            {
                if (!reader.TryReadNil())
                {
                    throw new InvalidOperationException($"Nil 'data' is expected in order to deserialize RedBlackTreeSet<TItem, TSortKey> {propName}.");
                }
            }
            else
            {
                var comparerType = Type.GetType(knownType, true, true);
                var comparerBytes = reader.ReadBytes();
                if (!comparerBytes.HasValue)
                {
                    throw new InvalidOperationException($"Non Nil 'data' is expected in order to deserialize RedBlackTreeSet<K> {propName}.");
                }
                comparer = (IComparer<T>)MessagePackSerializer.Deserialize(comparerType, comparerBytes.Value, options);
            }

            return comparer;
        }
    }
}
