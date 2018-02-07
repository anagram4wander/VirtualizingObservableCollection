using System.Linq;
using AlphaChiTech.Virtualization;
using NUnit.Framework;

namespace VirtualizingCollection.Tests
{
    [TestFixture]
    public class VirtualizingObservableCollectionTest
    {
        private readonly VirtualizingObservableCollection<TestItem> _vc;

        public VirtualizingObservableCollectionTest()
        {
            this._vc = new VirtualizingObservableCollection<TestItem>(
                new ItemSourceProvider<TestItem>(Enumerable.Range(0, 100).Select(i => new TestItem())));
        }

        [Test]
        public void _Count_100()
        {
            Assert.AreEqual(100, this._vc.Count);
        }

        [Test]
        public void _GetEnumerator_()
        {
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, this._vc[i].Index);
            }
        }
    }
}