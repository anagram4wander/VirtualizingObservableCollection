using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public interface IItemSourceProvider<T> : IBaseSourceProvider
    {
        T GetAt(int index, object voc, bool usePlaceholder);
        int GetCount(bool asyncOK);

        int IndexOf(T item);
    }
}
