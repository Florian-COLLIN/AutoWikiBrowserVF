﻿/*
Copyright (C) 2007 Martin Richards
(C) 2008 Sam Reed

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
using System.ComponentModel;
using System.Windows.Forms;

namespace WikiFunctions.Lists.Providers
{
    /// <summary>
    /// Gets the list of pages on the Named Special Pages
    /// </summary>
    public partial class SpecialPageListProvider : Form, IListProvider
    {
        private static readonly BindingList<IListProvider> ListItems = new BindingList<IListProvider>();

        public SpecialPageListProvider()
        {
            InitializeComponent();

            if (ListItems.Count == 0)
            {
                ListItems.Add(new PrefixIndexSpecialPageProvider());
                ListItems.Add(new AllPagesSpecialPageProvider());
                ListItems.Add(new AllCategoriesSpecialPageProvider());
                ListItems.Add(new AllFilesSpecialPageProvider());
                ListItems.Add(new AllRedirectsSpecialPageProvider());
                ListItems.Add(new RecentChangesSpecialPageProvider());
                ListItems.Add(new LinkSearchSpecialPageProvider());
                ListItems.Add(new RandomRedirectsSpecialPageProvider());
                ListItems.Add(new PagesWithoutLanguageLinksSpecialPageProvider());
                ListItems.Add(new ProtectedPagesSpecialPageProvider());
                ListItems.Add(new GalleryNewFilesSpecialPageProvider());
                ListItems.Add(new DisambiguationPagesSpecialPageProvider());
                ListItems.Add(new AllUsersSpecialPageProvider());
            }

            cmboSourceSelect.DataSource = ListItems;
            cmboSourceSelect.DisplayMember = "DisplayText";
            cmboSourceSelect.ValueMember = "DisplayText";
        }

        public SpecialPageListProvider(params IListProvider[] providers)
            : this()
        {
            if (!Globals.UsingMono)
            {
                foreach (IListProvider prov in providers)
                {
                    if (prov is ISpecialPageProvider)
                        ListItems.Add(prov);
                }
            }
        }

        // ReSharper disable once RedundantAssignment
        public List<Article> MakeList(params string[] searchCriteria)
        {
            if (Visible)
                return null;

            txtPages.Text = "";

            List<Article> list = new List<Article>();

            if (ShowDialog() == DialogResult.OK)
            {
                searchCriteria = txtPages.Text.Split('|');

                ISpecialPageProvider item = (ISpecialPageProvider)cmboSourceSelect.SelectedItem;

                if (!string.IsNullOrEmpty(txtPages.Text))
                    list = item.MakeList(Namespace.Determine(cboNamespace.Text), searchCriteria);
                else if (item.PagesNeeded)
                    MessageBox.Show("Pages needed!");
                else
                    list = item.MakeList(Namespace.Determine(cboNamespace.Text), "");
            }

            Hide();
            
            return list;
        }

        public string DisplayText
        { get { return "Special page"; } }

        public string UserInputTextBoxText
        { get { return ""; } }

        public bool UserInputTextBoxEnabled
        { get { return false; } }

        public void Selected() { }

        public bool RunOnSeparateThread
        { get { return true; } }

        public virtual bool StripUrl
        { get { return false; } }

        private void SpecialPageListProvider_Load(object sender, EventArgs e)
        {
            int currentSelected = cboNamespace.SelectedIndex;
            cboNamespace.Items.Clear();
            cboNamespace.Items.Add("Main:");
            foreach (string name in Variables.Namespaces.Values)
            {
                cboNamespace.Items.Add(name);
            }
            cboNamespace.SelectedIndex = currentSelected;
        }

        private void cmboSourceSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DesignMode) return;

            ISpecialPageProvider prov = (ISpecialPageProvider)cmboSourceSelect.SelectedItem;

            txtPages.Enabled = prov.UserInputTextBoxEnabled;
            cboNamespace.Enabled = prov.NamespacesEnabled;
        }
    }
}
