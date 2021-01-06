Namespace AutoWikiBrowser.Plugins.Kingbotk.Plugins
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
    Partial Class GenericTemplateSettings
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
            Me.components = New System.ComponentModel.Container()
            Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
            Me.StubClassCheckBox = New System.Windows.Forms.CheckBox()
            Me.AutoStubCheckBox = New System.Windows.Forms.CheckBox()
            Me.TemplateNameTextBox = New System.Windows.Forms.TextBox()
            Me.AlternateNamesTextBox = New System.Windows.Forms.TextBox()
            Me.Label3 = New System.Windows.Forms.Label()
            Me.AutoStubSupportYNCheckBox = New System.Windows.Forms.CheckBox()
            Me.SkipRegexTextBox = New System.Windows.Forms.TextBox()
            Me.SkipRegexCheckBox = New System.Windows.Forms.CheckBox()
            Me.GetRedirectsButton = New System.Windows.Forms.Button()
            Me.HasAlternateNamesCheckBox = New System.Windows.Forms.CheckBox()
            Me.Label2 = New System.Windows.Forms.Label()
            Me.Label1 = New System.Windows.Forms.Label()
            Me.LinkLabel1 = New System.Windows.Forms.LinkLabel()
            Me.GroupBox2 = New System.Windows.Forms.GroupBox()
            Me.TipLabel = New System.Windows.Forms.Label()
            Me.GroupBox3 = New System.Windows.Forms.GroupBox()
            Me.ImportanceCheckedListBox = New System.Windows.Forms.CheckedListBox()
            Me.PropertiesButton = New System.Windows.Forms.Button()
            Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
            Me.GroupBox2.SuspendLayout()
            Me.GroupBox3.SuspendLayout()
            Me.SuspendLayout()
            '
            'StubClassCheckBox
            '
            Me.StubClassCheckBox.AutoSize = True
            Me.StubClassCheckBox.Location = New System.Drawing.Point(85, 19)
            Me.StubClassCheckBox.Name = "StubClassCheckBox"
            Me.StubClassCheckBox.Size = New System.Drawing.Size(76, 17)
            Me.StubClassCheckBox.TabIndex = 5
            Me.StubClassCheckBox.Text = "Stub-Class"
            Me.ToolTip1.SetToolTip(Me.StubClassCheckBox, "class=Stub (not for use in bot mode; use Auto-Stub)")
            Me.StubClassCheckBox.UseVisualStyleBackColor = True
            '
            'AutoStubCheckBox
            '
            Me.AutoStubCheckBox.AutoSize = True
            Me.AutoStubCheckBox.Enabled = False
            Me.AutoStubCheckBox.Location = New System.Drawing.Point(6, 19)
            Me.AutoStubCheckBox.Name = "AutoStubCheckBox"
            Me.AutoStubCheckBox.Size = New System.Drawing.Size(73, 17)
            Me.AutoStubCheckBox.TabIndex = 4
            Me.AutoStubCheckBox.Text = "Auto-Stub"
            Me.ToolTip1.SetToolTip(Me.AutoStubCheckBox, "class=Stub|auto=yes")
            Me.AutoStubCheckBox.UseVisualStyleBackColor = True
            '
            'TemplateNameTextBox
            '
            Me.TemplateNameTextBox.Location = New System.Drawing.Point(109, 9)
            Me.TemplateNameTextBox.Name = "TemplateNameTextBox"
            Me.TemplateNameTextBox.Size = New System.Drawing.Size(90, 20)
            Me.TemplateNameTextBox.TabIndex = 0
            Me.ToolTip1.SetToolTip(Me.TemplateNameTextBox, "The usual (preferred) name of the template. e.g. {{Target}}")
            '
            'AlternateNamesTextBox
            '
            Me.AlternateNamesTextBox.Enabled = False
            Me.AlternateNamesTextBox.Location = New System.Drawing.Point(84, 58)
            Me.AlternateNamesTextBox.Name = "AlternateNamesTextBox"
            Me.AlternateNamesTextBox.Size = New System.Drawing.Size(136, 20)
            Me.AlternateNamesTextBox.TabIndex = 2
            Me.ToolTip1.SetToolTip(Me.AlternateNamesTextBox, "Enter the alternate names of the template. If there is more than one alternate na" & _
            "me seperate them with a vertical bar | and NO SPACES, e.g. WikiProjectBiography|" & _
            "BiographyWikiProject banner")
            '
            'Label3
            '
            Me.Label3.AutoSize = True
            Me.Label3.Location = New System.Drawing.Point(3, 16)
            Me.Label3.Name = "Label3"
            Me.Label3.Size = New System.Drawing.Size(60, 13)
            Me.Label3.TabIndex = 11
            Me.Label3.Text = "Importance"
            Me.ToolTip1.SetToolTip(Me.Label3, "The name of your importance= parameter (importance, priority, or not supported)")
            '
            'AutoStubSupportYNCheckBox
            '
            Me.AutoStubSupportYNCheckBox.AutoSize = True
            Me.AutoStubSupportYNCheckBox.Location = New System.Drawing.Point(168, 35)
            Me.AutoStubSupportYNCheckBox.Name = "AutoStubSupportYNCheckBox"
            Me.AutoStubSupportYNCheckBox.Size = New System.Drawing.Size(73, 17)
            Me.AutoStubSupportYNCheckBox.TabIndex = 15
            Me.AutoStubSupportYNCheckBox.Text = "Auto-Stub"
            Me.ToolTip1.SetToolTip(Me.AutoStubSupportYNCheckBox, "Do you have an auto=yes parameter?")
            Me.AutoStubSupportYNCheckBox.UseVisualStyleBackColor = True
            '
            'SkipRegexTextBox
            '
            Me.SkipRegexTextBox.Enabled = False
            Me.SkipRegexTextBox.Location = New System.Drawing.Point(97, 58)
            Me.SkipRegexTextBox.Name = "SkipRegexTextBox"
            Me.SkipRegexTextBox.Size = New System.Drawing.Size(144, 20)
            Me.SkipRegexTextBox.TabIndex = 9
            Me.ToolTip1.SetToolTip(Me.SkipRegexTextBox, "Advanced. Enter a REGULAR EXPRESSION, and the plugin will skip if the talk page c" & _
            "ontains it.")
            '
            'SkipRegexCheckBox
            '
            Me.SkipRegexCheckBox.AutoSize = True
            Me.SkipRegexCheckBox.Location = New System.Drawing.Point(97, 35)
            Me.SkipRegexCheckBox.Name = "SkipRegexCheckBox"
            Me.SkipRegexCheckBox.Size = New System.Drawing.Size(65, 17)
            Me.SkipRegexCheckBox.TabIndex = 16
            Me.SkipRegexCheckBox.Text = "Skip RE"
            Me.ToolTip1.SetToolTip(Me.SkipRegexCheckBox, "Check this if you want to supply a regular expression to tell the plugin when to " & _
            "skip pages")
            Me.SkipRegexCheckBox.UseVisualStyleBackColor = True
            '
            'GetRedirectsButton
            '
            Me.GetRedirectsButton.Enabled = False
            Me.GetRedirectsButton.Location = New System.Drawing.Point(226, 56)
            Me.GetRedirectsButton.Name = "GetRedirectsButton"
            Me.GetRedirectsButton.Size = New System.Drawing.Size(33, 23)
            Me.GetRedirectsButton.TabIndex = 5
            Me.GetRedirectsButton.Text = "Get"
            Me.GetRedirectsButton.UseVisualStyleBackColor = True
            '
            'HasAlternateNamesCheckBox
            '
            Me.HasAlternateNamesCheckBox.AutoSize = True
            Me.HasAlternateNamesCheckBox.Location = New System.Drawing.Point(30, 35)
            Me.HasAlternateNamesCheckBox.Name = "HasAlternateNamesCheckBox"
            Me.HasAlternateNamesCheckBox.Size = New System.Drawing.Size(217, 17)
            Me.HasAlternateNamesCheckBox.TabIndex = 4
            Me.HasAlternateNamesCheckBox.Text = "Template has alternate names (redirects)"
            Me.HasAlternateNamesCheckBox.UseVisualStyleBackColor = True
            '
            'Label2
            '
            Me.Label2.AutoSize = True
            Me.Label2.Location = New System.Drawing.Point(18, 58)
            Me.Label2.Name = "Label2"
            Me.Label2.Size = New System.Drawing.Size(49, 26)
            Me.Label2.TabIndex = 3
            Me.Label2.Text = "Alternate" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Names"
            '
            'Label1
            '
            Me.Label1.AutoSize = True
            Me.Label1.Location = New System.Drawing.Point(23, 12)
            Me.Label1.Name = "Label1"
            Me.Label1.Size = New System.Drawing.Size(82, 13)
            Me.Label1.TabIndex = 1
            Me.Label1.Text = "Template Name"
            '
            'LinkLabel1
            '
            Me.LinkLabel1.AutoSize = True
            Me.LinkLabel1.Location = New System.Drawing.Point(225, 12)
            Me.LinkLabel1.Name = "LinkLabel1"
            Me.LinkLabel1.Size = New System.Drawing.Size(29, 13)
            Me.LinkLabel1.TabIndex = 2
            Me.LinkLabel1.TabStop = True
            Me.LinkLabel1.Text = "Help"
            '
            'GroupBox2
            '
            Me.GroupBox2.Controls.Add(Me.StubClassCheckBox)
            Me.GroupBox2.Controls.Add(Me.AutoStubCheckBox)
            Me.GroupBox2.Location = New System.Drawing.Point(18, 180)
            Me.GroupBox2.Name = "GroupBox2"
            Me.GroupBox2.Size = New System.Drawing.Size(171, 45)
            Me.GroupBox2.TabIndex = 2
            Me.GroupBox2.TabStop = False
            Me.GroupBox2.Text = "Configuration"
            '
            'TipLabel
            '
            Me.TipLabel.AutoSize = True
            Me.TipLabel.Location = New System.Drawing.Point(15, 228)
            Me.TipLabel.Name = "TipLabel"
            Me.TipLabel.Size = New System.Drawing.Size(255, 39)
            Me.TipLabel.TabIndex = 7
            Me.TipLabel.Text = "Tip: The plugin also adds parameter insertion options" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "to the context menu of the" & _
        " edit box. Just right" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "click inside the edit box to access them."
            Me.TipLabel.Visible = False
            '
            'GroupBox3
            '
            Me.GroupBox3.Controls.Add(Me.SkipRegexCheckBox)
            Me.GroupBox3.Controls.Add(Me.SkipRegexTextBox)
            Me.GroupBox3.Controls.Add(Me.AutoStubSupportYNCheckBox)
            Me.GroupBox3.Controls.Add(Me.Label3)
            Me.GroupBox3.Controls.Add(Me.ImportanceCheckedListBox)
            Me.GroupBox3.Location = New System.Drawing.Point(12, 87)
            Me.GroupBox3.Name = "GroupBox3"
            Me.GroupBox3.Size = New System.Drawing.Size(252, 87)
            Me.GroupBox3.TabIndex = 8
            Me.GroupBox3.TabStop = False
            Me.GroupBox3.Text = "Template Properties"
            '
            'ImportanceCheckedListBox
            '
            Me.ImportanceCheckedListBox.CheckOnClick = True
            Me.ImportanceCheckedListBox.FormattingEnabled = True
            Me.ImportanceCheckedListBox.Items.AddRange(New Object() {"Importance", "Priority", "None"})
            Me.ImportanceCheckedListBox.Location = New System.Drawing.Point(6, 29)
            Me.ImportanceCheckedListBox.Name = "ImportanceCheckedListBox"
            Me.ImportanceCheckedListBox.Size = New System.Drawing.Size(86, 49)
            Me.ImportanceCheckedListBox.TabIndex = 9
            '
            'PropertiesButton
            '
            Me.PropertiesButton.Location = New System.Drawing.Point(195, 193)
            Me.PropertiesButton.Name = "PropertiesButton"
            Me.PropertiesButton.Size = New System.Drawing.Size(75, 23)
            Me.PropertiesButton.TabIndex = 17
            Me.PropertiesButton.Text = "Properties"
            Me.PropertiesButton.UseVisualStyleBackColor = True
            '
            'GenericTemplateSettings
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.GetRedirectsButton)
            Me.Controls.Add(Me.HasAlternateNamesCheckBox)
            Me.Controls.Add(Me.PropertiesButton)
            Me.Controls.Add(Me.Label2)
            Me.Controls.Add(Me.GroupBox3)
            Me.Controls.Add(Me.AlternateNamesTextBox)
            Me.Controls.Add(Me.TipLabel)
            Me.Controls.Add(Me.Label1)
            Me.Controls.Add(Me.GroupBox2)
            Me.Controls.Add(Me.LinkLabel1)
            Me.Controls.Add(Me.TemplateNameTextBox)
            Me.MaximumSize = New System.Drawing.Size(276, 349)
            Me.MinimumSize = New System.Drawing.Size(276, 349)
            Me.Name = "GenericTemplateSettings"
            Me.Size = New System.Drawing.Size(276, 349)
            Me.GroupBox2.ResumeLayout(False)
            Me.GroupBox2.PerformLayout()
            Me.GroupBox3.ResumeLayout(False)
            Me.GroupBox3.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
        Friend WithEvents ToolTip1 As System.Windows.Forms.ToolTip
        Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
        Friend WithEvents StubClassCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents AutoStubCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents TipLabel As System.Windows.Forms.Label
        Friend WithEvents Label1 As System.Windows.Forms.Label
        Friend WithEvents TemplateNameTextBox As System.Windows.Forms.TextBox
        Friend WithEvents LinkLabel1 As System.Windows.Forms.LinkLabel
        Friend WithEvents Label2 As System.Windows.Forms.Label
        Friend WithEvents AlternateNamesTextBox As System.Windows.Forms.TextBox
        Friend WithEvents HasAlternateNamesCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents GroupBox3 As System.Windows.Forms.GroupBox
        Friend WithEvents Label3 As System.Windows.Forms.Label
        Friend WithEvents ImportanceCheckedListBox As System.Windows.Forms.CheckedListBox
        Friend WithEvents AutoStubSupportYNCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents SkipRegexTextBox As System.Windows.Forms.TextBox
        Friend WithEvents SkipRegexCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents PropertiesButton As System.Windows.Forms.Button
        Friend WithEvents Timer1 As System.Windows.Forms.Timer
        Friend WithEvents GetRedirectsButton As System.Windows.Forms.Button

    End Class
End Namespace