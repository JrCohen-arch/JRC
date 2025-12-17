// Licensed under MIT license.
// Author: JRC
//
// Based on Microsoft's RBTree<K> from System.Data (Copyright Microsoft Corporation).
// Improvements: faster list enumeration, optimizations, simplified API.

using JRC.Collections.RedBlackTree;
using System.Collections.Generic;

namespace System.Linq
{
    /// <summary>
    /// Linq method extensions
    /// </summary>
    public static class RedBlackLinq
    {
        /// <summary>
        /// Creates a <see cref="RedBlackTreeList{T}" /> from an <see cref="IEnumerable{T}" />
        /// </summary>      
        public static RedBlackTreeList<T> ToRedBlackTreeList<T>(this IEnumerable<T> enumerable) where T : IRedBlackTreeListItem
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }
            var list = new RedBlackTreeList<T>();
            foreach (var item in enumerable)
            {
                list.Add(item);
            }
            return list;
        }

        /// <summary>
        /// Creates a <see cref="RedBlackTreeDictionary{TKey, TValue}" /> from an <see cref="IEnumerable{T}" /> according to a specified key selector function and an optional comparer (else uses TKey's default comparer).
        /// </summary>
        public static RedBlackTreeDictionary<TKey, TValue> ToRedBlackTreeDictionary<TKey, TValue>(this IEnumerable<TValue> enumerable, Func<TValue, TKey> keySelector, IComparer<TKey> comparer = null)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }
            var dico = new RedBlackTreeDictionary<TKey, TValue>(comparer);
            foreach (var item in enumerable)
            {
                var key = keySelector(item);
                dico.Add(key, item);
            }
            return dico;
        }

        /// <summary>
        /// Creates a <see cref="RedBlackTreeDictionary{TKey, TValue}" /> from an <see cref="IEnumerable{T}" /> according to a specified key selector function and an optional comparison (else uses TKey's default comparer).
        /// </summary>
        public static RedBlackTreeDictionary<TKey, TValue> ToRedBlackTreeDictionary<TKey, TValue>(this IEnumerable<TValue> enumerable, Func<TValue, TKey> keySelector, Comparison<TKey> comparison = null)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }            
            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }
            var dico = new RedBlackTreeDictionary<TKey, TValue>(comparison);
            foreach (var item in enumerable)
            {
                var key = keySelector(item);
                dico.Add(key, item);
            }
            return dico;
        }

        /// <summary>
        /// Creates a <see cref="RedBlackTreeDictionary{TKey, TValue}" /> from an <see cref="IEnumerable{T}" /> according to a specified key selector function and a specified value selector function, and an optional comparer (else uses TKey's default comparer).
        /// </summary>
        public static RedBlackTreeDictionary<TKey, TValue> ToRedBlackTreeDictionary<T, TKey, TValue>(this IEnumerable<T> enumerable, Func<T, TKey> keySelector, Func<T, TValue> valueSelector, IComparer<TKey> comparer = null)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }
            if (valueSelector == null)
            {
                throw new ArgumentNullException(nameof(valueSelector));
            }
            var dico = new RedBlackTreeDictionary<TKey, TValue>(comparer);
            foreach (var item in enumerable)
            {
                var key = keySelector(item);
                var value = valueSelector(item);
                dico.Add(key, value);
            }
            return dico;
        }

        /// <summary>
        /// Creates a <see cref="RedBlackTreeDictionary{TKey, TValue}" /> from an <see cref="IEnumerable{T}" /> according to a specified key selector function and a specified value selector function, and an optional comparison (else uses TKey's default comparer).
        /// </summary>
        public static RedBlackTreeDictionary<TKey, TValue> ToRedBlackTreeDictionary<T, TKey, TValue>(this IEnumerable<T> enumerable, Func<T, TKey> keySelector, Func<T, TValue> valueSelector, Comparison<TKey> comparison = null)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }
            if (valueSelector == null)
            {
                throw new ArgumentNullException(nameof(valueSelector));
            }
            var dico = new RedBlackTreeDictionary<TKey, TValue>(comparison);
            foreach (var item in enumerable)
            {
                var key = keySelector(item);
                var value = valueSelector(item);
                dico.Add(key, value);
            }
            return dico;
        }

        /// <summary>
        /// Creates a <see cref="RedBlackTreeSet{T}" /> from an <see cref="IEnumerable{T}" />, specifying optionally if set allows duplicates (default false), key comparer (else uses T's default comparer), and satellite comparer (else uses HashCode comparer)
        /// </summary>
        public static RedBlackTreeSet<T> ToRedBlackTreeSet<T>(this IEnumerable<T> enumerable, bool allowDuplicates = false, IComparer<T> comparer = null, IComparer<T> satelliteComparer = null)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }
            var set = new RedBlackTreeSet<T>(allowDuplicates, comparer, satelliteComparer);
            foreach (var item in enumerable)
            {
                set.Add(item);
            }
            return set;         
        }

        /// <summary>
        /// Creates a <see cref="RedBlackTreeSet{T, TSortKey}" /> from an <see cref="IEnumerable{T}" />, according to a specified sort key provider, and, specifying optionally if set allows duplicates (default false), key comparer (else uses TSortKey's default comparer), and satellite comparer (else uses HashCode comparer)
        /// </summary>
        public static RedBlackTreeSet<T, TSortKey> ToRedBlackTreeSet<T, TSortKey>(this IEnumerable<T> enumerable, RedBlackTreeSet<T, TSortKey>.ISortKeyProvider sortKeyProvider,  bool allowDuplicates = false, IComparer<TSortKey> comparer = null, IComparer<T> satelliteComparer = null)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }
            if (sortKeyProvider == null)
            {
                throw new ArgumentNullException(nameof(sortKeyProvider));
            }
            var set = new RedBlackTreeSet<T, TSortKey>(allowDuplicates, sortKeyProvider, comparer, satelliteComparer);
            foreach (var item in enumerable)
            {
                set.Add(item);
            }            
            return set;
        }

        /// <summary>
        /// Creates a <see cref="RedBlackTreeSet{T, TSortKey}" /> from an <see cref="IEnumerable{T}" />, according to a specified sort key provider function, and, specifying optionally if set allows duplicates (default false), key comparer (else uses TSortKey's default comparer), and satellite comparer (else uses HashCode comparer)
        /// </summary>
        public static RedBlackTreeSet<T, TSortKey> ToRedBlackTreeSet<T, TSortKey>(this IEnumerable<T> enumerable, Func<T, TSortKey> sortKeyProvider, bool allowDuplicates = false, IComparer<TSortKey> comparer = null, IComparer<T> satelliteComparer = null)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }
            if (sortKeyProvider == null)
            {
                throw new ArgumentNullException(nameof(sortKeyProvider));
            }
            var set = new RedBlackTreeSet<T, TSortKey>(allowDuplicates, sortKeyProvider, comparer, satelliteComparer);
            foreach (var item in enumerable)
            {
                set.Add(item);
            }
            return set;
        }
    }
}
