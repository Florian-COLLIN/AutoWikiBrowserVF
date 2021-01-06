Imports WikiFunctions.API

'Copyright � 2008 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/
'Copyright � 2008 Sam Reed (Reedy) http://www.reedyboy.net/

'This program is free software; you can redistribute it and/or modify it under the terms of Version 2 of the GNU General Public License as published by the Free Software Foundation.

'This program is distributed in the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

'You should have received a copy of the GNU General Public License Version 2 along with this program; if not, write to the Free Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

Namespace AutoWikiBrowser.Plugins.Kingbotk.Components
    Friend NotInheritable Class PluginSettingsControl

        ' XML parm-name constants:
        Private Const conManuallyAssessParm As String = "ManuallyAssess"
        Private Const conCleanupParm As String = "Cleanup"
        Private Const conSkipBadTagsParm As String = "SkipBadTags"
        Private Const conSkipWhenNoChangeParm As String = "SkipWhenNoChange"
        Private Const conAssessmentsAlwaysLeaveACommentParm As String = "AlwaysLeaveAComment"
        Private Const conOpenBadInBrowser As String = "OpenBadInBrowser"

        ' Statistics:
        Friend WithEvents PluginStats As Stats = New Stats
        Private ReadOnly StatLabels As New List(Of Label)

        Friend Sub New()
            ' This call is required by the Windows Form Designer and must come first:
            InitializeComponent()

            With PluginManager.AWBForm
                AddHandler .SkipNoChangesCheckBox.CheckedChanged, AddressOf SkipNoChangesCheckBoxCheckedChanged
            End With

            StatLabels.AddRange(New Label() {lblTagged, lblSkipped, lblNoChange, lblBadTag, lblNamespace, lblRedlink})
        End Sub

        ' AWB processing stopped/started:
        Friend Sub AWBProcessingStart(ByVal editor As AsyncApiEdit)
            For Each lbl As Label In StatLabels
                If lbl.Text = "" Then lbl.Text = "0"
            Next

            TimerStats1.Visible = True
            TimerStats1.Init(editor, ETALabel, PluginStats)

            PluginManager.StatusText.Text = "Started"
        End Sub

        Friend Sub AWBProcessingAborted()
            TimerStats1.StopStats()
        End Sub

        ' Properties:
        Private mAssessmentsAlwaysLeaveAComment As Boolean

        Friend Property ManuallyAssess() As Boolean
            Get
                Return ManuallyAssessCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                ManuallyAssessCheckBox.Checked = value
            End Set
        End Property
        Friend Property Cleanup() As Boolean
            Get
                Return CleanupCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                CleanupCheckBox.Checked = value
            End Set
        End Property
        Friend Property SkipBadTags() As Boolean
            Get
                Return SkipBadTagsCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                SkipBadTagsCheckBox.Checked = value
            End Set
        End Property
        Friend Property SkipWhenNoChange() As Boolean
            Get
                Return SkipNoChangesCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                SkipNoChangesCheckBox.Checked = value
            End Set
        End Property
        Friend Property AssessmentsAlwaysLeaveAComment() As Boolean
            Get
                Return mAssessmentsAlwaysLeaveAComment
            End Get
            Set(ByVal value As Boolean)
                mAssessmentsAlwaysLeaveAComment = value
            End Set
        End Property
        Friend Property OpenBadInBrowser() As Boolean
            Get
                Return OpenBadInBrowserCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                OpenBadInBrowserCheckBox.Checked = value
            End Set
        End Property

        ' XML interface:
        Friend Sub ReadXML(ByVal Reader As XmlTextReader)
            ManuallyAssess = PluginManager.XMLReadBoolean(Reader, conManuallyAssessParm, ManuallyAssess)
            Cleanup = PluginManager.XMLReadBoolean(Reader, conCleanupParm, Cleanup)
            SkipBadTags = PluginManager.XMLReadBoolean(Reader, conSkipBadTagsParm, SkipBadTags)
            SkipWhenNoChange = PluginManager.XMLReadBoolean(Reader, conSkipWhenNoChangeParm, SkipWhenNoChange)
            AssessmentsAlwaysLeaveAComment = PluginManager.XMLReadBoolean(Reader, _
               conAssessmentsAlwaysLeaveACommentParm, AssessmentsAlwaysLeaveAComment)
            OpenBadInBrowser = PluginManager.XMLReadBoolean(Reader, conOpenBadInBrowser, OpenBadInBrowser)
        End Sub
        Friend Sub Reset()
            ManuallyAssess = False
            Cleanup = False
            PluginStats = New Stats
            AssessmentsAlwaysLeaveAComment = False
            OpenBadInBrowser = False
        End Sub
        Friend Sub WriteXML(ByVal Writer As XmlTextWriter)
            Writer.WriteAttributeString(conManuallyAssessParm, ManuallyAssess.ToString)
            Writer.WriteAttributeString(conCleanupParm, Cleanup.ToString)
            Writer.WriteAttributeString(conSkipBadTagsParm, SkipBadTags.ToString)
            Writer.WriteAttributeString(conSkipWhenNoChangeParm, SkipWhenNoChange.ToString)
            Writer.WriteAttributeString(conAssessmentsAlwaysLeaveACommentParm, _
               AssessmentsAlwaysLeaveAComment.ToString)
            Writer.WriteAttributeString(conOpenBadInBrowser, OpenBadInBrowser.ToString)
        End Sub

        ' Event handlers - menu items:
        Private Sub SetAWBToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) _
        Handles SetAWBToolStripMenuItem.Click
            With PluginManager.AWBForm
                .SkipNonExistentPages.Checked = False
                .ApplyGeneralFixesCheckBox.Checked = False
                .AutoTagCheckBox.Checked = False
                If .EditSummaryComboBox.Text = "clean up" Then .EditSummaryComboBox.Text = ""
            End With
        End Sub
        Private Sub MenuAbout_Click(ByVal sender As Object, ByVal e As EventArgs) Handles MenuAbout.Click
            Dim about As New AboutBox()
            about.Show()
        End Sub
        Private Sub MenuHelp_Click(ByVal sender As Object, ByVal e As EventArgs) Handles MenuHelp.Click
            PluginManager.AWBForm.ShowHelpEnWiki("User:Kingbotk/Plugin/User guide")
        End Sub
        ' Event handlers - our controls:
        Private Sub ManuallyAssessCheckBox_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
        Handles ManuallyAssessCheckBox.CheckedChanged
            If ManuallyAssess Then
                PluginManager.AWBForm.BotModeCheckbox.Enabled = False
                PluginManager.AWBForm.BotModeCheckbox.Checked = False
                SkipBadTagsCheckBox.Checked = False
                SkipBadTagsCheckBox.Enabled = False
                SkipNoChangesCheckBox.Checked = False
                SkipNoChangesCheckBox.Enabled = False
            Else
                PluginManager.AWBForm.BotModeCheckbox.Enabled = PluginManager.AWBForm.TheSession.User.IsBot
                SkipBadTagsCheckBox.Enabled = True
                SkipNoChangesCheckBox.Enabled = True
            End If

            CleanupCheckBox.Checked = ManuallyAssess
            CleanupCheckBox.Enabled = ManuallyAssess
        End Sub
        Private Sub ResetTimerButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles ResetTimerButton.Click
            TimerStats1.Reset()
        End Sub
        Private Sub SkipBadTagsCheckBox_CheckedChanged(ByVal sender As Object, _
        ByVal e As EventArgs) Handles SkipBadTagsCheckBox.CheckedChanged
            OpenBadInBrowserCheckBox.Visible = SkipBadTagsCheckBox.Checked
        End Sub
        Private Sub SkipNoChangesCheckBoxCheckedChanged(ByVal sender As Object, ByVal e As EventArgs)
            If (PluginManager.AWBForm.SkipNoChanges <> SkipNoChangesCheckBox.Checked) Then
                SkipNoChangesCheckBox.Checked = PluginManager.AWBForm.SkipNoChanges
            End If
        End Sub
        ' Event handlers - plugin stats:
        Private Sub PluginStats_SkipBadTag(ByVal val As Integer) Handles PluginStats.SkipBadTag
            lblBadTag.Text = val.ToString
        End Sub
        Private Sub PluginStats_SkipMisc(ByVal val As Integer) Handles PluginStats.SkipMisc
            lblSkipped.Text = val.ToString
        End Sub
        Private Sub PluginStats_SkipNamespace(ByVal val As Integer) Handles PluginStats.SkipNamespace
            lblNamespace.Text = val.ToString
        End Sub
        Private Sub PluginStats_SkipNoChange(ByVal val As Integer) Handles PluginStats.SkipNoChange
            lblNoChange.Text = val.ToString
        End Sub
        Private Sub PluginStats_Tagged(ByVal val As Integer) Handles PluginStats.evTagged
            lblTagged.Text = val.ToString
        End Sub
        Private Sub PluginStats_RedLink(ByVal val As Integer) Handles PluginStats.RedLink
            lblRedlink.Text = val.ToString
        End Sub

        ' Statistics:
        Friend NotInheritable Class Stats
            Private mTagged As Integer, mSkipped As Integer, mSkippedNoChange As Integer
            Private mSkippedBadTag As Integer, mSkippedNamespace As Integer, mRedLinks As Integer

            Friend Event SkipMisc(ByVal val As Integer)
            Friend Event SkipNoChange(ByVal val As Integer)
            Friend Event SkipBadTag(ByVal val As Integer)
            Friend Event SkipNamespace(ByVal val As Integer)
            Friend Event evTagged(ByVal val As Integer)
            Friend Event RedLink(ByVal val As Integer)

            Friend Property Tagged() As Integer
                Get
                    Return mTagged
                End Get
                Set(ByVal value As Integer)
                    mTagged = value
                    RaiseEvent evTagged(value)
                End Set
            End Property
            Friend Property Skipped() As Integer
                Get
                    Return mSkipped
                End Get
                Private Set(ByVal value As Integer)
                    mSkipped = value
                    RaiseEvent SkipMisc(value)
                End Set
            End Property
            Friend Sub SkippedMiscellaneousIncrement()
                Skipped += 1
            End Sub
            Friend Sub SkippedMiscellaneousIncrement(ByVal DeincrementTagged As Boolean)
                Skipped += 1
                If DeincrementTagged Then Tagged -= 1
            End Sub
            Friend Property SkippedRedLink() As Integer
                Get
                    Return mRedLinks
                End Get
                Private Set(ByVal value As Integer)
                    mRedLinks = value
                    RaiseEvent RedLink(value)
                End Set
            End Property
            Friend Sub SkippedRedLinkIncrement()
                Skipped += 1
                SkippedRedLink += 1
            End Sub
            Private Property SkippedNoChange() As Integer
                Get
                    Return mSkippedNoChange
                End Get
                Set(ByVal value As Integer)
                    mSkippedNoChange = value
                    RaiseEvent SkipNoChange(value)
                End Set
            End Property
            Friend Sub SkippedNoChangeIncrement()
                SkippedNoChange += 1
                Skipped += 1
            End Sub
            Private Property SkippedBadTag() As Integer
                Get
                    Return mSkippedBadTag
                End Get
                Set(ByVal value As Integer)
                    mSkippedBadTag = value
                    RaiseEvent SkipBadTag(value)
                End Set
            End Property
            Friend Sub SkippedBadTagIncrement()
                SkippedBadTag += 1
                Skipped += 1
            End Sub
            Private Property SkippedNamespace() As Integer
                Get
                    Return mSkippedNamespace
                End Get
                Set(ByVal value As Integer)
                    mSkippedNamespace = value
                    RaiseEvent SkipNamespace(value)
                End Set
            End Property
            Friend Sub SkippedNamespaceIncrement()
                SkippedNamespace += 1
                Skipped += 1
            End Sub

            Friend Sub New()
                Skipped = 0
                SkippedBadTag = 0
                SkippedNamespace = 0
                SkippedNoChange = 0
                Tagged = 0
                SkippedRedLink = 0
            End Sub
        End Class

        Private Sub SkipNoChangesCheckBox_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) Handles SkipNoChangesCheckBox.CheckedChanged
            If (PluginManager.AWBForm.SkipNoChanges <> SkipNoChangesCheckBox.Checked) Then
                PluginManager.AWBForm.SkipNoChanges = SkipNoChangesCheckBox.Checked
            End If
        End Sub
    End Class
End Namespace