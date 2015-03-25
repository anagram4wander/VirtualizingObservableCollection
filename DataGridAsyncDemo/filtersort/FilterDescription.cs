namespace DataGridAsyncDemo.filtersort
{
  public class FilterDescription : IFilterOrderDescription
  {
    public FilterDescription( string propertyName, string filter )
    {
      PropertyName = propertyName;
      Filter = filter;
    }

    public string Filter { get; set; }
    public string PropertyName { get; set; }
  }
}