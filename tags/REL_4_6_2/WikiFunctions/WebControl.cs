/*
Copyright (C) 2007 Martin Richards
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

using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Web;

namespace WikiFunctions.Browser
{
    public delegate void WebControlDel(object sender, EventArgs e);

    public enum ProcessingStage { Load, Diff, Save, Delete, Protect, None }

    /// <summary>
    /// Provides a WebBrowser component adapted and extended for use with Wikis.
    /// </summary>
    public partial class WebControl : WebBrowser
    {
        #region Constructor etc.

        public WebControl()
        {
            InitializeComponent();
            timer.Interval = 1000;
            timer.Enabled = true;

            ScriptErrorsSuppressed = true;
            ProcessStage = ProcessingStage.None;
        }

        private readonly Timer timer = new Timer();

        public static bool Shutdown;

        /// <summary>
        /// Occurs when the edit page has finished loading
        /// </summary>
        public event WebControlDel Loaded;
        /// <summary>
        /// Occurs when the page has finished saving
        /// </summary>
        public event WebControlDel Saved;
        /// <summary>
        /// Occurs when the diff or preview page has finished loading
        /// </summary>
        //public event WebControlDel Diffed;
        /// <summary>
        /// Occurs when the page has finished deleting
        /// </summary>
        public event WebControlDel Deleted;
        /// <summary>
        /// Occurs when the page has finished loading, and it was not a save/load/diff
        /// </summary>
        public event WebControlDel None;
        /// <summary>
        /// Occurs when the page failed to load properly
        /// </summary>
        public event WebControlDel Fault;
        /// <summary>
        /// Occurs when the status changes
        /// </summary>
        public event WebControlDel StatusChanged;
        /// <summary>
        /// Occurs when the Busy state changes
        /// </summary>
        public event WebControlDel BusyChanged;
        #endregion

        #region Properties

        private ProcessingStage Stage = ProcessingStage.None;
        [Browsable(false)]
        public ProcessingStage ProcessStage
        {
            get { return Stage; }
            set { Stage = value; }
        }

        /// <summary>
        /// Returns title of currently loaded page
        /// </summary>
        [Browsable(false)]
        public string ArticleTitle
        {
            get
            {
                string s = DocumentText;
                s = s.Remove(0, s.IndexOf("var wgPageName = \"") + "var wgPageName = \"".Length);
                return HttpUtility.HtmlDecode(s.Substring(0, s.IndexOf("\""))).Replace("_", " ");
            }
        }

        /// <summary>
        /// Gets contents of currently loaded page
        /// </summary>
        /// <returns>HTML text</returns>
        public override string ToString()
        {
            return DocumentText ?? "";
        }

        /// <summary>
        /// Returns revision ID of currently loaded page
        /// </summary>
        [Browsable(false)]
        public int Revid
        {
            get
            {
                int rev;
                Regex r;
                try
                {
                    r = new Regex("&diff=\\d+");
                    Match m = r.Match(Url.ToString());
                    if (m.Success && int.TryParse(m.Value.Remove(0, 6), out rev)) return rev;
                }
                finally
                {
                    r = new Regex("&oldid=\\d+");
                    rev = int.Parse(r.Match(Url.ToString()).Value.Remove(0, 7));
                }
                return rev;
            }
        }
        /// <summary>
        /// Gets a value indicating whether the page can be saved
        /// </summary>
        [Browsable(false)]
        public bool CanSave
        {
            get { return (Document != null && Document.GetElementById("wpSave") != null && !Status.Equals("Saving")); }
        }

        /// <summary>
        /// Gets a value indicating whether the page can be previewed
        /// </summary>
        [Browsable(false)]
        public bool CanPreview
        {
            get { return (Document != null && Document.GetElementById("wpPreview") != null); }
        }

        /// <summary>
        /// Gets a value indicating whether the page can be deleted
        /// </summary>
        [Browsable(false)]
        public bool CanDelete
        {
            get { return (Document != null && Document.GetElementById("wpConfirmB") != null); }
        }

        /// <summary>
        /// Gets a value indicating whether the page can be protected
        /// </summary>
        [Browsable(false)]
        public bool CanProtect
        {
            get { return (Document != null && Document.GetElementById("mw-Protect-submit") != null); }
        }

        /// <summary>
        /// Gets a value indicating whether the user is logged in
        /// </summary>
        public bool GetLogInStatus()
        {
            if (Document == null)
                return false;

            try
            {
                return (UserName.Length > 0);
            }
            catch (Exception ex)
            {
                throw new WebBrowserOperationsException("Error getting log-in status", ex);
            }
        }

        /// <summary>
        /// Gets the user name if logged in
        /// </summary>
        [Browsable(false)]
        public string UserName
        {
            get
            {
                string s = GetScriptingVar("wgUserName");
                return (s == "null") ? "" : s;
            }
        }

        /// <summary>
        /// Login Function for use in AWB Profiles
        /// Allows username and password to be passed and then the user logged in
        /// </summary>
        public void Login(string username, string password)
        {
            LoadLogInPage();

            Wait();

            if (Document == null) //Maybe retry?
                return;

            HtmlElement wpName1 = Document.GetElementById("wpName1");

            if (wpName1 != null)
                wpName1.InnerText = username;

            HtmlElement wpPassword1 = Document.GetElementById("wpPassword1");

            if (wpPassword1 != null)
                wpPassword1.InnerText = password;

            HtmlElement wpRemember = Document.GetElementById("wpRemember");

            if (wpRemember != null)
                wpRemember.SetAttribute("value" , "1");

            HtmlElement wpLoginattempt = Document.GetElementById("wpLoginattempt");

            if (wpLoginattempt != null)
                wpLoginattempt.InvokeMember("click");

            Wait();
        }

        private static readonly Regex NewMessagesRegex = new Regex("\\<div id=\"contentSub\"\\>[^<>]*?\\</div\\>\\s*\\<div class=\"usermessage\"", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets a value indicating whether there is a new message
        /// </summary>
        [Browsable(false)]
        public bool NewMessage
        { get { return NewMessagesRegex.IsMatch(DocumentText); } }

        private static readonly Regex WpTextbox1Regex = new Regex(@"<textarea [^>]*?name=[""']wpTextbox1[""'].*?>", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Gets a value indicating whether the textbox is present
        /// </summary>
        [Browsable(false)]
        public bool HasArticleTextBox
        {
            get { return (EditBoxTag.Length > 0); }
        }

        private string CachedEditBox;

        /// <summary>
        /// Gets the opening tag for textarea that holds main edit box or empty string
        /// </summary>
        public string EditBoxTag
        {
            get
            {
                if (string.IsNullOrEmpty(CachedEditBox))
                    CachedEditBox = WpTextbox1Regex.Match(DocumentText).Value;

                return CachedEditBox;
            }
        }

        /// <summary>
        /// Returns true if the current user is allowed to edit the current page
        /// </summary>
        /// <returns></returns>
        public bool UserAllowedToEdit()
        {
            string restrictions = GetScriptingVar("wgRestrictionEdit");

            bool allowed = true;
            foreach (Match m in Regex.Matches(restrictions, "\"(.*?)\""))
            {
                if (!Variables.User.Groups.Contains(m.Groups[1].Value))
                {
                    allowed = false;
                    break;
                }
            }

            return allowed;
        }

        string strStatus = "";
        /// <summary>
        /// Gets a string indicating the current status
        /// </summary>
        [Browsable(false)]
        public string Status
        {
            get
            { return strStatus; }
            private set
            {
                strStatus = value;

                if (StatusChanged != null)
                    StatusChanged(null, null);
            }
        }

        bool boolBusy;

        /// <summary>
        /// Gets a value indicating whether articles are still being processed
        /// </summary>
        [Browsable(false)]
        public bool Busy
        {
            get { return boolBusy; }
            set
            {
                bool b = boolBusy;
                boolBusy = value;
                if (b != boolBusy && BusyChanged != null)
                    BusyChanged(null, null);
            }
        }

        /// <summary>
        /// Gets a bool indicating if the current page is a diff
        /// </summary>
        [Browsable(false)]
        public bool IsDiff
        {
            get { return (Document != null && Document.Body != null && Document.Body.InnerHtml.Contains("<DIV id=wikiDiff>")); }
        }

        bool boolTalkExists = true;
        /// <summary>
        /// Gets a value indicating if the associated talk page exists
        /// </summary>
        [Browsable(false)]
        public bool TalkPageExists
        {
            get { return boolTalkExists; }
            private set { boolTalkExists = value; }
        }

        bool boolArticlePageExists = true;
        /// <summary>
        /// Gets a value indicating if the associated article page exists
        /// </summary>
        [Browsable(false)]
        public bool ArticlePageExists
        {
            get { return boolArticlePageExists; }
            private set { boolArticlePageExists = value; }
        }

        #endregion

        #region Methods

        public void Stop2()
        {
            StopTimer();
            ProcessStage = ProcessingStage.None;
            Busy = false;
            Stop();
        }

        /// <summary>
        /// Gets the text from the textbox
        /// </summary>
        public string GetArticleText()
        {
            if (!HasArticleTextBox || Document == null)
                return "";

            HtmlElement wpTextbox1 = Document.GetElementById("wpTextbox1");

            if (wpTextbox1 == null)
                return "";

            return HttpUtility.HtmlDecode(wpTextbox1.InnerHtml) ?? "";
        }

        /// <summary>
        /// returns head of the currently loaded document or empty string of none
        /// </summary>
        public string GetHead()
        {
            if (Document == null) return "";
            try { return Tools.StringBetween(DocumentText, "<head>", "</head>"); }
            catch { return ""; }
        }

        /// <summary>
        /// returns value of one of JavaScript scripting variables set by MediaWiki
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetScriptingVar(string name)
        {
            if (Document == null)
                return "";

            try
            {
                Regex r = new Regex(@"^\s*(?:var )?\s*" + name + @"\s*=\s*(.*)[;,]", RegexOptions.Multiline);
                var head = GetHead();
                Match m = r.Match(head);

                if (!m.Groups[1].Success)
                    return "";

                string s = m.Groups[1].Value.Trim('"');
                return s.Replace("\\\"", "\"").Replace("\\'", "'");
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Sets the article text
        /// </summary>
        public void SetArticleText(string ArticleText)
        {
            if (HasArticleTextBox && Document != null)
            {
                HtmlElement wpTextbox1 = Document.GetElementById("wpTextbox1");

                if (wpTextbox1 == null)
                    return;

                wpTextbox1.Enabled = true;
                wpTextbox1.InnerText = ArticleText.Trim();
            }
        }

        /// <summary>
        /// Sets the minor checkbox
        /// </summary>
        public void SetMinor(bool IsMinor)
        {
            if (Document == null)
                return;

            HtmlElement wpMinorEdit = Document.GetElementById("wpMinoredit");

            if (wpMinorEdit != null)
                wpMinorEdit.SetAttribute("checked", IsMinor ? "checked" : "");
        }

        /// <summary>
        /// Sets the watch checkbox
        /// </summary>
        public void SetWatch(bool watch)
        {
            if (Document == null)
                return;

            HtmlElement wpWatchthis = Document.GetElementById("wpWatchthis");

            if (wpWatchthis == null)
                return;

            wpWatchthis.SetAttribute("checked", watch ? "checked" : "");
        }


        /// <summary>
        /// Sets the edit summary text
        /// </summary>
        public void SetSummary(string Summary)
        {
            if (Document == null)
                return;

            HtmlElement wpSummary = Document.GetElementById("wpSummary");

            if (wpSummary == null)
                return;

            wpSummary.InnerText = Summary;
        }

        /// <summary>
        /// Sets the reason given for deletion or move, returns true if successful
        /// </summary>
        public bool SetReason(string Reason)
        {
            if (Document == null)
                return false;

            HtmlElement wpReason = Document.GetElementById("wpReason");

            if (wpReason == null)
                return false;

            wpReason.InnerText = Reason;

            return true;
        }

        /// <summary>
        /// Sets the reason given for protection, returns true if successful
        /// </summary>
        public bool SetReasonAndExpiry(string Reason, string Expiry)
        {
            if (Document == null)
                return false;

            HtmlElement mwProtectreason = Document.GetElementById("mwProtect-reason");

            if (mwProtectreason == null)
                return false;

            mwProtectreason.InnerText = Reason;

            HtmlElement mwProtectexpiry= Document.GetElementById("mwProtect-expiry");

            if (mwProtectexpiry == null)
                return false;
            
            mwProtectexpiry.InnerText = Expiry;
            return true;
        }

        /// <summary>
        /// Gets a value indicating whether the minor checkbox is checked
        /// </summary>
        public bool IsMinor()
        {
            if (Document == null)
                return false;

            HtmlElement wpMinoredit = Document.GetElementById("wpMinoredit");

            if (wpMinoredit == null)
                return false;

            return (wpMinoredit.GetAttribute("checked") == "True");
        }

        /// <summary>
        /// Gets a value indicating whether the watch checkbox is checked
        /// </summary>
        public bool IsWatched()
        {
            if (Document == null)
                return false;

            HtmlElement wpWatchthis = Document.GetElementById("wpWatchthis");

            if (wpWatchthis == null)
                return false;

            return (wpWatchthis.GetAttribute("checked") == "True");
        }

        /// <summary>
        /// Gets the entered edit summary
        /// </summary>
        public string GetSummary()
        {
            if (Document == null)
                return "";

            HtmlElement wpSummary = Document.GetElementById("wpSummary");

            return (wpSummary != null) ? wpSummary.InnerText : "";
        }

        const string startMark = "<!-- start content -->", endMark = "<!-- end content -->";

        /// <summary>
        /// Gets the HTML within the <!-- start content --> and <!-- end content --> tags
        /// </summary>
        public string PageHTMLSubstring(string text)
        {
            if (Document != null && Document.Body != null && Document.Body.InnerHtml.Contains(startMark) && Document.Body.InnerHtml.Contains(endMark))
                return text.Substring(text.IndexOf(startMark), text.IndexOf(endMark) - text.IndexOf(startMark));
            return text;
        }

        /// <summary>
        /// Returns true if browser is currently doing something
        /// </summary>
        public bool Loading
        {
            get { return ReadyState != WebBrowserReadyState.Complete; }
        }

        /// <summary>
        /// wait for current operation to complete
        /// </summary>
        public void Wait()
        {
            while (Loading && !Shutdown) Application.DoEvents();
        }

        #endregion

        #region Save/load/diff methods and events

        /// <summary>
        /// Invokes the Save button
        /// </summary>
        public void Save()
        {
            if (Document == null)
                return;

            AllowNavigation = true;
            ProcessStage = ProcessingStage.Save;
            Status = "Saving";

            HtmlElement wpSave = Document.GetElementById("wpSave");

            if (wpSave == null)
                return;

            wpSave.InvokeMember("click");
        }

        /// <summary>
        /// Invokes the Preview button
        /// </summary>
        public void ShowPreview()
        {
            if (Document == null)
                return;

            AllowNavigation = true;
            ProcessStage = ProcessingStage.Diff;
            Status = "Loading preview";

            HtmlElement wpPreview = Document.GetElementById("wpPreview");

            if (wpPreview == null)
                return;

            wpPreview.InvokeMember("click");

            Wait();
        }

        /// <summary>
        /// Invokes the Delete button
        /// </summary>
        public void Delete()
        {
            if (Document == null)
                return;

            AllowNavigation = true;
            ProcessStage = ProcessingStage.Delete;
            Status = "Deleting page";

            HtmlElement wpConfirmB = Document.GetElementById("wpConfirmB");

            if (wpConfirmB == null)
                return;

            wpConfirmB.InvokeMember("click");

            Deleted(null, null);
        }

        /// <summary>
        /// Loads the delete page of the given article
        /// </summary>
        public void LoadDeletePage(string article)
        {
            try
            {
                AllowNavigation = true;
                ProcessStage = ProcessingStage.Delete;
                Status = "Loading delete page";
                Navigate(Variables.URLIndex + "?title=" + HttpUtility.UrlEncode(article) + "&action=delete");
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        /// <summary>
        /// Invokes the Protect button
        /// </summary>
        public void Protect(int editProtectionLevel, int moveProtectionLevel, bool cascadingProtection)
        {
            if (Document == null)
                return;

            AllowNavigation = true;
            ProcessStage = ProcessingStage.Protect;
            Status = "Protecting page";

            SetListBoxValues(Document.GetElementById("mwProtect-level-edit"), editProtectionLevel);
            SetListBoxValues(Document.GetElementById("mwProtect-level-move"), moveProtectionLevel);

            HtmlElement mwProtectcascade = Document.GetElementById("mwProtect-cascade");

            if (mwProtectcascade == null)
                return;

            mwProtectcascade.SetAttribute("checked", cascadingProtection ? "checked" : "");

            HtmlElement mwProtectsubmit = Document.GetElementById("mw-Protect-submit");

            if (mwProtectsubmit == null)
                return;

            mwProtectsubmit.InvokeMember("click");

        }

        private static void SetListBoxValues(HtmlElement element, int level)
        {
            if (element == null)
                return;

            if (level != 0)
            {
                element.Children[0].SetAttribute("selected", "");
                switch (level)
                {
                    case 1:
                        element.Children[1].SetAttribute("selected", "selected");
                        element.Children[2].SetAttribute("selected", "");
                        break;
                    case 2:
                        element.Children[1].SetAttribute("selected", "");
                        element.Children[2].SetAttribute("selected", "selected");
                        break;
                }
            }
            else
            {
                element.Children[0].SetAttribute("selected", "selected");
                element.Children[1].SetAttribute("selected", "");
                element.Children[2].SetAttribute("selected", "");
            }
        }

        /// <summary>
        /// Loads the protect page of the given article
        /// </summary>
        public void LoadProtectPage(string article)
        {
            try
            {
                AllowNavigation = true;
                ProcessStage = ProcessingStage.Protect;
                Status = "Loading protect page";
                Navigate(Variables.URLIndex + "?title=" + HttpUtility.UrlEncode(article) + "&action=protect");
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        /// <summary>
        /// Loads the edit page of the given article
        /// </summary>
        public void LoadEditPage(string article)
        {
            try
            {
                AllowNavigation = true;
                ProcessStage = ProcessingStage.Load;
                Status = "Loading page";
                Navigate(Variables.URLIndex + "?title=" + HttpUtility.UrlEncode(article) + "&action=edit&useskin=myskin");
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        /// <summary>
        /// Loads the edit page of the given article
        /// </summary>
        /// <param name="article">Article title</param>
        /// <param name="revision">Revision</param>
        public void LoadEditPage(string article, int revision)
        {
            LoadEditPage(article, revision.ToString());
        }

        /// <summary>
        /// Loads the edit page of the given article
        /// </summary>
        /// <param name="article">Article title</param>
        /// <param name="revision">Revision</param>
        public void LoadEditPage(string article, string revision)
        {
            try
            {
                AllowNavigation = true;
                ProcessStage = ProcessingStage.Load;
                Status = "Loading page";
                Navigate(Variables.URLIndex + "?title=" + HttpUtility.UrlEncode(article) + "&action=edit&oldid="
                    + revision);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        /// <summary>
        /// Loads the edit page of the given article
        /// </summary>
        /// <param name="article">Article title</param>
        /// <param name="section">Section name</param>
        public void LoadEditPageSection(string article, string section)
        {
            try
            {
                AllowNavigation = true;
                ProcessStage = ProcessingStage.Load;
                Status = "Loading page";
                string url = Variables.URLIndex + "?title=" + HttpUtility.UrlEncode(article) + "&action=edit&section=" + section;
                Navigate(url);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        /// <summary>
        /// Loads the log in page
        /// </summary>
        public void LoadLogInPage()
        {
            AllowNavigation = true;
            ProcessStage = ProcessingStage.None;
            Status = "Loading log in page";
            Navigate(Variables.URLIndex + "?title=Special:Userlogin&returnto=Main_Page");
            Busy = false;
        }

        /// <summary>
        /// Allows user to logout
        /// </summary>
        public void LoadLogOut()
        {
            AllowNavigation = true;
            ProcessStage = ProcessingStage.None;
            Status = "Logging Out";
            Navigate(Variables.URLApi + "?action=logout");
            Busy = false;
        }

        readonly Regex RegexArticleExists = new Regex("<LI (class=new|class=\"selected new\") id=ca-nstab", RegexOptions.Compiled);
        readonly Regex RegexArticleTalkExists = new Regex("<LI (class=new|class=\"selected new\") id=ca-talk", RegexOptions.Compiled);

        protected override void OnDocumentCompleted(WebBrowserDocumentCompletedEventArgs e)
        {
            base.OnDocumentCompleted(e);

            // reset cached variables
            CachedEditBox = null;

            StopTimer();

            if (Document == null || Document.Body == null || !Document.Body.InnerHtml.Contains("id=siteSub"))
            {
                ProcessStage = ProcessingStage.None;
                if (Fault != null)
                    Fault(null, null);
                return;
            }

            if (Url.AbsolutePath.Contains("api.php?action=logout"))
                AllowNavigation = false;
            else switch (ProcessStage)
            {
                case ProcessingStage.Load:
                    {
                        TalkPageExists = !RegexArticleTalkExists.IsMatch(Document.Body.InnerHtml);

                        ArticlePageExists = !RegexArticleExists.IsMatch(Document.Body.InnerHtml);

                        AllowNavigation = false;
                        ProcessStage = ProcessingStage.None;

                        Status = "Ready to save";

                        if (Loaded != null)
                            Loaded(null, null);

                        HtmlElement wpTextbox1 = Document.GetElementById("wpTextbox1");

                        if (wpTextbox1 != null)
                            wpTextbox1.Enabled = false;
                    }
                    break;
                case ProcessingStage.Diff:
                    AllowNavigation = false;
                    ProcessStage = ProcessingStage.None;
                    Status = "Ready to save";
                    break;
                case ProcessingStage.None:
                    if (None != null)
                        None(null, null);
                    break;
            }
        }

        protected override void OnProgressChanged(WebBrowserProgressChangedEventArgs e)
        {
            if (ReadyState == WebBrowserReadyState.Interactive && ProcessStage == ProcessingStage.Save)
            {
                StopTimer();
                OnDocumentCompleted(null);
                AllowNavigation = false;
                ProcessStage = ProcessingStage.None;
                Stop();
                if (Saved != null)
                    Saved(null, null);
            }
            base.OnProgressChanged(e);
        }

        /// <summary>
        /// Moves an article, returns true if successful
        /// </summary>
        public bool MovePage(string oldTitle, string newTitle, string summary)
        {
            AllowNavigation = true;

            Navigate(Variables.URL + "/wiki/Special:Movepage/" + oldTitle);
            Status = "Loading move page";
            Wait();

            if (Document == null || Document.Body == null || !Document.Body.InnerHtml.Contains("wpNewTitle"))
            {
                AllowNavigation = false;
                return false;
            }

            HtmlElement wpNewTitle = Document.GetElementById("wpNewTitle");

            if (wpNewTitle != null)
                wpNewTitle.InnerText = newTitle;

            if (!SetReason(summary))
            {
                AllowNavigation = false;
                return false;
            }

            HtmlElement movepage = Document.GetElementById("movepage");

            if (movepage == null)
                return false;

            foreach (HtmlElement e in movepage.GetElementsByTagName("input"))
            {
                if (e.GetAttribute("name") == "wpMove")
                {
                    Status = "Moving";
                    e.InvokeMember("click");
                    Wait();

                    AllowNavigation = false;
                    if (e.Document != null && e.Document.GetElementById("movepage") != null) return false;

                    Status = "Moved";
                    return true;
                }
            }

            AllowNavigation = false;
            return false;
        }

        /// <summary>
        /// Sets unwatched pages to watched, and watched pages to unwatched
        /// </summary>
        public void WatchUnwatch()
        {
            if (Document == null)
                return;

            HtmlElement ca = IsWatched() ? Document.GetElementById("ca-watch") : Document.GetElementById("ca-un");

            if (ca == null)
                return;

            ca.InvokeMember("click");

            Wait();
            AllowNavigation = false;
        }

        /// <summary>
        /// Deletes an article, returns true if successful
        /// </summary>
        public bool DeletePage(string article, string summary)
        {
            LoadDeletePage(article);
            Wait();

            if (Document == null)
            {
                AllowNavigation = false;
                return false;
            }

            if (!SetReason(summary))
            {
                AllowNavigation = false;
                return false;
            }

            Delete();
            Wait();
            AllowNavigation = false;

            Status = "Protected";
            return true;
        }

        /// <summary>
        /// Protects an article, returns true if successful
        /// </summary>
        public bool ProtectPage(string article, string summary, int editProtectionLevel, int moveProtectionLevel, string protectExpiry, bool cascadingProtection)
        {
            LoadProtectPage(article);
            Wait();

            if (Document == null)
            {
                AllowNavigation = false;
                return false;
            }

            if (!SetReasonAndExpiry(summary, protectExpiry))
            {
                AllowNavigation = false;
                return false;
            }

            Protect(editProtectionLevel, moveProtectionLevel, cascadingProtection);
            Wait();
            AllowNavigation = false;

            Status = "Protected";
            return true;
        }

        #endregion

        protected override void OnNavigating(WebBrowserNavigatingEventArgs e)
        {
            base.OnNavigating(e);
            StartTimer();
        }

        int LoadTime;
        private void StartTimer()
        {
            StopTimer();
            timer.Tick += IncrementTime;
        }

        private void StopTimer()
        {
            timer.Tick -= IncrementTime;
            LoadTime = 0;
        }

        private void IncrementTime(object sender, EventArgs e)
        {
            LoadTime++;

            if (LoadTime == Timeout)
            {
                StopTimer();
                Stop2();
                Status = "Timed out";
                if (Fault != null)
                    Fault(null, null);
            }
        }

        int Timeout = 30;
        public int TimeoutLimit
        {
            get { return Timeout; }
            set { Timeout = value; }
        }
    }

    [Serializable]
    public class WebBrowserOperationsException : ApplicationException
    {
        public WebBrowserOperationsException()
            : base("Web browser operations exception") { }

        public WebBrowserOperationsException(string message)
            : base(message) { }

        public WebBrowserOperationsException(string message, Exception inner)
            : base("Web browser operations exception: " + message, inner) { }

        protected WebBrowserOperationsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
