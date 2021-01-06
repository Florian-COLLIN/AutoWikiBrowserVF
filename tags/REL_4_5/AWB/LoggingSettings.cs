/*
(C) 2007 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

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

// From the Kingbotk plugin. Converted from VB to C#

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using WikiFunctions.Logging.Uploader;
using WikiFunctions;
using WikiFunctions.AWBSettings;

namespace AutoWikiBrowser
{
    /// <summary>
    /// Logging-configuration usercontrol
    /// </summary>
    internal sealed partial class LoggingSettings : UserControl
    {
        private bool mStartingUp;
        internal UsernamePassword2 LoginDetails = new UsernamePassword2();
        private bool mInitialised;
        internal Props Settings = new Props();

        public LoggingSettings()
        {
            mStartingUp = true;
            InitializeComponent();
            mStartingUp = false;
        }

        internal string LogFolder
        {
            set { Settings.LogFolder = FolderTextBox.Text = value; }
        }
        internal bool Initialised
        {
            get { return mInitialised; }
            set { mInitialised = value; }
        }
        internal bool IgnoreEvents
        {
            set { mStartingUp = value; }
        }
        internal WikiFunctions.Controls.Colour LedColour
        {
            get { return Led1.Colour; }
            set { Led1.Colour = value; }
        }

        public void TurnOffLogging()
        {
            Settings.LogXHTML = false;
            Settings.LogWiki = false;
            IgnoreEvents = true;
            WikiLogCheckBox.Checked = false;
            XHTMLLogCheckBox.Checked = false;
            IgnoreEvents = false;
        }

        /// <summary>
        /// Returns whether the Upload checkbox is checked
        /// </summary>
        public bool Upload
        {
            get { return UploadCheckBox.Checked; }
        }

        #region Settings
        [Browsable(false)]
        [Localizable(false)]
        public LoggingPrefs SerialisableSettings
        {
            get
            {
                LoggingPrefs prefs = new LoggingPrefs();
                prefs.LogFolder = FolderTextBox.Text;
                prefs.LogVerbose = VerboseCheckBox.Checked;
                prefs.LogWiki = WikiLogCheckBox.Checked;
                prefs.LogXHTML = XHTMLLogCheckBox.Checked;
                prefs.LogCategoryName = LoggingCategoryTextBox.Text;
                prefs.UploadJobName = UploadJobNameTextBox.Text;
                prefs.UploadLocation = UploadLocationTextBox.Text;
                prefs.UploadMaxLines = int.Parse(UploadMaxLinesControl.Value.ToString());
                prefs.UploadOpenInBrowser = UploadOpenInBrowserCheckBox.Checked;
                prefs.UploadToWikiProjects = UploadWikiProjectCheckBox.Checked;
                prefs.UploadAddToWatchlist = UploadWatchlistCheckBox.Checked;
                prefs.UploadYN = UploadCheckBox.Checked;
                prefs.DebugUploading = DebugUploadingCheckBox.Checked;
                return prefs;
            }
            set
            {
                if (DesignMode) return;
                if (value == null) return;
                LoggingPrefs prefs = value;

                try
                {
                    LoggingCategoryTextBox.Text = prefs.LogCategoryName;
                    FolderTextBox.Text = prefs.LogFolder;
                    VerboseCheckBox.Checked = prefs.LogVerbose;
                    WikiLogCheckBox.Checked = prefs.LogWiki;
                    XHTMLLogCheckBox.Checked = prefs.LogXHTML;
                    UploadJobNameTextBox.Text = prefs.UploadJobName;
                    UploadLocationTextBox.Text = prefs.UploadLocation;
                    UploadMaxLinesControl.Value = prefs.UploadMaxLines;
                    UploadOpenInBrowserCheckBox.Checked = prefs.UploadOpenInBrowser;
                    UploadWikiProjectCheckBox.Checked = prefs.UploadToWikiProjects;
                    UploadWatchlistCheckBox.Checked = prefs.UploadAddToWatchlist;
                    UploadCheckBox.Checked = prefs.UploadYN;
                    DebugUploadingCheckBox.Checked = prefs.DebugUploading;
                }
                catch { }
            }
        }
        internal void Reset()
        {
            Props newProps = new Props();

            if (!ApplyButton.Enabled && !(Settings.Equals(newProps)))
                WEHaveUnappliedChanges();

            // Settings = NewProps ' No, that doesn't happen until the apply button is clicked
            ApplySettingsToControls(newProps);
        }
        #endregion

        #region Event handlers - supporting routines
        private void ApplySettingsToControls(Props settingsObject)
        {
            FolderTextBox.Text = settingsObject.LogFolder;
            VerboseCheckBox.Checked = settingsObject.LogVerbose;
            WikiLogCheckBox.Checked = settingsObject.LogWiki;
            XHTMLLogCheckBox.Checked = settingsObject.LogXHTML;
            UploadCheckBox.Checked = settingsObject.UploadYN;
            UploadWatchlistCheckBox.Checked = settingsObject.UploadAddToWatchlist;
            UploadJobNameTextBox.Text = settingsObject.UploadJobName;
            UploadLocationTextBox.Text = settingsObject.UploadLocation;
            UploadMaxLinesControl.Value = Convert.ToDecimal(settingsObject.UploadMaxLines);
            UploadOpenInBrowserCheckBox.Checked = settingsObject.UploadOpenInBrowser;
            UploadWikiProjectCheckBox.Checked = settingsObject.UploadToWikiProjects;
            DebugUploadingCheckBox.Checked = settingsObject.DebugUploading;
        }
        private void GetSettingsFromControls()
        {
            DisableApplyButton();
            if (!string.IsNullOrEmpty(FolderTextBox.Text.Trim()))
            {
                if (System.IO.Directory.Exists(FolderTextBox.Text))
                {
                    LogFolder = FolderTextBox.Text;
                }
                else
                {
                    Program.AWB.NotifyBalloon("Folder doesn't exist, using previous setting (" + 
                        Settings.LogFolder + ")", ToolTipIcon.Warning);
                    FolderTextBox.Text = Settings.LogFolder;
                }
            }

            Settings.LogVerbose = VerboseCheckBox.Checked;
            Settings.LogWiki = WikiLogCheckBox.Checked;
            Settings.LogXHTML = XHTMLLogCheckBox.Checked;
            Settings.LogSQL = SQLLogCheckBox.Checked;
            Settings.UploadYN = UploadCheckBox.Checked;
            Settings.UploadAddToWatchlist = UploadWatchlistCheckBox.Checked;
            Settings.UploadJobName = UploadJobNameTextBox.Text;
            Settings.UploadLocation = UploadLocationTextBox.Text;
            Settings.UploadMaxLines = Convert.ToInt32(UploadMaxLinesControl.Value);
            Settings.Category = LoggingCategoryTextBox.Text;
            Settings.UploadOpenInBrowser = UploadOpenInBrowserCheckBox.Checked;
            Settings.UploadToWikiProjects = UploadWikiProjectCheckBox.Checked;
            Settings.DebugUploading = DebugUploadingCheckBox.Checked;

            if (mInitialised)
                Program.MyTrace.PropertiesChange();
        }
        internal void WEHaveUnappliedChanges()
        {
            if (!mStartingUp)
            {
                if (Program.MyTrace.HaveOpenFile)
                {
                    ApplyButton.Enabled = true;
                    ApplyButton.BackColor = System.Drawing.Color.Red;
                }
                else
                {
                    GetSettingsFromControls();
                }
            }
        }
        internal void WEHaveUnappliedChanges(object sender, EventArgs e)
        { WEHaveUnappliedChanges(); }
        private void DisableApplyButton()
        {
            ApplyButton.Enabled = false;
            ApplyButton.BackColor = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.Control);
        }
        private void EnableDisableUploadControls(bool enabled)
        {
            LoggingCategoryTextBox.Enabled = enabled;
            UploadJobNameTextBox.Enabled = enabled;
            UploadLocationTextBox.Enabled = enabled;
            UploadOpenInBrowserCheckBox.Enabled = enabled;
            UploadWatchlistCheckBox.Enabled = enabled;
            UploadWikiProjectCheckBox.Enabled = enabled;
            UploadMaxLinesControl.Enabled = enabled;
        }
        #endregion

        #region Event Handlers
		private void ApplyButton_Click(object sender, EventArgs e)
		{
			if (UploadCheckBox.Checked && string.IsNullOrEmpty(UploadJobNameTextBox.Text.Trim()))
			{
				MessageBox.Show("Please enter a job name", "Job name missing", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			else
			{
				GetSettingsFromControls();
			}
		}
        private void CategoryReset(object sender, EventArgs e)
        {
            UploadLocationTextBox.Text = "";
        }
		private void LocationReset(object sender, EventArgs e)
		{
			UploadLocationTextBox.Text = Props.ConUploadToUserSlashLogsToken;
		}
		private void JobNameReset(object sender, EventArgs e)
		{
			UploadJobNameTextBox.Text = Props.ConUploadCategoryIsJobName;
		}
		private void MaxLinesReset(object sender, EventArgs e)
		{
			UploadMaxLinesControl.Value = 1000;
		}
		private void SetLinesToMaximum(object sender, EventArgs e)
		{
			UploadMaxLinesControl.Value = UploadMaxLinesControl.Maximum;
		}
		private void WikiLogCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (WikiLogCheckBox.Checked && ! UploadCheckBox.Enabled)
			{
				UploadCheckBox.Enabled = true;
				EnableDisableUploadControls(UploadCheckBox.Checked);
			}
			else if (! WikiLogCheckBox.Checked && UploadCheckBox.Enabled)
			{
				UploadCheckBox.Enabled = false;
				EnableDisableUploadControls(false);
			}

			WEHaveUnappliedChanges();
		}
		private void UploadCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			EnableDisableUploadControls(UploadCheckBox.Checked);
			WEHaveUnappliedChanges();
		}
		private void FolderButton_Click(object sender, EventArgs e)
		{
			if (FolderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
			{
				FolderTextBox.Text = FolderBrowserDialog1.SelectedPath;
				WEHaveUnappliedChanges();
			}
		}

        private void ResetButton_Click(object sender, EventArgs e)
        {
            XHTMLLogCheckBox.Checked = SQLLogCheckBox.Checked =
                VerboseCheckBox.Checked = UploadCheckBox.Checked = false;
            WikiLogCheckBox.Checked = UploadOpenInBrowserCheckBox.Checked =
                UploadWatchlistCheckBox.Checked = UploadWikiProjectCheckBox.Checked = true;
            CategoryReset(null, null);
            LocationReset(null, null);
            JobNameReset(null, null);
            MaxLinesReset(null, null);
            if(string.IsNullOrEmpty(FolderTextBox.Text))
                FolderButton_Click(null, null);
        }

        private void toolStripMenuItemCategoryCut_Click(object sender, EventArgs e)
        {
            LoggingCategoryTextBox.Cut();
        }

        private void toolStripMenuItemCategoryCopy_Click(object sender, EventArgs e)
        {
            LoggingCategoryTextBox.Copy();
        }

        private void toolStripMenuItemCategoryPaste_Click(object sender, EventArgs e)
        {
            LoggingCategoryTextBox.Paste();
        }

        private void toolStripMenuItemCategoryClear_Click(object sender, EventArgs e)
        {
            LoggingCategoryTextBox.Text = "";
        }
#endregion

        internal sealed class Props : UploadableLogSettings2
        {
            private bool mUploadToWikiProjects = true, mDebugUploading;
            private string mCategory = "";
            internal const string ConUploadToUserSlashLogsToken = "$USER/Logs";
            internal const string ConUploadCategoryIsJobName = "$CATEGORY";

            internal Props()
            {
                mUploadLocation = ConUploadToUserSlashLogsToken;
                mUploadJobName = ConUploadCategoryIsJobName;
            }

            internal bool Equals(Props compare)
            {
                return ((compare.LogFolder == LogFolder) && (compare.LogVerbose == LogVerbose) 
                    && (compare.LogWiki == LogWiki) && (compare.LogXHTML == LogXHTML)
                    && (compare.UploadAddToWatchlist == UploadAddToWatchlist) && (compare.UploadJobName == UploadJobName)
                    && (compare.UploadLocation == UploadLocation) && (compare.UploadMaxLines == UploadMaxLines)
                    && (compare.UploadOpenInBrowser == UploadOpenInBrowser)
                    && (compare.UploadToWikiProjects == UploadToWikiProjects) && (compare.UploadYN == UploadYN)
                    && (compare.Category == Category) && (compare.DebugUploading == DebugUploading));
            }

            #region Additional properties:
            internal bool UploadToWikiProjects
            {
                get { return mUploadToWikiProjects; }
                set { mUploadToWikiProjects = value; }
            }
            internal bool DebugUploading
            {
                get { return mDebugUploading; }
                set { mDebugUploading = value; }
            }
            internal List<LogEntry> LinksToLog()
            {
                List<LogEntry> tempLinksToLog = new List<LogEntry>();
                // TODO: We could create a temporary AWB logs page and have alpha version upload to that?
                tempLinksToLog.Add(new LogEntry(GlobbedUploadLocation, false));

                // If "uploading to WikiProjects" (or other plugin-specified locations), get details from plugins:
                if (mUploadToWikiProjects)
                    ((MainForm)Program.AWB).GetLogUploadLocationsEvent(this, tempLinksToLog);

                return tempLinksToLog;
            }
            internal string GlobbedUploadLocation
            {
                get { return mUploadLocation.Replace("$USER", "User:" + UserName); }
            }
            internal static string UserName
            {
                get { return Variables.User.Name; }
            }
            internal string Category
            {
                get { return mCategory; }
                set { mCategory = value; }
            }
            internal string WikifiedCategory
            {
                get
                {
                    if (string.IsNullOrEmpty(mCategory))
                        return "";

                    return "[[:" + Variables.Namespaces[Namespace.Category] + mCategory 
                        + "|" + mCategory + "]]";
                }
            }
            internal string LogTitle
            {
                get { return Tools.RemoveInvalidChars(mUploadJobName.Replace(ConUploadCategoryIsJobName, mCategory)); }
            }
            #endregion
        }
    }
}
