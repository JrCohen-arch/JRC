using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JRC.Collections.RedBlackTree.Tests.Serialization.JsonText
{
    public class RedBlackTreeSetJsonTextConverter<K> : JsonConverter<RedBlackTreeSet<K>>
    {
        public override RedBlackTreeSet<K> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject");

            JsonElement? comparerElement = null;
            JsonElement? satelliteComparerElement = null;
            JsonElement? itemsElement = null;
            bool? allowDuplicates = null;

            #region read values
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected PropertyName");

                var propertyName = reader.GetString();
                reader.Read(); // Move to value

                if (propertyName == "allowDuplicates")
                {
                    allowDuplicates = reader.GetBoolean();
                }
                else if (propertyName == "comparer")
                {
                    comparerElement = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
                }
                else if (propertyName == "satelliteComparer")
                {
                    satelliteComparerElement = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
                }
                else if (propertyName == "items")
                {
                    itemsElement = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
                }
                else
                {
                    reader.Skip();
                }
            }
            #endregion

            var treeSet = new RedBlackTreeSet<K>(allowDuplicates ?? false);

            #region comparer
            if (!comparerElement.HasValue)
                throw new InvalidOperationException("No serialized comparer could be found in JSON stream");

            treeSet.Comparer = ReadComparer(options, comparerElement);
            #endregion

            #region satelliteComparer
            if (!satelliteComparerElement.HasValue)
                throw new InvalidOperationException("No serialized satelliteComparer could be found in JSON stream");

            treeSet.SatelliteComparer = ReadComparer(options, satelliteComparerElement);
            #endregion

            #region items
            if (itemsElement.HasValue)
            {
                foreach (JsonElement item in itemsElement.Value.EnumerateArray())
                {
                    var key = item.Deserialize<K>(options);
                    treeSet.Add(key);
                }
            }
            #endregion

            return treeSet;
        }

        private static IComparer<K> ReadComparer(JsonSerializerOptions options, JsonElement? comparerElement)
        {
            string knownType = null;
            JsonElement? dataElement = null;

            foreach (var prop in comparerElement.Value.EnumerateObject())
            {
                if (prop.Name == "knownType")
                {
                    knownType = prop.Value.GetString();
                }
                else if (prop.Name == "data")
                {
                    dataElement = prop.Value;
                }
            }

            if (string.IsNullOrEmpty(knownType))
                throw new JsonException("Missing knownType in comparer");

            IComparer<K> comparer = RedBlackComparerSerializationInfo<K>.GetComparerFromKnownText(knownType);
            if (comparer == null)
            {
                if (!dataElement.HasValue)
                    throw new JsonException($"Missing data for custom comparer of type {knownType}");
                var comparerType = Type.GetType(knownType, true, true);
                comparer = (IComparer<K>)JsonSerializer.Deserialize(dataElement.Value.GetRawText(), comparerType, options);
            }

            if (comparer == null)
            {
                throw new InvalidOperationException("No serialized comparer could be found in JSON stream");
            }

            return comparer;
        }

        public override void Write(Utf8JsonWriter writer, RedBlackTreeSet<K> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteBoolean("allowDuplicates", value.AllowDuplicates);
            WriteComparer(writer, options, "comparer", value.Comparer);
            WriteComparer(writer, options, "satelliteComparer", value.SatelliteComparer);

            

            // Write items
            writer.WritePropertyName("items");
            // readonly dictionary is used to by pass circular reference check.
            // it does not copy values but simply wraps them
            // note that in 3.5 .net framework you might have to create your own wrapper
            JsonSerializer.Serialize(writer, new ReadOnlyCollectionWrapper<K>(value), options);
            writer.WriteEndObject();
        }

        private static void WriteComparer(Utf8JsonWriter writer, JsonSerializerOptions options, string propName, IComparer<K> comparer)
        {
            writer.WritePropertyName(propName);
            writer.WriteStartObject();
            var comparerInfo = new RedBlackComparerSerializationInfo<K>(comparer);
            var knownType = comparerInfo.GetKnownType();
            if (knownType != null)
            {
                writer.WriteString("knownType", knownType);
            }
            else
            {
                if (comparerInfo.IsPublic && (comparerInfo.HasDefaultPublicConstructor || comparerInfo.HasJsonTextConstructor))
                {
                    writer.WriteString("knownType", comparerInfo.SimpleTypeName);
                    writer.WritePropertyName("data");
                    JsonSerializer.Serialize(writer, comparer, comparer.GetType(), options);
                }
                else
                {
                    // RedBlackTypeSerializationInfo.SubObjectsBinarySerializationAllowed has to be true else GetBinaryKnowType() returns null
                    knownType = comparerInfo.GetBinaryKnowType();
                    if (knownType != null)
                    {
                        writer.WriteString("knownType", knownType);
                    }
                    else
                    {
                        throw new InvalidOperationException($"KeyComparer of type {comparerInfo.Type.Name} cannot be serialized to JSON");
                    }
                }
            }
            writer.WriteEndObject();
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
