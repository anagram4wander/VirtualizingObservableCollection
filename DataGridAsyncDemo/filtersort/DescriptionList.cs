namespace DataGridAsyncDemo.filtersort
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Collections.Specialized;

  public class DescriptionList<T> : IEnumerable<T>, IEnumerable, INotifyCollectionChanged
    where T : IFilterOrderDescription
  {
    private readonly List<T> _filterDescriptions = new List<T>();

    IEnumerator IEnumerable.GetEnumerator()
    {
      return _filterDescriptions.GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
      return _filterDescriptions.GetEnumerator();
    }

    public event NotifyCollectionChangedEventHandler CollectionChanged;

    /// <summary>
    /// If it exist, remove existing filter that apply on same property name. The add item arg at first position into filter list.
    /// </summary>
    /// <param name="item"></param>
    public void Add( T item )
    {
      int index = _filterDescriptions.FindIndex( description => description.PropertyName.Equals( item.PropertyName, StringComparison.Ordinal ) );
      if (index >= 0)
      {
        T removed = _filterDescriptions[index];
        _filterDescriptions.RemoveAt( index );
        //OnCollectionChanged( new NotifyCollectionChangedEventArgs( NotifyCollectionChangedAction.Remove, removed, index ) );
        _filterDescriptions.Insert( 0, item );
        //OnCollectionChanged( new NotifyCollectionChangedEventArgs( NotifyCollectionChangedAction.Add, item, 0 ) );

        OnCollectionChanged( new NotifyCollectionChangedEventArgs( NotifyCollectionChangedAction.Move, removed, 0, index ) );
      }
      else
      {
        _filterDescriptions.Insert( 0, item );
        OnCollectionChanged( new NotifyCollectionChangedEventArgs( NotifyCollectionChangedAction.Add, item, 0 ) );
      }
    }

    protected void OnCollectionChanged( NotifyCollectionChangedEventArgs arg )
    {
      var evnt = CollectionChanged;

      if (evnt != null)
        evnt( this, arg );
    }

    /// <summary>
    /// If it exist, remove existing filter that apply on same property name. The add item arg at first position into filter list.
    /// </summary>
    /// <param name="item"></param>
    public void Remove( string propertyName )
    {
      int index = _filterDescriptions.FindIndex( description => description.PropertyName.Equals( propertyName, StringComparison.Ordinal ) );
      if (index >= 0)
      {
        T removed = _filterDescriptions[index];
        _filterDescriptions.RemoveAt( index );
        OnCollectionChanged( new NotifyCollectionChangedEventArgs( NotifyCollectionChangedAction.Remove, removed, index ) );
      }
    }
  }
}