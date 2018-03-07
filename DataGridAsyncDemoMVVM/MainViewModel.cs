using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using AlphaChiTech.Virtualization;
using AlphaChiTech.Virtualization.Pageing;
using DataGridAsyncDemoMVVM.filtersort;
using GalaSoft.MvvmLight.Command;
using SortDescription = DataGridAsyncDemoMVVM.filtersort.SortDescription;

namespace DataGridAsyncDemoMVVM
{
    internal class MainViewModel
    {
        private readonly VirtualizingObservableCollection<RemoteOrDbDataItem>
            _myDataVirtualizedAsyncFilterSortObservableCollection;

        private readonly RemoteOrDbDataSourceAsyncProxy _myRemoteOrDbDataSourceAsyncProxy;

        private int _filterWaitingCount;

        public MainViewModel()
        {
            this._myRemoteOrDbDataSourceAsyncProxy =
                new RemoteOrDbDataSourceAsyncProxy(new RemoteOrDbDataSourceEmulation(100));
            this._myDataVirtualizedAsyncFilterSortObservableCollection =
                new VirtualizingObservableCollection<RemoteOrDbDataItem>(
                    new PaginationManager<RemoteOrDbDataItem>(this._myRemoteOrDbDataSourceAsyncProxy,
                        pageSize: 10, maxPages: 2));
            this.MyDataVirtualizedAsyncFilterSortObservableCollectionCollectionView =
                CollectionViewSource.GetDefaultView(this._myDataVirtualizedAsyncFilterSortObservableCollection);

            this.FilterCommand = new RelayCommand<MemberPathFilterText>(async o => await this.Filter(o));
            this.SortCommand = new RelayCommand<MemberPathSortingDirection>(async o => await this.Sort(o));
        }

        public RelayCommand<MemberPathFilterText> FilterCommand { get; }

        public ICollectionView MyDataVirtualizedAsyncFilterSortObservableCollectionCollectionView { get; }

        public RelayCommand<MemberPathSortingDirection> SortCommand { get; }

        private async Task Filter(MemberPathFilterText memberPathFilterText)
        {
            if (string.IsNullOrWhiteSpace(memberPathFilterText.FilterText))
                this._myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.Remove(memberPathFilterText
                    .MemberPath);
            else
                this._myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.Add(
                    new FilterDescription(memberPathFilterText.MemberPath, memberPathFilterText.FilterText));
            Interlocked.Increment(ref this._filterWaitingCount);
            await Task.Delay(500);
            if (Interlocked.Decrement(ref this._filterWaitingCount) != 0) return;
            this._myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.OnCollectionReset();
            this._myDataVirtualizedAsyncFilterSortObservableCollection.Clear();
        }

        private async Task Sort(MemberPathSortingDirection memberPathSortingDirection)
        {
            while (this._filterWaitingCount != 0)
                await Task.Delay(500);
            var sortDirection = memberPathSortingDirection.SortDirection;
            var sortMemberPath = memberPathSortingDirection.MemberPath;
            switch (sortDirection)
            {
                case null:
                    this._myRemoteOrDbDataSourceAsyncProxy.SortDescriptionList.Remove(sortMemberPath);
                    break;
                case ListSortDirection.Ascending:
                    this._myRemoteOrDbDataSourceAsyncProxy.SortDescriptionList.Add(
                        new SortDescription(sortMemberPath, ListSortDirection.Ascending));
                    break;
                case ListSortDirection.Descending:
                    this._myRemoteOrDbDataSourceAsyncProxy.SortDescriptionList.Add(
                        new SortDescription(sortMemberPath, ListSortDirection.Descending));
                    break;
            }

            this._myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.OnCollectionReset();
            this._myDataVirtualizedAsyncFilterSortObservableCollection.Clear();
        }
    }
}