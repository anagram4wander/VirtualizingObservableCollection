using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using AlphaChiTech.Virtualization.Pageing;

namespace AlphaChiTech.VirtualizingCollection.Interfaces
{
    public class SourcePagePendingUpdates
    {
        public INotifyCollectionChanged Args { get; set; }
        public Object UpdatedAt { get; set; }
    }

    public interface ISourcePage<T>
    {
        bool CanReclaimPage { get; }

        int ItemsCount { get; }

        int ItemsPerPage { get; set; }

        Object LastTouch { get; set; }
        int Page { get; set; }

        PageFetchStateEnum PageFetchState { get; set; }

        List<SourcePagePendingUpdates> PendingUpdates { get; }

        Object WiredDateTime { get; set; }

        int Append(T item, object updatedAt, IPageExpiryComparer comparer);

        T GetAt(int offset);

        int IndexOf(T item);

        void InsertAt(int offset, T item, object updatedAt, IPageExpiryComparer comparer);

        bool RemoveAt(int offset, object updatedAt, IPageExpiryComparer comparer);

        T ReplaceAt(int offset, T newValue, object updatedAt, IPageExpiryComparer comparer);

        T ReplaceAt(T oldValue, T newValue, object updatedAt, IPageExpiryComparer comparer);

        bool ReplaceNeeded(int offset);
    }
}