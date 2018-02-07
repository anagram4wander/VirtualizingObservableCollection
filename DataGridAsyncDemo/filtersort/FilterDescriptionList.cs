using System.Collections.Specialized;

namespace DataGridAsyncDemo.filtersort
{
    public class FilterDescriptionList : DescriptionList<FilterDescription>
    {
        public void OnCollectionReset()
        {
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}