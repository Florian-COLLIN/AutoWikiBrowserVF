﻿/*
Copyright © 2008 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/
Copyright © 2008 Sam Reed (Reedy) http://www.reedyboy.net/

This program is free software; you can redistribute it and/or modify it under the terms of Version 2 of the GNU General Public License as published by the Free Software Foundation.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License Version 2 along with this program; if not, write to the Free Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AutoWikiBrowser.Plugins.Kingbotk.Components
{
    /// <summary>
    /// A form which displays the configuration properties of a "generic template"
    /// </summary>
    internal sealed partial class GenericTemplatePropertiesForm
    {
        public GenericTemplatePropertiesForm()
        {
            InitializeComponent();
        }

        private void OK_Button_Click(object sender, EventArgs e)
        {
            Close();
        }

        internal static void DoRegexTextBox(TextBox txt, Regex regx)
        {
            txt.Text = regx == null ? "<not set>" : regx.ToString();
        }
    }
}