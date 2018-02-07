#region

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using AlphaChiTech.Virtualization;
using AlphaChiTech.Virtualization.Pageing;
using AlphaChiTech.VirtualizingCollection;
using DataGridAsyncDemo.filtersort;
using SortDescription = DataGridAsyncDemo.filtersort.SortDescription;

#endregion

namespace DataGridAsyncDemo
{
    #region

    #endregion

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region  constructors

        //private VirtualizingObservableCollection<RemoteOrDbDataItem> _myDataVirtualizedAsyncFilterSortObservableCollection;
        //private RemoteOrDbDataSourceAsyncProxy _myRemoteOrDbDataSourceAsyncProxy;

        public MainWindow()
        {
            //this routine only needs to run once, so first check to make sure the
            //VirtualizationManager isn’t already initialized
            if (!VirtualizationManager.IsInitialized)
            {
                //set the VirtualizationManager’s UIThreadExcecuteAction. In this case
                //we’re using Dispatcher.Invoke to give the VirtualizationManager access
                //to the dispatcher thread, and using a DispatcherTimer to run the background
                //operations the VirtualizationManager needs to run to reclaim pages and manage memory.
                VirtualizationManager.Instance.UiThreadExcecuteAction = a => Application.Current.Dispatcher.Invoke(a);
                new DispatcherTimer(TimeSpan.FromMilliseconds(10),
                    DispatcherPriority.Background,
                    delegate { VirtualizationManager.Instance.ProcessActions(); }, this.Dispatcher).Start();
            }

            this.InitializeComponent();

            this.TstDataGridAsyncFilterSort.ItemsSource = this.MyDataVirtualizedAsyncFilterSortObservableCollection;
        }

        #endregion

        #region properties

        public VirtualizingObservableCollection<RemoteOrDbDataItem> MyDataVirtualizedAsyncFilterSortObservableCollection
        {
            get
            {
                if (this._myDataVirtualizedAsyncFilterSortObservableCollection == null)
                {
                    this._myRemoteOrDbDataSourceAsyncProxy =
                        new RemoteOrDbDataSourceAsyncProxy(new RemoteOrDbDataSourceEmulation());
                    this._myDataVirtualizedAsyncFilterSortObservableCollection =
                        new VirtualizingObservableCollection<RemoteOrDbDataItem>(
                            new PaginationManager<RemoteOrDbDataItem>(this._myRemoteOrDbDataSourceAsyncProxy,
                                pageSize: 10, maxPages: 2));
                }

                return this._myDataVirtualizedAsyncFilterSortObservableCollection;
            }
        }

        #endregion

        #region fields

        private Timer _filterTimer;

        private VirtualizingObservableCollection<RemoteOrDbDataItem>
            _myDataVirtualizedAsyncFilterSortObservableCollection;

        private RemoteOrDbDataSourceAsyncProxy _myRemoteOrDbDataSourceAsyncProxy;

        #endregion

        #region events

        private void DbgButton_Click(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }

        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this._filterTimer == null)
            {
                this._filterTimer = new Timer(1000);
                this._filterTimer.Elapsed += this.FilterTimerElapsed;
            }

            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                DataGridColumnHeader dataGridColumnHeader = textBox.ParentOfType<DataGridColumnHeader>();
                if (dataGridColumnHeader != null)
                {
                    this._filterTimer.Stop();

                    string dbg_SortMemberPath = dataGridColumnHeader.Column.SortMemberPath;
                    // TODO update filter

                    if (String.IsNullOrWhiteSpace(textBox.Text))
                        this._myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.Remove(dbg_SortMemberPath);
                    else
                        this._myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.Add(
                            new FilterDescription(dbg_SortMemberPath, textBox.Text));

                    // Will notify filter definition update
                    this._filterTimer.Start();
                }
            }
        }

        private void FilterTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                // Notify filter definition update
                this._filterTimer.Stop();

                this._myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.OnCollectionReset();

                this.Dispatcher.BeginInvoke((Action) (()
                    =>
                {
                    // call .Clear() on the virtualizingObservableCollection to force a refresh / reset
                    this._myDataVirtualizedAsyncFilterSortObservableCollection.Clear();
                }));
            }
            catch (Exception ex)
            {
                int aa = 0;
            }
        }

        private void TstDataGridAsyncFilterSort_Sorting(object sender, DataGridSortingEventArgs e)
        {
            DataGrid grid = sender as DataGrid;

            var sortDirection = e.Column.SortDirection;
            string sortMemberPath = e.Column.SortMemberPath;

            if (sortDirection == null)
            {
                e.Column.SortDirection = ListSortDirection.Ascending;
                this._myRemoteOrDbDataSourceAsyncProxy.SortDescriptionList.Add(
                    new SortDescription(sortMemberPath, ListSortDirection.Ascending));
            }
            else if (sortDirection == ListSortDirection.Ascending)
            {
                e.Column.SortDirection = ListSortDirection.Descending;
                this._myRemoteOrDbDataSourceAsyncProxy.SortDescriptionList.Add(
                    new SortDescription(sortMemberPath, ListSortDirection.Descending));
            }
            else if (sortDirection == ListSortDirection.Descending)
            {
                e.Column.SortDirection = null;
                this._myRemoteOrDbDataSourceAsyncProxy.SortDescriptionList.Remove(sortMemberPath);
            }

            this.Dispatcher.BeginInvoke((Action) (()
                =>
            {
                // call .Clear() on the virtualizingObservableCollection to force a refresh / reset
                this._myDataVirtualizedAsyncFilterSortObservableCollection.Clear();
            }));

            e.Handled = true;
        }

        #endregion
    }
}