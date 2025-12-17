using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace JRC.Collections.RedBlackTree.Tests.Serialization.Newton
{
    public class RedBlackTreeSetSortedKeyJsonNewtonConverter<TItem, TSortKey> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(RedBlackTreeSet<TItem, TSortKey>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var treeSet = existingValue as RedBlackTreeSet<TItem, TSortKey> ?? new RedBlackTreeSet<TItem, TSortKey>();

            var jObject = JObject.Load(reader);

            // dupes ?
            treeSet.AllowDuplicates = jObject["allowDuplicates"]?.Value<bool>() ?? false;

            // sortkey provider
            treeSet.SortKeyProvider = ReadProvider(serializer, jObject);

            // comparers
            treeSet.SortKeyComparer = ReadComparer<TSortKey>(serializer, jObject, "comparer");
            treeSet.SatelliteComparer = ReadComparer<TItem>(serializer, jObject, "satelliteComparer");

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

        private static RedBlackTreeSet<TItem, TSortKey>.ISortKeyProvider ReadProvider(JsonSerializer serializer, JObject jObject)
        {
            RedBlackTreeSet<TItem, TSortKey>.ISortKeyProvider provider = null;
            JToken providerToken = jObject["provider"];
            if (providerToken != null)
            {
                var knownType = providerToken["knownType"]?.Value<string>();
                if (knownType != null)
                {
                    provider = RedBlackTypeSerializationInfo.GetObjFromKnownText<RedBlackTreeSet<TItem, TSortKey>.ISortKeyProvider>(knownType);

                    if (provider == null)
                    {
                        var providerData = providerToken["data"];
                        if (providerData != null)
                        {
                            var providerType = Type.GetType(knownType, true, true);
                            provider = (RedBlackTreeSet<TItem, TSortKey>.ISortKeyProvider)providerData.ToObject(providerType, serializer);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Sort Key Provider data not found for type {knownType}");
                        }
                    }
                }
            }
            if (provider == null)
            {
                throw new InvalidOperationException("No serialized sort key provider could be found in JSON stream");
            }

            return provider;
        }

        private static IComparer<T> ReadComparer<T>(JsonSerializer serializer, JObject jObject, string propName)
        {
            IComparer<T> comparer = null;
            JToken comparerToken = jObject[propName];
            if (comparerToken != null)
            {
                var knownType = comparerToken["knownType"]?.Value<string>();
                if (knownType != null)
                {
                    comparer = RedBlackComparerSerializationInfo<T>.GetComparerFromKnownText(knownType);

                    if (comparer == null)
                    {
                        var comparerData = comparerToken["data"];
                        if (comparerData != null)
                        {
                            var comparerType = Type.GetType(knownType, true, true);
                            comparer = (IComparer<T>)comparerData.ToObject(comparerType, serializer);
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
            var treeSet = (RedBlackTreeSet<TItem, TSortKey>)value;

            writer.WriteStartObject();

            writer.WritePropertyName("allowDuplicates");
            writer.WriteValue(treeSet.AllowDuplicates);

            // Write provider
            WriteProvider(writer, serializer,  treeSet.SortKeyProvider);

            // Write comparer
            WriteComparer(writer, serializer, "comparer", treeSet.SortKeyComparer);
            WriteComparer(writer, serializer, "satelliteComparer", treeSet.SatelliteComparer);

            // Write items - let Newtonsoft serialize the dictionary
            writer.WritePropertyName("items");
            // readonly dictionary is used to by pass circular reference check.
            // it does not copy values but simply wraps them
            // note that in 3.5 .net framework you might have to create your own wrapper
            serializer.Serialize(writer, new ReadOnlyCollectionWrapper<TItem>(treeSet));

            writer.WriteEndObject();
        }

        private static void WriteProvider(JsonWriter writer, JsonSerializer serializer, RedBlackTreeSet<TItem, TSortKey>.ISortKeyProvider provider)
        {
            writer.WritePropertyName("provider");
            writer.WriteStartObject();

            var providerInfo = new RedBlackTypeSerializationInfo(provider);            

            if (providerInfo.IsPublic && (providerInfo.HasDefaultPublicConstructor || providerInfo.HasJsonNewtonConstructor))
            {
                writer.WritePropertyName("knownType");
                writer.WriteValue(providerInfo.SimpleTypeName);
                writer.WritePropertyName("data");
                serializer.Serialize(writer, provider);
            }
            else
            {
                // RedBlackTypeSerializationInfo.SubObjectsBinarySerializationAllowed has to be true else GetBinaryKnowType() returns null
                var knownType = providerInfo.GetBinaryKnowType();
                if (knownType != null)
                {
                    writer.WritePropertyName("knownType");
                    writer.WriteValue(knownType);
                }
                else
                {
                    throw new InvalidOperationException($"SortKeyProvider of type {providerInfo.Type.Name} cannot be serialized to JSON");
                }
            }

            writer.WriteEndObject(); // comparer
        }

        private static void WriteComparer<T>(JsonWriter writer, JsonSerializer serializer, string propName, IComparer<T> comparer)
        {
            writer.WritePropertyName(propName);
            writer.WriteStartObject();

            var comparerInfo = new RedBlackComparerSerializationInfo<T>(comparer);
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
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

    }
}
