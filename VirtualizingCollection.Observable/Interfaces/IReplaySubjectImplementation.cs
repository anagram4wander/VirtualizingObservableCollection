using System;

namespace AlphaChiTech.VirtualizingCollection.Interfaces
{
    internal interface IReplaySubjectImplementation<T> : ISubject<T>, ISubject<T, T>, IObserver<T>, IObservable<T>, IDisposable
    {
        bool HasObservers { get; }

        void Unsubscribe(IObserver<T> observer);
    }
}