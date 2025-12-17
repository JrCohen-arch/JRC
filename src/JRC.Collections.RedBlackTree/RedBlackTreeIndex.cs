// Licensed under MIT license.
// Author: JRC
//
// Based on Microsoft's RBTree<K> from System.Data (Copyright Microsoft Corporation).
// Improvements: faster list enumeration, optimizations, simplified API.

using System;
using System.Collections.Generic;

namespace JRC.Collections.RedBlackTree
{
    /// <summary>
    /// Class implementing raw list mechanism that can be wrapped in order to implement IList&lt;T&gt;.  
    /// If you want to use the default wrapper (easy to use), see <see cref="RedBlackTreeList{T}"/>
    /// </summary>
    /// <remarks>
    /// this[index] is O(logN) - slower than List&lt;T&gt; that is O(1).
    /// however, as a compensation, Add/Insert & Remove operations are also in O(logN) (quicker than List&lt;T&gt;).
    /// Enumeration is in O(1) like the List&lt;T&gt; thanks to successor link (not present on microsoft code).
    /// </remarks>
    [Serializable]
    public sealed class RedBlackTreeIndex<T> : RedBlackTreePlus<T>, IEnumerable<T>
    {       
        #region Like-List members              
        /// <summary>
        /// Set Item by position. Allow to implement this[index] set;
        /// </summary>        
        public int SetAt(int position, T value, out T oldValue)
        {
            int nodeId = GetNodeIdByIndex(this.root, unchecked(position + 1));
            if (nodeId == NIL)
            {
                throw new IndexOutOfRangeException(nameof(position));
            }
            oldValue = this.Value(nodeId);
            this.SetValue(nodeId, value);
            unchecked { _version++; }
            return nodeId;
        }

        public void Swap(int positionX, int positionY, out T newItemX, out T newItemY)
        {
            int nodeIdX = GetNodeIdByIndex(this.root, unchecked(positionX + 1));
            int nodeIdY = GetNodeIdByIndex(this.root, unchecked(positionY + 1));
            if (nodeIdX == NIL || nodeIdY == NIL)
                throw new IndexOutOfRangeException();
            newItemY = this.Value(nodeIdX);
            newItemX = this.Value(nodeIdY);
            this.SetValue(nodeIdX, newItemX);
            this.SetValue(nodeIdY, newItemY);
        }

        /// <summary>
        /// Return the index of the specified nodeId. Speed O(Log(n))
        /// </summary>
        public int IndexOf(int nodeId)
        {
            return this.IndexOfNode(nodeId);
        }
        /// <summary>
        /// Add the specified item and return his node id. Speed O(Log(n))
        /// </summary>
        public int Add(T item)
        {
            int nodeId = GetNewNode(item);
            RBInsert(nodeId, -1);
            return nodeId;
        }
        /// <summary>
        /// Add the specified item at position and return his node id. Speed O(Log(n))
        /// </summary>
        public int Insert(int position, T item)
        {
            int nodeId = GetNewNode(item);
            RBInsert(nodeId, position);
            return nodeId;
        }
        /// <summary>
        /// Remove node with specified node id. Speed O(Log(n))
        /// </summary>
        public bool Remove(int nodeId)
        {
            if (nodeId == NIL)
            {
                return false;
            }

            int pageId = nodeId >> 16;
            if (pageId >= _pageTable.Length || _pageTable[pageId] == null)
            {
                return false;
            }

            int slotIndex = nodeId & 0xFFFF;                      
            int mapIndex = slotIndex / TreePage.slotLineSize;
            int bitMask = 1 << (slotIndex % TreePage.slotLineSize);

            if ((_pageTable[pageId]._slotMap[mapIndex] & bitMask) == 0)
            {
                return false;
            }

            RBDelete(nodeId);
            return true;
        }        
        #endregion

        #region tree core methods             
        /// <summary>
        /// Inserts a new node id in the tree 
        /// </summary>
        private void RBInsert(int nodeId, int position)
        {
            unchecked { _version++; }

            // Insert Node x at the appropriate position            
            int y_id = NIL;
            int z_id = root;  

            if (position == -1)
            {
                position = SubTreeSize(root);   // append
            }

            while (z_id != NIL)    // in-order traverse and find node with a NILL left or right child
            {
                IncreaseSize(z_id);
                y_id = z_id;            // y_id set to the proposed parent of x_id
                int left_z_id = Left(z_id);
                int c = position - SubTreeSize(left_z_id);

                if (c <= 0)
                {
                    z_id = left_z_id;
                }
                else
                {
                    //position = position - SubTreeSize(z_id);
                    z_id = Right(z_id);
                    if (z_id != NIL)
                    {
                        position = c - 1;    //skip computation of position for leaf node
                    }
                }
            }

            SetParent(nodeId, y_id);
            if (y_id == NIL)
            {
                root = nodeId;
                // Successor: nodeId is the only node, so no predecessor
                SetLink(nodeId, NIL);
            }
            else
            {
                int c = (position <= 0) ? -1 : 1;

                if (c < 0)
                {
                    // Successor: x_id becomes predecessor of y_id
                    // Find y_id's predecessor and insert x_id between them

                    int pred = SearchPredecessor(y_id);

                    SetLeft(y_id, nodeId); // tree modification here

                    if (pred != NIL)
                    {
                        SetLink(pred, nodeId);
                    }
                    SetLink(nodeId, y_id);
                }
                else
                {
                    SetRight(y_id, nodeId);
                    // Successor: x_id becomes successor of y_id
                    // Insert x_id between y_id and its old successor
                    int succ = Link(y_id);
                    SetLink(y_id, nodeId);
                    SetLink(nodeId, succ);
                }
            }

            SetLeft(nodeId, NIL);
            SetRight(nodeId, NIL);
            SetColor(nodeId, NodeColor.red);
            
            RBInsertFixup(NIL, nodeId, NIL);
        }
        #endregion
    }
}
