namespace JRC.Collections.RedBlackTree.Tests
{
    using MessagePack;
    using MessagePack.Resolvers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using ProtoBuf;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Xml.Serialization;

    [TestClass]
    public class RedBlackTreeListTests
    {
        public TestContext TestContext { get; set; }

        // Helper class pour les tests
        [Serializable]
        [DataContract]
        [ProtoContract]
        public class TestItem : IRedBlackTreeListItem
        {
            [DataMember]
            [ProtoMember(1)]
            public int Value { get; set; }
            [XmlIgnore]
            [System.Text.Json.Serialization.JsonIgnore]

            public int NodeId { get; set; }

            public TestItem() { }

            public TestItem(int value)
            {
                Value = value;
                NodeId = RedBlackTreeBase<TestItem>.NIL;
            }

            public override string ToString() => $"Item({Value}, NodeId={NodeId})";
        }

        // Fixture - liste réinitialisée avant chaque test
        private RedBlackTreeList<TestItem> tree;

        [TestInitialize]
        public void TestSetup()
        {
            tree = new RedBlackTreeList<TestItem>();
        }

        [TestCleanup]
        public void TestTeardown()
        {
            tree?.Clear();
            tree = null;
        }

        #region Basic Operations

        [TestMethod]
        public void Add_SingleItem_CountIsOne()
        {
            var item = new TestItem(42);

            tree.Add(item);

            Assert.AreEqual(1, tree.Count);
            Assert.AreNotEqual(RedBlackTreeBase<TestItem>.NIL, item.NodeId);
        }

        [TestMethod]
        public void Add_MultipleItems_CountMatches()
        {
            for (int i = 0; i < 10; i++)
            {
                tree.Add(new TestItem(i));
            }

            Assert.AreEqual(10, tree.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Add_ItemWithExistingNodeId_ThrowsArgumentException()
        {
            var item = new TestItem(42);
            tree.Add(item);
            tree.Add(item); // Should throw
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_NullItem_ThrowsArgumentNullException()
        {
            tree.Add(null);
        }

        #endregion

        #region Indexer Get

        [TestMethod]
        public void Indexer_Get_ReturnsCorrectItem()
        {
            var items = Enumerable.Range(0, 5).Select(i => new TestItem(i * 10)).ToList();

            foreach (var item in items)
                tree.Add(item);

            for (int i = 0; i < items.Count; i++)
            {
                Assert.AreEqual(items[i].Value, tree[i].Value);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void Indexer_Get_OutOfRangePositive_ThrowsIndexOutOfRangeException()
        {
            tree.Add(new TestItem(1));
            var x = tree[5]; // Should throw
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void Indexer_Get_OutOfRangeNegative_ThrowsIndexOutOfRangeException()
        {
            tree.Add(new TestItem(1));
            var x = tree[-1]; // Should throw
        }

        [TestMethod]
        public void Indexer_Get_EmptyList_ThrowsIndexOutOfRangeException()
        {
            try
            {
                var x = tree[0];
                Assert.Fail("Should have thrown IndexOutOfRangeException");
            }
            catch (IndexOutOfRangeException)
            {
                // Expected
            }
        }

        #endregion

        #region Indexer Set

        [TestMethod]
        public void Indexer_Set_ReplacesItem()
        {
            var oldItem = new TestItem(100);
            var newItem = new TestItem(200);

            tree.Add(oldItem);
            tree[0] = newItem;

            Assert.AreEqual(200, tree[0].Value);
            Assert.AreNotEqual(RedBlackTreeBase<TestItem>.NIL, newItem.NodeId);
            Assert.AreEqual(RedBlackTreeBase<TestItem>.NIL, oldItem.NodeId); // Old item cleaned
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Indexer_Set_ItemWithExistingNodeId_ThrowsArgumentException()
        {
            var item1 = new TestItem(1);
            var item2 = new TestItem(2);

            tree.Add(item1);
            tree.Add(item2);

            tree[0] = item2; // Should throw
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Indexer_Set_NullItem_ThrowsArgumentNullException()
        {
            tree.Add(new TestItem(1));
            tree[0] = null; // Should throw
        }

        #endregion

        #region Insert

        [TestMethod]
        public void Insert_AtBeginning_ShiftsItems()
        {
            tree.Add(new TestItem(10));
            tree.Add(new TestItem(20));
            tree.Add(new TestItem(30));

            var newItem = new TestItem(5);
            tree.Insert(0, newItem);

            Assert.AreEqual(4, tree.Count);
            Assert.AreEqual(5, tree[0].Value);
            Assert.AreEqual(10, tree[1].Value);
            Assert.AreEqual(20, tree[2].Value);
            Assert.AreEqual(30, tree[3].Value);
        }

        [TestMethod]
        public void Insert_InMiddle_MaintainsOrder()
        {
            tree.Add(new TestItem(10));
            tree.Add(new TestItem(30));

            tree.Insert(1, new TestItem(20));

            Assert.AreEqual(10, tree[0].Value);
            Assert.AreEqual(20, tree[1].Value);
            Assert.AreEqual(30, tree[2].Value);
        }

        [TestMethod]
        public void Insert_AtEnd_AddsToEnd()
        {
            tree.Add(new TestItem(10));
            tree.Add(new TestItem(20));

            tree.Insert(2, new TestItem(30));

            Assert.AreEqual(3, tree.Count);
            Assert.AreEqual(30, tree[2].Value);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Insert_ItemWithExistingNodeId_ThrowsArgumentException()
        {
            var item = new TestItem(42);
            tree.Add(item);
            tree.Insert(0, item); // Should throw
        }

        #endregion

        #region Remove / RemoveAt

        [TestMethod]
        public void Remove_ExistingItem_RemovesAndReturnsTrue()
        {
            var item = new TestItem(42);
            tree.Add(item);

            bool removed = tree.Remove(item);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, tree.Count);
        }

        [TestMethod]
        public void Remove_NonExistentItem_ReturnsFalse()
        {
            var item = new TestItem(42);

            bool removed = tree.Remove(item);

            Assert.IsFalse(removed);
        }

        [TestMethod]
        public void Remove_NullItem_ThrowsArgumentNullException()
        {
            try
            {
                tree.Remove(null);
                Assert.Fail("Should have thrown ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }

        [TestMethod]
        public void RemoveAt_ValidIndex_RemovesItem()
        {
            tree.Add(new TestItem(10));
            tree.Add(new TestItem(20));
            tree.Add(new TestItem(30));

            tree.RemoveAt(1);

            Assert.AreEqual(2, tree.Count);
            Assert.AreEqual(10, tree[0].Value);
            Assert.AreEqual(30, tree[1].Value);
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void RemoveAt_InvalidIndex_ThrowsIndexOutOfRangeException()
        {
            tree.Add(new TestItem(1));
            tree.RemoveAt(5); // Should throw
        }

        [TestMethod]
        public void RemoveAt_FirstItem_UpdatesStructure()
        {
            tree.Add(new TestItem(10));
            tree.Add(new TestItem(20));
            tree.Add(new TestItem(30));

            tree.RemoveAt(0);

            Assert.AreEqual(2, tree.Count);
            Assert.AreEqual(20, tree[0].Value);
            Assert.AreEqual(30, tree[1].Value);
        }

        [TestMethod]
        public void RemoveAt_LastItem_UpdatesStructure()
        {
            tree.Add(new TestItem(10));
            tree.Add(new TestItem(20));
            tree.Add(new TestItem(30));

            tree.RemoveAt(2);

            Assert.AreEqual(2, tree.Count);
            Assert.AreEqual(10, tree[0].Value);
            Assert.AreEqual(20, tree[1].Value);
        }

        #endregion

        #region Swap

        [TestMethod]
        public void Swap_TwoPositions_SwapsItemsAndNodeIds()
        {
            var item0 = new TestItem(10);
            var item1 = new TestItem(20);
            var item2 = new TestItem(30);

            tree.Add(item0);
            tree.Add(item1);
            tree.Add(item2);

            int nodeId0Before = item0.NodeId;
            int nodeId1Before = item1.NodeId;

            tree.Swap(0, 1);

            Assert.AreEqual(20, tree[0].Value);
            Assert.AreEqual(10, tree[1].Value);
            Assert.AreEqual(30, tree[2].Value);

            // NodeIds are swapped
            Assert.AreEqual(nodeId1Before, item0.NodeId);  // item0 now has item1's old NodeId
            Assert.AreEqual(nodeId0Before, item1.NodeId);  // item1 now has item0's old NodeId
        }

        [TestMethod]
        public void Swap_AdjacentPositions_Works()
        {
            tree.Add(new TestItem(10));
            tree.Add(new TestItem(20));

            tree.Swap(0, 1);

            Assert.AreEqual(20, tree[0].Value);
            Assert.AreEqual(10, tree[1].Value);
        }

        [TestMethod]
        public void Swap_SamePosition_DoesNothing()
        {
            var item = new TestItem(42);
            tree.Add(item);

            int nodeIdBefore = item.NodeId;

            tree.Swap(0, 0);

            Assert.AreEqual(42, tree[0].Value);
            Assert.AreEqual(nodeIdBefore, item.NodeId);
        }

        #endregion

        #region Contains / IndexOf

        [TestMethod]
        public void Contains_ExistingItem_ReturnsTrue()
        {
            var item = new TestItem(42);
            tree.Add(item);

            Assert.IsTrue(tree.Contains(item));
        }

        [TestMethod]
        public void Contains_NonExistentItem_ReturnsFalse()
        {
            var item = new TestItem(42);

            Assert.IsFalse(tree.Contains(item));
        }

        [TestMethod]
        public void Contains_NullItem_ReturnsFalse()
        {
            Assert.IsFalse(tree.Contains(null));
        }

        [TestMethod]
        public void IndexOf_ExistingItem_ReturnsCorrectIndex()
        {
            var item0 = new TestItem(10);
            var item1 = new TestItem(20);
            var item2 = new TestItem(30);

            tree.Add(item0);
            tree.Add(item1);
            tree.Add(item2);

            Assert.AreEqual(0, tree.IndexOf(item0));
            Assert.AreEqual(1, tree.IndexOf(item1));
            Assert.AreEqual(2, tree.IndexOf(item2));
        }

        [TestMethod]
        public void IndexOf_NonExistentItem_ReturnsMinusOne()
        {
            var item = new TestItem(42);

            Assert.AreEqual(-1, tree.IndexOf(item));
        }

        [TestMethod]
        public void IndexOf_NullItem_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, tree.IndexOf(null));
        }

        [TestMethod]
        public void IndexOf_AfterRemoval_UpdatesCorrectly()
        {
            var item0 = new TestItem(10);
            var item1 = new TestItem(20);
            var item2 = new TestItem(30);

            tree.Add(item0);
            tree.Add(item1);
            tree.Add(item2);

            tree.RemoveAt(0);

            Assert.AreEqual(0, tree.IndexOf(item1));  // item1 moved to position 0
            Assert.AreEqual(1, tree.IndexOf(item2));  // item2 moved to position 1
            Assert.AreEqual(-1, tree.IndexOf(item0)); // item0 removed
        }

        #endregion

        #region Clear

        [TestMethod]
        public void Clear_RemovesAllItems()
        {
            for (int i = 0; i < 10; i++)
                tree.Add(new TestItem(i));

            tree.Clear();

            Assert.AreEqual(0, tree.Count);
        }

        [TestMethod]
        public void Clear_EmptyList_DoesNotThrow()
        {
            tree.Clear(); // Should not throw

            Assert.AreEqual(0, tree.Count);
        }

        [TestMethod]
        public void Clear_AllowsReuse()
        {
            tree.Add(new TestItem(1));
            tree.Clear();
            tree.Add(new TestItem(2));

            Assert.AreEqual(1, tree.Count);
            Assert.AreEqual(2, tree[0].Value);
        }

        #endregion

        #region Enumeration

        [TestMethod]
        public void GetEnumerator_IteratesInOrder()
        {
            var expected = new[] { 10, 20, 30, 40, 50 };

            foreach (var val in expected)
                tree.Add(new TestItem(val));

            var actual = tree.Select(item => item.Value).ToArray();

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Enumeration_AfterAdd_ThrowsInvalidOperationException()
        {
            tree.Add(new TestItem(1));
            tree.Add(new TestItem(2));

            var enumerator = tree.GetEnumerator();
            enumerator.MoveNext();

            tree.Add(new TestItem(3));  // Modify during enumeration

            enumerator.MoveNext(); // Should throw
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Enumeration_AfterRemove_ThrowsInvalidOperationException()
        {
            tree.Add(new TestItem(1));
            tree.Add(new TestItem(2));

            var enumerator = tree.GetEnumerator();
            enumerator.MoveNext();

            tree.RemoveAt(0);  // Modify during enumeration

            enumerator.MoveNext(); // Should throw
        }

        [TestMethod]
        public void Enumeration_EmptyList_DoesNotIterate()
        {
            int count = 0;
            foreach (var item in tree)
                count++;

            Assert.AreEqual(0, count);
        }

        #endregion

        #region CopyTo

        [TestMethod]
        public void CopyTo_CopiesToArray()
        {
            tree.Add(new TestItem(10));
            tree.Add(new TestItem(20));
            tree.Add(new TestItem(30));

            var array = new TestItem[5];
            tree.CopyTo(array, 1);

            Assert.IsNull(array[0]);
            Assert.AreEqual(10, array[1].Value);
            Assert.AreEqual(20, array[2].Value);
            Assert.AreEqual(30, array[3].Value);
            Assert.IsNull(array[4]);
        }

        [TestMethod]
        public void CopyTo_AtIndexZero_CopiesToStart()
        {
            tree.Add(new TestItem(10));
            tree.Add(new TestItem(20));

            var array = new TestItem[3];
            tree.CopyTo(array, 0);

            Assert.AreEqual(10, array[0].Value);
            Assert.AreEqual(20, array[1].Value);
            Assert.IsNull(array[2]);
        }

        #endregion

        #region Stress Tests

        [TestMethod]
        public void StressTest_AddAndRemove_1000Items()
        {
            var items = Enumerable.Range(0, 1000).Select(i => new TestItem(i)).ToList();

            // Add all
            foreach (var item in items)
                tree.Add(item);

            Assert.AreEqual(1000, tree.Count);

            // Verify order
            for (int i = 0; i < 1000; i++)
                Assert.AreEqual(i, tree[i].Value);

            // Remove every other item
            for (int i = 999; i >= 0; i -= 2)
                tree.RemoveAt(i);

            Assert.AreEqual(500, tree.Count);

            // Verify remaining items
            for (int i = 0; i < 500; i++)
                Assert.AreEqual(i * 2, tree[i].Value);
        }

        [TestMethod]
        public void StressTest_InsertAtRandomPositions()
        {
            var random = new Random(42);  // Fixed seed for reproducibility

            for (int i = 0; i < 100; i++)
            {
                int position = random.Next(0, tree.Count + 1);
                tree.Insert(position, new TestItem(i));
            }

            Assert.AreEqual(100, tree.Count);

            // Verify all items are accessible
            for (int i = 0; i < 100; i++)
            {
                var item = tree[i];
                Assert.AreNotEqual(RedBlackTreeBase<TestItem>.NIL, item.NodeId);
            }
        }

        [TestMethod]
        public void StressTest_MixedOperations()
        {
            // Add 100 items
            for (int i = 0; i < 100; i++)
                tree.Add(new TestItem(i));

            // Remove 50 items from various positions
            for (int i = 0; i < 50; i++)
                tree.RemoveAt(i % tree.Count);

            // Insert 25 items at random positions
            var random = new Random(42);
            for (int i = 0; i < 25; i++)
            {
                int pos = random.Next(0, tree.Count + 1);
                tree.Insert(pos, new TestItem(1000 + i));
            }

            Assert.AreEqual(75, tree.Count);

            // Verify all items are valid
            foreach (var item in tree)
            {
                Assert.AreNotEqual(RedBlackTreeBase<TestItem>.NIL, item.NodeId);
            }
        }

        #endregion

        #region Chaos & Edge Case Tests

        [TestMethod]
        public void ChaosTest_MassiveInsertUpdateSwapRemove()
        {
            var random = new Random(42);
            var items = new List<TestItem>();

            // Phase 1: Insert 500 items
            for (int i = 0; i < 500; i++)
            {
                var item = new TestItem(i);
                tree.Add(item);
                items.Add(item);
            }

            Assert.AreEqual(500, tree.Count);

            // Phase 2: 200 random swaps
            for (int i = 0; i < 200; i++)
            {
                int pos1 = random.Next(0, tree.Count);
                int pos2 = random.Next(0, tree.Count);
                tree.Swap(pos1, pos2);
            }

            Assert.AreEqual(500, tree.Count);

            // Phase 3: 100 random updates (replace)
            for (int i = 0; i < 100; i++)
            {
                int pos = random.Next(0, tree.Count);
                var newItem = new TestItem(1000 + i);
                tree[pos] = newItem;
            }

            Assert.AreEqual(500, tree.Count);

            // Phase 4: 100 random inserts
            for (int i = 0; i < 100; i++)
            {
                int pos = random.Next(0, tree.Count + 1);
                tree.Insert(pos, new TestItem(2000 + i));
            }

            Assert.AreEqual(600, tree.Count);

            // Phase 5: Remove 300 items from random positions
            for (int i = 0; i < 300; i++)
            {
                int pos = random.Next(0, tree.Count);
                tree.RemoveAt(pos);
            }

            Assert.AreEqual(300, tree.Count);

            // Phase 6: CRITICAL - Enumerate to verify successor chain integrity
            int enumeratedCount = 0;
            var visitedNodeIds = new HashSet<int>();

            foreach (var item in tree)
            {
                enumeratedCount++;

                // Detect infinite loop
                if (enumeratedCount > 1000)
                {
                    Assert.Fail("Enumeration infinite loop detected!");
                }

                // Detect cycle in successor chain
                if (!visitedNodeIds.Add(item.NodeId))
                {
                    Assert.Fail($"Cycle detected in successor chain! NodeId {item.NodeId} visited twice");
                }

                Assert.AreNotEqual(RedBlackTreeBase<TestItem>.NIL, item.NodeId);
            }

            Assert.AreEqual(300, enumeratedCount, "Enumeration count mismatch");
        }

        [TestMethod]
        public void EdgeCase_RemoveAllItemsOneByOne_FromEnd()
        {
            // Add 100 items
            for (int i = 0; i < 100; i++)
                tree.Add(new TestItem(i));

            // Remove all from end
            for (int i = 99; i >= 0; i--)
            {
                tree.RemoveAt(i);

                // Verify enumeration still works
                int count = 0;
                foreach (var item in tree)
                    count++;

                Assert.AreEqual(i, count, $"Enumeration failed after removing item at position {i}");
            }

            Assert.AreEqual(0, tree.Count);
        }

        [TestMethod]
        public void EdgeCase_RemoveAllItemsOneByOne_FromStart()
        {
            // Add 100 items
            for (int i = 0; i < 100; i++)
                tree.Add(new TestItem(i));

            // Remove all from start
            while (tree.Count > 0)
            {
                int countBefore = tree.Count;
                tree.RemoveAt(0);

                // Verify enumeration still works
                int enumeratedCount = 0;
                foreach (var item in tree)
                {
                    enumeratedCount++;
                    if (enumeratedCount > countBefore)
                        Assert.Fail("Infinite loop in enumeration!");
                }

                Assert.AreEqual(countBefore - 1, enumeratedCount);
            }

            Assert.AreEqual(0, tree.Count);
        }

        [TestMethod]
        public void EdgeCase_AlternatingInsertRemove()
        {
            for (int cycle = 0; cycle < 50; cycle++)
            {
                // Add 10 items
                for (int i = 0; i < 10; i++)
                    tree.Add(new TestItem(cycle * 100 + i));

                // Remove 5 items
                for (int i = 0; i < 5; i++)
                    tree.RemoveAt(tree.Count / 2);

                // Verify enumeration
                int count = 0;
                foreach (var item in tree)
                {
                    count++;
                    if (count > 1000)
                        Assert.Fail("Enumeration infinite loop!");
                }

                Assert.AreEqual(tree.Count, count);
            }
        }

        [TestMethod]
        public void EdgeCase_SwapFirstAndLast_Repeatedly()
        {
            for (int i = 0; i < 20; i++)
                tree.Add(new TestItem(i));

            // Swap first and last 50 times
            for (int i = 0; i < 50; i++)
            {
                tree.Swap(0, tree.Count - 1);

                // Verify enumeration integrity
                int count = 0;
                foreach (var item in tree)
                {
                    count++;
                    if (count > 100)
                        Assert.Fail("Infinite loop after swap!");
                }

                Assert.AreEqual(20, count);
            }
        }

        [TestMethod]
        public void EdgeCase_UpdateSamePositionRepeatedly()
        {
            tree.Add(new TestItem(1));
            tree.Add(new TestItem(2));
            tree.Add(new TestItem(3));

            // Update middle position 100 times
            for (int i = 0; i < 100; i++)
            {
                var newItem = new TestItem(1000 + i);
                tree[1] = newItem;

                // Verify enumeration
                var values = tree.Select(x => x.Value).ToArray();
                Assert.AreEqual(3, values.Length);
            }
        }

        [TestMethod]
        public void EdgeCase_InsertAtSamePositionRepeatedly()
        {
            tree.Add(new TestItem(1));
            tree.Add(new TestItem(2));

            // Insert at position 1 repeatedly
            for (int i = 0; i < 50; i++)
            {
                tree.Insert(1, new TestItem(100 + i));
            }

            Assert.AreEqual(52, tree.Count);

            // Verify enumeration
            int count = 0;
            foreach (var item in tree)
            {
                count++;
                if (count > 100)
                    Assert.Fail("Infinite loop!");
            }

            Assert.AreEqual(52, count);
        }

        [TestMethod]
        public void EdgeCase_RemoveAndReAddSameItem()
        {
            var item1 = new TestItem(10);
            var item2 = new TestItem(20);
            var item3 = new TestItem(30);

            tree.Add(item1);
            tree.Add(item2);
            tree.Add(item3);

            // Remove middle
            tree.Remove(item2);
            Assert.AreEqual(RedBlackTreeBase<TestItem>.NIL, item2.NodeId);

            // Re-add
            tree.Add(item2);
            Assert.AreNotEqual(RedBlackTreeBase<TestItem>.NIL, item2.NodeId);

            // Verify enumeration
            var values = tree.Select(x => x.Value).ToArray();
            CollectionAssert.AreEqual(new[] { 10, 30, 20 }, values);
        }

        [TestMethod]
        public void EnumerationIntegrity_AfterMassOperations()
        {
            var random = new Random(123);

            // Build up to 200 items with random operations
            for (int op = 0; op < 500; op++)
            {
                int action = random.Next(0, 4);

                if (tree.Count == 0 || action == 0)
                {
                    // Add
                    int pos = tree.Count == 0 ? 0 : random.Next(0, tree.Count + 1);
                    tree.Insert(pos, new TestItem(op));
                }
                else if (action == 1 && tree.Count > 0)
                {
                    // Remove
                    int pos = random.Next(0, tree.Count);
                    tree.RemoveAt(pos);
                }
                else if (action == 2 && tree.Count > 1)
                {
                    // Swap
                    int pos1 = random.Next(0, tree.Count);
                    int pos2 = random.Next(0, tree.Count);
                    tree.Swap(pos1, pos2);
                }
                else if (action == 3 && tree.Count > 0)
                {
                    // Update
                    int pos = random.Next(0, tree.Count);
                    tree[pos] = new TestItem(10000 + op);
                }

                // Every 50 operations, verify full enumeration
                if (op % 50 == 0)
                {
                    int count = 0;
                    var visitedIds = new HashSet<int>();

                    foreach (var item in tree)
                    {
                        count++;

                        if (count > 10000)
                            Assert.Fail($"Infinite loop detected at operation {op}");

                        if (!visitedIds.Add(item.NodeId))
                            Assert.Fail($"Cycle detected at operation {op}, NodeId {item.NodeId}");
                    }

                    Assert.AreEqual(tree.Count, count, $"Count mismatch at operation {op}");
                }
            }
        }

        #endregion

        #region SpeedTest
        [TestMethod]
        public void SpeedTest()
        {
            var list = new List<TestItem>();

            double listElapsed, treeElapsed;
            var sb = new StringBuilder();
            sb.AppendLine("==================================================================");
            sb.AppendLine("|                    |       List          |        Tree         |");
            sb.AppendLine("==================================================================");

            // ============================================================
            // Add
            // ============================================================
            const int AddCount = 100000;
            var watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < AddCount; i++)
            {
                tree.Add(new TestItem(i));
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < AddCount; i++)
            {
                list.Add(new TestItem(i));
            }
            watch.Stop();
            listElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{$" add (x{AddCount})",-20}| {listElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // Insert at beginning
            // ============================================================
            const int InsertCount = 20000;
            tree.Clear();
            list.Clear();

            watch.Reset();
            watch.Start();
            for (int i = 0; i < InsertCount; i++)
            {
                tree.Insert(0, new TestItem(i));
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < InsertCount; i++)
            {
                list.Insert(0, new TestItem(i));
            }
            watch.Stop();
            listElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{$" insert(0) (x{InsertCount})",-20}| {listElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // IndexOf
            // ============================================================
            tree.Clear();
            list.Clear();
            var searchItems = new List<TestItem>();
            for (int i = 0; i < 1000; i++)
            {
                var item = new TestItem(i);
                tree.Add(item);

                list.Add(item);
                if (i % 10 == 0)
                    searchItems.Add(item);
            }

            const int IndexOfCount = 10000;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < IndexOfCount; i++)
            {
                var item = searchItems[i % searchItems.Count];
                var idx = tree.IndexOf(item);
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < IndexOfCount; i++)
            {
                var item = searchItems[i % searchItems.Count];
                var idx = list.IndexOf(item);
            }
            watch.Stop();
            listElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{$" indexOf (x{IndexOfCount})",-20}| {listElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // Random access this[index]
            // ============================================================
            tree.Clear();
            list.Clear();
            for (int i = 0; i < 50000; i++)
            {
                tree.Add(new TestItem(i));
                list.Add(new TestItem(i));
            }

            const int AccessCount = 100000;
            var random = new Random(42);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < AccessCount; i++)
            {
                int idx = random.Next(0, tree.Count);
                var val = tree[idx].Value;
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;


            random = new Random(42);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < AccessCount; i++)
            {
                int idx = random.Next(0, list.Count);
                var val = list[idx].Value;
            }
            watch.Stop();
            listElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{$" this[i] (x{AccessCount})",-20}| {listElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");



            // ============================================================
            // Remove by item
            // ============================================================
            tree.Clear();
            list.Clear();
            const int RemoveCount = 100000;
            var itemsToRemove = new List<TestItem>();
            for (int i = 0; i < RemoveCount; i++)
            {
                var item = new TestItem(i);
                tree.Add(item);
                list.Add(item);
                itemsToRemove.Add(item);
            }

            watch.Reset();
            watch.Start();
            for (int i = 0; i < RemoveCount && i < itemsToRemove.Count; i++)
            {
                tree.Remove(itemsToRemove[i]);
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < RemoveCount && i < itemsToRemove.Count; i++)
            {
                list.Remove(itemsToRemove[i]);
            }
            watch.Stop();
            listElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{$" remove (x{RemoveCount})",-20}| {listElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // RemoveAt middle
            // ============================================================
            tree.Clear();
            list.Clear();
            const int RemoveAtCount = 100000;

            for (int i = 0; i < RemoveAtCount; i++)
            {
                tree.Add(new TestItem(i));
                list.Add(new TestItem(i));
            }
            watch.Reset();
            watch.Start();
            for (int i = 0; i < RemoveAtCount && tree.Count > 0; i++)
            {
                tree.RemoveAt(0);
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < RemoveAtCount && list.Count > 0; i++)
            {
                list.RemoveAt(0);
            }
            watch.Stop();
            listElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{$" removeAt (x{RemoveAtCount})",-20}| {listElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // Enumerate
            // ============================================================
            tree.Clear();
            list.Clear();
            for (int i = 0; i < 100000; i++)
            {
                tree.Add(new TestItem(i));
                list.Add(new TestItem(i));
            }

            watch.Reset();
            watch.Start();
            long sum = 0;
            foreach (var item in tree)
            {
                sum += item.Value;
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            sum = 0;
            foreach (var item in list)
            {
                sum += item.Value;
            }
            watch.Stop();
            listElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{$" enumerate (x100000)",-20}| {listElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("==================================================================");

            this.TestContext.Write(sb.ToString());

        }
        #endregion

        #region Serialize
        [TestMethod]
        public void SerializeBinaryTest()
        {
            var list = new RedBlackTreeList<TestItem>();
            list.Add(new TestItem(1));
            list.Add(new TestItem(2));
            list.Add(new TestItem(3));
            list.Add(new TestItem(4));

            var formatter = new BinaryFormatter { AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple };
            using(var mem = new MemoryStream())
            {
                formatter.Serialize(mem, list);
                mem.Position = 0;
                var clone = (RedBlackTreeList<TestItem>)formatter.Deserialize(mem);

                Assert.AreEqual(list.Count, clone.Count);
                Assert.AreEqual(list[0].Value, clone[0].Value);
                Assert.AreEqual(list[0].NodeId, clone[0].NodeId);
                Assert.AreEqual(list[1].Value, clone[1].Value);
                Assert.AreEqual(list[1].NodeId, clone[1].NodeId);
                Assert.AreEqual(list[2].Value, clone[2].Value);
                Assert.AreEqual(list[2].NodeId, clone[2].NodeId);
                Assert.AreEqual(list[3].Value, clone[3].Value);
                Assert.AreEqual(list[3].NodeId, clone[3].NodeId);

            }
        }
        [TestMethod]
        public void SerializeXmlTest()
        {
            var list = new RedBlackTreeList<TestItem>();
            list.Add(new TestItem(1));
            list.Add(new TestItem(2));
            list.Add(new TestItem(3));
            list.Add(new TestItem(4));

            var serializer = new XmlSerializer(typeof(RedBlackTreeList<TestItem>));
            using (var mem = new MemoryStream())
            {
                serializer.Serialize(mem, list);
                mem.Position = 0;
                var clone = (RedBlackTreeList<TestItem>)serializer.Deserialize(mem);

                Assert.AreEqual(list.Count, clone.Count);
                Assert.AreEqual(list[0].Value, clone[0].Value);                
                Assert.AreEqual(list[1].Value, clone[1].Value);
                Assert.AreEqual(list[2].Value, clone[2].Value);
                Assert.AreEqual(list[3].Value, clone[3].Value);

            }
        }

        [TestMethod]
        public void SerializeDataContractTest()
        {
            var list = new RedBlackTreeList<TestItem>();
            list.Add(new TestItem(1));
            list.Add(new TestItem(2));
            list.Add(new TestItem(3));
            list.Add(new TestItem(4));

            var serializer = new DataContractSerializer(typeof(RedBlackTreeList<TestItem>));
            using (var mem = new MemoryStream())
            {
                serializer.WriteObject(mem, list);
                mem.Position = 0;
                var clone = (RedBlackTreeList<TestItem>)serializer.ReadObject(mem);

                Assert.AreEqual(list.Count, clone.Count);
                Assert.AreEqual(list[0].Value, clone[0].Value);
                Assert.AreEqual(list[1].Value, clone[1].Value);
                Assert.AreEqual(list[2].Value, clone[2].Value);
                Assert.AreEqual(list[3].Value, clone[3].Value);

            }
        }

        [TestMethod]
        public void SerializeJsonTextTest()
        {
            var list = new RedBlackTreeList<TestItem>();
            list.Add(new TestItem(1));
            list.Add(new TestItem(2));
            list.Add(new TestItem(3));
            list.Add(new TestItem(4));

            var json = System.Text.Json.JsonSerializer.Serialize(list);
            var clone = System.Text.Json.JsonSerializer.Deserialize<RedBlackTreeList<TestItem>>(json);

            Assert.AreEqual(list.Count, clone.Count);
            Assert.AreEqual(list[0].Value, clone[0].Value);
            Assert.AreEqual(list[1].Value, clone[1].Value);
            Assert.AreEqual(list[2].Value, clone[2].Value);
            Assert.AreEqual(list[3].Value, clone[3].Value);
        }

        [TestMethod]
        public void SerializeJsonNewtonTest()
        {
            var list = new RedBlackTreeList<TestItem>();
            list.Add(new TestItem(1));
            list.Add(new TestItem(2));
            list.Add(new TestItem(3));
            list.Add(new TestItem(4));

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(list);
            var clone = System.Text.Json.JsonSerializer.Deserialize<RedBlackTreeList<TestItem>>(json);

            Assert.AreEqual(list.Count, clone.Count);
            Assert.AreEqual(list[0].Value, clone[0].Value);
            Assert.AreEqual(list[1].Value, clone[1].Value);
            Assert.AreEqual(list[2].Value, clone[2].Value);
            Assert.AreEqual(list[3].Value, clone[3].Value);
        }

        [TestMethod]
        public void SerializeMessagePackTest()
        {
            var list = new RedBlackTreeList<TestItem>();
            list.Add(new TestItem(1));
            list.Add(new TestItem(2));
            list.Add(new TestItem(3));
            list.Add(new TestItem(4));

            var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
            var bytes = MessagePackSerializer.Serialize(list);
            var clone = MessagePackSerializer.Deserialize<RedBlackTreeList<TestItem>>(bytes);

            Assert.AreEqual(list.Count, clone.Count);
            Assert.AreEqual(list[0].Value, clone[0].Value);
            Assert.AreEqual(list[1].Value, clone[1].Value);
            Assert.AreEqual(list[2].Value, clone[2].Value);
            Assert.AreEqual(list[3].Value, clone[3].Value);
        }

        [TestMethod]
        public void SerializeProtobuf()
        {
            var list = new RedBlackTreeList<TestItem>();
            list.Add(new TestItem(1));
            list.Add(new TestItem(2));
            list.Add(new TestItem(3));
            list.Add(new TestItem(4));

            RedBlackTreeList<TestItem> clone;
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, list);
                mem.Position = 0;
                clone = ProtoBuf.Serializer.Deserialize<RedBlackTreeList<TestItem>>(mem);
            }

            Assert.AreEqual(list.Count, clone.Count);
            Assert.AreEqual(list[0].Value, clone[0].Value);
            Assert.AreEqual(list[1].Value, clone[1].Value);
            Assert.AreEqual(list[2].Value, clone[2].Value);
            Assert.AreEqual(list[3].Value, clone[3].Value);
        }
        #endregion
    }
}