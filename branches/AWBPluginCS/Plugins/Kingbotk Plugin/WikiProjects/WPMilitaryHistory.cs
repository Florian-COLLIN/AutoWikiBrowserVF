using AutoWikiBrowser.Plugins.Kingbotk;
using AutoWikiBrowser.Plugins.Kingbotk.Plugins;

using System.Windows.Forms;
using System.Xml;

internal sealed class WPMilitaryHistory : PluginBase
{

	private const string PluginName = "MilHist";

	private const string TemplateName = "WPMILHIST";
	// Initialisation:
	internal WPMilitaryHistory() : base("WikiProject Military History")
	{
		// Specify alternate names only

		OurSettingsControl = new GenericWithWorkgroups(TemplateName, PluginName, false, @params);
	}

	// Settings:
	private readonly TabPage OurTab = new TabPage(PluginName);

	private readonly GenericWithWorkgroups OurSettingsControl;
	protected internal override string PluginShortName {
		get { return "MilitaryHistory"; }
	}

	protected override string PreferredTemplateName {
		get { return TemplateName; }
	}

	protected override void ImportanceParameter(Importance Importance)
	{
		// WPMILHIST doesn't do importance
	}

	protected internal override IGenericSettings GenericSettings {
		get { return OurSettingsControl; }
	}

	internal override bool HasReqPhotoParam {
		get { return false; }
	}

	internal override void ReqPhoto()
	{
	}

	const string PeriodsAndConflictsGroup = "Periods and Conflicts";
	const string GeneralGroup = "General Task Forces";

	const string NationsGroup = "Nations and Regions";

    private readonly TemplateParameters[] @params =
    {
        new TemplateParameters
        {
            StorageKey = "ACW",
            Group = PeriodsAndConflictsGroup,
            ParamName = "ACW"
        },
        new TemplateParameters
        {
            StorageKey = "ARW",
            Group = PeriodsAndConflictsGroup,
            ParamName = "ARW"
        },
        new TemplateParameters
        {
            StorageKey = "Classic",
            Group = PeriodsAndConflictsGroup,
            ParamName = "Classical"
        },
        new TemplateParameters
        {
            StorageKey = "Crusades",
            Group = PeriodsAndConflictsGroup,
            ParamName = "Crusades"
        },
        new TemplateParameters
        {
            StorageKey = "EarlyModern",
            Group = PeriodsAndConflictsGroup,
            ParamName = "Early-Modern"
        },
        new TemplateParameters
        {
            StorageKey = "Medieval",
            Group = PeriodsAndConflictsGroup,
            ParamName = "Medieval"
        },
        new TemplateParameters
        {
            StorageKey = "Muslim",
            Group = PeriodsAndConflictsGroup,
            ParamName = "Muslim"
        },
        new TemplateParameters
        {
            StorageKey = "Napol",
            Group = PeriodsAndConflictsGroup,
            ParamName = "Napoleonic"
        },
        new TemplateParameters
        {
            StorageKey = "WWI",
            Group = PeriodsAndConflictsGroup,
            ParamName = "WWI"
        },
        new TemplateParameters
        {
            StorageKey = "WWII",
            Group = PeriodsAndConflictsGroup,
            ParamName = "WWII"
        },
        new TemplateParameters
        {
            StorageKey = "Air",
            Group = GeneralGroup,
            ParamName = "Aviation"
        },
        new TemplateParameters
        {
            StorageKey = "Biography",
            Group = GeneralGroup,
            ParamName = "Biography"
        },
        new TemplateParameters
        {
            StorageKey = "Films",
            Group = GeneralGroup,
            ParamName = "Films"
        },
        new TemplateParameters
        {
            StorageKey = "Fortifications",
            Group = GeneralGroup,
            ParamName = "Fortifications"
        },
        new TemplateParameters
        {
            StorageKey = "Historiography",
            Group = GeneralGroup,
            ParamName = "Historiography"
        },
        new TemplateParameters
        {
            StorageKey = "Intel",
            Group = GeneralGroup,
            ParamName = "Intel"
        },
        new TemplateParameters
        {
            StorageKey = "LandVech",
            Group = GeneralGroup,
            ParamName = "Land Vehicles"
        },
        new TemplateParameters
        {
            StorageKey = "Marit",
            Group = GeneralGroup,
            ParamName = "Martime"
        },
        new TemplateParameters
        {
            StorageKey = "Memorial",
            Group = GeneralGroup,
            ParamName = "Memorials"
        },
        new TemplateParameters
        {
            StorageKey = "National",
            Group = GeneralGroup,
            ParamName = "Nationals"
        },
        new TemplateParameters
        {
            StorageKey = "Science",
            Group = GeneralGroup,
            ParamName = "Science"
        },
        new TemplateParameters
        {
            StorageKey = "Tech",
            Group = GeneralGroup,
            ParamName = "Technology"
        },
        new TemplateParameters
        {
            StorageKey = "Weapon",
            Group = GeneralGroup,
            ParamName = "Weaponry"
        },
        new TemplateParameters
        {
            StorageKey = "NTF",
            Group = NationsGroup,
            ParamName = "No Task Force"
        },
        new TemplateParameters
        {
            StorageKey = "African",
            Group = NationsGroup,
            ParamName = "Africa"
        },
        new TemplateParameters
        {
            StorageKey = "Aus",
            Group = NationsGroup,
            ParamName = "Australia"
        },
        new TemplateParameters
        {
            StorageKey = "Balkan",
            Group = NationsGroup,
            ParamName = "Balkan"
        },
        new TemplateParameters
        {
            StorageKey = "Baltic",
            Group = NationsGroup,
            ParamName = "Baltic"
        },
        new TemplateParameters
        {
            StorageKey = "Brit",
            Group = NationsGroup,
            ParamName = "British"
        },
        new TemplateParameters
        {
            StorageKey = "Canuck",
            Group = NationsGroup,
            ParamName = "Canadian"
        },
        new TemplateParameters
        {
            StorageKey = "China",
            Group = NationsGroup,
            ParamName = "Chinese"
        },
        new TemplateParameters
        {
            StorageKey = "Dutch",
            Group = NationsGroup,
            ParamName = "Dutch"
        },
        new TemplateParameters
        {
            StorageKey = "French",
            Group = NationsGroup,
            ParamName = "French"
        },
        new TemplateParameters
        {
            StorageKey = "German",
            Group = NationsGroup,
            ParamName = "German"
        },
        new TemplateParameters
        {
            StorageKey = "India",
            Group = NationsGroup,
            ParamName = "Indian"
        },
        new TemplateParameters
        {
            StorageKey = "Italy",
            Group = NationsGroup,
            ParamName = "Italian"
        },
        new TemplateParameters
        {
            StorageKey = "Japan",
            Group = NationsGroup,
            ParamName = "Japanese"
        },
        new TemplateParameters
        {
            StorageKey = "Korean",
            Group = NationsGroup,
            ParamName = "Korean"
        },
        new TemplateParameters
        {
            StorageKey = "Lebanese",
            Group = NationsGroup,
            ParamName = "Lebanese"
        },
        new TemplateParameters
        {
            StorageKey = "MidEast",
            Group = NationsGroup,
            ParamName = "Middle-Eastern"
        },
        new TemplateParameters
        {
            StorageKey = "NZ",
            Group = NationsGroup,
            ParamName = "New Zealand"
        },
        new TemplateParameters
        {
            StorageKey = "Nordic",
            Group = NationsGroup,
            ParamName = "Nordic"
        },
        new TemplateParameters
        {
            StorageKey = "Ottoman",
            Group = NationsGroup,
            ParamName = "Ottoman"
        },
        new TemplateParameters
        {
            StorageKey = "Poland",
            Group = NationsGroup,
            ParamName = "Polish"
        },
        new TemplateParameters
        {
            StorageKey = "Romanian",
            Group = NationsGroup,
            ParamName = "Romanian"
        },
        new TemplateParameters
        {
            StorageKey = "Russian",
            Group = NationsGroup,
            ParamName = "Russian"
        },
        new TemplateParameters
        {
            StorageKey = "Spanish",
            Group = NationsGroup,
            ParamName = "Spanish"
        },
        new TemplateParameters
        {
            StorageKey = "SAmerican",
            Group = NationsGroup,
            ParamName = "S American"
        },
        new TemplateParameters
        {
            StorageKey = "SEAsian",
            Group = NationsGroup,
            ParamName = "SE Asian"
        },
        new TemplateParameters
        {
            StorageKey = "Taiwanese",
            Group = NationsGroup,
            ParamName = "Taiwanese"
        },
        new TemplateParameters
        {
            StorageKey = "US",
            Group = NationsGroup,
            ParamName = "US"
        }

    };
	protected internal override void Initialise()
	{
		OurMenuItem = new ToolStripMenuItem("Military History Plugin");
		InitialiseBase();
		// must set menu item object first
		OurTab.UseVisualStyleBackColor = true;
		OurTab.Controls.Add(OurSettingsControl);
	}

	// Article processing:
	protected override bool SkipIfContains()
	{
	    return false;
	}

	protected override void ProcessArticleFinish()
	{
		StubClass();
		var _with1 = OurSettingsControl;
		foreach (ListViewItem lvi in _with1.ListView1.Items) {
			if (lvi.Checked) {
				TemplateParameters tp = (TemplateParameters)lvi.Tag;
				AddAndLogNewParamWithAYesValue(tp.ParamName.ToLower().Replace(" ", "-"));
				//Probably needs some reformatting
			}
		}
		if (Template.Parameters.ContainsKey("importance")) {
			Template.Parameters.Remove("importance");
			article.ArticleHasAMajorChange();
		}

		if (Template.Parameters.ContainsKey("auto")) {
			Template.Parameters.Remove("auto");
			article.ArticleHasAMajorChange();
		}
	}

	private const string conMedievalTaskForce = "Medieval-task-force";
	protected override bool TemplateFound()
	{
		const string conMiddleAges = "Middle-Ages-task-force";

		var _with2 = Template;
		if (_with2.Parameters.ContainsKey(conMiddleAges)) {
			if (_with2.Parameters[conMiddleAges].Value.ToLower() == "yes") {
				_with2.NewOrReplaceTemplateParm(conMedievalTaskForce, "yes", article, false, false, false, "", PluginShortName);
				article.DoneReplacement(conMiddleAges, conMedievalTaskForce, true, PluginShortName);
			} else {
				article.EditSummary += "deprecated Middle-Ages-task-force removed";
			}
			_with2.Parameters.Remove(conMiddleAges);
			article.ArticleHasAMinorChange();
		}
	    return false;
	}

	protected override string WriteTemplateHeader()
	{
		return "{{WPMILHIST" + WriteOutParameterToHeader("class");
	}

	//User interface:
	protected override void ShowHideOurObjects(bool Visible)
	{
		PluginManager.ShowHidePluginTab(OurTab, Visible);
	}

	// XML settings:
	protected internal override void ReadXML(XmlTextReader Reader)
	{
		bool blnNewVal = PluginManager.XMLReadBoolean(Reader, PluginName + "Enabled", Enabled);
        // ReSharper disable once RedundantCheckBeforeAssignment
		if (blnNewVal != Enabled) {
			Enabled = blnNewVal;
			// Mustn't set if the same or we get extra tabs
		}

		OurSettingsControl.ReadXML(Reader);
	}

	protected internal override void Reset()
	{
		OurSettingsControl.Reset();
	}

	protected internal override void WriteXML(XmlTextWriter Writer)
	{
		Writer.WriteAttributeString(PluginName + "Enabled", Enabled.ToString());
		OurSettingsControl.WriteXML(Writer);
	}
}
