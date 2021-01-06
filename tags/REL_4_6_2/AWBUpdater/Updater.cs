/*
AWBUpdater
Copyright (C) 2008 Sam Reed, Max Semenik

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
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;

using System.Text.RegularExpressions;
using System.Net;

namespace AwbUpdater
{
    internal sealed partial class Updater : Form
    {
        private readonly string AWBdirectory = "", TempDirectory = "";
        private string AWBZipName = "", AWBWebAddress = "";
        string UpdaterZipName = "", UpdaterWebAddress = "";

        IWebProxy Proxy;

        private bool UpdaterUpdate;
        private bool AWBUpdate;
        private bool UpdateSucessful;

        public Updater()
        {
            InitializeComponent();

            AWBdirectory = Path.GetDirectoryName(Application.ExecutablePath) + "\\";
            TempDirectory = Environment.GetEnvironmentVariable("TEMP") ?? "C:\\Windows\\Temp";
            if (!TempDirectory.EndsWith("\\")) TempDirectory += "\\";
            TempDirectory += "$AWB$Updater$Temp$\\";
        }

        /// <summary>
        /// Version of the Updater
        /// </summary>
        private static int AssemblyVersion
        {
            get { return StringToVersion(Assembly.GetExecutingAssembly().GetName().Version.ToString()); }
        }

        private void Updater_Load(object sender, EventArgs e)
        {
            tmrTimer.Enabled = true;
            UpdateUI("Initialising...", true);
        }

        /// <summary>
        /// Main program function
        /// </summary>
        private void UpdateAwb()
        {
            try
            {
                Proxy = WebRequest.GetSystemWebProxy();

                if (Proxy.IsBypassed(new Uri("http://en.wikipedia.org")))
                    Proxy = null;

                UpdateUI("Getting current AWB and Updater versions", true);
                AWBversion();

                if ((!UpdaterUpdate && !AWBUpdate) && string.IsNullOrEmpty(AWBWebAddress))
                    ExitEarly();
                else
                {
                    UpdateUI("Creating a temporary directory", true);
                    CreateTempDir();

                    UpdateUI("Downloading AWB", true);
                    GetAwbFromInternet();

                    UpdateUI("Unzipping AWB to the temporary directory", true);
                    UnzipAwb();

                    UpdateUI("Making sure AWB is closed", true);
                    CloseAwb();

                    UpdateUI("Copying AWB files from temp to AWB directory...", true);
                    CopyFiles();
                    UpdateUI("Update successful", true);


                    UpdateUI("Cleaning up from update", true);
                    KillTempDir();

                    UpdateSucessful = true;
                    ReadyToExit();
                }
            }
            catch (AbortException)
            {
                ReadyToExit();
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        #region UI functions
        /// <summary>
        /// Multiple use function to update the GUI items
        /// </summary>
        /// <param name="currentStatus">What the updater is currently doing</param>
        /// <param name="newLine">If true, adds new line to log instead of reusing last existing one</param>
        private void UpdateUI(string currentStatus, bool newLine)
        {
            if (newLine)
            {
                lstLog.Items.Add(currentStatus);
            }
            else
            {
                lstLog.Items[lstLog.Items.Count - 1] = currentStatus;
            }
            lstLog.SelectedIndex = lstLog.Items.Count - 1;
            Application.DoEvents();
        }

        private void AppendLine(string line)
        {
            lstLog.Items[lstLog.Items.Count - 1] += line;
        }

        /// <summary>
        /// Close the updater early due to lack of updates
        /// </summary>
        private void ExitEarly()
        {
            UpdateUI("Nothing to update", true);
            StartAwb();
            ReadyToExit();
        }

        /// <summary>
        /// Sets UI to "ready to exit" state
        /// </summary>
        private void ReadyToExit()
        {
            btnCancel.Text = "Close";
            lblStatus.Text = "Press close to exit";
            progressUpdate.Visible = false;
            btnCancel.Enabled = true;
        }
        #endregion

        /// <summary>
        /// Creates the temporary folder if it doesnt already exist
        /// </summary>
        private void CreateTempDir()
        {
            if (Directory.Exists(TempDirectory))
            {
                // clear its content just to be sure that no parasitic files are left
                Directory.Delete(TempDirectory, true);
            }

            Directory.CreateDirectory(TempDirectory);

            progressUpdate.Value = 10;
        }

        /// <summary>
        /// Checks and compares the current AWB version with the version listed on the checkpage
        /// </summary>
        private void AWBversion()
        {
            string text;

            UpdateUI("   Retrieving current version...", true);
            try
            {
                HttpWebRequest rq = (HttpWebRequest)WebRequest.Create("http://en.wikipedia.org/w/index.php?title=Wikipedia:AutoWikiBrowser/CheckPage/Version&action=raw");

                rq.Proxy = Proxy;

                rq.UserAgent = "AWBUpdater " + Assembly.GetExecutingAssembly().GetName().Version;

                HttpWebResponse response = (HttpWebResponse)rq.GetResponse();

                Stream stream = response.GetResponseStream();
                StreamReader sr = new StreamReader(stream, Encoding.UTF8);

                text = sr.ReadToEnd();

                sr.Close();
                stream.Close();
                response.Close();
            }
            catch
            {
                AppendLine("FAILED");
                throw new AbortException();
            }

            int awbCurrentVersion =
                StringToVersion(Regex.Match(text, @"<!-- Current version: (.*?) -->").Groups[1].Value);
            int awbNewestVersion =
                StringToVersion(Regex.Match(text, @"<!-- Newest version: (.*?) -->").Groups[1].Value);
            int updaterVersion = StringToVersion(Regex.Match(text, @"<!-- Updater version: (.*?) -->").Groups[1].Value);

            if ((awbCurrentVersion > 4000) || (awbNewestVersion > 4000))
            {
                try
                {
                    AWBUpdate = UpdaterUpdate = false;
                    FileVersionInfo awbVersionInfo = FileVersionInfo.GetVersionInfo(AWBdirectory + "AutoWikiBrowser.exe");
                    int awbFileVersion = StringToVersion(awbVersionInfo.FileVersion);

                    if (awbFileVersion < awbCurrentVersion)
                        AWBUpdate = true;
                    else if ((awbFileVersion >= awbCurrentVersion) &&
                        (awbFileVersion < awbNewestVersion) &&
                        MessageBox.Show("There is an optional update to AutoWikiBrowser. Would you like to upgrade?", "Optional update", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        AWBUpdate = true;

                    if (AWBUpdate)
                    {
                        AWBZipName = "AutoWikiBrowser" + awbNewestVersion + ".zip";
                        AWBWebAddress = "http://downloads.sourceforge.net/autowikibrowser/" + AWBZipName;
                    }
                    else if ((updaterVersion > 1400) &&
                        (updaterVersion > AssemblyVersion))
                    {
                        UpdaterZipName = "AWBUpdater" + updaterVersion + ".zip";
                        UpdaterWebAddress = "http://downloads.sourceforge.net/autowikibrowser/" + UpdaterZipName;
                        UpdaterUpdate = true;
                    }
                }
                catch
                { UpdateUI("   Unable to find AutoWikiBrowser.exe to query its version", true); }

                progressUpdate.Value = 35;
            }
        }

        /// <summary>
        /// Check the addresses for the files are valid (not null or empty), and downloads the files from the internet
        /// </summary>
        private void GetAwbFromInternet()
        {
            WebClient client = new WebClient {Proxy = Proxy};

            if (!string.IsNullOrEmpty(AWBWebAddress))
                client.DownloadFile(AWBWebAddress, TempDirectory + AWBZipName);
            else if (!string.IsNullOrEmpty(UpdaterWebAddress))
                client.DownloadFile(UpdaterWebAddress, TempDirectory + UpdaterZipName);

            client.Dispose();

            progressUpdate.Value = 50;
        }

        /// <summary>
        /// Checks the zip files exist and calls the functions to unzip them
        /// </summary>
        private void UnzipAwb()
        {
            if (!string.IsNullOrEmpty(AWBZipName) && File.Exists(TempDirectory + AWBZipName))
                Extract(TempDirectory + AWBZipName);

            if (!string.IsNullOrEmpty(UpdaterZipName) && File.Exists(TempDirectory + UpdaterZipName))
                Extract(TempDirectory + UpdaterZipName);

            progressUpdate.Value = 70;
        }

        /// <summary>
        /// Code used to unzip the zip files to the temporary directory
        /// </summary>
        /// <param name="file"></param>
        private void Extract(string file)
        {
            try
            {
                new FastZip().ExtractZip(file, TempDirectory, null);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
                Application.Exit();
            }
        }

        /// <summary>
        /// Looks if AWB or IRCM are open. If they are, tell the user
        /// </summary>
        private void CloseAwb()
        {
            bool awbOpen = false;

            do
            {
                foreach (Process p in Process.GetProcesses())
                {
                    awbOpen = (p.ProcessName == "AutoWikiBrowser" || p.ProcessName == "IRCMonitor");
                    if (awbOpen)
                    {
                        MessageBox.Show("Please save your settings (if you wish) and close " + p.ProcessName + " completely before pressing OK.");
                        break;
                    }
                }
            }
            while (awbOpen);

            progressUpdate.Value = 75;
        }

        /// <summary>
        /// Copies files from the temporary to the working directory
        /// </summary>
        private void CopyFiles()
        {
            if (UpdaterUpdate && File.Exists(TempDirectory + "AWBUpdater.exe"))
                CopyFile(TempDirectory + "AWBUpdater.exe", AWBdirectory + "AWBUpdater.exe.new");

            if (AWBUpdate)
            {
                //Explicit Deletions (Remove these if they exist!!)
                if (File.Exists(AWBdirectory + "Wikidiff2.dll"))
                    File.Delete(AWBdirectory + "Wikidiff2.dll");

                if (File.Exists(AWBdirectory + "WikiFunctions2.dll"))
                    File.Delete(AWBdirectory + "WikiFunctions2.dll");

                if (File.Exists(AWBdirectory + "WPAssessmentsCatCreator.dll"))
                    File.Delete(AWBdirectory + "WPAssessmentsCatCreator.dll");

                if (Directory.Exists(AWBdirectory + "\\Plugins\\WPAssessmentsCatCreator"))
                    Directory.Delete(AWBdirectory + "\\Plugins\\WPAssessmentsCatCreator", true);

                foreach (string file in Directory.GetFiles(TempDirectory, "*.*", SearchOption.AllDirectories))
                {
                    if (!file.Contains("AWBUpdater"))
                        CopyFile(file, AWBdirectory + file.Replace(TempDirectory, ""));
                    else
                        CopyFile(file, AWBdirectory + file.Replace(TempDirectory, "") + ".new");
                }

                string[] pluginFiles = Directory.GetFiles(AWBdirectory + "\\Plugins", "*.*", SearchOption.AllDirectories);

                foreach (string file in Directory.GetFiles(AWBdirectory, "*.*", SearchOption.TopDirectoryOnly))
                {
                    foreach (string pluginFile in pluginFiles)
                    {
                        if (file.Substring(file.LastIndexOf("\\")) == pluginFile.Substring(pluginFile.LastIndexOf("\\")))
                        {
                            File.Copy(pluginFile, file, true);
                            break;
                        }
                    }
                }
            }
            progressUpdate.Value = 95;
        }

        private void CopyFile(string source, string destination)
        {
            CreatePath(destination);
            UpdateUI("     " + destination, true);

            // loop until the file is successfully copied, or user is tired of retrying
            while (true)
            {
                try
                {
                    File.Copy(source, destination, true);
                }
                catch (Exception ex)
                {
                    if (MessageBox.Show(
                            this,
                            "Problem replacing file:\r\n   " + ex.Message + "\r\n\r\n" +
                            "Please close all applications that may use it and press 'Retry' to try again " +
                            "or 'Cancel' to cancel the upgrade.",
                            "Error",
                            MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                    {
                        continue;
                    }


                    AppendLine("... FAILED");
                    UpdateUI("Update aborted. AutoWikiBrowser may be unfunctional", true);
                    KillTempDir();
                    ReadyToExit();
                    throw new AbortException();

                }

                break;
            }
        }

        /// <summary>
        /// Creates all subdirectories in the path, if needed
        /// </summary>
        /// <param name="path">Path to process, assumed to start from </param>
        private void CreatePath(string path)
        {
            path = Path.GetDirectoryName(path); // strip filename
            if (!Directory.Exists(path))
            {
                UpdateUI("   Creating directory " + path + "...", true);
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception ex)
                {
                    AppendLine(" FAILED");
                    UpdateUI("     (" + ex.Message + ")", true);
                }
            }
        }

        /// <summary>
        /// Starts AWB if exists and is not already running
        /// </summary>
        private void StartAwb()
        {
            bool awbOpen = false;
            foreach (Process p in Process.GetProcesses())
            {
                awbOpen = (p.ProcessName == "AutoWikiBrowser");
                if (awbOpen)
                    break;
            }

            if (!awbOpen && File.Exists(AWBdirectory + "AutoWikiBrowser.exe")
                && MessageBox.Show("Would you like to start AWB?", "Start AWB?", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Process.Start(AWBdirectory + "AutoWikiBrowser.exe");
            }

            progressUpdate.Value = 99;
        }

        /// <summary>
        /// Deletes the temporary directory
        /// </summary>
        private void KillTempDir()
        {
            if (Directory.Exists(TempDirectory))
                Directory.Delete(TempDirectory, true);
            progressUpdate.Value = 100;
        }

        private void tmrTimer_Tick(object sender, EventArgs e)
        {
            tmrTimer.Enabled = false;
            UpdateAwb();
        }

        static int StringToVersion(string version)
        {
            int res;
            if (!int.TryParse(version.Replace(".", ""), out res))
                res = 0;

            return res;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (UpdateSucessful) StartAwb();

            Close();
        }
    }


    /// <summary>
    /// This exception stops processing and prepared the updater for exit
    /// </summary>
    [Serializable]
    public class AbortException : Exception
    {
        public AbortException()
        { }

        public AbortException(string message)
            : base(message)
        { }

        public AbortException(string message, Exception innerException) :
            base(message, innerException)
        { }

        protected AbortException(System.Runtime.Serialization.SerializationInfo info,
           System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}
