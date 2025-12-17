// Licensed under MIT license.
// Author: JRC
//
// Based on Microsoft's RBTree<K> from System.Data (Copyright Microsoft Corporation).
// Improvements: faster list enumeration, optimizations, simplified API.

using System;
using System.Collections;
using System.Collections.Generic;

namespace JRC.Collections.RedBlackTree
{
    /// <summary>
    /// Wrapper class that encapsule RedBlackTreeIndex in order to implement IList&lt;T;get;
    /// </summary>
    /// <remarks>This list does not supports duplicates. If you want to manage duplicates, create your own wrapper</remarks>
    [Serializable]
    public sealed class RedBlackTreeList<T> : IList<T>, IList
#if NET45_OR_GREATER || NETCOREAPP || NETSTANDARD
        , IReadOnlyList<T>
#endif
        where T : IRedBlackTreeListItem
    {       
        private readonly RedBlackTreeIndex<T> innerTree;

        public RedBlackTreeList()
        {
            this.innerTree = new RedBlackTreeIndex<T>();
        }

        #region IList<T> members
        public T this[int position]
        {
            get
            {
                return this.innerTree.GetAt(position);
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.NodeId != RedBlackTreeBase<T>.NIL)
                {
                    throw new ArgumentException($"Value already belongs to a red black tree. NodeId = {value.NodeId}");
                }
                var nodeId = this.innerTree.SetAt(position, value, out var oldValue);
                value.NodeId = nodeId;
                oldValue.NodeId = RedBlackTreeBase<T>.NIL;
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public int Count
        {
            get
            {
                return this.innerTree.Count;
            }
        }

        public void Swap(int positionX, int positionY)
        {
            this.innerTree.Swap(positionX, positionY, out var newItemX, out var newItemY);
            var tmp = newItemX.NodeId;
            newItemX.NodeId = newItemY.NodeId;
            newItemY.NodeId = tmp;
        }

        public bool Contains(T item)
        {
            if (item == null || item.NodeId == RedBlackTreeBase<T>.NIL)
            {
                return false;
            }
            return this.innerTree.IndexOf(item.NodeId) != -1;
        }

        public int IndexOf(T item)
        {
            if (item == null || item.NodeId == RedBlackTreeBase<T>.NIL)
            {
                return -1;
            }
            return this.innerTree.IndexOf(item.NodeId);
        }

        public void Add(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (item.NodeId != RedBlackTreeBase<T>.NIL)
            {
                throw new ArgumentException($"Value already belongs to a red black tree. NodeId = {item.NodeId}");
            }
            item.NodeId = this.innerTree.Add(item);
        }

        public void Insert(int position, T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (item.NodeId != RedBlackTreeBase<T>.NIL)
            {
                throw new ArgumentException($"Value already belongs to a red black tree. NodeId = {item.NodeId}");
            }
            item.NodeId = this.innerTree.Insert(position, item);
        }

        public bool Remove(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (this.innerTree.Remove(item.NodeId))
            {
                item.NodeId = RedBlackTreeBase<T>.NIL;
                return true;
            }
            return false;
        }

        public void RemoveAt(int position)
        {            
            var item = this.innerTree.RemoveAt(position);
            item.NodeId = RedBlackTreeBase<T>.NIL;
        }

        public void Clear()
        {
            foreach (var item in this.innerTree)
            {
                item.NodeId = RedBlackTreeBase<T>.NIL;
            }
            this.innerTree.Clear();            
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.innerTree.CopyTo(array, arrayIndex);
        }        

        public IEnumerator<T> GetEnumerator()
        {
            return this.innerTree.GetEnumerator();
        }
        #endregion

        #region IList members
        object IList.this[int position]
        {
            get
            {
                return this[position];
            }

            set
            {
                if (value is T item)
                {
                    this[position] = item;
                    return;
                }
                throw new InvalidOperationException($"Value should be of type {typeof(T).Name}");
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        int ICollection.Count
        {
            get
            {
                return this.Count;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return this;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        int IList.Add(object value)
        {
            if (value is T item)
            {
                this.Add(item);
                return this.Count - 1;
            }
            throw new InvalidOperationException($"Value should be of type {typeof(T).Name}");
        }

        void IList.Clear()
        {
            this.Clear();
        }

        bool IList.Contains(object value)
        {
            return value is T item && this.Contains(item);
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            this.innerTree.CopyTo(array, arrayIndex);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        int IList.IndexOf(object value)
        {
            if (value is T item)
            {
                return this.IndexOf(item);
            }
            return -1;
        }

        void IList.Insert(int position, object value)
        {
            if (value is T item)
            {
                this.Insert(position, item);
                return;
            }
            throw new InvalidOperationException($"Value should be of type {typeof(T).Name}");
        }

        void IList.Remove(object value)
        {
            if (value is T item)
            {
                this.Remove(item);
            }
        }

        void IList.RemoveAt(int position)
        {
            this.RemoveAt(position);
        }       
        #endregion
    }

    public interface IRedBlackTreeListItem
    {
        int NodeId { get; set; }
    }
}
