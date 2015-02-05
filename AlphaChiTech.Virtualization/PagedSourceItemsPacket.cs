using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public class PagedSourceItemsPacket<T>
    {
        public IEnumerable<T> Items { get; set; }
        private Object _LoadedAt = DateTime.Now;

        public Object LoadedAt
        {
            get { return _LoadedAt; }
            set { _LoadedAt = value; }
        }

    }
}
