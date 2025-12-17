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
    /// Base class for <see cref="RedBlackTreeIndex{T}"/> and <see cref="RedBlackTreeDictionary{TKey, TValue}"/>. Implements a successor mechanism for enumeration.
    /// If you want to use the default wrapper (easy to use), see <see cref="RedBlackTreeList{T}"/>
    /// </summary>
    /// <remarks>
    /// Enumeration is in O(1) like the List&lt;T&gt; thanks to successor link (not present on microsoft code).
    /// </remarks>
    [Serializable]
    public abstract class RedBlackTreePlus<T> : RedBlackTreeBase<T>, IEnumerable<T>
    {
        protected RedBlackTreePlus() : base(LinkUsage.Successor)
        { }

        #region Like-List members
        /// <summary>
        /// Gets the count of items in this tree
        /// </summary>
        public int Count
        {
            get
            {
                return _inUseNodeCount - 1;
            }
        }
        
        /// <summary>
        /// Get Item by position. Allow to implement this[index] get;
        /// </summary>        
        public T GetAt(int position)
        {
            int nodeId = GetNodeIdByIndex(this.root, unchecked(position + 1));
            if (nodeId == NIL)
            {
                throw new IndexOutOfRangeException(nameof(position));
            }
            return Value(nodeId);
        }

        /// <summary>
        /// Remove at position. Speed O(Log(n))
        /// </summary>
        public T RemoveAt(int position)
        {
            int nodeId = GetNodeIdByIndex(this.root, unchecked(position + 1));
            if (nodeId == NIL)
            {
                throw new IndexOutOfRangeException(nameof(position));
            }
            var value = Value(nodeId);
            RBDelete(nodeId);
            return value;
        }

        /// <summary>
        /// Copies this tree into the specified array at the specified index
        /// </summary>
        public void CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            int count = Count;
            if (array.Length - index < Count)
            {
                throw new ArgumentException("Destination array is not long enough");
            }

            int x_id = Minimum(root);
            for (int i = 0; i < count; ++i)
            {
                array.SetValue(Value(x_id), index + i);
                x_id = Link(x_id);
            }
        }
        /// <summary>
        /// Copies this tree into the specified array at the specified index
        /// </summary>
        public void CopyTo(T[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            int count = Count;
            if (array.Length - index < Count)
            {
                throw new ArgumentException("Destination array is not long enough");
            }

            int x_id = Minimum(root);
            for (int i = 0; i < count; ++i)
            {
                // Can't annotate generic array for element nullability
                array[index + i] = Value(x_id);
                x_id = Link(x_id);
            }
        }
        /// <summary>
        /// Returns an enumerator in order to enumerate this list
        /// </summary>        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        /// <summary>
        /// Returns an enumerator in order to enumerate this list
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return new RBTreeEnumerator(this);
        }
        #endregion

        #region tree core methods             
        /// <summary>
        /// Removes a node from tree
        /// </summary>        
        protected void RBDelete(int nodeId)
        {
            int x_id = NIL; // used for holding spliced node (y_id's) child
            int y_id;                // the spliced node
            int py_id;           // for holding spliced node (y_id's) parent

            // if we reach here, we are guaranteed z_id.next is NIL.            
            int pred = SearchPredecessor(nodeId);

            if (Left(nodeId) == NIL || Right(nodeId) == NIL)
                y_id = nodeId;
            else
                y_id = Link(nodeId);

            if (Left(y_id) != NIL)
                x_id = Left(y_id);
            else
                x_id = Right(y_id);

            py_id = Parent(y_id);
            if (x_id != NIL)
                SetParent(x_id, py_id);

            if (py_id == NIL) // if the spliced node is the root.
            {
                root = x_id;
            }
            else if (y_id == Left(py_id))    // update y's parent to point to X as its child
                SetLeft(py_id, x_id);
            else
                SetRight(py_id, x_id);

            // traverse from y_id's parent to root and decrement size by 1
            int tmp_py_id = py_id;
            // case: 1, 2, 3
            while (tmp_py_id != NIL)
            {
                //DecreaseSize (py_id, (Next(y_id)==NIL)?1:Size(Next(y_id)));
                RecomputeSize(tmp_py_id);
                tmp_py_id = Parent(tmp_py_id);
            }

            if (Color(y_id) == NodeColor.black)
            {
                RBDeleteFixup(NIL, x_id, py_id, NIL); // passing x.parent as y.parent, to handle x=Node.NIL case.
            }

            // In order to pin a key to it's node, free deleted z_id instead of the spliced y_id
            if (y_id != nodeId)
            {
                // we know that key, next and value are same for z_id and y_id
                SetLeft(y_id, Left(nodeId));
                SetRight(y_id, Right(nodeId));
                SetColor(y_id, Color(nodeId));
                SetSubTreeSize(y_id, SubTreeSize(nodeId));

                int parentNodeId = Parent(nodeId);
                if (parentNodeId != NIL)
                {
                    SetParent(y_id, parentNodeId);
                    if (Left(parentNodeId) == nodeId)
                    {
                        SetLeft(parentNodeId, y_id);
                    }
                    else
                    {
                        SetRight(parentNodeId, y_id);
                    }
                }
                else
                {
                    SetParent(y_id, NIL);
                }

                // update children.
                int left_nodeId = Left(nodeId);
                if (left_nodeId != NIL)
                {
                    SetParent(left_nodeId, y_id);
                }
                int right_nodeId = Right(nodeId);
                if (right_nodeId != NIL)
                {
                    SetParent(right_nodeId, y_id);
                }

                if (root == nodeId)
                {
                    root = y_id;
                }

                // successor:
                // we have copied all data from nodeId to y_id (except y_id's successor that still unchanged)
                // we now have just to change predecessor.successor to set it to y_id
                if (pred != NIL)
                {
                    SetLink(pred, y_id);
                }
            }
            else
            {
                // sucessor: Remove nodeId from chain by linking predecessor to nodeId's successor
                if (pred != NIL)
                {
                    SetLink(pred, Link(nodeId));  // ← Pas Link(nodeId)!
                }
            }

            FreeNode(nodeId);
            unchecked { _version++; }
        }
        #endregion

        #region inner types
        struct RBTreeEnumerator : IEnumerator<T>, IEnumerator
        {
            private readonly RedBlackTreePlus<T> _tree;
            private readonly int _version;
            private int _currentNodeId;
            private T _current;

            internal RBTreeEnumerator(RedBlackTreePlus<T> tree)
            {
                _tree = tree;
                _version = tree._version;
                _currentNodeId = _tree.Minimum(_tree.root);
                _current = default;
            }


            public bool MoveNext()
            {
                if (_version != _tree._version)
                {
                    throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
                }
                if (this._currentNodeId == NIL)
                {
                    _current = default;
                    return false;
                }

                var page = this._tree._pageTable[this._currentNodeId >> 16];
                var slotId = this._currentNodeId & 0xFFFF;
                _current = page._slots[slotId].Value;
                _currentNodeId = page._slots[slotId].LinkId;

                return true;
            }

            public T Current
            {
                get
                {
                    return _current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _tree._version)
                {
                    throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
                }

                _currentNodeId = _tree.Minimum(_tree.root);
                _current = default;
            }

            public void Dispose()
            {
            }

        }
        #endregion
    }
}
