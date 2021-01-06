﻿/*
Autowikibrowser

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

namespace AutoWikiBrowser
{
    internal sealed partial class SummaryEditor : Form
    {
        public SummaryEditor()
        {
            InitializeComponent();
        }

        private void btnSort_Click(object sender, EventArgs e)
        {
            List<string> list =
                new List<string>(Summaries.Text.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries));
            list.Sort();

            Summaries.Clear();

            foreach (string s in list)
                Summaries.Text += s + "\r\n";
        }
    }
}