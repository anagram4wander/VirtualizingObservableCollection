using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using AlphaChiTech.Virtualization.Pageing;
using AlphaChiTech.VirtualizingCollection;
using AlphaChiTech.VirtualizingCollection.Interfaces;

namespace AlphaChiTech.Virtualization
{
    public class VirtualizingObservableReactiveCollection<T> : VirtualizingObservableCollection<T>,  IReplaySubjectImplementation<T> where T : class
    {
        #region Ctors Etc
        /// <summary>
        ///     Initializes a new instance of the <see cref="VirtualizingObservableCollection{T}" /> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        public VirtualizingObservableReactiveCollection(IItemSourceProvider<T> provider) : base(provider)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualizingObservableCollection{T}" /> class.
        /// </summary>
        /// <param name="asyncProvider">The asynchronous provider.</param>
        public VirtualizingObservableReactiveCollection(IItemSourceProviderAsync<T> asyncProvider) : base(asyncProvider)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualizingObservableCollection{T}" /> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="reclaimer">The optional reclaimer.</param>
        /// <param name="expiryComparer">The optional expiry comparer.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="maxPages">The maximum pages.</param>
        /// <param name="maxDeltas">The maximum deltas.</param>
        /// <param name="maxDistance">The maximum distance.</param>
        public VirtualizingObservableReactiveCollection(
            IPagedSourceProvider<T> provider,
            IPageReclaimer<T> reclaimer = null,
            IPageExpiryComparer expiryComparer = null,
            int pageSize = 100,
            int maxPages = 100,
            int maxDeltas = -1,
            int maxDistance = -1) : base(provider,reclaimer, pageSize, maxPages, maxDeltas, maxDistance)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="VirtualizingObservableCollection{T}" /> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="reclaimer">The optional reclaimer.</param>
        /// <param name="expiryComparer">The optional expiry comparer.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="maxPages">The maximum pages.</param>
        /// <param name="maxDeltas">The maximum deltas.</param>
        /// <param name="maxDistance">The maximum distance.</param>
        /// //TODO Check implementation
        public VirtualizingObservableReactiveCollection(
            IPagedSourceObservableProvider<T> provider,
            IPageReclaimer<T> reclaimer = null,
            IPageExpiryComparer expiryComparer = null,
            int pageSize = 100,
            int maxPages = 100,
            int maxDeltas = -1,
            int maxDistance = -1) : base(provider, reclaimer, pageSize, maxPages, maxDeltas, maxDistance)
        {
            (this.Provider as PaginationManager<T>).CollectionChanged += this.VirtualizingObservableCollection_CollectionChanged;
            this.IsSourceObservable = true;

        }
        #endregion Ctors Etc

        private bool IsSourceObservable { get; } = false;

        

        #region Extended CRUD operators that take into account the DateTime of the change
      

        /// <summary>
        ///     Adds the range.
        /// </summary>
        /// <param name="newValues">The new values.</param>
        /// <param name="timestamp">The updatedat object.</param>
        /// <returns>Index of the last appended object</returns>
        public int AddRange(IEnumerable<T> newValues, object timestamp = null)
        {
            var edit = this.GetProviderAsEditable();

            var index = -1;
            var items = new List<T>();

            foreach(var item in newValues)
            {
                items.Add(item);
                index = edit.OnAppend(item, timestamp);

                if(!this.IsSourceObservable)
                {
                    var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);
                    this.RaiseCollectionChangedEvent(args);
                }
            }


            this.OnCountTouched();


            return index;
        }
        #endregion Extended CRUD operators that take into account the DateTime of the change


        #region Internal implementation

        internal void ReplaceAt(int index, T oldValue, T newValue, object timestamp)
        {
            var edit = this.GetProviderAsEditable();

            if(edit != null)
            {
                edit.OnReplace(index, oldValue, newValue, timestamp);
                //TODO check this  code
                if (!this.IsSourceObservable)
                {
                    var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newValue, oldValue, index);
                    this.RaiseCollectionChangedEvent(args);
                }
            }
        }
        
        private T InternalSetValue(int index, T newValue)
        {
            var oldValue = this.InternalGetValue(index, this.DefaultSelectionContext);
            var edit = this.GetProviderAsEditable();
            edit.OnReplace(index, oldValue, newValue, null);

            var newItems = new List<T> {newValue};
            var oldItems = new List<T> {oldValue};

            //TODO check
            if(!this.IsSourceObservable)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, oldItems, index);
                this.RaiseCollectionChangedEvent(args);
            }

            return oldValue;
        }

        private int InternalAdd(T newValue, object timestamp)
        {
            var edit = this.GetProviderAsEditable();
            var index = edit.OnAppend(newValue, timestamp);
            
            if (!this.IsSourceObservable)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newValue, index);
                this.RaiseCollectionChangedEvent(args);
            }
            this.OnCountTouched();
            return index;
        }
        

        private void InternalInsertAt(int index, T item, object timestamp = null)
        {
            var edit = this.GetProviderAsEditable();
            edit.OnInsert(index, item, timestamp);

            this.OnCountTouched();
            if (!this.IsSourceObservable)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);
                this.RaiseCollectionChangedEvent(args);
            }

            
        }

        private bool InternalRemoveAt(int index, object timestamp = null)
        {
            var edit = this.Provider as IEditableProviderIndexBased<T>;
            if (edit == null) return false;
            var oldValue = edit.OnRemove(index, timestamp);

            this.OnCountTouched();
            if (!this.IsSourceObservable)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldValue, index);
                this.RaiseCollectionChangedEvent(args);
            }
            

            return true;
        }

        private bool InternalRemove(T item, object timestamp = null)
        {
            if (item == null) { return false; }
            var edit = this.Provider as IEditableProviderItemBased<T>;
            if(edit == null)
                return false;
            var index = edit.OnRemove(item, timestamp);

            this.OnCountTouched();
            if (!this.IsSourceObservable)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);
                this.RaiseCollectionChangedEvent(args);
            }
            

            return true;
        }

      
        #endregion Internal implementation

        //#region Implementation of IObservable<out T>
        ///// <summary>
        ///// Notifies the provider that an observer is to receive notifications.
        ///// </summary>
        ///// <returns>
        ///// A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.
        ///// </returns>
        ///// <param name="observer">The object that is to receive notifications.</param>
        //public IDisposable Subscribe(IObserver<T> observer) { throw new NotImplementedException(); }
        //#endregion

        #region Implementation of IObservable<out T>
        private readonly object _gate = new object();

        private Exception _error;
        private bool _isDisposed;
        private bool _isStopped;
        private ImmutableList<IObserver<T>> _observers = new ImmutableList<IObserver<T>>();


        public bool HasObservers
        {
            get
            {
                var immutableList = this._observers;
                return immutableList?.Data.Length > 0;
            }
        }

        #region Implementation of IDisposable
        public void Dispose() { this.Dispose(true); }
        #endregion

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null) { throw new ArgumentNullException(nameof(observer)); }
            var subscription = new Subscription((IReplaySubjectImplementation<T>)this, observer);
            lock (this._gate)
            {
                this.CheckDisposed();
                this._observers = this._observers.Add(observer);
                this.ReplayBuffer(observer);
                if (this._error != null) { observer.OnError(this._error); }
                else if (this._isStopped) { observer.OnCompleted(); }
            }
            return subscription;
        }

        public void OnCompleted()
        {
            lock (this._gate)
            {
                this.CheckDisposed();
                if (this._isStopped) { return; }
                this._isStopped = true;
                foreach (var item_0 in this._observers.Data) { item_0.OnCompleted(); }
                this._observers = new ImmutableList<IObserver<T>>();
            }
        }

        public void OnError(Exception error)
        {
            if (error == null) { throw new ArgumentNullException("error"); }
            lock (this._gate)
            {
                this.CheckDisposed();
                if (this._isStopped) { return; }
                this._isStopped = true;
                this._error = error;
                foreach (var item_0 in this._observers.Data) { item_0.OnError(error); }
                this._observers = new ImmutableList<IObserver<T>>();
            }
        }

        public void OnNext(T value)
        {
            lock (this._gate)
            {
                this.CheckDisposed();
                if (this._isStopped) { return; }
                foreach (var item_0 in this._observers.Data) { item_0.OnNext(value); }
            }
        }

        public void Dispose(bool disposing)
        {
            lock (this._gate)
            {
                this._isDisposed = true;
                this._observers = null;
            }
        }

        public void Unsubscribe(IObserver<T> observer)
        {
            lock (this._gate)
            {
                if (this._isDisposed) { return; }
                this._observers = this._observers.Remove(observer);
            }
        }


        protected void ReplayBuffer(IObserver<T> observer) { foreach (var obj in this) { observer.OnNext(obj); } }


        private void CheckDisposed() { if (this._isDisposed) { throw new ObjectDisposedException(string.Empty); } }


        private class Subscription : IDisposable
        {
            private IObserver<T> _observer;
            private IReplaySubjectImplementation<T> _subject;

            public Subscription(IReplaySubjectImplementation<T> subject, IObserver<T> observer)
            {
                this._subject = subject;
                this._observer = observer;
            }

            public void Dispose()
            {
                var observer = Interlocked.Exchange(ref this._observer, null);
                if (observer == null) { return; }
                this._subject.Unsubscribe(observer);
                this._subject = null;
            }
        }

        private class ImmutableList<TT>
        {
            public ImmutableList() { this.Data = new TT[0]; }

            private ImmutableList(TT[] data) { this.Data = data; }

            public TT[] Data { get; }

            public ImmutableList<TT> Add(TT value)
            {
                var newData = new TT[this.Data.Length + 1];
                Array.Copy(this.Data, newData, this.Data.Length);
                newData[this.Data.Length] = value;
                return new ImmutableList<TT>(newData);
            }

            private int IndexOf(TT value)
            {
                for (var i = 0; i < this.Data.Length; ++i) { if (this.Data[i].Equals(value)) { return i; } }
                return -1;
            }

            public ImmutableList<TT> Remove(TT value)
            {
                var i = this.IndexOf(value);
                if (i < 0) { return this; }
                var newData = new TT[this.Data.Length - 1];
                Array.Copy(this.Data, 0, newData, 0, i);
                Array.Copy(this.Data, i + 1, newData, i, this.Data.Length - i - 1);
                return new ImmutableList<TT>(newData);
            }
        }

        #endregion
    }
}