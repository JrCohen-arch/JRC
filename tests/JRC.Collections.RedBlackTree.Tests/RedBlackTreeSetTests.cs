using JRC.Collections.RedBlackTree;
using JRC.Collections.RedBlackTree.Tests.Serialization.Protobuf;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;

namespace JRC.Collections.RedBlackTree.Tests
{
    [TestClass]
    public class RedBlackTreeSetTests
    {
        public TestContext TestContext { get; set; }

        #region Test Classes
        [Serializable]
        [DataContract]
        [ProtoContract]
        public class Person : IComparable<Person>
        {
            [DataMember]
            [ProtoMember(1)]
            public string Name { get; set; }
            [DataMember]
            [ProtoMember(2)]
            public int Age { get; set; }

            public Person()
            {

            }

            public Person(string name, int age)
            {
                Name = name;
                Age = age;
            }

            public override string ToString() => $"{Name} ({Age})";

            int IComparable<Person>.CompareTo(Person other)
            {
                return StringComparer.Ordinal.Compare(Name, other?.Name);
            }
        }
        #endregion

        #region Constructor Tests
        [TestMethod]
        public void Constructor_WithComparison_ShouldCreateEmptyTree()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            Assert.AreEqual(0, set.Count);
        }

        [TestMethod]
        public void Constructor_WithComparer_ShouldCreateEmptyTree()
        {
            var set = new RedBlackTreeSet<Person>(false, Comparer<Person>.Create((a, b) => a.Age - b.Age));
            Assert.AreEqual(0, set.Count);
        }

        [TestMethod]
        public void Constructor_AllowDuplicates_ShouldReflectSetting()
        {
            var noDupes = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            Assert.IsFalse(noDupes.AllowDuplicates);

            var withDupes = new RedBlackTreeSet<Person>(true, (a, b) => a.Age - b.Age);
            Assert.IsTrue(withDupes.AllowDuplicates);
        }
        #endregion

        #region Add Tests
        [TestMethod]
        public void Add_SingleItem_ShouldIncreaseCount()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 30));
            Assert.AreEqual(1, set.Count);
        }

        [TestMethod]
        public void Add_MultipleItems_ShouldMaintainSortOrder()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));

            var items = set.ToArray();
            Assert.AreEqual(10, items[0].Age);
            Assert.AreEqual(20, items[1].Age);
            Assert.AreEqual(30, items[2].Age);
        }

        [TestMethod]
        public void Add_LargeDataset_ShouldMaintainOrder()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            var random = new Random(42);
            var ages = Enumerable.Range(0, 1000).OrderBy(_ => random.Next()).ToList();

            foreach (var age in ages)
                set.Add(new Person($"Person{age}", age));

            var sorted = set.ToArray();
            for (int i = 0; i < 1000; i++)
                Assert.AreEqual(i, sorted[i].Age);
        }
        #endregion

        #region AppendUnsafeOrdered Tests
        [TestMethod]
        public void AppendUnsafeOrdered_InOrder_ShouldWork()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.AppendUnsafeOrdered(new Person("Charlie", 30));

            Assert.AreEqual(3, set.Count);
            Assert.AreEqual(30, set[2].Age);
        }
        #endregion

        #region Duplicate Sort Order Tests (allowDuplicates = true)
        [TestMethod]
        public void Add_DuplicateSortOrder_WhenAllowed_ShouldAddAll()
        {
            var set = new RedBlackTreeSet<Person>(true, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 25));
            set.Add(new Person("Bob", 25));
            set.Add(new Person("Charlie", 25));

            Assert.AreEqual(3, set.Count);
            Assert.IsTrue(set.HasDuplicates);
        }

        [TestMethod]
        [ExpectedException(typeof(System.Data.ConstraintException))]
        public void Add_DuplicateSortOrder_WhenNotAllowed_ShouldThrow()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 25));
            set.Add(new Person("Bob", 25)); // Same age, duplicates not allowed
        }

        [TestMethod]
        public void Add_WithCustomSatelliteComparer_ShouldWork()
        {
            var set = new RedBlackTreeSet<Person>(
                true,
                (a, b) => a.Age - b.Age,
                (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal)
            );

            set.Add(new Person("Charlie", 25));
            set.Add(new Person("Alice", 25));
            set.Add(new Person("Bob", 25));

            Assert.AreEqual(3, set.Count);

            // Should be sorted by name within same age
            var items = set.ToArray();
            Assert.AreEqual("Alice", items[0].Name);
            Assert.AreEqual("Bob", items[1].Name);
            Assert.AreEqual("Charlie", items[2].Name);
        }

        [TestMethod]
        public void Remove_FromDuplicateSortOrder_ShouldRemoveOne()
        {
            var set = new RedBlackTreeSet<Person>(true, (a, b) => a.Age - b.Age);
            var alice = new Person("Alice", 25);
            var bob = new Person("Bob", 25);
            var charlie = new Person("Charlie", 25);
            set.Add(alice);
            set.Add(bob);
            set.Add(charlie);

            set.Remove(bob);

            Assert.AreEqual(2, set.Count);
        }

        [TestMethod]
        public void Remove_AllDuplicateSortOrder_ShouldClearHasDuplicates()
        {
            var set = new RedBlackTreeSet<Person>(true, (a, b) => a.Age - b.Age);
            var alice = new Person("Alice", 25);
            var bob = new Person("Bob", 25);
            set.Add(alice);
            set.Add(bob);

            set.Remove(alice);
            set.Remove(bob);

            Assert.AreEqual(0, set.Count);
            Assert.IsFalse(set.HasDuplicates);
        }
        #endregion

        #region Identical Keys Tests (Should throw)
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Add_SameInstance_ShouldThrow()
        {
            var set = new RedBlackTreeSet<Person>(true, (a, b) => a.Age - b.Age);
            var alice = new Person("Alice", 25);
            set.Add(alice);
            set.Add(alice);
        }

        [TestMethod]
        public void Add_SameInstance_ShouldThrowWithClearMessage()
        {
            var set = new RedBlackTreeSet<Person>(true, (a, b) => a.Age - b.Age);
            var alice = new Person("Alice", 25);
            set.Add(alice);

            try
            {
                set.Add(alice);
                Assert.Fail("Should have thrown InvalidOperationException");
            }
            catch (InvalidOperationException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Duplicate keys are not supported"));
            }
        }
        #endregion

        #region Satellite Comparer Returns Zero Tests
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Add_SatelliteComparerReturnsZero_ShouldThrow()
        {
            var set = new RedBlackTreeSet<Person>(
                true,
                (a, b) => a.Age - b.Age,
                (a, b) => 0  // Bad comparer!
            );

            set.Add(new Person("Alice", 25));
            set.Add(new Person("Bob", 25));
        }

        [TestMethod]
        public void Add_SatelliteComparerReturnsZero_ShouldThrowWithClearMessage()
        {
            var set = new RedBlackTreeSet<Person>(
                true,
                (a, b) => a.Age - b.Age,
                (a, b) => 0
            );

            set.Add(new Person("Alice", 25));

            try
            {
                set.Add(new Person("Bob", 25));
                Assert.Fail("Should have thrown InvalidOperationException");
            }
            catch (InvalidOperationException ex)
            {
                Assert.IsTrue(ex.Message.Contains("satelliteNodeComparer"));
            }
        }
        #endregion

        #region Remove Tests
        [TestMethod]
        public void Remove_ExistingItem_ShouldReturnTrueAndDecreaseCount()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            var alice = new Person("Alice", 10);
            var bob = new Person("Bob", 20);
            var charlie = new Person("Charlie", 30);
            set.Add(alice);
            set.Add(bob);
            set.Add(charlie);

            bool removed = set.Remove(bob);

            Assert.IsTrue(removed);
            Assert.AreEqual(2, set.Count);
            Assert.IsFalse(set.Contains(bob));
        }

        [TestMethod]
        public void Remove_NonExistingItem_ShouldReturnFalse()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 10));

            bool removed = set.Remove(new Person("Nobody", 999));

            Assert.IsFalse(removed);
            Assert.AreEqual(1, set.Count);
        }
        #endregion

        #region RemoveAt Tests
        [TestMethod]
        public void RemoveAt_ValidIndex_ShouldRemoveAndReturnItem()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("Charlie", 30));

            var removed = set.RemoveAt(1);

            Assert.AreEqual(20, removed.Age);
            Assert.AreEqual(2, set.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void RemoveAt_InvalidIndex_ShouldThrow()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 10));

            set.RemoveAt(5);
        }

        [TestMethod]
        public void RemoveAt_FirstElement_Repeatedly_ShouldWork()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            for (int i = 0; i < 100; i++)
                set.Add(new Person($"Person{i}", i));

            for (int i = 0; i < 100; i++)
            {
                var removed = set.RemoveAt(0);
                Assert.AreEqual(i, removed.Age);
            }

            Assert.AreEqual(0, set.Count);
        }
        #endregion

        #region Contains Tests
        [TestMethod]
        public void Contains_ExistingItem_ShouldReturnTrue()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            var alice = new Person("Alice", 42);
            set.Add(alice);

            Assert.IsTrue(set.Contains(alice));
        }

        [TestMethod]
        public void Contains_NonExistingItem_ShouldReturnFalse()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 42));

            Assert.IsFalse(set.Contains(new Person("Nobody", 999)));
        }

        [TestMethod]
        public void Contains_EmptySet_ShouldReturnFalse()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);

            Assert.IsFalse(set.Contains(new Person("Alice", 42)));
        }
        #endregion

        #region IndexOf Tests
        [TestMethod]
        public void IndexOf_ExistingItem_ShouldReturnCorrectIndex()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            var alice = new Person("Alice", 10);
            var bob = new Person("Bob", 20);
            var charlie = new Person("Charlie", 30);
            set.Add(charlie);
            set.Add(alice);
            set.Add(bob);

            Assert.AreEqual(0, set.IndexOf(alice));
            Assert.AreEqual(1, set.IndexOf(bob));
            Assert.AreEqual(2, set.IndexOf(charlie));
        }

        [TestMethod]
        public void IndexOf_NonExistingItem_ShouldReturnMinusOne()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 10));

            Assert.AreEqual(-1, set.IndexOf(new Person("Nobody", 999)));
        }
        #endregion

        #region Indexer Tests
        [TestMethod]
        public void Indexer_ValidIndex_ShouldReturnCorrectItem()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));

            Assert.AreEqual(10, set[0].Age);
            Assert.AreEqual(20, set[1].Age);
            Assert.AreEqual(30, set[2].Age);
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void Indexer_InvalidIndex_ShouldThrow()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 10));

            var _ = set[5];
        }
        #endregion

        #region FindKey Tests
        [TestMethod]
        public void FindKey_ExistingKey_ShouldReturnKey()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 30));
            set.Add(new Person("Bob", 25));
            set.Add(new Person("Charlie", 20));

            var found = set.FindKey(p => p.Age - 25);

            Assert.IsNotNull(found);
            Assert.AreEqual("Bob", found.Name);
        }

        [TestMethod]
        public void FindKey_NonExistingKey_ShouldReturnDefault()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 30));

            var found = set.FindKey(p => p.Age - 99);

            Assert.IsNull(found);
        }
        #endregion

        #region FindKeys Tests
        [TestMethod]
        public void FindKeys_WithDuplicateSortOrder_ShouldReturnAll()
        {
            var set = new RedBlackTreeSet<Person>(true, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 25));
            set.Add(new Person("Bob", 25));
            set.Add(new Person("Charlie", 30));

            var found = set.FindKeys(p => p.Age - 25).ToList();

            Assert.AreEqual(2, found.Count);
            Assert.IsTrue(found.Any(p => p.Name == "Alice"));
            Assert.IsTrue(found.Any(p => p.Name == "Bob"));
        }

        [TestMethod]
        public void FindKeys_NoDuplicates_ShouldReturnSingle()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 25));
            set.Add(new Person("Bob", 30));

            var found = set.FindKeys(p => p.Age - 25).ToList();

            Assert.AreEqual(1, found.Count);
            Assert.AreEqual("Alice", found[0].Name);
        }

        [TestMethod]
        public void FindKeys_NoMatch_ShouldReturnEmpty()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 25));

            var found = set.FindKeys(p => p.Age - 99).ToList();

            Assert.AreEqual(0, found.Count);
        }
        #endregion

        #region FindRange Tests
        [TestMethod]
        public void FindRange_ShouldReturnItemsInRange()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            for (int i = 0; i < 100; i++)
                set.Add(new Person($"Person{i}", i));

            var range = set.FindRange(p => p.Age - 20, p => p.Age - 30).ToList();

            Assert.AreEqual(11, range.Count); // 20 to 30 inclusive
            Assert.AreEqual(20, range.First().Age);
            Assert.AreEqual(30, range.Last().Age);
        }

        [TestMethod]
        public void FindRange_EmptyRange_ShouldReturnEmpty()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Young", 10));
            set.Add(new Person("Old", 50));

            var range = set.FindRange(p => p.Age - 20, p => p.Age - 30).ToList();

            Assert.AreEqual(0, range.Count);
        }

        [TestMethod]
        public void FindRange_WithDuplicateSortOrder_ShouldReturnAll()
        {
            var set = new RedBlackTreeSet<Person>(true, (a, b) => a.Age - b.Age);
            set.Add(new Person("Young", 20));
            set.Add(new Person("Alice", 25));
            set.Add(new Person("Bob", 25));
            set.Add(new Person("Charlie", 25));
            set.Add(new Person("Old", 30));

            var range = set.FindRange(p => p.Age - 20, p => p.Age - 30).ToList();

            Assert.AreEqual(5, range.Count);
        }
        #endregion

        #region FindRangeByIndex Tests
        [TestMethod]
        public void FindRangeByIndex_ShouldReturnCorrectSlice()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            for (int i = 0; i < 100; i++)
                set.Add(new Person($"Person{i}", i));

            var range = set.FindRangeByIndex(10, 5).ToList();

            Assert.AreEqual(5, range.Count);
            Assert.AreEqual(10, range[0].Age);
            Assert.AreEqual(14, range[4].Age);
        }

        [TestMethod]
        public void FindRangeByIndex_CountExceedsRemaining_ShouldReturnAvailable()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            for (int i = 0; i < 10; i++)
                set.Add(new Person($"Person{i}", i));

            var range = set.FindRangeByIndex(8, 100).ToList();

            Assert.AreEqual(2, range.Count);
            Assert.AreEqual(8, range[0].Age);
            Assert.AreEqual(9, range[1].Age);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void FindRangeByIndex_NegativeStart_ShouldThrow()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 10));

            set.FindRangeByIndex(-1, 5).ToList();
        }
        #endregion

        #region Enumeration Tests
        [TestMethod]
        public void GetEnumerator_ShouldEnumerateInOrder()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));

            var items = new List<Person>();
            foreach (var item in set)
                items.Add(item);

            Assert.AreEqual(3, items.Count);
            Assert.AreEqual(10, items[0].Age);
            Assert.AreEqual(20, items[1].Age);
            Assert.AreEqual(30, items[2].Age);
        }

        [TestMethod]
        public void GetEnumerator_WithStartIndex_ShouldStartFromIndex()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            for (int i = 0; i < 10; i++)
                set.Add(new Person($"Person{i}", i));

            var items = new List<Person>();
            var enumerator = set.GetEnumerator(5);
            while (enumerator.MoveNext())
                items.Add(enumerator.Current);

            Assert.AreEqual(5, items.Count);
            Assert.AreEqual(5, items[0].Age);
            Assert.AreEqual(9, items[4].Age);
        }

        [TestMethod]
        public void GetEnumerator_WithDuplicateSortOrder_ShouldEnumerateAll()
        {
            var set = new RedBlackTreeSet<Person>(true, (a, b) => a.Age - b.Age);
            set.Add(new Person("First", 10));
            set.Add(new Person("Alice", 20));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("Charlie", 20));
            set.Add(new Person("Last", 30));

            var items = set.ToList();

            Assert.AreEqual(5, items.Count);
        }
        #endregion

        #region CopyTo Tests
        [TestMethod]
        public void CopyTo_ShouldCopyAllItems()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));

            var array = new Person[3];
            set.CopyTo(array, 0);

            Assert.AreEqual(10, array[0].Age);
            Assert.AreEqual(20, array[1].Age);
            Assert.AreEqual(30, array[2].Age);
        }

        [TestMethod]
        public void CopyTo_WithOffset_ShouldCopyAtOffset()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));

            var array = new Person[5];
            set.CopyTo(array, 2);

            Assert.IsNull(array[0]);
            Assert.IsNull(array[1]);
            Assert.AreEqual(10, array[2].Age);
            Assert.AreEqual(20, array[3].Age);
            Assert.IsNull(array[4]);
        }

        [TestMethod]
        public void CopyTo_WithDuplicateSortOrder_ShouldCopyAll()
        {
            var set = new RedBlackTreeSet<Person>(true, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 25));
            set.Add(new Person("Bob", 25));
            set.Add(new Person("Charlie", 25));

            var array = new Person[3];
            set.CopyTo(array, 0);

            Assert.AreEqual(3, array.Count(p => p != null));
            Assert.IsTrue(array.All(p => p.Age == 25));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CopyTo_NullArray_ShouldThrow()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 10));

            set.CopyTo((Person[])null, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CopyTo_NegativeIndex_ShouldThrow()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 10));

            set.CopyTo(new Person[1], -1);
        }
        #endregion

        #region UpdateNodeKey Tests
        [TestMethod]
        public void UpdateNodeKey_ShouldUpdateValue()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            var alice = new Person("Alice", 30);
            set.Add(alice);

            var updatedAlice = new Person("Alice Updated", 30);
            set.UpdateNodeKey(alice, updatedAlice);

            var found = set.FindKey(p => p.Age - 30);
            Assert.AreEqual("Alice Updated", found.Name);
        }
        #endregion

        #region Clear Tests
        [TestMethod]
        public void Clear_ShouldResetTree()
        {
            var set = new RedBlackTreeSet<Person>(true, (a, b) => a.Age - b.Age);
            set.Add(new Person("Alice", 25));
            set.Add(new Person("Bob", 25));
            set.Add(new Person("Charlie", 30));

            set.Clear();

            Assert.AreEqual(0, set.Count);
            Assert.IsFalse(set.HasDuplicates);
        }
        #endregion

        #region Edge Cases
        [TestMethod]
        public void EmptyTree_Operations_ShouldNotThrow()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);

            Assert.AreEqual(0, set.Count);
            Assert.IsFalse(set.Contains(new Person("Alice", 42)));
            Assert.IsFalse(set.Remove(new Person("Alice", 42)));
            Assert.AreEqual(-1, set.IndexOf(new Person("Alice", 42)));
            Assert.AreEqual(0, set.ToArray().Length);
        }

        [TestMethod]
        public void SingleItem_AllOperations_ShouldWork()
        {
            var set = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);
            var alice = new Person("Alice", 42);
            set.Add(alice);

            Assert.AreEqual(1, set.Count);
            Assert.IsTrue(set.Contains(alice));
            Assert.AreEqual(0, set.IndexOf(alice));
            Assert.AreEqual(alice, set[0]);

            var removed = set.RemoveAt(0);
            Assert.AreEqual(alice, removed);
            Assert.AreEqual(0, set.Count);
        }

        [TestMethod]
        public void StressTest_AddRemove_ShouldMaintainIntegrity()
        {
            var set = new RedBlackTreeSet<Person>(true, (a, b) => a.Age - b.Age);
            var random = new Random(42);

            // Add 1000 items with random ages (will create duplicates)
            for (int i = 0; i < 1000; i++)
                set.Add(new Person($"Person{i}", random.Next(100)));

            // Remove 500 items
            for (int i = 0; i < 500; i++)
                set.RemoveAt(0);

            // Verify order is maintained
            var items = set.ToArray();
            for (int i = 1; i < items.Length; i++)
                Assert.IsTrue(items[i].Age >= items[i - 1].Age, "Order should be maintained");
        }
        #endregion

        #region RedBlackTree vs SortedSet
        [TestMethod]
        public void SpeedTest_VsSortedSet()
        {
            var sortedSet = new SortedSet<Person>(Comparer<Person>.Create((a, b) => a.Age - b.Age));
            var tree = new RedBlackTreeSet<Person>(false, (a, b) => a.Age - b.Age);

            double sortedSetElapsed, treeElapsed;
            var sb = new StringBuilder();
            sb.AppendLine("==================================================================");
            sb.AppendLine("|                    |     SortedSet       |        Tree         |");
            sb.AppendLine("==================================================================");

            // ============================================================
            // Add (sorted order)
            // ============================================================
            const int AddCount = 100000;
            var watch = new Stopwatch();

            watch.Start();
            for (int i = 0; i < AddCount; i++)
            {
                tree.Add(new Person($"Person{i}", i));
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < AddCount; i++)
            {
                sortedSet.Add(new Person($"Person{i}", i));
            }
            watch.Stop();
            sortedSetElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" addsorted (x" + AddCount + ")",-20}| {sortedSetElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // Add (random order)
            // ============================================================
            tree.Clear();
            sortedSet.Clear();
            var random = new Random(42);
            var ages = Enumerable.Range(0, AddCount).OrderBy(_ => random.Next()).ToArray();

            watch.Reset();
            watch.Start();
            for (int i = 0; i < AddCount; i++)
            {
                tree.Add(new Person($"Person{ages[i]}", ages[i]));
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < AddCount; i++)
            {
                sortedSet.Add(new Person($"Person{ages[i]}", ages[i]));
            }
            watch.Stop();
            sortedSetElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" addrandom (x" + AddCount + ")",-20}| {sortedSetElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // Contains
            // ============================================================
            tree.Clear();
            sortedSet.Clear();
            var persons = new List<Person>();
            for (int i = 0; i < 10000; i++)
            {
                var p = new Person($"Person{i}", i);
                tree.Add(p);
                sortedSet.Add(p);
                persons.Add(p);
            }

            const int ContainsCount = 100000;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < ContainsCount; i++)
            {
                var p = persons[i % persons.Count];
                var found = tree.Contains(p);
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < ContainsCount; i++)
            {
                var p = persons[i % persons.Count];
                var found = sortedSet.Contains(p);
            }
            watch.Stop();
            sortedSetElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" contains (x" + ContainsCount + ")",-20}| {sortedSetElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // Random access this[index] - Tree only (SortedSet has no indexer)
            // ============================================================
            tree.Clear();
            for (int i = 0; i < 50000; i++)
            {
                tree.Add(new Person($"Person{i}", i));
            }

            const int AccessCount = 100000;
            random = new Random(42);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < AccessCount; i++)
            {
                int idx = random.Next(0, tree.Count);
                var val = tree[idx].Age;
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" this[i] (x" + AccessCount + ")",-20}| {"N/A",-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // IndexOf - Tree only (SortedSet has no IndexOf)
            // ============================================================
            tree.Clear();
            var searchItems = new List<Person>();
            for (int i = 0; i < 1000; i++)
            {
                var p = new Person($"Person{i}", i);
                tree.Add(p);
                if (i % 10 == 0)
                    searchItems.Add(p);
            }

            const int IndexOfCount = 10000;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < IndexOfCount; i++)
            {
                var p = searchItems[i % searchItems.Count];
                var idx = tree.IndexOf(p);
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" indexOf (x" + IndexOfCount + ")",-20}| {"N/A",-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // Remove by item
            // ============================================================
            tree.Clear();
            sortedSet.Clear();
            const int RemoveCount = 100000;
            var itemsToRemove = new List<Person>();
            for (int i = 0; i < RemoveCount; i++)
            {
                var p = new Person($"Person{i}", i);
                tree.Add(p);
                sortedSet.Add(p);
                itemsToRemove.Add(p);
            }

            watch.Reset();
            watch.Start();
            for (int i = 0; i < RemoveCount; i++)
            {
                tree.Remove(itemsToRemove[i]);
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < RemoveCount; i++)
            {
                sortedSet.Remove(itemsToRemove[i]);
            }
            watch.Stop();
            sortedSetElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" remove (x" + RemoveCount + ")",-20}| {sortedSetElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // RemoveAt - Tree only (SortedSet has no RemoveAt)
            // ============================================================
            tree.Clear();
            const int RemoveAtCount = 100000;
            for (int i = 0; i < RemoveAtCount; i++)
            {
                tree.Add(new Person($"Person{i}", i));
            }

            watch.Reset();
            watch.Start();
            while (tree.Count > 0)
            {
                tree.RemoveAt(0);
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" removeAt (x" + RemoveAtCount + ")",-20}| {"N/A",-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // Enumerate
            // ============================================================
            tree.Clear();
            sortedSet.Clear();
            for (int i = 0; i < 100000; i++)
            {
                var p = new Person($"Person{i}", i);
                tree.Add(p);
                sortedSet.Add(p);
            }

            watch.Reset();
            watch.Start();
            long sum = 0;
            foreach (var item in tree)
            {
                sum += item.Age;
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            sum = 0;
            foreach (var item in sortedSet)
            {
                sum += item.Age;
            }
            watch.Stop();
            sortedSetElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" enumerate (x100000)",-20}| {sortedSetElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // FindRange vs GetViewBetween
            // ============================================================
            tree.Clear();
            sortedSet.Clear();
            for (int i = 0; i < 100000; i++)
            {
                var p = new Person($"Person{i}", i);
                tree.Add(p);
                sortedSet.Add(p);
            }

            var lowBound = new Person("Low", 25000);
            var highBound = new Person("High", 75000);

            const int RangeCount = 1000;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < RangeCount; i++)
            {
                var range = tree.FindRange(p => p.Age - 25000, p => p.Age - 75000).ToList();
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < RangeCount; i++)
            {
                var range = sortedSet.GetViewBetween(lowBound, highBound).ToList();
            }
            watch.Stop();
            sortedSetElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" range (x" + RangeCount + ")",-20}| {sortedSetElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("==================================================================");

            this.TestContext.WriteLine(sb.ToString());
        }
        #endregion

        #region Serialization tests

        #region comparers
        [Serializable]
        class BinarySerializableComparer : IComparer<Person>
        {
            public int Compare(Person x, Person y)
            {
                return StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
            }
        }
        [Serializable]
        class BinarySerializableSatelliteComparer : IComparer<Person>
        {
            public int Compare(Person x, Person y)
            {
                return (x.Age - y.Age) * -1;
            }
        }
        [ProtoContract]
        public class PublicSerializableComparer : IComparer<Person>
        {
            public int Compare(Person x, Person y)
            {
                return this.IgnoreCase ? StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name) : StringComparer.Ordinal.Compare(x.Name, y.Name);
            }
            [XmlElement]
            [ProtoMember(1)]
            public bool IgnoreCase { get; set; }
        }
        [ProtoContract]
        public class PublicSerializableSatelliteComparer : IComparer<Person>
        {
            public int Compare(Person x, Person y)
            {
                return (x.Age - y.Age) * (this.Descending ? -1 : 1);
            }
            [XmlElement]
            [ProtoMember(1)]
            public bool Descending { get; set; }
        }
        #endregion

        #region Binary
        [TestMethod]
        public void SerializeBinaryTest()
        {
            var set = new RedBlackTreeSet<Person>();
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));


            var formatter = new BinaryFormatter { AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple };
            using (var mem = new MemoryStream())
            {
                formatter.Serialize(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person>)formatter.Deserialize(mem);

                Assert.AreEqual(set.Count, clone.Count);
                Assert.AreEqual(set[0].Name, clone[0].Name);
                Assert.AreEqual(set[0].Age, clone[0].Age);
                Assert.AreEqual(set[1].Name, clone[1].Name);
                Assert.AreEqual(set[1].Age, clone[1].Age);
                Assert.AreEqual(set[2].Name, clone[2].Name);
                Assert.AreEqual(set[2].Age, clone[2].Age);
                Assert.AreEqual(set[3].Name, clone[3].Name);
                Assert.AreEqual(set[3].Age, clone[3].Age);               
            }
        }

        [TestMethod]
        public void SerializeBinaryTest_CustomComparer()
        {
            var set = new RedBlackTreeSet<Person>(true, new BinarySerializableComparer(), new BinarySerializableSatelliteComparer());
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var formatter = new BinaryFormatter { AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple };
            using (var mem = new MemoryStream())
            {
                formatter.Serialize(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person>)formatter.Deserialize(mem);

                Assert.AreEqual(set.Count, clone.Count);
                Assert.AreEqual(set[0].Name, clone[0].Name);
                Assert.AreEqual(set[0].Age, clone[0].Age);
                Assert.AreEqual(set[1].Name, clone[1].Name);
                Assert.AreEqual(set[1].Age, clone[1].Age);
                Assert.AreEqual(set[2].Name, clone[2].Name);
                Assert.AreEqual(set[2].Age, clone[2].Age);
                Assert.AreEqual(set[3].Name, clone[3].Name);
                Assert.AreEqual(set[3].Age, clone[3].Age);
            }
        }
        #endregion

        #region Xml
        [TestMethod]
        public void SerializeXmlTest()
        {
            var set = new RedBlackTreeSet<Person>();
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));


            var serializer = new XmlSerializer(typeof(RedBlackTreeSet<Person>));
            using (var mem = new MemoryStream())
            {
                serializer.Serialize(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person>)serializer.Deserialize(mem);

                Assert.AreEqual(set.Count, clone.Count);
                Assert.AreEqual(set[0].Name, clone[0].Name);
                Assert.AreEqual(set[0].Age, clone[0].Age);
                Assert.AreEqual(set[1].Name, clone[1].Name);
                Assert.AreEqual(set[1].Age, clone[1].Age);
                Assert.AreEqual(set[2].Name, clone[2].Name);
                Assert.AreEqual(set[2].Age, clone[2].Age);
                Assert.AreEqual(set[3].Name, clone[3].Name);
                Assert.AreEqual(set[3].Age, clone[3].Age);
            }
        }

        [TestMethod]
        public void SerializeXmlTest_CustomManagedComparer()
        {
            var set = new RedBlackTreeSet<string>(true, StringComparer.OrdinalIgnoreCase, StringComparer.Ordinal);
            set.Add("Charlie");
            set.Add("CHARLIE");
            set.Add("Bob");
            set.Add("John");

            var serializer = new XmlSerializer(typeof(RedBlackTreeSet<string>));
            using (var mem = new MemoryStream())
            {
                serializer.Serialize(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<string>)serializer.Deserialize(mem);

                Assert.AreEqual(set.Count, clone.Count);
                Assert.AreEqual(set[0], clone[0]);
                Assert.AreEqual(set[1], clone[1]);
                Assert.AreEqual(set[2], clone[2]);
                Assert.AreEqual(set[3], clone[3]);                
            }
        }

        [TestMethod]
        public void SerializeXmlTest_CustomComparerBinarySerializable()
        {
            var set = new RedBlackTreeSet<Person>(true, new BinarySerializableComparer(), new BinarySerializableSatelliteComparer());
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var serializer = new XmlSerializer(typeof(RedBlackTreeSet<Person>));
            using (var mem = new MemoryStream())
            {
                serializer.Serialize(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person>)serializer.Deserialize(mem);

                Assert.AreEqual(set.Count, clone.Count);
                Assert.AreEqual(set[0].Name, clone[0].Name);
                Assert.AreEqual(set[0].Age, clone[0].Age);
                Assert.AreEqual(set[1].Name, clone[1].Name);
                Assert.AreEqual(set[1].Age, clone[1].Age);
                Assert.AreEqual(set[2].Name, clone[2].Name);
                Assert.AreEqual(set[2].Age, clone[2].Age);
                Assert.AreEqual(set[3].Name, clone[3].Name);
                Assert.AreEqual(set[3].Age, clone[3].Age);
            }
        }

        [TestMethod]
        public void SerializeXmlTest_CustomComparerXmlSerializable()
        {
            var set = new RedBlackTreeSet<Person>(true, new PublicSerializableComparer { IgnoreCase = true }, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var serializer = new XmlSerializer(typeof(RedBlackTreeSet<Person>));
            using (var mem = new MemoryStream())
            {
                serializer.Serialize(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person>)serializer.Deserialize(mem);

                Assert.AreEqual(set.Count, clone.Count);
                Assert.AreEqual(set[0].Name, clone[0].Name);
                Assert.AreEqual(set[0].Age, clone[0].Age);
                Assert.AreEqual(set[1].Name, clone[1].Name);
                Assert.AreEqual(set[1].Age, clone[1].Age);
                Assert.AreEqual(set[2].Name, clone[2].Name);
                Assert.AreEqual(set[2].Age, clone[2].Age);
                Assert.AreEqual(set[3].Name, clone[3].Name);
                Assert.AreEqual(set[3].Age, clone[3].Age);
            }
        }
        #endregion

        #region DataContract
        [TestMethod]
        public void SerializeDataContractTest()
        {
            var set = new RedBlackTreeSet<Person>();
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var serializer = new DataContractSerializer(typeof(RedBlackTreeSet<Person>));
            using (var mem = new MemoryStream())
            {
                serializer.WriteObject(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person>)serializer.ReadObject(mem);

                Assert.AreEqual(set.Count, clone.Count);
                Assert.AreEqual(set[0].Name, clone[0].Name);
                Assert.AreEqual(set[0].Age, clone[0].Age);
                Assert.AreEqual(set[1].Name, clone[1].Name);
                Assert.AreEqual(set[1].Age, clone[1].Age);
                Assert.AreEqual(set[2].Name, clone[2].Name);
                Assert.AreEqual(set[2].Age, clone[2].Age);
                Assert.AreEqual(set[3].Name, clone[3].Name);
                Assert.AreEqual(set[3].Age, clone[3].Age);

            }
        }

        [TestMethod]
        public void SerializeDataContractTest_CustomManagedComparer()
        {
            var set = new RedBlackTreeSet<string>(true, StringComparer.OrdinalIgnoreCase, StringComparer.Ordinal);
            set.Add("Charlie");
            set.Add("CHARLIE");
            set.Add("Bob");
            set.Add("John");


            var serializer = new DataContractSerializer(typeof(RedBlackTreeSet<string>));
            using (var mem = new MemoryStream())
            {
                serializer.WriteObject(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<string>)serializer.ReadObject(mem);

                Assert.AreEqual(set.Count, clone.Count);
                Assert.AreEqual(set[0], clone[0]);
                Assert.AreEqual(set[1], clone[1]);
                Assert.AreEqual(set[2], clone[2]);
                Assert.AreEqual(set[3], clone[3]);
            }
        }

        [TestMethod]
        public void SerializeDataContractTest_CustomComparerBinarySerializable()
        {
            var set = new RedBlackTreeSet<Person>(true, new BinarySerializableComparer(), new BinarySerializableSatelliteComparer());
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));


            var serializer = new DataContractSerializer(typeof(RedBlackTreeSet<Person>));
            using (var mem = new MemoryStream())
            {
                serializer.WriteObject(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person>)serializer.ReadObject(mem);

                Assert.AreEqual(set.Count, clone.Count);
                Assert.AreEqual(set[0].Name, clone[0].Name);
                Assert.AreEqual(set[0].Age, clone[0].Age);
                Assert.AreEqual(set[1].Name, clone[1].Name);
                Assert.AreEqual(set[1].Age, clone[1].Age);
                Assert.AreEqual(set[2].Name, clone[2].Name);
                Assert.AreEqual(set[2].Age, clone[2].Age);
                Assert.AreEqual(set[3].Name, clone[3].Name);
                Assert.AreEqual(set[3].Age, clone[3].Age);

            }
        }

        [TestMethod]
        public void SerializeDataContractTest_CustomComparerXmlSerializable()
        {
            var set = new RedBlackTreeSet<Person>(true, new PublicSerializableComparer { IgnoreCase = true }, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));


            var serializer = new DataContractSerializer(typeof(RedBlackTreeSet<Person>));
            using (var mem = new MemoryStream())
            {
                serializer.WriteObject(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person>)serializer.ReadObject(mem);

                Assert.AreEqual(set.Count, clone.Count);
                Assert.AreEqual(set[0].Name, clone[0].Name);
                Assert.AreEqual(set[0].Age, clone[0].Age);
                Assert.AreEqual(set[1].Name, clone[1].Name);
                Assert.AreEqual(set[1].Age, clone[1].Age);
                Assert.AreEqual(set[2].Name, clone[2].Name);
                Assert.AreEqual(set[2].Age, clone[2].Age);
                Assert.AreEqual(set[3].Name, clone[3].Name);
                Assert.AreEqual(set[3].Age, clone[3].Age);

            }
        }
        #endregion

        #region Json Text
        [TestMethod]
        public void SerializeJsonTextTest()
        {
            var set = new RedBlackTreeSet<Person>();
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var json = System.Text.Json.JsonSerializer.Serialize(set);
            var clone = System.Text.Json.JsonSerializer.Deserialize<RedBlackTreeSet<Person>>(json);

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0].Name, clone[0].Name);
            Assert.AreEqual(set[0].Age, clone[0].Age);
            Assert.AreEqual(set[1].Name, clone[1].Name);
            Assert.AreEqual(set[1].Age, clone[1].Age);
            Assert.AreEqual(set[2].Name, clone[2].Name);
            Assert.AreEqual(set[2].Age, clone[2].Age);
            Assert.AreEqual(set[3].Name, clone[3].Name);
            Assert.AreEqual(set[3].Age, clone[3].Age);
        }

        [TestMethod]
        public void SerializeJsonTextTest_CustomManagedComparer()
        {
            var set = new RedBlackTreeSet<string>(true, StringComparer.OrdinalIgnoreCase, StringComparer.Ordinal);
            set.Add("Charlie");
            set.Add("CHARLIE");
            set.Add("Bob");
            set.Add("John");

            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new Serialization.JsonText.RedBlackTreeSetJsonTextConverter<string>());

            var json = System.Text.Json.JsonSerializer.Serialize(set, options);
            var clone = System.Text.Json.JsonSerializer.Deserialize<RedBlackTreeSet<string>>(json, options);

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0], clone[0]);
            Assert.AreEqual(set[1], clone[1]);
            Assert.AreEqual(set[2], clone[2]);
            Assert.AreEqual(set[3], clone[3]);
        }

        [TestMethod]
        public void SerializeJsonTextTest_CustomComparerBinarySerializable()
        {
            var set = new RedBlackTreeSet<Person>(true, new BinarySerializableComparer(), new BinarySerializableSatelliteComparer());
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new Serialization.JsonText.RedBlackTreeSetJsonTextConverter<Person>());

            var json = System.Text.Json.JsonSerializer.Serialize(set, options);
            var clone = System.Text.Json.JsonSerializer.Deserialize<RedBlackTreeSet<Person>>(json, options);

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0].Name, clone[0].Name);
            Assert.AreEqual(set[0].Age, clone[0].Age);
            Assert.AreEqual(set[1].Name, clone[1].Name);
            Assert.AreEqual(set[1].Age, clone[1].Age);
            Assert.AreEqual(set[2].Name, clone[2].Name);
            Assert.AreEqual(set[2].Age, clone[2].Age);
            Assert.AreEqual(set[3].Name, clone[3].Name);
            Assert.AreEqual(set[3].Age, clone[3].Age);
        }

        [TestMethod]
        public void SerializeJsonTextTest_CustomComparerJsonSerializable()
        {
            var set = new RedBlackTreeSet<Person>(true, new PublicSerializableComparer { IgnoreCase = true }, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new Serialization.JsonText.RedBlackTreeSetJsonTextConverter<Person>());

            var json = System.Text.Json.JsonSerializer.Serialize(set, options);
            var clone = System.Text.Json.JsonSerializer.Deserialize<RedBlackTreeSet<Person>>(json, options);

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0].Name, clone[0].Name);
            Assert.AreEqual(set[0].Age, clone[0].Age);
            Assert.AreEqual(set[1].Name, clone[1].Name);
            Assert.AreEqual(set[1].Age, clone[1].Age);
            Assert.AreEqual(set[2].Name, clone[2].Name);
            Assert.AreEqual(set[2].Age, clone[2].Age);
            Assert.AreEqual(set[3].Name, clone[3].Name);
            Assert.AreEqual(set[3].Age, clone[3].Age);
        }
        #endregion

        #region Json Newton
        [TestMethod]
        public void SerializeJsonNewtonTest()
        {
            var set = new RedBlackTreeSet<Person>();
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(set);
            var clone = Newtonsoft.Json.JsonConvert.DeserializeObject<RedBlackTreeSet<Person>>(json);

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0].Name, clone[0].Name);
            Assert.AreEqual(set[0].Age, clone[0].Age);
            Assert.AreEqual(set[1].Name, clone[1].Name);
            Assert.AreEqual(set[1].Age, clone[1].Age);
            Assert.AreEqual(set[2].Name, clone[2].Name);
            Assert.AreEqual(set[2].Age, clone[2].Age);
            Assert.AreEqual(set[3].Name, clone[3].Name);
            Assert.AreEqual(set[3].Age, clone[3].Age);
        }


        [TestMethod]
        public void SerializeJsonNewtonTest_CustomManagedComparer()
        {
            var set = new RedBlackTreeSet<string>(true, StringComparer.OrdinalIgnoreCase, StringComparer.Ordinal);
            set.Add("Charlie");
            set.Add("CHARLIE");
            set.Add("Bob");
            set.Add("John");

            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.Converters.Add(new Serialization.Newton.RedBlackTreeSetJsonNewtonConverter<string>());

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(set, settings);
            var clone = Newtonsoft.Json.JsonConvert.DeserializeObject<RedBlackTreeSet<string>>(json, settings);

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0], clone[0]);
            Assert.AreEqual(set[1], clone[1]);
            Assert.AreEqual(set[2], clone[2]);
            Assert.AreEqual(set[3], clone[3]);
        }

        [TestMethod]
        public void SerializeJsonNewtonTest_CustomComparerBinarySerializable()
        {
            var set = new RedBlackTreeSet<Person>(true, new BinarySerializableComparer(), new BinarySerializableSatelliteComparer());
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));


            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.Converters.Add(new Serialization.Newton.RedBlackTreeSetJsonNewtonConverter<Person>());

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(set, settings);
            var clone = Newtonsoft.Json.JsonConvert.DeserializeObject<RedBlackTreeSet<Person>>(json, settings);

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0].Name, clone[0].Name);
            Assert.AreEqual(set[0].Age, clone[0].Age);
            Assert.AreEqual(set[1].Name, clone[1].Name);
            Assert.AreEqual(set[1].Age, clone[1].Age);
            Assert.AreEqual(set[2].Name, clone[2].Name);
            Assert.AreEqual(set[2].Age, clone[2].Age);
            Assert.AreEqual(set[3].Name, clone[3].Name);
            Assert.AreEqual(set[3].Age, clone[3].Age);
        }

        [TestMethod]
        public void SerializeJsonNewtonTest_CustomComparerJsonSerializable()
        {
            var set = new RedBlackTreeSet<Person>(true, new PublicSerializableComparer { IgnoreCase = true }, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));


            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.Converters.Add(new Serialization.Newton.RedBlackTreeSetJsonNewtonConverter<Person>());

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(set, settings);
            var clone = Newtonsoft.Json.JsonConvert.DeserializeObject< RedBlackTreeSet < Person >> (json, settings);

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0].Name, clone[0].Name);
            Assert.AreEqual(set[0].Age, clone[0].Age);
            Assert.AreEqual(set[1].Name, clone[1].Name);
            Assert.AreEqual(set[1].Age, clone[1].Age);
            Assert.AreEqual(set[2].Name, clone[2].Name);
            Assert.AreEqual(set[2].Age, clone[2].Age);
            Assert.AreEqual(set[3].Name, clone[3].Name);
            Assert.AreEqual(set[3].Age, clone[3].Age);
        }
        #endregion

        #region MessagePack
        [TestMethod]
        public void SerializeMessagePackTest()
        {
            var set = new RedBlackTreeSet<Person>();
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);

            var bytes = MessagePackSerializer.Serialize(set);
            var clone = MessagePackSerializer.Deserialize<RedBlackTreeSet<Person>>(bytes);

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0].Name, clone[0].Name);
            Assert.AreEqual(set[0].Age, clone[0].Age);
            Assert.AreEqual(set[1].Name, clone[1].Name);
            Assert.AreEqual(set[1].Age, clone[1].Age);
            Assert.AreEqual(set[2].Name, clone[2].Name);
            Assert.AreEqual(set[2].Age, clone[2].Age);
            Assert.AreEqual(set[3].Name, clone[3].Name);
            Assert.AreEqual(set[3].Age, clone[3].Age);
        }

        [TestMethod]
        public void SerializeMessagePackTest_CustomManagedComparer()
        {
            var set = new RedBlackTreeSet<string>(true, StringComparer.OrdinalIgnoreCase, StringComparer.Ordinal);
            set.Add("Charlie");
            set.Add("CHARLIE");
            set.Add("Bob");
            set.Add("John");

            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                new[] { new Serialization.MessagePack.RedBlackTreeSetMessagePackFormatter<string>() },
                new[] { MessagePack.Resolvers.ContractlessStandardResolver.Instance }
            );

            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

            var bytes = MessagePackSerializer.Serialize(set, options);
            var clone = MessagePackSerializer.Deserialize<RedBlackTreeSet<string>>(bytes, options);

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0], clone[0]);
            Assert.AreEqual(set[1], clone[1]);
            Assert.AreEqual(set[2], clone[2]);
            Assert.AreEqual(set[3], clone[3]);
        }

        [TestMethod]
        public void SerializeMessagePackTest_CustomComparerBinarySerializable()
        {
            var set = new RedBlackTreeSet<Person>(true, new BinarySerializableComparer(), new BinarySerializableSatelliteComparer());
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                new[] { new Serialization.MessagePack.RedBlackTreeSetMessagePackFormatter<Person>() },
                new[] { MessagePack.Resolvers.ContractlessStandardResolver.Instance }
            );

            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

            var bytes = MessagePackSerializer.Serialize(set, options);
            var clone = MessagePackSerializer.Deserialize<RedBlackTreeSet<Person>>(bytes, options);

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0].Name, clone[0].Name);
            Assert.AreEqual(set[0].Age, clone[0].Age);
            Assert.AreEqual(set[1].Name, clone[1].Name);
            Assert.AreEqual(set[1].Age, clone[1].Age);
            Assert.AreEqual(set[2].Name, clone[2].Name);
            Assert.AreEqual(set[2].Age, clone[2].Age);
            Assert.AreEqual(set[3].Name, clone[3].Name);
            Assert.AreEqual(set[3].Age, clone[3].Age);
        }

        [TestMethod]
        public void SerializeMessagePackTest_CustomComparerMessagePackSerializable()
        {
            var set = new RedBlackTreeSet<Person>(true, new PublicSerializableComparer { IgnoreCase = true }, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                new[] { new Serialization.MessagePack.RedBlackTreeSetMessagePackFormatter<Person>() },
                new[] { MessagePack.Resolvers.ContractlessStandardResolver.Instance }
            );

            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

            var bytes = MessagePackSerializer.Serialize(set, options);
            var clone = MessagePackSerializer.Deserialize<RedBlackTreeSet<Person>>(bytes, options);

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0].Name, clone[0].Name);
            Assert.AreEqual(set[0].Age, clone[0].Age);
            Assert.AreEqual(set[1].Name, clone[1].Name);
            Assert.AreEqual(set[1].Age, clone[1].Age);
            Assert.AreEqual(set[2].Name, clone[2].Name);
            Assert.AreEqual(set[2].Age, clone[2].Age);
            Assert.AreEqual(set[3].Name, clone[3].Name);
            Assert.AreEqual(set[3].Age, clone[3].Age);
        }
        #endregion

        #region Protobuf      
        [TestMethod]
        public void SerializeProtoBufTest()
        {
            var set = new RedBlackTreeSet<Person>();
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            RedBlackTreeSet<Person> clone;
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, set);
                mem.Position = 0;
                clone = ProtoBuf.Serializer.Deserialize<RedBlackTreeSet<Person>>(mem);
            }

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0].Name, clone[0].Name);
            Assert.AreEqual(set[0].Age, clone[0].Age);
            Assert.AreEqual(set[1].Name, clone[1].Name);
            Assert.AreEqual(set[1].Age, clone[1].Age);
            Assert.AreEqual(set[2].Name, clone[2].Name);
            Assert.AreEqual(set[2].Age, clone[2].Age);
            Assert.AreEqual(set[3].Name, clone[3].Name);
            Assert.AreEqual(set[3].Age, clone[3].Age);
        }

        [TestMethod]
        public void SerializeProtoBufTest_CustomManagedComparer()
        {
            // uses surrogate, see TestInitializer
            var set = new RedBlackTreeSet<string>(true, StringComparer.OrdinalIgnoreCase, StringComparer.Ordinal);
            set.Add("Charlie");
            set.Add("CHARLIE");
            set.Add("Bob");
            set.Add("John");

            RedBlackTreeSet<string> clone;
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, set);
                mem.Position = 0;
                clone = ProtoBuf.Serializer.Deserialize<RedBlackTreeSet<string>>(mem);
            }

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0], clone[0]);
            Assert.AreEqual(set[1], clone[1]);
            Assert.AreEqual(set[2], clone[2]);
            Assert.AreEqual(set[3], clone[3]);
        }

        [TestMethod]
        public void SerializeProtoBufTest_CustomComparerBinarySerializable()
        {
            // uses surrogate, see TestInitializer
            var set = new RedBlackTreeSet<Person>(true, new BinarySerializableComparer(), new BinarySerializableSatelliteComparer());
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            RedBlackTreeSet<Person> clone;
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, set);
                mem.Position = 0;
                clone = ProtoBuf.Serializer.Deserialize<RedBlackTreeSet<Person>>(mem);
            }

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0].Name, clone[0].Name);
            Assert.AreEqual(set[0].Age, clone[0].Age);
            Assert.AreEqual(set[1].Name, clone[1].Name);
            Assert.AreEqual(set[1].Age, clone[1].Age);
            Assert.AreEqual(set[2].Name, clone[2].Name);
            Assert.AreEqual(set[2].Age, clone[2].Age);
            Assert.AreEqual(set[3].Name, clone[3].Name);
            Assert.AreEqual(set[3].Age, clone[3].Age);
        }

        [TestMethod]
        public void SerializeProtoBufTest_CustomComparerProtobufSerializable()
        {
            // uses surrogate, see TestInitializer
            var set = new RedBlackTreeSet<Person>(true, new PublicSerializableComparer { IgnoreCase = true }, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            RedBlackTreeSet<Person> clone;
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, set);
                mem.Position = 0;
                clone = ProtoBuf.Serializer.Deserialize<RedBlackTreeSet<Person>>(mem);
            }            

            Assert.AreEqual(set.Count, clone.Count);
            Assert.AreEqual(set[0].Name, clone[0].Name);
            Assert.AreEqual(set[0].Age, clone[0].Age);
            Assert.AreEqual(set[1].Name, clone[1].Name);
            Assert.AreEqual(set[1].Age, clone[1].Age);
            Assert.AreEqual(set[2].Name, clone[2].Name);
            Assert.AreEqual(set[2].Age, clone[2].Age);
            Assert.AreEqual(set[3].Name, clone[3].Name);
            Assert.AreEqual(set[3].Age, clone[3].Age);
        }
        #endregion

        #endregion
    }
}