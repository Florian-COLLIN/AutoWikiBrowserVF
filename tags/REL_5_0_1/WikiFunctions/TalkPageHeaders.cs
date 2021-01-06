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

/* Some of this is currently only suitable for enwiki. */

using System.Text.RegularExpressions;
using WikiFunctions.Logging;

namespace WikiFunctions.TalkPages
{
    public enum DEFAULTSORT
    {
        NoChange,
        MoveToTop,
        MoveToBottom
    }

    internal class Processor
    {
        public string DefaultSortKey;
        public bool FoundDefaultSort;
        public bool FoundSkipToTalk;
        public bool FoundTalkHeader;

        // Match evaluators:
        public string DefaultSortMatchEvaluator(Match match)
        {
            FoundDefaultSort = true;
            if (match.Groups["key"].Captures.Count > 0)
                DefaultSortKey = match.Groups["key"].Captures[0].Value.Trim();
            return "";
        }

        public string SkipTOCMatchEvaluator(Match match)
        {
            FoundSkipToTalk = true;
            return "";
        }

        public string TalkHeaderMatchEvaluator(Match match)
        {
            FoundTalkHeader = true;
            return "";
        }
    }

    public static class TalkPageHeaders
    {
        public static bool ContainsDefaultSortKeywordOrTemplate(string articleText)
        {
            return WikiRegexes.Defaultsort.IsMatch(articleText);
        }

        public static bool ProcessTalkPage(ref string articleText, ref string summary, DEFAULTSORT moveDefaultsort)
        {
            Processor pr = new Processor();

            articleText = WikiRegexes.SkipTOCTemplateRegex.Replace(articleText, new MatchEvaluator(pr.SkipTOCMatchEvaluator), 1);
            
            // move talk page header to the top
            articleText = MoveTalkHeader(articleText, ref summary);

            if (pr.FoundSkipToTalk)
                WriteHeaderTemplate("skip to talk", ref articleText, ref summary);

            if (moveDefaultsort != DEFAULTSORT.NoChange)
            {
                articleText = WikiRegexes.Defaultsort.Replace(articleText, 
                    new MatchEvaluator(pr.DefaultSortMatchEvaluator), 1);
                if (pr.FoundDefaultSort)
                {
                    if (string.IsNullOrEmpty(pr.DefaultSortKey))
                    {
                        AppendToSummary(ref summary, "DEFAULTSORT has no key; removed");
                    }
                    else
                    {
                        articleText = SetDefaultSort(pr.DefaultSortKey, moveDefaultsort, articleText, ref summary);
                    }
                }
            }
            
            articleText = AddMissingFirstCommentHeader(articleText, ref summary);
            
            return pr.FoundTalkHeader || pr.FoundSkipToTalk || pr.FoundDefaultSort;
        }

        public static string FormatDefaultSort(string articleText)
        {
            return WikiRegexes.Defaultsort.Replace(articleText, "{{DEFAULTSORT:${key}}}");
        }

        /// <summary>
        /// Appends the input text to the input edit summary (comma separated clauses)
        /// </summary>
        /// <param name="summary">the edit summary to update</param>
        /// <param name="newText">the text to append</param>
        private static void AppendToSummary(ref string summary, string newText)
        {
            if (!string.IsNullOrEmpty(summary))
                summary += ", ";

            summary += newText;
        }
        
        // Helper routines:
        private static string SetDefaultSort(string key, DEFAULTSORT location, string articleText, ref string summary)
        {
            string movedTo;
            switch (location)
            {
                case DEFAULTSORT.MoveToTop:
                    articleText = "{{DEFAULTSORT:" + key + "}}\r\n" + articleText;
                    movedTo = " given top billing";
                    break;
                case DEFAULTSORT.MoveToBottom:
                    articleText = articleText + "\r\n{{DEFAULTSORT:" + key + "}}";
                    movedTo = " sent to the bottom";
                    break;
                default:
                    movedTo = "";
                    break;
            }

            if (location != DEFAULTSORT.NoChange)
                AppendToSummary(ref summary, "DEFAULTSORT" + movedTo);

            return articleText;
        }

        /// <summary>
        /// Writes the input template to the top of the input page; updates the input edit summary
        /// </summary>
        /// <param name="name">template name to write</param>
        /// <param name="articleText">article text to update</param>
        /// <param name="summary">edit summary to update</param>
        private static void WriteHeaderTemplate(string name, ref string articleText, ref string summary)
        {
            articleText = "{{" + name + "}}\r\n" + articleText;

            AppendToSummary(ref summary, "{{tl|" + name + "}} given top billing");
        }
        
        /// <summary>
        /// Moves the {{talk header}} template to the top of the talk page
        /// </summary>
        /// <param name="articleText">the talk page text</param>
        /// <param name="summary">the edit summary</param>
        /// <returns>the update talk page text</returns>
        private static string MoveTalkHeader(string articleText, ref string summary)
        {
            Match m = WikiRegexes.TalkHeaderTemplate.Match(articleText);
            
            if(m.Success && m.Index > 0)
            {
                // remove existing talk header – handle case where not at beginning of line
                if(articleText.Contains("\r\n" + m.Value))
                    articleText = articleText.Replace(m.Value, "");
                else
                    articleText = articleText.Replace(m.Value, "\r\n");
                
                // write existing talk header to top
                articleText = m.Value.TrimEnd() + "\r\n" + articleText.TrimStart();
                
                // ensure template is now named {{talk header}}
                articleText = articleText.Replace(m.Groups[1].Value, "talk header");
                
                AppendToSummary(ref summary, "{{tl|Talk header}} given top billing");
            }
            
            return articleText;
        }
        
        private static readonly Regex FirstComment = new Regex(@"^ *[:\*\w'""](?<!_)", RegexOptions.Compiled | RegexOptions.Multiline);
        
        /// <summary>
        /// Adds a section 2 heading before the first comment if the talk page does not have one
        /// </summary>
        /// <param name="articleText">The talk page text</param>
        /// <param name="summary">the edit summary</param>
        /// <returns>The updated article text</returns>
        private static string AddMissingFirstCommentHeader(string articleText, ref string summary)
        {
            // don't match on lines within templates
            string articleTextTemplatesSpaced = Tools.ReplaceWithSpaces(articleText, WikiRegexes.NestedTemplates.Matches(articleText));
            
            if(FirstComment.IsMatch(articleTextTemplatesSpaced))
            {
                int firstCommentIndex = FirstComment.Match(articleTextTemplatesSpaced).Index;
                
                int firstLevelTwoHeading = WikiRegexes.HeadingLevelTwo.IsMatch(articleText) ? WikiRegexes.HeadingLevelTwo.Match(articleText).Index : 99999999;
                
                if (firstCommentIndex < firstLevelTwoHeading)
                {
                    
                    // is there a heading level 3? If yes, change to level 2
                    string articletexttofirstcomment = articleText.Substring(0, firstCommentIndex);
                    
                    if(WikiRegexes.HeadingLevelThree.IsMatch(articletexttofirstcomment))
                    {
                        AppendToSummary(ref summary, "Corrected comments section header");
                        articleText = WikiRegexes.HeadingLevelThree.Replace(articleText, @"==$1==", 1);
                    }
                    else
                    {
                        AppendToSummary(ref summary, "Added missing comments section header");
                        articleText = articleText.Insert(firstCommentIndex, "\r\n==Untitled==\r\n");
                    }
                }
            }
            
            return articleText;
        }
    }
}

