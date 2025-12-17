using MessagePack;
using MessagePack.Formatters;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JRC.Collections.RedBlackTree.Tests.Serialization.MessagePack
{
    public class RedBlackTreeDictionaryMessagePackFormatter<TKey, TValue> : IMessagePackFormatter<RedBlackTreeDictionary<TKey, TValue>>
    {
        public void Serialize(ref MessagePackWriter writer, RedBlackTreeDictionary<TKey, TValue> value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(2); // comparer + items

            // Write comparer
            writer.Write("comparer");
            writer.WriteMapHeader(2); // knownType + data?
            var comparerInfo = new RedBlackComparerSerializationInfo<TKey>(value.Comparer);
            string knownType = comparerInfo.GetKnownType();
            byte[] comparerPackData = null;
            if (knownType == null)
            {
                try
                {                  
                    using(var mem = new MemoryStream())
                    {
                        MessagePackSerializer.Serialize(value.Comparer.GetType(), mem, value.Comparer, options);
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
            // Write items
            writer.Write("items");
            writer.WriteMapHeader(value.Count);
            foreach (var kvp in value)
            {
                var keyFormatter = options.Resolver.GetFormatterWithVerify<TKey>();
                var valueFormatter = options.Resolver.GetFormatterWithVerify<TValue>();

                keyFormatter.Serialize(ref writer, kvp.Key, options);
                valueFormatter.Serialize(ref writer, kvp.Value, options);
            }
        }

        public RedBlackTreeDictionary<TKey, TValue> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            int count;
            string key;

            var dict = new RedBlackTreeDictionary<TKey, TValue>();

            count = reader.ReadMapHeader();
            if (count != 2)
            {
                throw new InvalidOperationException("MapHeader of size 2 (root) is expected in order to deserialize ReadBlackTreeDictionary.");
            }
           
            #region comparer
            key = reader.ReadString();
            if (key != "comparer")
            {
                throw new InvalidOperationException("Key 'comparer' is expected in order to deserialize ReadBlackTreeDictionary.");
            }
            count = reader.ReadMapHeader();
            if (count != 2)
            {
                throw new InvalidOperationException("MapHeader of size 2 (comparer) is expected in order to deserialize ReadBlackTreeDictionary.");
            }
            key = reader.ReadString();
            if (key != "knownType")
            {
                throw new InvalidOperationException("Key 'knownType' is expected in order to deserialize ReadBlackTreeDictionary comparer.");
            }
            var knownType = reader.ReadString();
            if (string.IsNullOrEmpty(knownType))
            {
                throw new InvalidOperationException("Data 'knownType' is expected in order to deserialize ReadBlackTreeDictionary comparer.");
            }
            IComparer<TKey> comparer = RedBlackComparerSerializationInfo<TKey>.GetComparerFromKnownText(knownType);
            key = reader.ReadString();
            if (key != "data")
            {
                throw new InvalidOperationException("Key 'data' is expected in order to deserialize ReadBlackTreeDictionary comparer.");
            }
            if (comparer != null)
            {
                if (!reader.TryReadNil())
                {
                    throw new InvalidOperationException("Nil 'data' is expected in order to deserialize ReadBlackTreeDictionary comparer.");
                }
            }
            else
            {
                var comparerType = Type.GetType(knownType, true, true);
                var comparerBytes = reader.ReadBytes();
                if (!comparerBytes.HasValue)
                {
                    throw new InvalidOperationException("Non Nil 'data' is expected in order to deserialize ReadBlackTreeDictionary comparer.");
                }
                comparer = (IComparer<TKey>) MessagePackSerializer.Deserialize(comparerType, comparerBytes.Value, options);
            }

            dict.Comparer = comparer;
            #endregion

            #region items
            key = reader.ReadString();
            if (key != "items")
            {
                throw new InvalidOperationException("Key 'items' is expected in order to deserialize ReadBlackTreeDictionary comparer.");
            }
            count = reader.ReadMapHeader();           
            var keyFormatter = options.Resolver.GetFormatterWithVerify<TKey>();
            var valueFormatter = options.Resolver.GetFormatterWithVerify<TValue>();
            for (int i = 0; i < count; i++)
            {
                
                var pairKey = keyFormatter.Deserialize(ref reader, options);
                var pairValue = valueFormatter.Deserialize(ref reader, options);
                dict.Add(pairKey, pairValue);
            }
            #endregion

            return dict;
        }
    }
}
