﻿/*

Copyright (C) 2007 Martin Richards
Copyright (C) 2008 Sam Reed

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections;

namespace WikiFunctions.Controls
{
    /// <summary>
    /// Inreface for creation of custom sorters
    /// </summary>
    public interface IListViewItemComparerFactory
    {
        IComparer CreateComparer(int column, SortOrder order);
    }

    /// <summary>
    /// Advanced ListView
    /// </summary>
    public class NoFlickerExtendedListView : ListView, IListViewItemComparerFactory
    {
        /// <summary>
        /// 
        /// </summary>
        public NoFlickerExtendedListView()
            : this(false, false)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sortColumnOnClick"></param>
        /// <param name="resizeColumnsOnControlResize"></param>
        public NoFlickerExtendedListView(bool sortColumnOnClick, bool resizeColumnsOnControlResize)
        {
            SortColumnsOnClick = sortColumnOnClick;
            ResizeColumnsOnControlResize = resizeColumnsOnControlResize;
            _sortColumnsOnClick = sortColumnOnClick;
            DoubleBuffered = true;
            comparerFactory = this;
        }

        private void NoFlickerExtendedListView_Resize(object sender, EventArgs e)
        {
            ResizeColumns(true);
        }

        private bool _resizeColumnsOnControlResize;

        /// <summary>
        /// 
        /// </summary>
        [DefaultValue(false)]
        public bool ResizeColumnsOnControlResize
        {
            set
            {
                if (value && !_resizeColumnsOnControlResize)
                    Resize += NoFlickerExtendedListView_Resize;
                else if (!value && _resizeColumnsOnControlResize)
                    Resize -= NoFlickerExtendedListView_Resize;

                _resizeColumnsOnControlResize = value;
            }
            get { return _resizeColumnsOnControlResize; }
        }

        private bool _sortColumnsOnClick;

        /// <summary>
        /// Enables or disables sorting upon click on column header
        /// </summary>
        [DefaultValue(false)]
        public bool SortColumnsOnClick
        {
            set
            {
                if (value && !_sortColumnsOnClick)
                    ColumnClick += ExtendedListView_ColumnClick;
                else if (!value && _sortColumnsOnClick)
                    ColumnClick -= ExtendedListView_ColumnClick;

                _sortColumnsOnClick = value;
            }
            get { return _sortColumnsOnClick; }
        }

        private int sortColumn;

        /// <remarks>From http://msdn2.microsoft.com/en-us/library/ms996467.aspx </remarks>
        private void ExtendedListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            try
            {
                BeginUpdate();
                // Determine whether the column is the same as the last column clicked.
                if (e.Column != sortColumn)
                {
                    // Set the sort column to the new column.
                    sortColumn = e.Column;
                    // Set the sort order to ascending by default.
                    Sorting = SortOrder.Ascending;
                }
                else
                {
                    // Determine what the last sort order was and change it.
                    Sorting = Sorting == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
                }

                // Call the sort method to manually sort.
                Sort();
                // Set the lstAccountsItemSorter property to a new lstAccountsItemComparer
                // object.
                ListViewItemSorter = comparerFactory.CreateComparer(e.Column, Sorting);
                EndUpdate();
            }
            catch { }
        }

        // Cache of column headings and sizes for listview
        System.Collections.Generic.Dictionary<string, int> colheadsizes = new System.Collections.Generic.Dictionary<string, int>();

        /// <summary>
        /// Automatically resize all the columns in the list view based on whether the text or the title is larger
        /// No BeginUpdate()/EndUpdate called
        /// </summary>
        public void ResizeColumns()
        {
            // cache the column sizes of the listview when sized based on column headings
            // so we calculate this once per listview not once per change to listview
            if (colheadsizes.Count == 0)
            {
                foreach (ColumnHeader head in Columns)
                {
                    head.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                    if (!head.Text.Equals("ColumnHeader"))
                        colheadsizes.Add(head.Text, head.Width);
                }
            }

            foreach (ColumnHeader head in Columns)
            {
                // Performance: Time heading only ever has date & time so don't need to resize on every item addition
                if (head.Text.Equals("Time"))
                {
                    if (Items.Count == 0)
                        head.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                    else if (Items.Count == 1)
                        head.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);

                    continue;
                }
                // Performance: Skipped By is "User" or "AWB" only so is always defined by header size not content
                else if (head.Text.Equals("Skipped By") && Items.Count <= 1)
                {
                    head.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                    continue;
                }

                int width;

                if (!colheadsizes.TryGetValue(head.Text, out width))
                {
                    head.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                    width = head.Width;
                }

                head.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                int width2 = head.Width;

                if (width2 < width)
                    head.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
        }

        private IListViewItemComparerFactory comparerFactory;

        /// <summary>
        /// Allows to override default column sorting behaviour by providing a factory for custom sorters
        /// </summary>
        [Browsable(false)]
        [Localizable(false)]
        public IListViewItemComparerFactory ComparerFactory
        {
            get { return comparerFactory; }
            set
            {
                comparerFactory = value;
                ListViewItemSorter = comparerFactory.CreateComparer(sortColumn, Sorting);
            }
        }

        public IComparer CreateComparer(int column, SortOrder order)
        {
            return new ListViewItemComparer(column, order);
        }

        /// <summary>
        /// Automatically resize all the columns in the list view based on whether the text or the title is larger
        /// </summary>
        /// <param name="useUpdateMethods">Whether to call BeginUpdate() and EndUpdate()</param>
        public void ResizeColumns(bool useUpdateMethods)
        {
            if (useUpdateMethods)
                BeginUpdate();

            ResizeColumns();

            if (useUpdateMethods)
                EndUpdate();
        }

        sealed class ListViewItemComparer : IComparer
        {
            private readonly int Col;
            private readonly SortOrder Order;

            public ListViewItemComparer()
            {
                Order = SortOrder.Ascending;
            }

            public ListViewItemComparer(int column, SortOrder order)
            {
                Col = column;
                Order = order;
            }

            public int Compare(object x, object y)
            {
                int returnVal;

                string sx = ((ListViewItem) x).SubItems[Col].Text;
                string sy = ((ListViewItem) y).SubItems[Col].Text;

                DateTime firstDate, secondDate;
                double dblX, dblY;

                // first try to parse as ints
                if (double.TryParse(sx, out dblX) && double.TryParse(sy, out dblY))
                {
                    returnVal = dblX.CompareTo(dblY);
                }
                else
                    // Determine whether the type being compared is a DateTime type
                    if (DateTime.TryParse(sx, out firstDate) && DateTime.TryParse(sy, out secondDate))
                    {
                        // Compare the two dates.
                        returnVal = DateTime.Compare(firstDate, secondDate);
                    }
                    else
                    {
                        // If neither compared object has a valid int or date format, compare
                        // as a string.
                        returnVal = String.Compare(sx, sy);
                    }

                // Determine whether the sort order is descending.
                if (Order == SortOrder.Descending)
                    // Invert the value returned by String.Compare.
                    returnVal *= -1;
                return returnVal;
            }
        }
    }
}
