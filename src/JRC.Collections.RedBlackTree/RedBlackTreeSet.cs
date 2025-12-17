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
    /// Class implementing a sorted set, but that supports duplicate values on the contrary of System.Collections.Generic.SortedSet&lt;T&gt;.
    /// Indeed, even Keys have be different, the comparer can indicate that they are "equal" (i.e. return zero).
    /// </summary>
    [Serializable]
    public sealed class RedBlackTreeSet<K> : RedBlackTreeSetBase<K>, IXmlSerializable
    {
        #region constructors
        /// <summary>
        /// Initialize a new instance of RedBlackTreeSet that does not allow duplicates, that uses Comparer&lt;K>.Default as main comparer.
        /// Use this constructor for typical Set behavior where each key appears only once.
        /// </summary>
        public RedBlackTreeSet() : base(false, Comparer<K>.Default, null)
        {
        }

        /// <summary>
        /// Initialize a new instance of RedBlackTreeSet with comparers
        /// </summary>
        /// <param name="allowDuplicates">true if this tree allows duplicates (i.e. n DIFFERENT keys returning 0 with the nodeComparer)</param>
        /// <param name="comparer">comparer in order to compare keys. If not provided, Comparer&lt;K&gt; is used</param>
        /// <param name="satelliteComparer">comparer in order to compare duplicate keys - allows to return duplicates in a stable order. If not provided GetHashCode() is used.</param>
        /// <remarks>In case allowDuplicates is true and nodeComparer is Comparer&lt;K&gt; is used, K should implement IComparable&lt;K&gt; and should be able to return 0 when comparing different instances.</remarks>
        public RedBlackTreeSet(bool allowDuplicates, IComparer<K> comparer = null, IComparer<K> satelliteComparer = null) : base(allowDuplicates, comparer ?? Comparer<K>.Default, satelliteComparer)
        {
        }


        /// <summary>
        /// Initialize a new instance of RedBlackTreeSet with comparisons
        /// </summary>
        /// <param name="allowDuplicates">true if this tree allows duplicates (i.e. n different keys returning 0 with the nodeComparison)</param>
        /// <param name="comparison">comparison in order to compare keys</param>
        /// <param name="satelliteComparison">comparison in order to compare duplicate keys - allows to return duplicates in a stable order. If not provided GetHashCode() is used.</param>
        public RedBlackTreeSet(bool allowDuplicates, Comparison<K> comparison, Comparison<K> satelliteComparison = null)
            : base(allowDuplicates, comparison == null ? (IComparer<K>)Comparer<K>.Default : new RedBlackLambdaComparer<K>(comparison), satelliteComparison == null ? null : new RedBlackLambdaComparer<K>(satelliteComparison))
        {
        }
        #endregion

        #region properties
        /// <summary>
        /// Gets or sets the main comparer for this treeset. Assigning a new comparer is possible only if treeset does not contain any item.
        /// </summary>
        public IComparer<K> Comparer
        {
            get 
            { 
                return this.comparer; 
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (this.Count != 0)
                {
                    throw new InvalidOperationException("Unable to set comparer if treeset already contains item(s)");
                }
                this.comparer = value;
            }
        }
        #endregion

        #region methods
        /// <summary>
        /// Finds a key using binary search navigation in O(log n).
        /// </summary>
        /// <param name="comparison">
        /// Navigation function that must be consistent with the tree's sort order and that returns :
        /// <list type="bullet">
        ///     <item>negative: search left subtree</item>
        ///     <item>zero: key found</item>
        ///     <item>positive: search right subtree</item>
        /// </list>       
        /// </param>
        /// <returns>The matching key, or default(K) if not found.</returns>
        /// <example>
        /// // Tree sorted by Age
        /// var set = new RedBlackTreeSet&lt;Person&gt;(true, (a, b) => a.Age - b.Age);
        /// set.Add(new Person("Alice", 30));
        /// set.Add(new Person("Bob", 25));
        /// set.Add(new Person("Charlie", 20));
        /// var person = set.FindKey(p => p.Age - 25); // Find Bob in O(log(n))
        /// </example>
        public K FindKey(Func<K, int> comparison)
        {
            return this.FindKeyImpl(comparison);
        }
        /// <summary>
        /// Finds keys using binary search navigation in O(log n).
        /// </summary>
        /// <param name="comparison">
        /// Navigation function that must be consistent with the tree's sort order and that returns :
        /// <list type="bullet">
        ///     <item>negative: search left subtree</item>
        ///     <item>zero: key found</item>
        ///     <item>positive: search right subtree</item>
        /// </list>       
        /// </param>
        /// <returns>The matching keys, or empty enumerable if not found.</returns>
        /// <example>
        /// // Tree sorted by Age
        /// var set = new RedBlackTreeSet&lt;Person&gt;(true, (a, b) => a.Age - b.Age);
        /// set.Add(new Person("Alice", 30));
        /// set.Add(new Person("Bob", 25));
        /// set.Add(new Person("John", 25));
        /// set.Add(new Person("Charlie", 20));
        /// var person = set.FindKeys(p => p.Age - 25); // Find Bob and John in O(log(n))
        /// </example>
        public IEnumerable<K> FindKeys(Func<K, int> comparison)
        {
            return this.FindKeysImpl(comparison);
        }
        /// <summary>
        /// Returns keys where comparer returns 0. O(log n + k) where k = number of results.
        /// </summary>
        /// <param name="minComparison">Returns 0 or negative for keys >= min boundary</param>
        /// <param name="maxComparison">Returns 0 or negative for keys &lt;= max boundary</param>
        /// <returns>Keys in the specified range</returns>
        /// <example>
        /// // Tree sorted by Age
        /// var set = new RedBlackTreeSet&lt;Person&gt;(true, (a, b) => a.Age - b.Age);
        /// set.Add(new Person("Alice", 30));
        /// set.Add(new Person("Bob", 25));
        /// set.Add(new Person("Charlie", 20));
        /// // Find all persons aged 20-30
        /// var persons = set.FindRange(
        ///     p => p.Age - 25,  // >= 25
        ///     p => p.Age - 30   // &lt;= 30
        /// ); // Alice and Bob
        /// </example>
        public IEnumerable<K> FindRange(Func<K, int> minComparison, Func<K, int> maxComparison)
        {
            return FindRangeImp(minComparison, maxComparison);
        }
        #endregion

        #region IXmlSerializable members
        private static Type KeyType = typeof(K);

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

            bool foundComparer = false;
            bool foundSatelliteComparer = false;
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    // Read main comparer
                    if (reader.Name == "comparer")
                    {
                        var comparer = reader.ReadXmlComparer<K>();
                        if (comparer != null)
                        {
                            this.Comparer = comparer;
                            foundComparer = true;
                        }                                                
                    }
                    // Read Satellite comparer
                    else if (reader.Name == "satelliteComparer")
                    {
                        var comparer = reader.ReadXmlComparer<K>();
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
                        K key = (K)reader.ReadValue(KeyType);                       
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

            if (!foundComparer)
            {
                throw new InvalidOperationException("No serialized comparer could be found on xml stream");
            }
            if (!foundSatelliteComparer)
            {
                throw new InvalidOperationException("No serialized satellite comparer could be found on xml stream");
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("allowDuplicates", this.AllowDuplicates.ToString().ToLowerInvariant());
            writer.WriteXmlComparer("comparer", this.Comparer);
            writer.WriteXmlComparer("satelliteComparer", this.SatelliteComparer);
          
            foreach (var key in this)
            {
                writer.WriteStartElement("item");
                writer.WriteValue(KeyType, key);
                writer.WriteEndElement();
            }
        }
        #endregion
    }



}
