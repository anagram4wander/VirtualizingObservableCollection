﻿using System.Threading.Tasks;
using AlphaChiTech.VirtualizingCollection.Pageing;

namespace AlphaChiTech.VirtualizingCollection.Interfaces
{
    public interface IPagedSourceProviderAsync<T> : IPagedSourceProvider<T>
    {
         Task<PagedSourceItemsPacket<T>> GetItemsAtAsync(int pageoffset, int count, bool usePlaceholder);

        T GetPlaceHolder(int index, int page, int offset);

        Task<int> GetCountAsync();

        Task<int> IndexOfAsync( T item );
    }
}