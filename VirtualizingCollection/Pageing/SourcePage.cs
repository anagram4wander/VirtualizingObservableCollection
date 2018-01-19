using System;
using System.Collections.Generic;
using AlphaChiTech.VirtualizingCollection.Interfaces;

namespace AlphaChiTech.Virtualization.Pageing
{
    public class SourcePage<T> : ISourcePage<T>
    {
        protected List<T> Items = new List<T>();


        /// <summary>
        /// Gets or sets the page.
        /// </summary>
        /// <value>
        /// The page.
        /// </value>
        public int Page { get; set; }

        public List<SourcePagePendingUpdates> PendingUpdates { get; } = new List<SourcePagePendingUpdates>();

        /// <summary>
        /// Gets or sets the items per page.
        /// </summary>
        /// <value>
        /// The items per page.
        /// </value>
        public int ItemsPerPage { get; set; }

        public int ItemsCount => this.Items.Count;

        private readonly List<int> _replaceNeededList = new List<int>();

        /// <summary>
        /// Gets a value indicating whether [can reclaim page].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [can reclaim page]; otherwise, <c>false</c>.
        /// </value>
        public bool CanReclaimPage => this.PageFetchState != PageFetchStateEnum.Placeholders;

        /// <summary>
        /// Determines whether it is safe to update into a page where the pending update was generated at a given time.
        /// </summary>
        /// <param name="updatedAt">The updated happened at this datetime.</param>
        /// <returns></returns>
        public bool IsSafeToUpdate(IPageExpiryComparer comparer, object updatedAt)
        {
            var ret = true;

            if (comparer != null)
            {
                ret = comparer.IsUpdateValid(this.WiredDateTime, updatedAt);
            }

            return ret;
        }

        /// <summary>
        /// Gets or sets the last touch.
        /// </summary>
        /// <value>
        /// The last touch.
        /// </value>
        public Object LastTouch { get; set; }

        /// <summary>
        /// Gets at.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns></returns>
        public T GetAt(int offset)
        {
            var ret = default(T);

            if (this.PageFetchState == PageFetchStateEnum.Placeholders) this._replaceNeededList.Add(offset);

            if (this.Items.Count > offset) ret = this.Items[offset];

            this.LastTouch = DateTime.Now;

            return ret;
        }

        public bool ReplaceNeeded(int offset)
        {

            return this._replaceNeededList.Contains(offset); 
        }

        /// <summary>
        /// Appends the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public int Append(T item, object updatedAt, IPageExpiryComparer comparer)
        {
            this.Items.Add(item);

            this.LastTouch = DateTime.Now;

            return this.Items.IndexOf(item);
            //TODO<-return this.Items.Count;
        }

        /// <summary>
        /// Inserts at.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="item">The item.</param>
        /// <param name="updatedAt">The updated at.</param>
        public void InsertAt(int offset, T item, object updatedAt, IPageExpiryComparer comparer)
        {
            if (this.IsSafeToUpdate(comparer, updatedAt))
            {
                if (this.Items.Count > offset)
                {
                    this.Items.Insert(offset, item);
                }
                else
                {
                    this.Items.Add(item);
                }

            }
        }

        /// <summary>
        /// Removes at.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="updatedAt">The updated at.</param>
        /// <returns></returns>
        public bool RemoveAt(int offset, object updatedAt, IPageExpiryComparer comparer)
        {
            var removed = true;

            if (this.IsSafeToUpdate(comparer, updatedAt))
            {
                this.Items.RemoveAt(offset);
            }
            return removed;
        }

    
        /// <summary>
        /// Replaces at.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="updatedAt">The updated at.</param>
        public T ReplaceAt(int offset, T newValue, object updatedAt, IPageExpiryComparer comparer)
        {
            var oldValue = this.Items[offset];
            if(!this.IsSafeToUpdate(comparer, updatedAt))
            {
                if (this.Items.Count > offset) this.Items[offset] = newValue;
                return oldValue;
            }
            this.Items[offset] = newValue;
            return oldValue;
        }

        /// <summary>
        /// Replaces at.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="updatedAt">The updated at.</param>
        public T ReplaceAt(T oldValue, T newValue, object updatedAt, IPageExpiryComparer comparer)
        {
            if(!this.IsSafeToUpdate(comparer, updatedAt))
            {
                
                return oldValue;
            }
            var offset = this.Items.IndexOf(oldValue);
            this.Items[offset] = newValue;
            return oldValue;
        }

        /// <summary>
        /// Indexes the of.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            this.LastTouch = DateTime.Now;

            return this.Items.IndexOf(item);
        }

        /// <summary>
        /// Gets or sets the state of the page fetch state.
        /// </summary>
        /// <value>
        /// The state of the page fetch.
        /// </value>
        public PageFetchStateEnum PageFetchState { get; set; } = PageFetchStateEnum.Placeholders;

        /// <summary>
        /// Gets or sets the wired date time.
        /// </summary>
        /// <value>
        /// The wired date time.
        /// </value>
        public object WiredDateTime { get; set; } = DateTime.MinValue;
    }

}
