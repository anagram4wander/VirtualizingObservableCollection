using System.Threading;

namespace VirtualizingCollection.Tests
{
    public class TestItem
    {
        private static int _root = -1;
        public int Index { get; set; } = Interlocked.Increment(ref _root);
    }
}