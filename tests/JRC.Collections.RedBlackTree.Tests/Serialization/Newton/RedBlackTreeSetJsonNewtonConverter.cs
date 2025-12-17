using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JRC.Collections.RedBlackTree.Tests.Serialization.Newton
{
    public class RedBlackTreeSetJsonNewtonConverter<K> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(RedBlackTreeSet<K>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var treeSet = existingValue as RedBlackTreeSet<K> ?? new RedBlackTreeSet<K>();

            var jObject = JObject.Load(reader);

            // dupes ?
            treeSet.AllowDuplicates = jObject["allowDuplicates"]?.Value<bool>() ?? false;                    

            // comparers
            treeSet.Comparer = ReadComparer(serializer, jObject, "comparer");
            treeSet.SatelliteComparer = ReadComparer(serializer, jObject, "satelliteComparer");

            // Now populate items using Newtonsoft's Populate
            var itemsToken = jObject["items"];
            if (itemsToken != null)
            {
                using (var itemsReader = itemsToken.CreateReader())
                {
                    serializer.Populate(itemsReader, treeSet);
                }
            }

            return treeSet;
        }

        private static IComparer<K> ReadComparer(JsonSerializer serializer, JObject jObject, string propName)
        {
            IComparer<K> comparer = null;
            JToken comparerToken = jObject[propName];
            if (comparerToken != null)
            {
                var knownType = comparerToken["knownType"]?.Value<string>();
                if (knownType != null)
                {
                    comparer = RedBlackComparerSerializationInfo<K>.GetComparerFromKnownText(knownType);

                    if (comparer == null)
                    {
                        var comparerData = comparerToken["data"];
                        if (comparerData != null)
                        {
                            var comparerType = Type.GetType(knownType, true, true);
                            comparer = (IComparer<K>)comparerData.ToObject(comparerType, serializer);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Comparer data not found for type {knownType}");
                        }
                    }
                }
            }
            if (comparer == null)
            {
                throw new InvalidOperationException("No serialized comparer could be found in JSON stream");
            }

            return comparer;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var treeSet = (RedBlackTreeSet<K>)value;

            writer.WriteStartObject();

            writer.WritePropertyName("allowDuplicates");
            writer.WriteValue(treeSet.AllowDuplicates);

            // Write comparer
            WriteComparer(writer, serializer, "comparer", treeSet.Comparer);
            WriteComparer(writer, serializer, "satelliteComparer", treeSet.SatelliteComparer);

            // Write items - let Newtonsoft serialize the dictionary
            writer.WritePropertyName("items");
            // readonly dictionary is used to by pass circular reference check.
            // it does not copy values but simply wraps them
            // note that in 3.5 .net framework you might have to create your own wrapper
            serializer.Serialize(writer, new ReadOnlyCollectionWrapper<K>(treeSet));

            writer.WriteEndObject();
        }

        private static void WriteComparer(JsonWriter writer, JsonSerializer serializer, string propName, IComparer<K> comparer)
        {
            writer.WritePropertyName(propName);
            writer.WriteStartObject();

            var comparerInfo = new RedBlackComparerSerializationInfo<K>(comparer);
            var knownType = comparerInfo.GetKnownType();

            if (knownType != null)
            {
                writer.WritePropertyName("knownType");
                writer.WriteValue(knownType);
            }
            else
            {
                if (comparerInfo.IsPublic && (comparerInfo.HasDefaultPublicConstructor || comparerInfo.HasJsonNewtonConstructor))
                {
                    writer.WritePropertyName("knownType");
                    writer.WriteValue(comparerInfo.SimpleTypeName);
                    writer.WritePropertyName("data");
                    serializer.Serialize(writer, comparer);
                }
                else
                {
                    // RedBlackTypeSerializationInfo.SubObjectsBinarySerializationAllowed has to be true else GetBinaryKnowType() returns null
                    knownType = comparerInfo.GetBinaryKnowType();
                    if (knownType != null)
                    {
                        writer.WritePropertyName("knownType");
                        writer.WriteValue(knownType);
                    }
                    else
                    {
                        throw new InvalidOperationException($"KeyComparer of type {comparerInfo.Type.Name} cannot be serialized to JSON");
                    }
                }
            }

            writer.WriteEndObject(); // comparer
        }

        // tool class in order to wrap collection (copied in all converters that need it for convenience)
        sealed class ReadOnlyCollectionWrapper<T> : ICollection<T>, IEnumerable<T>
        {
            private readonly ICollection<T> _inner;

            public ReadOnlyCollectionWrapper(ICollection<T> inner)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            }

            public int Count => _inner.Count;
            public bool IsReadOnly => true;
            public bool Contains(T item) => _inner.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
            public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }
}
