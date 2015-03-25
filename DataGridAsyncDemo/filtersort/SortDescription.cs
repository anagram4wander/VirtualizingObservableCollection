namespace DataGridAsyncDemo.filtersort
{
  using System.ComponentModel;

  public class SortDescription : IFilterOrderDescription
  {
    public SortDescription( string propertyName, ListSortDirection? direction )
    {
      Direction = direction;
      PropertyName = propertyName;
    }

    public ListSortDirection? Direction { get; set; }
    public string PropertyName { get; set; }
  }
}