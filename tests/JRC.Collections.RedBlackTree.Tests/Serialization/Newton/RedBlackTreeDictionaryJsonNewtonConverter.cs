using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JRC.Collections.RedBlackTree.Tests.Serialization.Newton
{
    public class RedBlackTreeDictionaryJsonNewtonConverter<TKey, TValue> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(RedBlackTreeDictionary<TKey, TValue>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var dict = existingValue as RedBlackTreeDictionary<TKey, TValue> ?? new RedBlackTreeDictionary<TKey, TValue>();

            var jObject = JObject.Load(reader);

            IComparer<TKey> comparer = null;

            // Read comparer first
            var comparerToken = jObject["comparer"];
            if (comparerToken != null)
            {              
                var knownType = comparerToken["knownType"]?.Value<string>();
                if (knownType != null)
                {
                    comparer = RedBlackComparerSerializationInfo<TKey>.GetComparerFromKnownText(knownType);

                    if (comparer == null)
                    {
                        var comparerData = comparerToken["data"];
                        if (comparerData != null)
                        {
                            var comparerType = Type.GetType(knownType, true, true);
                            comparer = (IComparer<TKey>)comparerData.ToObject(comparerType, serializer);
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
            dict.Comparer = comparer;

            // Now populate items using Newtonsoft's Populate
            var itemsToken = jObject["items"];
            if (itemsToken != null)
            {
                using (var itemsReader = itemsToken.CreateReader())
                {
                    serializer.Populate(itemsReader, dict);
                }
            }

            return dict;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dict = (RedBlackTreeDictionary<TKey, TValue>)value;

            writer.WriteStartObject();

            // Write comparer
            writer.WritePropertyName("comparer");
            writer.WriteStartObject();

            var comparerInfo = new RedBlackComparerSerializationInfo<TKey>(dict.Comparer);
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
                    serializer.Serialize(writer, dict.Comparer);
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

            // Write items - let Newtonsoft serialize the dictionary
            writer.WritePropertyName("items");
            // readonly dictionary is used to by pass circular reference check.
            // it does not copy values but simply wraps them
            // note that in 3.5 .net framework you might have to create your own wrapper
            serializer.Serialize(writer, new System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>(dict)); 

            writer.WriteEndObject();
        }
    }
}
