using System.Collections.Generic;
using System.Linq;
using AlphaChiTech.VirtualizingCollection.Interfaces;

namespace VirtualizingCollection.Tests
{
    public class ItemSourceProvider<T> : IItemSourceProvider<T>
    {
        private readonly IList<T> _source;

        public ItemSourceProvider(IEnumerable<T> source)
        {
            this._source = source.ToList();
        }

        public void OnReset(int count)
        {
            this._source.Clear();
            ;
        }

        public bool Contains(T item)
        {
            return this._source.Contains(item);
        }

        public T GetAt(int index, object voc, bool usePlaceholder)
        {
            return this._source[index];
        }

        public int GetCount(bool asyncOk)
        {
            return this._source.Count;
        }

        public int IndexOf(T item)
        {
            return this._source.IndexOf(item);
        }

        public bool IsSynchronized { get; } = false;
        public object SyncRoot => this;
    }
}