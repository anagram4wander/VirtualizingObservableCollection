using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public class SourcePagePendingUpdates
    {
        public INotifyCollectionChanged Args { get; set; }
        public Object UpdatedAt { get; set; }
    }

    public interface ISourcePage<T>
    {
        int Page { get; set; }

        int ItemsPerPage { get; set; }

        bool CanReclaimPage { get; }

        Object LastTouch { get; set; }

        T GetAt(int offset);

        int Append(T item, object updatedAt, IPageExpiryComparer comparer);

        int IndexOf(T item);

        void InsertAt(int offset, T item, object updatedAt, IPageExpiryComparer comparer);

        bool RemoveAt(int offset, object updatedAt, IPageExpiryComparer comparer);

        void ReplaceAt(int offset, T oldItem, T newItem, object updatedAt, IPageExpiryComparer comparer);

        PageFetchStateEnum PageFetchState { get; set; }

        Object WiredDateTime { get; set; }

        bool ReplaceNeeded(int offset);

        List<SourcePagePendingUpdates> PendingUpdates { get; }
    }
}
