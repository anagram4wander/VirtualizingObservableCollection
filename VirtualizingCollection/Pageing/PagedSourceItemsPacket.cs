using System;
using System.Collections.Generic;

namespace AlphaChiTech.VirtualizingCollection.Pageing
{
    public class PagedSourceItemsPacket<T>
    {
        public IEnumerable<T> Items { get; set; }
        private Object _LoadedAt = DateTime.Now;

        public Object LoadedAt
        {
            get { return this._LoadedAt; }
            set { this._LoadedAt = value; }
        }

    }
}
