// Licensed under MIT license.
// Author: JRC
//
// Based on Microsoft's RBTree<K> from System.Data (Copyright Microsoft Corporation).
// Improvements: faster list enumeration, optimizations, simplified API.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace JRC.Collections.RedBlackTree
{
    /// <summary>
    /// A sorted dictionary implementation using a Red-Black tree with O(1) enumeration via successor chain.
    /// Unlike SortedDictionary, this supports O(log n) index-based access.
    /// </summary>
    [Serializable]    
    public sealed class RedBlackTreeDictionary<TKey, TValue> : RedBlackTreePlus<KeyValuePair<TKey, TValue>>,
        IDictionary<TKey, TValue>, IXmlSerializable
#if NET45_OR_GREATER || NETCOREAPP || NETSTANDARD
        , IReadOnlyDictionary<TKey, TValue>
#endif
    {
        private IComparer<TKey> _keyComparer;
        [NonSerialized]
        private KeyCollection _keys;
        [NonSerialized]
        private ValueCollection _values;

        /// <summary>
        /// Initialize a new instance using the default comparer for TKey
        /// </summary>
        public RedBlackTreeDictionary()
            : this(Comparer<TKey>.Default)
        {
        }

        /// <summary>
        /// Initialize a new instance using the specified key comparer
        /// </summary>
        public RedBlackTreeDictionary(IComparer<TKey> keyComparer)            
        {
            _keyComparer = keyComparer ?? Comparer<TKey>.Default;
        }

        /// <summary>
        /// Initialize a new instance using the specified key comparison
        /// </summary>
        public RedBlackTreeDictionary(Comparison<TKey> keyComparison)
            : this(keyComparison == null ? Comparer<TKey>.Default : (IComparer<TKey>) new RedBlackLambdaComparer<TKey>(keyComparison))
        {
        }

        #region Properties        
        /// <summary>
        /// Gets or set the key comparer. Assigning new comparer is possible only if this dictionary does not contains any key-value;
        /// </summary>
        public IComparer<TKey> Comparer
        {
            get
            {
                return _keyComparer;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (this.Count != 0)
                {
                    throw new InvalidOperationException("Unable to set comparer if dictionary already contains item(s)");
                }
                this._keyComparer = value;
            }
        }


        /// <summary>
        /// Always returns false
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets or sets the value associated with the specified key
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                int nodeId = FindNode(key);
                if (nodeId == NIL)
                    throw new KeyNotFoundException($"Key not found: {key}");
                return Value(nodeId).Value;
            }
            set
            {
                int nodeId = FindNode(key);
                if (nodeId == NIL)
                {
                    Add(key, value);
                }
                else
                {
                    SetValue(nodeId, new KeyValuePair<TKey, TValue>(key, value));
                    unchecked { _version++; }
                }
            }
        }        

        /// <summary>
        /// Gets the collection of keys
        /// </summary>
        public ICollection<TKey> Keys => _keys ?? (_keys = new KeyCollection(this));       

        /// <summary>
        /// Gets the collection of values
        /// </summary>
        public ICollection<TValue> Values => _values ?? (_values = new ValueCollection(this));

#if NET45_OR_GREATER || NETCOREAPP || NETSTANDARD
        /// <summary>
        /// Gets the collection of keys
        /// </summary>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
        /// <summary>
        /// Gets the collection of values
        /// </summary>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
#endif
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds a key-value pair to the dictionary O(log n)
        /// </summary>
        /// <exception cref="ArgumentException">If key already exists</exception>
        public void Add(TKey key, TValue value)
        {
            int nodeId = GetNewNode(new KeyValuePair<TKey, TValue>(key, value));
            RBInsert(nodeId, key);
        }

        /// <summary>
        /// Adds a key-value pair to the dictionary 
        /// </summary>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Tries to add a key-value pair. Returns false if key already exists. O(log n)
        /// </summary>
        public bool TryAdd(TKey key, TValue value)
        {
            if (ContainsKey(key))
                return false;
            Add(key, value);
            return true;
        }

        /// <summary>
        /// Removes the entry with the specified key O(log n)
        /// </summary>
        public bool Remove(TKey key)
        {
            int nodeId = FindNode(key);
            if (nodeId == NIL)
                return false;
            RBDelete(nodeId);
            return true;
        }

        /// <summary>
        /// Removes the specified key-value pair (both key and value must match)
        /// </summary>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            int nodeId = FindNode(item.Key);
            if (nodeId == NIL)
                return false;
            if (!EqualityComparer<TValue>.Default.Equals(Value(nodeId).Value, item.Value))
                return false;
            RBDelete(nodeId);
            return true;
        }

        /// <summary>
        /// Returns true if the dictionary contains the specified key O(log n)
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            return FindNode(key) != NIL;
        }

        /// <summary>
        /// Returns true if the dictionary contains the specified key-value pair
        /// </summary>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            int nodeId = FindNode(item.Key);
            if (nodeId == NIL)
                return false;
            return EqualityComparer<TValue>.Default.Equals(Value(nodeId).Value, item.Value);
        }

        /// <summary>
        /// Tries to get the value associated with the specified key O(log n)
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            int nodeId = FindNode(key);
            if (nodeId == NIL)
            {
                value = default;
                return false;
            }
            value = Value(nodeId).Value;
            return true;
        }

        /// <summary>
        /// Returns the index of the specified key, or -1 if not found. O(2log n)
        /// </summary>
        public int IndexOfKey(TKey key)
        {
            int nodeId = FindNode(key);
            if (nodeId == NIL)
                return -1;
            return IndexOfNode(nodeId);
        }       
        #endregion

        #region Private Methods
        /// <summary>
        /// Find node by key
        /// </summary>
        private int FindNode(TKey key)
        {
            int x_id = root;
            while (x_id != NIL)
            {
                int c = _keyComparer.Compare(key, Value(x_id).Key);
                if (c == 0)
                    return x_id;
                x_id = (c < 0) ? Left(x_id) : Right(x_id);
            }
            return NIL;
        }

        /// <summary>
        /// Insert node using key comparison (with successor chain maintenance)
        /// </summary>
        private void RBInsert(int nodeId, TKey key)
        {
            unchecked { _version++; }

            int y_id = NIL;
            int z_id = root;
            int lastLeftParent = NIL;
            int lastRightParent = NIL;

            while (z_id != NIL)
            {
                IncreaseSize(z_id);
                y_id = z_id;

                int c = _keyComparer.Compare(key, Value(z_id).Key);

                if (c < 0)
                {
                    lastLeftParent = z_id;
                    z_id = Left(z_id);
                }
                else if (c > 0)
                {
                    lastRightParent = z_id;
                    z_id = Right(z_id);
                }
                else
                {
                    // Key already exists - undo size increases and throw
                    while (y_id != NIL)
                    {
                        DecreaseSize(y_id);
                        y_id = Parent(y_id);
                    }
                    FreeNode(nodeId);
                    throw new ArgumentException($"Key already exists: {key}");
                }
            }

            SetParent(nodeId, y_id);

            if (y_id == NIL)
            {
                root = nodeId;
                SetLink(nodeId, NIL);
            }
            else
            {
                int c = _keyComparer.Compare(key, Value(y_id).Key);

                if (c < 0)
                {
                    SetLeft(y_id, nodeId);

                    if (lastRightParent != NIL)
                    {
                        SetLink(lastRightParent, nodeId);
                    }
                    SetLink(nodeId, y_id);
                }
                else
                {
                    SetRight(y_id, nodeId);

                    int oldSucc = Link(y_id);
                    SetLink(y_id, nodeId);
                    SetLink(nodeId, oldSucc);
                }
            }

            SetLeft(nodeId, NIL);
            SetRight(nodeId, NIL);
            SetColor(nodeId, NodeColor.red);

            RBInsertFixup(NIL, nodeId, NIL);
        }       
        #endregion

        #region KeyCollection        
        public sealed class KeyCollection : ICollection<TKey>
        {
            private readonly RedBlackTreeDictionary<TKey, TValue> _dict;

            internal KeyCollection(RedBlackTreeDictionary<TKey, TValue> dict)
            {
                _dict = dict;
            }

            public int Count => _dict.Count;

            public bool IsReadOnly => true;

            public bool Contains(TKey item) => _dict.ContainsKey(item);

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                if (array.Length - arrayIndex < _dict.Count)
                    throw new ArgumentException("Destination array is not long enough");

                int x_id = _dict.Minimum(_dict.root);
                int count = _dict.Count;
                for (int i = 0; i < count; i++)
                {
                    array[arrayIndex + i] = _dict.Value(x_id).Key;
                    x_id = _dict.Link(x_id);
                }
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                return _dict.Select(kvp => kvp.Key).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<TKey>.Add(TKey item) =>
                throw new NotSupportedException("KeyCollection is read-only");

            bool ICollection<TKey>.Remove(TKey item) =>
                throw new NotSupportedException("KeyCollection is read-only");

            void ICollection<TKey>.Clear() =>
                throw new NotSupportedException("KeyCollection is read-only");
        }
        #endregion

        #region ValueCollection        
        public sealed class ValueCollection : ICollection<TValue>
        {
            private readonly RedBlackTreeDictionary<TKey, TValue> _dict;

            internal ValueCollection(RedBlackTreeDictionary<TKey, TValue> dict)
            {
                _dict = dict;
            }

            public int Count => _dict.Count;

            public bool IsReadOnly => true;

            public bool Contains(TValue item)
            {
                var comparer = EqualityComparer<TValue>.Default;
                int x_id = _dict.Minimum(_dict.root);
                while (x_id != NIL)
                {
                    if (comparer.Equals(_dict.Value(x_id).Value, item))
                        return true;
                    x_id = _dict.Link(x_id);
                }
                return false;
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                if (array.Length - arrayIndex < _dict.Count)
                    throw new ArgumentException("Destination array is not long enough");

                int x_id = _dict.Minimum(_dict.root);
                int count = _dict.Count;
                for (int i = 0; i < count; i++)
                {
                    array[arrayIndex + i] = _dict.Value(x_id).Value;
                    x_id = _dict.Link(x_id);
                }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                return _dict.Select(kvp => kvp.Value).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<TValue>.Add(TValue item) =>
                throw new NotSupportedException("ValueCollection is read-only");

            bool ICollection<TValue>.Remove(TValue item) =>
                throw new NotSupportedException("ValueCollection is read-only");

            void ICollection<TValue>.Clear() =>
                throw new NotSupportedException("ValueCollection is read-only");
        }
        #endregion

        #region IXmlSerializable
        private static Type KeyType = typeof(TKey);
        private static Type ValueType = typeof(TValue);

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
                return;

            bool foundComparer = false;
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    // Read comparer
                    if (reader.Name == "comparer")
                    {
                        var comparer = reader.ReadXmlComparer<TKey>();
                        if (comparer != null)
                        {
                            this.Comparer = comparer;
                            foundComparer = true;
                        }                        
                    }
                    // Read items
                    else if (reader.Name == "item")
                    {
                        reader.ReadStartElement("item");

                        reader.ReadStartElement("key");
                        TKey key = (TKey)reader.ReadValue(KeyType);
                        reader.ReadEndElement();

                        reader.ReadStartElement("value");
                        TValue value = (TValue)reader.ReadValue(ValueType);
                        reader.ReadEndElement();

                        reader.ReadEndElement(); // item

                        this.Add(key, value);
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
                else
                {
                    reader.Skip();
                }

                reader.MoveToContent();
            }

            reader.ReadEndElement();

            if (!foundComparer)
            {
                throw new InvalidOperationException("No serialized comparer could be found on xml stream");
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteXmlComparer("comparer", this.Comparer);          

            foreach (var kvp in this)
            {
                writer.WriteStartElement("item");

                writer.WriteStartElement("key");
                writer.WriteValue(KeyType, kvp.Key);
                writer.WriteEndElement();
                writer.WriteStartElement("value");
                writer.WriteValue(ValueType, kvp.Value);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }
        #endregion
    }
}