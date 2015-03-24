namespace DataGridAsyncDemo
{
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
  using filtersort;
  using SortDescription = filtersort.SortDescription;

  #endregion

  /// <summary>
  /// Interaction logic for MainWindow.xaml
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
      if ( !VirtualizationManager.IsInitialized )
      {
        //set the VirtualizationManager’s UIThreadExcecuteAction. In this case
        //we’re using Dispatcher.Invoke to give the VirtualizationManager access
        //to the dispatcher thread, and using a DispatcherTimer to run the background
        //operations the VirtualizationManager needs to run to reclaim pages and manage memory.
        VirtualizationManager.Instance.UIThreadExcecuteAction = a => Dispatcher.Invoke( a );
        new DispatcherTimer( TimeSpan.FromSeconds( 1 ),
                             DispatcherPriority.Background,
                             delegate { VirtualizationManager.Instance.ProcessActions(); },
                             Dispatcher ).Start();
      }

      InitializeComponent();

      TstDataGridAsyncFilterSort.ItemsSource = MyDataVirtualizedAsyncFilterSortObservableCollection;
    }

    #endregion

    #region Asynchrony source filter sort

    private VirtualizingObservableCollection<RemoteOrDbDataItem> _myDataVirtualizedAsyncFilterSortObservableCollection = null;
    private RemoteOrDbDataSourceAsyncProxy _myRemoteOrDbDataSourceAsyncProxy = null;

    public VirtualizingObservableCollection<RemoteOrDbDataItem> MyDataVirtualizedAsyncFilterSortObservableCollection
    {
      get
      {
        if ( _myDataVirtualizedAsyncFilterSortObservableCollection == null )
        {
          _myRemoteOrDbDataSourceAsyncProxy = new RemoteOrDbDataSourceAsyncProxy( new RemoteOrDbDataSourceEmulation() );
          //class RemoteOrDbDataSourceAsyncProxy : IPagedSourceProviderAsync<RemoteOrDbDataItem>, IFilteredSortedSourceProviderAsync, IPagedSourceProvider<T>
          _myDataVirtualizedAsyncFilterSortObservableCollection =
            new VirtualizingObservableCollection<RemoteOrDbDataItem>( // BUG ici il faut un IItemSourceProviderAsync<T> asyncProvider
              // et non un IItemSourceProvider<T> provider
              new PaginationManager<RemoteOrDbDataItem>( _myRemoteOrDbDataSourceAsyncProxy ) );
        }
        return _myDataVirtualizedAsyncFilterSortObservableCollection;
      }
    }

    private void TstDataGridAsyncFilterSort_Sorting( object sender, DataGridSortingEventArgs e )
    {
      DataGrid grid = sender as DataGrid;

      var sortDirection = e.Column.SortDirection;
      string sortMemberPath = e.Column.SortMemberPath;

      if ( sortDirection == null )
      {
        e.Column.SortDirection = ListSortDirection.Ascending;
        _myRemoteOrDbDataSourceAsyncProxy.SortDescriptionList.Add( new SortDescription( sortMemberPath, ListSortDirection.Ascending ) );
      }
      else if ( sortDirection == ListSortDirection.Ascending )
      {
        e.Column.SortDirection = ListSortDirection.Descending;
        _myRemoteOrDbDataSourceAsyncProxy.SortDescriptionList.Add( new SortDescription( sortMemberPath, ListSortDirection.Descending ) );
      }
      else if ( sortDirection == ListSortDirection.Descending )
      {
        e.Column.SortDirection = null;
        _myRemoteOrDbDataSourceAsyncProxy.SortDescriptionList.Remove( sortMemberPath );
      }

      Dispatcher.BeginInvoke( (Action)( ()
                                        =>
                                        {
                                          // call .Clear() on the virtualizingObservableCollection to force a refresh / reset
                                          _myDataVirtualizedAsyncFilterSortObservableCollection.Clear();
                                        } ) );

      e.Handled = true;
    }

    private Timer _filterTimer;
    //static System.Threading.Timer _filterTimer = new System.Threading.Timer( FilterTimerElapsed );

    private void FilterTextBox_TextChanged( object sender, TextChangedEventArgs e )
    {
      if ( _filterTimer == null )
      {
        _filterTimer = new Timer( 3000 );
        _filterTimer.Elapsed += FilterTimerElapsed;
      }

      TextBox textBox = sender as TextBox;
      if ( textBox != null )
      {
        DataGridColumnHeader dataGridColumnHeader = textBox.ParentOfType<DataGridColumnHeader>();
        if ( dataGridColumnHeader != null )
        {
          _filterTimer.Stop();

          string dbg_SortMemberPath = dataGridColumnHeader.Column.SortMemberPath;
          // TODO update filter

          if ( String.IsNullOrWhiteSpace( textBox.Text ) )
            _myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.Remove( dbg_SortMemberPath );
          else
            _myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.Add( new FilterDescription( dbg_SortMemberPath, textBox.Text ) );

          // Will notify filter definition update
          _filterTimer.Start();
        }
      }
    }

    private void FilterTimerElapsed( object sender, ElapsedEventArgs elapsedEventArgs )
    {
      try
      {
        // Notify filter definition update
        _filterTimer.Stop();

        //Task<int> task = _myRemoteOrDbDataSourceAsyncProxy.GetCountAsync();
        //task.Wait();
        //int aa1 = task.Result;

        _myRemoteOrDbDataSourceAsyncProxy.FilterDescriptionList.OnCollectionReset();

        Dispatcher.BeginInvoke( (Action)( ()
                                          =>
                                          {
                                            // call .Clear() on the virtualizingObservableCollection to force a refresh / reset
                                            _myDataVirtualizedAsyncFilterSortObservableCollection.Clear();
                                          } ) );
        // BUG bug bizare. Le filter est bien appliqué, le count dans la source est bon mais le count dans la DataGrid (vertical scroll est faut
        // comme ci la view interne de la DataGrid fichait le bazard


        //TstDataGridAsyncFilterSort.ItemsSource = null;
        //TstDataGridAsyncFilterSort.ItemsSource = MyDataVirtualizedAsyncFilterSortObservableCollection;

        //task = _myRemoteOrDbDataSourceAsyncProxy.GetCountAsync();
        //task.Wait();
        //int aa2 = task.Result;
      }
      catch ( Exception ex )
      {
        int qq = 0;
      }
    }

    #endregion Asynchrony source filter sort

    private void DbgButton_Click( object sender, RoutedEventArgs e )
    {
      //PaginationManager<RemoteOrDbDataItem> aa = MyDataVirtualized.Provider as PaginationManager<RemoteOrDbDataItem>;
      Debugger.Break();
    }
  }
}
