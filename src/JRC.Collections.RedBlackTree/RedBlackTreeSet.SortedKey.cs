// Licensed under MIT license.
// Author: JRC
//
// Based on Microsoft's RBTree<K> from System.Data (Copyright Microsoft Corporation).
// Improvements: faster list enumeration, optimizations, simplified API.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace JRC.Collections.RedBlackTree
{
    /// <summary>
    /// Implements "an easy to use" sorted set with red black tree implementing node position and duplicates with satellite nodes.
    /// </summary>
    [Serializable]
    public sealed class RedBlackTreeSet<TItem, TSortKey> : RedBlackTreeSetBase<TItem>, IXmlSerializable
    {
        /// <summary>
        /// Serialization ctor.
        /// Creates a RedBlackTreeSet instance that does not allow duplicates, that uses Comparer&lt;TSortKey> as SortKeyComparer.
        /// You have to set the <see cref="SortKeyProvider"/> property before adding any item if you want to use this constructor outside a serialization context.
        /// </summary>
        public RedBlackTreeSet() 
            : base(false, new KeyComparer())
        {

        }

        /// <summary>
        /// Initialize a new instance of RedBlackTreeSet using TSortKey's default comparer and that does not accept duplicates
        /// </summary>
        public RedBlackTreeSet(ISortKeyProvider sortKeyProvider)
            : base(false, new KeyComparer(sortKeyProvider))
        {
        }

        /// <summary>
        /// Initialize a new instance of RedBlackTreeSet using TSortKey's default comparer and that does not accept duplicates
        /// </summary>
        /// <param name="sortKeyProviderFunc">delegate in order to extract the sort key from item</param>
        public RedBlackTreeSet(Func<TItem, TSortKey> sortKeyProviderFunc)
            : this(new LambdaSortKeyProvider(sortKeyProviderFunc))
        {
        }

        /// <summary>
        ///  Initialize a new instance of RedBlackTreeSet
        /// </summary>
        /// <param name="allowDuplicates">true if this tree allows duplicates (i.e. n different keys returning 0 with the nodeComparison)</param>
        /// <param name="sortKeyProvider">provider in order to extract the sort key from item</param>
        /// <param name="sortKeyComparer">comparer between two sort keys</param>
        /// <param name="satelliteComparer">comparer in order to compare duplicate items - allows to return duplicates in a stable order. If not provided GetHashCode() is used.</param>
        public RedBlackTreeSet(bool allowDuplicates, ISortKeyProvider sortKeyProvider, IComparer<TSortKey> sortKeyComparer, IComparer<TItem> satelliteComparer = null)
            : base(allowDuplicates, new KeyComparer(sortKeyProvider, sortKeyComparer), satelliteComparer)
        {
        }

        /// <summary>
        ///  Initialize a new instance of RedBlackTreeSet
        /// </summary>
        /// <param name="allowDuplicates">true if this tree allows duplicates (i.e. n different keys returning 0 with the nodeComparison)</param>
        /// <param name="sortKeyProviderFunc">delegate in order to extract the sort key from item</param>
        /// <param name="sortKeyComparer">comparer between two sort keys</param>
        /// <param name="satelliteComparer">comparer in order to compare duplicate items - allows to return duplicates in a stable order. If not provided GetHashCode() is used.</param>
        public RedBlackTreeSet(bool allowDuplicates, Func<TItem, TSortKey> sortKeyProviderFunc, IComparer<TSortKey> sortKeyComparer, IComparer<TItem> satelliteComparer = null)
            : this(allowDuplicates, new LambdaSortKeyProvider(sortKeyProviderFunc), sortKeyComparer, satelliteComparer)
        {
        }

        /// <summary>
        ///  Initialize a new instance of RedBlackTreeSet
        /// </summary>
        /// <param name="allowDuplicates">true if this tree allows duplicates (i.e. n different keys returning 0 with the nodeComparison)</param>
        /// <param name="sortKeyProviderFunc">delegate in order to extract the sort key from item</param>
        /// <param name="sortKeyComparison">comparison between two sort keys</param>
        /// <param name="satelliteComparison">comparison in order to compare duplicate items - allows to return duplicates in a stable order. If not provided GetHashCode() is used.</param>
        public RedBlackTreeSet(bool allowDuplicates, Func<TItem, TSortKey> sortKeyProviderFunc, Comparison<TSortKey> sortKeyComparison, Comparison<TItem> satelliteComparison = null)
            : this(allowDuplicates, new LambdaSortKeyProvider(sortKeyProviderFunc), new RedBlackLambdaComparer<TSortKey>(sortKeyComparison), satelliteComparison == null ? null : new RedBlackLambdaComparer<TItem>(satelliteComparison))
        {
        }      

        #region properties
        public IComparer<TSortKey> SortKeyComparer
        {
            get
            {
                return ((KeyComparer)this.comparer).Comparer;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (this.Count != 0)
                {
                    throw new InvalidOperationException("Unable to set sort key comparer if treeset already contains item(s)");
                }
                ((KeyComparer)this.comparer).Comparer = value;
            }
        }

        public ISortKeyProvider SortKeyProvider
        {
            get
            {
                return ((KeyComparer)this.comparer).KeyProvider;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (this.Count != 0)
                {
                    throw new InvalidOperationException("Unable to set sort key provider if treeset already contains item(s)");
                }
                ((KeyComparer)this.comparer).KeyProvider = value;
            }
        }
        #endregion

        #region methods
        /// <summary>
        /// Finds a key using binary search navigation in O(log n).
        /// </summary>
        /// <param name="value">value to search for</param>
        /// <returns>The matching key, or default(K) if not found.</returns>
        /// <example>
        /// // Tree sorted by Age
        /// var set = new RedBlackTreeSet&lt;Person, int&gt;(true, p => p.Age, (age1, age2) => age1 - age2);
        /// set.Add(new Person("Alice", 30));
        /// set.Add(new Person("Bob", 25));
        /// set.Add(new Person("Charlie", 20));
        /// var person = set.FindKey(25); // Find Bob in O(log(n))
        /// </example>
        public TItem FindKey(TSortKey value)
        {
            var valComparer = (KeyComparer)this.comparer;
            return this.FindKeyImpl(k => valComparer.CompareKey(k, value));
        }
        /// <summary>
        /// Finds keys using binary search navigation in O(log n).
        /// </summary>
        /// <param name="value">value to search for</param>
        /// <returns>The matching keys, or empty enumerable if not found.</returns>
        /// <example>
        /// // Tree sorted by Age
        /// var set = new RedBlackTreeSet&lt;Person, int&gt;(true, p => p.Age, (age1, age2) => age1 - age2);
        /// set.Add(new Person("Alice", 30));
        /// set.Add(new Person("Bob", 25));
        /// set.Add(new Person("John", 25));
        /// set.Add(new Person("Charlie", 20));
        /// var person = set.FindKeys(25); // Find Bob and John in O(log(n))
        /// </example>
        public IEnumerable<TItem> FindKeys(TSortKey value)
        {
            var valComparer = (KeyComparer)this.comparer;
            return this.FindKeysImpl(k => valComparer.CompareKey(k, value));
        }

        /// <summary>
        /// Returns keys where value is between min and max (inclusive). O(log n + k)
        /// </summary>
        /// <param name="min">Minimum value (inclusive)</param>
        /// <param name="max">Maximum value (inclusive)</param>
        /// <returns>Keys in the specified range</returns>
        /// <example>
        /// // Tree sorted by Age
        /// var set = new RedBlackTreeSet&lt;Person, int&gt;(true, p => p.Age, (age1, age2) => age1 - age2);
        /// set.Add(new Person("Alice", 30));
        /// set.Add(new Person("Bob", 25));
        /// set.Add(new Person("Charlie", 20));
        /// // Find all persons aged 20-30
        /// var persons = set.FindRange(25, 30); // Alice and Bob
        /// </example>
        public IEnumerable<TItem> FindRange(TSortKey min, TSortKey max)
        {
            var valComparer = (KeyComparer)this.comparer;
            return FindRangeImp(
                k => valComparer.CompareKey(k, min),
                k => valComparer.CompareKey(k, max)
            );
        }
        #endregion

        #region IXmlSerializable members
        private static Type ItemType = typeof(TItem);

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }
        void IXmlSerializable.ReadXml(XmlReader reader)
        {

            bool wasEmpty = reader.IsEmptyElement;

            this.AllowDuplicates = reader.GetAttribute("allowDuplicates") == "true";

            reader.Read();

            if (wasEmpty)
                return;

            bool foundProvider = false;
            bool foundComparer = false;
            bool foundSatelliteComparer = false;
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "provider")
                    {
                        var provider = reader.ReadXmlProvider<TItem, TSortKey>();
                        if (provider != null)
                        {
                            this.SortKeyProvider = provider;
                            foundProvider = true;
                        }
                    }
                    // Read main comparer
                    else if (reader.Name == "comparer")
                    {
                        var comparer = reader.ReadXmlComparer<TSortKey>();
                        if (comparer != null)
                        {
                            this.SortKeyComparer = comparer;
                            foundComparer = true;
                        }
                    }
                    // Read Satellite comparer
                    else if (reader.Name == "satelliteComparer")
                    {
                        var comparer = reader.ReadXmlComparer<TItem>();
                        if (comparer != null)
                        {
                            this.SatelliteComparer = comparer;
                            foundSatelliteComparer = true;
                        }
                    }
                    // Read items
                    else if (reader.Name == "item")
                    {
                        reader.ReadStartElement("item");
                        TItem key = (TItem)reader.ReadValue(ItemType);
                        reader.ReadEndElement();
                        this.Add(key);
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

            if (!foundProvider)
            {
                throw new InvalidOperationException("No serialized sort key provider could be found on xml stream");
            }
            if (!foundComparer)
            {
                throw new InvalidOperationException("No serialized sort key comparer could be found on xml stream");
            }
            if (!foundSatelliteComparer)
            {
                throw new InvalidOperationException("No serialized satellite comparer could be found on xml stream");
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("allowDuplicates", this.AllowDuplicates.ToString().ToLowerInvariant());
            writer.WriteXmlProvider("provider", this.SortKeyProvider);
            writer.WriteXmlComparer("comparer", this.SortKeyComparer);
            writer.WriteXmlComparer("satelliteComparer", this.SatelliteComparer);

            foreach (var key in this)
            {
                writer.WriteStartElement("item");
                writer.WriteValue(ItemType, key);
                writer.WriteEndElement();
            }
        }
        #endregion

        #region inner types
        /// <summary>
        /// Provides the TSortKey from a TItem
        /// </summary>
        public interface ISortKeyProvider
        {
            /// <summary>
            /// Returns a sort key from the item given as argument.
            /// </summary>            
            TSortKey GetSortKey(TItem item);
        }
      
        sealed class LambdaSortKeyProvider : ISortKeyProvider
        {
            private readonly Func<TItem, TSortKey> func;

            public LambdaSortKeyProvider(Func<TItem, TSortKey> sortKeyProviderFunc)
            {
                if (sortKeyProviderFunc == null)
                {
                    throw new ArgumentNullException(nameof(sortKeyProviderFunc));
                }
                this.func = sortKeyProviderFunc;
            }

            public TSortKey GetSortKey(TItem item)
            {
                return this.func(item);
            }
        }

        [Serializable]
        sealed class KeyComparer : IComparer<TItem>
        {
            public KeyComparer()
            {
                this.Comparer = Comparer<TSortKey>.Default;
            }
            
            public KeyComparer(ISortKeyProvider provider, IComparer<TSortKey> comparer = null)
            {
                if (provider == null)
                {
                    throw new ArgumentNullException(nameof(provider));
                }
                this.KeyProvider = provider;
                this.Comparer = comparer ?? Comparer<TSortKey>.Default;
            }          

            public ISortKeyProvider KeyProvider { get; set; }
            public IComparer<TSortKey> Comparer { get; set; }

            public int CompareKey(TItem k, TSortKey v)
            {
                var tx = KeyProvider.GetSortKey(k);
                return Comparer.Compare(tx, v);
            }

            public int Compare(TItem x, TItem y)
            {
                var tx = KeyProvider.GetSortKey(x);
                var ty = KeyProvider.GetSortKey(y);
                return Comparer.Compare(tx, ty);
            }
        }
        #endregion
    }
}
