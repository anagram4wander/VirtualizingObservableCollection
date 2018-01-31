#region

#endregion

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Dynamic;
using System.Text.RegularExpressions;
using DataGridAsyncDemoMVVM.filtersort;

namespace DataGridAsyncDemoMVVM
{
    /// <summary>
    ///     Emulate a remote data repository (list of item + sort & filter values)
    /// </summary>
    public class RemoteOrDbDataSourceEmulation : IFilteredSortedSourceProviderAsync
    {
        public RemoteOrDbDataSourceEmulation(int itemsCount)
        {
            for (var i = 0; i < itemsCount; i++)
                this._items.Add(new RemoteOrDbDataItem("Name_" + i, "Str1_" + i, "Str1_" + i, i, i));

            this.SortDescriptionList.CollectionChanged += this.SortDescriptionListOnCollectionChanged;
            this.FilterDescriptionList.CollectionChanged += this.FilterDescriptionListOnCollectionChanged;
        }

        public bool Contains(RemoteOrDbDataItem item)
        {
            return this._items.Contains(item);
        }

        #region fields

        private readonly List<RemoteOrDbDataItem> _items = new List<RemoteOrDbDataItem>();
        private readonly List<RemoteOrDbDataItem> _orderedItems = new List<RemoteOrDbDataItem>();
        private bool _isFilteredItemsValid;
        private string _orderByLinqExpression = "";
        private string _whereLinqExpression = "";

        #endregion

        #region properties

        public IList<RemoteOrDbDataItem> FilteredOrderedItems
        {
            get
            {
                if (this._isFilteredItemsValid) return this._orderedItems;

                lock (this)
                {
                    this._orderedItems.Clear();

                    try
                    {
                        if (string.IsNullOrWhiteSpace(this.WhereLinqExpression) &&
                            string.IsNullOrWhiteSpace(this.OrderByLinqExpression))
                            this._orderedItems.AddRange(this._items);
                        else if (!string.IsNullOrWhiteSpace(this.WhereLinqExpression) &&
                                 string.IsNullOrWhiteSpace(this.OrderByLinqExpression))
                            this._orderedItems.AddRange(this._items.Where(this.WhereLinqExpression));
                        else if (string.IsNullOrWhiteSpace(this.WhereLinqExpression) &&
                                 !string.IsNullOrWhiteSpace(this.OrderByLinqExpression))
                            this._orderedItems.AddRange(this._items.OrderBy(this.OrderByLinqExpression));
                        else if (!string.IsNullOrWhiteSpace(this.WhereLinqExpression) &&
                                 !string.IsNullOrWhiteSpace(this.OrderByLinqExpression))
                            this._orderedItems.AddRange(this._items.Where(this.WhereLinqExpression)
                                .OrderBy(this.OrderByLinqExpression));
                    }
                    catch
                    {
                    }

                    this._isFilteredItemsValid = true;
                }

                return this._orderedItems;
            }
        }

        public string OrderByLinqExpression
        {
            get => this._orderByLinqExpression;
            set
            {
                if (!string.Equals(this._orderByLinqExpression, value))
                {
                    this._orderByLinqExpression = value;
                    this._isFilteredItemsValid = false;
                }
            }
        }

        public string WhereLinqExpression
        {
            get => this._whereLinqExpression;
            set
            {
                if (!string.Equals(this._whereLinqExpression, value))
                {
                    this._whereLinqExpression = value;
                    this._isFilteredItemsValid = false;
                }
            }
        }

        #endregion

        #region public members

        public void OrderBy(string orderByExpression)
        {
            if (!string.Equals(orderByExpression, this.OrderByLinqExpression))
                this.OrderByLinqExpression = orderByExpression;
        }

        public void Where(string whereExpression)
        {
            if (!string.Equals(whereExpression, this.WhereLinqExpression))
                this.WhereLinqExpression = whereExpression;
        }

        #endregion

        #region filter & sort Descrioption list

        public SortDescriptionList SortDescriptionList { get; } = new SortDescriptionList();

        public FilterDescriptionList FilterDescriptionList { get; } = new FilterDescriptionList();

        private void SortDescriptionListOnCollectionChanged(object sender,
            NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            var sort = "";

            var sortFound = false;
            foreach (var sortDescription in this.SortDescriptionList)
            {
                if (sortFound)
                    sort += ", ";

                sortFound = true;

                sort += sortDescription.PropertyName;
                sort += sortDescription.Direction == ListSortDirection.Ascending ? " ASC" : " DESC";
            }

            //if ((!sortFound) && (!string.IsNullOrWhiteSpace( primaryKey )))
            //  sort += primaryKey + " ASC";

            this.OrderByLinqExpression = sort;
        }

        private void FilterDescriptionListOnCollectionChanged(object sender,
            NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Reset)
            {
                var filter = "";

                var filterFound = false;
                foreach (var filterDescription in this.FilterDescriptionList)
                {
                    var subFilter = GetLinqQueryString(filterDescription);
                    if (!string.IsNullOrWhiteSpace(subFilter))
                    {
                        if (filterFound)
                            filter += " and ";
                        filterFound = true;
                        filter += " " + subFilter + " ";
                    }
                }

                this.WhereLinqExpression = filter;
            }
        }

        #region query builder

        private static readonly Regex _regexSplit = new Regex(
            @"(and)|(or)|(==)|(<>)|(!=)|(<=)|(>=)|(&&)|(\|\|)|(=)|(>)|(<)|(\*[\-_a-zA-Z0-9]+)|([\-_a-zA-Z0-9]+\*)|([\-_a-zA-Z0-9]+)",
            RegexOptions.IgnoreCase);

        private static readonly Regex _regexOp =
            new Regex(@"(and)|(or)|(==)|(<>)|(!=)|(<=)|(>=)|(&&)|(\|\|)|(=)|(>)|(<)", RegexOptions.IgnoreCase);

        private static readonly Regex _regexComparOp =
            new Regex(@"(==)|(<>)|(!=)|(<=)|(>=)|(=)|(>)|(<)", RegexOptions.None);

        private static string GetLinqQueryString(FilterDescription filterDescription)
        {
            var ret = "";

            if (!string.IsNullOrWhiteSpace(filterDescription.Filter))
                try
                {
                    // xceed syntax : empty (contains), AND (uppercase), OR (uppercase), <>, * (end with), =, >, >=, <, <=, * (start with)
                    //    see http://doc.xceedsoft.com/products/XceedWpfDataGrid/Filter_Row.html 
                    // linq.dynamic syntax : =, ==, <>, !=, <, >, <=, >=, &&, and, ||, or, x.m(…) (where x is the attrib and m the function (ex: Contains, StartsWith, EndsWith ...)
                    //    see D:\DevC#\VirtualisingCollectionTest1\DynamicQuery\Dynamic Expressions.html 
                    // ex : RemoteOrDbDataSourceEmulation.Instance.Items.Where( "Name.Contains(\"e_1\") or Name.Contains(\"e_2\")" );

                    var exp = filterDescription.Filter;

                    // arrange expression

                    var previousTermIsOperator = false;
                    foreach (Match match in _regexSplit.Matches(exp))
                        if (match.Success)
                            if (_regexOp.IsMatch(match.Value))
                            {
                                if (_regexComparOp.IsMatch(match.Value))
                                {
                                    // simple operator >, <, ==, != ...
                                    ret += " " + filterDescription.PropertyName + " " + match.Value;
                                    previousTermIsOperator = true;
                                }
                                else
                                {
                                    // and, or ...
                                    ret += " " + match.Value;
                                    previousTermIsOperator = false;
                                }
                            }
                            else
                            {
                                // Value
                                if (previousTermIsOperator)
                                {
                                    ret += " " + match.Value;
                                    previousTermIsOperator = false;
                                }
                                else
                                {
                                    if (match.Value.StartsWith("*"))
                                        ret += " " + filterDescription.PropertyName + ".EndsWith( \"" +
                                               match.Value.Substring(1) + "\" )";
                                    else if (match.Value.EndsWith("*"))
                                        ret += " " + filterDescription.PropertyName + ".StartsWith( \"" +
                                               match.Value.Substring(0, match.Value.Length - 1) + "\" )";
                                    else
                                        ret += " " + filterDescription.PropertyName + ".Contains( \"" + match.Value +
                                               "\" )";
                                    previousTermIsOperator = false;
                                }
                            }
                }
                catch (Exception)
                {
                }

            return ret;
        }

        #endregion query builder

        #endregion filter & sort Descrioption list
    }
}