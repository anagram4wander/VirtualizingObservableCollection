using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AlphaChiTech.Virtualization
{
    public delegate void OnCountChanged(object sender);

    public class PaginationManager<T> : IItemSourceProvider<T>, IEditableProvider<T>, IReclaimableService
    {
        Dictionary<int, ISourcePage<T>> _Pages = new Dictionary<int, ISourcePage<T>>();

        Dictionary<int, PageDelta> _Deltas = new Dictionary<int, PageDelta>();

        Dictionary<int, CancellationTokenSource> _Tasks = new Dictionary<int, CancellationTokenSource>();

        IPageReclaimer<T> _Reclaimer = null;

        IPageExpiryComparer _ExpiryComparer = null;

        public IPageExpiryComparer ExpiryComparer
        {
            get { return _ExpiryComparer; }
            set { _ExpiryComparer = value; }
        }

        protected void CancelPageRequest(int page)
        {
            lock (_PageLock)
            {
                if (_Tasks.ContainsKey(page))
                {
                    try
                    {
                        _Tasks[page].Cancel();
                    }
                    catch (Exception e1)
                    {

                    }

                    try
                    {
                        _Tasks.Remove(page);
                    }
                    catch (Exception e2)
                    {

                    }
                }
            }
        }

        protected void CancelAllRequests()
        {
            lock (_PageLock)
            {
                var c = _Tasks.Values.ToList();
                foreach (var t in c)
                {
                    try
                    {
                        t.Cancel(false);
                    }
                    catch (Exception e)
                    {

                    }
                }

                _Tasks.Clear();
            }
        }

        protected void RemovePageRequest(int page)
        {
            lock (_PageLock)
            {
                if (_Tasks.ContainsKey(page))
                {
                    try
                    {
                        _Tasks.Remove(page);
                    }
                    catch (Exception e)
                    {

                    }
                }
            }
        }

        protected CancellationTokenSource StartPageRequest(int page)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            CancelPageRequest(page);

            lock (_PageLock)
            {
                if (!_Tasks.ContainsKey(page))
                {
                    _Tasks.Add(page, cts);
                }
                else
                {
                    _Tasks[page] = cts;
                }
            }

            return cts;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaginationManager{T}" /> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="reclaimer">The reclaimer.</param>
        /// <param name="expiryComparer">The expiry comparer.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="maxPages">The maximum pages.</param>
        /// <param name="maxDeltas">The maximum deltas.</param>
        /// <param name="maxDistance">The maximum distance.</param>
        /// <param name="sectionContext">The section context.</param>
        public PaginationManager(IPagedSourceProvider<T> provider,
            IPageReclaimer<T> reclaimer = null,
            IPageExpiryComparer expiryComparer = null,
            int pageSize = 100,
            int maxPages = 100,
            int maxDeltas = -1,
            int maxDistance = -1,
            string sectionContext = ""
            )
        {
            this.PageSize = pageSize;
            this.MaxPages = maxPages;
            this.MaxDeltas = maxDeltas;
            this.MaxDistance = maxDistance;

            this.Provider = provider;

            if (reclaimer != null)
            {
                _Reclaimer = reclaimer;
            }
            else
            {
                _Reclaimer = new PageReclaimOnTouched<T>();
            }

            this.ExpiryComparer = expiryComparer;

            VirtualizationManager.Instance.AddAction(new ReclaimPagesWA(this, sectionContext));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaginationManager{T}" /> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="reclaimer">The reclaimer.</param>
        /// <param name="expiryComparer">The expiry comparer.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="maxPages">The maximum pages.</param>
        /// <param name="maxDeltas">The maximum deltas.</param>
        /// <param name="maxDistance">The maximum distance.</param>
        /// <param name="sectionContext">The section context.</param>
        public PaginationManager(
            IPagedSourceProviderAsync<T> provider,
            IPageReclaimer<T> reclaimer = null,
            IPageExpiryComparer expiryComparer = null,
            int pageSize = 100,
            int maxPages = 100,
            int maxDeltas = -1,
            int maxDistance = -1,
            string sectionContext = ""
            )
        {
            this.PageSize = pageSize;
            this.MaxPages = maxPages;
            this.MaxDeltas = maxDeltas;
            this.MaxDistance = maxDistance;

            this.ProviderAsync = provider;

            if (reclaimer != null)
            {
                _Reclaimer = reclaimer;
            }
            else
            {
                _Reclaimer = new PageReclaimOnTouched<T>();
            }

            this.ExpiryComparer = expiryComparer;

            VirtualizationManager.Instance.AddAction(new ReclaimPagesWA(this, sectionContext));
        }

        /// <summary>
        /// Adds the or update adjustment.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="offsetChange">The offset change.</param>
        public void AddOrUpdateAdjustment(int page, int offsetChange)
        {
            lock (_PageLock)
            {
                if (!_Deltas.ContainsKey(page))
                {
                    if (this.MaxDeltas == -1 || _Deltas.Count < this.MaxDeltas)
                    {
                        _Deltas.Add(page, new PageDelta() { Page = page, Delta = offsetChange });
                    }
                    else
                    {
                        DropAllDeltasAndPages();
                    }
                }
                else
                {
                    var adjustment = _Deltas[page];
                    adjustment.Delta += offsetChange;

                    if (adjustment.Delta == 0)
                    {
                        _Deltas.Remove(page);
                    }
                }
            }
        }

        /// <summary>
        /// Drops all deltas and pages.
        /// </summary>
        protected void DropAllDeltasAndPages()
        {
            lock (_PageLock)
            {
                _Deltas.Clear();
                _Pages.Clear();
                CancelAllRequests();
            }
        }

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

            return ret;
        }

        // The _LastXXX are used for optimizations..
        private int _LastIndex = -1;
        private int _LastPage = -1;
        private int _LastOffset = -1;

        /// <summary>
        /// Clears the optimizations.
        /// </summary>
        protected void ClearOptimizations()
        {
            _LastIndex = -1;
        }

        /// <summary>
        /// Calculates the page and the offset from the index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="indexAdjustment">The index adjustment.</param>
        /// <param name="page">The page.</param>
        /// <param name="inneroffset">The inneroffset.</param>
        protected void CalculateFromIndex(int index, int indexAdjustment, out int page, out int inneroffset, int adjustmentsAppliedToPages = -1)
        {
            if (adjustmentsAppliedToPages == -1)
            {
                // See if we can use some optimization.. aka its the same index as last time..
                if (_LastIndex != -1 && index == _LastIndex)
                {
                    page = _LastPage;
                    inneroffset = _LastOffset;
                    return;
                }
                // See if we can use some optimization... aka its the next index from last..
                if (_LastIndex != -1 && index == _LastIndex + 1)
                {
                    int basepageg = page = _LastPage;

                    inneroffset = _LastOffset + 1;

                    int items = this.PageSize;
                    if (_Deltas.ContainsKey(basepageg))
                    {

                        items += _Deltas[basepageg].Delta;
                    }


                    if (inneroffset >= items)
                    {
                        bool got = false;
                        inneroffset = 0;
                        basepageg = page = page + 1;

                        while (!got)
                        {
                            int itemsg = this.PageSize;
                            if (_Deltas.ContainsKey(basepageg))
                            {

                                itemsg += _Deltas[basepageg].Delta;
                            }

                            if (inneroffset < itemsg) got = true;
                        }
                    }

                    _LastIndex = index;
                    _LastPage = page;
                    _LastOffset = inneroffset;
                    return;
                }
            }

            // First work out the base page from the index and the offset inside that page
            int basepage = page = index / this.PageSize;
            inneroffset = (index + indexAdjustment) - (page * this.PageSize);
            if (adjustmentsAppliedToPages == -1)
            {
                _LastIndex = index;
            }
            _LastPage = basepage;
            _LastOffset = inneroffset;

            // We only need to do the rest if there have been modifications to the page sizes on pages (deltas)
            if (_Deltas.Count > 0)
            {
                int adjustment = 0;

                lock (_PageLock)
                {
                    // First, get the total adjustments for any pages BEFORE the current page..
                    adjustment = (from d in _Deltas.Values where d.Page < basepage && d.Page > adjustmentsAppliedToPages select d.Delta).Sum();
                }

                // If we do have adjustments...
                if (adjustment != 0)
                {
                    // cull down the inner offset by the adjustments (so an extra item reduces the offset by one etc)
                    inneroffset -= adjustment;

                    if (inneroffset < 0)
                    {
                        while (inneroffset < 0 && page >= 0)
                        {
                            page = --basepage;
                            var items = this.PageSize;

                            if (_Deltas.ContainsKey(basepage))
                            {

                                items += _Deltas[basepage].Delta;
                            }


                            inneroffset = items + inneroffset;
                        }


                        // We should be on an earlier page, so recurse using the adjustments
                        //CalculateFromIndex(index - adjustment, adjustment, out page, out inneroffset, basepage);
                    }
                    else if (inneroffset >= this.PageSize)
                    {
                        // If the inneroffset seems to be on a later page, but we need to check to see if this page is expanded
                        if (!_Deltas.ContainsKey(basepage))
                        {
                            // Its not expanded, so recurse in using the adjusted index
                            CalculateFromIndex(index - adjustment, 0, out page, out inneroffset, basepage);
                        }
                        else
                        {
                            // It is expanded, see if the expanded page contains this offset
                            if (inneroffset < (this.PageSize - adjustment))
                            {
                                // Its just fine
                            }
                            else
                            {
                                // No it does not include this offset, so recurse in using the adjusted index
                                CalculateFromIndex(index - adjustment, 0, out page, out inneroffset, basepage);
                            }
                        }
                    }
                }
                else
                {
                    // we dont have any earlier page adjustments, but we might have a short page, so check the offset is within range..
                    PageDelta adjustmentForCurrentPage = null;
                    if (_Deltas.ContainsKey(basepage)) adjustmentForCurrentPage = _Deltas[basepage];
                    if (adjustmentForCurrentPage != null && adjustmentForCurrentPage.Delta < 0)
                    {
                        if (inneroffset >= this.PageSize + adjustmentForCurrentPage.Delta)
                        {
                            // Recurse in using the adjustment for the current page to deal with the offset..
                            CalculateFromIndex(index - adjustmentForCurrentPage.Delta, adjustmentForCurrentPage.Delta, out page, out inneroffset);
                        }
                    }
                }
            }

        }

        IPagedSourceProvider<T> _Provider = null;

        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        /// <value>
        /// The provider.
        /// </value>
        public IPagedSourceProvider<T> Provider
        {
            get { return _Provider; }
            set { _Provider = value; }
        }

        IPagedSourceProviderAsync<T> _ProviderAsync = null;

        /// <summary>
        /// Gets or sets the provider asynchronous.
        /// </summary>
        /// <value>
        /// The provider asynchronous.
        /// </value>
        public IPagedSourceProviderAsync<T> ProviderAsync
        {
            get { return _ProviderAsync; }
            set { _ProviderAsync = value; }
        }

        int _PageSize = 100;

        /// <summary>
        /// Gets or sets the size of the page.
        /// </summary>
        /// <value>
        /// The size of the page.
        /// </value>
        public int PageSize
        {
            get { return _PageSize; }
            set
            {
                DropAllDeltasAndPages();
                _PageSize = value;
            }
        }

        int _MaxPages = 100;

        /// <summary>
        /// Gets or sets the maximum pages.
        /// </summary>
        /// <value>
        /// The maximum pages.
        /// </value>
        public int MaxPages
        {
            get { return _MaxPages; }
            set { _MaxPages = value; }
        }

        int _MaxDeltas = -1;

        /// <summary>
        /// Gets or sets the maximum deltas.
        /// </summary>
        /// <value>
        /// The maximum deltas.
        /// </value>
        public int MaxDeltas
        {
            get { return _MaxDeltas; }
            set { _MaxDeltas = value; }
        }

        int _MaxDistance = -1;

        /// <summary>
        /// Gets or sets the maximum distance.
        /// </summary>
        /// <value>
        /// The maximum distance.
        /// </value>
        public int MaxDistance
        {
            get { return _MaxDistance; }
            set { _MaxDistance = value; }
        }

        protected object _PageLock = new Object();

        /// <summary>
        /// Gets at.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="voc">The voc.</param>
        /// <param name="usePlaceholder">if set to <c>true</c> [use placeholder].</param>
        /// <returns></returns>
        public T GetAt(int index, object voc, bool usePlaceholder = true)
        {
            T ret = default(T);

            int page;
            int offset;

            CalculateFromIndex(index, 0, out page, out offset);

            var datapage = SafeGetPage(page, usePlaceholder, voc, index);

            if (datapage != null) ret = datapage.GetAt(offset);

            if (ret == null)
            {

            }
            return ret;
        }

        /// <summary>
        /// Fills the page.
        /// </summary>
        /// <param name="newPage">The new page.</param>
        /// <param name="pageOffset">The page offset.</param>
        void FillPage(ISourcePage<T> newPage, int pageOffset)
        {

            var data = this.Provider.GetItemsAt(pageOffset, newPage.ItemsPerPage, false);
            newPage.WiredDateTime = data.LoadedAt;
            foreach (var o in data.Items)
            {
                newPage.Append(o, null, this.ExpiryComparer);
            }

            newPage.PageFetchState = PageFetchStateEnum.Fetched;
        }

        /// <summary>
        /// Fills the page from asynchronous provider.
        /// </summary>
        /// <param name="newPage">The new page.</param>
        /// <param name="pageOffset">The page offset.</param>
        void FillPageFromAsyncProvider(ISourcePage<T> newPage, int pageOffset)
        {
            var data = this.ProviderAsync.GetItemsAt(pageOffset, newPage.ItemsPerPage, false).Result;
            newPage.WiredDateTime = data.LoadedAt;
            foreach (var o in data.Items)
            {
                newPage.Append(o, null, this.ExpiryComparer);
            }
            newPage.PageFetchState = PageFetchStateEnum.Fetched;

        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count
        {
            get
            {
                int ret = 0;

                if (this.Provider != null)
                {
                    ret = this.Provider.Count;
                }
                else if (this.ProviderAsync != null)
                {
                    ret = this.ProviderAsync.Count.Result;
                }

                return ret;
            }
        }

        /// <summary>
        /// Indexes the of.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            // Attempt to get the item from the pages, else call  the provider to get it..
            lock (_PageLock)
            {

                foreach (var p in _Pages)
                {
                    int o = p.Value.IndexOf(item);
                    if (o >= 0)
                    {
                        return o + (p.Key * this.PageSize) + (from d in _Deltas.Values where d.Page < p.Key select d.Delta).Sum();
                    }
                }
            }

            if (this.Provider != null)
            {
                return this.Provider.IndexOf(item);
            }
            else
            {
                return this.ProviderAsync.IndexOf(item).Result;
            }
        }

        /// <summary>
        /// Resets the specified count.
        /// </summary>
        /// <param name="count">The count.</param>
        public void OnReset(int count)
        {
            ClearOptimizations();

            if (this.Provider != null)
            {
                this.Provider.OnReset(count);
            }
            else
            {
                this.ProviderAsync.OnReset(count);
            }

            lock (_PageLock)
            {
                DropAllDeltasAndPages();
            }

            RaiseCountChanged(count);
        }

        /// <summary>
        /// Raises the count changed.
        /// </summary>
        /// <param name="count">The count.</param>
        protected void RaiseCountChanged(int count)
        {
            var evnt = this.CountChanged;
            if (evnt != null)
            {
                evnt(this);
            }
        }

        /// <summary>
        /// Occurs when [count changed].
        /// </summary>
        public event OnCountChanged CountChanged;

        #region IEditableProvider<T> Implementation

        public int OnAppend(T item, object timestamp)
        {
            ClearOptimizations();

            var edit = GetProviderAsEditable();

            if (edit != null)
            {
                return edit.OnAppend(item, timestamp);
            }
            else
            {
                return -1;
            }

        }

        protected ISourcePage<T> SafeGetPage(int page, bool allowPlaceholders, object voc, int index)
        {
            ISourcePage<T> ret = null;

            lock (_PageLock)
            {
                if (_Pages.ContainsKey(page))
                {
                    ret = _Pages[page];
                    _Reclaimer.OnPageTouched(ret);
                }
                else
                {
                    PageDelta delta = null;
                    if (_Deltas.ContainsKey(page)) delta = _Deltas[page];
                    int pageSize = this.PageSize;
                    int pageOffset = page * this.PageSize + (from d in _Deltas.Values where d.Page < page select d.Delta).Sum();
                    if (delta != null) pageSize += delta.Delta;
                    var newPage = this._Reclaimer.MakePage(page, pageSize);
                    _Pages.Add(page, newPage);

                    if (this.Provider != null)
                    {
                        FillPage(newPage, pageOffset);

                        ret = newPage;
                    }
                    else
                    {
                        bool up = allowPlaceholders;

                        if (up)
                        {
                            object o = this.ProviderAsync.GetPlaceHolder(newPage.Page, 0);
                            if (o == null) up = false;
                        }

                        if (up && voc != null)
                        {
                            // Fill with placeholders
                            for (int loop = 0; loop < pageSize; loop++)
                            {
                                newPage.Append(this.ProviderAsync.GetPlaceHolder(newPage.Page, loop), null, this.ExpiryComparer);
                            }

                            ret = newPage;

                            CancellationTokenSource cts = StartPageRequest(newPage.Page);
                            Task.Run(() => DoRealPageGet(voc, newPage, pageOffset, index, cts));
                        }
                        else
                        {
                            FillPageFromAsyncProvider(newPage, pageOffset);
                            ret = newPage;
                        }
                    }
                }
            }

            return ret;
        }

        private async void DoRealPageGet(Object voc, ISourcePage<T> page, int pageOffset, int index, CancellationTokenSource cts)
        {
            VirtualizingObservableCollection<T> realVOC = (VirtualizingObservableCollection<T>)voc;

            if (realVOC != null)
            {
                var data = await ProviderAsync.GetItemsAt(pageOffset, page.ItemsPerPage, false);

                page.WiredDateTime = DateTime.Now; // TODO: Should come from the provider ??

                foreach (var item in data.Items)
                {
                    VirtualizationManager.Instance.RunOnUI(new PlaceholderReplaceWA<T>(realVOC, page.GetAt(index), item, index));
                    index++;
                }
            }

            page.PageFetchState = PageFetchStateEnum.Fetched;

            RemovePageRequest(page.Page);
        }

        protected bool IsPageWired(int page)
        {
            bool wired = false;

            lock (_PageLock)
            {
                if (_Pages.ContainsKey(page)) wired = true;
            }

            return wired;
        }

        public void OnInsert(int index, T item, object timestamp)
        {
            int page; int offset;

            CalculateFromIndex(index, 0, out page, out offset);

            if (IsPageWired(page))
            {
                var dataPage = SafeGetPage(page, false, null, index);
                dataPage.InsertAt(offset, item, timestamp, this.ExpiryComparer);
            }
            AddOrUpdateAdjustment(page, 1);

            var edit = GetProviderAsEditable();
            if (edit != null)
            {
                edit.OnInsert(index, item, timestamp);
            }

            ClearOptimizations();
        }

        public void OnRemove(int index, T item, object timestamp)
        {
            int page; int offset;

            CalculateFromIndex(index, 0, out page, out offset);

            if (IsPageWired(page))
            {
                var dataPage = SafeGetPage(page, false, null, index);
                dataPage.RemoveAt(offset, timestamp, this.ExpiryComparer);
            }
            AddOrUpdateAdjustment(page, -1);

            var edit = GetProviderAsEditable();
            if (edit != null)
            {
                edit.OnRemove(index, item, timestamp);
            }

            ClearOptimizations();
        }

        public void OnReplace(int index, T oldItem, T newItem, object timestamp)
        {
            int page; int offset;

            CalculateFromIndex(index, 0, out page, out offset);

            if (IsPageWired(page))
            {
                var dataPage = SafeGetPage(page, false, null, index);
                dataPage.ReplaceAt(offset, oldItem, newItem, timestamp, this.ExpiryComparer);
            }

            var edit = GetProviderAsEditable();
            if (edit != null)
            {
                edit.OnReplace(index, oldItem, newItem, timestamp);
            }
        }

        #endregion IEditableProvider<T> Implementation


        public void RunClaim(string sectionContext = "")
        {
            if (_Reclaimer != null)
            {
                int needed = 0;

                lock (_PageLock)
                {
                    needed = Math.Max(0, _Pages.Count - this.MaxPages);
                    if (needed != 0)
                    {
                        var l = _Reclaimer.ReclaimPages(_Pages.Values, needed, sectionContext).ToList();

                        foreach (var p in l)
                        {
                            if (p.Page > 0)
                            {
                                lock (_Pages)
                                {
                                    if (_Pages.ContainsKey(p.Page))
                                    {
                                        _Pages.Remove(p.Page);
                                        _Reclaimer.OnPageReleased(p);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

}
