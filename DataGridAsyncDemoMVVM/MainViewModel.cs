using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlphaChiTech.Virtualization;

namespace DataGridAsyncDemoMVVM
{
    class MainViewModel
    {
        private VirtualizingObservableCollection<RemoteOrDbDataItem> _myDataVirtualizedAsyncFilterSortObservableCollection = null;
        private RemoteOrDbDataSourceAsyncProxy _myRemoteOrDbDataSourceAsyncProxy = null;

        public VirtualizingObservableCollection<RemoteOrDbDataItem> MyDataVirtualizedAsyncFilterSortObservableCollection
        {
            get
            {
                if (this._myDataVirtualizedAsyncFilterSortObservableCollection == null)
                {
                    this._myRemoteOrDbDataSourceAsyncProxy = new RemoteOrDbDataSourceAsyncProxy(new RemoteOrDbDataSourceEmulation());
                    this._myDataVirtualizedAsyncFilterSortObservableCollection =
                        new VirtualizingObservableCollection<RemoteOrDbDataItem>(
                            new PaginationManager<RemoteOrDbDataItem>(this._myRemoteOrDbDataSourceAsyncProxy, pageSize: 10, maxPages: 2));
                }
                return this._myDataVirtualizedAsyncFilterSortObservableCollection;
            }
        }
    }
}
