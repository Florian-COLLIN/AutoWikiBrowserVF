﻿﻿/*
ListComparer
Copyright (C) 2007 Martin Richards

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
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;

namespace WikiFunctions.Controls.Lists
{
    /// <summary>
    /// Provides a form for comparing 2 lists, to find duplicates and/or removing one list from another.
    /// </summary>
    public partial class ListComparer : Form
    {
        private readonly ListMaker _mainFormListMaker;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lmMain"></param>
        public ListComparer(ListMaker lmMain)
        {
            InitializeComponent();

            if (lmMain != null)
                _mainFormListMaker = lmMain;

            listMaker1.MakeListEnabled = true;
            listMaker2.MakeListEnabled = true;
            listMaker1.NoOfArticlesChanged +=UpdateButtons;
            listMaker2.NoOfArticlesChanged +=UpdateButtons;

            // ensure button enablement of Remove, Filter correct in list maker when opened with no articles in
            listMaker1.UpdateNumberOfArticles();
            listMaker2.UpdateNumberOfArticles();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lmMain"></param>
        /// <param name="list"></param>
        public ListComparer(ListMaker lmMain, List<Article> list)
            : this(lmMain)
        {
            listMaker1.Add(list);
        }

        private void UpdateButtons(object sender, EventArgs e)
        {
            btnGo.Enabled = (listMaker1.Count > 0 && listMaker2.Count > 0);
        }

        /// <summary>
        /// Compares the lists of articles in the 2 ListMakers
        /// </summary>
        private void CompareLists()
        {
            CompareLists(listMaker1.GetArticleList(), listMaker2.GetArticleList(), lbNo1, lbNo2, lbBoth);

            UpdateCounts();
        }

        /// <summary>
        /// Compares the lists of articles in the 2 provided Lists
        /// Best to provide an already sorted list. List 1 should be the smallest list
        /// </summary>
        /// <param name="list1">First List (preferably the smallest)</param>
        /// <param name="list2">Second List</param>
        /// <param name="lb1">List Box where unique items from list1 should go</param>
        /// <param name="lb2">List Box where unique items from list2 should go</param>
        /// <param name="lb3">List Box where the duplicates should go</param>
        public static void CompareLists(IList<Article> list1, List<Article> list2, ListBox lb1, ListBox lb2, ListBox lb3)
        {

            lb1.BeginUpdate();
            lb2.BeginUpdate();
            lb3.BeginUpdate();

            lb1.Items.AddRange(list1.Except(list2).ToArray());
            lb2.Items.AddRange(list2.Except(list1).ToArray());
            lb3.Items.AddRange(list1.Intersect(list2).ToArray());

            lb1.EndUpdate();
            lb2.EndUpdate();
            lb3.EndUpdate();
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            Clear();
            CompareLists();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            Clear();
            UpdateCounts();
        }

        private void Clear()
        {
            lbBoth.Items.Clear();
            lbNo1.Items.Clear();
            lbNo2.Items.Clear();
        }

        private void UpdateCounts()
        {
            lblNo1.Text = lbNo1.Items.Count + " pages";
            lblNo2.Text = lbNo2.Items.Count + " pages";
            lblNoBoth.Text = lbBoth.Items.Count + " pages";

            btnClear.Enabled = (lbNo1.Items.Count > 0 || lbNo2.Items.Count > 0 || lbBoth.Items.Count > 0);
            btnSaveOnly1.Enabled = btnMoveOnly1.Enabled = lbNo1.Items.Count > 0;
            btnSaveOnly2.Enabled = btnMoveOnly2.Enabled = lbNo2.Items.Count > 0;
            btnSaveBoth.Enabled = btnMoveCommon.Enabled = lbBoth.Items.Count > 0;
        }

        private void btnSaveOnly1_Click(object sender, EventArgs e)
        {
            SaveList(lbNo1);
        }

        private void btnSaveOnly2_Click(object sender, EventArgs e)
        {
            SaveList(lbNo2);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveList(lbBoth);
        }

        private static void SaveList(ListBoxArticle lb)
        {
            lb.SaveList();
        }

        private void transferDuplicatesToList1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddListToListMaker(listMaker1, MenuItemOwner(sender));
        }

        private void transferToListMaker2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddListToListMaker(listMaker2, MenuItemOwner(sender));
        }

        private void openInBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Article a in MenuItemOwner(sender).SelectedItems)
                Tools.OpenArticleInBrowser(a.Name);
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
			ListBoxArticle selectedListBox = this.ActiveControl as ListBoxArticle;

			if(selectedListBox != null)
				Tools.Copy(selectedListBox);
        }

        private static ListBoxArticle MenuItemOwner(object sender)
        {
            ToolStripMenuItem t = (sender as ToolStripMenuItem);
            Control c = (t != null)
                            ? ((ContextMenuStrip)t.Owner).SourceControl
                            : ((ContextMenuStrip)sender).SourceControl;

            return (ListBoxArticle)c;
        }

        private void btnMoveOnly1_Click(object sender, EventArgs e)
        {
            AddListToListMaker(_mainFormListMaker, lbNo1);
        }

        private void btnMoveCommon_Click(object sender, EventArgs e)
        {
            AddListToListMaker(_mainFormListMaker, lbBoth);
        }

        private void btnMoveOnly2_Click(object sender, EventArgs e)
        {
            AddListToListMaker(_mainFormListMaker, lbNo2);
        }

        private void removeSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
			ListBoxArticle selectedListBox = this.ActiveControl as ListBoxArticle;

			if(selectedListBox != null)
				selectedListBox.RemoveSelected(true);

			UpdateCounts();
        }

        private void AddListToListMaker(ListMaker lm, IEnumerable<Article> lb)
        {
            List<Article> articles = new List<Article>();
            articles.AddRange(lb);
            lm.Add(articles);
        }

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
			ListBoxArticle selectedListBox = this.ActiveControl as ListBoxArticle;

			if(selectedListBox != null)
            {
        	    selectedListBox.BeginUpdate();
        	
        	    SendKeys.SendWait("{HOME}");
        	    SendKeys.SendWait("+{END}");
        	
        	    selectedListBox.EndUpdate();
            }
        }

		private void mnuList_Opening(object sender, EventArgs e)
        {
            ListBoxArticle selectedListBox = this.ActiveControl as ListBoxArticle;

			if(selectedListBox != null)
            {
				transferToListMaker1ToolStripMenuItem.Enabled = transferToListMaker2ToolStripMenuItem.Enabled = openInBrowserToolStripMenuItem.Enabled 
                = copyToolStripMenuItem.Enabled =  removeSelectedToolStripMenuItem.Enabled =  selectAllToolStripMenuItem.Enabled = selectedListBox.Items.Count > 0;
            }
        }
    }
}
