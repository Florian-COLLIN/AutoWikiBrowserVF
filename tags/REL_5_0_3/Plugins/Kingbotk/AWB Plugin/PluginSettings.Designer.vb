Namespace AutoWikiBrowser.Plugins.Kingbotk.Components
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
   Partial Class PluginSettingsControl
        Inherits System.Windows.Forms.UserControl

        'UserControl overrides dispose to clean up the component list.
        <System.Diagnostics.DebuggerNonUserCode()> _
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerStepThrough()> _
        Private Sub InitializeComponent()
        	Me.components = New System.ComponentModel.Container
        	Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        	Me.GroupBox2 = New System.Windows.Forms.GroupBox
        	Me.Label9 = New System.Windows.Forms.Label
        	Me.lblRedlink = New System.Windows.Forms.Label
        	Me.lblTagged = New System.Windows.Forms.Label
        	Me.lblSkipped = New System.Windows.Forms.Label
        	Me.lblNoChange = New System.Windows.Forms.Label
        	Me.lblBadTag = New System.Windows.Forms.Label
        	Me.lblNamespace = New System.Windows.Forms.Label
        	Me.Label5 = New System.Windows.Forms.Label
        	Me.Label4 = New System.Windows.Forms.Label
        	Me.Label3 = New System.Windows.Forms.Label
        	Me.Label2 = New System.Windows.Forms.Label
        	Me.Label1 = New System.Windows.Forms.Label
        	Me.ManuallyAssessCheckBox = New System.Windows.Forms.CheckBox
        	Me.CleanupCheckBox = New System.Windows.Forms.CheckBox
        	Me.Label7 = New System.Windows.Forms.Label
        	Me.SkipNoChangesCheckBox = New System.Windows.Forms.CheckBox
        	Me.SkipBadTagsCheckBox = New System.Windows.Forms.CheckBox
        	Me.lblAWBNudges = New System.Windows.Forms.Label
        	Me.ResetTimerButton = New System.Windows.Forms.Button
        	Me.ETALabel = New System.Windows.Forms.Label
        	Me.Led1 = New WikiFunctions.Controls.LED
        	Me.OpenBadInBrowserCheckBox = New System.Windows.Forms.CheckBox
        	Me.GroupBox4 = New System.Windows.Forms.GroupBox
        	Me.PluginMenuStrip = New System.Windows.Forms.MenuStrip
        	Me.PluginToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.SetAWBToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.MenuAbout = New System.Windows.Forms.ToolStripMenuItem
        	Me.MenuHelp = New System.Windows.Forms.ToolStripMenuItem
        	Me.TextInsertContextMenuStrip = New System.Windows.Forms.ContextMenuStrip(Me.components)
        	Me.UniversalToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.ClassToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.StubClassMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.StartClassMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.BClassMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.GAClassMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.AClassMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.FAClassMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.ToolStripSeparator4 = New System.Windows.Forms.ToolStripSeparator
        	Me.NeededClassMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.CatClassMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.DabClassMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.TemplateClassMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.NAClassMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.ImportanceToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.LowImportanceMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.MidImportanceMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.HighImportanceMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.TopImportanceMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.ToolStripSeparator5 = New System.Windows.Forms.ToolStripSeparator
        	Me.NAImportanceMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.PriorityToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.LowPriorityMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.MidPriorityMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.HighPriorityMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.TopPriorityMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.ToolStripSeparator6 = New System.Windows.Forms.ToolStripSeparator
        	Me.NAPriorityMenuItem = New System.Windows.Forms.ToolStripMenuItem
        	Me.BotTimer = New System.Windows.Forms.Timer(Me.components)
        	Me.TimerStats1 = New AutoWikiBrowser.Plugins.Kingbotk.Components.TimerStats
        	Me.GroupBox2.SuspendLayout
        	Me.GroupBox4.SuspendLayout
        	Me.PluginMenuStrip.SuspendLayout
        	Me.TextInsertContextMenuStrip.SuspendLayout
        	Me.SuspendLayout
        	'
        	'GroupBox2
        	'
        	Me.GroupBox2.Controls.Add(Me.Label9)
        	Me.GroupBox2.Controls.Add(Me.lblRedlink)
        	Me.GroupBox2.Controls.Add(Me.lblTagged)
        	Me.GroupBox2.Controls.Add(Me.lblSkipped)
        	Me.GroupBox2.Controls.Add(Me.lblNoChange)
        	Me.GroupBox2.Controls.Add(Me.lblBadTag)
        	Me.GroupBox2.Controls.Add(Me.lblNamespace)
        	Me.GroupBox2.Controls.Add(Me.Label5)
        	Me.GroupBox2.Controls.Add(Me.Label4)
        	Me.GroupBox2.Controls.Add(Me.Label3)
        	Me.GroupBox2.Controls.Add(Me.Label2)
        	Me.GroupBox2.Controls.Add(Me.Label1)
        	Me.GroupBox2.Location = New System.Drawing.Point(3, 3)
        	Me.GroupBox2.Name = "GroupBox2"
        	Me.GroupBox2.Size = New System.Drawing.Size(135, 128)
        	Me.GroupBox2.TabIndex = 36
        	Me.GroupBox2.TabStop = false
        	Me.GroupBox2.Text = "Statistics"
        	Me.ToolTip1.SetToolTip(Me.GroupBox2, "Lies, damned lies and statistics")
        	'
        	'Label9
        	'
        	Me.Label9.AutoSize = true
        	Me.Label9.Location = New System.Drawing.Point(3, 111)
        	Me.Label9.Name = "Label9"
        	Me.Label9.Size = New System.Drawing.Size(46, 13)
        	Me.Label9.TabIndex = 45
        	Me.Label9.Text = "Redlink:"
        	Me.ToolTip1.SetToolTip(Me.Label9, "Talk pages skipped because the article was a redlink")
        	'
        	'lblRedlink
        	'
        	Me.lblRedlink.AutoSize = true
        	Me.lblRedlink.Location = New System.Drawing.Point(73, 111)
        	Me.lblRedlink.Name = "lblRedlink"
        	Me.lblRedlink.Size = New System.Drawing.Size(0, 13)
        	Me.lblRedlink.TabIndex = 44
        	Me.ToolTip1.SetToolTip(Me.lblRedlink, "Talk pages skipped because the article was a redlink")
        	'
        	'lblTagged
        	'
        	Me.lblTagged.AutoSize = true
        	Me.lblTagged.Location = New System.Drawing.Point(73, 16)
        	Me.lblTagged.Name = "lblTagged"
        	Me.lblTagged.Size = New System.Drawing.Size(0, 13)
        	Me.lblTagged.TabIndex = 37
        	Me.ToolTip1.SetToolTip(Me.lblTagged, "Number of articles tagged")
        	'
        	'lblSkipped
        	'
        	Me.lblSkipped.AutoSize = true
        	Me.lblSkipped.Location = New System.Drawing.Point(73, 35)
        	Me.lblSkipped.Name = "lblSkipped"
        	Me.lblSkipped.Size = New System.Drawing.Size(0, 13)
        	Me.lblSkipped.TabIndex = 38
        	Me.ToolTip1.SetToolTip(Me.lblSkipped, "Number of articles skipped")
        	'
        	'lblNoChange
        	'
        	Me.lblNoChange.AutoSize = true
        	Me.lblNoChange.Location = New System.Drawing.Point(73, 54)
        	Me.lblNoChange.Name = "lblNoChange"
        	Me.lblNoChange.Size = New System.Drawing.Size(0, 13)
        	Me.lblNoChange.TabIndex = 39
        	Me.ToolTip1.SetToolTip(Me.lblNoChange, "Number of articles skipped because no change was made")
        	'
        	'lblBadTag
        	'
        	Me.lblBadTag.AutoSize = true
        	Me.lblBadTag.Location = New System.Drawing.Point(73, 73)
        	Me.lblBadTag.Name = "lblBadTag"
        	Me.lblBadTag.Size = New System.Drawing.Size(0, 13)
        	Me.lblBadTag.TabIndex = 40
        	Me.ToolTip1.SetToolTip(Me.lblBadTag, "Number of articles skipped because they had an unparseable template")
        	'
        	'lblNamespace
        	'
        	Me.lblNamespace.AutoSize = true
        	Me.lblNamespace.Location = New System.Drawing.Point(73, 92)
        	Me.lblNamespace.Name = "lblNamespace"
        	Me.lblNamespace.Size = New System.Drawing.Size(0, 13)
        	Me.lblNamespace.TabIndex = 41
        	Me.ToolTip1.SetToolTip(Me.lblNamespace, "Number of articles skipped because they were in an incorrect namespace (e.g. we w"& _ 
        	        	"on't tag articles with talk page templates)")
        	'
        	'Label5
        	'
        	Me.Label5.AutoSize = true
        	Me.Label5.Location = New System.Drawing.Point(3, 92)
        	Me.Label5.Name = "Label5"
        	Me.Label5.Size = New System.Drawing.Size(67, 13)
        	Me.Label5.TabIndex = 4
        	Me.Label5.Text = "Namespace:"
        	Me.ToolTip1.SetToolTip(Me.Label5, "Number of articles skipped because they were in an incorrect namespace (e.g. we w"& _ 
        	        	"on't tag articles with talk page templates)")
        	'
        	'Label4
        	'
        	Me.Label4.AutoSize = true
        	Me.Label4.Location = New System.Drawing.Point(3, 73)
        	Me.Label4.Name = "Label4"
        	Me.Label4.Size = New System.Drawing.Size(51, 13)
        	Me.Label4.TabIndex = 3
        	Me.Label4.Text = "Bad Tag:"
        	Me.ToolTip1.SetToolTip(Me.Label4, "Number of articles skipped because they had an unparseable template")
        	'
        	'Label3
        	'
        	Me.Label3.AutoSize = true
        	Me.Label3.Location = New System.Drawing.Point(3, 54)
        	Me.Label3.Name = "Label3"
        	Me.Label3.Size = New System.Drawing.Size(64, 13)
        	Me.Label3.TabIndex = 2
        	Me.Label3.Text = "No Change:"
        	Me.ToolTip1.SetToolTip(Me.Label3, "Number of articles skipped because no change was made")
        	'
        	'Label2
        	'
        	Me.Label2.AutoSize = true
        	Me.Label2.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0,Byte))
        	Me.Label2.Location = New System.Drawing.Point(3, 35)
        	Me.Label2.Name = "Label2"
        	Me.Label2.Size = New System.Drawing.Size(57, 13)
        	Me.Label2.TabIndex = 1
        	Me.Label2.Text = "Skipped:"
        	Me.ToolTip1.SetToolTip(Me.Label2, "Number of articles skipped by the plugin (not  necessarily by AWB too)")
        	'
        	'Label1
        	'
        	Me.Label1.AutoSize = true
        	Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0,Byte))
        	Me.Label1.Location = New System.Drawing.Point(3, 16)
        	Me.Label1.Name = "Label1"
        	Me.Label1.Size = New System.Drawing.Size(54, 13)
        	Me.Label1.TabIndex = 0
        	Me.Label1.Text = "Tagged:"
        	Me.ToolTip1.SetToolTip(Me.Label1, "Number of articles tagged")
        	'
        	'ManuallyAssessCheckBox
        	'
        	Me.ManuallyAssessCheckBox.AutoSize = true
        	Me.ManuallyAssessCheckBox.Location = New System.Drawing.Point(3, 14)
        	Me.ManuallyAssessCheckBox.Name = "ManuallyAssessCheckBox"
        	Me.ManuallyAssessCheckBox.Size = New System.Drawing.Size(59, 17)
        	Me.ManuallyAssessCheckBox.TabIndex = 38
        	Me.ManuallyAssessCheckBox.Text = "Assess"
        	Me.ToolTip1.SetToolTip(Me.ManuallyAssessCheckBox, "Assess articles by loading an article list and having the plugin load the talk pa"& _ 
        	        	"ge after the article has been reviewed")
        	Me.ManuallyAssessCheckBox.UseVisualStyleBackColor = true
        	'
        	'CleanupCheckBox
        	'
        	Me.CleanupCheckBox.AutoSize = true
        	Me.CleanupCheckBox.Enabled = false
        	Me.CleanupCheckBox.Location = New System.Drawing.Point(59, 14)
        	Me.CleanupCheckBox.Name = "CleanupCheckBox"
        	Me.CleanupCheckBox.Size = New System.Drawing.Size(70, 17)
        	Me.CleanupCheckBox.TabIndex = 39
        	Me.CleanupCheckBox.Text = "Clean Up"
        	Me.ToolTip1.SetToolTip(Me.CleanupCheckBox, "Clean-up articles during the assessment process (Unicodify, auto-tag and general "& _ 
        	        	"fixes)")
        	Me.CleanupCheckBox.UseVisualStyleBackColor = true
        	'
        	'Label7
        	'
        	Me.Label7.AutoSize = true
        	Me.Label7.Location = New System.Drawing.Point(149, 19)
        	Me.Label7.Name = "Label7"
        	Me.Label7.Size = New System.Drawing.Size(31, 13)
        	Me.Label7.TabIndex = 45
        	Me.Label7.Text = "Skip:"
        	Me.ToolTip1.SetToolTip(Me.Label7, "Yes, the plugin has skip options too, sorry.")
        	'
        	'SkipNoChangesCheckBox
        	'
        	Me.SkipNoChangesCheckBox.AutoSize = true
        	Me.SkipNoChangesCheckBox.Location = New System.Drawing.Point(181, 19)
        	Me.SkipNoChangesCheckBox.Name = "SkipNoChangesCheckBox"
        	Me.SkipNoChangesCheckBox.Size = New System.Drawing.Size(46, 17)
        	Me.SkipNoChangesCheckBox.TabIndex = 46
        	Me.SkipNoChangesCheckBox.Text = "N/C"
        	Me.ToolTip1.SetToolTip(Me.SkipNoChangesCheckBox, "Skip the talk page if the plugin doesn't make a change (suggest YES for bots, NO "& _ 
        	        	"for manual editing)")
        	Me.SkipNoChangesCheckBox.UseVisualStyleBackColor = true
        	'
        	'SkipBadTagsCheckBox
        	'
        	Me.SkipBadTagsCheckBox.AutoSize = true
        	Me.SkipBadTagsCheckBox.Location = New System.Drawing.Point(228, 19)
        	Me.SkipBadTagsCheckBox.Name = "SkipBadTagsCheckBox"
        	Me.SkipBadTagsCheckBox.Size = New System.Drawing.Size(45, 17)
        	Me.SkipBadTagsCheckBox.TabIndex = 47
        	Me.SkipBadTagsCheckBox.Text = "Bad"
        	Me.ToolTip1.SetToolTip(Me.SkipBadTagsCheckBox, "Skip the talk page if the existing template instance is bad  (suggest YES for bot"& _ 
        	        	"s, NO for manual editing)")
        	Me.SkipBadTagsCheckBox.UseVisualStyleBackColor = true
        	'
        	'lblAWBNudges
        	'
        	Me.lblAWBNudges.AutoSize = true
        	Me.lblAWBNudges.Location = New System.Drawing.Point(67, 146)
        	Me.lblAWBNudges.Name = "lblAWBNudges"
        	Me.lblAWBNudges.Size = New System.Drawing.Size(56, 13)
        	Me.lblAWBNudges.TabIndex = 48
        	Me.lblAWBNudges.Text = "Nudges: 0"
        	Me.ToolTip1.SetToolTip(Me.lblAWBNudges, "Number of times AWB got nudged")
        	Me.lblAWBNudges.Visible = false
        	'
        	'ResetTimerButton
        	'
        	Me.ResetTimerButton.Location = New System.Drawing.Point(64, 163)
        	Me.ResetTimerButton.Name = "ResetTimerButton"
        	Me.ResetTimerButton.Size = New System.Drawing.Size(69, 23)
        	Me.ResetTimerButton.TabIndex = 49
        	Me.ResetTimerButton.Text = "Reset"
        	Me.ToolTip1.SetToolTip(Me.ResetTimerButton, "Reset the timer")
        	Me.ResetTimerButton.UseVisualStyleBackColor = true
        	'
        	'ETALabel
        	'
        	Me.ETALabel.AutoSize = true
        	Me.ETALabel.Location = New System.Drawing.Point(141, 168)
        	Me.ETALabel.Name = "ETALabel"
        	Me.ETALabel.Size = New System.Drawing.Size(34, 13)
        	Me.ETALabel.TabIndex = 50
        	Me.ETALabel.Text = "ETC: "
        	Me.ToolTip1.SetToolTip(Me.ETALabel, "Estimated time of completion")
        	Me.ETALabel.Visible = false
        	'
        	'Led1
        	'
        	Me.Led1.Colour = WikiFunctions.Controls.Colour.Red
        	Me.Led1.Location = New System.Drawing.Point(7, 207)
        	Me.Led1.Name = "Led1"
        	Me.Led1.Size = New System.Drawing.Size(20, 20)
        	Me.Led1.TabIndex = 43
        	Me.ToolTip1.SetToolTip(Me.Led1, "Green when the plugin is processing article text")
        	'
        	'OpenBadInBrowserCheckBox
        	'
        	Me.OpenBadInBrowserCheckBox.AutoSize = true
        	Me.OpenBadInBrowserCheckBox.Location = New System.Drawing.Point(163, 45)
        	Me.OpenBadInBrowserCheckBox.Name = "OpenBadInBrowserCheckBox"
        	Me.OpenBadInBrowserCheckBox.Size = New System.Drawing.Size(105, 30)
        	Me.OpenBadInBrowserCheckBox.TabIndex = 52
        	Me.OpenBadInBrowserCheckBox.Text = "Open bad pages"&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10)&"in browser"
        	Me.ToolTip1.SetToolTip(Me.OpenBadInBrowserCheckBox, "Open in the web browser pages skipped because they have bad tags")
        	Me.OpenBadInBrowserCheckBox.UseVisualStyleBackColor = true
        	Me.OpenBadInBrowserCheckBox.Visible = false
        	'
        	'GroupBox4
        	'
        	Me.GroupBox4.Controls.Add(Me.CleanupCheckBox)
        	Me.GroupBox4.Controls.Add(Me.ManuallyAssessCheckBox)
        	Me.GroupBox4.Location = New System.Drawing.Point(144, 81)
        	Me.GroupBox4.Name = "GroupBox4"
        	Me.GroupBox4.Size = New System.Drawing.Size(132, 35)
        	Me.GroupBox4.TabIndex = 41
        	Me.GroupBox4.TabStop = false
        	Me.GroupBox4.Text = "Wikipedia Assessments"
        	'
        	'PluginMenuStrip
        	'
        	Me.PluginMenuStrip.Dock = System.Windows.Forms.DockStyle.None
        	Me.PluginMenuStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.PluginToolStripMenuItem, Me.MenuAbout, Me.MenuHelp})
        	Me.PluginMenuStrip.Location = New System.Drawing.Point(0, 0)
        	Me.PluginMenuStrip.Name = "PluginMenuStrip"
        	Me.PluginMenuStrip.Size = New System.Drawing.Size(392, 24)
        	Me.PluginMenuStrip.TabIndex = 42
        	Me.PluginMenuStrip.Text = "MenuStrip1"
        	Me.PluginMenuStrip.Visible = false
        	'
        	'PluginToolStripMenuItem
        	'
        	Me.PluginToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.SetAWBToolStripMenuItem})
        	Me.PluginToolStripMenuItem.Name = "PluginToolStripMenuItem"
        	Me.PluginToolStripMenuItem.Size = New System.Drawing.Size(91, 20)
        	Me.PluginToolStripMenuItem.Text = "Kingbotk Plugin"
        	'
        	'SetAWBToolStripMenuItem
        	'
        	Me.SetAWBToolStripMenuItem.Name = "SetAWBToolStripMenuItem"
        	Me.SetAWBToolStripMenuItem.Size = New System.Drawing.Size(127, 22)
        	Me.SetAWBToolStripMenuItem.Text = "Set AWB"
        	Me.SetAWBToolStripMenuItem.ToolTipText = "Reset AWB to default values suitable for use with the plugin"
        	'
        	'MenuAbout
        	'
        	Me.MenuAbout.Name = "MenuAbout"
        	Me.MenuAbout.Size = New System.Drawing.Size(142, 20)
        	Me.MenuAbout.Text = "About the Kingbotk plugin"
        	'
        	'MenuHelp
        	'
        	Me.MenuHelp.Name = "MenuHelp"
        	Me.MenuHelp.Size = New System.Drawing.Size(151, 20)
        	Me.MenuHelp.Text = "Help for the Kingbotk plugin"
        	'
        	'TextInsertContextMenuStrip
        	'
        	Me.TextInsertContextMenuStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.UniversalToolStripMenuItem})
        	Me.TextInsertContextMenuStrip.Name = "EditBoxContextMenuStrip"
        	Me.TextInsertContextMenuStrip.Size = New System.Drawing.Size(130, 26)
        	'
        	'UniversalToolStripMenuItem
        	'
        	Me.UniversalToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ClassToolStripMenuItem, Me.ImportanceToolStripMenuItem, Me.PriorityToolStripMenuItem})
        	Me.UniversalToolStripMenuItem.Name = "UniversalToolStripMenuItem"
        	Me.UniversalToolStripMenuItem.Size = New System.Drawing.Size(129, 22)
        	Me.UniversalToolStripMenuItem.Text = "Universal"
        	'
        	'ClassToolStripMenuItem
        	'
        	Me.ClassToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.StubClassMenuItem, Me.StartClassMenuItem, Me.BClassMenuItem, Me.GAClassMenuItem, Me.AClassMenuItem, Me.FAClassMenuItem, Me.ToolStripSeparator4, Me.NeededClassMenuItem, Me.CatClassMenuItem, Me.DabClassMenuItem, Me.TemplateClassMenuItem, Me.NAClassMenuItem})
        	Me.ClassToolStripMenuItem.Name = "ClassToolStripMenuItem"
        	Me.ClassToolStripMenuItem.Size = New System.Drawing.Size(140, 22)
        	Me.ClassToolStripMenuItem.Text = "Class"
        	'
        	'StubClassMenuItem
        	'
        	Me.StubClassMenuItem.Name = "StubClassMenuItem"
        	Me.StubClassMenuItem.Size = New System.Drawing.Size(184, 22)
        	Me.StubClassMenuItem.Text = "Stub"
        	'
        	'StartClassMenuItem
        	'
        	Me.StartClassMenuItem.Name = "StartClassMenuItem"
        	Me.StartClassMenuItem.Size = New System.Drawing.Size(184, 22)
        	Me.StartClassMenuItem.Text = "Start"
        	'
        	'BClassMenuItem
        	'
        	Me.BClassMenuItem.Name = "BClassMenuItem"
        	Me.BClassMenuItem.Size = New System.Drawing.Size(184, 22)
        	Me.BClassMenuItem.Text = "B"
        	'
        	'GAClassMenuItem
        	'
        	Me.GAClassMenuItem.Name = "GAClassMenuItem"
        	Me.GAClassMenuItem.Size = New System.Drawing.Size(184, 22)
        	Me.GAClassMenuItem.Text = "GA"
        	'
        	'AClassMenuItem
        	'
        	Me.AClassMenuItem.Name = "AClassMenuItem"
        	Me.AClassMenuItem.Size = New System.Drawing.Size(184, 22)
        	Me.AClassMenuItem.Text = "A"
        	'
        	'FAClassMenuItem
        	'
        	Me.FAClassMenuItem.Name = "FAClassMenuItem"
        	Me.FAClassMenuItem.Size = New System.Drawing.Size(184, 22)
        	Me.FAClassMenuItem.Text = "FA"
        	'
        	'ToolStripSeparator4
        	'
        	Me.ToolStripSeparator4.Name = "ToolStripSeparator4"
        	Me.ToolStripSeparator4.Size = New System.Drawing.Size(181, 6)
        	'
        	'NeededClassMenuItem
        	'
        	Me.NeededClassMenuItem.Name = "NeededClassMenuItem"
        	Me.NeededClassMenuItem.Size = New System.Drawing.Size(184, 22)
        	Me.NeededClassMenuItem.Text = "Article needed"
        	'
        	'CatClassMenuItem
        	'
        	Me.CatClassMenuItem.Name = "CatClassMenuItem"
        	Me.CatClassMenuItem.Size = New System.Drawing.Size(184, 22)
        	Me.CatClassMenuItem.Text = "Category"
        	'
        	'DabClassMenuItem
        	'
        	Me.DabClassMenuItem.Name = "DabClassMenuItem"
        	Me.DabClassMenuItem.Size = New System.Drawing.Size(184, 22)
        	Me.DabClassMenuItem.Text = "Disambiguation page"
        	'
        	'TemplateClassMenuItem
        	'
        	Me.TemplateClassMenuItem.Name = "TemplateClassMenuItem"
        	Me.TemplateClassMenuItem.Size = New System.Drawing.Size(184, 22)
        	Me.TemplateClassMenuItem.Text = "Template"
        	'
        	'NAClassMenuItem
        	'
        	Me.NAClassMenuItem.Name = "NAClassMenuItem"
        	Me.NAClassMenuItem.Size = New System.Drawing.Size(184, 22)
        	Me.NAClassMenuItem.Text = "Not applicable"
        	'
        	'ImportanceToolStripMenuItem
        	'
        	Me.ImportanceToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.LowImportanceMenuItem, Me.MidImportanceMenuItem, Me.HighImportanceMenuItem, Me.TopImportanceMenuItem, Me.ToolStripSeparator5, Me.NAImportanceMenuItem})
        	Me.ImportanceToolStripMenuItem.Name = "ImportanceToolStripMenuItem"
        	Me.ImportanceToolStripMenuItem.Size = New System.Drawing.Size(140, 22)
        	Me.ImportanceToolStripMenuItem.Text = "Importance"
        	'
        	'LowImportanceMenuItem
        	'
        	Me.LowImportanceMenuItem.Name = "LowImportanceMenuItem"
        	Me.LowImportanceMenuItem.Size = New System.Drawing.Size(152, 22)
        	Me.LowImportanceMenuItem.Text = "Low"
        	'
        	'MidImportanceMenuItem
        	'
        	Me.MidImportanceMenuItem.Name = "MidImportanceMenuItem"
        	Me.MidImportanceMenuItem.Size = New System.Drawing.Size(152, 22)
        	Me.MidImportanceMenuItem.Text = "Mid"
        	'
        	'HighImportanceMenuItem
        	'
        	Me.HighImportanceMenuItem.Name = "HighImportanceMenuItem"
        	Me.HighImportanceMenuItem.Size = New System.Drawing.Size(152, 22)
        	Me.HighImportanceMenuItem.Text = "High"
        	'
        	'TopImportanceMenuItem
        	'
        	Me.TopImportanceMenuItem.Name = "TopImportanceMenuItem"
        	Me.TopImportanceMenuItem.Size = New System.Drawing.Size(152, 22)
        	Me.TopImportanceMenuItem.Text = "Top"
        	'
        	'ToolStripSeparator5
        	'
        	Me.ToolStripSeparator5.Name = "ToolStripSeparator5"
        	Me.ToolStripSeparator5.Size = New System.Drawing.Size(149, 6)
        	'
        	'NAImportanceMenuItem
        	'
        	Me.NAImportanceMenuItem.Name = "NAImportanceMenuItem"
        	Me.NAImportanceMenuItem.Size = New System.Drawing.Size(152, 22)
        	Me.NAImportanceMenuItem.Text = "Not applicable"
        	'
        	'PriorityToolStripMenuItem
        	'
        	Me.PriorityToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.LowPriorityMenuItem, Me.MidPriorityMenuItem, Me.HighPriorityMenuItem, Me.TopPriorityMenuItem, Me.ToolStripSeparator6, Me.NAPriorityMenuItem})
        	Me.PriorityToolStripMenuItem.Name = "PriorityToolStripMenuItem"
        	Me.PriorityToolStripMenuItem.Size = New System.Drawing.Size(140, 22)
        	Me.PriorityToolStripMenuItem.Text = "Priority"
        	'
        	'LowPriorityMenuItem
        	'
        	Me.LowPriorityMenuItem.Name = "LowPriorityMenuItem"
        	Me.LowPriorityMenuItem.Size = New System.Drawing.Size(152, 22)
        	Me.LowPriorityMenuItem.Text = "Low"
        	'
        	'MidPriorityMenuItem
        	'
        	Me.MidPriorityMenuItem.Name = "MidPriorityMenuItem"
        	Me.MidPriorityMenuItem.Size = New System.Drawing.Size(152, 22)
        	Me.MidPriorityMenuItem.Text = "Mid"
        	'
        	'HighPriorityMenuItem
        	'
        	Me.HighPriorityMenuItem.Name = "HighPriorityMenuItem"
        	Me.HighPriorityMenuItem.Size = New System.Drawing.Size(152, 22)
        	Me.HighPriorityMenuItem.Text = "High"
        	'
        	'TopPriorityMenuItem
        	'
        	Me.TopPriorityMenuItem.Name = "TopPriorityMenuItem"
        	Me.TopPriorityMenuItem.Size = New System.Drawing.Size(152, 22)
        	Me.TopPriorityMenuItem.Text = "Top"
        	'
        	'ToolStripSeparator6
        	'
        	Me.ToolStripSeparator6.Name = "ToolStripSeparator6"
        	Me.ToolStripSeparator6.Size = New System.Drawing.Size(149, 6)
        	'
        	'NAPriorityMenuItem
        	'
        	Me.NAPriorityMenuItem.Name = "NAPriorityMenuItem"
        	Me.NAPriorityMenuItem.Size = New System.Drawing.Size(152, 22)
        	Me.NAPriorityMenuItem.Text = "Not applicable"
        	'
        	'BotTimer
        	'
        	Me.BotTimer.Interval = 600000
        	'
        	'TimerStats1
        	'
        	Me.TimerStats1.Location = New System.Drawing.Point(3, 139)
        	Me.TimerStats1.MaximumSize = New System.Drawing.Size(63, 70)
        	Me.TimerStats1.Name = "TimerStats1"
        	Me.TimerStats1.Size = New System.Drawing.Size(61, 68)
        	Me.TimerStats1.TabIndex = 44
        	Me.TimerStats1.Visible = false
        	'
        	'PluginSettingsControl
        	'
        	Me.AutoScaleDimensions = New System.Drawing.SizeF(6!, 13!)
        	Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        	Me.Controls.Add(Me.OpenBadInBrowserCheckBox)
        	Me.Controls.Add(Me.ResetTimerButton)
        	Me.Controls.Add(Me.ETALabel)
        	Me.Controls.Add(Me.GroupBox2)
        	Me.Controls.Add(Me.lblAWBNudges)
        	Me.Controls.Add(Me.GroupBox4)
        	Me.Controls.Add(Me.TimerStats1)
        	Me.Controls.Add(Me.SkipNoChangesCheckBox)
        	Me.Controls.Add(Me.PluginMenuStrip)
        	Me.Controls.Add(Me.Led1)
        	Me.Controls.Add(Me.SkipBadTagsCheckBox)
        	Me.Controls.Add(Me.Label7)
        	Me.Name = "PluginSettingsControl"
        	Me.Size = New System.Drawing.Size(276, 349)
        	Me.GroupBox2.ResumeLayout(false)
        	Me.GroupBox2.PerformLayout
        	Me.GroupBox4.ResumeLayout(false)
        	Me.GroupBox4.PerformLayout
        	Me.PluginMenuStrip.ResumeLayout(false)
        	Me.PluginMenuStrip.PerformLayout
        	Me.TextInsertContextMenuStrip.ResumeLayout(false)
        	Me.ResumeLayout(false)
        	Me.PerformLayout
        End Sub
        Friend WithEvents ToolTip1 As System.Windows.Forms.ToolTip
        Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
        Friend WithEvents Label5 As System.Windows.Forms.Label
        Friend WithEvents Label4 As System.Windows.Forms.Label
        Friend WithEvents Label3 As System.Windows.Forms.Label
        Friend WithEvents Label2 As System.Windows.Forms.Label
        Friend WithEvents Label1 As System.Windows.Forms.Label
        Friend WithEvents lblSkipped As System.Windows.Forms.Label
        Friend WithEvents lblNoChange As System.Windows.Forms.Label
        Friend WithEvents lblBadTag As System.Windows.Forms.Label
        Friend WithEvents lblNamespace As System.Windows.Forms.Label
        Friend WithEvents lblTagged As System.Windows.Forms.Label
        Friend WithEvents ManuallyAssessCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents CleanupCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents GroupBox4 As System.Windows.Forms.GroupBox
        Friend WithEvents PluginMenuStrip As System.Windows.Forms.MenuStrip
        Friend WithEvents PluginToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents MenuAbout As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents MenuHelp As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents Led1 As WikiFunctions.Controls.LED
        Friend WithEvents TimerStats1 As AutoWikiBrowser.Plugins.Kingbotk.Components.TimerStats
        Friend WithEvents TextInsertContextMenuStrip As System.Windows.Forms.ContextMenuStrip
        Friend WithEvents Label7 As System.Windows.Forms.Label
        Friend WithEvents SkipNoChangesCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents SkipBadTagsCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents Label9 As System.Windows.Forms.Label
        Friend WithEvents lblRedlink As System.Windows.Forms.Label
        Friend WithEvents lblAWBNudges As System.Windows.Forms.Label
        Friend WithEvents ResetTimerButton As System.Windows.Forms.Button
        Friend WithEvents BotTimer As System.Windows.Forms.Timer
        Friend WithEvents UniversalToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents ClassToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents StubClassMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents StartClassMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents BClassMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents GAClassMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents AClassMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents FAClassMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents ToolStripSeparator4 As System.Windows.Forms.ToolStripSeparator
        Friend WithEvents NeededClassMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents CatClassMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents DabClassMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents TemplateClassMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents NAClassMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents ImportanceToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents LowImportanceMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents MidImportanceMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents HighImportanceMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents TopImportanceMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents ToolStripSeparator5 As System.Windows.Forms.ToolStripSeparator
        Friend WithEvents NAImportanceMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents PriorityToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents LowPriorityMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents MidPriorityMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents HighPriorityMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents TopPriorityMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents ToolStripSeparator6 As System.Windows.Forms.ToolStripSeparator
        Friend WithEvents NAPriorityMenuItem As System.Windows.Forms.ToolStripMenuItem
        Private WithEvents ETALabel As System.Windows.Forms.Label
        Friend WithEvents SetAWBToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents OpenBadInBrowserCheckBox As System.Windows.Forms.CheckBox

    End Class

End Namespace