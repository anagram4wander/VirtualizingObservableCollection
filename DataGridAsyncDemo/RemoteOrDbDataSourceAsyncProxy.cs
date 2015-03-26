namespace DataGridAsyncDemo
{
  #region

  using System;
  using System.Linq;
  using System.Threading.Tasks;
  using AlphaChiTech.Virtualization;
  using filtersort;

  #endregion

  /// <summary>
  /// Remote/disk async data proxy.
  /// Also add a delay on calls to simulate network/disk delay.
  /// </summary>
  public class RemoteOrDbDataSourceAsyncProxy
    : IPagedSourceProviderAsync<RemoteOrDbDataItem>, IFilteredSortedSourceProviderAsync
  {
    #region fields

    private readonly RemoteOrDbDataSourceEmulation _remoteDatas;

    #endregion

    public RemoteOrDbDataSourceAsyncProxy( RemoteOrDbDataSourceEmulation remoteDatas )
    {
      _remoteDatas = remoteDatas;
    }

    #region properties



    public FilterDescriptionList FilterDescriptionList
    {
      get { return _remoteDatas.FilterDescriptionList; }
    }

    #endregion

    #region IFilteredSortedSourceProviderAsync Members

    public SortDescriptionList SortDescriptionList
    {
      get { return _remoteDatas.SortDescriptionList; }
    }

    #endregion

    #region IPagedSourceProvider<RemoteOrDbDataItem> Members (synchronous not available members)

    int IPagedSourceProvider<RemoteOrDbDataItem>.IndexOf( RemoteOrDbDataItem item )
    {
      throw new NotImplementedException();
    }
    public PagedSourceItemsPacket<RemoteOrDbDataItem> GetItemsAt( int pageoffset, int count, bool usePlaceholder )
    {
      throw new NotImplementedException();
    }
    public int Count
    {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region public members



    public Task<int> GetCountAsync()
    {
      return Task.Run( () =>
                       {
                         Task.Delay( 1000 ).Wait(); // Just to slow it down !
                         return _remoteDatas.FilteredOrderedItems.Count;
                       } );
    }

    public Task<PagedSourceItemsPacket<RemoteOrDbDataItem>> GetItemsAtAsync( int pageoffset, int count, bool usePlaceholder )
    {
      return Task.Run( () =>
                       {
                         Task.Delay( 1000 ).Wait(); // Just to slow it down !
                         return new PagedSourceItemsPacket<RemoteOrDbDataItem>
                                {
                                  LoadedAt = DateTime.Now,
                                  Items = ( from items in _remoteDatas.FilteredOrderedItems select items ).Skip( pageoffset ).Take( count )
                                };
                       } );
    }

    public RemoteOrDbDataItem GetPlaceHolder( int index, int page, int offset )
    {
      return new RemoteOrDbDataItem {Name = "Waiting [" + page + "/" + offset + "]"};
    }

    /// <summary>
    /// This returns the index of a specific item. This method is optional – you can just return -1 if you 
    /// don’t need to use IndexOf. It’s not strictly required if don’t need to be able to seeking to a 
    /// specific item, but if you are selecting items implementing this method is recommended.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public Task<int> IndexOfAsync( RemoteOrDbDataItem item )
    {
      return Task.Run( () => { return _remoteDatas.FilteredOrderedItems.IndexOf( item ); } );
    }

    /// <summary>
    /// This is a callback that runs when a Reset is called on a provider. Implementing this is also optional. 
    /// If you don’t need to do anything in particular when resets occur, you can leave this method body empty.
    /// </summary>
    /// <param name="count"></param>
    public void OnReset( int count )
    {
      // Do nothing for now
    }

    #endregion
  }
}
