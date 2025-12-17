# JRC.Collections.RedBlackTree

**⚡ Blazing fast sorted collections with index access — what .NET should have been**

[![NuGet](https://img.shields.io/nuget/v/JRC.Collections.RedBlackTree.svg)](https://www.nuget.org/packages/JRC.Collections.RedBlackTree/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/JRC.Collections.RedBlackTree.svg)](https://www.nuget.org/packages/JRC.Collections.RedBlackTree/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

> 🚀 **30% less memory** than Microsoft's SortedSet/SortedDictionary  
> 📍 **Index-based access** on all collections — `set[i]`, `dict.GetAt(i)`, `IndexOf()`  
> ⚡ **O(log n) Insert/Remove** for List operations  
> 🎯 **Duplicate support** in sorted sets  

---

## The Problem

Ever needed to:
- Get the **5th element** from a `SortedSet<T>`? You can't.
- Find the **index** of a key in `SortedDictionary<K,V>`? Impossible.
- Do **fast Insert/Remove** in the middle of a large `List<T>`? O(n) kills you.

Microsoft's collections force you to choose: sorted OR indexed. **Not anymore.**

---

## Features

| Feature | SortedSet | SortedDictionary | List | **This Library** |
|---------|-----------|------------------|------|------------------|
| Sorted | ✅ | ✅ | ❌ | ✅ |
| Index access `[i]` | ❌ | ❌ | ✅ | ✅ |
| `IndexOf()` | ❌ | ❌ | O(n) | **O(log n)** |
| `Insert(i, x)` | ❌ | ❌ | O(n) | **O(log n)** |
| `RemoveAt(i)` | ❌ | ❌ | O(n) | **O(log n)** |
| Duplicates | ❌ | ❌ | ✅ | ✅ |
| Range queries | ✅ | ❌ | ❌ | ✅ |
| Memory efficient | ❌ | ❌ | ✅ | ✅ |

---

## Installation

```bash
dotnet add package JRC.Collections.RedBlackTree
```

---

## Available Classes

### RedBlackTreeSet&lt;T&gt;
A sorted set with index access and duplicate support.

```csharp
var set = new RedBlackTreeSet<Person>(
    allowDuplicates: true, 
    (a, b) => a.Age - b.Age);

set.Add(new Person("Alice", 30));
set.Add(new Person("Bob", 25));
set.Add(new Person("Charlie", 25)); // Same age = duplicate OK

var youngest = set[0];              // O(log n) - index access!
int index = set.IndexOf(bob);       // O(log n)
var aged25 = set.FindKeys(p => p.Age - 25); // All persons aged 25
```

### RedBlackTreeSet&lt;TItem, TSortKey&gt;
Simplified API when sorting by a property.

```csharp
var set = new RedBlackTreeSet<Person, int>(
    allowDuplicates: true,
    person => person.Age,           // Sort key extractor
    (a, b) => a - b);               // Key comparer

set.Add(new Person("Alice", 30));
var person = set.FindKey(30);       // Find by age directly
var range = set.FindRange(20, 30);  // All persons aged 20-30
```

### RedBlackTreeDictionary&lt;TKey, TValue&gt;
A sorted dictionary with index access.

```csharp
var dict = new RedBlackTreeDictionary<int, string>();
dict.Add(100, "hundred");
dict.Add(50, "fifty");
dict.Add(75, "seventy-five");

var middle = dict.GetAt(1);         // O(log n) - {75, "seventy-five"}
int idx = dict.IndexOfKey(75);      // O(log n) - returns 1
var removed = dict.RemoveAt(0);     // O(log n) - removes {50, "fifty"}
```

### RedBlackTreeList&lt;T&gt;
A list with O(log n) insert/remove operations.

```csharp
var list = new RedBlackTreeList<string>();

// Insert 1 million items at random positions? No problem.
for (int i = 0; i < 1_000_000; i++)
    list.Insert(random.Next(list.Count), $"item{i}");  // O(log n) each!

list.RemoveAt(500_000);  // O(log n)
var item = list[250_000]; // O(log n)
```

### RedBlackTreeIndex&lt;T&gt;
The underlying engine behind `RedBlackTreeList<T>`. Use this directly if you need to build your own `IList<T>` implementation or want maximum control.

```csharp
var index = new RedBlackTreeIndex<MyItem>();

// Add returns the internal nodeId - store it for O(log n) operations later
int nodeId = index.Add(new MyItem("data"));

// If your items track their own nodeId, you get O(log n) IndexOf
int position = index.IndexOf(nodeId);       // O(log n) with nodeId!
index.Remove(nodeId);                        // O(log n) direct removal

// Without nodeId, you can still find items (but O(n))
int foundNodeId = index.FindNodeId(item);   // O(n) scan
```

**Pro tip**: If your `T` stores its `nodeId` internally, like the default wrapper `RedBlackTreeList<T>` does, you can achieve O(log n) for all operations including `IndexOf` and `Remove` — something even `LinkedList<T>` can't do efficiently.

---

## Linq support:

`enumerable.ToRedBlackTreeList<T>()`, `enumerable.ToRedBlackTreeDictionary<TKey, TValue>(...)`, `enumerable.ToRedBlackTreeSet<T>(...)` and `enumerable.ToRedBlackTreeSet<T, TSortedKey>(...)` are provided since you are using `System.Linq`.

---

## Benchmarks

### Set vs SortedSet (.NET 8)

| Operation | SortedSet | RedBlackTreeSet |
|-----------|-----------|-----------------|
| Add (random) | 44 ms | **39 ms** |
| Contains | 6 ms | 10 ms |
| Enumerate | 3 ms | 3 ms |
| Range query | 2035 ms | **1539 ms** |
| **this[index]** | ❌ | **27 ms** |
| **IndexOf** | ❌ | **2 ms** |
| **RemoveAt** | ❌ | **21 ms** |

### Dictionary vs SortedDictionary (.NET 8)

| Operation | SortedDictionary | RedBlackTreeDictionary |
|-----------|------------------|------------------------|
| Add (sorted) | 42 ms | **34 ms** |
| Add (random) | 44 ms | **39 ms** |
| TryGetValue | 15 ms | 15 ms |
| Enumerate | 3 ms | **1 ms** |
| Remove | 39 ms | 42 ms |
| **GetAt(index)** | ❌ | **30 ms** |
| **IndexOfKey** | ❌ | **2 ms** |
| **RemoveAt** | ❌ | **21 ms** |

*100,000 items, average of multiple runs*

### Memory Usage (1M items)

| Collection | Memory |
|------------|--------|
| SortedSet&lt;int&gt; | 40 MB |
| RedBlackTreeSet&lt;int&gt; | **28 MB** |

**30% less memory** thanks to page-based allocation with contiguous arrays instead of individual heap-allocated nodes.

---

## Serialization:

- `RedBlackTreeList<T>`, `RedBlackTreeDictionary<K, V>`, `RedBlackTreeSet<K>` and `RedBlackTreeSet<T, TSortedKey>` are **Binary serializable** (even since .NET 8.0, where Microsoft's binary serialization is marked obsolete).
- `RedBlackTreeList<T>`, `RedBlackTreeDictionary<K, V>`, `RedBlackTreeSet<K>` and `RedBlackTreeSet<T, TSortedKey>` are **XML serializable**. Comparers and SortKeyProvider must be XML serializable too, or binary serializable (if `RedBlackTypeSerializationInfo.SubObjectsBinarySerializationAllowed` is true).
- `RedBlackTreeList<T>`, `RedBlackTreeDictionary<K, V>`, `RedBlackTreeSet<K>` are **Newtonsoft.Json serializable**. If custom comparers are used, or if `RedBlackTreeSet<T, TSortedKey>` needs to be serialized, use a converter like these [ones](https://github.com/JrCohen-arch/JRC/tree/main/tests/JRC.Collections.RedBlackTree.Tests/Serialization/Newton).
- `RedBlackTreeList<T>`, `RedBlackTreeDictionary<K, V>`, `RedBlackTreeSet<K>` are **System.Text.Json serializable**. If custom comparers are used, or if `RedBlackTreeSet<T, TSortedKey>` needs to be serialized, use a converter like these [ones](https://github.com/JrCohen-arch/JRC/tree/main/tests/JRC.Collections.RedBlackTree.Tests/Serialization/JsonText).
- `RedBlackTreeList<T>`, `RedBlackTreeDictionary<K, V>`, `RedBlackTreeSet<K>` are **MessagePack serializable**. If custom comparers are used, or if `RedBlackTreeSet<T, TSortedKey>` needs to be serialized, use a converter like these [ones](https://github.com/JrCohen-arch/JRC/tree/main/tests/JRC.Collections.RedBlackTree.Tests/Serialization/MessagePack).
- `RedBlackTreeList<T>`, `RedBlackTreeDictionary<K, V>`, `RedBlackTreeSet<K>` are **Protobuf serializable**. If custom comparers are used, or if `RedBlackTreeSet<T, TSortedKey>` needs to be serialized, use a converter like these [ones](https://github.com/JrCohen-arch/JRC/tree/main/tests/JRC.Collections.RedBlackTree.Tests/Serialization/Protobuf).

> ℹ️ **Info:** `RedBlackTreeList<T>` requires no comparer and is serializable in all formats without additional converters.

---

## Technical Notes

This library is based on Microsoft's internal `RBTree<K>` class from `System.Data`, with significant improvements:

- **Successor chain**: O(1) enumeration via linked successor pointers (vs O(log n) tree traversal) for `RedBlackTreeList<K>` (and `RedBlackTreeIndex<K>`), and also `RedBlackTreeDictionary<K, V>()`
- **Simplified API & Optimized Performance**: Unlike Microsoft's shared `RBTree<K>` implementation used across multiple collection types, this library provides dedicated classes with specialized optimizations for each use case, resulting in cleaner APIs and better performance.

### Node Structure
```
┌────────────────────────────────────────────┐
│ LeftId    (4 bytes)                        │
│ RightId   (4 bytes)                        │
│ ParentId  (4 bytes)                        │
│ LinkId    (4 bytes) ─► Successor or Next   │
│ SubTreeSize (4 bytes)                      │
│ Color     (1 byte)                         │
│ Value     (variable)                       │
└────────────────────────────────────────────┘
```

---

## Acknowledgments

- **Microsoft** for their internal `RBTree<K>` implementation in `System.Data` which served as the foundation for this library
- **Claude (Anthropic)** — an excellent pair programming partner for algorithm design, optimization, and thorough testing

---

## License

MIT License - see [LICENSE](https://github.com/JrCohen-arch/JRC/tree/main/LICENSE) for details.

---

## Contributing

Issues and PRs welcome! If you find a bug or have a feature request, please open an issue.
