using JRC.Collections.RedBlackTree;
using JRC.Collections.RedBlackTree.Tests.Serialization.Protobuf;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace JRC.Collections.RedBlackTree.Tests
{
    [TestClass]
    public class RedBlackTreeSetSortedKeyTests
    {
        #region Test Classes
        [Serializable]
        [DataContract]
        [ProtoContract]
        [ProtoInclude(100, typeof(Employee))]
        public class Person
        {
            public Person()
            {
            }

            public Person(string name, int age)
            {
                Name = name;
                Age = age;
            }

            [DataMember]
            [ProtoMember(1)]
            public string Name { get; set; }
            [DataMember]
            [ProtoMember(2)]
            public int Age { get; set; }
        }

        [Serializable]
        [DataContract]
        [ProtoContract]
        public class Employee : Person
        {
            [DataMember]
            [ProtoMember(1)]
            public int Id { get; set; }
           
            [DataMember]
            [ProtoMember(2)]
            public string Department { get; set; }

            public Employee(int id, string name, int age, string department = null)
                : base(name, age)
            {
                Id = id;
                Department = department;
            }            

            public override string ToString() => $"{Name} ({Age}) - {Department}";
        }
        [Serializable]
        [DataContract]
        [ProtoContract]
        public class Product
        {
            [DataMember]
            [ProtoMember(1)]
            public string Sku { get; set; }
            [DataMember]
            [ProtoMember(2)]
            public string Name { get; set; }
            [DataMember]
            [ProtoMember(3)]
            public decimal Price { get; set; }

            public Product(string sku, string name, decimal price)
            {
                Sku = sku;
                Name = name;
                Price = price;
            }

            public override string ToString() => $"{Sku}: {Name} @ {Price:C}";
        }
        #endregion

        #region Constructor Tests
        [TestMethod]
        public void Constructor_WithComparison_ShouldCreateEmptyTree()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            Assert.AreEqual(0, set.Count);
        }

        [TestMethod]
        public void Constructor_WithComparer_ShouldCreateEmptyTree()
        {
            var set = new RedBlackTreeSet<Employee, int>(p => p.Age);
            Assert.AreEqual(0, set.Count);
        }

        [TestMethod]
        public void Constructor_Unique_ShouldSetProperty()
        {
            var set = new RedBlackTreeSet<Employee, int>(true, p => p.Age, (a, b) => a - b);
            Assert.IsTrue(set.AllowDuplicates);

            var nonUnique = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            Assert.IsFalse(nonUnique.AllowDuplicates);
        }
        #endregion

        #region Add Tests
        [TestMethod]
        public void Add_SingleItem_ShouldIncreaseCount()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 30));

            Assert.AreEqual(1, set.Count);
        }

        [TestMethod]
        public void Add_MultipleItems_ShouldSortByExtractedValue()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 30));
            set.Add(new Employee(2, "Bob", 20));
            set.Add(new Employee(3, "Charlie", 25));

            var items = set.ToArray();
            Assert.AreEqual("Bob", items[0].Name);      // Age 20
            Assert.AreEqual("Charlie", items[1].Name);  // Age 25
            Assert.AreEqual("Alice", items[2].Name);    // Age 30
        }

        [TestMethod]
        public void Add_DuplicateValues_WhenAllowed_ShouldAddAll()
        {
            var set = new RedBlackTreeSet<Employee, int>(true, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 25));
            set.Add(new Employee(2, "Bob", 25));
            set.Add(new Employee(3, "Charlie", 25));
            set.Add(new Employee(4, "John", 27));
            set.Add(new Employee(5, "Carl", 27));

            Assert.AreEqual(5, set.Count);
            Assert.IsTrue(set.HasDuplicates);
        }

        [TestMethod]
        [ExpectedException(typeof(System.Data.ConstraintException))]
        public void Add_DuplicateValues_WhenUnique_ShouldThrow()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 25));
            set.Add(new Employee(2, "Bob", 25)); // Same age, should throw
        }

        [TestMethod]
        public void Add_DescendingOrder_ShouldMaintainSort()
        {
            var set = new RedBlackTreeSet<int, int>(false, x => x, (a, b) => a - b);
            for (int i = 100; i >= 0; i--)
                set.Add(i);

            var items = set.ToArray();
            for (int i = 0; i <= 100; i++)
                Assert.AreEqual(i, items[i]);
        }
        #endregion

        #region FindKey Tests
        [TestMethod]
        public void FindKey_ExistingValue_ShouldReturnKey()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 30));
            set.Add(new Employee(2, "Bob", 25));
            set.Add(new Employee(3, "Charlie", 20));

            var found = set.FindKey(25);

            Assert.IsNotNull(found);
            Assert.AreEqual("Bob", found.Name);
        }

        [TestMethod]
        public void FindKey_NonExistingValue_ShouldReturnDefault()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 30));

            var found = set.FindKey(99);

            Assert.IsNull(found);
        }

        [TestMethod]
        public void FindKey_EmptySet_ShouldReturnDefault()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);

            var found = set.FindKey(25);

            Assert.IsNull(found);
        }

        [TestMethod]
        public void FindKey_StringValue_ShouldWork()
        {
            var set = new RedBlackTreeSet<Employee, string>(false, p => p.Name, StringComparer.Ordinal);
            set.Add(new Employee(1, "Alice", 30));
            set.Add(new Employee(2, "Bob", 25));
            set.Add(new Employee(3, "Charlie", 20));

            var found = set.FindKey("Bob");

            Assert.IsNotNull(found);
            Assert.AreEqual(25, found.Age);
        }

        [TestMethod]
        public void FindKey_DecimalValue_ShouldWork()
        {
            var set = new RedBlackTreeSet<Product, decimal>(false, p => p.Price, (a, b) => a.CompareTo(b));
            set.Add(new Product("SKU1", "Widget", 9.99m));
            set.Add(new Product("SKU2", "Gadget", 19.99m));
            set.Add(new Product("SKU3", "Gizmo", 14.99m));

            var found = set.FindKey(14.99m);

            Assert.IsNotNull(found);
            Assert.AreEqual("Gizmo", found.Name);
        }
        #endregion

        #region FindKeys Tests
        [TestMethod]
        public void FindKeys_WithDuplicates_ShouldReturnAll()
        {
            var set = new RedBlackTreeSet<Employee, int>(true, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 25));
            set.Add(new Employee(2, "Bob", 25));
            set.Add(new Employee(3, "Charlie", 25));
            set.Add(new Employee(4, "Diana", 30));

            var found = set.FindKeys(25).ToList();

            Assert.AreEqual(3, found.Count);
            Assert.IsTrue(found.Any(p => p.Name == "Alice"));
            Assert.IsTrue(found.Any(p => p.Name == "Bob"));
            Assert.IsTrue(found.Any(p => p.Name == "Charlie"));
        }

        [TestMethod]
        public void FindKeys_NoDuplicates_ShouldReturnSingle()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 25));
            set.Add(new Employee(2, "Bob", 30));

            var found = set.FindKeys(25).ToList();

            Assert.AreEqual(1, found.Count);
            Assert.AreEqual("Alice", found[0].Name);
        }

        [TestMethod]
        public void FindKeys_NoMatch_ShouldReturnEmpty()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 25));

            var found = set.FindKeys(99).ToList();

            Assert.AreEqual(0, found.Count);
        }

        [TestMethod]
        public void FindKeys_ManyDuplicates_ShouldReturnAll()
        {
            var set = new RedBlackTreeSet<Employee, string>(true, p => p.Department, StringComparer.Ordinal);

            //for (int i = 0; i < 1; i++)
            //    set.Add(new Person(i, $"Dev{i}", 25 + i % 10, "Engineering"));
            //for (int i = 50; i < 51; i++)
            //    set.Add(new Person(i, $"Sales{i}", 30 + i % 10, "Sales"));
            //for (int i = 80; i < 81; i++)
            //    set.Add(new Person(i, $"HR{i}", 35 + i % 5, "HR"));

            for (int i = 0; i < 50; i++)
                set.Add(new Employee(i, $"Dev{i}", 25 + i % 10, "Engineering"));
            for (int i = 50; i < 80; i++)
                set.Add(new Employee(i, $"Sales{i}", 30 + i % 10, "Sales"));
            for (int i = 80; i < 100; i++)
                set.Add(new Employee(i, $"HR{i}", 35 + i % 5, "HR"));

            var engineers = set.FindKeys("Engineering").ToList();
            var salesPeople = set.FindKeys("Sales").ToList();
            var hrPeople = set.FindKeys("HR").ToList();

            Assert.AreEqual(50, engineers.Count);
            Assert.AreEqual(30, salesPeople.Count);
            Assert.AreEqual(20, hrPeople.Count);
        }
        #endregion

        #region FindRange Tests
        [TestMethod]
        public void FindRange_ShouldReturnItemsInRange()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            for (int i = 18; i <= 65; i++)
                set.Add(new Employee(i, $"Person{i}", i));

            var young = set.FindRange(18, 25).ToList();

            Assert.AreEqual(8, young.Count); // 18 to 25 inclusive
            Assert.IsTrue(young.All(p => p.Age >= 18 && p.Age <= 25));
        }

        [TestMethod]
        public void FindRange_EmptyRange_ShouldReturnEmpty()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 20));
            set.Add(new Employee(2, "Bob", 40));

            var range = set.FindRange(25, 35).ToList();

            Assert.AreEqual(0, range.Count);
        }

        [TestMethod]
        public void FindRange_SingleValueRange_ShouldReturnMatching()
        {
            var set = new RedBlackTreeSet<Employee, int>(true, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 25));
            set.Add(new Employee(2, "Bob", 25));
            set.Add(new Employee(3, "Charlie", 25));

            var range = set.FindRange(25, 25).ToList();

            Assert.AreEqual(3, range.Count);
        }

        [TestMethod]
        public void FindRange_PriceRange_ShouldWork()
        {
            var set = new RedBlackTreeSet<Product, decimal>(false, p => p.Price, (a, b) => a.CompareTo(b));
            set.Add(new Product("SKU1", "Cheap", 5.00m));
            set.Add(new Product("SKU2", "Medium", 15.00m));
            set.Add(new Product("SKU3", "Pricey", 25.00m));
            set.Add(new Product("SKU4", "Expensive", 100.00m));

            var affordable = set.FindRange(10.00m, 50.00m).ToList();

            Assert.AreEqual(2, affordable.Count);
            Assert.IsTrue(affordable.Any(p => p.Name == "Medium"));
            Assert.IsTrue(affordable.Any(p => p.Name == "Pricey"));
        }
        #endregion

        #region FindRangeByIndex Tests
        [TestMethod]
        public void FindRangeByIndex_ShouldReturnCorrectSlice()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            for (int i = 0; i < 100; i++)
                set.Add(new Employee(i, $"Person{i}", i));

            var range = set.FindRangeByIndex(10, 5).ToList();

            Assert.AreEqual(5, range.Count);
            Assert.AreEqual(10, range[0].Age);
            Assert.AreEqual(14, range[4].Age);
        }

        [TestMethod]
        public void FindRangeByIndex_Pagination_ShouldWork()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            for (int i = 0; i < 100; i++)
                set.Add(new Employee(i, $"Person{i}", i));

            int pageSize = 10;

            // Page 1
            var page1 = set.FindRangeByIndex(0, pageSize).ToList();
            Assert.AreEqual(10, page1.Count);
            Assert.AreEqual(0, page1.First().Age);
            Assert.AreEqual(9, page1.Last().Age);

            // Page 2
            var page2 = set.FindRangeByIndex(10, pageSize).ToList();
            Assert.AreEqual(10, page2.Count);
            Assert.AreEqual(10, page2.First().Age);
            Assert.AreEqual(19, page2.Last().Age);

            // Last page (partial)
            var lastPage = set.FindRangeByIndex(95, pageSize).ToList();
            Assert.AreEqual(5, lastPage.Count);
            Assert.AreEqual(95, lastPage.First().Age);
            Assert.AreEqual(99, lastPage.Last().Age);
        }
        #endregion

        #region Remove Tests
        [TestMethod]
        public void Remove_ExistingKey_ShouldRemove()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            var alice = new Employee(1, "Alice", 30);
            set.Add(alice);

            bool removed = set.Remove(alice);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, set.Count);
        }

        [TestMethod]
        public void Remove_FromDuplicates_ShouldRemoveOne()
        {
            var set = new RedBlackTreeSet<Employee, int>(true, p => p.Age, (a, b) => a - b);
            var alice = new Employee(1, "Alice", 25);
            var bob = new Employee(2, "Bob", 25);
            set.Add(alice);
            set.Add(bob);

            set.Remove(alice);

            Assert.AreEqual(1, set.Count);
            var remaining = set.FindKey(25);
            Assert.AreEqual("Bob", remaining.Name);
        }
        #endregion

        #region RemoveAt Tests
        [TestMethod]
        public void RemoveAt_ValidIndex_ShouldRemoveCorrectItem()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 30));
            set.Add(new Employee(2, "Bob", 20));
            set.Add(new Employee(3, "Charlie", 25));

            var removed = set.RemoveAt(1); // Should remove Charlie (age 25, middle)

            Assert.AreEqual("Charlie", removed.Name);
            Assert.AreEqual(2, set.Count);
        }
        #endregion

        #region Indexer Tests
        [TestMethod]
        public void Indexer_ShouldReturnItemsSortedByValue()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 30));
            set.Add(new Employee(2, "Bob", 20));
            set.Add(new Employee(3, "Charlie", 25));

            Assert.AreEqual("Bob", set[0].Name);      // Youngest
            Assert.AreEqual("Charlie", set[1].Name);
            Assert.AreEqual("Alice", set[2].Name);   // Oldest
        }
        #endregion

        #region IndexOf Tests
        [TestMethod]
        public void IndexOf_ShouldReturnCorrectSortedPosition()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            var alice = new Employee(1, "Alice", 30);
            var bob = new Employee(2, "Bob", 20);
            var charlie = new Employee(3, "Charlie", 25);

            set.Add(alice);
            set.Add(bob);
            set.Add(charlie);

            Assert.AreEqual(0, set.IndexOf(bob));       // Youngest
            Assert.AreEqual(1, set.IndexOf(charlie));
            Assert.AreEqual(2, set.IndexOf(alice));     // Oldest
        }
        #endregion

        #region Contains Tests
        [TestMethod]
        public void Contains_ExistingKey_ShouldReturnTrue()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            var alice = new Employee(1, "Alice", 30);
            set.Add(alice);

            Assert.IsTrue(set.Contains(alice));
        }

        [TestMethod]
        public void Contains_NonExistingKey_ShouldReturnFalse()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 30));

            Assert.IsFalse(set.Contains(new Employee(2, "Bob", 25)));
        }
        #endregion

        #region Enumeration Tests
        [TestMethod]
        public void Enumeration_ShouldBeInSortedOrder()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            var random = new Random(42);
            var ages = Enumerable.Range(18, 50).OrderBy(_ => random.Next()).ToList();

            foreach (var age in ages)
                set.Add(new Employee(age, $"Person{age}", age));

            var items = set.ToList();
            for (int i = 1; i < items.Count; i++)
                Assert.IsTrue(items[i].Age > items[i - 1].Age);
        }

        [TestMethod]
        public void Enumeration_WithDuplicates_ShouldIncludeAll()
        {
            var set = new RedBlackTreeSet<Employee, int>(true, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 25));
            set.Add(new Employee(2, "Bob", 25));
            set.Add(new Employee(3, "Charlie", 25));

            var items = set.ToList();

            Assert.AreEqual(3, items.Count);
        }
        #endregion

        #region CopyTo Tests
        [TestMethod]
        public void CopyTo_ShouldCopyInSortedOrder()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 30));
            set.Add(new Employee(2, "Bob", 20));
            set.Add(new Employee(3, "Charlie", 25));

            var array = new Employee[3];
            set.CopyTo(array, 0);

            Assert.AreEqual("Bob", array[0].Name);
            Assert.AreEqual("Charlie", array[1].Name);
            Assert.AreEqual("Alice", array[2].Name);
        }

        [TestMethod]
        public void CopyTo_WithDuplicates_ShouldCopyAll()
        {
            var set = new RedBlackTreeSet<Employee, int>(true, p => p.Age, (a, b) => a - b);
            set.Add(new Employee(1, "Alice", 25));
            set.Add(new Employee(2, "Bob", 25));

            var array = new Employee[2];
            set.CopyTo(array, 0);

            Assert.AreEqual(2, array.Count(p => p != null));
        }
        #endregion

        #region Edge Cases
        [TestMethod]
        public void EmptySet_AllOperations_ShouldWork()
        {
            var set = new RedBlackTreeSet<Employee, int>(false, p => p.Age, (a, b) => a - b);

            Assert.AreEqual(0, set.Count);
            Assert.IsFalse(set.HasDuplicates);
            Assert.IsNull(set.FindKey(25));
            Assert.AreEqual(0, set.FindKeys(25).Count());
            Assert.AreEqual(0, set.FindRange(20, 30).Count());
            Assert.AreEqual(0, set.ToArray().Length);
        }

        [TestMethod]
        public void NullValueProvider_ShouldHandleGracefully()
        {
            var set = new RedBlackTreeSet<Employee, string>(false, p => p.Department, StringComparer.Ordinal);
            set.Add(new Employee(1, "Alice", 30, "Engineering"));
            set.Add(new Employee(2, "Bob", 25, null)); // null department

            Assert.AreEqual(2, set.Count);
        }

        [TestMethod]
        public void LargeDataset_ShouldMaintainPerformance()
        {
            var set = new RedBlackTreeSet<Employee, int>(true, p => p.Age, (a, b) => a - b);
            var random = new Random(42);

            // Add 10000 items
            for (int i = 0; i < 10000; i++)
                set.Add(new Employee(i, $"Person{i}", random.Next(18, 100)));

            // Verify count
            Assert.AreEqual(10000, set.Count);

            // Verify sorted order
            var items = set.ToArray();
            for (int i = 1; i < items.Length; i++)
                Assert.IsTrue(items[i].Age >= items[i - 1].Age);
        }

        [TestMethod]
        public void StressTest_AddFindRemove_ShouldWork()
        {
            var set = new RedBlackTreeSet<Employee, int>(true, p => p.Age, (a, b) => a - b);

            // Add
            for (int i = 0; i < 1000; i++)
                set.Add(new Employee(i, $"Person{i}", i % 100));

            // Find
            for (int age = 0; age < 100; age++)
            {
                var found = set.FindKeys(age).ToList();
                Assert.AreEqual(10, found.Count); // 1000/100 = 10 per age
            }

            // Remove half
            for (int i = 0; i < 500; i++)
                set.RemoveAt(0);

            Assert.AreEqual(500, set.Count);

            // Verify integrity
            var remaining = set.ToArray();
            for (int i = 1; i < remaining.Length; i++)
                Assert.IsTrue(remaining[i].Age >= remaining[i - 1].Age);
        }
        #endregion

        #region Real-World Scenarios
        [TestMethod]
        public void Scenario_EmployeeDirectory_SortedByAge()
        {
            var employees = new RedBlackTreeSet<Employee, int>(true, p => p.Age, (a, b) => a - b);

            employees.Add(new Employee(1, "Junior Dev", 22, "Engineering"));
            employees.Add(new Employee(2, "Senior Dev", 35, "Engineering"));
            employees.Add(new Employee(3, "Manager", 45, "Engineering"));
            employees.Add(new Employee(4, "Intern", 20, "Engineering"));
            employees.Add(new Employee(5, "Tech Lead", 35, "Engineering")); // Same age as Senior Dev

            // Find employees in 30s
            var thirties = employees.FindRange(30, 39).ToList();
            Assert.AreEqual(2, thirties.Count);

            // Get youngest 3
            var youngest = employees.FindRangeByIndex(0, 3).ToList();
            Assert.AreEqual(20, youngest[0].Age);
            Assert.AreEqual(22, youngest[1].Age);
            Assert.AreEqual(35, youngest[2].Age); // One of the 35s
        }

        [TestMethod]
        public void Scenario_ProductCatalog_SortedByPrice()
        {
            var products = new RedBlackTreeSet<Product, decimal>(true, p => p.Price, (a, b) => a.CompareTo(b));

            products.Add(new Product("LAPTOP1", "Budget Laptop", 499.99m));
            products.Add(new Product("LAPTOP2", "Pro Laptop", 1299.99m));
            products.Add(new Product("PHONE1", "Smartphone", 799.99m));
            products.Add(new Product("TABLET1", "Tablet", 499.99m)); // Same price as Budget Laptop

            // Find products under $1000
            var affordable = products.FindRange(0m, 999.99m).ToList();
            Assert.AreEqual(3, affordable.Count);

            // Get cheapest product
            var cheapest = products[0];
            Assert.AreEqual(499.99m, cheapest.Price);
        }
        #endregion

        #region Serialization tests

        #region provider
        [Serializable]
        public class PersonNameProvider : RedBlackTreeSet<Person, string>.ISortKeyProvider
        {
            public string GetSortKey(Person item)
            {
                return item.Name;
            }
        }      
        #endregion

        #region comparers
        [Serializable]
        class BinarySerializableComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return StringComparer.OrdinalIgnoreCase.Compare(x, y);
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
            var set = new RedBlackTreeSet<Person, string>(new PersonNameProvider());
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));


            var formatter = new BinaryFormatter { AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple };
            using (var mem = new MemoryStream())
            {
                formatter.Serialize(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person, string>)formatter.Deserialize(mem);

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
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), new BinarySerializableComparer(), new BinarySerializableSatelliteComparer());
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var formatter = new BinaryFormatter { AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple };
            using (var mem = new MemoryStream())
            {
                formatter.Serialize(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person, string>)formatter.Deserialize(mem);

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
            var set = new RedBlackTreeSet<Person, string>(new PersonNameProvider());
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));


            var serializer = new XmlSerializer(typeof(RedBlackTreeSet<Person, string>));
            using (var mem = new MemoryStream())
            {
                serializer.Serialize(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person, string>)serializer.Deserialize(mem);

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
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), StringComparer.OrdinalIgnoreCase, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var serializer = new XmlSerializer(typeof(RedBlackTreeSet<Person, string>));
            using (var mem = new MemoryStream())
            {
                serializer.Serialize(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person, string>)serializer.Deserialize(mem);

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
        public void SerializeXmlTest_CustomComparerBinarySerializable()
        {
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), new BinarySerializableComparer(), new BinarySerializableSatelliteComparer());
            set.Add(new Person("Charlie", 10));
            set.Add(new Person("CHARLIE", 30));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var serializer = new XmlSerializer(typeof(RedBlackTreeSet<Person, string>));
            using (var mem = new MemoryStream())
            {
                serializer.Serialize(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person, string>)serializer.Deserialize(mem);

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
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), new PublicSerializableComparer { IgnoreCase = true }, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 10));
            set.Add(new Person("CHARLIE", 30));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var serializer = new XmlSerializer(typeof(RedBlackTreeSet<Person, string>));
            using (var mem = new MemoryStream())
            {
                serializer.Serialize(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person, string>)serializer.Deserialize(mem);

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
            var set = new RedBlackTreeSet<Person, string>(new PersonNameProvider());
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var serializer = new DataContractSerializer(typeof(RedBlackTreeSet<Person, string>));
            using (var mem = new MemoryStream())
            {
                serializer.WriteObject(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person, string>)serializer.ReadObject(mem);

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
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), StringComparer.OrdinalIgnoreCase, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));


            var serializer = new DataContractSerializer(typeof(RedBlackTreeSet<Person, string>));
            using (var mem = new MemoryStream())
            {
                serializer.WriteObject(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person, string>)serializer.ReadObject(mem);

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
        public void SerializeDataContractTest_CustomComparerBinarySerializable()
        {
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), new BinarySerializableComparer(), new BinarySerializableSatelliteComparer());
            set.Add(new Person("Charlie", 10));
            set.Add(new Person("CHARLIE", 30));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));


            var serializer = new DataContractSerializer(typeof(RedBlackTreeSet<Person, string>));
            using (var mem = new MemoryStream())
            {
                serializer.WriteObject(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person, string>)serializer.ReadObject(mem);

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
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), new PublicSerializableComparer { IgnoreCase = true }, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 10));
            set.Add(new Person("CHARLIE", 30));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));


            var serializer = new DataContractSerializer(typeof(RedBlackTreeSet<Person, string>));
            using (var mem = new MemoryStream())
            {
                serializer.WriteObject(mem, set);
                mem.Position = 0;
                var clone = (RedBlackTreeSet<Person, string>)serializer.ReadObject(mem);

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
            var set = new RedBlackTreeSet<Person, string>(new PersonNameProvider());
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new Serialization.JsonText.RedBlackTreeSetSortedKeyJsonTextConverter<Person, string>());

            var json = System.Text.Json.JsonSerializer.Serialize(set, options);
            var clone = System.Text.Json.JsonSerializer.Deserialize<RedBlackTreeSet<Person, string>>(json, options);

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
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), StringComparer.OrdinalIgnoreCase, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new Serialization.JsonText.RedBlackTreeSetSortedKeyJsonTextConverter<Person, string>());

            var json = System.Text.Json.JsonSerializer.Serialize(set, options);
            var clone = System.Text.Json.JsonSerializer.Deserialize<RedBlackTreeSet<Person, string>>(json, options);

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
        public void SerializeJsonTextTest_CustomComparerBinarySerializable()
        {
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), new BinarySerializableComparer(), new BinarySerializableSatelliteComparer());
            set.Add(new Person("Charlie", 10));
            set.Add(new Person("CHARLIE", 30));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new Serialization.JsonText.RedBlackTreeSetSortedKeyJsonTextConverter<Person, string>());

            var json = System.Text.Json.JsonSerializer.Serialize(set, options);
            var clone = System.Text.Json.JsonSerializer.Deserialize<RedBlackTreeSet<Person, string>>(json, options);

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
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), new PublicSerializableComparer { IgnoreCase = true }, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 10));
            set.Add(new Person("CHARLIE", 30));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new Serialization.JsonText.RedBlackTreeSetSortedKeyJsonTextConverter<Person, string>());

            var json = System.Text.Json.JsonSerializer.Serialize(set, options);
            var clone = System.Text.Json.JsonSerializer.Deserialize<RedBlackTreeSet<Person, string>>(json, options);

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
            var set = new RedBlackTreeSet<Person, string>(new PersonNameProvider());
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.Converters.Add(new Serialization.Newton.RedBlackTreeSetSortedKeyJsonNewtonConverter<Person, string>());

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(set, settings);
            var clone = Newtonsoft.Json.JsonConvert.DeserializeObject<RedBlackTreeSet<Person, string>>(json, settings);

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
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), StringComparer.OrdinalIgnoreCase, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.Converters.Add(new Serialization.Newton.RedBlackTreeSetSortedKeyJsonNewtonConverter<Person, string>());

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(set, settings);
            var clone = Newtonsoft.Json.JsonConvert.DeserializeObject<RedBlackTreeSet<Person, string>>(json, settings);

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
        public void SerializeJsonNewtonTest_CustomComparerBinarySerializable()
        {
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), new BinarySerializableComparer(), new BinarySerializableSatelliteComparer());
            set.Add(new Person("Charlie", 10));
            set.Add(new Person("CHARLIE", 30));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));


            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.Converters.Add(new Serialization.Newton.RedBlackTreeSetSortedKeyJsonNewtonConverter<Person, string>());

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(set, settings);
            var clone = Newtonsoft.Json.JsonConvert.DeserializeObject<RedBlackTreeSet<Person, string>>(json, settings);

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
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), new PublicSerializableComparer { IgnoreCase = true }, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 10));
            set.Add(new Person("CHARLIE", 30));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));


            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.Converters.Add(new Serialization.Newton.RedBlackTreeSetSortedKeyJsonNewtonConverter<Person, string>());

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(set, settings);
            var clone = Newtonsoft.Json.JsonConvert.DeserializeObject<RedBlackTreeSet<Person, string>>(json, settings);

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
            var set = new RedBlackTreeSet<Person, string>(new PersonNameProvider());
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                new[] { new Serialization.MessagePack.RedBlackTreeSetSortedKeyMessagePackFormatter<Person, string>() },
                new[] { MessagePack.Resolvers.ContractlessStandardResolver.Instance }
            );

            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

            var bytes = MessagePackSerializer.Serialize(set, options);
            var clone = MessagePackSerializer.Deserialize<RedBlackTreeSet<Person, string>>(bytes, options);

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
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), StringComparer.OrdinalIgnoreCase, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                new[] { new Serialization.MessagePack.RedBlackTreeSetSortedKeyMessagePackFormatter<Person, string>() },
                new[] { MessagePack.Resolvers.ContractlessStandardResolver.Instance }
            );

            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

            var bytes = MessagePackSerializer.Serialize(set, options);
            var clone = MessagePackSerializer.Deserialize<RedBlackTreeSet<Person, string>>(bytes, options);

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
        public void SerializeMessagePackTest_CustomComparerBinarySerializable()
        {
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), new BinarySerializableComparer(), new BinarySerializableSatelliteComparer());
            set.Add(new Person("Charlie", 10));
            set.Add(new Person("CHARLIE", 30));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
              new[] { new Serialization.MessagePack.RedBlackTreeSetSortedKeyMessagePackFormatter<Person, string>() },
              new[] { MessagePack.Resolvers.ContractlessStandardResolver.Instance });

            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

            var bytes = MessagePackSerializer.Serialize(set, options);
            var clone = MessagePackSerializer.Deserialize<RedBlackTreeSet<Person, string>>(bytes, options);

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
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), new PublicSerializableComparer { IgnoreCase = true }, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 10));
            set.Add(new Person("CHARLIE", 30));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                 new[] { new Serialization.MessagePack.RedBlackTreeSetSortedKeyMessagePackFormatter<Person, string>() },
                 new[] { MessagePack.Resolvers.ContractlessStandardResolver.Instance }
             );

            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

            var bytes = MessagePackSerializer.Serialize(set, options);
            var clone = MessagePackSerializer.Deserialize<RedBlackTreeSet<Person, string>>(bytes, options);
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
            // uses surrogate, see TestInitializer
            var set = new RedBlackTreeSet<Person, string>(new PersonNameProvider());
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("Alice", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            RedBlackTreeSet<Person, string> clone;
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, set);
                mem.Position = 0;
                clone = ProtoBuf.Serializer.Deserialize<RedBlackTreeSet<Person, string>>(mem);
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
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), StringComparer.OrdinalIgnoreCase, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 30));
            set.Add(new Person("CHARLIE", 10));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            RedBlackTreeSet<Person, string> clone;
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, set);
                mem.Position = 0;
                clone = ProtoBuf.Serializer.Deserialize<RedBlackTreeSet<Person, string>>(mem);
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
        public void SerializeProtoBufTest_CustomComparerBinarySerializable()
        {
            // uses surrogate, see TestInitializer
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), new BinarySerializableComparer(), new BinarySerializableSatelliteComparer());
            set.Add(new Person("Charlie", 10));
            set.Add(new Person("CHARLIE", 30));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            RedBlackTreeSet<Person, string> clone;
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, set);
                mem.Position = 0;
                clone = ProtoBuf.Serializer.Deserialize<RedBlackTreeSet<Person, string>>(mem);
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
            var set = new RedBlackTreeSet<Person, string>(true, new PersonNameProvider(), new PublicSerializableComparer { IgnoreCase = true }, new PublicSerializableSatelliteComparer { Descending = true });
            set.Add(new Person("Charlie", 10));
            set.Add(new Person("CHARLIE", 30));
            set.Add(new Person("Bob", 20));
            set.Add(new Person("John", 40));

            RedBlackTreeSet<Person, string> clone;
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, set);
                mem.Position = 0;
                clone = ProtoBuf.Serializer.Deserialize<RedBlackTreeSet<Person, string>>(mem);
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