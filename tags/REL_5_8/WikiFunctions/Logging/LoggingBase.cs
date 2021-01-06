﻿/*
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

using System.Text.RegularExpressions;

namespace WikiFunctions.Logging
{
    /// <summary>
    /// This abstract class can be used to build trace listener classes, or you can build a class from scratch and implement IMyTraceListener
    /// </summary>
    public abstract class TraceListenerBase : System.IO.StreamWriter, IMyTraceListener
    {
        // Initialisation
        private static readonly Regex GetArticleTemplateRegex = new Regex("( talk)?:", RegexOptions.Compiled);

        protected TraceListenerBase(string filename)
            : base(filename, false, System.Text.Encoding.UTF8)
        {
        }

        #region IMyTraceListener interface
        public abstract void ProcessingArticle(string fullArticleTitle, int ns);
        public abstract void WriteBulletedLine(string line, bool bold, bool verboseOnly, bool dateStamp);
        public void WriteBulletedLine(string line, bool bold, bool verboseOnly)
        {
            WriteBulletedLine(line, bold, verboseOnly, false);
        }
        public abstract void SkippedArticle(string skippedBy, string reason);
        public abstract void SkippedArticleBadTag(string skippedBy, string fullArticleTitle, int ns);
        public abstract void WriteArticleActionLine(string line, string pluginName);
        public abstract void WriteTemplateAdded(string template, string pluginName);
        public abstract void WriteComment(string line);
        public abstract void WriteCommentAndNewLine(string line);

        public virtual void SkippedArticleRedlink(string skippedBy, string fullArticleTitle, int ns)
        {
            SkippedArticle(skippedBy, "Attached article doesn't exist - maybe deleted?");
        }

        public void WriteArticleActionLine(string line, string pluginName, bool verboseOnly)
        {
            WriteArticleActionLineVerbose(line, pluginName, verboseOnly);
        }

        public void WriteArticleActionLineVerbose(string line, string pluginName, bool verboseOnly)
        {
            if (verboseOnly && ! Verbose)
            {
                return;
            }
            WriteArticleActionLine(line, pluginName);
        }

        public abstract bool Uploadable {get;}
#endregion

        // Protected and public members:
        public static string GetArticleTemplate(string articleFullTitle, int ns)
        {
            switch (ns)
            {
                case Namespace.Article:
                    return "#{{subst:la|" + articleFullTitle + "}}";

                case Namespace.Talk:
                    return "#{{subst:lat|" + Tools.RemoveNamespaceString(articleFullTitle).Trim() + "}}";

                default:
                    string strnamespace = GetArticleTemplateRegex.Replace(Variables.Namespaces[ns], "");

                    string templ = ns % 2 == 1 ? "lnt" : "ln";

                    return "#{{subst:" + templ + "|" + strnamespace + "|" + 
                        Tools.RemoveNamespaceString(articleFullTitle).Trim() + "}}";
            }
        }
        public abstract bool Verbose { get; }
    }
}
