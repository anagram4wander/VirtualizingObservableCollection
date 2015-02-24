using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AlphaChiTech.Virtualization
{

    public class PaginationManager<T> : IItemSourceProvider<T>, IEditableProvider<T>, IReclaimableService, INotifyCountChanged
    {
        Dictionary<int, ISourcePage<T>> _Pages = new Dictionary<int, ISourcePage<T>>();

        Dictionary<int, PageDelta> _Deltas = new Dictionary<int, PageDelta>();

        Dictionary<int, CancellationTokenSource> _Tasks = new Dictionary<int, CancellationTokenSource>();

        IPageReclaimer<T> _Reclaimer = null;

        IPageExpiryComparer _ExpiryComparer = null;

        bool _HasGotCount = false;
        int _LocalCount = 0;

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

            if (provider is IPagedSourceProviderAsync<T>)
            {
                this.ProviderAsync = (IPagedSourceProviderAsync<T>)provider;
            }
            else
            {
                this.Provider = provider;
            }

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
        public int AddOrUpdateAdjustment(int page, int offsetChange)
        {
            int ret = 0;

            lock (_PageLock)
            {
                if (!_Deltas.ContainsKey(page))
                {
                    if (this.MaxDeltas == -1 || _Deltas.Count < this.MaxDeltas)
                    {
                        ret = offsetChange;
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

                    ret = adjustment.Delta;
                }
            }

            return ret;
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

        /* Old implementation

        /// <summary>
        /// Calculates the page and the offset from the index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="indexAdjustment">The index adjustment.</param>
        /// <param name="page">The page.</param>
        /// <param name="inneroffset">The inneroffset.</param>
        protected void CalculateFromIndex(int index, int indexAdjustment, out int page, out int inneroffset, int adjustmentsAppliedToPages = -1)
        {
            if (adjustmentsAppliedToPages == -1 && 1==0)
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
                            var delta = _Deltas[basepage];
                            // It is expanded, see if the expanded page contains this offset
                            if (inneroffset < (this.PageSize + delta.Delta))
                            {
                                // Its just fine
                            }
                            else
                            {
                                // No it does not include this offset, so recurse in using the adjusted index
                                CalculateFromIndex(index + delta.Delta, 0, out page, out inneroffset, basepage);
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
                            //CalculateFromIndex(index - adjustmentForCurrentPage.Delta, 0, out page, out inneroffset);

                            inneroffset += adjustmentForCurrentPage.Delta;

                            while(inneroffset<0)
                            {
                                page = ++basepage;

                                var items = this.PageSize;

                                if (_Deltas.ContainsKey(basepage))
                                {

                                    items += _Deltas[basepage].Delta;
                                }

                                inneroffset += items;
                            }      
                        }
                    }
                }
            }

            if (adjustmentsAppliedToPages == -1)
            {
                _LastIndex = index;

                _LastPage = basepage;
                _LastOffset = inneroffset;
            }

        }
        */

        int _BasePage = 0;

        protected void CalculateFromIndex(int index, out int page, out int inneroffset)
        {
            // First work out the base page from the index and the offset inside that page
            int basepage = page = (index / this.PageSize) + _BasePage;
            inneroffset = (index+(_BasePage*this.PageSize)) - (page * this.PageSize);

            // We only need to do the rest if there have been modifications to the page sizes on pages (deltas)
            if (_Deltas.Count > 0)
            {
                // Get the adjustment BEFORE checking for a short page, because we are going to adjust for that first..
                int adjustment = 0;

                lock (_PageLock)
                {
                    // First, get the total adjustments for any pages BEFORE the current page..
                    adjustment = (from d in _Deltas.Values where d.Page < basepage select d.Delta).Sum();
                }

                // Now check to see if we are currently in a short page - in which case we need to adjust for that
                if (_Deltas.ContainsKey(page))
                {
                    int delta = _Deltas[page].Delta;
        
                    if(delta<0)
                    {
                        // In a short page, are we over the edge ?
                        if(inneroffset >= this.PageSize+delta)
                        {
                            int step = inneroffset-(this.PageSize+delta-1);
                            inneroffset -= step;
                            DoStepForward(ref page, ref inneroffset, step);
                        }
                    }
                }

                // If we do have adjustments...
                if (adjustment != 0)
                {
                    if(adjustment>0)
                    {
                        // items have been added to earlier pages, so we need to step back
                        DoStepBackwards(ref page, ref inneroffset, adjustment);
                    }
                    else
                    {
                        // items have been removed from earlier pages, so we need to step forward
                        DoStepForward(ref page, ref inneroffset, Math.Abs(adjustment));
                    }
                }

            }

        }

        private void DoStepBackwards(ref int page, ref int offset, int stepAmount)
        {
            bool done = false;

            while(!done)
            {
                int items = this.PageSize;
                if (_Deltas.ContainsKey(page)) items += _Deltas[page].Delta;
                if(offset - stepAmount < 0)
                {
                    stepAmount -= (offset+1);
                    page--;
                    items = this.PageSize;
                    if (_Deltas.ContainsKey(page)) items += _Deltas[page].Delta;
                    offset = items-1;
                } 
                else
                {
                    offset -= stepAmount;
                    done = true;
                }
            }
        }

        private void DoStepForward(ref int page, ref int offset, int stepAmount)
        {
            bool done = false;

            while(!done)
            {
                int items = this.PageSize;
                if (_Deltas.ContainsKey(page)) items += _Deltas[page].Delta;
                if(items<=offset+stepAmount)
                {
                    stepAmount -= (items)-offset;
                    offset = 0;
                    page++;
                }
                else
                {
                    offset += stepAmount;
                    done = true;
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

            CalculateFromIndex(index, out page, out offset);

            var datapage = SafeGetPage(page, usePlaceholder, voc, index);

            if (datapage != null) ret = datapage.GetAt(offset);

            if (ret == null)
            {

            }

            //Debug.WriteLine("Get at index:" + index + "returned:" + ret.ToString() + " page=" + page + " offset=" + offset);
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
            var data = this.ProviderAsync.GetItemsAt(pageOffset, newPage.ItemsPerPage, false);
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
        public int GetCount(bool asyncOK)
        {

            int ret = 0;

            if (!_HasGotCount)
            {
                lock (this)
                {
                    if (!IsAsync)
                    {
                        ret = this.Provider.Count;
                    }
                    else
                    {
                        if (!asyncOK)
                        {
                            ret = this.ProviderAsync.Count;
                            _LocalCount = ret;
                        }
                        else
                        {
                            ret = 0;
                            var cts = StartPageRequest(Int32.MinValue);
                            GetCountAsync(cts);
                        }
                    }

                    _HasGotCount = true;
                }
            }

            return _LocalCount;

        }

        private async void GetCountAsync(CancellationTokenSource cts)
        {
            if (!cts.IsCancellationRequested)
            {
                int ret = await this.ProviderAsync.GetCountAsync();

                if (!cts.IsCancellationRequested)
                {
                    lock (this)
                    {
                        _HasGotCount = true;
                        _LocalCount = ret;
                    }
                }

                if (!cts.IsCancellationRequested) 
                    this.RaiseCountChanged(true, _LocalCount);
            }

            RemovePageRequest(Int32.MinValue);
        }

        /// <summary>
        /// Gets the Index of item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>the index of the item, or -1 if not found</returns>
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
                        return o + ((p.Key - _BasePage) * this.PageSize) + (from d in _Deltas.Values where d.Page < p.Key select d.Delta).Sum();
                    }
                }
            }

            if (!IsAsync)
            {
                return this.Provider.IndexOf(item);
            }
            else
            {
                return this.ProviderAsync.IndexOf(item);
            }
        }

        /// <summary>
        /// Resets the specified count.
        /// </summary>
        /// <param name="count">The count.</param>
        public void OnReset(int count)
        {
            CancelAllRequests();

            ClearOptimizations();
            _HasGotCount = false;

            if (!IsAsync)
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

            RaiseCountChanged(true, count);

            GetCount(true);
        }

        /// <summary>
        /// Raises the count changed.
        /// </summary>
        /// <param name="count">The count.</param>
        protected void RaiseCountChanged(bool needsReset, int count)
        {
            var evnt = this.CountChanged;
            if (evnt != null)
            {
                evnt(this, new CountChangedEventArgs() { NeedsReset = needsReset, Count = count });
            }
        }

        /// <summary>
        /// Occurs when [count changed].
        /// </summary>
        public event OnCountChanged CountChanged;

        #region IEditableProvider<T> Implementation

        /// <summary>
        /// Called when [append].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns></returns>
        public int OnAppend(T item, object timestamp)
        {
            ClearOptimizations();

            int index = _LocalCount;

            int page; int offset;

            if (!_HasGotCount) EnsureCount();

            CalculateFromIndex(index, out page, out offset);

            if (IsPageWired(page))
            {
                bool shortpage = false;
                var dataPage = SafeGetPage(page, false, null, index);
                if (dataPage.ItemsPerPage < this.PageSize) shortpage = true;

                dataPage.Append(item, timestamp, this.ExpiryComparer);

                if(shortpage)
                {
                    dataPage.ItemsPerPage++;
                }
                else
                {
                    AddOrUpdateAdjustment(page, 1);
                }

            }

            _LocalCount++;

            ClearOptimizations();

            if (this.IsAsync)
            {
                var test = this.GetAt(index, this, false);
            }


            var edit = GetProviderAsEditable();
            if (edit != null)
            {
                edit.OnInsert(index, item, timestamp);
            }

            ClearOptimizations();

            return index;
        }

        /// <summary>
        /// Gets the page, if use placeholders is false - then gets page sync else async.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="allowPlaceholders">if set to <c>true</c> [allow placeholders].</param>
        /// <param name="voc">The voc.</param>
        /// <param name="index">The index that this page refers to (effectively the pageoffset.</param>
        /// <returns></returns>
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
                    int pageOffset = (page - _BasePage) * this.PageSize + (from d in _Deltas.Values where d.Page < page select d.Delta).Sum();
                    int pageSize = Math.Min(this.PageSize, this.GetCount(false)-pageOffset);
                    if (delta != null) pageSize += delta.Delta;
                    var newPage = this._Reclaimer.MakePage(page, pageSize);
                    _Pages.Add(page, newPage);

                    if (!IsAsync)
                    {
                        FillPage(newPage, pageOffset);

                        ret = newPage;
                    }
                    else
                    {
                        bool up = allowPlaceholders;                        

                        if (up && voc != null)
                        {
                            // Fill with placeholders
                            //Debug.WriteLine("Filling with placeholders, pagesize=" + pageSize);
                            for (int loop = 0; loop < pageSize; loop++)
                            {
                                newPage.Append(this.ProviderAsync.GetPlaceHolder(index+loop, newPage.Page, loop), null, this.ExpiryComparer);
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
            //Debug.WriteLine("DoRealPageGet: pageOffset=" + pageOffset + " index=" + index);
            VirtualizingObservableCollection<T> realVOC = (VirtualizingObservableCollection<T>)voc;
            List<PlaceholderReplaceWA<T>> listOfReplaces = new List<PlaceholderReplaceWA<T>>();

            if (realVOC != null)
            {
                if (cts.IsCancellationRequested) return;

                var data = await ProviderAsync.GetItemsAtAsync(pageOffset, page.ItemsPerPage, false);

                if (cts.IsCancellationRequested) return;

                page.WiredDateTime = data.LoadedAt;

                int i = 0;
                foreach (var item in data.Items)
                {
                    if (cts.IsCancellationRequested)
                    {
                        RemovePageRequest(page.Page);
                        return;
                    }

                    ClearOptimizations();
                    if(page.ReplaceNeeded(i))
                    {
                        var old = page.GetAt(i);
                        if (old == null)
                        {

                        }

                        ClearOptimizations();
                        //Debug.WriteLine("Replacing:" + old.ToString() + " with " + item.ToString());

                        page.ReplaceAt(i, old, item, null, null);
                        //VirtualizationManager.Instance.RunOnUI(new PlaceholderReplaceWA<T>(realVOC, old, item, pageOffset+i));
                        listOfReplaces.Add(new PlaceholderReplaceWA<T>(realVOC, old, item, pageOffset + i));
                    }
                    else
                    {
                        page.ReplaceAt(i, default(T), item, null, null);
                    }

                    i++;
                }

            }

            page.PageFetchState = PageFetchStateEnum.Fetched;

            ClearOptimizations();
            foreach (var replace in listOfReplaces)
            {
                if (cts.IsCancellationRequested)
                {
                    RemovePageRequest(page.Page);
                    return;
                }
                VirtualizationManager.Instance.RunOnUI(replace);
            }

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

            if (!_HasGotCount) EnsureCount();

            CalculateFromIndex(index, out page, out offset);

            if (IsPageWired(page))
            {
                var dataPage = SafeGetPage(page, false, null, index);
                dataPage.InsertAt(offset, item, timestamp, this.ExpiryComparer);
            }
            int adj = AddOrUpdateAdjustment(page, 1);

            if(page == _BasePage && adj == this.PageSize*2)
            {
                lock (_PageLock)
                {
                    if (IsPageWired(page))
                    {
                        var dataPage = SafeGetPage(page, false, null, index);
                        ISourcePage<T> newdataPage = null;
                        if (IsPageWired(page - 1))
                        {
                            newdataPage = SafeGetPage(page-1, false, null, index);
                        }
                        else
                        {
                            newdataPage = this._Reclaimer.MakePage(page - 1, this.PageSize);
                            _Pages.Add(page - 1, newdataPage);
                        }

                        for (int loop = 0; loop < this.PageSize; loop++)
                        {
                            var i = dataPage.GetAt(0);

                            dataPage.RemoveAt(0, null, null);
                            newdataPage.Append(i, null, null);
                        }

                    }

                    AddOrUpdateAdjustment(page, -this.PageSize);

                    _BasePage--;
                }
            }

            if (this.IsAsync)
            {
                var test = this.GetAt(index, this, false);
            }

            var edit = GetProviderAsEditable();
            if (edit != null)
            {
                edit.OnInsert(index, item, timestamp);
            }

            _LocalCount++;

            ClearOptimizations();
        }

        void EnsureCount()
        {
            GetCount(false);
        }

        protected bool IsAsync
        {
            get
            {
                return _ProviderAsync != null ? true : false;
            }
        }

        public void OnRemove(int index, T item, object timestamp)
        {
            int page; int offset;

            if (!_HasGotCount) EnsureCount();

            CalculateFromIndex(index, out page, out offset);

            if (IsPageWired(page))
            {
                var dataPage = SafeGetPage(page, false, null, index);
                dataPage.RemoveAt(offset, timestamp, this.ExpiryComparer);
            }
            AddOrUpdateAdjustment(page, -1);

            if (page == _BasePage)
            {
                int items = this.PageSize;
                if (_Deltas.ContainsKey(page)) items += _Deltas[page].Delta;
                if (items == 0)
                {
                    _Deltas.Remove(page);
                    _BasePage++;
                }
            }

            if (this.IsAsync)
            {
                var test = this.GetAt(index, this, false);
            }

            var edit = GetProviderAsEditable();
            if (edit != null)
            {
                edit.OnRemove(index, item, timestamp);
            }

            _LocalCount--;

            ClearOptimizations();
        }

        public void OnReplace(int index, T oldItem, T newItem, object timestamp)
        {
            int page; int offset;

            CalculateFromIndex(index, out page, out offset);

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
                            if (p.Page != _BasePage)
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
