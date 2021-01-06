/*
Copyright (C) 2008 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/
Copyright (C) 2008 Sam Reed (Reedy) http://www.reedyboy.net/

This program is free software; you can redistribute it and/or modify it under the terms of Version 2 of the GNU General Public License as published by the Free Software Foundation.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License Version 2 along with this program; if not, write to the Free Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System.Windows.Forms;
using System.Xml;

namespace AutoWikiBrowser.Plugins.Kingbotk.WikiProjects
{
    internal sealed class WPAlbums : PluginBase
    {
        private const string PluginName = "WikiProject Albums";

        private const string Prefix = "Albums";

        internal WPAlbums() : base("Album|Albums|WP Albums|WPAlbums")
        {
            // Specify alternate names only
            TemplateParameters[] @params = new TemplateParameters[-1 + 1];

            _ourSettingsControl = new GenericWithWorkgroups("WikiProject Albums", Prefix, true, @params);
        }

        // Settings:
        private readonly TabPage _ourTab = new TabPage("Albums");

        private readonly GenericWithWorkgroups _ourSettingsControl;

        protected internal override string PluginShortName
        {
            get { return "Albums"; }
        }

        protected override string PreferredTemplateName
        {
            get { return PluginName; }
        }

        protected override void ImportanceParameter(Importance importance)
        {
            Template.NewOrReplaceTemplateParm("importance", importance.ToString(), TheArticle, false, false);
        }

        protected internal override IGenericSettings GenericSettings
        {
            get { return _ourSettingsControl; }
        }

        // Initialisation:
        protected internal override void Initialise()
        {
            OurMenuItem = new ToolStripMenuItem("Albums Plugin");
            InitialiseBase();
            // must set menu item object first
            _ourTab.UseVisualStyleBackColor = true;
            _ourTab.Controls.Add(_ourSettingsControl);
        }

        // Article processing:
        protected override bool SkipIfContains()
        {
            return false;
        }

        protected override void ProcessArticleFinish()
        {
            StubClass();
        }

        protected override bool TemplateFound()
        {
            // Nothing to do here
            return false;
        }

        protected override string WriteTemplateHeader()
        {
            return "{{" + PluginName + WriteOutParameterToHeader("class") + WriteOutParameterToHeader("importance");
        }

        // User interface:
        protected override void ShowHideOurObjects(bool visible)
        {
            PluginManager.ShowHidePluginTab(_ourTab, visible);
        }

        // XML settings:
        protected internal override void ReadXML(XmlTextReader reader)
        {
            Enabled = PluginManager.XMLReadBoolean(reader, Prefix + "Enabled", Enabled);
            _ourSettingsControl.ReadXML(reader);
        }

        protected internal override void Reset()
        {
            _ourSettingsControl.Reset();
        }

        protected internal override void WriteXML(XmlTextWriter writer)
        {
            writer.WriteAttributeString(Prefix + "Enabled", Enabled.ToString());
            _ourSettingsControl.WriteXML(writer);
        }

        // Misc:
        internal override bool HasReqPhotoParam
        {
            get { return false; }
        }

        internal override void ReqPhoto()
        {
        }
    }
}