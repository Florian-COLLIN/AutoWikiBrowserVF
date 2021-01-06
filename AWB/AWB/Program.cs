﻿/*
Autowikibrowser
Copyright (C) 2007 Martin Richards
(C) 2008 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

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
using System.Security;
using System.Windows.Forms;
using WikiFunctions;

namespace AutoWikiBrowser
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {        
                System.Threading.Thread.CurrentThread.Name = "Main thread";
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += ApplicationThreadException;

                if (Globals.UsingMono)
                {
                    MessageBox.Show("AWB is not currently supported by mono. You may use it for testing purposes, but functionality is not guaranteed.",
                        "Not supported",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                AwbDirs.MigrateDefaultSettings();

                MainForm awb = new MainForm();
                AWB = awb;
                awb.ParseCommandLine(args);

                Article.SetAddListener(MyTrace.AddListener, MyTrace, "AWB");

                Application.Run(awb);
            }
            catch (Exception ex)
            {
                if (ex is SecurityException) //"Fix" - http://geekswithblogs.net/TimH/archive/2006/03/08/71714.aspx
                    MessageBox.Show("AWB is unable to start up from the current location due to a lack of permissions.\r\nPlease try on a local drive or similar.", "Permissions Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    ErrorHandler.HandleException(ex);
            }
        }

        private static void ApplicationThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            ErrorHandler.HandleException(e.Exception);
        }

        internal static Version Version { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version; } }
        internal static string VersionString { get { return Version.ToString(); } }
        internal const string Name = "AutoWikiBrowser";
        internal static WikiFunctions.Plugin.IAutoWikiBrowser AWB;
        internal static readonly Logging.MyTrace MyTrace = new Logging.MyTrace();
    }
}