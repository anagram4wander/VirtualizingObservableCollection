using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace DataGridAsyncDemoMVVM.filtersort
{
    public class DescriptionList<T> : IEnumerable<T>, IEnumerable, INotifyCollectionChanged
        where T : IFilterOrderDescription
    {
        private readonly List<T> _filterDescriptions = new List<T>();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._filterDescriptions.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this._filterDescriptions.GetEnumerator();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        ///     If it exist, remove existing filter that apply on same property name. The add item arg at first position into
        ///     filter list.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            var index = this._filterDescriptions.FindIndex(description =>
                description.PropertyName.Equals(item.PropertyName, StringComparison.Ordinal));
            if (index >= 0)
            {
                var removed = this._filterDescriptions[index];
                this._filterDescriptions.RemoveAt(index);
                //OnCollectionChanged( new NotifyCollectionChangedEventArgs( NotifyCollectionChangedAction.Remove, removed, index ) );
                this._filterDescriptions.Insert(0, item);
                //OnCollectionChanged( new NotifyCollectionChangedEventArgs( NotifyCollectionChangedAction.Add, item, 0 ) );

                this.OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, removed, 0, index));
            }
            else
            {
                this._filterDescriptions.Insert(0, item);
                this.OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, 0));
            }
        }

        /// <summary>
        ///     If it exist, remove existing filter that apply on same property name. The add item arg at first position into
        ///     filter list.
        /// </summary>
        /// <param name="propertyName"></param>
        public void Remove(string propertyName)
        {
            var index = this._filterDescriptions.FindIndex(description =>
                description.PropertyName.Equals(propertyName, StringComparison.Ordinal));
            if (index >= 0)
            {
                var removed = this._filterDescriptions[index];
                this._filterDescriptions.RemoveAt(index);
                this.OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, index));
            }
        }

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs arg)
        {
            var evnt = this.CollectionChanged;

            evnt?.Invoke(this, arg);
        }
    }
}