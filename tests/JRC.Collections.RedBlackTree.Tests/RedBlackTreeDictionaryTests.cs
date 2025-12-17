using JRC.Collections.RedBlackTree;
using JRC.Collections.RedBlackTree.Tests.Serialization.Protobuf;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
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
using System.Text.Json;
using System.Xml.Serialization;

namespace JRC.Collections.RedBlackTree.Tests
{
    [TestClass]
    public class RedBlackTreeDictionaryTests
    {
        public TestContext TestContext { get; set; }

        #region Constructor Tests
        [TestMethod]
        public void Constructor_Default_ShouldCreateEmptyDictionary()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            Assert.AreEqual(0, dict.Count);
        }

        [TestMethod]
        public void Constructor_WithComparer_ShouldUseComparer()
        {
            var dict = new RedBlackTreeDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            dict.Add("ABC", 1);
            Assert.IsTrue(dict.ContainsKey("abc"));
        }

        [TestMethod]
        public void Constructor_WithComparison_ShouldUseComparison()
        {
            var dict = new RedBlackTreeDictionary<int, string>((a, b) => b - a); // reverse order
            dict.Add(1, "one");
            dict.Add(2, "two");
            dict.Add(3, "three");

            var keys = dict.Keys.ToList();
            Assert.AreEqual(3, keys[0]);
            Assert.AreEqual(2, keys[1]);
            Assert.AreEqual(1, keys[2]);
        }
        #endregion

        #region Add Tests
        [TestMethod]
        public void Add_SingleItem_ShouldIncreaseCount()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");
            Assert.AreEqual(1, dict.Count);
        }

        [TestMethod]
        public void Add_MultipleItems_ShouldMaintainSortOrder()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(30, "thirty");
            dict.Add(10, "ten");
            dict.Add(20, "twenty");

            var keys = dict.Keys.ToList();
            Assert.AreEqual(10, keys[0]);
            Assert.AreEqual(20, keys[1]);
            Assert.AreEqual(30, keys[2]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Add_DuplicateKey_ShouldThrow()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");
            dict.Add(1, "uno");
        }

        [TestMethod]
        public void Add_KeyValuePair_ShouldWork()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            ((ICollection<KeyValuePair<int, string>>)dict).Add(new KeyValuePair<int, string>(1, "one"));
            Assert.AreEqual("one", dict[1]);
        }

        [TestMethod]
        public void Add_LargeDataset_ShouldMaintainOrder()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            var random = new Random(42);
            var values = Enumerable.Range(0, 1000).OrderBy(_ => random.Next()).ToList();

            foreach (var v in values)
                dict.Add(v, v.ToString());

            var keys = dict.Keys.ToList();
            for (int i = 0; i < 1000; i++)
                Assert.AreEqual(i, keys[i]);
        }
        #endregion

        #region TryAdd Tests
        [TestMethod]
        public void TryAdd_NewKey_ShouldReturnTrue()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            bool result = dict.TryAdd(1, "one");
            Assert.IsTrue(result);
            Assert.AreEqual(1, dict.Count);
        }

        [TestMethod]
        public void TryAdd_ExistingKey_ShouldReturnFalse()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");
            bool result = dict.TryAdd(1, "uno");
            Assert.IsFalse(result);
            Assert.AreEqual("one", dict[1]);
        }
        #endregion

        #region Remove Tests
        [TestMethod]
        public void Remove_ExistingKey_ShouldReturnTrueAndDecreaseCount()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");
            dict.Add(2, "two");
            dict.Add(3, "three");

            bool removed = dict.Remove(2);

            Assert.IsTrue(removed);
            Assert.AreEqual(2, dict.Count);
            Assert.IsFalse(dict.ContainsKey(2));
        }

        [TestMethod]
        public void Remove_NonExistingKey_ShouldReturnFalse()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");

            bool removed = dict.Remove(999);

            Assert.IsFalse(removed);
            Assert.AreEqual(1, dict.Count);
        }

        [TestMethod]
        public void Remove_KeyValuePair_MatchingValue_ShouldRemove()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");

            bool removed = ((ICollection<KeyValuePair<int, string>>)dict)
                .Remove(new KeyValuePair<int, string>(1, "one"));

            Assert.IsTrue(removed);
            Assert.AreEqual(0, dict.Count);
        }

        [TestMethod]
        public void Remove_KeyValuePair_NonMatchingValue_ShouldNotRemove()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");

            bool removed = ((ICollection<KeyValuePair<int, string>>)dict)
                .Remove(new KeyValuePair<int, string>(1, "wrong"));

            Assert.IsFalse(removed);
            Assert.AreEqual(1, dict.Count);
        }
        #endregion

        #region RemoveAt Tests
        [TestMethod]
        public void RemoveAt_ValidIndex_ShouldRemoveAndReturnItem()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(10, "ten");
            dict.Add(20, "twenty");
            dict.Add(30, "thirty");

            var removed = dict.RemoveAt(1);

            Assert.AreEqual(20, removed.Key);
            Assert.AreEqual("twenty", removed.Value);
            Assert.AreEqual(2, dict.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void RemoveAt_InvalidIndex_ShouldThrow()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");

            dict.RemoveAt(5);
        }

        [TestMethod]
        public void RemoveAt_FirstElement_Repeatedly_ShouldWork()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            for (int i = 0; i < 100; i++)
                dict.Add(i, i.ToString());

            for (int i = 0; i < 100; i++)
            {
                var removed = dict.RemoveAt(0);
                Assert.AreEqual(i, removed.Key);
            }

            Assert.AreEqual(0, dict.Count);
        }
        #endregion

        #region Indexer Tests
        [TestMethod]
        public void Indexer_Get_ExistingKey_ShouldReturnValue()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");

            Assert.AreEqual("one", dict[1]);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void Indexer_Get_NonExistingKey_ShouldThrow()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            var _ = dict[999];
        }

        [TestMethod]
        public void Indexer_Set_ExistingKey_ShouldUpdateValue()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");

            dict[1] = "uno";

            Assert.AreEqual("uno", dict[1]);
            Assert.AreEqual(1, dict.Count);
        }

        [TestMethod]
        public void Indexer_Set_NewKey_ShouldAddEntry()
        {
            var dict = new RedBlackTreeDictionary<int, string>();

            dict[1] = "one";

            Assert.AreEqual("one", dict[1]);
            Assert.AreEqual(1, dict.Count);
        }
        #endregion

        #region GetAt Tests
        [TestMethod]
        public void GetAt_ValidIndex_ShouldReturnCorrectItem()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(30, "thirty");
            dict.Add(10, "ten");
            dict.Add(20, "twenty");

            Assert.AreEqual(10, dict.GetAt(0).Key);
            Assert.AreEqual(20, dict.GetAt(1).Key);
            Assert.AreEqual(30, dict.GetAt(2).Key);
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void GetAt_InvalidIndex_ShouldThrow()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");

            dict.GetAt(5);
        }
        #endregion

        #region ContainsKey Tests
        [TestMethod]
        public void ContainsKey_ExistingKey_ShouldReturnTrue()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(42, "answer");

            Assert.IsTrue(dict.ContainsKey(42));
        }

        [TestMethod]
        public void ContainsKey_NonExistingKey_ShouldReturnFalse()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(42, "answer");

            Assert.IsFalse(dict.ContainsKey(999));
        }

        [TestMethod]
        public void ContainsKey_EmptyDictionary_ShouldReturnFalse()
        {
            var dict = new RedBlackTreeDictionary<int, string>();

            Assert.IsFalse(dict.ContainsKey(42));
        }
        #endregion

        #region Contains KeyValuePair Tests
        [TestMethod]
        public void Contains_MatchingKeyAndValue_ShouldReturnTrue()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");

            bool contains = ((ICollection<KeyValuePair<int, string>>)dict)
                .Contains(new KeyValuePair<int, string>(1, "one"));

            Assert.IsTrue(contains);
        }

        [TestMethod]
        public void Contains_MatchingKeyWrongValue_ShouldReturnFalse()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");

            bool contains = ((ICollection<KeyValuePair<int, string>>)dict)
                .Contains(new KeyValuePair<int, string>(1, "wrong"));

            Assert.IsFalse(contains);
        }

        [TestMethod]
        public void Contains_NonExistingKey_ShouldReturnFalse()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");

            bool contains = ((ICollection<KeyValuePair<int, string>>)dict)
                .Contains(new KeyValuePair<int, string>(999, "whatever"));

            Assert.IsFalse(contains);
        }
        #endregion

        #region TryGetValue Tests
        [TestMethod]
        public void TryGetValue_ExistingKey_ShouldReturnTrueAndValue()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");

            bool found = dict.TryGetValue(1, out string value);

            Assert.IsTrue(found);
            Assert.AreEqual("one", value);
        }

        [TestMethod]
        public void TryGetValue_NonExistingKey_ShouldReturnFalse()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");

            bool found = dict.TryGetValue(999, out string value);

            Assert.IsFalse(found);
            Assert.IsNull(value);
        }
        #endregion

        #region IndexOfKey Tests
        [TestMethod]
        public void IndexOfKey_ExistingKey_ShouldReturnCorrectIndex()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(30, "thirty");
            dict.Add(10, "ten");
            dict.Add(20, "twenty");

            Assert.AreEqual(0, dict.IndexOfKey(10));
            Assert.AreEqual(1, dict.IndexOfKey(20));
            Assert.AreEqual(2, dict.IndexOfKey(30));
        }

        [TestMethod]
        public void IndexOfKey_NonExistingKey_ShouldReturnMinusOne()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");

            Assert.AreEqual(-1, dict.IndexOfKey(999));
        }
        #endregion

        #region Keys Collection Tests
        [TestMethod]
        public void Keys_ShouldReturnAllKeysInOrder()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(30, "thirty");
            dict.Add(10, "ten");
            dict.Add(20, "twenty");

            var keys = dict.Keys.ToList();

            Assert.AreEqual(3, keys.Count);
            Assert.AreEqual(10, keys[0]);
            Assert.AreEqual(20, keys[1]);
            Assert.AreEqual(30, keys[2]);
        }

        [TestMethod]
        public void Keys_Contains_ShouldWork()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");
            dict.Add(2, "two");

            Assert.IsTrue(dict.Keys.Contains(1));
            Assert.IsFalse(dict.Keys.Contains(999));
        }

        [TestMethod]
        public void Keys_Count_ShouldMatchDictionaryCount()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");
            dict.Add(2, "two");
            dict.Add(3, "three");

            Assert.AreEqual(dict.Count, dict.Keys.Count);
        }

        [TestMethod]
        public void Keys_CopyTo_ShouldCopyAllKeys()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(30, "thirty");
            dict.Add(10, "ten");
            dict.Add(20, "twenty");

            var array = new int[3];
            dict.Keys.CopyTo(array, 0);

            Assert.AreEqual(10, array[0]);
            Assert.AreEqual(20, array[1]);
            Assert.AreEqual(30, array[2]);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void Keys_Add_ShouldThrow()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Keys.Add(1);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void Keys_Remove_ShouldThrow()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");
            dict.Keys.Remove(1);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void Keys_Clear_ShouldThrow()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Keys.Clear();
        }
        #endregion

        #region Values Collection Tests
        [TestMethod]
        public void Values_ShouldReturnAllValuesInKeyOrder()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(30, "thirty");
            dict.Add(10, "ten");
            dict.Add(20, "twenty");

            var values = dict.Values.ToList();

            Assert.AreEqual(3, values.Count);
            Assert.AreEqual("ten", values[0]);
            Assert.AreEqual("twenty", values[1]);
            Assert.AreEqual("thirty", values[2]);
        }

        [TestMethod]
        public void Values_Contains_ShouldWork()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");
            dict.Add(2, "two");

            Assert.IsTrue(dict.Values.Contains("one"));
            Assert.IsFalse(dict.Values.Contains("three"));
        }

        [TestMethod]
        public void Values_Count_ShouldMatchDictionaryCount()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");
            dict.Add(2, "two");
            dict.Add(3, "three");

            Assert.AreEqual(dict.Count, dict.Values.Count);
        }

        [TestMethod]
        public void Values_CopyTo_ShouldCopyAllValues()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(30, "thirty");
            dict.Add(10, "ten");
            dict.Add(20, "twenty");

            var array = new string[3];
            dict.Values.CopyTo(array, 0);

            Assert.AreEqual("ten", array[0]);
            Assert.AreEqual("twenty", array[1]);
            Assert.AreEqual("thirty", array[2]);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void Values_Add_ShouldThrow()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Values.Add("one");
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void Values_Remove_ShouldThrow()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");
            dict.Values.Remove("one");
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void Values_Clear_ShouldThrow()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Values.Clear();
        }
        #endregion

        #region Enumeration Tests
        [TestMethod]
        public void GetEnumerator_ShouldEnumerateInKeyOrder()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(30, "thirty");
            dict.Add(10, "ten");
            dict.Add(20, "twenty");

            var items = new List<KeyValuePair<int, string>>();
            foreach (var item in dict)
                items.Add(item);

            Assert.AreEqual(3, items.Count);
            Assert.AreEqual(10, items[0].Key);
            Assert.AreEqual(20, items[1].Key);
            Assert.AreEqual(30, items[2].Key);
        }

        [TestMethod]
        public void Enumerator_AfterModification_ShouldThrow()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");
            dict.Add(2, "two");

            var enumerator = dict.GetEnumerator();
            enumerator.MoveNext();

            dict.Add(3, "three");

            Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [TestMethod]
        public void Enumeration_EmptyDictionary_ShouldNotIterate()
        {
            var dict = new RedBlackTreeDictionary<int, string>();

            int count = 0;
            foreach (var item in dict)
                count++;

            Assert.AreEqual(0, count);
        }
        #endregion

        #region CopyTo Tests
        [TestMethod]
        public void CopyTo_ShouldCopyAllItems()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(30, "thirty");
            dict.Add(10, "ten");
            dict.Add(20, "twenty");

            var array = new KeyValuePair<int, string>[3];
            dict.CopyTo(array, 0);

            Assert.AreEqual(10, array[0].Key);
            Assert.AreEqual(20, array[1].Key);
            Assert.AreEqual(30, array[2].Key);
        }

        [TestMethod]
        public void CopyTo_WithOffset_ShouldCopyAtOffset()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(10, "ten");
            dict.Add(20, "twenty");

            var array = new KeyValuePair<int, string>[5];
            dict.CopyTo(array, 2);

            Assert.AreEqual(default, array[0]);
            Assert.AreEqual(default, array[1]);
            Assert.AreEqual(10, array[2].Key);
            Assert.AreEqual(20, array[3].Key);
            Assert.AreEqual(default, array[4]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CopyTo_NullArray_ShouldThrow()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");

            dict.CopyTo(null, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CopyTo_NegativeIndex_ShouldThrow()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");

            dict.CopyTo(new KeyValuePair<int, string>[1], -1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CopyTo_ArrayTooSmall_ShouldThrow()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");
            dict.Add(2, "two");
            dict.Add(3, "three");

            dict.CopyTo(new KeyValuePair<int, string>[2], 0);
        }
        #endregion

        #region Clear Tests
        [TestMethod]
        public void Clear_ShouldResetDictionary()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, "one");
            dict.Add(2, "two");
            dict.Add(3, "three");

            dict.Clear();

            Assert.AreEqual(0, dict.Count);
            Assert.IsFalse(dict.ContainsKey(1));
        }
        #endregion

        #region IsReadOnly Tests
        [TestMethod]
        public void IsReadOnly_ShouldReturnFalse()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            Assert.IsFalse(dict.IsReadOnly);
        }
        #endregion

        #region Edge Cases
        [TestMethod]
        public void EmptyDictionary_Operations_ShouldNotThrow()
        {
            var dict = new RedBlackTreeDictionary<int, string>();

            Assert.AreEqual(0, dict.Count);
            Assert.IsFalse(dict.ContainsKey(42));
            Assert.IsFalse(dict.Remove(42));
            Assert.AreEqual(-1, dict.IndexOfKey(42));
            Assert.AreEqual(0, dict.Keys.Count);
            Assert.AreEqual(0, dict.Values.Count);
        }

        [TestMethod]
        public void SingleItem_AllOperations_ShouldWork()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(42, "answer");

            Assert.AreEqual(1, dict.Count);
            Assert.IsTrue(dict.ContainsKey(42));
            Assert.AreEqual(0, dict.IndexOfKey(42));
            Assert.AreEqual("answer", dict[42]);
            Assert.AreEqual(42, dict.GetAt(0).Key);

            bool removed = dict.Remove(42);
            Assert.IsTrue(removed);
            Assert.AreEqual(0, dict.Count);
        }

        [TestMethod]
        public void StressTest_AddRemove_ShouldMaintainIntegrity()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            var random = new Random(42);

            // Add 1000 items
            for (int i = 0; i < 1000; i++)
                dict.Add(i, i.ToString());

            // Remove 500 random items
            var keysToRemove = Enumerable.Range(0, 1000).OrderBy(_ => random.Next()).Take(500).ToList();
            foreach (var key in keysToRemove)
                dict.Remove(key);

            // Verify order is maintained
            var keys = dict.Keys.ToList();
            for (int i = 1; i < keys.Count; i++)
                Assert.IsTrue(keys[i] > keys[i - 1], "Order should be maintained");

            // Verify count
            Assert.AreEqual(500, dict.Count);
        }

        [TestMethod]
        public void NullValue_ShouldBeAllowed()
        {
            var dict = new RedBlackTreeDictionary<int, string>();
            dict.Add(1, null);

            Assert.IsNull(dict[1]);
            Assert.IsTrue(dict.TryGetValue(1, out string value));
            Assert.IsNull(value);
        }

        [TestMethod]
        public void NullKey_WhenAllowed_ShouldWork()
        {
            // String keys can be null if comparer handles it
            var dict = new RedBlackTreeDictionary<string, int>(
                Comparer<string>.Create((a, b) =>
                    string.Compare(a ?? "", b ?? "", StringComparison.Ordinal)));

            dict.Add(null, 0);
            dict.Add("a", 1);

            Assert.AreEqual(0, dict[null]);
            Assert.AreEqual(1, dict["a"]);
        }
        #endregion

        #region Successor Chain Tests (Enumeration Performance)
        [TestMethod]
        public void Enumeration_ShouldUseSuccessorChain()
        {
            // This test verifies enumeration works correctly
            // The O(1) per-element performance is verified by the speed test
            var dict = new RedBlackTreeDictionary<int, string>();

            for (int i = 0; i < 100; i++)
                dict.Add(i, i.ToString());

            int expected = 0;
            foreach (var kvp in dict)
            {
                Assert.AreEqual(expected, kvp.Key);
                Assert.AreEqual(expected.ToString(), kvp.Value);
                expected++;
            }

            Assert.AreEqual(100, expected);
        }
        #endregion

        #region SpeedTest Dictionary
        [TestMethod]
        public void SpeedTest_VsSortedDictionary()
        {
            var sortedDict = new SortedDictionary<int, string>();
            var tree = new RedBlackTreeDictionary<int, string>();

            double sortedDictElapsed, treeElapsed;
            var sb = new StringBuilder();
            sb.AppendLine("==================================================================");
            sb.AppendLine("|                    |   SortedDict        |        Tree         |");
            sb.AppendLine("==================================================================");

            // ============================================================
            // Add (sorted order)
            // ============================================================
            const int AddCount = 100000;
            var watch = new Stopwatch();

            watch.Start();
            for (int i = 0; i < AddCount; i++)
            {
                tree.Add(i, i.ToString());
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < AddCount; i++)
            {
                sortedDict.Add(i, i.ToString());
            }
            watch.Stop();
            sortedDictElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" add sorted (x" + AddCount + ")",-20}| {sortedDictElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // Add (random order)
            // ============================================================
            tree.Clear();
            sortedDict.Clear();
            var random = new Random(42);
            var keys = Enumerable.Range(0, AddCount).OrderBy(_ => random.Next()).ToArray();

            watch.Reset();
            watch.Start();
            for (int i = 0; i < AddCount; i++)
            {
                tree.Add(keys[i], keys[i].ToString());
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < AddCount; i++)
            {
                sortedDict.Add(keys[i], keys[i].ToString());
            }
            watch.Stop();
            sortedDictElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" add random (x" + AddCount + ")",-20}| {sortedDictElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // ContainsKey
            // ============================================================
            const int ContainsCount = 100000;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < ContainsCount; i++)
            {
                var _ = tree.ContainsKey(i % AddCount);
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < ContainsCount; i++)
            {
                var _ = sortedDict.ContainsKey(i % AddCount);
            }
            watch.Stop();
            sortedDictElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" containsKey (x" + ContainsCount + ")",-20}| {sortedDictElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // TryGetValue
            // ============================================================
            const int TryGetCount = 100000;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < TryGetCount; i++)
            {
                tree.TryGetValue(i % AddCount, out _);
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < TryGetCount; i++)
            {
                sortedDict.TryGetValue(i % AddCount, out _);
            }
            watch.Stop();
            sortedDictElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" tryGetValue (x" + TryGetCount + ")",-20}| {sortedDictElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // Indexer get
            // ============================================================
            const int IndexerCount = 100000;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < IndexerCount; i++)
            {
                var _ = tree[i % AddCount];
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < IndexerCount; i++)
            {
                var _ = sortedDict[i % AddCount];
            }
            watch.Stop();
            sortedDictElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" this[key] (x" + IndexerCount + ")",-20}| {sortedDictElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // GetAt (index access) - Tree only
            // ============================================================
            const int GetAtCount = 100000;
            random = new Random(42);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < GetAtCount; i++)
            {
                int idx = random.Next(0, tree.Count);
                var _ = tree.GetAt(idx);
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" getAt[i] (x" + GetAtCount + ")",-20}| {"N/A",-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // IndexOfKey - Tree only
            // ============================================================
            const int IndexOfCount = 10000;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < IndexOfCount; i++)
            {
                var _ = tree.IndexOfKey(i % AddCount);
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" indexOfKey (x" + IndexOfCount + ")",-20}| {"N/A",-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // Remove
            // ============================================================
            tree.Clear();
            sortedDict.Clear();
            for (int i = 0; i < AddCount; i++)
            {
                tree.Add(i, i.ToString());
                sortedDict.Add(i, i.ToString());
            }

            var keysToRemove = Enumerable.Range(0, AddCount).OrderBy(_ => random.Next()).ToArray();

            watch.Reset();
            watch.Start();
            for (int i = 0; i < AddCount; i++)
            {
                tree.Remove(keysToRemove[i]);
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < AddCount; i++)
            {
                sortedDict.Remove(keysToRemove[i]);
            }
            watch.Stop();
            sortedDictElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" remove (x" + AddCount + ")",-20}| {sortedDictElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // RemoveAt - Tree only
            // ============================================================
            tree.Clear();
            for (int i = 0; i < AddCount; i++)
            {
                tree.Add(i, i.ToString());
            }

            watch.Reset();
            watch.Start();
            while (tree.Count > 0)
            {
                tree.RemoveAt(0);
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" removeAt (x" + AddCount + ")",-20}| {"N/A",-20}| {treeElapsed,-20}|");
            sb.AppendLine("------------------------------------------------------------------");

            // ============================================================
            // Enumerate
            // ============================================================
            tree.Clear();
            sortedDict.Clear();
            for (int i = 0; i < AddCount; i++)
            {
                tree.Add(i, i.ToString());
                sortedDict.Add(i, i.ToString());
            }

            watch.Reset();
            watch.Start();
            long sum = 0;
            foreach (var kvp in tree)
            {
                sum += kvp.Key;
            }
            watch.Stop();
            treeElapsed = watch.ElapsedMilliseconds;

            watch.Reset();
            watch.Start();
            sum = 0;
            foreach (var kvp in sortedDict)
            {
                sum += kvp.Key;
            }
            watch.Stop();
            sortedDictElapsed = watch.ElapsedMilliseconds;
            sb.AppendLine($"|{" enumerate (x" + AddCount + ")",-20}| {sortedDictElapsed,-20}| {treeElapsed,-20}|");
            sb.AppendLine("==================================================================");

            TestContext.WriteLine(sb.ToString());
        }
        #endregion

        #region Serialize

        #region sub objects
        [Serializable]
        [DataContract]
        [ProtoContract]
        public class ValueItem
        {
            public ValueItem() { }

            public ValueItem(string name, string category)
            {
                Name = name;
                Category = category;
            }
            [DataMember]
            [ProtoMember(1)]
            public string Name { get; set; }
            [DataMember]
            [ProtoMember(2)]
            public string Category { get; set; }
        }
        [Serializable]
        class BinarySerializableComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return StringComparer.OrdinalIgnoreCase.Compare(x, y);
            }
        }

        [ProtoContract]
        public class PublicSerializableComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return this.IgnoreCase ? StringComparer.OrdinalIgnoreCase.Compare(x, y) : StringComparer.Ordinal.Compare(x, y);
            }
            [XmlElement]
            [ProtoMember(1)]
            public bool IgnoreCase { get; set; }
        }
        #endregion

        #region Binary
        [TestMethod]
        public void SerializeBinaryTest()
        {
            var dico = new RedBlackTreeDictionary<int, ValueItem>();
            dico.Add(1, new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add(2, new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add(3, new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add(4, new ValueItem { Name = "Bread", Category = "Food" });
            

            var formatter = new BinaryFormatter { AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple };
            using (var mem = new MemoryStream())
            {
                formatter.Serialize(mem, dico);
                mem.Position = 0;
                var clone = (RedBlackTreeDictionary<int, ValueItem>)formatter.Deserialize(mem);

                Assert.AreEqual(dico.Count, clone.Count);                
                Assert.AreEqual(dico[1].Name, clone[1].Name);
                Assert.AreEqual(dico[1].Category, clone[1].Category);
                Assert.AreEqual(dico[2].Name, clone[2].Name);
                Assert.AreEqual(dico[2].Category, clone[2].Category);
                Assert.AreEqual(dico[3].Name, clone[3].Name);
                Assert.AreEqual(dico[3].Category, clone[3].Category);
                Assert.AreEqual(dico[4].Name, clone[4].Name);
                Assert.AreEqual(dico[4].Category, clone[4].Category);

            }
        }

        [TestMethod]
        public void SerializeBinaryTest_CustomComparer()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(StringComparer.OrdinalIgnoreCase);
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });


            var formatter = new BinaryFormatter { AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple };
            using (var mem = new MemoryStream())
            {
                formatter.Serialize(mem, dico);
                mem.Position = 0;
                var clone = (RedBlackTreeDictionary<string, ValueItem>)formatter.Deserialize(mem);

                Assert.AreEqual(dico.Count, clone.Count);
                Assert.AreEqual(dico["a"].Name, clone["a"].Name);
                Assert.AreEqual(dico["a"].Category, clone["a"].Category);
                Assert.AreEqual(dico["b"].Name, clone["b"].Name);
                Assert.AreEqual(dico["b"].Category, clone["b"].Category);
                Assert.AreEqual(dico["c"].Name, clone["c"].Name);
                Assert.AreEqual(dico["c"].Category, clone["c"].Category);
                Assert.AreEqual(dico["d"].Name, clone["d"].Name);
                Assert.AreEqual(dico["d"].Category, clone["d"].Category);

            }
        }
        #endregion

        #region Xml
        [TestMethod]
        public void SerializeXmlTest()
        {
            var dico = new RedBlackTreeDictionary<int, ValueItem>();
            dico.Add(1, new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add(2, new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add(3, new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add(4, new ValueItem { Name = "Bread", Category = "Food" });

            var serializer = new XmlSerializer(typeof(RedBlackTreeDictionary<int, ValueItem>));
            using (var mem = new MemoryStream())
            {
                serializer.Serialize(mem, dico);
                mem.Position = 0;
                var clone = (RedBlackTreeDictionary<int, ValueItem>)serializer.Deserialize(mem);

                Assert.AreEqual(dico.Count, clone.Count);
                Assert.AreEqual(dico[1].Name, clone[1].Name);
                Assert.AreEqual(dico[1].Category, clone[1].Category);
                Assert.AreEqual(dico[2].Name, clone[2].Name);
                Assert.AreEqual(dico[2].Category, clone[2].Category);
                Assert.AreEqual(dico[3].Name, clone[3].Name);
                Assert.AreEqual(dico[3].Category, clone[3].Category);
                Assert.AreEqual(dico[4].Name, clone[4].Name);
                Assert.AreEqual(dico[4].Category, clone[4].Category);

            }
        }

        [TestMethod]
        public void SerializeXmlTest_CustomManagedComparer()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(StringComparer.OrdinalIgnoreCase);
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });


            var serializer = new XmlSerializer(typeof(RedBlackTreeDictionary<string, ValueItem>));
            using (var mem = new MemoryStream())
            {
                serializer.Serialize(mem, dico);
                mem.Position = 0;
                var clone = (RedBlackTreeDictionary<string, ValueItem>)serializer.Deserialize(mem);

                Assert.AreEqual(dico.Count, clone.Count);
                Assert.AreEqual(dico["a"].Name, clone["a"].Name);
                Assert.AreEqual(dico["a"].Category, clone["a"].Category);
                Assert.AreEqual(dico["b"].Name, clone["b"].Name);
                Assert.AreEqual(dico["b"].Category, clone["b"].Category);
                Assert.AreEqual(dico["c"].Name, clone["c"].Name);
                Assert.AreEqual(dico["c"].Category, clone["c"].Category);
                Assert.AreEqual(dico["d"].Name, clone["d"].Name);
                Assert.AreEqual(dico["d"].Category, clone["d"].Category);

            }
        }
        
        [TestMethod]
        public void SerializeXmlTest_CustomComparerBinarySerializable()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(new BinarySerializableComparer());
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });


            var serializer = new XmlSerializer(typeof(RedBlackTreeDictionary<string, ValueItem>));
            using (var mem = new MemoryStream())
            {
                serializer.Serialize(mem, dico);
                mem.Position = 0;
                var clone = (RedBlackTreeDictionary<string, ValueItem>)serializer.Deserialize(mem);

                Assert.AreEqual(dico.Count, clone.Count);
                Assert.AreEqual(dico["a"].Name, clone["a"].Name);
                Assert.AreEqual(dico["a"].Category, clone["a"].Category);
                Assert.AreEqual(dico["b"].Name, clone["b"].Name);
                Assert.AreEqual(dico["b"].Category, clone["b"].Category);
                Assert.AreEqual(dico["c"].Name, clone["c"].Name);
                Assert.AreEqual(dico["c"].Category, clone["c"].Category);
                Assert.AreEqual(dico["d"].Name, clone["d"].Name);
                Assert.AreEqual(dico["d"].Category, clone["d"].Category);

            }
        }
               
        [TestMethod]
        public void SerializeXmlTest_CustomComparerXmlSerializable()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(new PublicSerializableComparer { IgnoreCase = true });
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });


            var serializer = new XmlSerializer(typeof(RedBlackTreeDictionary<string, ValueItem>));
            using (var mem = new MemoryStream())
            {
                serializer.Serialize(mem, dico);
                mem.Position = 0;
                var clone = (RedBlackTreeDictionary<string, ValueItem>)serializer.Deserialize(mem);

                Assert.AreEqual(dico.Count, clone.Count);
                Assert.AreEqual(dico["a"].Name, clone["a"].Name);
                Assert.AreEqual(dico["a"].Category, clone["a"].Category);
                Assert.AreEqual(dico["b"].Name, clone["b"].Name);
                Assert.AreEqual(dico["b"].Category, clone["b"].Category);
                Assert.AreEqual(dico["c"].Name, clone["c"].Name);
                Assert.AreEqual(dico["c"].Category, clone["c"].Category);
                Assert.AreEqual(dico["d"].Name, clone["d"].Name);
                Assert.AreEqual(dico["d"].Category, clone["d"].Category);

            }
        }
        #endregion

        #region DataContract
        [TestMethod]
        public void SerializeDataContractTest()
        {
            var dico = new RedBlackTreeDictionary<int, ValueItem>();
            dico.Add(1, new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add(2, new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add(3, new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add(4, new ValueItem { Name = "Bread", Category = "Food" });

            var serializer = new DataContractSerializer(typeof(RedBlackTreeDictionary<int, ValueItem>));
            using (var mem = new MemoryStream())
            {
                serializer.WriteObject(mem, dico);
                mem.Position = 0;
                var clone = (RedBlackTreeDictionary<int, ValueItem>)serializer.ReadObject(mem);

                Assert.AreEqual(dico.Count, clone.Count);
                Assert.AreEqual(dico[1].Name, clone[1].Name);
                Assert.AreEqual(dico[1].Category, clone[1].Category);
                Assert.AreEqual(dico[2].Name, clone[2].Name);
                Assert.AreEqual(dico[2].Category, clone[2].Category);
                Assert.AreEqual(dico[3].Name, clone[3].Name);
                Assert.AreEqual(dico[3].Category, clone[3].Category);
                Assert.AreEqual(dico[4].Name, clone[4].Name);
                Assert.AreEqual(dico[4].Category, clone[4].Category);

            }
        }
        [TestMethod]
        public void SerializeDataContractTest_CustomManagedComparer()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(StringComparer.OrdinalIgnoreCase);
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });


            var serializer = new DataContractSerializer(typeof(RedBlackTreeDictionary<string, ValueItem>));
            using (var mem = new MemoryStream())
            {
                serializer.WriteObject(mem, dico);
                mem.Position = 0;
                var clone = (RedBlackTreeDictionary<string, ValueItem>)serializer.ReadObject(mem);

                Assert.AreEqual(dico.Count, clone.Count);
                Assert.AreEqual(dico["a"].Name, clone["a"].Name);
                Assert.AreEqual(dico["a"].Category, clone["a"].Category);
                Assert.AreEqual(dico["b"].Name, clone["b"].Name);
                Assert.AreEqual(dico["b"].Category, clone["b"].Category);
                Assert.AreEqual(dico["c"].Name, clone["c"].Name);
                Assert.AreEqual(dico["c"].Category, clone["c"].Category);
                Assert.AreEqual(dico["d"].Name, clone["d"].Name);
                Assert.AreEqual(dico["d"].Category, clone["d"].Category);

            }
        }

        [TestMethod]
        public void SerializeDataContractTest_CustomComparerBinarySerializable()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(new BinarySerializableComparer());
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });


            var serializer = new DataContractSerializer(typeof(RedBlackTreeDictionary<string, ValueItem>));
            using (var mem = new MemoryStream())
            {
                serializer.WriteObject(mem, dico);
                mem.Position = 0;
                var clone = (RedBlackTreeDictionary<string, ValueItem>)serializer.ReadObject(mem);

                Assert.AreEqual(dico.Count, clone.Count);
                Assert.AreEqual(dico["a"].Name, clone["a"].Name);
                Assert.AreEqual(dico["a"].Category, clone["a"].Category);
                Assert.AreEqual(dico["b"].Name, clone["b"].Name);
                Assert.AreEqual(dico["b"].Category, clone["b"].Category);
                Assert.AreEqual(dico["c"].Name, clone["c"].Name);
                Assert.AreEqual(dico["c"].Category, clone["c"].Category);
                Assert.AreEqual(dico["d"].Name, clone["d"].Name);
                Assert.AreEqual(dico["d"].Category, clone["d"].Category);

            }
        }

        [TestMethod]
        public void SerializeDataContractTest_CustomComparerXmlSerializable()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(new PublicSerializableComparer { IgnoreCase = true });
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });


            var serializer = new DataContractSerializer(typeof(RedBlackTreeDictionary<string, ValueItem>));
            using (var mem = new MemoryStream())
            {
                serializer.WriteObject(mem, dico);
                mem.Position = 0;
                var clone = (RedBlackTreeDictionary<string, ValueItem>)serializer.ReadObject(mem);

                Assert.AreEqual(dico.Count, clone.Count);
                Assert.AreEqual(dico["a"].Name, clone["a"].Name);
                Assert.AreEqual(dico["a"].Category, clone["a"].Category);
                Assert.AreEqual(dico["b"].Name, clone["b"].Name);
                Assert.AreEqual(dico["b"].Category, clone["b"].Category);
                Assert.AreEqual(dico["c"].Name, clone["c"].Name);
                Assert.AreEqual(dico["c"].Category, clone["c"].Category);
                Assert.AreEqual(dico["d"].Name, clone["d"].Name);
                Assert.AreEqual(dico["d"].Category, clone["d"].Category);

            }
        }
        #endregion

        #region Json Text
        [TestMethod]
        public void SerializeJsonTextTest()
        {
            var dico = new RedBlackTreeDictionary<int, ValueItem>();
            dico.Add(1, new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add(2, new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add(3, new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add(4, new ValueItem { Name = "Bread", Category = "Food" });

            var json = System.Text.Json.JsonSerializer.Serialize(dico);
            var clone = System.Text.Json.JsonSerializer.Deserialize<RedBlackTreeDictionary<int, ValueItem>>(json);

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico[1].Name, clone[1].Name);
            Assert.AreEqual(dico[1].Category, clone[1].Category);
            Assert.AreEqual(dico[2].Name, clone[2].Name);
            Assert.AreEqual(dico[2].Category, clone[2].Category);
            Assert.AreEqual(dico[3].Name, clone[3].Name);
            Assert.AreEqual(dico[3].Category, clone[3].Category);
            Assert.AreEqual(dico[4].Name, clone[4].Name);
            Assert.AreEqual(dico[4].Category, clone[4].Category);
        }

        [TestMethod]
        public void SerializeJsonTextTest_CustomManagedComparer()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(StringComparer.OrdinalIgnoreCase);
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });

            var options = new JsonSerializerOptions();
            options.Converters.Add(new Serialization.JsonText.RedBlackTreeDictionaryJsonTextConverter<string, ValueItem>());

            var json = System.Text.Json.JsonSerializer.Serialize(dico, options);
            var clone = System.Text.Json.JsonSerializer.Deserialize<RedBlackTreeDictionary<string, ValueItem>>(json, options);

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico["a"].Name, clone["a"].Name);
            Assert.AreEqual(dico["a"].Category, clone["a"].Category);
            Assert.AreEqual(dico["b"].Name, clone["b"].Name);
            Assert.AreEqual(dico["b"].Category, clone["b"].Category);
            Assert.AreEqual(dico["c"].Name, clone["c"].Name);
            Assert.AreEqual(dico["c"].Category, clone["c"].Category);
            Assert.AreEqual(dico["d"].Name, clone["d"].Name);
            Assert.AreEqual(dico["d"].Category, clone["d"].Category);
        }

        [TestMethod]
        public void SerializeJsonTextTest_CustomComparerBinarySerializable()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(new BinarySerializableComparer());
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });

            var options = new JsonSerializerOptions();
            options.Converters.Add(new Serialization.JsonText.RedBlackTreeDictionaryJsonTextConverter<string, ValueItem>());

            var json = System.Text.Json.JsonSerializer.Serialize(dico, options);
            var clone = System.Text.Json.JsonSerializer.Deserialize<RedBlackTreeDictionary<string, ValueItem>>(json, options);

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico["a"].Name, clone["a"].Name);
            Assert.AreEqual(dico["a"].Category, clone["a"].Category);
            Assert.AreEqual(dico["b"].Name, clone["b"].Name);
            Assert.AreEqual(dico["b"].Category, clone["b"].Category);
            Assert.AreEqual(dico["c"].Name, clone["c"].Name);
            Assert.AreEqual(dico["c"].Category, clone["c"].Category);
            Assert.AreEqual(dico["d"].Name, clone["d"].Name);
            Assert.AreEqual(dico["d"].Category, clone["d"].Category);
        }

        [TestMethod]
        public void SerializeJsonTextTest_CustomComparerJsonSerializable()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(new PublicSerializableComparer { IgnoreCase = true });
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });

            var options = new JsonSerializerOptions();
            options.Converters.Add(new Serialization.JsonText.RedBlackTreeDictionaryJsonTextConverter<string, ValueItem>());

            var json = System.Text.Json.JsonSerializer.Serialize(dico, options);
            var clone = System.Text.Json.JsonSerializer.Deserialize<RedBlackTreeDictionary<string, ValueItem>>(json, options);

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico["a"].Name, clone["a"].Name);
            Assert.AreEqual(dico["a"].Category, clone["a"].Category);
            Assert.AreEqual(dico["b"].Name, clone["b"].Name);
            Assert.AreEqual(dico["b"].Category, clone["b"].Category);
            Assert.AreEqual(dico["c"].Name, clone["c"].Name);
            Assert.AreEqual(dico["c"].Category, clone["c"].Category);
            Assert.AreEqual(dico["d"].Name, clone["d"].Name);
            Assert.AreEqual(dico["d"].Category, clone["d"].Category);
        }



        #endregion

        #region Json Newton
        [TestMethod]
        public void SerializeJsonNewtonTest()
        {
            var dico = new RedBlackTreeDictionary<int, ValueItem>();
            dico.Add(1, new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add(2, new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add(3, new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add(4, new ValueItem { Name = "Bread", Category = "Food" });

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(dico);
            var clone = Newtonsoft.Json.JsonConvert.DeserializeObject<RedBlackTreeDictionary<int, ValueItem>>(json);

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico[1].Name, clone[1].Name);
            Assert.AreEqual(dico[1].Category, clone[1].Category);
            Assert.AreEqual(dico[2].Name, clone[2].Name);
            Assert.AreEqual(dico[2].Category, clone[2].Category);
            Assert.AreEqual(dico[3].Name, clone[3].Name);
            Assert.AreEqual(dico[3].Category, clone[3].Category);
            Assert.AreEqual(dico[4].Name, clone[4].Name);
            Assert.AreEqual(dico[4].Category, clone[4].Category);
        }


        [TestMethod]
        public void SerializeJsonNewtonTest_CustomManagedComparer()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(StringComparer.OrdinalIgnoreCase);
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });


            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new Serialization.Newton.RedBlackTreeDictionaryJsonNewtonConverter<string, ValueItem>());

            var json = JsonConvert.SerializeObject(dico, settings);
            var clone = Newtonsoft.Json.JsonConvert.DeserializeObject<RedBlackTreeDictionary<string, ValueItem>>(json, settings);

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico["a"].Name, clone["a"].Name);
            Assert.AreEqual(dico["a"].Category, clone["a"].Category);
            Assert.AreEqual(dico["b"].Name, clone["b"].Name);
            Assert.AreEqual(dico["b"].Category, clone["b"].Category);
            Assert.AreEqual(dico["c"].Name, clone["c"].Name);
            Assert.AreEqual(dico["c"].Category, clone["c"].Category);
            Assert.AreEqual(dico["d"].Name, clone["d"].Name);
            Assert.AreEqual(dico["d"].Category, clone["d"].Category);
        }

        [TestMethod]
        public void SerializeJsonNewtonTest_CustomComparerBinarySerializable()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(new BinarySerializableComparer());
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });


            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new Serialization.Newton.RedBlackTreeDictionaryJsonNewtonConverter<string, ValueItem>());

            var json = JsonConvert.SerializeObject(dico, settings);
            var clone = Newtonsoft.Json.JsonConvert.DeserializeObject<RedBlackTreeDictionary<string, ValueItem>>(json, settings);

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico["a"].Name, clone["a"].Name);
            Assert.AreEqual(dico["a"].Category, clone["a"].Category);
            Assert.AreEqual(dico["b"].Name, clone["b"].Name);
            Assert.AreEqual(dico["b"].Category, clone["b"].Category);
            Assert.AreEqual(dico["c"].Name, clone["c"].Name);
            Assert.AreEqual(dico["c"].Category, clone["c"].Category);
            Assert.AreEqual(dico["d"].Name, clone["d"].Name);
            Assert.AreEqual(dico["d"].Category, clone["d"].Category);
        }

        [TestMethod]
        public void SerializeJsonNewtonTest_CustomComparerJsonSerializable()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(new PublicSerializableComparer { IgnoreCase = true });
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });


            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new Serialization.Newton.RedBlackTreeDictionaryJsonNewtonConverter<string, ValueItem>());

            var json = JsonConvert.SerializeObject(dico, settings);
            var clone = Newtonsoft.Json.JsonConvert.DeserializeObject<RedBlackTreeDictionary<string, ValueItem>>(json, settings);

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico["a"].Name, clone["a"].Name);
            Assert.AreEqual(dico["a"].Category, clone["a"].Category);
            Assert.AreEqual(dico["b"].Name, clone["b"].Name);
            Assert.AreEqual(dico["b"].Category, clone["b"].Category);
            Assert.AreEqual(dico["c"].Name, clone["c"].Name);
            Assert.AreEqual(dico["c"].Category, clone["c"].Category);
            Assert.AreEqual(dico["d"].Name, clone["d"].Name);
            Assert.AreEqual(dico["d"].Category, clone["d"].Category);
        }
        #endregion

        #region MessagePack
        [TestMethod]
        public void SerializeMessagePackTest()
        {
            var dico = new RedBlackTreeDictionary<int, ValueItem>();
            dico.Add(1, new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add(2, new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add(3, new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add(4, new ValueItem { Name = "Bread", Category = "Food" });

            var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
            
            var bytes = MessagePackSerializer.Serialize(dico);
            var clone = MessagePackSerializer.Deserialize<RedBlackTreeDictionary<int, ValueItem>>(bytes);

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico[1].Name, clone[1].Name);
            Assert.AreEqual(dico[1].Category, clone[1].Category);
            Assert.AreEqual(dico[2].Name, clone[2].Name);
            Assert.AreEqual(dico[2].Category, clone[2].Category);
            Assert.AreEqual(dico[3].Name, clone[3].Name);
            Assert.AreEqual(dico[3].Category, clone[3].Category);
            Assert.AreEqual(dico[4].Name, clone[4].Name);
            Assert.AreEqual(dico[4].Category, clone[4].Category);
        }
        [TestMethod]
        public void SerializeMessagePackTest_CustomManagedComparer()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(StringComparer.OrdinalIgnoreCase);
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });

            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                new[] { new Serialization.MessagePack.RedBlackTreeDictionaryMessagePackFormatter<string, ValueItem>() },
                new[] { MessagePack.Resolvers.ContractlessStandardResolver.Instance }
            );

            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

            var bytes = MessagePackSerializer.Serialize(dico, options);
            var clone = MessagePackSerializer.Deserialize<RedBlackTreeDictionary<string, ValueItem>>(bytes, options);

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico["a"].Name, clone["a"].Name);
            Assert.AreEqual(dico["a"].Category, clone["a"].Category);
            Assert.AreEqual(dico["b"].Name, clone["b"].Name);
            Assert.AreEqual(dico["b"].Category, clone["b"].Category);
            Assert.AreEqual(dico["c"].Name, clone["c"].Name);
            Assert.AreEqual(dico["c"].Category, clone["c"].Category);
            Assert.AreEqual(dico["d"].Name, clone["d"].Name);
            Assert.AreEqual(dico["d"].Category, clone["d"].Category);
        }

        [TestMethod]
        public void SerializeMessagePackTest_CustomComparerBinarySerializable()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(new BinarySerializableComparer());
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });

            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                new[] { new Serialization.MessagePack.RedBlackTreeDictionaryMessagePackFormatter<string, ValueItem>() },
                new[] { MessagePack.Resolvers.ContractlessStandardResolver.Instance }
            );

            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

            var bytes = MessagePackSerializer.Serialize(dico, options);
            var clone = MessagePackSerializer.Deserialize<RedBlackTreeDictionary<string, ValueItem>>(bytes, options);

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico["a"].Name, clone["a"].Name);
            Assert.AreEqual(dico["a"].Category, clone["a"].Category);
            Assert.AreEqual(dico["b"].Name, clone["b"].Name);
            Assert.AreEqual(dico["b"].Category, clone["b"].Category);
            Assert.AreEqual(dico["c"].Name, clone["c"].Name);
            Assert.AreEqual(dico["c"].Category, clone["c"].Category);
            Assert.AreEqual(dico["d"].Name, clone["d"].Name);
            Assert.AreEqual(dico["d"].Category, clone["d"].Category);
        }

        [TestMethod]
        public void SerializeMessagePackTest_CustomComparerMessagePackSerializable()
        {
            var dico = new RedBlackTreeDictionary<string, ValueItem>(new PublicSerializableComparer { IgnoreCase = true });
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });

            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                new[] { new Serialization.MessagePack.RedBlackTreeDictionaryMessagePackFormatter<string, ValueItem>() },
                new[] { MessagePack.Resolvers.ContractlessStandardResolver.Instance }
            );

            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

            var bytes = MessagePackSerializer.Serialize(dico, options);
            var clone = MessagePackSerializer.Deserialize<RedBlackTreeDictionary<string, ValueItem>>(bytes, options);

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico["a"].Name, clone["a"].Name);
            Assert.AreEqual(dico["a"].Category, clone["a"].Category);
            Assert.AreEqual(dico["b"].Name, clone["b"].Name);
            Assert.AreEqual(dico["b"].Category, clone["b"].Category);
            Assert.AreEqual(dico["c"].Name, clone["c"].Name);
            Assert.AreEqual(dico["c"].Category, clone["c"].Category);
            Assert.AreEqual(dico["d"].Name, clone["d"].Name);
            Assert.AreEqual(dico["d"].Category, clone["d"].Category);
        }
        #endregion

        #region Protobuf       
        [TestMethod]
        public void SerializeProtoBufTest()
        {
            var dico = new RedBlackTreeDictionary<int, ValueItem>();
            dico.Add(1, new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add(2, new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add(3, new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add(4, new ValueItem { Name = "Bread", Category = "Food" });

            RedBlackTreeDictionary<int, ValueItem> clone;
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, dico);
                mem.Position = 0;
                clone = ProtoBuf.Serializer.Deserialize<RedBlackTreeDictionary<int, ValueItem>>(mem);
            }

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico[1].Name, clone[1].Name);
            Assert.AreEqual(dico[1].Category, clone[1].Category);
            Assert.AreEqual(dico[2].Name, clone[2].Name);
            Assert.AreEqual(dico[2].Category, clone[2].Category);
            Assert.AreEqual(dico[3].Name, clone[3].Name);
            Assert.AreEqual(dico[3].Category, clone[3].Category);
            Assert.AreEqual(dico[4].Name, clone[4].Name);
            Assert.AreEqual(dico[4].Category, clone[4].Category);
        }

        [TestMethod]
        public void SerializeProtoBufTest_CustomManagedComparer()
        {
            // uses surrogate, see TestInitializer
            var dico = new RedBlackTreeDictionary<string, ValueItem>(StringComparer.OrdinalIgnoreCase);
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });

            RedBlackTreeDictionary<string, ValueItem> clone;
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, dico);
                mem.Position = 0;
                clone = ProtoBuf.Serializer.Deserialize<RedBlackTreeDictionary<string, ValueItem>>(mem);
            }

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico["a"].Name, clone["a"].Name);
            Assert.AreEqual(dico["a"].Category, clone["a"].Category);
            Assert.AreEqual(dico["b"].Name, clone["b"].Name);
            Assert.AreEqual(dico["b"].Category, clone["b"].Category);
            Assert.AreEqual(dico["c"].Name, clone["c"].Name);
            Assert.AreEqual(dico["c"].Category, clone["c"].Category);
            Assert.AreEqual(dico["d"].Name, clone["d"].Name);
            Assert.AreEqual(dico["d"].Category, clone["d"].Category);
        }

        [TestMethod]
        public void SerializeProtoBufTest_CustomComparerBinarySerializable()
        {
            // uses surrogate, see TestInitializer
            var dico = new RedBlackTreeDictionary<string, ValueItem>(new BinarySerializableComparer());
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });

            RedBlackTreeDictionary<string, ValueItem> clone;
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, dico);
                mem.Position = 0;
                clone = ProtoBuf.Serializer.Deserialize<RedBlackTreeDictionary<string, ValueItem>>(mem);
            }

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico["a"].Name, clone["a"].Name);
            Assert.AreEqual(dico["a"].Category, clone["a"].Category);
            Assert.AreEqual(dico["b"].Name, clone["b"].Name);
            Assert.AreEqual(dico["b"].Category, clone["b"].Category);
            Assert.AreEqual(dico["c"].Name, clone["c"].Name);
            Assert.AreEqual(dico["c"].Category, clone["c"].Category);
            Assert.AreEqual(dico["d"].Name, clone["d"].Name);
            Assert.AreEqual(dico["d"].Category, clone["d"].Category);
        }

        [TestMethod]
        public void SerializeProtoBufTest_CustomComparerProtobufSerializable()
        {
            // uses surrogate, see TestInitializer
            var dico = new RedBlackTreeDictionary<string, ValueItem>(new PublicSerializableComparer { IgnoreCase = true });
            dico.Add("a", new ValueItem { Name = "Water", Category = "Drink" });
            dico.Add("B", new ValueItem { Name = "Beer", Category = "Drink" });
            dico.Add("c", new ValueItem { Name = "Meat", Category = "Food" });
            dico.Add("D", new ValueItem { Name = "Bread", Category = "Food" });

            RedBlackTreeDictionary<string, ValueItem> clone;
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, dico);
                mem.Position = 0;
                clone = ProtoBuf.Serializer.Deserialize<RedBlackTreeDictionary<string, ValueItem>>(mem);
            }

            Assert.AreEqual(dico.Count, clone.Count);
            Assert.AreEqual(dico["a"].Name, clone["a"].Name);
            Assert.AreEqual(dico["a"].Category, clone["a"].Category);
            Assert.AreEqual(dico["b"].Name, clone["b"].Name);
            Assert.AreEqual(dico["b"].Category, clone["b"].Category);
            Assert.AreEqual(dico["c"].Name, clone["c"].Name);
            Assert.AreEqual(dico["c"].Category, clone["c"].Category);
            Assert.AreEqual(dico["d"].Name, clone["d"].Name);
            Assert.AreEqual(dico["d"].Category, clone["d"].Category);
        }
        #endregion

        #endregion
    }
}