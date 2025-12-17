// Licensed under MIT license.
// Author: JRC
//
// Based on Microsoft's RBTree<K> from System.Data (Copyright Microsoft Corporation).
// Improvements: faster list enumeration, optimizations, simplified API.

using System;
#if NET45_OR_GREATER || NETCOREAPP
using System.Runtime.CompilerServices;
#endif
namespace JRC.Collections.RedBlackTree
{
    /// <summary>
    /// Base class for red black trees
    /// </summary>    
    [Serializable]
    public abstract class RedBlackTreeBase<T>
    {
        // 2^16 #pages * 2^n == total number of nodes.  512 = 32 million, 1024 = 64 million, 2048 = 128m, 4096=256m, 8192=512m, 16284=1 billion 32K=2 billion.
        protected const int DefaultPageSize = 32; 
        public const int NIL = 0; // 0th page, 0th slot for each tree 

        private readonly LinkUsage linkUsage;

        protected TreePage[] _pageTable; // initial size 4, then doubles (grows) - it never shrinks.
        private int[] _pageTableMap;
        private int _inUsePageCount;   // contains count of allocated pages per tree, its <= the capacity of  pageTable
        private int _nextFreePageLine; // used for keeping track of position of last used free page in pageTable
        protected int root;
        protected int _version;
        protected int _inUseNodeCount; // total number of nodes currently in use by this tree.              

        protected RedBlackTreeBase(LinkUsage linkUsage)
        {
            this.linkUsage = linkUsage;
            this.Reset();
        }      

        protected virtual void Reset()
        {
            root = NIL;
            _pageTable = new TreePage[1 * TreePage.slotLineSize];
            _pageTableMap = new int[(_pageTable.Length + TreePage.slotLineSize - 1) / TreePage.slotLineSize]; // Ceiling(size)
            _inUsePageCount = 0;
            _nextFreePageLine = 0;
            AllocPage(DefaultPageSize);

            _inUseNodeCount = 1;

            // alloc storage for reserved NIL node. segment 0, slot 0; Initialize NIL
            _pageTable[0]._slotMap[0] = 0x1;
            _pageTable[0].InUseCount = 1;
            _pageTable[0]._slots[0].Color = NodeColor.black;
        }

        protected int IndexOfNode(int nodeId)
        {
            int myRank = SubTreeSize(Left(nodeId));
            while (nodeId != NIL)
            {
                int parent = Parent(nodeId);
                if (nodeId == Right(parent))
                {
                    myRank += (SubTreeSize(Left(parent)) + 1);
                }
                nodeId = parent;
            }
            return myRank;
        }

        /// <summary>
        /// Clears this treelist
        /// </summary>
        public void Clear()
        {
            unchecked { _version++; }
            this.Reset();
        }

        #region Tree Operations
        /// <summary>
        /// Fix the specified tree for RedBlack properties after insert
        /// </summary>
        protected int RBInsertFixup(int root_id, int nodeId, int mainTreeNodeID)
        {
            int y_id;
            int parent_nodeId;
            while (Color((parent_nodeId = Parent(nodeId))) == NodeColor.red)
            {
                var grandParent_nodeId = Parent(parent_nodeId);
                if (parent_nodeId == Left(grandParent_nodeId))     // if x.parent is a left child
                {
                    y_id = Right(grandParent_nodeId);              // x.parent.parent.right;
                    if (Color(y_id) == NodeColor.red)              // my right uncle is red
                    {
                        SetColor(parent_nodeId, NodeColor.black);      // x.parent.color = Color.black;
                        SetColor(y_id, NodeColor.black);
                        SetColor(grandParent_nodeId, NodeColor.red);   // x.parent.parent.color = Color.red;
                        nodeId = grandParent_nodeId;                     // x = x.parent.parent;
                    }
                    else
                    {     // my right uncle is black
                        if (nodeId == Right(parent_nodeId))
                        {
                            nodeId = parent_nodeId;
                            root_id = LeftRotate(root_id, nodeId, mainTreeNodeID);
                            parent_nodeId = Parent(nodeId);
                            grandParent_nodeId = Parent(parent_nodeId);
                        }

                        SetColor(parent_nodeId, NodeColor.black);                           // x.parent.color = Color.black;
                        SetColor(grandParent_nodeId, NodeColor.red);                 //    x.parent.parent.color = Color.red;
                        root_id = RightRotate(root_id, grandParent_nodeId, mainTreeNodeID);   //    RightRotate (x.parent.parent);
                    }
                }
                else
                {     // x.parent is a right child
                    y_id = Left(grandParent_nodeId);          // y = x.parent.parent.left;
                    if (Color(y_id) == NodeColor.red)      // if (y.color == Color.red)    // my right uncle is red
                    {
                        SetColor(parent_nodeId, NodeColor.black);
                        SetColor(y_id, NodeColor.black);
                        SetColor(grandParent_nodeId, NodeColor.red);   // x.parent.parent.color = Color.red;
                        nodeId = grandParent_nodeId;
                    }
                    else
                    {// my right uncle is black
                        if (nodeId == Left(parent_nodeId))
                        {
                            nodeId = parent_nodeId;
                            root_id = RightRotate(root_id, nodeId, mainTreeNodeID);
                            parent_nodeId = Parent(nodeId);
                            grandParent_nodeId = Parent(parent_nodeId);
                        }

                        SetColor(parent_nodeId, NodeColor.black);             // x.parent.color = Color.black;
                        SetColor(grandParent_nodeId, NodeColor.red);   // x.parent.parent.color = Color.red;
                        root_id = LeftRotate(root_id, grandParent_nodeId, mainTreeNodeID);
                    }
                }
            }

            if (root_id == NIL)
                SetColor(root, NodeColor.black);
            else
                SetColor(root_id, NodeColor.black);
            return root_id;
        }
        /// <summary>
        /// Fix the specified tree for RedBlack properties after delete
        /// </summary>
        protected int RBDeleteFixup(int root_id, int x_id, int px_id /* px is parent of x */, int mainTreeNodeID)
        {
            //x is successor's non nil child or nil if both children are nil
            int w_id;

            if (x_id == NIL && px_id == NIL)
            {
                return NIL; //case of satellite tree root being deleted.
            }

            while (((root_id == NIL ? root : root_id) != x_id) && Color(x_id) == NodeColor.black)
            {
                var parent_x_id = Parent(x_id);

                // (1) x's parent should have aleast 1 non-NIL child.
                // (2) check if x is a NIL left child or a non NIL left child
                if ((x_id != NIL && x_id == Left(parent_x_id)) || (x_id == NIL && Left(px_id) == NIL))
                {
                    // we have from DELETE, then x cannot be NIL and be a right child of its parent
                    // also from DELETE, if x is non nil, it will be a left child.
                    w_id = (x_id == NIL) ? Right(px_id) : Right(parent_x_id);     // w is x's right sibling and it cannot be NIL

                    if (w_id == NIL)
                    {
                        throw new InvalidOperationException("Internal RBTree error in RBDeleteFixup");
                    }

                    if (Color(w_id) == NodeColor.red)
                    {
                        SetColor(w_id, NodeColor.black);
                        SetColor(px_id, NodeColor.red);
                        root_id = LeftRotate(root_id, px_id, mainTreeNodeID);
                        w_id = (x_id == NIL) ? Right(px_id) : Right(parent_x_id);
                    }

                    if (Color(Left(w_id)) == NodeColor.black && Color(Right(w_id)) == NodeColor.black)
                    {
                        SetColor(w_id, NodeColor.red);
                        x_id = px_id;
                        px_id = Parent(px_id); //maintain px_id
                    }
                    else
                    {
                        if (Color(Right(w_id)) == NodeColor.black)
                        {
                            SetColor(Left(w_id), NodeColor.black);
                            SetColor(w_id, NodeColor.red);
                            root_id = RightRotate(root_id, w_id, mainTreeNodeID);
                            w_id = (x_id == NIL) ? Right(px_id) : Right(parent_x_id);
                        }

                        SetColor(w_id, Color(px_id));
                        SetColor(px_id, NodeColor.black);
                        SetColor(Right(w_id), NodeColor.black);
                        root_id = LeftRotate(root_id, px_id, mainTreeNodeID);

                        x_id = (root_id == NIL) ? root : root_id;
                        px_id = parent_x_id;
                    }
                }
                else
                {  //x is a right child or it is NIL
                    w_id = Left(px_id);
                    if (Color(w_id) == NodeColor.red)
                    {   // x_id is y's (the spliced node) sole non-NIL child or NIL if y had no children
                        SetColor(w_id, NodeColor.black);
                        if (x_id != NIL)
                        {
                            SetColor(px_id, NodeColor.red);
                            root_id = RightRotate(root_id, px_id, mainTreeNodeID);
                            w_id = (x_id == NIL) ? Left(px_id) : Left(parent_x_id);
                        }
                        else
                        {
                            //we have from DELETE, then x cannot be NIL and be a right child of its parent
                            // w_id cannot be nil.
                            SetColor(px_id, NodeColor.red);
                            root_id = RightRotate(root_id, px_id, mainTreeNodeID);
                            w_id = (x_id == NIL) ? Left(px_id) : Left(parent_x_id);

                            if (w_id == NIL)
                            {
                                throw new InvalidOperationException("Internal RBTree error in RBDeleteFixup - Cannot rotate invalid successor");                                
                            }
                        }
                    }

                    if (Color(Right(w_id)) == NodeColor.black && Color(Left(w_id)) == NodeColor.black)
                    {
                        SetColor(w_id, NodeColor.red);
                        x_id = px_id;
                        px_id = Parent(px_id);
                    }
                    else
                    {
                        if (Color(Left(w_id)) == NodeColor.black)
                        {
                            SetColor(Right(w_id), NodeColor.black);
                            SetColor(w_id, NodeColor.red);
                            root_id = LeftRotate(root_id, w_id, mainTreeNodeID);
                            w_id = (x_id == NIL) ? Left(px_id) : Left(parent_x_id);
                        }

                        SetColor(w_id, Color(px_id));
                        SetColor(px_id, NodeColor.black);
                        SetColor(Left(w_id), NodeColor.black);
                        root_id = RightRotate(root_id, px_id, mainTreeNodeID);

                        x_id = (root_id == NIL) ? root : root_id;
                        px_id = parent_x_id;
                    }
                }
            }

            SetColor(x_id, NodeColor.black);
            return root_id;
        }
        /// <summary>
        /// Left rotate on x_id
        /// </summary>
        protected int LeftRotate(int root_id, int x_id, int mainTreeNode)
        {
            int y_id = Right(x_id);
            int left_y_id = Left(y_id);
            // Turn y's left subtree into x's right subtree
            SetRight(x_id, left_y_id);
            if (left_y_id != NIL)
            {
                SetParent(left_y_id, x_id);
            }

            int parent_x_id = Parent(x_id);
            SetParent(y_id, parent_x_id);
            if (parent_x_id == NIL)
            {
                if (root_id == NIL)
                {
                    root = y_id;
                }
                else
                {
                    // case : NextUsage - when a satellite tree is present.
                    SetLink(mainTreeNode, y_id); 
                    SetValue(mainTreeNode, Value(y_id));
                    root_id = y_id;
                }
            }
            else if (x_id == Left(parent_x_id))
            {  
                // x is left child of its parent
                SetLeft(parent_x_id, y_id);
            }
            else
            {
                SetRight(parent_x_id, y_id);
            }

            SetLeft(y_id, x_id);
            SetParent(x_id, y_id);

            //maintain size:  y_id = parent & x_id == child
            if (x_id != NIL)
            {
                this.RecomputeSize(x_id);                
            }

            if (y_id != NIL)
            {
                this.RecomputeSize(y_id);                
            }
            return root_id;
        }
        /// <summary>
        /// Right rotate on x_id
        /// </summary>
        protected int RightRotate(int root_id, int x_id, int mainTreeNode)
        {
            int y_id = Left(x_id);
            int right_y_id = Right(y_id);
            SetLeft(x_id, right_y_id);       // Turn y's right subtree into x's left subtree
            if (right_y_id != NIL)
            {
                SetParent(right_y_id, x_id);
            }

            int parent_x_id = Parent(x_id);
            SetParent(y_id, parent_x_id);
            if (parent_x_id == NIL)
            {
                if (root_id == NIL)
                {
                    root = y_id;
                }
                else
                {
                    // case : NextUsage - when a satellite tree is present.
                    SetLink(mainTreeNode, y_id);
                    SetValue(mainTreeNode, Value(y_id));
                    root_id = y_id;
                }
            }
            else if (x_id == Left(parent_x_id)) // x is left child of its parent
            {
                SetLeft(parent_x_id, y_id);
            }
            else
            {
                SetRight(parent_x_id, y_id);
            }

            SetRight(y_id, x_id);
            SetParent(x_id, y_id);

            //maintain size: y_id == parent && x_id == child.
            if (x_id != NIL)
            {
                this.RecomputeSize(x_id);
            }

            if (y_id != NIL)
            {
                this.RecomputeSize(y_id);
            }
            return root_id;
        }
        #endregion

        #region Node Operations
        protected int Minimum(int x_id)
        {
            int left_x_id;
            while ((left_x_id = Left(x_id)) != NIL)
            {
                x_id = left_x_id;
            }
            return x_id;
        }
        private int Maximum(int x_id)
        {
            int right_x_id;
            while ((right_x_id = Right(x_id)) != NIL)
            {
                x_id = right_x_id;
            }
            return x_id;
        }
        protected void IncreaseSize(int nodeId)
        {
            _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].SubTreeSize += 1;
        }
        protected void DecreaseSize(int nodeId)
        {
            _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].SubTreeSize -= 1;
        }
        protected void RecomputeSize(int nodeId)
        {
            int size = SubTreeSize(Left(nodeId)) + SubTreeSize(Right(nodeId));
            int next;
            if (this.linkUsage == LinkUsage.Next && (next = Link(nodeId)) != NIL)
            {
                size += SubTreeSize(next);
            }
            else
            {
                size += 1;
            }                
            _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].SubTreeSize = size;
        }
        protected int SearchPredecessor(int x_id)
        {
            int left_x_id = Left(x_id);
            if (left_x_id != NIL)
                return Maximum(left_x_id); // return right most node in left sub-tree

            int y_id = Parent(x_id);
            while (y_id != NIL && x_id == Left(y_id))
            {
                x_id = y_id;
                y_id = Parent(y_id);
            }
            return y_id;
        }
        protected int SearchSuccessor(int x_id)
        {
            int right_x_id = Right(x_id);
            if (right_x_id != NIL)
                return Minimum(right_x_id); //return left most node in right sub-tree.

            int y_id = Parent(x_id);
            while (y_id != NIL && x_id == Right(y_id))
            {
                x_id = y_id;
                y_id = Parent(y_id);
            }
            return y_id;
        }
        protected int GetNodeIdByIndex(int x_id, int index)
        {
            while (x_id != NIL)
            {
                int y_id = Left(x_id);
                int rank = SubTreeSize(y_id) + 1;
                if (index < rank)
                {
                    x_id = y_id;
                }
                else if (rank < index)
                {
                    x_id = Right(x_id);
                    index -= rank;
                }
                else
                {
                    break;
                }
            }
            return x_id;
        }
        #endregion

        #region Node Properties (per NodeId)
#if NET45_OR_GREATER || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected int Left(int nodeId)
        {           
            return _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].LeftId;
        }
#if NET45_OR_GREATER || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void SetLeft(int nodeId, int leftNodeId)
        {         
            _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].LeftId = leftNodeId;
        }
#if NET45_OR_GREATER || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected int Right(int nodeId)
        {
            return _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].RightId;
        }
#if NET45_OR_GREATER || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void SetRight(int nodeId, int rightNodeId)
        {            
            _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].RightId = rightNodeId;
        }
#if NET45_OR_GREATER || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected int Parent(int nodeId)
        {           
            return _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].ParentId;
        }
#if NET45_OR_GREATER || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void SetParent(int nodeId, int parentNodeId)
        {           
            _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].ParentId = parentNodeId;
        }
#if NET45_OR_GREATER || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected int SubTreeSize(int nodeId)
        {           
            return _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].SubTreeSize;
        }
#if NET45_OR_GREATER || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void SetSubTreeSize(int nodeId, int size)
        {
            _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].SubTreeSize = size;
        }
#if NET45_OR_GREATER || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected NodeColor Color(int nodeId)
        {            
            return _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].Color;
        }
#if NET45_OR_GREATER || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void SetColor(int nodeId, NodeColor color)
        {            
            _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].Color = color;
        }
#if NET45_OR_GREATER || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected int Link(int nodeId)
        {            
            return (_pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].LinkId);
        }
#if NET45_OR_GREATER || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void SetLink(int nodeId, int nextNodeId)
        {            
            if(nodeId == nextNodeId)
            {
                throw new ArgumentException("Node cannot be a successor of itself");
            }
            _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].LinkId = nextNodeId;
        }
#if NET45_OR_GREATER || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected T Value(int nodeId)
        {          
            return _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].Value;
        }
#if NET45_OR_GREATER || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        protected void SetValue(int nodeId, T key)
        {
            _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF].Value = key;
        }        
        #endregion

        #region Node New/Free
        /// <summary>
        /// Allocate storage for a new node and assign in the specified key.
        /// </summary>
        /// <remarks>Find a page with free slots or allocate a new page. Use bitmap associated with page to allocate a slot. Mark the slot as used and return its index.</remarks>
        protected int GetNewNode(T key)
        {
            // find page with free slots, if none, allocate a new page
            TreePage page;

            int freePageIndex = GetIndexOfPageWithFreeSlot(true);
            if (freePageIndex != -1)
                page = _pageTable[freePageIndex];
            else if (_inUsePageCount < 4)
                page = AllocPage(DefaultPageSize);  // First 128 slots
            else if (_inUsePageCount < 32)
                page = AllocPage(256);
            else if (_inUsePageCount < 128)
                page = AllocPage(1024);
            else if (_inUsePageCount < 4096)
                page = AllocPage(4096);
            else if (_inUsePageCount < 32768)
                page = AllocPage(8192);              // approximately First 16 million slots (2^24)
            else
                page = AllocPage(65536);             // Page size to accommodate more than 16 million slots (Max 2 Billion and 16 million slots)

            // page contains atleast 1 free slot.
            int slotId = page.AllocSlot(this);

            if (slotId == -1)
                throw new InvalidOperationException("Internal RBTree error : no free slots");            

            // NodeId: Upper 16 bits pageId, lower bits slotId
            var selfId = (int)(((uint)page.PageId) << 16) | slotId;
            page._slots[slotId].SubTreeSize = 1;     // new Nodes have size 1.
            page._slots[slotId].Value = key;
            return selfId;
        }

        /// <summary>
        /// Remove node from page system. Remove page if page is now empty
        /// </summary>
        /// <param name="nodeId">The nodeId of the node to be freed</param>
        protected void FreeNode(int nodeId)
        {
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;

            page._slots[slotIndex] = default;

            // clear slotMap entry associated with nodeId
            page._slotMap[slotIndex / TreePage.slotLineSize] &= ~(1 << slotIndex % TreePage.slotLineSize);
            page.InUseCount--;
            _inUseNodeCount--;
            if (page.InUseCount == 0)
                FreePage(page);
            else if (page.InUseCount == page._slots.Length - 1)
                MarkPageFree(page); // With freeing of a node, a previous full page has a free slot.
        }
        #endregion

        #region Page Management
        /// <summary>
        /// Allocate a page.
        /// Look for an unallocated page entry.
        /// (1) If entry for an unallocated page exists in current pageTable - use it
        /// (2) else extend pageTable
        /// </summary>
        /// <param name="size">The size of the page to allocate</param>
        /// <returns></returns>
        private TreePage AllocPage(int size)
        {
            int freePageIndex = GetIndexOfPageWithFreeSlot(false);

            if (freePageIndex != -1)
            {
                _pageTable[freePageIndex] = new TreePage(size);
                _nextFreePageLine = freePageIndex / TreePage.slotLineSize;
            }
            else
            {
                // no free position found, increase pageTable size
                TreePage[] newPageTable = new TreePage[_pageTable.Length * 2];
                Array.Copy(_pageTable, newPageTable, _pageTable.Length);
                int[] newPageTableMap = new int[(newPageTable.Length + TreePage.slotLineSize - 1) / TreePage.slotLineSize];
                Array.Copy(_pageTableMap, newPageTableMap, _pageTableMap.Length);

                _nextFreePageLine = _pageTableMap.Length;
                freePageIndex = _pageTable.Length;
                _pageTable = newPageTable;
                _pageTableMap = newPageTableMap;
                _pageTable[freePageIndex] = new TreePage(size);
            }
            _pageTable[freePageIndex].PageId = freePageIndex;
            _inUsePageCount++;
            return _pageTable[freePageIndex];
        }

        /// <summary>
        /// Get index of page with a free slot
        /// </summary>
        /// <param name="allocatedPage">If true, look for an allocatedPage with free slot else look for an unallocated page entry in pageTable</param>
        /// <returns>if allocatedPage is true, return index of a page with at least 1 free slot else return index of an unallocated page, pageTable[index] is empty.</returns>        
        private int GetIndexOfPageWithFreeSlot(bool allocatedPage)
        {
            int pageTableMapPos = _nextFreePageLine;
            int pageIndex = -1;

            while (pageTableMapPos < _pageTableMap.Length)
            {
                if (((uint)_pageTableMap[pageTableMapPos]) < 0xFFFFFFFF)
                {
                    uint pageSegmentMap = (uint)_pageTableMap[pageTableMapPos];
                    while ((pageSegmentMap ^ (0xFFFFFFFF)) != 0)         //atleast one "0" is there (same as <0xFFFFFFFF)
                    {
                        uint pageWithFreeSlot = (~(pageSegmentMap)) & (pageSegmentMap + 1);

                        if ((_pageTableMap[pageTableMapPos] & pageWithFreeSlot) != 0) //paranoia check
                        {
                            throw new InvalidOperationException("Slot is already in use");
                        }

                        pageIndex = (pageTableMapPos * TreePage.slotLineSize) + GetIntValueFromBitMap(pageWithFreeSlot); // segment + offset
                        if (allocatedPage)
                        {
                            if (_pageTable[pageIndex] != null)
                                return pageIndex;
                        }
                        else
                        {
                            if (_pageTable[pageIndex] == null)
                                return pageIndex;           // pageIndex points to an unallocated Page
                        }
                        pageIndex = -1;
                        pageSegmentMap |= pageWithFreeSlot; // found "reset bit", but unallocated page, mark it as unavaiable and continue search
                    }
                }

                pageTableMapPos++;
            }

            if (_nextFreePageLine != 0)
            {
                //Try one more time, starting from 0th page segment position to locate a page with free slots
                _nextFreePageLine = 0;
                pageIndex = GetIndexOfPageWithFreeSlot(allocatedPage);
            }
            return pageIndex;
        }

        /// <summary>
        /// Frees the given page
        /// </summary>
        /// <param name="page"></param>
        private void FreePage(TreePage page)
        {
            MarkPageFree(page);
            _pageTable[page.PageId] = null;
            _inUsePageCount--;
        }

        /// <summary>
        /// Mark the specified page "Full" as all its slots aer in use
        /// </summary>
        /// <param name="page"></param>
        private void MarkPageFull(TreePage page)
        {
            // set bit associated with page to mark it as full            
            _pageTableMap[page.PageId / TreePage.slotLineSize] |= (1 << (page.PageId % TreePage.slotLineSize));
        }

        /// <summary>
        /// Mark the specified page as "Free". It has atleast 1 available slot.
        /// </summary>
        /// <param name="page"></param>
        private void MarkPageFree(TreePage page)
        {
            // set bit associated with page to mark it as free            
            _pageTableMap[page.PageId / TreePage.slotLineSize] &= ~(1 << (page.PageId % TreePage.slotLineSize));
        }

        /// <summary>
        /// Return the count of 0-bits before the first 1-bit is found - starts search from trail.
        /// </summary>
        /// <param name="bitMap"></param>
        /// <returns></returns>
        private static int GetIntValueFromBitMap(uint bitMap)
        {
#if NET3_0_OR_GREATER
            return System.Numerics.BitOperations.TrailingZeroCount(bitMap);
#else
            int value = 0; // 0 based slot position

            /*
             * Assumption: bitMap can have max, exactly 1 bit set.
             * convert bitMap to int value giving number of 0's to its right
             * return value between 0 and 31
             */
            if ((bitMap & 0xFFFF0000) != 0)
            {
                value += 16;
                bitMap >>= 16;
            }
            if ((bitMap & 0x0000FF00) != 0)
            {
                value += 8;
                bitMap >>= 8;
            }
            if ((bitMap & 0x000000F0) != 0)
            {
                value += 4;
                bitMap >>= 4;
            }
            if ((bitMap & 0x0000000C) != 0)
            {
                value += 2;
                bitMap >>= 2;
            }
            if ((bitMap & 0x00000002) != 0)
                value += 1;
            return value;
#endif
        }
        #endregion

        #region Inner Types
        protected enum LinkUsage : byte
        {
            Successor = 0,
            Next = 1,
        };

        /// <summary>
        /// Node color
        /// </summary>
        protected enum NodeColor : byte
        {
            red = 0,
            black = 1,
        };
        /// <summary>
        /// Node of the tree
        /// </summary>
        [Serializable]
        protected struct Node
        {
            public int LeftId;            // 4 - offset 0
            public int RightId;           // 4 - offset 4
            public int ParentId;          // 4 - offset 8
            public int LinkId;            // 4 - offset 12 (successor or next)
            public int SubTreeSize;       // 4 - offset 16
            public NodeColor Color;   // 1 - offset 20
            // padding : 3 bytes
            public T Value;           // offset 24. Optimized for T = 4 bytes (ex: int) [Total size:28]  or T = 8 bytes (ex: long or reference) [Total size: 32]
            
        }

        [Serializable]
        protected sealed class TreePage
        {
            public const int slotLineSize = 32;
            public const int MaxPageSize = 1024 * 64;
            public readonly Node[] _slots;             // List of slots
            public readonly int[] _slotMap;            // CEILING(slots.size/slotLineSize)
            public int InUseCount;                     // 0 to _slots.size
            public int PageId;                         // Page's Id
            private int _nextFreeSlotLine;             // o based position of next free slot line

            /*
             * size: number of slots per page. Maximum allowed is 64K
             */
            public TreePage(int size)
            {
                if (size > MaxPageSize)
                {
                    throw new InvalidOperationException($"Size cannot exceed {MaxPageSize}");
                }
                _slots = new Node[size];
                _slotMap = new int[(size + slotLineSize - 1) / slotLineSize];
            }

            /*
             * Allocate a free slot from the current page belonging to the specified tree.
             * return the Id of the allocated slot, or -1 if the current page does not have any free slots.
             */
            public int AllocSlot(RedBlackTreeBase<T> tree)
            {
                int segmentPos;  // index into _SlotMap
                int freeSlot;  // Uint, slot offset within the segment
                int freeSlotId = -1; // 0 based slot position

                if (InUseCount < _slots.Length)
                {
                    segmentPos = _nextFreeSlotLine;
                    while (segmentPos < _slotMap.Length)
                    {
                        if (unchecked((uint)_slotMap[segmentPos]) < 0xFFFFFFFF)
                        {
                            freeSlot = (~(_slotMap[segmentPos])) & unchecked(_slotMap[segmentPos] + 1);
                           
                            _slotMap[segmentPos] |= freeSlot; //mark free slot as used.
                            InUseCount++;
                            if (InUseCount == _slots.Length) // mark page as full
                                tree.MarkPageFull(this);
                            tree._inUseNodeCount++;

                            // convert freeSlotPos to int value giving number of 0's to its right i.e. freeSlotId
                            freeSlotId = GetIntValueFromBitMap(unchecked((uint)freeSlot));

                            _nextFreeSlotLine = segmentPos;
                            freeSlotId = (segmentPos * slotLineSize) + freeSlotId;
                            break;
                        }
                        else
                        {
                            segmentPos++;
                        }
                    }

                    if (freeSlotId == -1 && _nextFreeSlotLine != 0)
                    {
                        //Try one more time, starting from 0th segment position to locate a free slot.
                        _nextFreeSlotLine = 0;
                        freeSlotId = AllocSlot(tree);
                    }
                }

                return freeSlotId; // 0 based slot position
            }           
        }        
        #endregion
    }
}
