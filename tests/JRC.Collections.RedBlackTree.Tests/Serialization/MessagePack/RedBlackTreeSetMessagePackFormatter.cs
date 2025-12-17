using MessagePack;
using MessagePack.Formatters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JRC.Collections.RedBlackTree.Tests.Serialization.MessagePack
{
    public class RedBlackTreeSetMessagePackFormatter<K> : IMessagePackFormatter<RedBlackTreeSet<K>>
    {
        public void Serialize(ref MessagePackWriter writer, RedBlackTreeSet<K> value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(4); // allowDuplicates + comparer + satelliteComparer + items
            // dupes
            writer.Write("allowDuplicates");
            writer.Write(value.AllowDuplicates);
            // Write comparers            
            WriteComparer(ref writer, options, "comparer", value.Comparer);
            WriteComparer(ref writer, options, "satelliteComparer", value.SatelliteComparer);
            // Write items
            writer.Write("items");
            writer.WriteMapHeader(value.Count);
            foreach (K key in value)
            {
                var keyFormatter = options.Resolver.GetFormatterWithVerify<K>();
                keyFormatter.Serialize(ref writer, key, options);
            }
        }

        private static void WriteComparer(ref MessagePackWriter writer, MessagePackSerializerOptions options, string propName, IComparer<K> comparer)
        {
            writer.Write(propName);
            writer.WriteMapHeader(2); // knownType + data?

            var comparerInfo = new RedBlackComparerSerializationInfo<K>(comparer);
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

        public RedBlackTreeSet<K> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            string key;
            int count;            
            var treeSet = new RedBlackTreeSet<K>();

            count = reader.ReadMapHeader();
            if (count != 4)
            {
                throw new InvalidOperationException("MapHeader of size 4 (root) is expected in order to deserialize RedBlackTreeSet<K>.");
            }

            key = reader.ReadString();
            if (key != "allowDuplicates")
            {
                throw new InvalidOperationException($"Key 'allowDuplicates' is expected in order to deserialize RedBlackTreeSet<K>.");
            }
            treeSet.AllowDuplicates = reader.ReadBoolean();
            treeSet.Comparer = ReadComparer(ref reader, options, "comparer");
            treeSet.SatelliteComparer = ReadComparer(ref reader, options, "satelliteComparer");

            #region items
            key = reader.ReadString();
            if (key != "items")
            {
                throw new InvalidOperationException("Key 'items' is expected in order to deserialize ReadBlackTreeDictionary comparer.");
            }
            count = reader.ReadMapHeader();
            var keyFormatter = options.Resolver.GetFormatterWithVerify<K>();
            for (int i = 0; i < count; i++)
            {
                var keyItem = keyFormatter.Deserialize(ref reader, options);
                treeSet.Add(keyItem);
            }
            #endregion

            return treeSet;
        }

        private static IComparer<K> ReadComparer(ref MessagePackReader reader, MessagePackSerializerOptions options, string propName)
        {
            string key = reader.ReadString();
            if (key != propName)
            {
                throw new InvalidOperationException($"Key '{propName}' is expected in order to deserialize RedBlackTreeSet<K>.");
            }
            int count = reader.ReadMapHeader();
            if (count != 2)
            {
                throw new InvalidOperationException($"MapHeader of size 2 ({propName}) is expected in order to deserialize RedBlackTreeSet<K>.");
            }
            key = reader.ReadString();
            if (key != "knownType")
            {
                throw new InvalidOperationException($"Key 'knownType' is expected in order to deserialize RedBlackTreeSet<K> {propName}.");
            }
            var knownType = reader.ReadString();
            if (string.IsNullOrEmpty(knownType))
            {
                throw new InvalidOperationException($"Data 'knownType' is expected in order to deserialize RedBlackTreeSet<K> {propName}.");
            }
            var comparer = RedBlackComparerSerializationInfo<K>.GetComparerFromKnownText(knownType);
            key = reader.ReadString();
            if (key != "data")
            {
                throw new InvalidOperationException($"Key 'data' is expected in order to deserialize RedBlackTreeSet<K> {propName}.");
            }
            if (comparer != null)
            {
                if (!reader.TryReadNil())
                {
                    throw new InvalidOperationException($"Nil 'data' is expected in order to deserialize RedBlackTreeSet<K> {propName}.");
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
                comparer = (IComparer<K>)MessagePackSerializer.Deserialize(comparerType, comparerBytes.Value, options);
            }

            return comparer;
        }
    }
}
