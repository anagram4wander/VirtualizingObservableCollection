using System.ComponentModel;
using System.Windows.Controls;
using DataGridAsyncDemoMVVM.filtersort;
using GalaSoft.MvvmLight.Command;

namespace DataGridAsyncDemoMVVM.converters
{
    public class DataGridSortingEventArgsToMemberPathSortDirection : IEventArgsConverter
    {
        public object Convert(object value, object parameter)
        {
            if (!(value is DataGridSortingEventArgs sortingEventArgs)) return null;

            var sortMemberPath = sortingEventArgs.Column.SortMemberPath;
            var sortDirection = sortingEventArgs.Column.SortDirection;

            switch (sortDirection)
            {
                case null:
                    sortDirection = ListSortDirection.Ascending;
                    break;
                case ListSortDirection.Ascending:
                    sortDirection = ListSortDirection.Descending;
                    break;
                case ListSortDirection.Descending:
                    sortDirection = null;
                    break;
            }

            sortingEventArgs.Column.SortDirection = sortDirection;
            sortingEventArgs.Handled = true;

            return new MemberPathSortingDirection {MemberPath = sortMemberPath, SortDirection = sortDirection};
        }
    }
}