using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using DataGridAsyncDemoMVVM.filtersort;
using GalaSoft.MvvmLight.Command;

namespace DataGridAsyncDemoMVVM.converters
{
    public class TextChangedEventArgsToDatagridHeaderAndText : IEventArgsConverter
    {
        public object Convert(object value, object parameter)
        {
            if (value is TextChangedEventArgs eventArgs)
            {
                if (eventArgs.Source is TextBox textBox)
                {

                    DataGridColumnHeader dataGridColumnHeader = textBox.ParentOfType<DataGridColumnHeader>();
                    if (dataGridColumnHeader != null)
                    {
                        string columnSortMemberPath = dataGridColumnHeader.Column.SortMemberPath;
                        var filterText = textBox.Text;
                        return new MemberPathFilterText{ ColumnSortMemberPath =columnSortMemberPath, FilterText= filterText };
                    }
                }
            }
            return null;
        }
    }
}