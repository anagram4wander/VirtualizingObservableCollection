using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using AlphaChiTech.VirtualizingCollection.Interfaces;
using AlphaChiTech.VirtualizingCollection.Pageing;

namespace AlphaChiTech.VirtualizingCollection
{
    public class VirtualizingObservableCollection<T> : IEnumerable, IEnumerable<T>, ICollection, ICollection<T>, IList, IList<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        #region Ctors Etc

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualizingObservableCollection{T}"/> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        public VirtualizingObservableCollection(IItemSourceProvider<T> provider)
        {
            this.Provider = provider;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualizingObservableCollection{T}"/> class.
        /// </summary>
        /// <param name="asyncProvider">The asynchronous provider.</param>
        public VirtualizingObservableCollection(IItemSourceProviderAsync<T> asyncProvider)
        {
            this.ProviderAsync = asyncProvider;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualizingObservableCollection{T}"/> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="reclaimer">The optional reclaimer.</param>
        /// <param name="expiryComparer">The optional expiry comparer.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="maxPages">The maximum pages.</param>
        /// <param name="maxDeltas">The maximum deltas.</param>
        /// <param name="maxDistance">The maximum distance.</param>
        public VirtualizingObservableCollection(
            IPagedSourceProvider<T> provider,
            IPageReclaimer<T> reclaimer = null,
            IPageExpiryComparer expiryComparer = null,
            int pageSize = 100,
            int maxPages = 100,
            int maxDeltas = -1,
            int maxDistance = -1
            )
        {
            this.Provider = new PaginationManager<T>(provider, reclaimer, expiryComparer, pageSize, maxPages, maxDeltas, maxDistance);
        }


        #endregion Ctors Etc

        #region IEnumerable Implementation

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            string sc = new Guid().ToString();

            this.EnsureCountIsGotNONASync();

            int count = this.InternalGetCount();

            for (int iLoop = 0; iLoop < count; iLoop++)
            {
                yield return this.InternalGetValue(iLoop, sc);
            }
        }

        #endregion IEnumerable Implementation

        #region IEnumerable<T> Implementation

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            string sc = new Guid().ToString();

            this.EnsureCountIsGotNONASync();

            int count = this.InternalGetCount();

            for (int iLoop = 0; iLoop < count; iLoop++)
            {
                yield return this.InternalGetValue(iLoop, sc);
            }
        }

        #endregion IEnumerable<T> Implementation

        #region ICollection Implementation

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.ICollection" />.</returns>
        public int Count
        {
            get { return this.InternalGetCount(); }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe).
        /// </summary>
        /// <returns>true if access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe); otherwise, false.</returns>
        public bool IsSynchronized
        {
            get { return false; }
        }

        private object _SyncRoot = new object();

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.</returns>
        public object SyncRoot
        {
            get { return this._SyncRoot; }
        }

        #endregion ICollection Implementation

        #region ICollection<T> Implementation

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        public void Add(T item)
        {
            this.InternalAdd(item, null);
        }

        /// <summary>
        /// Resets the collection - aka forces a get all data, including the count <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        public void Clear()
        {
            this.InternalClear();
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        /// true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.
        /// </returns>
        public bool Contains(T item)
        {
            return this.IndexOf(item) != -1 ? true : false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.</returns>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        /// true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </returns>
        public bool Remove(T item)
        {
            return this.InternalRemoveAt(this.IndexOf(item));
        }

        #endregion ICollection<T> Implementation

        #region Extended CRUD operators that take into account the DateTime of the change

        /// <summary>
        /// Removes the specified item - extended to only remove the item if the page was not pulled before the updatedat DateTime.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updatedAt">The updated at.</param>
        /// <returns></returns>
        public bool Remove(T item, object updatedAt)
        {
            return this.InternalRemoveAt(this.IndexOf(item), updatedAt);
        }

        /// <summary>
        /// Removes at the given index - extended to only remove the item if the page was not pulled before the updatedat DateTime.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="updatedAt">The updated at.</param>
        /// <returns></returns>
        public bool RemoveAt(int index, object updatedAt)
        {
            return this.InternalRemoveAt(index, updatedAt);
        }

        /// <summary>
        /// Adds (appends) the specified item - extended to only add the item if the page was not pulled before the updatedat DateTime.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updatedAt">The updated at.</param>
        /// <returns></returns>
        public int Add(T item, object updatedAt)
        {
            return this.InternalAdd(item, updatedAt);
        }

        /// <summary>
        /// Inserts the specified index - extended to only insert the item if the page was not pulled before the updatedat DateTime.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <param name="updatedAt">The updated at.</param>
        public void Insert(int index, T item, object updatedAt)
        {
            this.InternalInsertAt(index, item, updatedAt);
        }
        /// <summary>
        /// Adds the range.
        /// </summary>
        /// <param name="newValues">The new values.</param>
        /// <param name="timestamp">The updatedat object.</param>
        /// <returns>Index of the last appended object</returns>
        public int AddRange(IEnumerable<T> newValues, object timestamp = null)
        {
            var edit = this.GetProviderAsEditable();

            int index = -1;
            List<T> items = new List<T>();

            foreach (var item in newValues)
            {
                items.Add(item);
                index = edit.OnAppend(item, timestamp);

                NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);
                this.RaiseCollectionChangedEvent(args);
            }


            this.OnCountTouched();


            return index;
        }

        #endregion Extended CRUD operators that take into account the DateTime of the change

        #region IList Implementation

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The object to add to the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        /// The position into which the new element was inserted, or -1 to indicate that the item was not inserted into the collection.
        /// </returns>
        public int Add(object value)
        {
            return this.InternalAdd((T)value, null);
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.IList" /> contains a specific value.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        /// true if the <see cref="T:System.Object" /> is found in the <see cref="T:System.Collections.IList" />; otherwise, false.
        /// </returns>
        public bool Contains(object value)
        {
            return this.Contains((T)value);
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList" />.</param>
        /// <returns>
        /// The index of <paramref name="value" /> if found in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(object value)
        {
            return this.IndexOf((T)value);
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.IList" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value" /> should be inserted.</param>
        /// <param name="value">The object to insert into the <see cref="T:System.Collections.IList" />.</param>
        public void Insert(int index, object value)
        {
            this.Insert(index, (T)value);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IList" /> has a fixed size.
        /// </summary>
        /// <returns>true if the <see cref="T:System.Collections.IList" /> has a fixed size; otherwise, false.</returns>
        public bool IsFixedSize
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The object to remove from the <see cref="T:System.Collections.IList" />.</param>
        public void Remove(object value)
        {
            this.Remove((T)value);
        }

        /// <summary>
        /// Removes the <see cref="T:System.Collections.IList" /> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            this.InternalRemoveAt(index);
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public object this[int index]
        {
            get
            {
                return this.InternalGetValue(index, this._DefaultSelectionContext);
            }
            set
            {
                this.InternalSetValue(index, (T)value);
            }
        }

        #endregion IList Implementation

        #region IList<T> Implementation

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <returns>
        /// The index of <paramref name="item" /> if found in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(T item)
        {
            return this.InternalIndexOf(item);
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        public void Insert(int index, T item)
        {
            this.InternalInsertAt(index, item);
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        T IList<T>.this[int index]
        {
            get
            {
                return this.InternalGetValue(index, this._DefaultSelectionContext);
            }
            set
            {
                this.InternalSetValue(index, value);
            }
        }

        #endregion IList<T> Implementation

        #region Public Properties

        /// <summary>
        /// Gets or sets the provider if its asynchronous.
        /// </summary>
        /// <value>
        /// The provider asynchronous.
        /// </value>
        public IItemSourceProviderAsync<T> ProviderAsync
        {
            get { return this._ProviderAsync; }
            set
            {
                this.ClearCountChangedHooks();
                this._ProviderAsync = value;
                if (this._ProviderAsync is INotifyCountChanged)
                {
                    (this._ProviderAsync as INotifyCountChanged).CountChanged += this.VirtualizingObservableCollection_CountChanged;
                }
            }
        }

        void VirtualizingObservableCollection_CountChanged(object sender, CountChangedEventArgs args)
        {
            if (args.NeedsReset)
            {
                // Send a reset..
                this.RaiseCollectionChangedEvent(_CC_ResetArgs);
            }
            this.OnCountTouched();
        }

        /// <summary>
        /// Gets or sets the provider if its not asynchronous.
        /// </summary>
        /// <value>
        /// The provider.
        /// </value>
        public IItemSourceProvider<T> Provider
        {
            get { return this._Provider; }
            set
            {
                this.ClearCountChangedHooks();
                this._Provider = value;

                if (this._Provider is INotifyCountChanged)
                {
                    (this._Provider as INotifyCountChanged).CountChanged += this.VirtualizingObservableCollection_CountChanged;
                }
            }
        }

        void ClearCountChangedHooks()
        {
            if(this._Provider is INotifyCountChanged)
            {
                (this._Provider as INotifyCountChanged).CountChanged -= this.VirtualizingObservableCollection_CountChanged;
            }

            if(this._ProviderAsync is INotifyCountChanged)
            {
                (this._ProviderAsync as INotifyCountChanged).CountChanged -= this.VirtualizingObservableCollection_CountChanged;
            }
        }

        #endregion Public Properties

        #region INotifyCollectionChanged Implementation

        private bool _SupressEventErrors = false;

        public bool SupressEventErrors
        {
            get
            {
                return this._SupressEventErrors;
            }

            set
            {
                this._SupressEventErrors = value;
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Raises the collection changed event.
        /// </summary>
        /// <param name="args">The <see cref="NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        internal void RaiseCollectionChangedEvent(NotifyCollectionChangedEventArgs args)
        {
            if (this._BulkCount > 0) return;

            var evnt = this.CollectionChanged;

            if (evnt != null)
            {
                try
                {
                    evnt(this, args);
                }
                catch (Exception ex)
                {
                    if (!this.SupressEventErrors)
                    {
                        throw ex;
                    }
                }
            }
        }

        #endregion INotifyCollectionChanged Implementation

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private static PropertyChangedEventArgs _PC_CountArgs = new PropertyChangedEventArgs("Count");
        private static NotifyCollectionChangedEventArgs _CC_ResetArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);

        private void OnCountTouched()
        {
            this.RaisePropertyChanged(_PC_CountArgs);
        }

        protected void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            if (this._BulkCount > 0) return;

            var evnt = this.PropertyChanged;

            if (evnt != null)
            {
                evnt(this, args);
            }
        }

        #endregion INotifyPropertyChanged implementation

        #region Bulk Operation implementation

        /// <summary>
        /// Releases the bulk mode.
        /// </summary>
        internal void ReleaseBulkMode()
        {
            if (this._BulkCount > 0) this._BulkCount--;

            if (this._BulkCount == 0)
            {
                this.RaiseCollectionChangedEvent(_CC_ResetArgs);
                this.RaisePropertyChanged(_PC_CountArgs);
            }
        }

        /// <summary>
        /// Enters the bulk mode.
        /// </summary>
        /// <returns></returns>
        public BulkMode EnterBulkMode()
        {
            this._BulkCount++;

            return new BulkMode(this);
        }

        /// <summary>
        /// The Bulk mode IDisposable proxy
        /// </summary>
        public class BulkMode : IDisposable
        {
            public BulkMode(VirtualizingObservableCollection<T> voc)
            {
                this._voc = voc;
            }

            private VirtualizingObservableCollection<T> _voc = null;

            bool _IsDisposed = false;

            public void Dispose()
            {
                this.OnDispose();
            }

            void OnDispose()
            {
                if (!this._IsDisposed)
                {
                    this._IsDisposed = true;
                    if (this._voc != null) this._voc.ReleaseBulkMode();
                }
            }

            ~BulkMode()
            {
                this.OnDispose();
            }
        }

        #endregion Bulk Operation implementation

        #region Private Properties

        protected String _DefaultSelectionContext = new Guid().ToString();
        private IItemSourceProvider<T> _Provider = null;
        private IItemSourceProviderAsync<T> _ProviderAsync = null;
        private int _BulkCount = 0;

        #endregion Private Properties

        #region Internal implementation


        /// <summary>
        /// Gets the provider as editable.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException"></exception>
        protected IEditableProvider<T> GetProviderAsEditable()
        {
            IEditableProvider<T> ret = null;

            if (this.Provider != null)
            {
                ret = this.Provider as IEditableProvider<T>;

            }
            else
            {
                ret = this.ProviderAsync as IEditableProvider<T>;
            }

            if (ret == null)
            {
                throw new NotSupportedException();
            }

            return ret;
        }

        /// <summary>
        /// Replaces oldValue with newValue at index if updatedat is newer or null.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="timestamp">The timestamp.</param>
        internal void ReplaceAt(int index, T oldValue, T newValue, object timestamp)
        {
            var edit = this.GetProviderAsEditable();

            if (edit != null)
            {
                edit.OnReplace(index, oldValue, newValue, timestamp);

                NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newValue, oldValue, index);
                this.RaiseCollectionChangedEvent(args);
            }
        }

        void InternalClear()
        {
            if (this.Provider != null)
            {
                if (this.Provider is IProviderPreReset)
                {
                    (this.Provider as IProviderPreReset).OnBeforeReset();
                }
                this.Provider.OnReset(-1);
            }
            else
            {
                if (this.ProviderAsync is IProviderPreReset)
                {
                    (this.ProviderAsync as IProviderPreReset).OnBeforeReset();
                }
                this.ProviderAsync.OnReset(-1);
            }
        }

        CancellationTokenSource _ResetToken = null;

        public async void ResetAsync()
        {
            CancellationTokenSource cts = null;

            lock(this)
            {
                if(this._ResetToken != null)
                {
                    this._ResetToken.Cancel();
                    this._ResetToken = null;
                }

                cts = this._ResetToken = new CancellationTokenSource();
            }

            if (this.Provider != null)
            {
                if (this.Provider is IProviderPreReset)
                {
                    (this.Provider as IProviderPreReset).OnBeforeReset();
                    if (cts.IsCancellationRequested)
                    {
                        return;
                    }
                    
                }

                //this.Provider.OnReset(-2);

                Task.Run(async () =>
                    {
                        if (this.Provider is IAsyncResetProvider)
                        {
                            int count = await (this.Provider as IAsyncResetProvider).GetCountAsync();
                            if (!cts.IsCancellationRequested)
                            {
                                VirtualizationManager.Instance.RunOnUI(() =>
                                    this.Provider.OnReset(count)
                                );
                            }

                        }
                        else
                        {
                            int count = this.Provider.GetCount(false);
                            if (!cts.IsCancellationRequested)
                            {
                                VirtualizationManager.Instance.RunOnUI(() =>
                                    this.Provider.OnReset(count)
                                );
                            }
                        }

                    });

            }
            else
            {
                if (this.ProviderAsync is IProviderPreReset)
                {
                    (this.ProviderAsync as IProviderPreReset).OnBeforeReset();
                }
                this.ProviderAsync.OnReset(await this.ProviderAsync.Count);
            }

            lock(this)
            {
                if(this._ResetToken == cts)
                {
                    this._ResetToken = null;
                }
            }
        }

        T InternalGetValue(int index, string selectionContext)
        {
            bool allowPlaceholder = true;
            if (selectionContext != this._DefaultSelectionContext) allowPlaceholder = false;

            if (this.Provider != null)
            {
                return this.Provider.GetAt(index, this, allowPlaceholder);
            }
            else
            {
                return Task.Run(() => this.ProviderAsync.GetAt(index, this, allowPlaceholder)).Result;
            }
        }

        T InternalSetValue(int index, T newValue)
        {
            T oldValue = this.InternalGetValue(index, this._DefaultSelectionContext);
            var edit = this.GetProviderAsEditable();
            edit.OnReplace(index, oldValue, newValue, null);

            List<T> newItems = new List<T>(); newItems.Add(newValue);
            List<T> oldItems = new List<T>(); oldItems.Add(oldValue);

            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, oldItems, index);
            this.RaiseCollectionChangedEvent(args);

            return oldValue;
        }

        int InternalAdd(T newValue, object timestamp)
        {
            var edit = this.GetProviderAsEditable();
            var index = edit.OnAppend(newValue, timestamp);

            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newValue, index);
            this.RaiseCollectionChangedEvent(args);

            this.OnCountTouched();

            return index;
        }

        int InternalGetCount()
        {
            int ret = 0;

            if (this.Provider != null)
            {
                ret = this.Provider.GetCount(true);
            }
            else
            {
                ret = Task.Run( () => this.ProviderAsync.Count).Result;
            }


            return ret;
        }

        void InternalInsertAt(int index, T item, object timestamp = null)
        {

            var edit = this.GetProviderAsEditable();
            edit.OnInsert(index, item, timestamp);

            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);
            this.RaiseCollectionChangedEvent(args);

            this.OnCountTouched();
        }

        bool InternalRemoveAt(int index, object timestamp = null)
        {
            T oldValue = this.InternalGetValue(index, this._DefaultSelectionContext);

            if (oldValue == null)
            {
                return false;
            }
            else
            {
                var edit = this.GetProviderAsEditable();
                edit.OnRemove(index, oldValue, timestamp);

                NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldValue, index);
                this.RaiseCollectionChangedEvent(args);

                this.OnCountTouched();

                return true;
            }
        }

        int InternalIndexOf(T item)
        {
            if (this.Provider != null)
            {
                return this.Provider.IndexOf(item);
            }
            else
            {
                return Task.Run( () => this.ProviderAsync.IndexOf(item)).Result;
            }
        }

        void EnsureCountIsGotNONASync()
        {
            if(this.Provider != null)
            {
                this.Provider.GetCount(false);
            }
            else
            {
                
            }
        }

        #endregion Internal implementation

    }

}
