using System.ComponentModel;

namespace DataGridAsyncDemoMVVM.filtersort
{
    public class MemberPathSortingDirection
    {
        public string MemberPath { get; set; }
        public ListSortDirection? SortDirection { get; set; }
    }
}