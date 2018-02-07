using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DataGridAsyncDemoMVVM.filtersort;
using GalaSoft.MvvmLight.Command;

namespace DataGridAsyncDemoMVVM.converters
{
    public class TextChangedEventArgsToMemberPathAndText : IEventArgsConverter
    {
        public object Convert(object value, object parameter)
        {
            if (value is TextChangedEventArgs eventArgs)
                if (eventArgs.Source is TextBox textBox)
                {
                    var dataGridColumnHeader = textBox.ParentOfType<DataGridColumnHeader>();
                    if (dataGridColumnHeader != null)
                    {
                        var columnSortMemberPath = dataGridColumnHeader.Column.SortMemberPath;
                        var filterText = textBox.Text;
                        return new MemberPathFilterText {MemberPath = columnSortMemberPath, FilterText = filterText};
                    }
                }

            return null;
        }
    }
}