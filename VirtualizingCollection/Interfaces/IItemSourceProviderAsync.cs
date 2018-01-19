﻿using System.Threading.Tasks;

namespace AlphaChiTech.VirtualizingCollection.Interfaces
{
    public interface IItemSourceProviderAsync<T> : IBaseSourceProvider
    {
        Task<T> GetAt(int index, object voc, bool usePlaceholder);

        T GetPlaceHolder(int index);

        Task<int> Count { get; }

        Task<int> IndexOf(T item);
    }
}