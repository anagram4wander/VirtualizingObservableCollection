using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlphaChiTech.VirtualizingCollection.Actions;
using AlphaChiTech.VirtualizingCollection.Interfaces;

namespace AlphaChiTech.VirtualizingCollection.Pageing
{

    public interface IAsyncResetProvider
    {
        Task<int> GetCountAsync();
    }

    public interface IProviderPreReset
    {
        void OnBeforeReset();
    }

    public class PaginationManager<T> : IItemSourceProvider<T>, IEditableProvider<T>, IReclaimableService, IAsyncResetProvider, IProviderPreReset, INotifyCountChanged
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
            get { return this._ExpiryComparer; }
            set { this._ExpiryComparer = value; }
        }

        protected void CancelPageRequest(int page)
        {
            lock (this._PageLock)
            {
                if (this._Tasks.ContainsKey(page))
                {
                    try
                    {
                        this._Tasks[page].Cancel();
                    }
                    catch (Exception e1)
                    {

                    }

                    try
                    {
                        this._Tasks.Remove(page);
                    }
                    catch (Exception e2)
                    {

                    }
                }
            }
        }

        protected void CancelAllRequests()
        {
            lock (this._PageLock)
            {
                var c = this._Tasks.Values.ToList();
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

                this._Tasks.Clear();
            }
        }

        protected void RemovePageRequest(int page)
        {
            lock (this._PageLock)
            {
                if (this._Tasks.ContainsKey(page))
                {
                    try
                    {
                        this._Tasks.Remove(page);
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

            
            this.CancelPageRequest(page);

            lock (this._PageLock)
            {
                if (!this._Tasks.ContainsKey(page))
                {
                    this._Tasks.Add(page, cts);
                }
                else
                {
                    this._Tasks[page] = cts;
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
                this._Reclaimer = reclaimer;
            }
            else
            {
                this._Reclaimer = new PageReclaimOnTouched<T>();
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

            lock (this._PageLock)
            {
                if (!this._Deltas.ContainsKey(page))
                {
                    if (this.MaxDeltas == -1 || this._Deltas.Count < this.MaxDeltas)
                    {
                        ret = offsetChange;
                        this._Deltas.Add(page, new PageDelta() { Page = page, Delta = offsetChange });
                    }
                    else
                    {
                        this.DropAllDeltasAndPages();
                    }
                }
                else
                {
                    var adjustment = this._Deltas[page];
                    adjustment.Delta += offsetChange;

                    if (adjustment.Delta == 0)
                    {
                        this._Deltas.Remove(page);
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
            lock (this._PageLock)
            {
                this._Deltas.Clear();
                this._Pages.Clear();
                this._BasePage = 0;
                this.CancelAllRequests();
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
            this._LastIndex = -1;
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
            int basepage = page = (index / this.PageSize) + this._BasePage;
            inneroffset = (index+(this._BasePage*this.PageSize)) - (page * this.PageSize);

            // We only need to do the rest if there have been modifications to the page sizes on pages (deltas)
            if (this._Deltas.Count > 0)
            {
                // Get the adjustment BEFORE checking for a short page, because we are going to adjust for that first..
                int adjustment = 0;

                lock (this._PageLock)
                {
                    // First, get the total adjustments for any pages BEFORE the current page..
                    adjustment = (from d in this._Deltas.Values where d.Page < basepage select d.Delta).Sum();
                }

                // Now check to see if we are currently in a short page - in which case we need to adjust for that
                if (this._Deltas.ContainsKey(page))
                {
                    int delta = this._Deltas[page].Delta;
        
                    if(delta<0)
                    {
                        // In a short page, are we over the edge ?
                        if(inneroffset >= this.PageSize+delta)
                        {
                            int step = inneroffset-(this.PageSize+delta-1);
                            inneroffset -= step;
                            this.DoStepForward(ref page, ref inneroffset, step);
                        }
                    }
                }

                // If we do have adjustments...
                if (adjustment != 0)
                {
                    if(adjustment>0)
                    {
                        // items have been added to earlier pages, so we need to step back
                        this.DoStepBackwards(ref page, ref inneroffset, adjustment);
                    }
                    else
                    {
                        // items have been removed from earlier pages, so we need to step forward
                        this.DoStepForward(ref page, ref inneroffset, Math.Abs(adjustment));
                    }
                }

            }

        }

        private int _StepToJumpThreashold = 10;

        public int StepToJumpThreashold
        {
            get { return this._StepToJumpThreashold; }
            set { this._StepToJumpThreashold = value; }
        }

        private void DoStepBackwards(ref int page, ref int offset, int stepAmount)
        {
            bool done = false;
            int ignoreSteps = -1;

            while(!done)
            {

                if (stepAmount > this.PageSize * this.StepToJumpThreashold && ignoreSteps <= 0)
                {
                    int targetPage =  page - stepAmount/this.PageSize;
                    int sourcePage = page;
                    var adj = (from a in this._Deltas.Values where a.Page >= targetPage && a.Page <= sourcePage orderby a.Page select a);
                    if(adj.Count() == 0)
                    {
                        page = targetPage;
                        stepAmount -= (sourcePage - targetPage) * this.PageSize;

                        if(stepAmount == 0)
                        {
                            done = true;
                        }
                    } else if(adj.Last().Page < page-2)
                    {
                        targetPage = adj.Last().Page + 1;
                        page = targetPage;
                        stepAmount -= (sourcePage - targetPage) * this.PageSize;

                        if (stepAmount == 0)
                        {
                            done = true;
                        }
                    }
                    else
                    {
                        ignoreSteps = sourcePage - adj.Last().Page;
                    }
                }

                if (!done)
                {
                    int items = this.PageSize;
                    if (this._Deltas.ContainsKey(page)) items += this._Deltas[page].Delta;
                    if (offset - stepAmount < 0)
                    {
                        stepAmount -= (offset + 1);
                        page--;
                        items = this.PageSize;
                        if (this._Deltas.ContainsKey(page)) items += this._Deltas[page].Delta;
                        offset = items - 1;
                    }
                    else
                    {
                        offset -= stepAmount;
                        done = true;
                    }

                    ignoreSteps--;
                }
            }
        }

        private void DoStepForward(ref int page, ref int offset, int stepAmount)
        {
            bool done = false;
            int ignoreSteps = -1;

            while(!done)
            {

                if (stepAmount > this.PageSize * this.StepToJumpThreashold && ignoreSteps <= 0)
                {
                    int targetPage = page + stepAmount / this.PageSize;
                    int sourcePage = page;
                    var adj = (from a in this._Deltas.Values where a.Page <= targetPage && a.Page >= sourcePage orderby a.Page select a);
                    if (adj.Count() == 0)
                    {
                        page = targetPage;
                        stepAmount -= (targetPage - sourcePage) * this.PageSize;

                        if (stepAmount == 0)
                        {
                            done = true;
                        }
                    }
                    else if (adj.Last().Page > page - 2)
                    {
                        targetPage = adj.Last().Page - 1;
                        page = targetPage;
                        stepAmount -= (targetPage - sourcePage) * this.PageSize;

                        if (stepAmount == 0)
                        {
                            done = true;
                        }
                    } else
                    {
                        ignoreSteps = adj.Last().Page - sourcePage;
                    }
                }

                if (!done)
                {
                    int items = this.PageSize;
                    if (this._Deltas.ContainsKey(page)) items += this._Deltas[page].Delta;
                    if (items <= offset + stepAmount)
                    {
                        stepAmount -= (items) - offset;
                        offset = 0;
                        page++;
                    }
                    else
                    {
                        offset += stepAmount;
                        done = true;
                    }

                    ignoreSteps--;
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
            get { return this._Provider; }
            set { this._Provider = value; }
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
            get { return this._ProviderAsync; }
            set { this._ProviderAsync = value; }
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
            get { return this._PageSize; }
            set
            {
                this.DropAllDeltasAndPages();
                this._PageSize = value;
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
            get { return this._MaxPages; }
            set { this._MaxPages = value; }
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
            get { return this._MaxDeltas; }
            set { this._MaxDeltas = value; }
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
            get { return this._MaxDistance; }
            set { this._MaxDistance = value; }
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

            this.CalculateFromIndex(index, out page, out offset);

            var datapage = this.SafeGetPage(page, usePlaceholder, voc, index);

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

            if (!this._HasGotCount)
            {
                lock (this)
                {
                    if (!this.IsAsync)
                    {
                        ret = this.Provider.Count;
                        this._LocalCount = ret;
                    }
                    else
                    {
                        if (!asyncOK)
                        {
                            ret = this.ProviderAsync.GetCountAsync().Result;
                            //ret = this.ProviderAsync.Count;
                            this._LocalCount = ret;
                        }
                        else
                        {
                            ret = 0;
                            var cts = this.StartPageRequest(Int32.MinValue);
                            this.GetCountAsync(cts);
                        }
                    }

                    this._HasGotCount = true;
                }
            }

            return this._LocalCount;

        }

        public async Task<int> GetCountAsync()
        {
            int ret = 0;


            if (!this.IsAsync)
            {
                ret = this.Provider.Count;
            }
            else
            {
                ret = await this.ProviderAsync.GetCountAsync();
            }

            this._HasGotCount = true;

            return ret;
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
                        this._HasGotCount = true;
                        this._LocalCount = ret;
                    }
                }

                if (!cts.IsCancellationRequested) 
                    this.RaiseCountChanged(true, this._LocalCount);
            }

            this.RemovePageRequest(Int32.MinValue);
        }

        /// <summary>
        /// Gets the Index of item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>the index of the item, or -1 if not found</returns>
        public int IndexOf(T item)
        {
            // Attempt to get the item from the pages, else call  the provider to get it..
            lock (this._PageLock)
            {

                foreach (var p in this._Pages)
                {
                    int o = p.Value.IndexOf(item);
                    if (o >= 0)
                    {
                        return o + ((p.Key - this._BasePage) * this.PageSize) + (from d in this._Deltas.Values where d.Page < p.Key select d.Delta).Sum();
                    }
                }
            }

            if (!this.IsAsync)
            {
                return this.Provider.IndexOf(item);
            }
            else
            {
                return this.ProviderAsync.IndexOfAsync( item ).Result;
                //return this.ProviderAsync.IndexOf(item);
            }
        }

        /// <summary>
        /// Resets the specified count.
        /// </summary>
        /// <param name="count">The count.</param>
        public void OnReset(int count)
        {
            this.CancelAllRequests();

            lock (this._PageLock)
            {
                this.DropAllDeltasAndPages();
            }

            this.ClearOptimizations();

            if (count < 0)
            {
                this._HasGotCount = false;
            }
            else
            {
                lock (this)
                {
                    this._LocalCount = count;
                    this._HasGotCount = true;
                }
            }

            if (!this.IsAsync)
            {
                this.Provider.OnReset(count);
            }
            else
            {
                this.ProviderAsync.OnReset(count);
            }

            if(count >= -1)
                this.RaiseCountChanged(true, count);
            
        }

        public void OnBeforeReset()
        {
            if(!this.IsAsync)
            {
                if(this.Provider is IProviderPreReset)
                {
                    (this.Provider as IProviderPreReset).OnBeforeReset();
                }
            }
            else
            {
                if(this.ProviderAsync is IProviderPreReset)
                {
                    (this.ProviderAsync as IProviderPreReset).OnBeforeReset();
                }
            }
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
            this.ClearOptimizations();

            int index = this._LocalCount;

            int page; int offset;

            if (!this._HasGotCount) this.EnsureCount();

            this.CalculateFromIndex(index, out page, out offset);

            if (this.IsPageWired(page))
            {
                bool shortpage = false;
                var dataPage = this.SafeGetPage(page, false, null, index);
                if (dataPage.ItemsPerPage < this.PageSize) shortpage = true;

                dataPage.Append(item, timestamp, this.ExpiryComparer);

                if(shortpage)
                {
                    dataPage.ItemsPerPage++;
                }
                else
                {
                    this.AddOrUpdateAdjustment(page, 1);
                }

            }

            this._LocalCount++;

            this.ClearOptimizations();

            if (this.IsAsync)
            {
                var test = this.GetAt(index, this, false);
            }


            var edit = this.GetProviderAsEditable();
            if (edit != null)
            {
                edit.OnInsert(index, item, timestamp);
            }

            this.ClearOptimizations();

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

            lock (this._PageLock)
            {
                if (this._Pages.ContainsKey(page))
                {
                    ret = this._Pages[page];
                    this._Reclaimer.OnPageTouched(ret);
                }
                else
                {
                    PageDelta delta = null;
                    if (this._Deltas.ContainsKey(page)) delta = this._Deltas[page];
                    int pageOffset = (page - this._BasePage) * this.PageSize + (from d in this._Deltas.Values where d.Page < page select d.Delta).Sum();
                    int pageSize = Math.Min(this.PageSize, this.GetCount(false)-pageOffset);
                    if (delta != null) pageSize += delta.Delta;
                    var newPage = this._Reclaimer.MakePage(page, pageSize);
                    this._Pages.Add(page, newPage);

                    if (!this.IsAsync)
                    {
                        this.FillPage(newPage, pageOffset);

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
                                newPage.Append(this.ProviderAsync.GetPlaceHolder(newPage.Page * pageSize + loop, newPage.Page, loop), null, this.ExpiryComparer);
                            }

                            ret = newPage;

                            CancellationTokenSource cts = this.StartPageRequest(newPage.Page);
                            Task.Run(() => this.DoRealPageGet(voc, newPage, pageOffset, index, cts));
                        }
                        else
                        {
                            this.FillPageFromAsyncProvider(newPage, pageOffset);
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

                var data = await this.ProviderAsync.GetItemsAtAsync(pageOffset, page.ItemsPerPage, false);

                if (cts.IsCancellationRequested) return;

                page.WiredDateTime = data.LoadedAt;

                int i = 0;
                foreach (var item in data.Items)
                {
                    if (cts.IsCancellationRequested)
                    {
                        this.RemovePageRequest(page.Page);
                        return;
                    }

                    this.ClearOptimizations();
                    if(page.ReplaceNeeded(i))
                    {
                        var old = page.GetAt(i);
                        if (old == null)
                        {

                        }

                        this.ClearOptimizations();
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

            this.ClearOptimizations();
            foreach (var replace in listOfReplaces)
            {
                if (cts.IsCancellationRequested)
                {
                    this.RemovePageRequest(page.Page);
                    return;
                }
                VirtualizationManager.Instance.RunOnUI(replace);
            }

            this.RemovePageRequest(page.Page);
        }

        protected bool IsPageWired(int page)
        {
            bool wired = false;

            lock (this._PageLock)
            {
                if (this._Pages.ContainsKey(page)) wired = true;
            }

            return wired;
        }

        public void OnInsert(int index, T item, object timestamp)
        {
            int page; int offset;

            if (!this._HasGotCount) this.EnsureCount();

            this.CalculateFromIndex(index, out page, out offset);

            if (this.IsPageWired(page))
            {
                var dataPage = this.SafeGetPage(page, false, null, index);
                dataPage.InsertAt(offset, item, timestamp, this.ExpiryComparer);
            }
            int adj = this.AddOrUpdateAdjustment(page, 1);

            if(page == this._BasePage && adj == this.PageSize*2)
            {
                lock (this._PageLock)
                {
                    if (this.IsPageWired(page))
                    {
                        var dataPage = this.SafeGetPage(page, false, null, index);
                        ISourcePage<T> newdataPage = null;
                        if (this.IsPageWired(page - 1))
                        {
                            newdataPage = this.SafeGetPage(page-1, false, null, index);
                        }
                        else
                        {
                            newdataPage = this._Reclaimer.MakePage(page - 1, this.PageSize);
                            this._Pages.Add(page - 1, newdataPage);
                        }

                        for (int loop = 0; loop < this.PageSize; loop++)
                        {
                            var i = dataPage.GetAt(0);

                            dataPage.RemoveAt(0, null, null);
                            newdataPage.Append(i, null, null);
                        }

                    }

                    this.AddOrUpdateAdjustment(page, -this.PageSize);

                    this._BasePage--;
                }
            }

            if (this.IsAsync)
            {
                var test = this.GetAt(index, this, false);
            }

            var edit = this.GetProviderAsEditable();
            if (edit != null)
            {
                edit.OnInsert(index, item, timestamp);
            }

            this._LocalCount++;

            this.ClearOptimizations();
        }

        void EnsureCount()
        {
            this.GetCount(false);
        }

        protected bool IsAsync
        {
            get
            {
                return this._ProviderAsync != null ? true : false;
            }
        }

        public void OnRemove(int index, T item, object timestamp)
        {
            int page; int offset;

            if (!this._HasGotCount) this.EnsureCount();

            this.CalculateFromIndex(index, out page, out offset);

            if (this.IsPageWired(page))
            {
                var dataPage = this.SafeGetPage(page, false, null, index);
                dataPage.RemoveAt(offset, timestamp, this.ExpiryComparer);
            }
            this.AddOrUpdateAdjustment(page, -1);

            if (page == this._BasePage)
            {
                int items = this.PageSize;
                if (this._Deltas.ContainsKey(page)) items += this._Deltas[page].Delta;
                if (items == 0)
                {
                    this._Deltas.Remove(page);
                    this._BasePage++;
                }
            }

            if (this.IsAsync)
            {
                var test = this.GetAt(index, this, false);
            }

            var edit = this.GetProviderAsEditable();
            if (edit != null)
            {
                edit.OnRemove(index, item, timestamp);
            }

            this._LocalCount--;

            this.ClearOptimizations();
        }

        public void OnReplace(int index, T oldItem, T newItem, object timestamp)
        {
            int page; int offset;

            this.CalculateFromIndex(index, out page, out offset);

            if (this.IsPageWired(page))
            {
                var dataPage = this.SafeGetPage(page, false, null, index);
                dataPage.ReplaceAt(offset, oldItem, newItem, timestamp, this.ExpiryComparer);
            }

            var edit = this.GetProviderAsEditable();
            if (edit != null)
            {
                edit.OnReplace(index, oldItem, newItem, timestamp);
            }
        }

        #endregion IEditableProvider<T> Implementation


        public void RunClaim(string sectionContext = "")
        {
            if (this._Reclaimer != null)
            {
                int needed = 0;

                lock (this._PageLock)
                {
                    needed = Math.Max(0, this._Pages.Count - this.MaxPages);
                    if (needed != 0)
                    {
                        var l = this._Reclaimer.ReclaimPages(this._Pages.Values, needed, sectionContext).ToList();

                        foreach (var p in l)
                        {
                            if (p.Page != this._BasePage)
                            {
                                lock (this._Pages)
                                {
                                    if (this._Pages.ContainsKey(p.Page))
                                    {
                                        this._Pages.Remove(p.Page);
                                        this._Reclaimer.OnPageReleased(p);
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
