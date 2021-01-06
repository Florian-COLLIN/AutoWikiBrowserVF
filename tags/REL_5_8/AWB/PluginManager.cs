﻿/*
AWB Plugin Manager
Copyright
(C) 2007 Martin Richards
(C) 2008 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/
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
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Linq;
using AutoWikiBrowser.Plugins;
using WikiFunctions.Plugin;
using WikiFunctions;

namespace AutoWikiBrowser
{
    internal sealed partial class PluginManager : Form
    {
        private readonly IAutoWikiBrowser _awb;
        // List<string> prevArticlePlugins;
        // List<string> prevLMPlugins;

        private static string _lastPluginLoadedLocation;

        public PluginManager(IAutoWikiBrowser awb) //, List<string> previousPlugins)
        {
            InitializeComponent();
            _awb = awb;
            // prevPlugins = previousPlugins;
        }

        public static void LoadNewPlugin(IAutoWikiBrowser awb)
        {
            OpenFileDialog pluginOpen = new OpenFileDialog();
            if (string.IsNullOrEmpty(_lastPluginLoadedLocation))
                LoadLastPluginLoadedLocation();

            pluginOpen.InitialDirectory = string.IsNullOrEmpty(_lastPluginLoadedLocation) ? Application.StartupPath : _lastPluginLoadedLocation;

            pluginOpen.DefaultExt = "dll";
            pluginOpen.Filter = "DLL files|*.dll";
            pluginOpen.CheckFileExists = pluginOpen.Multiselect = true;

            pluginOpen.ShowDialog();

            if (!string.IsNullOrEmpty(pluginOpen.FileName))
            {
                string newPath = Path.GetDirectoryName(pluginOpen.FileName);
                if (_lastPluginLoadedLocation != newPath)
                {
                    _lastPluginLoadedLocation = newPath;
                    SaveLastPluginLoadedLocation();
                }
            }

            Plugin.LoadPlugins(awb, pluginOpen.FileNames, true);
        }

        //TODO:Use Utils
        static void LoadLastPluginLoadedLocation()
        {
            try
            {
                Microsoft.Win32.RegistryKey reg = Microsoft.Win32.Registry.CurrentUser.
                    OpenSubKey("Software\\AutoWikiBrowser");

                if (reg != null)
                    _lastPluginLoadedLocation = reg.GetValue("RecentPluginLoadedLocation", "").ToString();
            }
            catch
            {
            }
        }

        static void SaveLastPluginLoadedLocation()
        {
            try
            {
                Microsoft.Win32.RegistryKey reg = Microsoft.Win32.Registry.CurrentUser.
                    CreateSubKey("Software\\AutoWikiBrowser");

                if (reg != null)
                    reg.SetValue("RecentPluginLoadedLocation", _lastPluginLoadedLocation);
            }
            catch
            {
            }
        }

        private void PluginManager_Load(object sender, EventArgs e)
        {
            LoadLoadedPluginList();
            //LoadPreviouslyLoadedPluginList();
        }

        private void loadNewPluginsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadNewPlugin(_awb);
            lvPlugin.Items.Clear();
            LoadLoadedPluginList();
            //LoadPreviouslyLoadedPluginList();
        }

        private void LoadLoadedPluginList()
        {
            foreach (string pluginName in Plugin.GetAWBPluginList())
            {
                lvPlugin.Items.Add(new ListViewItem(pluginName) { Group = lvPlugin.Groups["groupAWBLoaded"] });
            }
            foreach (string pluginName in Plugin.GetBasePluginList())
            {
                lvPlugin.Items.Add(new ListViewItem(pluginName) { Group = lvPlugin.Groups["groupBaseLoaded"] });
            }
            foreach (string pluginName in Plugin.GetListMakerPluginList())
            {
                lvPlugin.Items.Add(new ListViewItem(pluginName) { Group = lvPlugin.Groups["groupLMLoaded"] });
            }

            UpdatePluginCount();
        }

        //private void LoadPreviouslyLoadedPluginList()
        //{
        //    foreach (string pluginName in prevArticlePlugins)
        //    {
        //        if (System.IO.File.Exists(pluginName))
        //        {
        //            lvi = new ListViewItem(pluginName);
        //            lvi.Group = lvPlugin.Groups["groupAWBPrevious"];
        //            lvPlugin.Items.Add(lvi);
        //        }
        //    }
        //    foreach (string pluginName in prevLMPlugins)
        //    {
        //        if (System.IO.File.Exists(pluginName))
        //        {
        //            lvi = new ListViewItem(pluginName);
        //            lvi.Group = lvPlugin.Groups["groupLMPrevious"];
        //            lvPlugin.Items.Add(lvi);
        //        }
        //    }
        //}

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            //foreach (ListViewItem item in lvPlugin.SelectedItems)
            //{
            //    if (item.Group == lvPlugin.Groups["groupAWBLoaded"])
            //    {
            //        loadPluginToolStripMenuItem.Enabled = false;
            //        return;
            //    }
            //}
            //loadPluginToolStripMenuItem.Enabled = true;
            loadPluginToolStripMenuItem.Enabled = false;
        }

        private void loadPluginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string[] plugins = new string[lvPlugin.SelectedItems.Count];

            for (int i = 0; i < lvPlugin.SelectedItems.Count; i++)
            {
                plugins[i] = lvPlugin.Items[lvPlugin.SelectedIndices[i]].Text;
            }

            Plugin.LoadPlugins(_awb, plugins, true);
        }

        private void UpdatePluginCount()
        {
            lblPluginCount.Text = lvPlugin.Items.Count.ToString();
        }
    }

    namespace Plugins
    {
        internal static class Plugin
        {
            static Plugin()
            {
                ErrorHandler.AppendToErrorHandler += ErrorHandlerAppendToErrorHandler;
            }

            static string ErrorHandlerAppendToErrorHandler()
            {
            	if (!AWBPlugins.Any() && !AWBBasePlugins.Any() && !ListMakerPlugins.Any())
                    return "";

                StringBuilder builder = new StringBuilder();

                builder.AppendLine("```");

                builder.AppendLine("AWBPlugins");
                foreach (var p in AWBPlugins)
                {
                    builder.AppendLine("- " + p.Key);
                }
                builder.AppendLine();

                builder.AppendLine("AWBBasePlugins");
                foreach (var p in AWBBasePlugins)
                {
                    builder.AppendLine("- " + p.Key);
                }
                builder.AppendLine();

                builder.AppendLine("ListMakerPlugins");
                foreach (var p in ListMakerPlugins)
                {
                    builder.AppendLine("- " + p.Key);
                }

                builder.AppendLine("```");

                return builder.ToString();
            }

            /// <summary>
            /// Dictionary of Plugins, name, and reference to AWB Plugin
            /// </summary>
            internal static readonly Dictionary<string, IAWBPlugin> AWBPlugins = new Dictionary<string, IAWBPlugin>();

            internal static readonly Dictionary<string, IAWBBasePlugin> AWBBasePlugins = new Dictionary<string, IAWBBasePlugin>();

            internal static readonly Dictionary<string, IListMakerPlugin> ListMakerPlugins = new Dictionary<string, IListMakerPlugin>();

            public static readonly List<IAWBPlugin> FailedPlugins = new List<IAWBPlugin>();

            /// <summary>
            /// Gets a List of all the plugin names currently loaded
            /// </summary>
            /// <returns>List of Plugin Names</returns>
            internal static List<string> GetAWBPluginList()
            {
                return AWBPlugins.Select(a => a.Key).ToList();
            }

            /// <summary>
            /// Gets a List of all the plugin names currently loaded
            /// </summary>
            /// <returns>List of Plugin Names</returns>
            internal static List<string> GetBasePluginList()
            {
                return AWBBasePlugins.Select(a => a.Key).ToList();
            }

            /// <summary>
            /// Gets a list of all the List Maker Plugins currently loaded
            /// </summary>
            /// <returns>List of Plugin Names</returns>
            internal static List<string> GetListMakerPluginList()
            {
                return ListMakerPlugins.Select(a => a.Key).ToList();
            }

            /// <summary>
            /// Loads the plugin at startup, and updates the splash screen
            /// </summary>
            /// <param name="awb">IAutoWikiBrowser instance of AWB</param>
            /// <param name="splash">Splash Screen instance</param>
            internal static void LoadPluginsStartup(IAutoWikiBrowser awb, Splash splash)
            {
                splash.SetProgress(25);
                string path = Application.StartupPath;
                string[] pluginFiles = Directory.GetFiles(path, "*.DLL");

                LoadPlugins(awb, pluginFiles, false);
                splash.SetProgress(50);
            }

            public static void PluginObsolete(IAWBPlugin plugin)
            {
                if (!FailedPlugins.Contains(plugin))
                    FailedPlugins.Add(plugin);

                PluginObsolete(plugin.GetType().Assembly.Location, plugin.GetType().Assembly.GetName().Version.ToString());
            }

            static void PluginObsolete(string name, string version)
            {
                MessageBox.Show(
                    "The plugin '" + name + "' " + (!string.IsNullOrEmpty(name) ? "(" + version + ")" : "") +
                    "is out-of date and needs to be updated.",
                    "Plugin error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            public static void PurgeFailedPlugins()
            {
            	if (!FailedPlugins.Any())
            		return;

                foreach (IAWBPlugin p in FailedPlugins)
                {
                    foreach (string s in AWBPlugins.Keys)
                    {
                        if (AWBPlugins[s] == p)
                        {
                            AWBPlugins.Remove(s);
                            break;
                        }
                    }
                }
                FailedPlugins.Clear();
            }

            private static List<string> NotPlugins = new List<string>(new[] {"DotNetWikiBot", "Diff", "WikiFunctions", "Newtonsoft.Json", "Microsoft.mshtml"});

            /// <summary>
            /// Loads all the plugins from the directory where AWB resides
            /// </summary>
            /// <param name="awb">IAutoWikiBrowser instance of AWB</param>
            /// <param name="plugins">Array of Plugin Names</param>
            /// <param name="afterStartup">Whether the plugin(s) are being loaded post-startup</param>
            internal static void LoadPlugins(IAutoWikiBrowser awb, string[] plugins, bool afterStartup)
            {
                try
                {
                    // ignore known DLL files that aren't plugins such as WikiFunctions.dll
                    plugins = plugins.Where(p => !NotPlugins.Any(n => p.EndsWith(n + ".dll"))).ToArray();

                    foreach (string plugin in plugins)
                    {
                        Assembly asm;
                        try
                        {
                            asm = Assembly.LoadFile(plugin);
                        }
#if DEBUG
                        catch (Exception ex)
                        {
                            Tools.WriteDebug(plugin, ex.ToString());
                            continue;
                        }
#else
                        catch (Exception)
                        {
                            continue;
                        }
#endif

                        if (asm == null)
                            continue;

                        try
                        {
                            foreach (Type t in asm.GetTypes())
                            {
                                if (t.GetInterface("IAWBPlugin") != null)
                                {
                                    IAWBPlugin awbPlugin =
                                        (IAWBPlugin) Activator.CreateInstance(t);

                                    if (AWBPlugins.ContainsKey(awbPlugin.Name))
                                    {
                                        MessageBox.Show(
                                            "A plugin with the name \"" + awbPlugin.Name +
                                            "\", has already been added.\r\nPlease remove old duplicates from your AutoWikiBrowser Directory, and restart AWB.\r\nThis was loaded from the plugin file \"" +
                                            plugin + "\".", "Duplicate AWB Plugin");
                                        break;
                                    }

                                    InitialisePlugin(awbPlugin, awb);

                                    AWBPlugins.Add(awbPlugin.Name, awbPlugin);

                                    if (afterStartup) UsageStats.AddedPlugin(awbPlugin);
                                }
                                else if (t.GetInterface("IAWBBasePlugin") != null)
                                    //IAWBBasePlugin needs to be checked after IAWBPlugin, as IAWBPlugin extends IAWBBasePlugin
                                {
                                    IAWBBasePlugin awbBasePlugin = (IAWBBasePlugin) Activator.CreateInstance(t);

                                    if (AWBBasePlugins.ContainsKey(awbBasePlugin.Name))
                                    {
                                        MessageBox.Show(
                                            "A plugin with the name \"" + awbBasePlugin.Name +
                                            "\", has already been added.\r\nPlease remove old duplicates from your AutoWikiBrowser Directory, and restart AWB.\r\nThis was loaded from the plugin file \"" +
                                            plugin + "\".", "Duplicate AWB Base Plugin");
                                        break;
                                    }

                                    InitialisePlugin(awbBasePlugin, awb);

                                    AWBBasePlugins.Add(awbBasePlugin.Name, awbBasePlugin);

                                    if (afterStartup) UsageStats.AddedPlugin(awbBasePlugin);
                                }
                                else if (t.GetInterface("IListMakerPlugin") != null)
                                {
                                    IListMakerPlugin listMakerPlugin =
                                        (IListMakerPlugin) Activator.CreateInstance(t);

                                    if (ListMakerPlugins.ContainsKey(listMakerPlugin.Name))
                                    {
                                        MessageBox.Show(
                                            "A plugin with the name \"" + listMakerPlugin.Name +
                                            "\", has already been added.\r\nPlease remove old duplicates from your AutoWikiBrowser Directory, and restart AWB.\r\nThis was loaded from the plugin file \"" +
                                            plugin + "\".", "Duplicate AWB ListMaker Plugin");
                                        break;
                                    }

                                    WikiFunctions.Controls.Lists.ListMaker.AddProvider(listMakerPlugin);

                                    ListMakerPlugins.Add(listMakerPlugin.Name, listMakerPlugin);

                                    if (afterStartup) UsageStats.AddedPlugin(listMakerPlugin);
                                }
                            }
                        }
                        catch (ReflectionTypeLoadException)
                        {
                            PluginObsolete(plugin, asm.GetName().Version.ToString());
                        }
                        catch (MissingMemberException)
                        {
                            PluginObsolete(plugin, asm.GetName().Version.ToString());
                        }
                        catch (Exception ex)
                        {
                            ErrorHandler.HandleException(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    ErrorHandler.HandleException(ex);
#else
                    MessageBox.Show(ex.Message, "Problem loading plugins");
#endif
                }

            }

            /// <summary>
            /// Passes a reference of the main form to the plugin for initialisation
            /// </summary>
            /// <param name="plugin">IAWBBasePlugin (Or IAWBPlugin) to initialise</param>
            /// <param name="awb">IAutoWikiBrowser instance of AWB</param>
            private static void InitialisePlugin(IAWBBasePlugin plugin, IAutoWikiBrowser awb)
            {
                plugin.Initialise(awb);
            }

            /// <summary>
            /// Gets the Version string of a IAWBBasePlugin
            /// </summary>
            /// <param name="plugin">IAWBBasePlugin to get Version of</param>
            /// <returns>Version String</returns>
            internal static string GetPluginVersionString(IAWBBasePlugin plugin)
            { return Assembly.GetAssembly(plugin.GetType()).GetName().Version.ToString(); }

            /// <summary>
            /// Gets the Version string of a IListMakerPlugin
            /// </summary>
            /// <param name="plugin">IListMakerPlugin to get Version of</param>
            /// <returns>Version String</returns>
            internal static string GetPluginVersionString(IListMakerPlugin plugin)
            { return Assembly.GetAssembly(plugin.GetType()).GetName().Version.ToString(); }
        }
    }
}