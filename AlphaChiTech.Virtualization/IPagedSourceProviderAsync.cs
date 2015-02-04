using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaChiTech.Virtualization
{
    public interface IPagedSourceProviderAsync<T> : IBaseSourceProvider
    {
         Task<PagedSourceItemsPacket<T>> GetItemsAt(int pageoffset, int count, bool usePlaceholder);

        T GetPlaceHolder(int page, int offset);

        Task<int> GetCount();

        Task<int> IndexOf(T item);

    }
}
