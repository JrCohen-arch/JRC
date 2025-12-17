// Licensed under MIT license.
// Author: JRC
//
// Based on Microsoft's RBTree<K> from System.Data (Copyright Microsoft Corporation).
// Improvements: faster list enumeration, optimizations, simplified API.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace JRC.Collections.RedBlackTree
{
    internal static class XmlSerializerHelper
    {
        private static readonly XmlSerializerNamespaces EmptyNamespaces = GetEmptyNamespaces();
        private static XmlSerializerNamespaces GetEmptyNamespaces()
        {
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");
            return namespaces;
        }

        #region Serializer Cache

        private class SerializerWrapper
        {
            private readonly object _lock = new object();
            private XmlSerializer _serializer;
            private readonly Type _type;

            public SerializerWrapper(Type type)
            {
                _type = type;
            }

            public XmlSerializer Serializer
            {
                get
                {
                    if (_serializer == null)
                    {
                        lock (_lock)
                        {
                            if (_serializer == null)
                            {
                                _serializer = new XmlSerializer(_type);
                            }
                        }
                    }
                    return _serializer;
                }
            }
        }

        // not a dictionary because of thread-safety with double check
        // indeed, as we are compatible with .net 3.5, we do not have yet concurent dictionary
        // and using macros here is not necessary (few usages)
        private static readonly Hashtable _cache = new Hashtable(); 

        public static XmlSerializer GetCachedSerializer(Type type)
        {
            SerializerWrapper wrapper;

            if ( (wrapper = (SerializerWrapper) _cache[type]) == null)
            {
                lock (_cache)
                {
                    if ((wrapper = (SerializerWrapper)_cache[type]) == null)
                    {
                        wrapper = new SerializerWrapper(type);
                        _cache[type] = wrapper;
                    }
                }
            }

            return wrapper.Serializer;
        }

        #endregion

        #region Write Value

        public static void WriteValue(this XmlWriter writer, Type type, object value)
        {
            // Handle null
            if (value == null)
            {
                writer.WriteAttributeString("xsi", "nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
                return;
            }

            // Handle nullable types
            Type underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                type = underlyingType;
            }

            // Handle enums
            if (type.IsEnum)
            {
                writer.WriteString(value.ToString());
                return;
            }

            // Handle primitives via TypeCode
            TypeCode typeCode = Type.GetTypeCode(type);

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    writer.WriteValue((bool)value);
                    break;
                case TypeCode.Char:
                    writer.WriteValue((char)value);
                    break;
                case TypeCode.SByte:
                    writer.WriteValue((sbyte)value);
                    break;
                case TypeCode.Byte:
                    writer.WriteValue((byte)value);
                    break;
                case TypeCode.Int16:
                    writer.WriteValue((short)value);
                    break;
                case TypeCode.UInt16:
                    writer.WriteValue((ushort)value);
                    break;
                case TypeCode.Int32:
                    writer.WriteValue((int)value);
                    break;
                case TypeCode.UInt32:
                    writer.WriteValue((uint)value);
                    break;
                case TypeCode.Int64:
                    writer.WriteValue((long)value);
                    break;
                case TypeCode.UInt64:
                    writer.WriteString(XmlConvert.ToString((ulong)value));
                    break;
                case TypeCode.Single:
                    writer.WriteValue((float)value);
                    break;
                case TypeCode.Double:
                    writer.WriteValue((double)value);
                    break;
                case TypeCode.Decimal:
                    writer.WriteValue((decimal)value);
                    break;
                case TypeCode.String:
                    writer.WriteValue((string)value);
                    break;
                case TypeCode.DateTime:
                    writer.WriteValue((DateTime)value);
                    break;
               default:
                    // Special cases not covered by TypeCode
                    if (type == typeof(TimeSpan))
                    {
                        writer.WriteString(XmlConvert.ToString((TimeSpan)value));
                    }
                    else if (type == typeof(Guid))
                    {
                        writer.WriteValue(value.ToString());
                    }
                    else if (type == typeof(DateTimeOffset))
                    {
                        writer.WriteValue(XmlConvert.ToString((DateTimeOffset)value));
                    }
                    else if (type == typeof(byte[]))
                    {
                        writer.WriteBase64((byte[])value, 0, ((byte[])value).Length);
                    }
                    else
                    {
                        var ns = new XmlSerializerNamespaces();
                        ns.Add("", ""); // Ajoute un namespace vide
                        GetCachedSerializer(type).Serialize(writer, value, ns);
                    }
                    break;               
            }
        }

        #endregion

        #region Read Value
        public static object ReadValue(this XmlReader reader, Type type)
        {
            // Check for nil
            string nilAttr = reader.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance");
            if (nilAttr == "true")
            {
                reader.Read();
                return null;
            }

            // Handle nullable types
            Type underlyingType = Nullable.GetUnderlyingType(type);
            bool isNullable = underlyingType != null;
            if (isNullable)
            {
                type = underlyingType;
            }

            // Handle enums
            if (type.IsEnum)
            {
                string enumValue = reader.ReadElementContentAsString();
                return Enum.Parse(type, enumValue);
            }

            // Handle primitives via TypeCode
            TypeCode typeCode = Type.GetTypeCode(type);

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return reader.ReadContentAsBoolean();
                case TypeCode.Char:
                    return (char)reader.ReadContentAsInt();
                case TypeCode.SByte:
                    return (sbyte)reader.ReadContentAsInt();
                case TypeCode.Byte:
                    return (byte)reader.ReadContentAsInt();
                case TypeCode.Int16:
                    return (short)reader.ReadContentAsInt();
                case TypeCode.UInt16:
                    return (ushort)reader.ReadContentAsInt();
                case TypeCode.Int32:
                    return reader.ReadContentAsInt();
                case TypeCode.UInt32:
                    return (uint)reader.ReadContentAsLong();
                case TypeCode.Int64:
                    return reader.ReadContentAsLong();
                case TypeCode.UInt64:
                    return XmlConvert.ToUInt64(reader.ReadContentAsString());
                case TypeCode.Single:
                    return reader.ReadContentAsFloat();
                case TypeCode.Double:
                    return reader.ReadContentAsDouble();
                case TypeCode.Decimal:
                    return reader.ReadContentAsDecimal();
                case TypeCode.String:
                    return reader.ReadContentAsString();
                case TypeCode.DateTime:
                    return reader.ReadContentAsDateTime();
                default:
                    // Special cases not covered by TypeCode
                    if (type == typeof(TimeSpan))
                    {
                        return XmlConvert.ToTimeSpan(reader.ReadElementContentAsString());
                    }
                    else if (type == typeof(Guid))
                    {
                        return new Guid(reader.ReadElementContentAsString());
                    }
                    else if (type == typeof(DateTimeOffset))
                    {
                        return XmlConvert.ToDateTimeOffset(reader.ReadElementContentAsString());
                    }
                    else if (type == typeof(byte[]))
                    {
                        return Convert.FromBase64String(reader.ReadElementContentAsString());
                    }
                    else
                    {
                        // Fall back to XmlSerializer for complex types
                        return GetCachedSerializer(type).Deserialize(reader);
                    }                
            }
        }
        #endregion

        #region ReadProvider
        public static RedBlackTreeSet<TItem, TSortKey>.ISortKeyProvider ReadXmlProvider<TItem, TSortKey>(this XmlReader reader)
        {
            RedBlackTreeSet<TItem, TSortKey>.ISortKeyProvider provider = null;
            var knownType = reader.GetAttribute("knownType");
            if (knownType != null)
            {
                provider = RedBlackTypeSerializationInfo.GetObjFromKnownText<RedBlackTreeSet<TItem, TSortKey>.ISortKeyProvider>(knownType);
                if (provider == null)
                {
                    var d = reader.Depth;
                    reader.Read(); // move to first child
                    if (reader.Depth != d + 1 || reader.NodeType != XmlNodeType.Element)
                    {
                        var lineInfo = reader as IXmlLineInfo;
                        var coord = lineInfo == null ? "" : $" ({lineInfo.LineNumber}, {lineInfo.LinePosition})";
                        throw new InvalidOperationException($"Error in xml document{coord} - start element expected");
                    }

                    var providerType = Type.GetType(knownType, true, true);
                    var providerSerializer = XmlSerializerHelper.GetCachedSerializer(providerType);
                    provider = (RedBlackTreeSet<TItem, TSortKey>.ISortKeyProvider)providerSerializer.Deserialize(reader);
                }
            }
            reader.Read();
            return provider;
        }
        #endregion

        #region WriteProvider
        public static void WriteXmlProvider<TItem, TSortKey>(this XmlWriter writer, string elementName, RedBlackTreeSet<TItem, TSortKey>.ISortKeyProvider provider)
        {
            writer.WriteStartElement(elementName);
            var typeInfo = new RedBlackTypeSerializationInfo(provider);
            XmlSerializer providerSerializer = null;
            if (typeInfo.IsPublic && typeInfo.HasDefaultPublicConstructor)
            {
                try
                {
                    providerSerializer = XmlSerializerHelper.GetCachedSerializer(provider.GetType());
                }
                catch (InvalidOperationException)
                {
                    // not xml serializable
                }
            }
            if (providerSerializer != null)
            {
                writer.WriteAttributeString("knownType", typeInfo.SimpleTypeName);
                providerSerializer.Serialize(writer, provider, EmptyNamespaces);
            }
            else
            {
                var knownType = typeInfo.GetBinaryKnowType();
                if (knownType != null)
                {
                    writer.WriteAttributeString("knownType", knownType);
                }
                else
                {
                    throw new InvalidOperationException($"SortKeyProvider of type {typeInfo.Type.Name} cannot be serialized");
                }
            }
            writer.WriteEndElement();
        }
        #endregion

        #region ReadComparer
        public static IComparer<T> ReadXmlComparer<T>(this XmlReader reader)
        {
            IComparer<T> comparer = null;
            var knownType = reader.GetAttribute("knownType");
            if (knownType != null)
            {
                comparer = RedBlackComparerSerializationInfo<T>.GetComparerFromKnownText(knownType);
                if (comparer == null)
                {
                    var d = reader.Depth;
                    reader.Read(); // move to first child
                    if (reader.Depth != d + 1 || reader.NodeType != XmlNodeType.Element)
                    {
                        var lineInfo = reader as IXmlLineInfo;
                        var coord = lineInfo == null ? "" : $" ({lineInfo.LineNumber}, {lineInfo.LinePosition})";
                        throw new InvalidOperationException($"Error in xml document{coord} - start element expected");
                    }

                    var comparerType = Type.GetType(knownType, true, true);
                    var comparerSerializer = XmlSerializerHelper.GetCachedSerializer(comparerType);
                    comparer = (IComparer<T>)comparerSerializer.Deserialize(reader);
                }
            }
            reader.Read();
            return comparer;
        }
        #endregion

        #region WriteComparer
        public static void WriteXmlComparer<K>(this XmlWriter writer, string elementName, IComparer<K> comparer)
        {
            writer.WriteStartElement(elementName);
            var comparerInfo = new RedBlackComparerSerializationInfo<K>(comparer);
            var knownType = comparerInfo.GetKnownType();
            if (knownType != null)
            {
                writer.WriteAttributeString("knownType", knownType);
            }
            else
            {
                XmlSerializer comparerSerializer = null;
                if (comparerInfo.IsPublic && comparerInfo.HasDefaultPublicConstructor)
                {
                    try
                    {
                        comparerSerializer = XmlSerializerHelper.GetCachedSerializer(comparer.GetType());
                    }
                    catch (InvalidOperationException)
                    {
                        // not xml serializable
                    }
                }
                if (comparerSerializer != null)
                {
                    writer.WriteAttributeString("knownType", comparerInfo.SimpleTypeName);

                    comparerSerializer.Serialize(writer, comparer, EmptyNamespaces);
                }
                else
                {
                    knownType = comparerInfo.GetBinaryKnowType();
                    if (knownType != null)
                    {
                        writer.WriteAttributeString("knownType", knownType);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Comparer of type {comparerInfo.Type.Name} cannot be serialized");
                    }
                }

            }
            writer.WriteEndElement();
        }
        #endregion
    }
}
