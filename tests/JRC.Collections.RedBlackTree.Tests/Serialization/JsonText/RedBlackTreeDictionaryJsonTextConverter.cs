using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JRC.Collections.RedBlackTree.Tests.Serialization.JsonText
{
    public class RedBlackTreeDictionaryJsonTextConverter<TKey, TValue> : JsonConverter<RedBlackTreeDictionary<TKey, TValue>>
    {
        public override RedBlackTreeDictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject");

            JsonElement? comparerElement = null;
            JsonElement? itemsElement = null;

            #region read values
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected PropertyName");

                var propertyName = reader.GetString();
                reader.Read(); // Move to value

                if (propertyName == "comparer")
                {
                    comparerElement = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
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

            var dict = new RedBlackTreeDictionary<TKey, TValue>();

            #region comparer
            if (!comparerElement.HasValue)
                throw new InvalidOperationException("No serialized comparer could be found in JSON stream");

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

            IComparer<TKey> comparer = RedBlackComparerSerializationInfo<TKey>.GetComparerFromKnownText(knownType);
            if (comparer == null)
            {
                if (!dataElement.HasValue)
                    throw new JsonException($"Missing data for custom comparer of type {knownType}");
                var comparerType = Type.GetType(knownType, true, true);
                comparer = (IComparer<TKey>)JsonSerializer.Deserialize(dataElement.Value.GetRawText(), comparerType, options);
            }

            if (comparer == null)
            {
                throw new InvalidOperationException("No serialized comparer could be found in JSON stream");
            }

            dict.Comparer = comparer;
            #endregion

            #region items
            if (itemsElement.HasValue)
            {
                foreach (var prop in itemsElement.Value.EnumerateObject())
                {
                    var key = JsonSerializer.Deserialize<TKey>($"\"{prop.Name}\"", options);
                    var value = JsonSerializer.Deserialize<TValue>(prop.Value.GetRawText(), options);
                    dict.Add(key, value);
                }             
            }
            #endregion

            return dict;
        }

        public override void Write(Utf8JsonWriter writer, RedBlackTreeDictionary<TKey, TValue> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Write comparer (même logique que Newtonsoft)
            writer.WritePropertyName("comparer");

            writer.WriteStartObject();
            var comparerInfo = new RedBlackComparerSerializationInfo<TKey>(value.Comparer);
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
                    JsonSerializer.Serialize(writer, value.Comparer, value.Comparer.GetType(), options);
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

            // Write items
            writer.WritePropertyName("items");
            // readonly dictionary is used to by pass circular reference check.
            // it does not copy values but simply wraps them
            // note that in 3.5 .net framework you might have to create your own wrapper
            JsonSerializer.Serialize(writer, new ReadOnlyDictionary<TKey, TValue>(value), options);
            writer.WriteEndObject();
        }
    }
}
