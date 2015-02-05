using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public class SourcePage<T> : ISourcePage<T>
    {
        protected List<T> _Items = new List<T>();


        /// <summary>
        /// Gets or sets the page.
        /// </summary>
        /// <value>
        /// The page.
        /// </value>
        public int Page { get; set; }

        private List<SourcePagePendingUpdates> _PendingUpdates = new List<SourcePagePendingUpdates>();

        public List<SourcePagePendingUpdates> PendingUpdates
        {
            get { return _PendingUpdates; }
        }

        /// <summary>
        /// Gets or sets the items per page.
        /// </summary>
        /// <value>
        /// The items per page.
        /// </value>
        public int ItemsPerPage { get; set; }

        private List<int> _ReplaceNeededList = new List<int>();

        /// <summary>
        /// Gets a value indicating whether [can reclaim page].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [can reclaim page]; otherwise, <c>false</c>.
        /// </value>
        public bool CanReclaimPage
        {
            get
            {
                bool ret = true;
                if (this._PageFetchState == PageFetchStateEnum.Placeholders) ret = false;
                return ret;
            }
        }

        /// <summary>
        /// Determines whether it is safe to update into a page where the pending update was generated at a given time.
        /// </summary>
        /// <param name="updatedAt">The updated happened at this datetime.</param>
        /// <returns></returns>
        public bool IsSafeToUpdate(IPageExpiryComparer comparer, object updatedAt)
        {
            bool ret = true;

            if (comparer != null)
            {
                ret = comparer.IsUpdateValid(this.WiredDateTime, updatedAt);
            }

            //if(updatedAt.HasValue && updatedAt.Value != DateTime.MinValue)
            //{
            //    if(updatedAt.Value< this.WiredDateTime)
            //    {
            //        ret = false;
            //    }
            //}

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
            T ret = default(T);

            if (this._PageFetchState == PageFetchStateEnum.Placeholders) _ReplaceNeededList.Add(offset);

            if (_Items.Count > offset) ret = _Items[offset];

            LastTouch = DateTime.Now;

            return ret;
        }

        public bool ReplaceNeeded(int offset)
        {
            bool ret = false;

            if (_ReplaceNeededList.Contains(offset)) ret = true;

            return ret;
        }

        /// <summary>
        /// Appends the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public int Append(T item, object updatedAt, IPageExpiryComparer comparer)
        {
            _Items.Add(item);

            LastTouch = DateTime.Now;

            return _Items.IndexOf(item);
        }

        /// <summary>
        /// Inserts at.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="item">The item.</param>
        /// <param name="updatedAt">The updated at.</param>
        public void InsertAt(int offset, T item, object updatedAt, IPageExpiryComparer comparer)
        {
            if (IsSafeToUpdate(comparer, updatedAt))
            {
                if (_Items.Count > offset)
                {
                    _Items.Insert(offset, item);
                }
                else
                {
                    _Items.Add(item);
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
            bool removed = true;

            if (IsSafeToUpdate(comparer, updatedAt))
            {
                _Items.RemoveAt(offset);
            }

            return removed;
        }

        /// <summary>
        /// Replaces at.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="updatedAt">The updated at.</param>
        public void ReplaceAt(int offset, T oldValue, T newValue, object updatedAt, IPageExpiryComparer comparer)
        {
            if (IsSafeToUpdate(comparer, updatedAt))
            {
                _Items[offset] = newValue;
            }
        }

        /// <summary>
        /// Indexes the of.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            LastTouch = DateTime.Now;

            return _Items.IndexOf(item);
        }

        private PageFetchStateEnum _PageFetchState = PageFetchStateEnum.Placeholders;

        /// <summary>
        /// Gets or sets the state of the page fetch state.
        /// </summary>
        /// <value>
        /// The state of the page fetch.
        /// </value>
        public PageFetchStateEnum PageFetchState
        {
            get { return _PageFetchState; }
            set { _PageFetchState = value; }
        }

        private Object _WiredDateTime = DateTime.MinValue;

        /// <summary>
        /// Gets or sets the wired date time.
        /// </summary>
        /// <value>
        /// The wired date time.
        /// </value>
        public object WiredDateTime
        {
            get { return _WiredDateTime; }
            set { _WiredDateTime = value; }
        }


    }

}
