using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public class VirtualizingObservableCollection<T> : IEnumerable, IEnumerable<T>, ICollection, ICollection<T>, IList, IList<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        #region Ctors Etc

        public VirtualizingObservableCollection(IItemSourceProvider<T> provider)
        {
            this.Provider = provider;
        }

        public VirtualizingObservableCollection(IItemSourceProviderAsync<T> asyncProvider)
        {
            this.ProviderAsync = asyncProvider;
        }

        #endregion Ctors Etc

        #region IEnumerable Implementation

        public IEnumerator GetEnumerator()
        {
            string sc = new Guid().ToString();

            EnsureCountIsGotNONASync();

            int count = InternalGetCount();

            for (int iLoop = 0; iLoop < count; iLoop++)
            {
                yield return InternalGetValue(iLoop, sc);
            }
        }

        #endregion IEnumerable Implementation

        #region IEnumerable<T> Implementation

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            string sc = new Guid().ToString();

            EnsureCountIsGotNONASync();

            int count = InternalGetCount();

            for (int iLoop = 0; iLoop < count; iLoop++)
            {
                yield return InternalGetValue(iLoop, sc);
            }
        }

        #endregion IEnumerable<T> Implementation

        #region ICollection Implementation

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return InternalGetCount(); }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        private object _SyncRoot = new object();

        public object SyncRoot
        {
            get { return _SyncRoot; }
        }

        #endregion ICollection Implementation

        #region ICollection<T> Implementation

        public void Add(T item)
        {
            InternalAdd(item, null);
        }

        public void Clear()
        {
            InternalClear();
        }

        public bool Contains(T item)
        {
            return IndexOf(item) != -1 ? true : false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            return InternalRemoveAt(IndexOf(item));
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
            return InternalRemoveAt(IndexOf(item), updatedAt);
        }

        /// <summary>
        /// Removes at the given index - extended to only remove the item if the page was not pulled before the updatedat DateTime.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="updatedAt">The updated at.</param>
        /// <returns></returns>
        public bool RemoveAt(int index, object updatedAt)
        {
            return InternalRemoveAt(index, updatedAt);
        }

        /// <summary>
        /// Adds (appends) the specified item - extended to only add the item if the page was not pulled before the updatedat DateTime.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updatedAt">The updated at.</param>
        /// <returns></returns>
        public int Add(T item, object updatedAt)
        {
            return InternalAdd(item, updatedAt);
        }

        /// <summary>
        /// Inserts the specified index - extended to only insert the item if the page was not pulled before the updatedat DateTime.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <param name="updatedAt">The updated at.</param>
        public void Insert(int index, T item, object updatedAt)
        {
            InternalInsertAt(index, item, updatedAt);
        }

        #endregion Extended CRUD operators that take into account the DateTime of the change

        #region IList Implementation

        public int Add(object value)
        {
            return InternalAdd((T)value, null);
        }

        public bool Contains(object value)
        {
            return Contains((T)value);
        }

        public int IndexOf(object value)
        {
            return IndexOf((T)value);
        }

        public void Insert(int index, object value)
        {
            Insert(index, (T)value);
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public void Remove(object value)
        {
            Remove((T)value);
        }

        public void RemoveAt(int index)
        {
            InternalRemoveAt(index);
        }

        public object this[int index]
        {
            get
            {
                return InternalGetValue(index, _DefaultSelectionContext);
            }
            set
            {
                InternalSetValue(index, (T)value);
            }
        }

        #endregion IList Implementation

        #region IList<T> Implementation

        public int IndexOf(T item)
        {
            return InternalIndexOf(item);
        }

        public void Insert(int index, T item)
        {
            InternalInsertAt(index, item);
        }

        T IList<T>.this[int index]
        {
            get
            {
                return InternalGetValue(index, _DefaultSelectionContext);
            }
            set
            {
                InternalSetValue(index, value);
            }
        }

        #endregion IList<T> Implementation

        #region Public Properties

        public IItemSourceProviderAsync<T> ProviderAsync
        {
            get { return _ProviderAsync; }
            set
            {
                ClearCountChangedHooks();
                _ProviderAsync = value;
                if (_ProviderAsync is INotifyCountChanged)
                {
                    (_ProviderAsync as INotifyCountChanged).CountChanged += VirtualizingObservableCollection_CountChanged;
                }
            }
        }

        void VirtualizingObservableCollection_CountChanged(object sender, CountChangedEventArgs args)
        {
            if (args.NeedsReset)
            {
                // Send a reset..
                RaiseCollectionChangedEvent(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
            OnCountTouched();
        }

        public IItemSourceProvider<T> Provider
        {
            get { return _Provider; }
            set
            {
                ClearCountChangedHooks();
                _Provider = value;

                if (_Provider is INotifyCountChanged)
                {
                    (_Provider as INotifyCountChanged).CountChanged += VirtualizingObservableCollection_CountChanged;
                }
            }
        }

        void ClearCountChangedHooks()
        {
            if(_Provider is INotifyCountChanged)
            {
                (_Provider as INotifyCountChanged).CountChanged -= VirtualizingObservableCollection_CountChanged;
            }

            if(_ProviderAsync is INotifyCountChanged)
            {
                (_ProviderAsync as INotifyCountChanged).CountChanged -= VirtualizingObservableCollection_CountChanged;
            }
        }

        #endregion Public Properties

        #region INotifyCollectionChanged Implementation

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        internal void RaiseCollectionChangedEvent(NotifyCollectionChangedEventArgs args)
        {
            var evnt = CollectionChanged;

            if (evnt != null)
            {
                evnt(this, args);
            }
        }

        #endregion INotifyCollectionChanged Implementation

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private static PropertyChangedEventArgs _PC_CountArgs = new PropertyChangedEventArgs("Count"); 

        private void OnCountTouched()
        {
            RaisePropertyChanged(_PC_CountArgs);
        }

        protected void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            var evnt = PropertyChanged;

            if (evnt != null)
            {
                evnt(this, args);
            }
        }

        #endregion INotifyPropertyChanged implementation

        #region Internal implementation

        protected String _DefaultSelectionContext = new Guid().ToString();
        private IItemSourceProvider<T> _Provider = null;
        private IItemSourceProviderAsync<T> _ProviderAsync = null;

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

        internal void ReplaceAt(int index, T oldValue, T newValue, object timestamp)
        {
            var edit = this.GetProviderAsEditable();

            if (edit != null)
            {
                edit.OnReplace(index, oldValue, newValue, timestamp);

                NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newValue, oldValue, index);
                RaiseCollectionChangedEvent(args);
            }
        }

        void InternalClear()
        {
            InternalGetCount();
        }

        T InternalGetValue(int index, string selectionContext)
        {
            bool allowPlaceholder = true;
            if (selectionContext != _DefaultSelectionContext) allowPlaceholder = false;

            if (this.Provider != null)
            {
                return this.Provider.GetAt(index, this, allowPlaceholder);
            }
            else
            {
                return this.ProviderAsync.GetAt(index, this, allowPlaceholder).Result;
            }
        }

        T InternalSetValue(int index, T newValue)
        {
            T oldValue = InternalGetValue(index, _DefaultSelectionContext);
            var edit = GetProviderAsEditable();
            edit.OnReplace(index, oldValue, newValue, null);

            List<T> newItems = new List<T>(); newItems.Add(newValue);
            List<T> oldItems = new List<T>(); oldItems.Add(oldValue);

            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, oldItems, index);
            RaiseCollectionChangedEvent(args);

            return oldValue;
        }

        int InternalAdd(T newValue, object timestamp)
        {
            //var edit = GetProviderAsEditable();
            //var index = edit.OnAppend(newValue, timestamp);

            //_LocalCount = InternalGetCount() + 1;

            //NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newValue, _LocalCount);
            //RaiseCollectionChangedEvent(args);

            //OnCountTouched();

            int index = InternalGetCount() + 1;

            InternalInsertAt(index, newValue, timestamp);

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
                ret = this.ProviderAsync.Count.Result;
            }


            return ret;
        }

        void InternalInsertAt(int index, T item, object timestamp = null)
        {

            var edit = GetProviderAsEditable();
            edit.OnInsert(index, item, timestamp);

            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);
            RaiseCollectionChangedEvent(args);

            OnCountTouched();
        }

        bool InternalRemoveAt(int index, object timestamp = null)
        {
            T oldValue = InternalGetValue(index, _DefaultSelectionContext);

            if (oldValue == null)
            {
                return false;
            }
            else
            {
                var edit = GetProviderAsEditable();
                edit.OnRemove(index, oldValue, timestamp);

                NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldValue, index);
                RaiseCollectionChangedEvent(args);

                OnCountTouched();

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
                return this.ProviderAsync.IndexOf(item).Result;
            }
        }

        void EnsureCountIsGotNONASync()
        {

        }

        #endregion Internal implementation

    }

}
