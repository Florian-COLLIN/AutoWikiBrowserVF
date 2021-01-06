﻿/*
Copyright (C) 2008 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/
Copyright (C) 2008 Sam Reed (Reedy) http://www.reedyboy.net/

This program is free software; you can redistribute it and/or modify it under the terms of Version 2 of the GNU General Public License as published by the Free Software Foundation.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License Version 2 along with this program; if not, write to the Free Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using AutoWikiBrowser.Plugins.Kingbotk.Properties;
using System;
using System.Windows.Forms;
using WikiFunctions;

namespace AutoWikiBrowser.Plugins.Kingbotk.Components
{
    internal sealed partial class AboutBox
    {
        private void AboutBox_Load(object sender, EventArgs e)
        {
            TextBoxDescription.Text =
                "An AWB plugin for adding and updating WikiProject templates on Wikipedia talk pages. " +
                Environment.NewLine +
                Environment.NewLine + "AWB Version: " + Application.ProductVersion + Environment.NewLine +
                Environment.NewLine +
                "Made in England. Store in a dry place and consume within 7 days of opening. COMES WITH NO WARRANTY - " +
                "check your edits and use sensibly!";
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        internal static string Version
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        internal AboutBox()
        {
            Load += AboutBox_Load;
            // This call is required by the Windows Form Designer.
            InitializeComponent();

            // Add any initialization after the InitializeComponent() call.
            LabelVersion.Text = string.Format("Version {0}", Version);
        }

        private void linkKingboy_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkKingboy.LinkVisited = true;
            Tools.OpenENArticleInBrowser("Kingboyk", true);
        }

        private void linkReedy_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkReedy.LinkVisited = true;
            Tools.OpenENArticleInBrowser("Reedy", true);
        }

        private void LicencingButton_Click(object sender, EventArgs e)
        {
            GPLAboutBox GPL = new GPLAboutBox();
            GPL.ShowDialog(PluginManager.AWBForm.Form);
        }

        private class GPLAboutBox : WikiFunctions.Controls.AboutBox
        {
            protected override void Initialise()
            {
                Text = Constants.AWBPluginName;
                linkLabel1.Visible = false;
                lblMadeBy.Text = "Made by Stephen Kennedy with Sam Reed";
                lblVersion.Text = "Version " + Version;
                textBoxDescription.Text = AssemblyDescription(System.Reflection.Assembly.GetExecutingAssembly()) +
                                          Environment.NewLine + Environment.NewLine + Resources.GPL;
            }
        }
    }
}