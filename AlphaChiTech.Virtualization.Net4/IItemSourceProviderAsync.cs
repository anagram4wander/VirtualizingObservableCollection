using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaChiTech.Virtualization
{
    public interface IItemSourceProviderAsync<T> : IBaseSourceProvider
    {
        Task<T> GetAt(int index, object voc, bool usePlaceholder);

        T GetPlaceHolder(int index);

        Task<int> Count { get; }

        Task<int> IndexOf(T item);
    }
}
