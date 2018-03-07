namespace DataGridAsyncDemoMVVM.filtersort
{
    public class FilterDescription : IFilterOrderDescription
    {
        public FilterDescription(string propertyName, string filter)
        {
            this.PropertyName = propertyName;
            this.Filter = filter;
        }

        public string Filter { get; set; }
        public string PropertyName { get; set; }
    }
}