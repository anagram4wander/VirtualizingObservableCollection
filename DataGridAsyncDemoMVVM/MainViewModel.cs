using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using AlphaChiTech.Virtualization;
using DataGridAsyncDemoMVVM.filtersort;
using GalaSoft.MvvmLight.Command;

namespace DataGridAsyncDemoMVVM
{
    internal class MainViewModel
    {
        private readonly RemoteOrDbDataSourceAsyncProxy _myRemoteOrDbDataSourceAsyncProxy;
        private VirtualizingObservableCollection<RemoteOrDbDataItem> myDataVirtualizedAsyncFilterSortObservableCollection;

        public MainViewModel()
        {
            this._myRemoteOrDbDataSourceAsyncProxy = new RemoteOrDbDataSourceAsyncProxy(new RemoteOrDbDataSourceEmulation(100));
            this.myDataVirtualizedAsyncFilterSortObservableCollection =
                new VirtualizingObservableCollection<RemoteOrDbDataItem>(
                    new PaginationManager<RemoteOrDbDataItem>(this._myRemoteOrDbDataSourceAsyncProxy,
                        pageSize: 10, maxPages: 2));
            this.MyDataVirtualizedAsyncFilterSortObservableCollectionCollectionView =
                CollectionViewSource.GetDefaultView(myDataVirtualizedAsyncFilterSortObservableCollection);

            this.FilterCommand = new RelayCommand<object>(async o => await this.Filter(o as MemberPathFilterText));
        }

        private int _filterWaitingCount = 0;
        private async Task Filter(MemberPathFilterText memberPathFilterText)
        {
            if (String.IsNullOrWhiteSpace(memberPathFilterText.FilterText))
            {
                this._myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.Remove(memberPathFilterText
                    .ColumnSortMemberPath);
            }
            else
            {
                this._myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.Add(new FilterDescription(memberPathFilterText.ColumnSortMemberPath, memberPathFilterText.FilterText));
            }
            Interlocked.Increment(ref this._filterWaitingCount);
            await Task.Delay(500);
            if (Interlocked.Decrement(ref this._filterWaitingCount) != 0) return;
            this._myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.OnCollectionReset();
            this.myDataVirtualizedAsyncFilterSortObservableCollection.Clear();
        }

        public ICollectionView MyDataVirtualizedAsyncFilterSortObservableCollectionCollectionView { get; }

        public RelayCommand<object> FilterCommand { get; }
    }
}