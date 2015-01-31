using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public interface IPagedSourceProvider<T> : IBaseSourceProvider
    {
        PagedSourceItemsPacket<T> GetItemsAt(int pageoffset, int count, bool usePlaceholder);

        int Count { get; }

        int IndexOf(T item);
    }
}
