using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public interface ISourcePage<T>
    {
        int Page { get; set; }

        int ItemsPerPage { get; set; }

        bool CanReclaimPage { get; }

        DateTime LastTouch { get; set; }

        T GetAt(int offset);

        int Append(T item, object updatedAt, IPageExpiryComparer comparer);

        int IndexOf(T item);

        void InsertAt(int offset, T item, object updatedAt, IPageExpiryComparer comparer);

        bool RemoveAt(int offset, object updatedAt, IPageExpiryComparer comparer);

        void ReplaceAt(int offset, T oldItem, T newItem, object updatedAt, IPageExpiryComparer comparer);

        PageFetchStateEnum PageFetchState { get; set; }

        DateTime WiredDateTime { get; set; }

    }
}
