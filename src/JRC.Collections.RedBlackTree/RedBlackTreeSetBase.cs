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
    /// Base class for <see cref="RedBlackTreeSet{K}"/> and <see cref="RedBlackTreeSet{TItem, TSortKey}"/>.
    /// </summary>
    [Serializable]
    public abstract class RedBlackTreeSetBase<K> : RedBlackTreeBase<K>, ICollection<K>
    {
        private int _inUseSatelliteTreeCount; // total number of satellite associated with this tree.        
        protected IComparer<K> comparer;
        private IComparer<K> satelliteComparer;
        private bool allowDuplicates;
        
        /// <summary>
        /// Initialize a new instance of RedBlackTreeSet
        /// </summary>
        /// <param name="allowDuplicates">true if this tree allows duplicates (i.e. n different keys returning 0 with the nodeComparer)</param>
        /// <param name="nodeComparison">comparer in order to compare keys</param>
        /// <param name="satelliteNodeComparison">comparer in order to compare duplicate keys - allows to return duplicates in a stable order. If not provided GetHashCode() is used.</param>
        protected RedBlackTreeSetBase(bool allowDuplicates, IComparer<K> comparer, IComparer<K> satelliteComparer = null) : base(LinkUsage.Next)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }
            this.allowDuplicates = allowDuplicates;
            this.comparer = comparer;
            this.satelliteComparer = satelliteComparer ?? new HashCodeComparer();
        }

        #region properties
        /// <summary>
        /// True if this tree has duplicates
        /// </summary>
        public bool HasDuplicates
        {
            get
            {
                return 0 != _inUseSatelliteTreeCount;
            }
        }

        /// <summary>
        /// True if duplicates are not allowed
        /// </summary>
        public bool AllowDuplicates
        {
            get
            {
                return allowDuplicates;
            }
            set
            {
                if (!value && this.HasDuplicates)
                {
                    throw new InvalidOperationException("Unable to forbit duplicates because this treeset already contains duplicates");
                }
                this.allowDuplicates = value;
            }
        }
        /// <summary>
        /// Gets the count of items in this tree
        /// </summary>
        public int Count
        {
            get
            {
                return _inUseNodeCount - 1 - _inUseSatelliteTreeCount;
            }
        }

        /// <summary>
        /// Gets if this collection is readonly
        /// </summary>
        bool ICollection<K>.IsReadOnly
        {
            get
            {
                return false;
            }
        }        

        public IComparer<K> SatelliteComparer
        {
            get { return this.satelliteComparer; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (this.HasDuplicates)
                {
                    throw new InvalidOperationException("Unable to set satellite comparer if treeset already contains duplicates item(s)");
                }
                this.satelliteComparer = value;
            }
        }
        #endregion

        #region Public
        /// <summary>
        /// Add the specified key to the tree.
        /// </summary>
        /// <exception cref="InvalidOperationException">If item is already present in the tree (comparer can return zero so 2 keys can be equivalent, but they need to still different instances), or if satellite comparer returns zero in case of duplicate</exception>
        public void Add(K item)
        {
            int nodeId = GetNewNode(item);
            RBInsert(NIL, nodeId, NIL, -1, false);
        }

        /// <summary>
        /// Append the given item at the end of the tree WITHOUT any validation. You must check before calling this method that greatestItem instance is not already present in the tree and that satellite comparer cannot return zero when greatest item is compared with another key present in this tree.
        /// </summary>
        public void AppendUnsafeOrdered(K greatestItem)
        {
            int nodeId = GetNewNode(greatestItem);
            RBInsert(NIL, nodeId, NIL, -1, true);
        }

        /// <summary>
        /// Removes the specified key
        /// </summary>     
        public bool Remove(K key)
        {
            NodePath path = GetNodeByKey(key);
            if (path._nodeID == NIL)
                return false;

            RBDeleteX(NIL, path._nodeID, path._mainTreeNodeID);
            return true;
        }

        /// <summary>
        /// Return true if key is contained in this tree. Speed  O(Log(n))
        /// </summary>
        public bool Contains(K key)
        {
            return this.SearchSubTree(NIL, key) != NIL;
        }

        /// <summary>
        ///  Get the key at position. Speed O(Log(n))
        /// </summary>
        public K this[int position]
        {
            get
            {
                return this.Value(GetNodePathByIndex(position)._nodeID);
            }
        }

        /// <summary>
        /// return the index of the given key. Speed O(2*Log(n))
        /// </summary>        
        public int IndexOf(K key)
        {
            int nodeIndex = -1;
            NodePath nodeId = GetNodeByKey(key);
            if (nodeId._nodeID != NIL)
            {
                nodeIndex = GetIndexByNodePath(nodeId);
            }
            return nodeIndex;
        }

        /// <summary>
        /// Removes key at specified position. Speed O(log(n))
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public K RemoveAt(int position)
        {
            // This check was not correct, it should have been ((uint)this.Count <= (uint)i)
            // Even then, the index will be checked by GetNodebyIndex which will throw either
            // using RowOutOfRange or InternalRBTreeError depending on _accessMethod
            //
            //if (i >= (_inUseNodeCount - 1)) {
            //    hrow new ArgumentOutOfRangeException(nameof(position));
            //}

            K key;
            NodePath x_id = GetNodePathByIndex(position); // it'l throw if corresponding node does not exist
            key = Value(x_id._nodeID);
            RBDeleteX(NIL, x_id._nodeID, x_id._mainTreeNodeID);
            return key;
        }
        /// <summary>
        /// Copy keys to array at specified index (including satellite keys)
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
            if (array.Length - index < count)
            {
                throw new ArgumentException("Destination array is not long enough");
            }

            int nodeId = NIL;
            int mainTreeNodeId = root;
            int i = 0;
            while (SearchSuccessor(ref nodeId, ref mainTreeNodeId))
            {
                array.SetValue(Value(nodeId), index + i);
                i++;
            }
        }
        /// <summary>
        /// Copy keys to array at specified index (including satellite keys)
        /// </summary>
        public void CopyTo(K[] array, int index)
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
            if (array.Length - index < count)
            {
                throw new ArgumentException("Destination array is not long enough");
            }

            int nodeId = NIL;
            int mainTreeNodeId = root;
            int i = 0;
            while (SearchSuccessor(ref nodeId, ref mainTreeNodeId))
            {
                array[index + i] = Value(nodeId);
                i++;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<K> GetEnumerator()
        {
            return new RBTreeEnumerator(this);
        }

        public IEnumerator<K> GetEnumerator(int startIndex)
        {
            return new RBTreeEnumerator(this, startIndex);
        }

        public void UpdateNodeKey(K currentKey, K newKey)
        {
            // swap oldRecord with NewRecord in nodeId associated with oldRecord
            // if the matched node is a satellite root then also change the key in the associated main tree node.
            NodePath x_id = GetNodeByKey(currentKey);
            if (Parent(x_id._nodeID) == NIL && x_id._nodeID != root) //determine if x_id is a satellite root.
            {
                SetValue(x_id._mainTreeNodeID, newKey);
            }
            SetValue(x_id._nodeID, newKey);
        }

        /// <summary>
        /// Returns keys in the specified index range. O(log n + count)
        /// </summary>
        /// <param name="startIndex">Starting index (inclusive)</param>
        /// <param name="count">Number of elements to return</param>
        /// <returns>Keys in the specified range</returns>
        public IEnumerable<K> FindRangeByIndex(int startIndex, int count)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                yield break;

            var enumerator = GetEnumerator(startIndex);
            int returned = 0;
            while (returned < count && enumerator.MoveNext())
            {
                yield return enumerator.Current;
                returned++;
            }
        }

        #endregion

        #region Protected
        /// <summary>
        /// Finds a key using binary search navigation in O(log n).
        /// </summary>
        /// <param name="comparer">
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
        protected K FindKeyImpl(Func<K, int> comparer)
        {
            int nodeId = FindNodeInMainTree(comparer);
            return (nodeId != NIL) ? Value(nodeId) : default;
        }
        /// <summary>
        /// Finds keys using binary search navigation in O(log n).
        /// </summary>
        /// <param name="comparer">
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
        protected IEnumerable<K> FindKeysImpl(Func<K, int> comparer)
        {
            // 1. Trouver dans le main tree
            int mainNodeId = FindNodeInMainTree(comparer);

            if (mainNodeId == NIL)
                yield break;

            var nextId = Link(mainNodeId);
            if (nextId == NIL)
            {
                // main node
                yield return Value(mainNodeId);
            }
            else
            {
                // next ?
                // collect keys in satellite tree (including nextId)
                foreach (var k in CollectSatelliteKeys(nextId))
                {
                    yield return k;
                }
            }

        }
        /// <summary>
        /// Returns keys where comparer returns 0. O(log n + k) where k = number of results.
        /// </summary>
        /// <param name="minComparer">Returns 0 or negative for keys >= min boundary</param>
        /// <param name="maxComparer">Returns 0 or negative for keys &lt;= max boundary</param>
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
        protected IEnumerable<K> FindRangeImp(Func<K, int> minComparer, Func<K, int> maxComparer)
        {
            int nodeId = FindFirstInRange(minComparer);
            if (nodeId == NIL)
                yield break;

            int mainTreeNodeId = NIL;

            int nextId = Link(nodeId);
            if (nextId != NIL)
            {
                mainTreeNodeId = nodeId;
                nodeId = Minimum(nextId);
            }

            while (nodeId != NIL)
            {
                var key = Value(nodeId);
                if (maxComparer(key) > 0)
                    yield break;

                yield return key;

                SearchSuccessor(ref nodeId, ref mainTreeNodeId);
            }
        }
        protected override void Reset()
        {
            base.Reset();
            _inUseSatelliteTreeCount = 0;
        }
        #endregion

        #region Private
        private int RBInsert(int root_id, int nodeId, int mainTreeNodeID, int position, bool append)
        {
            unchecked { _version++; }

            // Insert Node x at the appropriate position
            int y_id = NIL;
            int z_id = (root_id == NIL) ? root : root_id;  //if non NIL, then use the specifid root_id as tree's root.

            if (!append)
            {
                while (z_id != NIL)  // in-order traverse and find node with a NILL left or right child
                {
                    IncreaseSize(z_id);
                    y_id = z_id;            // y_id set to the proposed parent of x_id

                    var nodeValue = Value(nodeId);
                    var zValue = Value(z_id);
                    int c = (root_id == NIL)
                        ? this.comparer.Compare(nodeValue, zValue)
                        : this.satelliteComparer.Compare(nodeValue, zValue);

                    if (c < 0)
                    {
                        z_id = Left(z_id);
                    }
                    else if (c > 0)
                    {
                        z_id = Right(z_id);
                    }
                    else
                    {
                        // Multiple records with same key - insert it to the duplicate record tree associated with current node

                        if (!this.allowDuplicates)
                        {
                            FreeNode(nodeId);
                            throw new System.Data.ConstraintException("Duplicate keys are not allowed");
                        }

                        if (root_id != NIL)
                        {
                            FreeNode(nodeId);
                            var areKeysDuplicates = object.Equals(nodeValue, zValue);
                            if (areKeysDuplicates)
                            {
                                throw new InvalidOperationException(
                                   $"Duplicate keys are not supported ({nodeValue}). Tree supports multiple keys with the same sort order (i.e. main comparer can return zero - so we have a logical duplicate) but keys have to be distinct.");
                            }
                            else
                            {
                                throw new InvalidOperationException(
                                   $"Satellite comparer returned 0 for keys {nodeValue} and {zValue}. Keys must be distinguishable by satelliteNodeComparer.");
                            }
                        }
                        if (Link(z_id) != NIL)
                        {
                            root_id = RBInsert(Link(z_id), nodeId, z_id, -1, false); // z_id is existing mainTreeNodeID
                            SetValue(z_id, Value(Link(z_id)));
                        }
                        else
                        {
                            int newMainTreeNodeId = NIL;
                            // The existing node is pushed into the Satellite Tree and a new Node
                            // is created in the main tree, whose's next points to satellite root.
                            newMainTreeNodeId = GetNewNode(Value(z_id));
                            _inUseSatelliteTreeCount++;

                            var parent_z_id = Parent(z_id);
                            var left_z_id = Left(z_id);
                            var right_z_id = Right(z_id);
                            // copy contents of z_id to dupRootId (main tree node).
                            SetLink(newMainTreeNodeId, z_id);
                            SetColor(newMainTreeNodeId, Color(z_id));
                            SetParent(newMainTreeNodeId, parent_z_id);
                            SetLeft(newMainTreeNodeId, left_z_id);
                            SetRight(newMainTreeNodeId, right_z_id);

                            // Update z_id's non-nil parent
                            if (Left(parent_z_id) == z_id)
                                SetLeft(parent_z_id, newMainTreeNodeId);
                            else if (Right(parent_z_id) == z_id)
                                SetRight(parent_z_id, newMainTreeNodeId);

                            // update children.
                            if (left_z_id != NIL)
                                SetParent(left_z_id, newMainTreeNodeId);
                            if (right_z_id != NIL)
                                SetParent(right_z_id, newMainTreeNodeId);

                            if (root == z_id)
                                root = newMainTreeNodeId;

                            // Reset z_id's pointers to NIL. It will start as the satellite tree's root.
                            SetColor(z_id, NodeColor.black);
                            SetParent(z_id, NIL);
                            SetLeft(z_id, NIL);
                            SetRight(z_id, NIL);

                            int savedSize = SubTreeSize(z_id);
                            SetSubTreeSize(z_id, 1);
                            // With z_id as satellite root, insert x_id
                            root_id = RBInsert(z_id, nodeId, newMainTreeNodeId, -1, false);

                            SetSubTreeSize(newMainTreeNodeId, savedSize);
                        }
                        return root_id;
                    }
                }
            }
            else
            {
                if (position == -1)
                {
                    position = SubTreeSize(root);   // append
                }

                while (z_id != NIL)    // in-order traverse and find node with a NILL left or right child
                {
                    IncreaseSize(z_id);
                    y_id = z_id;            // y_id set to the proposed parent of x_id
                    int left_y_id = Left(y_id);
                    //int c = (SubTreeSize(y_id)-(position)); // Actually it should be: SubTreeSize(y_id)+1 - (position + 1)
                    int c = position - SubTreeSize(left_y_id);

                    if (c <= 0)
                    {
                        z_id = left_y_id;
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
            }

            SetParent(nodeId, y_id);
            if (y_id == NIL)
            {
                if (root_id == NIL)
                {
                    root = nodeId;
                }
                else
                {
                    // technically we should never come here. Satellite tree always has a root and atleast 1 child.
                    // if it has only root as it's node, then the satellite tree gets collapsed into the main tree.
                    SetLink(mainTreeNodeID, nodeId);
                    SetValue(mainTreeNodeID, Value(nodeId));
                    root_id = nodeId;
                }
            }
            else
            {
                int c = (root_id == NIL)
                    ? this.comparer.Compare(Value(nodeId), Value(y_id))
                    : this.satelliteComparer.Compare(Value(nodeId), Value(y_id));

                if (c < 0)
                    SetLeft(y_id, nodeId);
                else
                    SetRight(y_id, nodeId);
            }

            SetLeft(nodeId, NIL);
            SetRight(nodeId, NIL);
            SetColor(nodeId, NodeColor.red);
            z_id = nodeId; // for verification later

            // fix the tree
            return RBInsertFixup(root_id, nodeId, mainTreeNodeID);
        } //Insert

        /*
         * RBDelete()
         *  root_id: root_id of the tree. it is NIL for Main tree.
         *  z_id    : node_id of node to be deleted
         *
         * returns: The id of the spliced node
         *
         * Case 1: Node is in main tree only        (decrease size in main tree)
         * Case 2: Node's key is shared with a main tree node whose next is non-NIL
         *                                       (decrease size in both trees)
         * Case 3: special case of case 2: After deletion, node leaves satellite tree with only 1 node (only root),
         *             it should collapse the satellite tree - go to case 4. (decrease size in both trees)
         * Case 4: (1) Node is in Main tree and is a satellite tree root AND
         *             (2) It is the only node in Satellite tree
         *                   (Do not decrease size in any tree, as its a collpase operation)
         *
         */
        private int RBDeleteX(int root_id, int nodeId, int mainTreeNodeID)
        {
            int x_id = NIL;      // used for holding spliced node (y_id's) child
            int y_id;            // the spliced node
            int py_id;           // for holding spliced node (y_id's) parent

            var nextId = Link(nodeId);
            if (nextId != NIL)
            {
                return RBDeleteX(nextId, nextId, nodeId); // delete root of satellite tree.
            }

            // if we reach here, we are guaranteed z_id.next is NIL.
            bool isCase3 = false;
            int mNode = mainTreeNodeID;

            int nextmNode = Link(mNode);
            if (nextmNode != NIL)
                root_id = nextmNode;

            int subTreeSizemNode = SubTreeSize(nextmNode);
            if (subTreeSizemNode == 2) // Link(mNode) == root_id
                isCase3 = true;
            else if (subTreeSizemNode == 1)
            {
                throw new InvalidOperationException("Internal RBTree error in SearchSuccessor. Invalid state in RBDelete (begin)."); ;
            }

            if (Left(nodeId) == NIL || Right(nodeId) == NIL)
                y_id = nodeId;
            else
                y_id = SearchSuccessor(nodeId);

            var leftId = Left(y_id);
            if (leftId != NIL)
                x_id = leftId;
            else
                x_id = Right(y_id);

            py_id = Parent(y_id);
            if (x_id != NIL)
                SetParent(x_id, py_id);

            if (py_id == NIL) // if the spliced node is the root.
            {
                // check for main tree or Satellite tree root
                if (root_id == NIL)
                    root = x_id;
                else
                {
                    // spliced node is root of satellite tree
                    root_id = x_id;
                }
            }
            else if (y_id == Left(py_id))    // update y's parent to point to X as its child
                SetLeft(py_id, x_id);
            else
                SetRight(py_id, x_id);

            if (y_id != nodeId)
            {
                // assign all values from y (spliced node) to z (node containing key to be deleted)
                // -----------

                SetValue(nodeId, Value(y_id));      // assign all values from y to z
                SetLink(nodeId, Link(y_id));    //z.value = y.value;
            }

            if (nextmNode != NIL)
            {
                // update mNode to point to satellite tree root and have the same key value.
                // mNode will have to be patched again after RBDeleteFixup as root_id can again change
                if (root_id == NIL && nodeId != mNode)
                {
                    throw new InvalidOperationException("Internal RBTree error in SearchSuccessor. Invalid state in RBDelete."); ;
                }
                // -- it's possible for Link(mNode) to be != NIL and root_id == NIL when, the spliced node is a mNode of some
                // -- satellite tree and its "next" gets assigned to mNode
                if (root_id != NIL)
                {
                    SetLink(mNode, root_id);
                    nextmNode = root_id;
                    SetValue(mNode, Value(root_id));
                }
            }

            // traverse from y_id's parent to root and decrement size by 1
            int tmp_py_id = py_id;
            // case: 1, 2, 3
            while (tmp_py_id != NIL)
            {
                //DecreaseSize (py_id, (Link(y_id)==NIL)?1:Size(Link(y_id)));
                RecomputeSize(tmp_py_id);
                tmp_py_id = Parent(tmp_py_id);
            }

            //if satellite tree node deleted, decrease size in main tree as well.
            if (root_id != NIL)
            {
                // case 2, 3
                int tmpId = mNode;
                while (tmpId != NIL)
                {
                    DecreaseSize(tmpId);
                    tmpId = Parent(tmpId);
                }
            }

            if (Color(y_id) == NodeColor.black)
                root_id = RBDeleteFixup(root_id, x_id, py_id, mainTreeNodeID); // passing x.parent as y.parent, to handle x=Node.NIL case.

            if (isCase3)
            {
                subTreeSizemNode = SubTreeSize(nextmNode); // refresh as we recomputed above
                // Collpase satellite tree, by swapping it with the main tree counterpart and freeing the main tree node
                if (mNode == NIL || subTreeSizemNode != 1)
                {
                    throw new InvalidOperationException("Internal RBTree error in SearchSuccessor. Invalid state in RBDelete (middle)."); ;
                }
                _inUseSatelliteTreeCount--;
                int satelliteRootId = nextmNode;
                var leftmNode = Left(mNode);
                var rightmNode = Right(mNode);
                var parentmNode = Parent(mNode);

                SetLeft(satelliteRootId, leftmNode);
                SetRight(satelliteRootId, rightmNode);
                SetSubTreeSize(satelliteRootId, subTreeSizemNode);
                SetColor(satelliteRootId, Color(mNode));  // Next of satelliteRootId is already NIL

                if (parentmNode != NIL)
                {
                    SetParent(satelliteRootId, parentmNode);
                    if (Left(parentmNode) == mNode)
                    {
                        SetLeft(parentmNode, satelliteRootId);
                    }
                    else
                    {
                        SetRight(parentmNode, satelliteRootId);
                    }
                }

                // update mNode's children.
                if (leftmNode != NIL)
                {
                    SetParent(leftmNode, satelliteRootId);
                }
                if (rightmNode != NIL)
                {
                    SetParent(rightmNode, satelliteRootId);
                }
                if (root == mNode)
                {
                    root = satelliteRootId;
                }

                FreeNode(mNode);
                mNode = NIL;
            }
            else if (nextmNode != NIL)
            {
                // update mNode to point to satellite tree root and have the same key value
                if (root_id == NIL && nodeId != mNode)
                { //if mNode being deleted, its OK for root_id (it should be) NIL.
                    throw new InvalidOperationException("Internal RBTree error in SearchSuccessor. Invalid state in RBDelete (end)."); ;
                }

                if (root_id != NIL)
                {
                    SetLink(mNode, root_id);
                    SetValue(mNode, Value(root_id));
                }
            }

            // In order to pin a key to it's node, free deleted z_id instead of the spliced y_id
            if (y_id != nodeId)
            {
                leftId = Left(nodeId);
                int rightId = Right(nodeId);

                // we know that key, next and value are same for z_id and y_id
                SetLeft(y_id, leftId);
                SetRight(y_id, rightId);
                SetColor(y_id, Color(nodeId));
                SetSubTreeSize(y_id, SubTreeSize(nodeId));
                var parentNodeId = Parent(nodeId);
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

                if (leftId != NIL)
                {
                    SetParent(leftId, y_id);
                }
                if (rightId != NIL)
                {
                    SetParent(rightId, y_id);
                }

                if (root == nodeId)
                {
                    root = y_id;
                }
                else if (root_id == nodeId)
                {
                    root_id = y_id;
                }
                // update a next reference to z_id (if any)
                if (mNode != NIL && Link(mNode) == nodeId)
                {
                    SetLink(mNode, y_id);
                }
            }
            FreeNode(nodeId);
            unchecked { _version++; }
            return nodeId;
        }



        private NodePath GetNodeByKey(K key) //i.e. GetNodeByKey
        {
            int nodeId = SearchSubTree(NIL, key);
            int nextId = Link(nodeId);
            if (nextId != NIL)
            {
                return new NodePath(SearchSubTree(nextId, key), nodeId);
            }
            else if (nodeId != NIL && !Value(nodeId).Equals(key))
            {
                nodeId = NIL;
            }
            return new NodePath(nodeId, NIL);
        }

        private int SearchSubTree(int root_id, K key)
        {
            int x_id = (root_id == NIL) ? root : root_id;
            int c;
            while (x_id != NIL)
            {
                c = (root_id == NIL)
                    ? this.comparer.Compare(key, Value(x_id))
                    : this.satelliteComparer.Compare(key, Value(x_id));
                if (c == 0)
                {
                    break;
                }
                if (c < 0)
                {
                    x_id = Left(x_id);
                }
                else
                {
                    x_id = Right(x_id);
                }
            }
            return x_id;
        }

        /// <returns>Determine node and the branch it took to get there.</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        private NodePath GetNodePathByIndex(int userIndex)
        {
            int x_id, satelliteRootId;
            if (0 == _inUseSatelliteTreeCount)
            {
                // if rows were only contiguously append, then using (userIndex -= _pageTable[i].InUseCount) would
                // be faster for the first 12 pages (about 5248) nodes before (log2 of Count) becomes faster again.
                // the additional complexity was deemed not worthy for the possible perf gain

                // computation cost is (log2 of Count)
                x_id = GetNodeIdByIndex(root, unchecked(userIndex + 1));
                satelliteRootId = NIL;
            }
            else
            {
                // computation cost is ((log2 of Distinct Count) + (log2 of Duplicate Count))
                x_id = GetNodeIdByIndex(userIndex, out satelliteRootId);
            }
            if (x_id == NIL)
            {
                throw new IndexOutOfRangeException(nameof(userIndex));
            }
            return new NodePath(x_id, satelliteRootId);
        }

        private int GetNodeIdByIndex(int index, out int satelliteRootId)
        {
            index = unchecked(index + 1);
            satelliteRootId = NIL;
            int x_id = root;
            int rank;

            while (x_id != NIL)
            {
                int left_x = Left(x_id);
                int next_id = Link(x_id);
                rank = SubTreeSize(left_x) + 1;

                if (rank == index && next_id == NIL)
                {
                    break;
                }

                if (index < rank)
                {
                    x_id = left_x;
                }
                else if (next_id != NIL && index <= rank + SubTreeSize(next_id) - 1)
                {
                    satelliteRootId = x_id;
                    index = index - rank + 1;
                    return GetNodeIdByIndex(next_id, index);
                }
                else
                {
                    if (next_id == NIL)
                        index -= rank;
                    else
                        index -= rank + SubTreeSize(next_id) - 1;
                    x_id = Right(x_id);
                }
            }
            return x_id;
        }

        /// <summary>Determine tree index position from node path.</summary>
        /// <remarks>This differs from GetIndexByNode which would search for the main tree node instead of just knowing it</remarks>
        private int GetIndexByNodePath(NodePath path)
        {
            if (0 == _inUseSatelliteTreeCount)
            {   // compute from the main tree when no satellite branches exist
                return IndexOfNode(path._nodeID);
            }
            else if (NIL == path._mainTreeNodeID)
            {   // compute from the main tree accounting for satellite branches
                return ComputeIndexWithSatelliteByNode(path._nodeID);
            }
            else
            {   //compute the main tree rank + satellite branch rank
                return ComputeIndexWithSatelliteByNode(path._mainTreeNodeID) + IndexOfNode(path._nodeID);
            }
        }

        private int ComputeIndexWithSatelliteByNode(int nodeId)
        {
            int myRank = SubTreeSize(Left(nodeId));
            while (nodeId != NIL)
            {
                int parent = Parent(nodeId);
                if (nodeId == Right(parent))
                {
                    myRank += (SubTreeSize(Left(parent)) + ((Link(parent) == NIL) ? 1 : SubTreeSize(Link(parent))));
                }
                nodeId = parent;
            }
            return myRank;
        }

        private int FindNodeInMainTree(Func<K, int> compareFunc)
        {
            int x_id = root;
            while (x_id != NIL)
            {
                int c = compareFunc(Value(x_id));
                if (c == 0) break;
                x_id = (c > 0) ? Left(x_id) : Right(x_id);
            }
            return x_id;
        }

        /* Find first node where comparer returns >= 0 (i.e., node >= min boundary) */
        private int FindFirstInRange(Func<K, int> minComparer)
        {
            int x_id = root;
            int candidate = NIL;
            while (x_id != NIL)
            {
                int c = minComparer(Value(x_id));
                if (c >= 0)
                {
                    // node >= min, this is candidate
                    candidate = x_id;
                    // look for smaller candidates on the left
                    x_id = Left(x_id);
                }
                else
                {
                    // node < min, goto right
                    x_id = Right(x_id);
                }
            }
            return candidate;
        }

        private IEnumerable<K> CollectSatelliteKeys(int nodeId)
        {
            if (nodeId != NIL)
            {
                // In-order traversal
                foreach (var k in CollectSatelliteKeys(Left(nodeId)))
                {
                    yield return k;
                }
                yield return Value(nodeId);
                foreach (var k in CollectSatelliteKeys(Right(nodeId)))
                {
                    yield return k;
                }
            }
        }

        private bool SearchSuccessor(ref int nodeId, ref int mainTreeNodeId)
        {
            if (NIL == nodeId)
            {   // find first node, using branchNodeId as the root
                nodeId = Minimum(mainTreeNodeId);
                mainTreeNodeId = NIL;
            }
            else
            {   // find next node
                nodeId = SearchSuccessor(nodeId);

                if ((NIL == nodeId) && (NIL != mainTreeNodeId))
                {
                    // done with satellite branch, move back to main tree
                    nodeId = SearchSuccessor(mainTreeNodeId);
                    mainTreeNodeId = NIL;
                }
            }
            if (NIL != nodeId)
            {
                var nextId = Link(nodeId);
                // test for satellite branch
                if (NIL != nextId)
                {
                    // find first node of satellite branch
                    if (NIL != mainTreeNodeId)
                    {
                        // satellite branch has satellite branch - very bad
                        throw new InvalidOperationException("Internal RBTree error in SearchSuccessor. Satellite should not have nested satellite.");
                    }
                    mainTreeNodeId = nodeId;
                    nodeId = Minimum(nextId);
                }
                // has value
                return true;
            }
            // else no value, done with main tree
            return false;
        }
        #endregion

        #region inner types
        /// <summary>Represents the node in the tree and the satellite branch it took to get there.</summary>
        protected readonly struct NodePath
        {
            /// <summary>Represents the node in the tree</summary>
            public readonly int _nodeID;

            /// <summary>
            /// When not NIL, it represents the fact NodeID is has duplicate values in the tree.
            /// This is the 'fake' node in the main tree that redirects to the root of the satellite tree.
            /// By tracking this value, we don't have to repeatedly search for this node.
            /// </summary>
            public readonly int _mainTreeNodeID;

            public NodePath(int nodeID, int mainTreeNodeID)
            {
                _nodeID = nodeID;
                _mainTreeNodeID = mainTreeNodeID;
            }
        }

        // this improves performance allowing to iterating of the index instead of computing record by index
        // changes are required to handle satellite nodes which do not exist in DataRowCollection
        // enumerator over index will not be handed to the user, only used internally

        // instance of this enumerator will be handed to the user via DataRowCollection.GetEnumerator()
        private struct RBTreeEnumerator : IEnumerator<K>, IEnumerator
        {
            private readonly RedBlackTreeSetBase<K> _tree;
            private readonly int _version;
            private int _index, _mainTreeNodeId;
            private K _current;

            internal RBTreeEnumerator(RedBlackTreeSetBase<K> tree)
            {
                _tree = tree;
                _version = tree._version;
                _index = NIL;
                _mainTreeNodeId = tree.root;
                _current = default;
            }

            internal RBTreeEnumerator(RedBlackTreeSetBase<K> tree, int position)
            {
                _tree = tree;
                _version = tree._version;
                if (0 == position)
                {
                    _index = NIL;
                    _mainTreeNodeId = tree.root;
                }
                else
                {
                    _index = tree.GetNodeIdByIndex(position - 1, out _mainTreeNodeId);
                    if (NIL == _index)
                    {
                        throw new IndexOutOfRangeException(nameof(position));
                    }
                }
                _current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_version != _tree._version)
                {
                    throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
                }

                bool hasCurrent = _tree.SearchSuccessor(ref _index, ref _mainTreeNodeId);
                _current = hasCurrent ? _tree.Value(_index) : default;
                return hasCurrent;
            }

            public K Current
            {
                get
                {
                    // TODO: Should throw if MoveNext hasn't been called
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

                _index = NIL;
                _mainTreeNodeId = _tree.root;
                _current = default;
            }
        }
        [Serializable]
        public sealed class HashCodeComparer : IComparer<K>
        {            
            public HashCodeComparer() { }

            public int Compare(K x, K y)
            {
                return (object.Equals(x, null) ? 0 : x.GetHashCode()).CompareTo(object.Equals(y, null) ? 0 : y.GetHashCode());
            }
        }
        #endregion
    }
}
