using System;

namespace AlphaChiTech.VirtualizingCollection.Interfaces
{
    public interface IObservableReactiveCollection<T> : IObservableCollection<T>, IObservable<T>
    {
        new int Count { get; }
        new bool Remove(object item);
    }
}