using JRC.Collections.RedBlackTree.Tests.Serialization.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JRC.Collections.RedBlackTree.Tests
{
    [TestClass]
    public class TestInitializer
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            MetaType metaType = RuntimeTypeModel.Default.Add(typeof(RedBlackTreeSet<RedBlackTreeSetTests.Person>), false);
            metaType.SetSurrogate(typeof(RedBlackTreeSetProtoSurrogate<RedBlackTreeSetTests.Person>));
            metaType.IgnoreListHandling = true;

            metaType = RuntimeTypeModel.Default.Add(typeof(RedBlackTreeSet<string>), false);
            metaType.SetSurrogate(typeof(RedBlackTreeSetProtoSurrogate<string>));
            metaType.IgnoreListHandling = true;

            metaType = RuntimeTypeModel.Default.Add(typeof(RedBlackTreeDictionary<string, RedBlackTreeDictionaryTests.ValueItem>), false);
            metaType.SetSurrogate(typeof(RedBlackTreeDictionaryProtoSurrogate<string, RedBlackTreeDictionaryTests.ValueItem>));
            metaType.IgnoreListHandling = true;

            metaType = RuntimeTypeModel.Default.Add(typeof(RedBlackTreeSet<RedBlackTreeSetSortedKeyTests.Person, string>), false);
            metaType.SetSurrogate(typeof(RedBlackTreeSetSortedKeyProtoSurrogate<RedBlackTreeSetSortedKeyTests.Person, string>));
            metaType.IgnoreListHandling = true;

            RuntimeTypeModel.Default.CompileInPlace();
        }
    }
}
