﻿/*

Copyright (C) 2007 Martin Richards

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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using WikiFunctions.Lists.Providers;

namespace WikiFunctions.Parse
{
    //TODO:Make Regexes Compiled as required
    //TODO:Move Regexes to WikiRegexes as required
    //TODO:Split Parser code into separate files (for manageability), or even into separate classes
    //TODO:Move regexes declared in method bodies (if not dynamic based on article title, etc), into class body

    /// <summary>
    /// Provides functions for editing wiki text, such as formatting and re-categorisation.
    /// </summary>
    public class Parsers
    {
        #region constructor etc.
        public Parsers()
        {
        }

        /// <summary>
        /// Re-organises the Person Data, stub/disambig templates, categories and interwikis
        /// </summary>
        /// <param name="stubWordCount">The number of maximum number of words for a stub.</param>
        /// <param name="addHumanKey"></param>
        public Parsers(int stubWordCount, bool addHumanKey)
        {
            StubMaxWordCount = stubWordCount;
            Sorter.AddCatKey = addHumanKey;
        }

        /// <summary>
        /// 
        /// </summary>
        static Parsers()
        {
            //look bad if changed
            RegexUnicode.Add(new Regex("&(ndash|mdash|minus|times|lt|gt|nbsp|thinsp|shy|lrm|rlm|[Pp]rime|ensp|emsp|#x2011|#820[13]|#8239);", RegexOptions.Compiled), "&amp;$1;");
            //IE6 does like these
            RegexUnicode.Add(new Regex("&#(705|803|596|620|699|700|8652|9408|9848|12288|160|61|x27|39);", RegexOptions.Compiled), "&amp;#$1;");

            //Decoder doesn't like these
            RegexUnicode.Add(new Regex("&#(x109[0-9A-Z]{2});", RegexOptions.Compiled), "&amp;#$1;");
            RegexUnicode.Add(new Regex("&#((?:277|119|84|x1D|x100)[A-Z0-9a-z]{2,3});", RegexOptions.Compiled), "&amp;#$1;");
            RegexUnicode.Add(new Regex("&#(x12[A-Za-z0-9]{3}|65536|769);", RegexOptions.Compiled), "&amp;#$1;");

            //interfere with wiki syntax
            RegexUnicode.Add(new Regex("&#(0?13|126|x5[BD]|x7[bcd]|0?9[13]|0?12[345]|0?0?3[92]);", RegexOptions.Compiled | RegexOptions.IgnoreCase), "&amp;#$1;");

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Greedy regex for unicode characters
            // .NET doesn't seem to like the Unicode versions of these – deleted from edit box
            RegexUnicode.Add(new Regex("&#(x2[0-9AB][0-9A-Fa-f]{3});", RegexOptions.Compiled), "&amp;#$1;");

            // clean 'do-attempt =July 2006|att=April 2008' to 'do attempt = April 2008'
            RegexConversion.Add(new Regex(@"({{\s*(?:[Aa]rticle|[Mm]ultiple) ?issues\s*(?:\|[^{}]*|\|)\s*[Dd]o-attempt\s*=\s*)[^{}\|]+\|\s*att\s*=\s*([^{}\|]+)(?=\||}})", RegexOptions.Compiled), "$1$2");

            // clean "Copyedit for=grammar|date=April 2009"to "Copyedit=April 2009"
            RegexConversion.Add(new Regex(@"({{\s*(?:[Aa]rticle|[Mm]ultiple) ?issues\s*(?:\|[^{}]*|\|)\s*[Cc]opyedit\s*)for\s*=\s*[^{}\|]+\|\s*date(\s*=[^{}\|]+)(?=\||}})", RegexOptions.Compiled), "$1$2");

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#.7B.7Bcommons.7CCategory:XXX.7D.7D_.3E_.7B.7Bcommonscat.7CXXX.7D.7D
            RegexConversion.Add(new Regex(@"\{\{[Cc]ommons\|\s*[Cc]ategory:\s*([^{}]+?)\s*\}\}", RegexOptions.Compiled), @"{{Commons category|$1}}");

            //https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Commons_category
            RegexConversion.Add(new Regex(@"(?<={{[Cc]ommons cat(?:egory)?\|\s*)([^{}\|]+?)\s*\|\s*\1\s*}}", RegexOptions.Compiled), @"$1}}");

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Remove_empty_.7B.7BArticle_issues.7D.7D
            // article issues with no issues -> remove tag
            RegexConversion.Add(new Regex(@"\{\{(?:[Aa]rticle|[Mm]ultiple) ?issues(?:\s*\|\s*(?:section|article)\s*=\s*[Yy])?\s*\}\}", RegexOptions.Compiled), "");

            // remove duplicate / populated and null fields in cite/multiple issues templates
            RegexConversion.Add(new Regex(@"({{\s*(?:[Aa]rticle|[Mm]ultiple) ?issues[^{}]*\|\s*)(\w+)\s*=\s*([^\|}{]+?)\s*\|((?:[^{}]*?\|)?\s*)\2(\s*=\s*)\3(\s*(\||\}\}))", RegexOptions.Compiled), "$1$4$2$5$3$6"); // duplicate field remover for cite templates
            RegexConversion.Add(new Regex(@"(\{\{\s*(?:[Aa]rticle|[Mm]ultiple) ?issues[^{}]*\|\s*)(\w+)(\s*=\s*[^\|}{]+(?:\|[^{}]+?)?)\|\s*\2\s*=\s*(\||\}\})", RegexOptions.Compiled), "$1$2$3$4"); // 'field=populated | field=null' drop field=null
            RegexConversion.Add(new Regex(@"(\{\{\s*(?:[Aa]rticle|[Mm]ultiple) ?issues[^{}]*\|\s*)(\w+)\s*=\s*\|\s*((?:[^{}]+?\|)?\s*\2\s*=\s*[^\|}{\s])", RegexOptions.Compiled), "$1$3"); // 'field=null | field=populated' drop field=null

            // replace any {{articleissues}} with {{Multiple issues}}
            RegexConversion.Add(new Regex(@"(?<={{\s*)(?:[Aa]rticle) ?(?=issues.*}})", RegexOptions.Compiled), "Multiple ");

            RegexConversion.Add(new Regex(@"({{\s*[Cc]itation needed\s*\|)\s*(?:[Dd]ate:)?([A-Z][a-z]+ 20\d\d)\s*\|\s*(date\s*=\s*\2\s*}})", RegexOptions.Compiled | RegexOptions.IgnoreCase), @"$1$3");

            SmallTagRegexes.Add(WikiRegexes.SupSub);
            SmallTagRegexes.Add(WikiRegexes.FileNamespaceLink);
            SmallTagRegexes.Add(WikiRegexes.Refs);
            SmallTagRegexes.Add(WikiRegexes.Small);
        }

        private static readonly Dictionary<Regex, string> RegexUnicode = new Dictionary<Regex, string>();
        private static readonly Dictionary<Regex, string> RegexConversion = new Dictionary<Regex, string>();

        private readonly HideText Hider = new HideText();
        private readonly HideText HiderHideExtLinksImages = new HideText(true, true, true);
        public static int StubMaxWordCount = 500;

        /// <summary>
        /// Sort interwiki link order
        /// </summary>
        public bool SortInterwikis
        {
            get { return Sorter.SortInterwikis; }
            set { Sorter.SortInterwikis = value; }
        }

        /// <summary>
        /// The interwiki link order to use
        /// </summary>
        public InterWikiOrderEnum InterWikiOrder
        {
            set { Sorter.InterWikiOrder = value; }
            get { return Sorter.InterWikiOrder; }
        }

        /// <summary>
        /// When set to true, adds key to categories (for people only) when parsed
        /// </summary>
        //public bool AddCatKey { get; set; }

        // should NOT be accessed directly, use Sorter
        private MetaDataSorter metaDataSorter;

        public MetaDataSorter Sorter
        {
            get { return metaDataSorter ?? (metaDataSorter = new MetaDataSorter()); }
        }

        #endregion

        #region General Parse

        public string HideText(string articleText)
        {
            return Hider.Hide(articleText);
        }

        public string AddBackText(string articleText)
        {
            return Hider.AddBack(articleText);
        }

        public string HideTextImages(string articleText)
        {
            return HiderHideExtLinksImages.Hide(articleText);
        }

        public string AddBackTextImages(string articleText)
        {
            return HiderHideExtLinksImages.AddBack(articleText);
        }

        public string HideMoreText(string articleText, bool hideOnlyTargetOfWikilink)
        {
            return HiderHideExtLinksImages.HideMore(articleText, hideOnlyTargetOfWikilink);
        }

        public string HideMoreText(string articleText)
        {
            return HiderHideExtLinksImages.HideMore(articleText);
        }

        public string AddBackMoreText(string articleText)
        {
            return HiderHideExtLinksImages.AddBackMore(articleText);
        }

        /// <summary>
        /// Re-organises the Person Data, stub/disambig templates, categories and interwikis
        /// except when a mainspace article has some 'includeonly' tags etc.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">The article title.</param>
        /// <returns>The re-organised text.</returns>
        public string SortMetaData(string articleText, string articleTitle)
        {
            return SortMetaData(articleText, articleTitle, true);
        }

        /// <summary>
        /// Re-organises the Person Data, stub/disambig templates, categories and interwikis
        /// except when a mainspace article has some 'includeonly' tags etc.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">The article title.</param>
        /// <param name="fixOptionalWhitespace">Whether to request optional excess whitespace to be fixed</param>
        /// <returns>The re-organised text.</returns>
        public string SortMetaData(string articleText, string articleTitle, bool fixOptionalWhitespace)
        {
            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Substituted_templates
            // if article contains some substituted template stuff, sorting the data may mess it up (further)
            if (Namespace.IsMainSpace(articleTitle) && NoIncludeIncludeOnlyProgrammingElement(articleText))
                return articleText;

            return (Variables.Project <= ProjectEnum.species) ? Sorter.Sort(articleText, articleTitle, fixOptionalWhitespace) : articleText;
        }

        private static readonly Regex RegexHeadings0 = new Regex("(== ?)(see also:?|related topics:?|related articles:?|internal links:?|also see:?)( ?==)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegexHeadings1 = new Regex("(== ?)(external link[s]?|external site[s]?|outside link[s]?|web ?link[s]?|exterior link[s]?):?( ?==)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        //private readonly Regex regexHeadings2 = new Regex("(== ?)(external link:?|external site:?|web ?link:?|exterior link:?)( ?==)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegexHeadings3 = new Regex("(== ?)(referen[sc]e:?)(s? ?==)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegexHeadings4 = new Regex("(== ?)(source:?)(s? ?==)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegexHeadings5 = new Regex("(== ?)(further readings?:?)( ?==)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegexHeadings6 = new Regex("(== ?)(Early|Personal|Adult|Later) Life( ?==)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegexHeadings7 = new Regex("(== ?)(Current|Past|Prior) Members( ?==)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegexHeadingsBold = new Regex(@"^(=+ ?)(?:'''|<b>)(.*?)(?:'''|</b>)( ?=+)\s*?(\r)?$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex RegexHeadings9 = new Regex("(== ?)track listing( ?==)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegexHeadings10 = new Regex("(== ?)Life and Career( ?==)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegexHeadingsCareer = new Regex("(== ?)([a-zA-Z]+) Career( ?==)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RegexRemoveLinksInHeadings = new Regex(@"^ *((={1,4})[^\[\]\{\}\|=\r\n]*)\[\[(?:[^\[\]\{\}\|=\r\n]+\|)?([^\[\]\{\}\|\r\n]+)(?<!.*(?:File|Image):.*)\]\]([^\{\}=\r\n]*\2)", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex RegexBadHeader = new Regex("^(={1,4} ?(about|description|overview|definition|profile|(?:general )?information|background|intro(?:duction)?|summary|bio(?:graphy)?) ?={1,4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RegexHeadingWhitespaceBefore = new Regex(@"^ *(==+)(\s*.+?\s*)\1 +(\r|\n)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex RegexHeadingWhitespaceAfter = new Regex(@"^ +(==+)(\s*.+?\s*)\1 *(\r|\n)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex RegexHeadingUpOneLevel = new Regex(@"^=(==+[^=].*?[^=]==+)=(\r\n?|\n)$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex ReferencesExternalLinksSeeAlso = new Regex(@"== *([Rr]eferences|[Ee]xternal +[Ll]inks?|[Ss]ee +[Aa]lso) *==\s", RegexOptions.Compiled);

        private static readonly Regex RegexHeadingColonAtEnd = new Regex(@"^(=+)(\s*[^=\s].*?)\:(\s*\1(?:\r\n?|\n))$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex RegexHeadingWithBold = new Regex(@"(?<====+.*?)(?:'''|<b>)(.*?)(?:'''|</b>)(?=.*?===+)", RegexOptions.Compiled);

        /// <summary>
        /// Fix ==See also== and similar section common errors.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">the title of the article</param>
        /// <param name="noChange">Value that indicates whether no change was made.</param>
        /// <returns>The modified article text.</returns>
        public static string FixHeadings(string articleText, string articleTitle, out bool noChange)
        {
            string newText = FixHeadings(articleText, articleTitle);

            noChange = (newText == articleText);

            return newText.Trim();
        }

        private static readonly Regex HeadingsWhitespaceBefore = new Regex(@"\s+(?:< *[Bb][Rr] *\/? *>\s*)*(^={1,6} *(.*?) *={1,6} *(?=\r\n))", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex LevelOneSeeAlso = new Regex("= ?See also ?=", RegexOptions.Compiled);
        private static readonly Regex ListOf = new Regex(@"^Lists? of", RegexOptions.Compiled);

        // Covered by: FormattingTests.TestFixHeadings(), incomplete
        /// <summary>
        /// Fix ==See also== and similar section common errors. Removes unecessary introductory headings and cleans excess whitespace.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">the title of the article</param>
        /// <returns>The modified article text.</returns>
        public static string FixHeadings(string articleText, string articleTitle)
        {
            // remove any <br> from headings
            foreach (Match m in WikiRegexes.Headings.Matches(articleText))
            {
                string hBefore = m.Value;
                string hAfter = WikiRegexes.Br.Replace(hBefore, "");
                hAfter = WikiRegexes.Big.Replace(hAfter, "$1");

                if (!hBefore.Equals(hAfter))
                    articleText = articleText.Replace(hBefore, hAfter);
            }

            articleText = Regex.Replace(articleText, "^={1,4} ?" + Regex.Escape(articleTitle) + " ?={1,4}", "", RegexOptions.IgnoreCase);

            articleText = RegexHeadingsBold.Replace(articleText, "$1$2$3$4");
            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Headlines_end_with_colon_.28WikiProject_Check_Wikipedia_.2357.29
            articleText = RegexHeadingColonAtEnd.Replace(articleText, "$1$2$3");
            articleText = RegexBadHeader.Replace(articleText, "");

            // only apply if < 6 matches, otherwise (badly done) articles with 'list of...' and lots of links in headings will be further messed up
            if (RegexRemoveLinksInHeadings.Matches(articleText).Count < 6
                && !(Regex.IsMatch(articleTitle, WikiRegexes.Months) || ListOf.IsMatch(articleTitle) || WikiRegexes.GregorianYear.IsMatch(articleTitle)))
            {
                // loop through in case a heading has mulitple wikilinks in it
                while (RegexRemoveLinksInHeadings.IsMatch(articleText))
                {
                    articleText = RegexRemoveLinksInHeadings.Replace(articleText, "$1$3$4");
                }
            }

            if (!LevelOneSeeAlso.IsMatch(articleText))
                articleText = RegexHeadings0.Replace(articleText, "$1See also$3");

            articleText = RegexHeadings1.Replace(articleText, "$1External links$3");

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_11#ReferenceS
            Match refsHeader = RegexHeadings3.Match(articleText);
            string refsheader1 = refsHeader.Groups[1].Value;
            string refsheader2 = refsHeader.Groups[2].Value;
            string refsheader3 = refsHeader.Groups[3].Value;
            if (refsheader2.Length > 0)
                articleText = articleText.Replace(refsheader1 + refsheader2 + refsheader3,
                                                  refsheader1 + "Reference" + refsheader3.ToLower());

            Match sourcesHeader = RegexHeadings4.Match(articleText);
            string sourcesheader1 = sourcesHeader.Groups[1].Value;
            string sourcesheader2 = sourcesHeader.Groups[2].Value;
            string sourcesheader3 = sourcesHeader.Groups[3].Value;
            if (sourcesheader2.Length > 0)
                articleText = articleText.Replace(sourcesheader1 + sourcesheader2 + sourcesheader3,
                                                  sourcesheader1 + "Source" + sourcesheader3.ToLower());

            articleText = RegexHeadings5.Replace(articleText, "$1Further reading$3");
            articleText = RegexHeadings6.Replace(articleText, "$1$2 life$3");
            articleText = RegexHeadings7.Replace(articleText, "$1$2 members$3");
            articleText = RegexHeadings9.Replace(articleText, "$1Track listing$2");
            articleText = RegexHeadings10.Replace(articleText, "$1Life and career$2");
            articleText = RegexHeadingsCareer.Replace(articleText, "$1$2 career$3");

            articleText = RegexHeadingWhitespaceBefore.Replace(articleText, "$1$2$1$3");
            articleText = RegexHeadingWhitespaceAfter.Replace(articleText, "$1$2$1$3");

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Section_header_level_.28WikiProject_Check_Wikipedia_.237.29
            // if no level 2 heading in article, remove a level from all headings (i.e. '===blah===' to '==blah==' etc.)
            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Standard_level_2_headers
            // don't consider the "references", "see also", or "external links" level 2 headings when counting level two headings
            string articleTextLocal = articleText;
            articleTextLocal = ReferencesExternalLinksSeeAlso.Replace(articleTextLocal, "");

            // only apply if all level 3 headings and lower are before the fist of references/external links/see also
            string originalarticleText = "";
            while (!originalarticleText.Equals(articleText))
            {
                originalarticleText = articleText;
                if (!WikiRegexes.HeadingLevelTwo.IsMatch(articleTextLocal) && Namespace.IsMainSpace(articleTitle))
                {
                    int upone = 0;
                    foreach (Match m in RegexHeadingUpOneLevel.Matches(articleText))
                    {
                        if (m.Index > upone)
                            upone = m.Index;
                    }

                    if (!ReferencesExternalLinksSeeAlso.IsMatch(articleText) || (upone < ReferencesExternalLinksSeeAlso.Match(articleText).Index))
                        articleText = RegexHeadingUpOneLevel.Replace(articleText, "$1$2");
                }

                articleTextLocal = ReferencesExternalLinksSeeAlso.Replace(articleText, "");
            }

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Bold_text_in_headers
            // remove bold from level 3 headers and below, as it makes no visible difference
            articleText = RegexHeadingWithBold.Replace(articleText, "$1");

            // one blank line before each heading per MOS:HEAD
            if (Variables.IsWikipediaEN)
                articleText = HeadingsWhitespaceBefore.Replace(articleText, "\r\n\r\n$1");

            return articleText;
        }

        private const int MinCleanupTagsToCombine = 3; // article must have at least this many tags to combine to {{multiple issues}}
        private static readonly Regex ExpertSubject = Tools.NestedTemplateRegex("expert-subject");

        /// <summary>
        /// Combines multiple cleanup tags into {{multiple issues}} template, ensures parameters have correct case, removes date parameter where not needed
        /// only for English-language wikis
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public string MultipleIssues(string articleText)
        {
            string ESDate = "";

            if (!Variables.LangCode.Equals("en"))
                return articleText;

            // convert title case parameters within {{Multiple issues}} to lower case
            foreach (Match m in WikiRegexes.MultipleIssuesInTitleCase.Matches(articleText))
            {
                string firstPart = m.Groups[1].Value;
                string parameterFirstChar = m.Groups[2].Value.ToLower();
                string lastPart = m.Groups[3].Value;

                articleText = articleText.Replace(m.Value, firstPart + parameterFirstChar + lastPart);
            }

            // remove any date field within  {{Multiple issues}} if no 'expert=subject' field using it
            string MICall = WikiRegexes.MultipleIssues.Match(articleText).Value;
            if (MICall.Length > 10 && (Tools.GetTemplateParameterValue(MICall, "expert").Length == 0 ||
                                       MonthYear.IsMatch(Tools.GetTemplateParameterValue(MICall, "expert"))))
                articleText = articleText.Replace(MICall, Tools.RemoveTemplateParameter(MICall, "date"));

            articleText = SectionTemplates.Replace(articleText, new MatchEvaluator(SectionTemplateConversionsME));

            string newTags = "";

            // get the zeroth section (text upto first heading)
            string zerothSection = WikiRegexes.ZerothSection.Match(articleText).Value;

            // get the rest of the article including first heading (may be null if entire article falls in zeroth section)
            string restOfArticle = articleText.Remove(0, zerothSection.Length);

            if (ExpertSubject.IsMatch(zerothSection))
            {
                ESDate = Tools.GetTemplateParameterValue(ExpertSubject.Match(zerothSection).Value, "date");
                zerothSection = Tools.RemoveTemplateParameter(zerothSection, "expert-subject", "date");
            }

            int tagsToAdd = WikiRegexes.MultipleIssuesTemplates.Matches(zerothSection).Count;

            // if currently no {{Multiple issues}} and less than the min number of cleanup templates, do nothing
            if (!WikiRegexes.MultipleIssues.IsMatch(zerothSection) && WikiRegexes.MultipleIssuesTemplates.Matches(zerothSection).Count < MinCleanupTagsToCombine)
            {
                // article issues with one issue -> single issue tag (e.g. {{multiple issues|cleanup=January 2008}} to {{cleanup|date=January 2008}} etc.)
                articleText = WikiRegexes.MultipleIssues.Replace(articleText, new MatchEvaluator(MultipleIssuesSingleTagME));

                return MultipleIssuesBLPUnreferenced(articleText);
            }

            // only add tags to multiple issues if new tags + existing >= MinCleanupTagsToCombine
            MICall = Tools.RenameTemplateParameter(WikiRegexes.MultipleIssues.Match(zerothSection).Value, "OR", "original research");

            if ((WikiRegexes.MultipleIssuesTemplateNameRegex.Matches(MICall).Count + tagsToAdd) < MinCleanupTagsToCombine || tagsToAdd == 0)
            {
                // article issues with one issue -> single issue tag (e.g. {{multiple issues|cleanup=January 2008}} to {{cleanup|date=January 2008}} etc.)
                articleText = WikiRegexes.MultipleIssues.Replace(articleText, new MatchEvaluator(MultipleIssuesSingleTagME));

                return MultipleIssuesBLPUnreferenced(articleText);
            }

            foreach (Match m in WikiRegexes.MultipleIssuesTemplates.Matches(zerothSection))
            {
                // all fields except COI, OR, POV and ones with BLP should be lower case
                string singleTag = m.Groups[1].Value;
                string tagValue = m.Groups[2].Value;
                if (!WikiRegexes.CoiOrPovBlp.IsMatch(singleTag))
                    singleTag = singleTag.ToLower();

                string singleTagLower = singleTag.ToLower();

                // tag renaming
                if (singleTagLower.Equals("cleanup-rewrite"))
                    singleTag = "rewrite";
                else if (singleTagLower.Equals("cleanup-laundry"))
                    singleTag = "laundrylists";
                else if (singleTagLower.Equals("cleanup-jargon"))
                    singleTag = "jargon";
                else if (singleTagLower.Equals("primary sources"))
                    singleTag = "primarysources";
                else if (singleTagLower.Equals("news release"))
                    singleTag = "newsrelease";
                else if (singleTagLower.Equals("game guide"))
                    singleTag = "gameguide";
                else if (singleTagLower.Equals("travel guide"))
                    singleTag = "travelguide";
                else if (singleTagLower.Equals("very long"))
                    singleTag = "verylong";
                else if (singleTagLower.Equals("cleanup-reorganise"))
                    singleTag = "restructure";
                else if (singleTagLower.Equals("cleanup-reorganize"))
                    singleTag = "restructure";
                else if (singleTagLower.Equals("cleanup-spam"))
                    singleTag = "spam";
                else if (singleTagLower.Equals("criticism section"))
                    singleTag = "criticisms";
                else if (singleTagLower.Equals("pov-check"))
                    singleTag = "pov-check";
                else if (singleTagLower.Equals("expert-subject"))
                    singleTag = "expert";

                // copy edit|for=grammar --> grammar
                if (singleTag.Replace(" ", "").Equals("copyedit") && Tools.GetTemplateParameterValue(m.Value, "for").Equals("grammar"))
                {
                    singleTag = "grammar";
                    tagValue = Regex.Replace(tagValue, @"for\s*=\s*grammar\s*\|?", "");
                }

                // expert must have a parameter
                if (singleTag == "expert" && tagValue.Trim().Length == 0)
                    continue;

                // for tags with a parameter, that parameter must be the date
                if ((tagValue.Contains("=") && Regex.IsMatch(tagValue, @"(?i)date")) || tagValue.Length == 0 || singleTag == "expert")
                {
                    tagValue = Regex.Replace(tagValue, @"^[Dd]ate\s*=\s*", "= ");

                    // every tag except expert needs a date
                    if (!singleTag.Equals("expert") && tagValue.Length == 0)
                        tagValue = @"= {{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}";
                    else if (!tagValue.Contains(@"="))
                        tagValue = @"= " + tagValue;

                    // don't add duplicate tags
                    if (MICall.Length == 0 || Tools.GetTemplateParameterValue(MICall, singleTag).Length == 0)
                        newTags += @"|" + singleTag + @" " + tagValue;
                }
                else
                    continue;

                newTags = newTags.Trim();

                // remove the single template
                zerothSection = zerothSection.Replace(m.Value, "");
            }

            if (ESDate.Length > 0)
                newTags += ("|date = " + ESDate);

            // if article currently has {{Multiple issues}}, add tags to it
            string ai = WikiRegexes.MultipleIssues.Match(zerothSection).Value;
            if (ai.Length > 0)
                zerothSection = zerothSection.Replace(ai, ai.Substring(0, ai.Length - 2) + newTags + @"}}");

            else // add {{Multiple issues}} to top of article, metaDataSorter will arrange correctly later
                zerothSection = @"{{Multiple issues" + newTags + "}}\r\n" + zerothSection;

            articleText = zerothSection + restOfArticle;

            // Conversions() will add any missing dates and correct ...|wikify date=May 2008|...
            return MultipleIssuesBLPUnreferenced(articleText);
        }

        /// <summary>
        /// Converts multiple issues with one issue to stand alone tag
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private string MultipleIssuesSingleTagME(Match m)
        {
            string newValue = Parsers.Conversions(Tools.RemoveTemplateParameter(m.Value, "section"));

            if (Tools.GetTemplateArgumentCount(newValue) == 1 && !WikiRegexes.NestedTemplates.IsMatch(Tools.GetTemplateArgument(newValue, 1)))
            {
                string single = Tools.GetTemplateArgument(newValue, 1);

                if (single.Contains(@"="))
                    newValue = @"{{" + single.Substring(0, single.IndexOf("=")).Trim() + @"|date=" + single.Substring(single.IndexOf("=") + 1).Trim() + @"}}";
            }
            else return m.Value;

            return newValue;
        }

        private static readonly Regex MultipleIssuesDate = new Regex(@"(?<={{\s*(?:[Aa]rticle|[Mm]ultiple) ?issues\s*(?:\|[^{}]*?)?(?:{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}[^{}]*?){0,4}\|[^{}\|]{3,}?)\b(?i)date(?<!.*out of.*)", RegexOptions.Compiled);

        /// <summary>
        /// In the {{Multiple issues}} template renames unref to BLPunref for living person bio articles
        /// </summary>
        /// <param name="articleText">The page text</param>
        /// <returns>The updated page text</returns>
        private string MultipleIssuesBLPUnreferenced(string articleText)
        {
            articleText = MultipleIssuesDate.Replace(articleText, "");

            if (WikiRegexes.MultipleIssues.IsMatch(articleText))
            {
                string aiat = WikiRegexes.MultipleIssues.Match(articleText).Value;

                // unref to BLPunref for living person bio articles
                if (Tools.GetTemplateParameterValue(aiat, "unreferenced").Length > 0 && articleText.Contains(@"[[Category:Living people"))
                    articleText = articleText.Replace(aiat, Tools.RenameTemplateParameter(aiat, "unreferenced", "BLP unsourced"));
                else if (Tools.GetTemplateParameterValue(aiat, "unref").Length > 0 && articleText.Contains(@"[[Category:Living people"))
                    articleText = articleText.Replace(aiat, Tools.RenameTemplateParameter(aiat, "unref", "BLP unsourced"));

                articleText = MetaDataSorter.MoveMaintenanceTags(articleText);
            }

            return articleText;
        }

        private static readonly Regex PortalBox = Tools.NestedTemplateRegex(new[] { "portal box", "portalbox" });

        /// <summary>
        /// Merges multiple {{portal}} templates into a single one, removing any duplicates. En-wiki only.
        /// Restricted to {{portal}} calls with one argument
        /// Article must have existing {{portal}} and/or a 'see also' section
        /// </summary>
        /// <param name="articleText">The article text</param>
        /// <returns>The updated article text</returns>
        public static string MergePortals(string articleText)
        {
            if (!Variables.LangCode.Equals("en"))
                return articleText;

            int firstPortal = WikiRegexes.PortalTemplate.Match(articleText).Index;

            string originalArticleText = articleText;
            List<string> Portals = new List<string>();

            foreach (Match m in WikiRegexes.PortalTemplate.Matches(articleText))
            {
                string thePortalCall = m.Value, thePortalName = Tools.GetTemplateArgument(m.Value, 1);

                if (!Portals.Contains(thePortalName) && Tools.GetTemplateArgumentCount(thePortalCall) == 1)
                {
                    Portals.Add(thePortalName);
                    articleText = Regex.Replace(articleText, Regex.Escape(thePortalCall) + @"\s*(?:\r\n)?", "");
                }
            }

            if (Portals.Count == 0)
                return articleText;

            // generate portal string
            string PortalsToAdd = "";
            foreach (string portal in Portals)
                PortalsToAdd += ("|" + portal.Trim());

            Match pb = PortalBox.Match(articleText);

            if (pb.Success) // append portals to existing portal
                return articleText.Replace(pb.Value, pb.Value.Substring(0, pb.Length - 2) + PortalsToAdd + @"}}");

            // merge in new portal if multiple portals
            if (Portals.Count < 2)
                return originalArticleText;

            // first merge to see also section
            if (WikiRegexes.SeeAlso.IsMatch(articleText))
                return WikiRegexes.SeeAlso.Replace(articleText, "$0" + Tools.Newline(@"{{Portal" + PortalsToAdd + @"}}"));

            // otherwise merge to original location if all portals in same section
            if (Summary.ModifiedSection(originalArticleText, articleText).Length > 0)
                return articleText.Insert(firstPortal, @"{{Portal" + PortalsToAdd + @"}}" + "\r\n");

            return originalArticleText;
        }

        /// <summary>
        /// Performs some cleanup operations on dablinks
        /// Merges some for & about dablinks
        /// Merges multiple distinguish into one
        /// </summary>
        /// <param name="articleText">The article text</param>
        /// <returns>The updated article text</returns>
        public static string Dablinks(string articleText)
        {
            if (!Variables.LangCode.Equals("en"))
                return articleText;

            string oldArticleText = "";

            string zerothSection = WikiRegexes.ZerothSection.Match(articleText).Value;
            string restOfArticle = articleText.Remove(0, zerothSection.Length);
            articleText = zerothSection;

            // conversions

            // otheruses4 rename - Wikipedia only
            if (Variables.IsWikipediaEN)
                articleText = Tools.RenameTemplate(articleText, "otheruses4", "about");

            // "{{about|about x..." --> "{{about|x..."
            foreach (Match m in Tools.NestedTemplateRegex("about").Matches(articleText))
            {
                if (m.Groups[3].Value.TrimStart("| ".ToCharArray()).ToLower().StartsWith("about"))
                    articleText = articleText.Replace(m.Value, m.Groups[1].Value + m.Groups[2].Value + Regex.Replace(m.Groups[3].Value, @"^\|\s*[Aa]bout\s*", "|"));
            }

            // merging

            // multiple same about into one
            oldArticleText = "";
            while (oldArticleText != articleText)
            {
                oldArticleText = articleText;
                bool doneAboutMerge = false;
                foreach (Match m in Tools.NestedTemplateRegex("about").Matches(articleText))
                {
                    string firstarg = Tools.GetTemplateArgument(m.Value, 1);

                    foreach (Match m2 in Tools.NestedTemplateRegex("about").Matches(articleText))
                    {
                        if (m2.Value == m.Value)
                            continue;

                        // match when reason is the same, not matching on self
                        if (Tools.GetTemplateArgument(m2.Value, 1).Equals(firstarg))
                        {
                            // argument 2 length > 0
                            if (Tools.GetTemplateArgument(m.Value, 2).Length > 0 && Tools.GetTemplateArgument(m2.Value, 2).Length > 0)
                            {
                                articleText = articleText.Replace(m.Value, m.Value.TrimEnd('}') + @"|" + Tools.GetTemplateArgument(m2.Value, 2) + @"|" + Tools.GetTemplateArgument(m2.Value, 3) + @"}}");
                                doneAboutMerge = true;
                            }

                            // argument 2 is null
                            if (Tools.GetTemplateArgument(m.Value, 2).Length == 0 && Tools.GetTemplateArgument(m2.Value, 2).Length == 0)
                            {
                                articleText = articleText.Replace(m.Value, m.Value.TrimEnd('}') + @"|and|" + Tools.GetTemplateArgument(m2.Value, 3) + @"}}");
                                doneAboutMerge = true;
                            }
                        }
                        // match when reason of one is null, the other not
                        else if (Tools.GetTemplateArgument(m2.Value, 1).Length == 0)
                        {
                            // argument 2 length > 0
                            if (Tools.GetTemplateArgument(m.Value, 2).Length > 0 && Tools.GetTemplateArgument(m2.Value, 2).Length > 0)
                            {
                                articleText = articleText.Replace(m.Value, m.Value.TrimEnd('}') + @"|" + Tools.GetTemplateArgument(m2.Value, 2) + @"|" + Tools.GetTemplateArgument(m2.Value, 3) + @"}}");
                                doneAboutMerge = true;
                            }
                        }

                        if (doneAboutMerge)
                        {
                            articleText = articleText.Replace(m2.Value, "");
                            break;
                        }
                    }
                    if (doneAboutMerge)
                        break;
                }
            }

            // multiple for into about: rename a 2-argument for into an about with no reason value
            if (Tools.NestedTemplateRegex("for").Matches(articleText).Count > 1 && Tools.NestedTemplateRegex("about").Matches(articleText).Count == 0)
            {
                foreach (Match m in Tools.NestedTemplateRegex("for").Matches(articleText))
                {
                    if (Tools.GetTemplateArgument(m.Value, 3).Length == 0)
                    {
                        articleText = articleText.Replace(m.Value, Tools.RenameTemplate(m.Value, "about|"));
                        break;
                    }
                }
            }

            // for into existing about, when about has >=2 arguments
            if (Tools.NestedTemplateRegex("about").Matches(articleText).Count == 1 &&
                Tools.GetTemplateArgument(Tools.NestedTemplateRegex("about").Match(articleText).Value, 2).Length > 0)
            {
                foreach (Match m in Tools.NestedTemplateRegex("for").Matches(articleText))
                {
                    string about = Tools.NestedTemplateRegex("about").Match(articleText).Value;
                    string extra = "";

                    // where about has 2 arguments need extra pipe
                    if (Tools.GetTemplateArgument(Tools.NestedTemplateRegex("about").Match(articleText).Value, 3).Length == 0
                        && Tools.GetTemplateArgument(Tools.NestedTemplateRegex("about").Match(articleText).Value, 4).Length == 0)
                        extra = @"|";

                    // append {{for}} value to the {{about}}
                    if (Tools.GetTemplateArgument(m.Value, 3).Length == 0)
                        articleText = articleText.Replace(about, about.TrimEnd('}') + extra + m.Groups[3].Value);
                    else // where for has 3 arguments need extra and
                        articleText = articleText.Replace(about, about.TrimEnd('}') + extra + m.Groups[3].Value.Insert(m.Groups[3].Value.LastIndexOf('|') + 1, "and|"));

                    // remove the old {{for}}
                    articleText = articleText.Replace(m.Value, "");
                }
            }

            // non-mainspace links need escaping in {{about}}
            foreach (Match m in Tools.NestedTemplateRegex("about").Matches(articleText))
            {
                string aboutcall = m.Value;
                for (int a = 1; a <= Tools.GetTemplateArgumentCount(m.Value); a++)
                {
                    string arg = Tools.GetTemplateArgument(aboutcall, a);
                    if (arg.Length > 0 && Namespace.Determine(arg) != Namespace.Mainspace)
                        aboutcall = aboutcall.Replace(arg, @":" + arg);
                }

                if (!m.Value.Equals(aboutcall))
                    articleText = articleText.Replace(m.Value, aboutcall);
            }

            // multiple {{distinguish}} into one
            oldArticleText = "";
            while (oldArticleText != articleText)
            {
                oldArticleText = articleText;
                bool doneDistinguishMerge = false;
                foreach (Match m in Tools.NestedTemplateRegex("distinguish").Matches(articleText))
                {
                    foreach (Match m2 in Tools.NestedTemplateRegex("distinguish").Matches(articleText))
                    {
                        if (m2.Value.Equals(m.Value))
                            continue;

                        articleText = articleText.Replace(m.Value, m.Value.TrimEnd('}') + m2.Groups[3].Value);

                        doneDistinguishMerge = true;
                        articleText = articleText.Replace(m2.Value, "");
                        break;
                    }

                    if (doneDistinguishMerge)
                        break;
                }
            }

            return (articleText + restOfArticle);
        }

        private static readonly List<string> SectionMergedTemplates = new List<string>(new[] { "see also", "see also2" });

        /// <summary>
        /// Merges multiple instances of the same template in the same section
        /// </summary>
        /// <param name="articleText">The article text</param>
        /// <returns>The updated article text</returns>
        public static string MergeTemplatesBySection(string articleText)
        {
            if (Tools.NestedTemplateRegex(SectionMergedTemplates).Matches(articleText).Count < 2)
                return articleText;

            string[] articleTextInSections = Tools.SplitToSections(articleText);
            StringBuilder newArticleText = new StringBuilder();

            for (int a = 0; a < articleTextInSections.Length; a++)
            {
                string sectionText = articleTextInSections[a].ToString();

                foreach (string t in SectionMergedTemplates)
                {
                    sectionText = MergeTemplates(sectionText, t);
                }
                newArticleText.Append(sectionText);
            }

            return newArticleText.ToString().TrimEnd();
        }

        /// <summary>
        /// Merges all instances of the given template in the given section of the article, only when templates at top of section
        /// </summary>
        /// <param name="sectionText">The article section text</param>
        /// <param name="templateName">The template to merge</param>
        /// <returns>The updated article section text</returns>
        private static string MergeTemplates(string sectionText, string templateName)
        {
        	if (!Variables.LangCode.Equals("en"))
        		return sectionText;
        	
        	Regex TemplateToMerge = Tools.NestedTemplateRegex(templateName);
        	string mergedTemplates = "";
        	
        	while(TemplateToMerge.IsMatch(sectionText))
        	{
        		Match m = TemplateToMerge.Match(sectionText);
        		
        		if(m.Index > 0)
        			break;
        		
        		if(mergedTemplates.Length == 0)
        			mergedTemplates = m.Value;
        		else
        			mergedTemplates = mergedTemplates.Replace(@"}}", m.Groups[3].Value);
        		
        		sectionText = sectionText.Substring(m.Length);
        	}
        	
        	if(mergedTemplates.Length > 0)
        		return (mergedTemplates + "\r\n" + sectionText);
        	else
        		return sectionText;
        }

        // fixes extra comma in American format dates
        private static readonly Regex CommaDates = new Regex(WikiRegexes.Months + @" ?, *([1-3]?\d) ?, ?((?:200|19\d)\d)\b");

        // fixes missing comma or space in American format dates
        private static readonly Regex NoCommaAmericanDates = new Regex(@"\b(" + WikiRegexes.MonthsNoGroup + @" *[1-3]?\d(?:–[1-3]?\d)?)\b *,?([12]\d{3})\b");
        private static readonly Regex NoSpaceAmericanDates = new Regex(@"\b" + WikiRegexes.Months + @"([1-3]?\d(?:–[1-3]?\d)?), *([12]\d{3})\b");

        // fix incorrect comma between month and year in Internaltional-format dates
        private static readonly Regex IncorrectCommaInternationalDates = new Regex(@"\b((?:[1-3]?\d) +" + WikiRegexes.MonthsNoGroup + @") *, *(1\d{3}|20\d{2})\b", RegexOptions.Compiled);

        // date ranges use an en-dash per [[WP:MOSDATE]]
        private static readonly Regex SameMonthInternationalDateRange = new Regex(@"\b([1-3]?\d) *- *([1-3]?\d +" + WikiRegexes.MonthsNoGroup + @")\b", RegexOptions.Compiled);
        private static readonly Regex SameMonthAmericanDateRange = new Regex(@"(" + WikiRegexes.MonthsNoGroup + @" *)([0-3]?\d) *- *([0-3]?\d)\b(?!\-)", RegexOptions.Compiled);

        // 13 July -28 July 2009 -> 13–28 July 2009
        // July 13 - July 28 2009 -> July 13–28, 2009
        private static readonly Regex LongFormatInternationalDateRange = new Regex(@"\b([1-3]?\d) +" + WikiRegexes.Months + @" *(?:-|–|&nbsp;) *([1-3]?\d) +\2,? *([12]\d{3})\b", RegexOptions.Compiled);
        private static readonly Regex LongFormatAmericanDateRange = new Regex(WikiRegexes.Months + @" +([1-3]?\d) +" + @" *(?:-|–|&nbsp;) *\1 +([1-3]?\d) *,? *([12]\d{3})\b", RegexOptions.Compiled);
        private static readonly Regex EnMonthRange = new Regex(@"\b" + WikiRegexes.Months + @"-" + WikiRegexes.Months + @"\b", RegexOptions.Compiled);

        private static readonly Regex FullYearRange = new Regex(@"((?:[\(,=;\|]|\b(?:from|between|and|reigned|f?or|ca?\.?\]*|circa)) *)([12]\d{3}) *- *([12]\d{3})(?= *(?:\)|[,;\|]|and\b|\s*$))", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex SpacedFullYearRange = new Regex(@"(?<!\b(?:ca?\.?\]*|circa) *)([12]\d{3})(?: +– *| *– +)([12]\d{3})", RegexOptions.Compiled);
        private static readonly Regex YearRangeShortenedCentury = new Regex(@"((?:[\(,=;]|\b(?:from|between|and|reigned)) *)([12]\d{3}) *- *(\d{2})(?= *(?:\)|[,;]|and\b|\s*$))", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex DateRangeToPresent = new Regex(@"\b(" + WikiRegexes.MonthsNoGroup + @"|[0-3]?\d,?) +" + @"([12]\d{3}) *- *([Pp]resent\b)", RegexOptions.Compiled);
        private static readonly Regex DateRangeToYear = new Regex(@"\b(" + WikiRegexes.MonthsNoGroup + @"|(?:&nbsp;|\s+)[0-3]?\d,?) +" + @"([12]\d{3})–([12]\d{3})\b", RegexOptions.Compiled);
        private static readonly Regex YearRangeToPresent = new Regex(@"\b([12]\d{3}) *- *([Pp]resent\b)", RegexOptions.Compiled);
        private static readonly Regex CircaLinkTemplate = new Regex(@"({{[Cc]irca}}|\[\[[Cc]irca *(?:\|[Cc]a?\.?)?\]\]|\[\[[Cc]a?\.?\]*\.?)", RegexOptions.Compiled);

        // Covered by: LinkTests.FixDates()
        /// <summary>
        /// Fix date and decade formatting errors, and replace &lt;br&gt; and &lt;p&gt; HTML tags
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public string FixDates(string articleText)
        {
            if (!Variables.LangCode.Equals("en"))
                return articleText;

            bool CircaLink = CircaLinkTemplate.IsMatch(articleText);

            articleText = HideTextImages(articleText);

            articleText = CommaDates.Replace(articleText, @"$1 $2, $3");
            articleText = IncorrectCommaInternationalDates.Replace(articleText, @"$1 $2");

            articleText = SameMonthInternationalDateRange.Replace(articleText, @"$1–$2");

            foreach (Match m in SameMonthAmericanDateRange.Matches(articleText))
            {
                int day1 = Convert.ToInt32(m.Groups[2].Value);
                int day2 = Convert.ToInt32(m.Groups[3].Value);

                if (day2 > day1)
                    articleText = articleText.Replace(m.Value, Regex.Replace(m.Value, @" *- *", @"–"));
            }

            articleText = LongFormatInternationalDateRange.Replace(articleText, @"$1–$3 $2 $4");
            articleText = LongFormatAmericanDateRange.Replace(articleText, @"$1 $2–$3, $4");

            // run this after the date range fixes
            articleText = NoCommaAmericanDates.Replace(articleText, @"$1, $2");
            articleText = NoSpaceAmericanDates.Replace(articleText, @"$1 $2, $3");

            // month range
            articleText = EnMonthRange.Replace(articleText, @"$1–$2");

            articleText = AddBackTextImages(articleText);

            // fixes bellow need full HideMore
            string articleTextRaw = articleText;
            articleText = HideMoreText(articleText);

            articleText = DateRangeToPresent.Replace(articleText, @"$1 $2 – $3");
            articleText = YearRangeToPresent.Replace(articleText, @"$1–$2");

            // 1965–1968 fixes: only appy year range fix if two years are in order
            if (!CircaLink)
            {
                articleText = FullYearRange.Replace(articleText, FullYearRangeME);
                articleText = SpacedFullYearRange.Replace(articleText, SpacedFullYearRangeME);
            }

            // 1965–68 fixes
            articleText = YearRangeShortenedCentury.Replace(articleText, YearRangeShortenedCenturyME);

            // date–year --> date – year
            articleText = DateRangeToYear.Replace(articleText, @"$1 $2 – $3");

            //Remove 2 or more <br />'s
            //This piece's existance here is counter-intuitive, but it requires HideMore()
            //and I don't want to call this slow function yet another time --MaxSem
            articleText = SyntaxRemoveBr.Replace(articleText, "\r\n\r\n");
            articleText = SyntaxRemoveParagraphs.Replace(articleText, "\r\n\r\n");
            articleText = SyntaxRegexListRowBrTagStart.Replace(articleText, "$1");

            // replace first occurrence of unlinked floruit with linked version, zeroth section only
            string zeroth = WikiRegexes.ZerothSection.Match(articleTextRaw).Value;
            if (!zeroth.Contains(@"[[floruit|fl.]]") && UnlinkedFloruit.IsMatch(zeroth))
                articleText = UnlinkedFloruit.Replace(articleText, @"([[floruit|fl.]] $1", 1);

            return AddBackMoreText(articleText);
        }

        private static string FullYearRangeME(Match m)
        {
            int year1 = Convert.ToInt32(m.Groups[2].Value), year2 = Convert.ToInt32(m.Groups[3].Value);

            if (year2 > year1 && year2 - year1 <= 300)
                return m.Groups[1].Value + m.Groups[2].Value + (m.Groups[1].Value.ToLower().Contains("c") ? @" – " : @"–") + m.Groups[3].Value;

            return m.Value;
        }

        private static string SpacedFullYearRangeME(Match m)
        {
            int year1 = Convert.ToInt32(m.Groups[1].Value), year2 = Convert.ToInt32(m.Groups[2].Value);

            if (year2 > year1 && year2 - year1 <= 300)
                return m.Groups[1].Value + @"–" + m.Groups[2].Value;

            return m.Value;
        }

        private static string YearRangeShortenedCenturyME(Match m)
        {
            int year1 = Convert.ToInt32(m.Groups[2].Value); // 1965
            int year2 = Convert.ToInt32(m.Groups[2].Value.Substring(0, 2) + m.Groups[3].Value); // 68 -> 19 || 68 -> 1968

            if (year2 > year1 && year2 - year1 <= 99)
                return m.Groups[1].Value + m.Groups[2].Value + @"–" + m.Groups[3].Value;

            return m.Value;
        }

        private static readonly Regex DiedDateRegex =
            new Regex(
                @"('''(?:[^']+|.*?[^'])'''\s*\()d\.(\s+\[*(?:" + WikiRegexes.MonthsNoGroup + @"\s+0?([1-3]?[0-9])|0?([1-3]?[0-9])\s*" +
                WikiRegexes.MonthsNoGroup + @")?\]*\s*\[*[1-2]?\d{3}\]*\)\s*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DOBRegex =
            new Regex(
                @"('''(?:[^']+|.*?[^'])'''\s*\()b\.(\s+\[*(?:" + WikiRegexes.MonthsNoGroup + @"\s+0?([1-3]?\d)|0?([1-3]?\d)\s*" +
                WikiRegexes.MonthsNoGroup + @")?\]*\s*\[*[1-2]?\d{3}\]*\)\s*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DOBRegexDash =
            new Regex(
                @"(?<!\*.*)('''(?:[^']+|.*?[^'])'''\s*\()(\[*(?:" + WikiRegexes.MonthsNoGroup + @"\s+0?([1-3]?\d)|0?([1-3]?\d)\s*" +
                WikiRegexes.MonthsNoGroup + @")?\]*\s*\[*[1-2]?\d{3}\]*)\s*(?:\-|–|&ndash;)\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex BornDeathRegex =
            new Regex(
                @"('''(?:[^']+|.*?[^'])'''\s*\()(?:[Bb]orn|b\.)\s+(\[*(?:" + WikiRegexes.MonthsNoGroup +
                @"\s+0?(?:[1-3]?\d)|0?(?:[1-3]?\d)\s*" + WikiRegexes.MonthsNoGroup +
                @")?\]*,?\s*\[*[1-2]?\d{3}\]*)\s*(.|&.dash;)\s*(?:[Dd]ied|d\.)\s+(\[*(?:" + WikiRegexes.MonthsNoGroup +
                @"\s+0?(?:[1-3]?\d)|0?(?:[1-3]?\d)\s*" + WikiRegexes.MonthsNoGroup + @")\]*,?\s*\[*[1-2]?\d{3}\]*\)\s*)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex UnlinkedFloruit = new Regex(@"\(\s*(?:[Ff]l)\.*\s*(\d\d)", RegexOptions.Compiled);

        //Covered by: LinkTests.FixLivingThingsRelatedDates()
        /// <summary>
        /// Replace b. and d. for born/died, or date– for born
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public static string FixLivingThingsRelatedDates(string articleText)
        {
            articleText = DiedDateRegex.Replace(articleText, "$1died$2"); // date of death
            articleText = DOBRegex.Replace(articleText, "$1born$2"); // date of birth
            if (!DOBRegexDash.IsMatch(WikiRegexes.InfoBox.Match(articleText).Value))
                articleText = DOBRegexDash.Replace(articleText, "$1born $2)"); // date of birth – dash
            return BornDeathRegex.Replace(articleText, "$1$2 – $4"); // birth and death
        }

        private const string InlineCitationCleanupTemplatesRp = @"(?:Author incomplete|Author missing|Citation broken|Citation not found|Clarifyref|Clarifyref2|Date missing|Episode|ISBN missing|Page needed|Publisher missing|Season needed|Time needed|Title incomplete|Title missing|Volume needed|Year missing|rp)";
        private const string OutofOrderRefs = @"(<ref\s+name\s*=\s*(?:""|')?([^<>""=]+?)(?:""|')?\s*(?:\/\s*|>[^<>]+</ref)>)(\s*{{\s*" + InlineCitationCleanupTemplatesRp + @"\s*\|[^{}]+}})?(\s*)(<ref\s+name\s*=\s*(?:""|')?([^<>""=]+?)(?:""|')?\s*(?:\/\s*|>[^<>]+</ref)>)(\s*{{\s*" + InlineCitationCleanupTemplatesRp + @"\s*\|[^{}]+}})?";
        private static readonly Regex OutofOrderRefs1 = new Regex(@"(<ref>[^<>]+</ref>)(\s*)(<ref\s+name\s*=\s*(?:""|')?([^<>""=]+?)(?:""|')?\s*(?:\/\s*|>[^<>]+</ref)>)(\s*{{\s*" + InlineCitationCleanupTemplatesRp + @"\s*\|[^{}]+}})?", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex OutofOrderRefs2 = new Regex(OutofOrderRefs, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // regex below ensures a forced match on second and third of consecutive references
        private static readonly Regex OutofOrderRefs3 = new Regex(@"(?<=\s*(?:\/\s*|>[^<>]+</ref)>\s*(?:{{\s*" + InlineCitationCleanupTemplatesRp + @"\s*\|[^{}]+}})?)" + OutofOrderRefs, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Reorders references so that they appear in numerical order, allows for use of {{rp}}, doesn't modify grouped references [[WP:REFGROUP]]
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public static string ReorderReferences(string articleText)
        {
            // do not reorder stuff in the <references>...</references> section
            int referencestags = WikiRegexes.ReferencesTemplate.Match(articleText).Index;
            if (referencestags <= 0)
                referencestags = articleText.Length;

            for (int i = 0; i < 9; i++) // allows for up to 9 consecutive references
            {
                string articleTextBefore = articleText;

                foreach (Match m in OutofOrderRefs1.Matches(articleText))
                {
                    string ref1 = m.Groups[1].Value;
                    int ref1Index = Regex.Match(articleText, @"(?si)<ref\s+name\s*=\s*(?:""|')?" + Regex.Escape(m.Groups[4].Value) + @"(?:""|')?\s*(?:\/\s*|>.+?</ref)>").Index;
                    int ref2Index = articleText.IndexOf(ref1);

                    if (ref1Index < ref2Index && ref2Index > 0 && ref1Index > 0 && m.Groups[3].Index < referencestags)
                    {
                        string whitespace = m.Groups[2].Value;
                        string rptemplate = m.Groups[5].Value;
                        string ref2 = m.Groups[3].Value;
                        articleText = articleText.Replace(ref1 + whitespace + ref2 + rptemplate, ref2 + rptemplate + whitespace + ref1);
                    }
                }

                articleText = ReorderRefs(articleText, OutofOrderRefs2, referencestags);
                articleText = ReorderRefs(articleText, OutofOrderRefs3, referencestags);

                if (articleTextBefore == articleText)
                    break;
            }

            return articleText;
        }

        private const string RefsPunctuation = @"([,\.;])";
        private static readonly Regex RefsBeforePunctuationR = new Regex(@" *" + WikiRegexes.Refs + @" *" + RefsPunctuation + @"([^,\.:;])", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex RefsBeforePunctuationQuick = new Regex(@">\s*" + RefsPunctuation, RegexOptions.Compiled);
        private static readonly Regex RefsAfterDupePunctuation = new Regex(@"([^,\.:;])" + RefsPunctuation + @"\2 *" + WikiRegexes.Refs, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Puts &lt;ref&gt; references after punctuation (comma, full stop) per WP:REFPUNC when this is the majority style in the article
        /// Applies to en-wiki only
        /// </summary>
        /// <param name="articleText">The article text</param>
        /// <returns>The updated article text</returns>
        public static string RefsAfterPunctuation(string articleText)
        {
            if (!Variables.LangCode.Equals("en") && !Variables.LangCode.Equals("el"))
                return articleText;

            // quick check of ">" followed by punctuation in article, for performance saving
            if (RefsBeforePunctuationQuick.IsMatch(articleText))
            {
                while (RefsBeforePunctuationR.IsMatch(articleText))
                {
                    articleText = RefsBeforePunctuationR.Replace(articleText, "$2$1$3");
                    articleText = RefsAfterDupePunctuation.Replace(articleText, "$1$2$3");
                }
            }

            return RefsAfterDupePunctuation.Replace(articleText, "$1$2$3");
        }

        /// <summary>
        /// reorders references within the article text based on the input regular expression providing matches for references that are out of numerical order
        /// </summary>
        /// <param name="articleText">the wiki text of the article</param>
        /// <param name="outofOrderRegex">a regular expression representing two references that are out of numerical order</param>
        /// <param name="referencestagindex"></param>
        /// <returns>the modified article text</returns>
        private static string ReorderRefs(string articleText, Regex outofOrderRegex, int referencestagindex)
        {
            foreach (Match m in outofOrderRegex.Matches(articleText))
            {
                int ref1Index = Regex.Match(articleText, @"(?si)<ref\s+name\s*=\s*(?:""|')?" + Regex.Escape(m.Groups[2].Value) + @"(?:""|')?\s*(?:\/\s*|>.+?</ref)>").Index;
                int ref2Index = Regex.Match(articleText, @"(?si)<ref\s+name\s*=\s*(?:""|')?" + Regex.Escape(m.Groups[6].Value) + @"(?:""|')?\s*(?:\/\s*|>.+?</ref)>").Index;

                if (ref1Index > ref2Index && ref1Index > 0 && m.Groups[5].Index < referencestagindex)
                {
                    string ref1 = m.Groups[1].Value;
                    string ref2 = m.Groups[5].Value;
                    string whitespace = m.Groups[4].Value;
                    string rptemplate1 = m.Groups[3].Value;
                    string rptemplate2 = m.Groups[7].Value;

                    articleText = articleText.Replace(ref1 + rptemplate1 + whitespace + ref2 + rptemplate2, ref2 + rptemplate2 + whitespace + ref1 + rptemplate1);
                }
            }

            return articleText;
        }

        private static readonly Regex LongNamedReferences = new Regex(@"(<\s*ref\s+name\s*=\s*(?:""|')?([^<>=\r\n]+?)(?:""|')?\s*>\s*([^<>]{30,}?)\s*<\s*/\s*ref>)", RegexOptions.Compiled);

        // Covered by: DuplicateNamedReferencesTests()
        /// <summary>
        /// Where an unnamed reference is a duplicate of another named reference, set the unnamed one to use the named ref
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public static string DuplicateNamedReferences(string articleText)
        {
            Dictionary<string, string> NamedRefs = new Dictionary<string, string>();
            bool reparse = false;

            for (; ; )
            {
                reparse = false;
                NamedRefs.Clear();

                foreach (Match m in WikiRegexes.NamedReferences.Matches(articleText))
                {
                    string refName = m.Groups[2].Value;
                    string namedRefValue = m.Groups[3].Value;
                    string name2 = "";

                    if (!NamedRefs.ContainsKey(namedRefValue))
                        NamedRefs.Add(namedRefValue, refName);
                    else
                    {
                        // we've already seen this reference, can condense later ones
                        NamedRefs.TryGetValue(namedRefValue, out name2);

                        if (name2.Equals(refName) && namedRefValue.Length >= 25)
                        {
                            int reflistIndex = WikiRegexes.ReferencesTemplate.Match(articleText).Index;

                            // don't condense refs in {{reflist...}}
                            if (reflistIndex > 0 && m.Index > reflistIndex)
                                continue;

                            if (m.Index > articleText.Length)
                            {
                                reparse = true;
                                break;
                            }

                            // duplicate citation fixer (both named): <ref name="Fred">(...)</ref>....<ref name="Fred">\2</ref> --> ..<ref name="Fred"/>, minimum 25 characters to avoid short refs
                            string texttomatch = articleText.Substring(0, m.Index);
                            string textaftermatch = articleText.Substring(m.Index);

                            if (textaftermatch.Contains(m.Value))
                                articleText = texttomatch + textaftermatch.Replace(m.Value, @"<ref name=""" + refName + @"""/>");
                            else
                            {
                                reparse = true;
                                break;
                            }
                        }
                    }

                    // duplicate citation fixer (first named): <ref name="Fred">(...)</ref>....<ref>\2</ref> --> ..<ref name="Fred"/>
                    // duplicate citation fixer (second named): <ref>(...)</ref>....<ref name="Fred">\2</ref> --> ..<ref name="Fred"/>
                    foreach (Match m3 in Regex.Matches(articleText, @"<\s*ref\s*>\s*" + Regex.Escape(namedRefValue) + @"\s*<\s*/\s*ref>"))
                        articleText = articleText.Replace(m3.Value, @"<ref name=""" + refName + @"""/>");
                }

                if (!reparse)
                    break;
            }

            return articleText;
        }

        private const string RefName = @"(?si)<\s*ref\s+name\s*=\s*(?:""|')?";

        /// <summary>
        /// Matches unnamed references i.e. &lt;ref>...&lt;/ref>, group 1 being the ref value
        /// </summary>
        private static readonly Regex UnnamedRef = new Regex(@"<\s*ref\s*>\s*([^<>]+)\s*<\s*/\s*ref>", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Checks for named references
        /// </summary>
        /// <param name="articleText">The article text</param>
        /// <returns>Whether the text contains named references</returns>
        public static bool HasNamedReferences(string articleText)
        {
            return WikiRegexes.NamedReferences.IsMatch(WikiRegexes.Comments.Replace(articleText, ""));
        }

        private struct Ref
        {
            public string Text;
            public string InnerText;
        }

        /// <summary>
        /// Derives and sets a reference name per [[WP:REFNAME]] for duplicate &lt;ref&gt;s
        /// </summary>
        /// <param name="articleText">the text of the article</param>
        /// <returns>the modified article text</returns>
        public static string DuplicateUnnamedReferences(string articleText)
        {
            /* On en-wiki AWB is asked not to add named references to an article if there are none currently, as some users feel
             * this is a change of citation style, so is against the [[WP:CITE]] "don't change established style" guidelines */
            if (Variables.LangCode.Equals("en") && !HasNamedReferences(articleText))
                return articleText;

            Dictionary<int, List<Ref>> refs = new Dictionary<int, List<Ref>>();
            bool haveRefsToFix = false;

            // loop through all unnamed refs, add any duplicates to dictionary
            foreach (Match m in UnnamedRef.Matches(articleText))
            {
                string fullReference = m.Value;

                // ref contains ibid/op cit, don't combine it, could refer to any ref on page
                if (WikiRegexes.IbidOpCitation.IsMatch(fullReference))
                    continue;

                string refContent = m.Groups[1].Value.Trim();
                int hash = refContent.GetHashCode();
                List<Ref> list;
                if (refs.TryGetValue(hash, out list))
                {
                    list.Add(new Ref { Text = fullReference, InnerText = refContent });
                    haveRefsToFix = true;
                }
                else
                {
                    list = new List<Ref> { new Ref { Text = fullReference, InnerText = refContent } };
                    refs.Add(hash, list);
                }
            }

            if (!haveRefsToFix)
                return articleText;

            StringBuilder result = new StringBuilder(articleText);

            // process each duplicate reference in dictionary
            foreach (KeyValuePair<int, List<Ref>> kvp in refs)
            {
                List<Ref> list = kvp.Value;
                if (list.Count < 2)
                    continue; // nothing to consolidate

                // get the reference name to use
                string friendlyName = DeriveReferenceName(articleText, list[0].InnerText);

                // check reference name not already in use for some other reference
                if (friendlyName.Length <= 3 || Regex.IsMatch(result.ToString(), RefName + Regex.Escape(friendlyName) + @"""\s*/?\s*>"))
                    continue;

                for (int i = 0; i < list.Count; i++)
                {
                    StringBuilder newValue = new StringBuilder();

                    newValue.Append(@"<ref name=""");
                    newValue.Append(friendlyName);
                    newValue.Append('"');

                    if (i == 0)
                    {
                        newValue.Append('>');
                        newValue.Append(list[0].InnerText);
                        newValue.Append("</ref>");

                        Tools.ReplaceOnce(result, list[0].Text, newValue.ToString());
                    }
                    else
                    {
                        newValue.Append("/>");
                        result.Replace(list[i].Text, newValue.ToString());
                    }
                }

                articleText = result.ToString();
            }

            return articleText;
        }

        private static readonly Regex PageRef = new Regex(@"\s*(?:(?:[Pp]ages?|[Pp][pg]?[:\.]?)|^)\s*[XVI\d]", RegexOptions.Compiled);

        /// <summary>
        /// Corrects named references where the reference is the same but the reference name is different
        /// </summary>
        /// <param name="articleText">the wiki text of the page</param>
        /// <returns>the updated wiki text</returns>
        public static string SameRefDifferentName(string articleText)
        {
            // refs with same name, but one is very short, so just change to <ref name=foo/> notation
            articleText = SameNamedRefShortText(articleText);

            Dictionary<string, string> NamedRefs = new Dictionary<string, string>();

            foreach (Match m in WikiRegexes.NamedReferences.Matches(articleText))
            {
                string refname = m.Groups[2].Value;
                string refvalue = m.Groups[3].Value;

                string existingname = "";

                if (!NamedRefs.ContainsKey(refvalue))
                {
                    NamedRefs.Add(refvalue, refname);
                    continue;
                }

                NamedRefs.TryGetValue(refvalue, out existingname);

                // don't apply to ibid short ref
                if (existingname.Length > 0 && !existingname.Equals(refname) && !WikiRegexes.IbidOpCitation.IsMatch(refvalue))
                {
                    string newRefName = refname;
                    string oldRefName = existingname;

                    // use longest ref name as the one to keep
                    if ((existingname.Length > refname.Length && !existingname.Contains("autogenerated")
                         && !existingname.Contains("ReferenceA")) || (refname.Contains("autogenerated")
                                                                     || refname.Contains("ReferenceA")))
                    {
                        newRefName = existingname;
                        oldRefName = refname;
                    }

                    Regex a = new Regex(@"<\s*ref\s+name\s*=\s*(?:""|')?" + Regex.Escape(oldRefName) + @"(?:""|')?\s*(?=/\s*>|>\s*" + Regex.Escape(refvalue) + @"\s*</ref>)");

                    articleText = a.Replace(articleText, @"<ref name=""" + newRefName + @"""");
                }
            }

            return DuplicateNamedReferences(articleText);
        }

        /// <summary>
        /// refs with same name, but one is very short, so just change to &lt;ref name=foo/&gt; notation
        /// </summary>
        /// <param name="articleText">the wiki text of the page</param>
        /// <returns>the update wiki text</returns>
        private static string SameNamedRefShortText(string articleText)
        {
            foreach (Match m in LongNamedReferences.Matches(articleText))
            {
                string refname = m.Groups[2].Value;
                string refvalue = m.Groups[3].Value;

                Regex shortNamedReferences = new Regex(@"(<\s*ref\s+name\s*=\s*(?:""|')?(" + Regex.Escape(refname) + @")(?:""|')?\s*>\s*([^<>]{1,9}?|\[?[Ss]ee above\]?)\s*<\s*/\s*ref>)");

                foreach (Match m2 in shortNamedReferences.Matches(articleText))
                {
                    // don't apply if short ref is a page ref
                    if (refvalue.Length > 30 && !PageRef.IsMatch(m2.Groups[3].Value))
                        articleText = articleText.Replace(m2.Value, @"<ref name=""" + refname + @"""/>");
                }
            }

            return articleText;
        }

        /// <summary>
        /// Extracts strings from an input string using the input regex to derive a name for a reference
        /// </summary>
        /// <param name="reference">value of the reference needing a name</param>
        /// <param name="referenceNameMask">regular expression to apply</param>
        /// <param name="components">number of groups to extract</param>
        /// <returns>the derived reference name</returns>
        private static string ExtractReferenceNameComponents(string reference, Regex referenceNameMask, int components)
        {
            string referenceName = "";

            if (referenceNameMask.Matches(reference).Count > 0)
            {
                Match m = referenceNameMask.Match(reference);

                referenceName = m.Groups[1].Value;

                if (components > 1)
                    referenceName += " " + m.Groups[2].Value;

                if (components > 2)
                    referenceName += " " + m.Groups[3].Value;
            }

            return CleanDerivedReferenceName(referenceName);
        }

        private const string CharsToTrim = @".;: {}[]|`?\/$’‘-_–=+,";
        // U230A is Floor Left; U230B is Floor Right
        private static readonly Regex CommentOrFloorNumber = new Regex(@"(\<\!--.*?--\>|" + "\u230A" + @"{3,}\d+" + "\u230B" + "{3,})", RegexOptions.Compiled);
        private static readonly Regex SequenceOfQuotesInDerivedName = new Regex(@"(''+|[“‘”""\[\]\(\)\<\>" + "\u230A\u230B" + "])", RegexOptions.Compiled);
        private static readonly Regex WhitespaceInDerivedName = new Regex(@"(\s{2,}|&nbsp;|\t|\n)", RegexOptions.Compiled);
        private static readonly Regex DateRetrievedOrAccessed = new Regex(@"(?im)(\s*(date\s+)?(retrieved|accessed)\b|^\d+$)", RegexOptions.Compiled);
        /// <summary>
        /// Removes various unwanted punctuation and comment characters from a derived reference name
        /// </summary>
        /// <param name="derivedName">the input reference name</param>
        /// <returns>the cleaned reference name</returns>
        private static string CleanDerivedReferenceName(string derivedName)
        {
            derivedName = WikiRegexes.PipedWikiLink.Replace(derivedName, "$2"); // piped wikilinks -> text value

            derivedName = CommentOrFloorNumber.Replace(derivedName, "");
            // rm comments from ref name, might be masked
            derivedName = derivedName.Trim(CharsToTrim.ToCharArray());
            derivedName = SequenceOfQuotesInDerivedName.Replace(derivedName, ""); // remove chars
            derivedName = WhitespaceInDerivedName.Replace(derivedName, " "); // spacing fixes
            derivedName = derivedName.Replace(@"&ndash;", "–");

            Parsers p = new Parsers();
            derivedName = p.FixDates(derivedName);

            return DateRetrievedOrAccessed.IsMatch(derivedName) ? "" : derivedName;
        }

        private const string NameMask = @"(?-i)\s*(?:sir)?\s*((?:[A-Z]+\.?){0,3}\s*[A-Z][\w-']{2,}[,\.]?\s*(?:\s+\w\.?|\b(?:[A-Z]+\.?){0,3})?(?:\s+[A-Z][\w-']{2,}){0,3}(?:\s+\w(?:\.?|\b)){0,2})\s*(?:[,\.'&;:\[\(“`]|et\s+al)(?i)[^{}<>\n]*?";
        private const string YearMask = @"(\([12]\d{3}\)|\b[12]\d{3}[,\.\)])";
        private const string PageMask = @"('*(?:p+g?|pages?)'*\.?'*(?:&nbsp;)?\s*(?:\d{1,3}|(?-i)[XVICM]+(?i))\.?(?:\s*[-/&\.,]\s*(?:\d{1,3}|(?-i)[XVICM]+(?i)))?\b)";

        private static readonly Regex CitationCiteBook = new Regex(@"{{[Cc]it[ae]((?>[^\{\}]+|\{(?<DEPTH>)|\}(?<-DEPTH>))*(?(DEPTH)(?!))}})", RegexOptions.Compiled);
        private static readonly Regex CiteTemplatePagesParameter = new Regex(@"(?<=\s*pages?\s*=\s*)([^{}\|<>]+?)(?=\s*(?:\||}}))", RegexOptions.Compiled);
        private static readonly Regex UrlShortDescription = new Regex(@"\s*[^{}<>\n]*?\s*\[*(?:http://www\.|http://|www\.)[^\[\]<>""\s]+?\s+([^{}<>\[\]]{4,35}?)\s*(?:\]|<!--|\u230A\u230A\u230A\u230A)", RegexOptions.Compiled);
        private static readonly Regex UrlDomain = new Regex(@"\s*\w*?[^{}<>]{0,4}?\s*(?:\[?|\{\{\s*cit[^{}<>]*\|\s*url\s*=\s*)\s*(?:http://www\.|http://|www\.)([^\[\]<>""\s\/:]+)", RegexOptions.Compiled);
        private static readonly Regex HarvnbTemplate = new Regex(@"\s*{{ *(?:[Hh]arv(?:(?:col)?(?:nb|txt)|ard citation no brackets)?|[Ss]fn)\s*\|\s*([^{}\|]+?)\s*\|(?:[^{}]*?\|)?\s*(\d{4})\s*(?:\|\s*(?:pp?\s*=\s*)?([^{}\|]+?)\s*)?}}\s*", RegexOptions.Compiled);
        private static readonly Regex WholeShortReference = new Regex(@"\s*([^<>{}]{4,35})\s*", RegexOptions.Compiled);
        private static readonly Regex CiteTemplateUrl = new Regex(@"\s*\{\{\s*cit[^{}<>]*\|\s*url\s*=\s*([^\/<>{}\|]{4,35})", RegexOptions.Compiled);
        private static readonly Regex NameYearPage = new Regex(NameMask + YearMask + @"[^{}<>\n]*?" + PageMask + @"\s*", RegexOptions.Compiled);
        private static readonly Regex NamePage = new Regex(NameMask + PageMask + @"\s*", RegexOptions.Compiled);
        private static readonly Regex NameYear = new Regex(NameMask + YearMask + @"\s*", RegexOptions.Compiled);
        private static readonly Regex CiteDOIPMID = Tools.NestedTemplateRegex(new[] { "cite doi", "cite pmid" });

        /// <summary>
        /// Derives a name for a reference by searching for author names and dates, or website base URL etc.
        /// </summary>
        /// <param name="articleText">text of article, to check the derived name is not already used for some other reference</param>
        /// <param name="reference">the value of the reference a name is needed for</param>
        /// <returns>the derived reference name, or null if none could be determined</returns>
        public static string DeriveReferenceName(string articleText, string reference)
        {
            string derivedReferenceName = "";

            // try parameters from a citation: lastname, year and page
            string citationTemplate = CitationCiteBook.Match(reference).Value;

            if (citationTemplate.Length > 10)
            {
                string last = Tools.GetTemplateParameterValue(reference, "last");

                if (last.Length < 1)
                {
                    last = Tools.GetTemplateParameterValue(reference, "author");
                }

                if (last.Length > 1)
                {
                    derivedReferenceName = last;
                    string year = Tools.GetTemplateParameterValue(reference, "year");

                    string pages = CiteTemplatePagesParameter.Match(reference).Value.Trim();

                    if (year.Length > 3)
                        derivedReferenceName += " " + year;
                    else
                    {
                        string date = YearOnly.Match(Tools.GetTemplateParameterValue(reference, "date")).Value;

                        if (date.Length > 3)
                            derivedReferenceName += " " + date;
                    }

                    if (pages.Length > 0)
                        derivedReferenceName += " " + pages;

                    derivedReferenceName = CleanDerivedReferenceName(derivedReferenceName);
                }
                // otherwise try title
                else
                {
                    string title = Tools.GetTemplateParameterValue(reference, "title");

                    if (title.Length > 3 && title.Length < 35)
                        derivedReferenceName = title;
                    derivedReferenceName = CleanDerivedReferenceName(derivedReferenceName);

                    // try publisher
                    if (derivedReferenceName.Length < 4)
                    {
                        title = Tools.GetTemplateParameterValue(reference, "publisher");

                        if (title.Length > 3 && title.Length < 35)
                            derivedReferenceName = title;
                        derivedReferenceName = CleanDerivedReferenceName(derivedReferenceName);
                    }
                }
            }

            if (ReferenceNameValid(articleText, derivedReferenceName))
                return derivedReferenceName;

            // try description of a simple external link
            derivedReferenceName = ExtractReferenceNameComponents(reference, UrlShortDescription, 1);

            if (ReferenceNameValid(articleText, derivedReferenceName))
                return derivedReferenceName;

            // website URL first, allowing a name before link
            derivedReferenceName = ExtractReferenceNameComponents(reference, UrlDomain, 1);

            if (ReferenceNameValid(articleText, derivedReferenceName))
                return derivedReferenceName;

            // Harvnb template {{Harvnb|Young|1852|p=50}}
            derivedReferenceName = ExtractReferenceNameComponents(reference, HarvnbTemplate, 3);

            if (ReferenceNameValid(articleText, derivedReferenceName))
                return derivedReferenceName;

            // cite pmid / cite doi
            derivedReferenceName = Regex.Replace(ExtractReferenceNameComponents(reference, CiteDOIPMID, 3), @"[Cc]ite (pmid|doi)\s*\|\s*", "$1");

            if (ReferenceNameValid(articleText, derivedReferenceName))
                return derivedReferenceName;

            // now just try to use the whole reference if it's short (<35 characters)
            if (reference.Length < 35)
                derivedReferenceName = ExtractReferenceNameComponents(reference, WholeShortReference, 1);

            if (ReferenceNameValid(articleText, derivedReferenceName))
                return derivedReferenceName;

            //now try title of a citation
            derivedReferenceName = ExtractReferenceNameComponents(reference, CiteTemplateUrl, 1);

            if (ReferenceNameValid(articleText, derivedReferenceName))
                return derivedReferenceName;

            // name...year...page
            derivedReferenceName = ExtractReferenceNameComponents(reference, NameYearPage, 3);

            if (ReferenceNameValid(articleText, derivedReferenceName))
                return derivedReferenceName;

            // name...page
            derivedReferenceName = ExtractReferenceNameComponents(reference, NamePage, 2);

            if (ReferenceNameValid(articleText, derivedReferenceName))
                return derivedReferenceName;

            // name...year
            derivedReferenceName = ExtractReferenceNameComponents(reference, NameYear, 2);

            if (ReferenceNameValid(articleText, derivedReferenceName))
                return derivedReferenceName;

            // generic ReferenceA
            derivedReferenceName = @"ReferenceA";

            if (ReferenceNameValid(articleText, derivedReferenceName))
                return derivedReferenceName;

            // generic ReferenceB
            derivedReferenceName = @"ReferenceB";

            if (ReferenceNameValid(articleText, derivedReferenceName))
                return derivedReferenceName;

            // generic ReferenceC
            derivedReferenceName = @"ReferenceC";

            return ReferenceNameValid(articleText, derivedReferenceName) ? derivedReferenceName : "";
        }

        /// <summary>
        /// Checks the validity of a new reference name
        /// </summary>
        /// <param name="articleText">The article text</param>
        /// <param name="derivedReferenceName">The reference name</param>
        /// <returns>Whether the article does not already have a reference of that name</returns>
        private static bool ReferenceNameValid(string articleText, string derivedReferenceName)
        {
            return !Regex.IsMatch(articleText, RefName + Regex.Escape(derivedReferenceName) + @"(?:""|')?\s*/?\s*>") && derivedReferenceName.Length >= 3;
        }

        private static readonly Regex TlOrTlx = Tools.NestedTemplateRegex(new List<string>(new[] { "tl", "tlx" }));
        private static readonly Regex TemplateRedirectsR = new Regex(@"({{ *[Tt]lx? *\|.*}}) *→[ ']*({{ *[Tt]lx? *\| *(.*?) *}})", RegexOptions.Compiled);

        /// <summary>
        /// Processes the text of [[WP:AWB/Template redirects]] into a dictionary of regexes and new template names
        /// Format: {{tl|template 1}}, {{tl|template 2}} --> {{tl|actual template}}
        /// </summary>
        public static Dictionary<Regex, string> LoadTemplateRedirects(string text)
        {
            text = WikiRegexes.UnformattedText.Replace(text, "");
            Dictionary<Regex, string> TRs = new Dictionary<Regex, string>();

            foreach (Match m in TemplateRedirectsR.Matches(text))
            {
                string redirects = m.Groups[1].Value, templateName = m.Groups[2].Value;
                templateName = TlOrTlx.Match(templateName).Groups[3].Value.Trim('|').TrimEnd('}').Trim();

                // get all redirects into a list
                List<string> redirectsList = new List<string>();

                foreach (Match r in TlOrTlx.Matches(redirects))
                {
                    redirectsList.Add(r.Groups[3].Value.Trim('|').TrimEnd('}').Trim());
                }

                TRs.Add(Tools.NestedTemplateRegex(redirectsList), templateName);
            }

            return TRs;
        }

        /// <summary>
        /// Processes the text of [[WP:AWB/Dated templates]] into a list of regexes to match each template
        /// Format: * {{tl|Wikify}}
        /// </summary>
        /// <param name="text">The rule page text</param>
        /// <returns>List of regexes to match dated templates</returns>
        public static List<Regex> LoadDatedTemplates(string text)
        {
            text = WikiRegexes.UnformattedText.Replace(text, "");
            List<Regex> DTs = new List<Regex>();

            foreach (Match m in TlOrTlx.Matches(text))
            {
                string templateName = m.Groups[3].Value.Trim('|').TrimEnd('}').Trim();
                DTs.Add(Tools.NestedTemplateRegex(templateName));
            }

            return DTs;
        }

        /// <summary>
        /// Renames templates to bypass template redirects from [[WP:AWB/Template redirects]]
        /// The first letter casing of the existing redirect is kept in the new template name,
        ///  except for acronym templates where first letter uppercase is enforced
        /// </summary>
        /// <param name="articleText">the page text</param>
        /// <param name="TemplateRedirects">Dictionary of redirects and templates</param>
        /// <returns>The updated article text</returns>
        public static string TemplateRedirects(string articleText, Dictionary<Regex, string> TemplateRedirects)
        {
            foreach (KeyValuePair<Regex, string> kvp in TemplateRedirects)
            {
                articleText = kvp.Key.Replace(articleText, m => TemplateRedirectsME(m, kvp.Value));
            }

            return articleText;
        }

        private static readonly Regex AcronymTemplate = new Regex(@"^[A-Z]{3}", RegexOptions.Compiled);

        private static string TemplateRedirectsME(Match m, string newTemplateName)
        {
            string originalTemplateName = m.Groups[2].Value;

            if (!AcronymTemplate.IsMatch(newTemplateName))
            {
                if (Tools.TurnFirstToUpper(originalTemplateName).Equals(originalTemplateName))
                    newTemplateName = Tools.TurnFirstToUpper(newTemplateName);
                else
                    newTemplateName = Tools.TurnFirstToLower(newTemplateName);
            }

            return (m.Groups[1].Value + newTemplateName + m.Groups[3].Value);
        }

        private static Regex RenameTemplateParametersTemplates;

        /// <summary>
        /// Renames parameters in template calls
        /// </summary>
        /// <param name="articleText">The wiki text</param>
        /// <param name="RenamedTemplateParameters">List of templates, old parameter, new parameter</param>
        /// <returns>The updated wiki text</returns>
        public static string RenameTemplateParameters(string articleText, List<WikiRegexes.TemplateParameters> RenamedTemplateParameters)
        {
            if (RenamedTemplateParameters.Count == 0)
            {
                return articleText;
            }
            if (RenameTemplateParametersTemplates == null)
            {
                List<string> Templates = new List<string>();

                foreach (WikiRegexes.TemplateParameters Params in RenamedTemplateParameters)
                {
                    if (!Templates.Contains(Params.TemplateName))
                        Templates.Add(Params.TemplateName);
                }

                RenameTemplateParametersTemplates = Tools.NestedTemplateRegex(Templates);
            }
            return RenameTemplateParametersTemplates.Replace(articleText,
                                                             m =>
                                                             RenameTemplateParametersME(m, RenamedTemplateParameters));
        }

        private static string RenameTemplateParametersME(Match m, List<WikiRegexes.TemplateParameters> RenamedTemplateParameters)
        {
            string templatename = Tools.TurnFirstToLower(Tools.GetTemplateName(m.Value));
            string newvalue = m.Value;

            foreach (WikiRegexes.TemplateParameters Params in RenamedTemplateParameters)
            {
                if (Params.TemplateName.Equals(templatename) && newvalue.Contains(Params.OldParameter))
                    newvalue = Tools.RenameTemplateParameter(newvalue, Params.OldParameter, Params.NewParameter);
            }
            return newvalue;
        }

        /// <summary>
        /// Loads List of templates, old parameter, new parameter from within {{AWB rename template parameter}}
        /// </summary>
        /// <param name="text">Source page of {{AWB rename template parameter}} rules</param>
        /// <returns>List of templates, old parameter, new parameter</returns>
        public static List<WikiRegexes.TemplateParameters> LoadRenamedTemplateParameters(string text)
        {
            text = WikiRegexes.UnformattedText.Replace(text, "");
            List<WikiRegexes.TemplateParameters> TPs = new List<WikiRegexes.TemplateParameters>();

            foreach (Match m in Tools.NestedTemplateRegex("AWB rename template parameter").Matches(text))
            {
                string templatename = Tools.TurnFirstToLower(Tools.GetTemplateArgument(m.Value, 1)), oldparam = Tools.GetTemplateArgument(m.Value, 2),
                newparam = Tools.GetTemplateArgument(m.Value, 3);

                WikiRegexes.TemplateParameters Params;
                Params.TemplateName = templatename;
                Params.OldParameter = oldparam;
                Params.NewParameter = newparam;

                TPs.Add(Params);
            }

            return TPs;
        }

        /// <summary>
        /// Corrects common formatting errors in dates in external reference citation templates (doesn't link/delink dates)
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="noChange"></param>
        /// <returns>The modified article text.</returns>
        public string CiteTemplateDates(string articleText, out bool noChange)
        {
            string newText = CiteTemplateDates(articleText);

            noChange = (newText == articleText);

            return newText;
        }

        private static readonly Regex CiteWeb = Tools.NestedTemplateRegex(new[] { "cite web", "citeweb" });
        private static readonly Regex CitationPopulatedParameter = new Regex(@"\|\s*([\w_0-9-]+)\s*=\s*([^\|}]+)");

        private static readonly Regex citeWebParameters = new Regex(@"\b(accessdate|archivedate|archiveurl|arxiv|asin|at|author\d?|authorlink\d?|bibcode|coauthors?|date|deadurl|doi|doibroken|editor|editor1?-first|editor2-first|editor3-first|editor4-first|editor1?-last|editor2-last|editor3-last|editor4-last|editor1?-link|editor2-link|editor3-link|editor4-link|first\d?|format|id|isbn|issn|jfm|jstor|language|last\d?|lccn|location|month|mr|oclc|ol|osti|pages?|pmc|pmid|postscript|publisher|quote|ref|rfc|separator|ssrn|title|trans_title|type|url|work|year|zbl)\b", RegexOptions.Compiled);

        /// <summary>
        /// Searches for unknown/invalid parameters within citation templates
        /// </summary>
        /// <param name="articleText">the wiki text to search</param>
        /// <returns>Dictionary of parameter index in wiki text, and parameter length</returns>
        public static Dictionary<int, int> BadCiteParameters(string articleText)
        {
            Dictionary<int, int> found = new Dictionary<int, int>();

            // unknown parameters in cite web
            foreach (Match m in CiteWeb.Matches(articleText))
            {
                // ignore parameters in templates within cite
                string cite = @"{{" + Tools.ReplaceWithSpaces(m.Value.Substring(2), WikiRegexes.NestedTemplates.Matches(m.Value.Substring(2)));

                foreach (Match m2 in CitationPopulatedParameter.Matches(cite))
                {
                    if (!citeWebParameters.IsMatch(m2.Groups[1].Value) && Tools.GetTemplateParameterValue(cite, m2.Groups[1].Value).Length > 0)
                        found.Add(m.Index + m2.Groups[1].Index, m2.Groups[1].Length);
                }
            }

            foreach (Match m in WikiRegexes.CiteTemplate.Matches(articleText))
            {
                string pipecleaned = Tools.PipeCleanedTemplate(m.Value, false);

                // no equals between two separator pipes
                if (Regex.Matches(pipecleaned, @"=").Count > 0)
                {
                    int noequals = Regex.Match(pipecleaned, @"\|[^=]+?\|").Index;

                    if (noequals > 0)
                        found.Add(m.Index + noequals, Regex.Match(pipecleaned, @"\|[^=]+?\|").Value.Length);
                }

                // URL has space in it
                string URL = Tools.GetTemplateParameterValue(m.Value, "url");
                if (WikiRegexes.UnformattedText.Replace(WikiRegexes.NestedTemplates.Replace(URL, ""), "").Trim().Contains(" "))
                    found.Add(m.Index + m.Value.IndexOf(URL), URL.Length);
            }

            return found;
        }

        /// <summary>
        /// Searches for {{dead link}}s
        /// </summary>
        /// <param name="articleText">The article text</param>
        /// <returns>Dictionary of dead links found</returns>
        public static Dictionary<int, int> DeadLinks(string articleText)
        {
            articleText = Tools.ReplaceWithSpaces(articleText, WikiRegexes.Comments);
            Dictionary<int, int> found = new Dictionary<int, int>();

            foreach (Match m in WikiRegexes.DeadLink.Matches(articleText))
            {
                found.Add(m.Index, m.Length);
            }

            return found;
        }

        private const string SiCitStart = @"(?si)(\|\s*";
        private const string CitAccessdate = SiCitStart + @"(?:access|archive)date\s*=\s*";
        private const string CitDate = SiCitStart + @"(?:archive|air)?date2?\s*=\s*";
        //private const string CitYMonthD = SiCitStart + @"(?:archive|air|access)?date2?\s*=\s*\d{4})[-/\s]";
        //private const string dTemEnd = @"?[-/\s]([0-3]?\d\s*(?:\||}}))";

        private static readonly Regex AccessOrArchiveDate = new Regex(@"\b(access|archive)date\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly RegexReplacement[] CiteTemplateIncorrectISOAccessdates = new[] {
            new RegexReplacement(CitAccessdate + @")(1[0-2])[/_\-\.]?(1[3-9])[/_\-\.]?(?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$4-$2-$3"),
            new RegexReplacement(CitAccessdate + @")(1[0-2])[/_\-\.]?([2-3]\d)[/_\-\.]?(?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$4-$2-$3"),
            new RegexReplacement(CitAccessdate + @")(1[0-2])[/_\-\.]?\2[/_\-\.]?(?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$3-$2-$2"), // nn-nn-2004 and nn-nn-04 to ISO format (both nn the same)
            new RegexReplacement(CitAccessdate + @")(1[3-9])[/_\-\.]?(1[0-2])[/_\-\.]?(?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$4-$3-$2"),
            new RegexReplacement(CitAccessdate + @")(1[3-9])[/_\-\.]?0?([1-9])[/_\-\.]?(?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$4-0$3-$2"),
            new RegexReplacement(CitAccessdate + @")(20[01]\d)0?([01]\d)[/_\-\.]([0-3]\d\s*(?:\||}}))", "$1$2-$3-$4"),
            new RegexReplacement(CitAccessdate + @")(20[01]\d)[/_\-\.]([01]\d)0?([0-3]\d\s*(?:\||}}))", "$1$2-$3-$4"),
            new RegexReplacement(CitAccessdate + @")(20[01]\d)[/_\-\.]?([01]\d)[/_\-\.]?([1-9]\s*(?:\||}}))", "$1$2-$3-0$4"),
            new RegexReplacement(CitAccessdate + @")(20[01]\d)[/_\-\.]?([1-9])[/_\-\.]?([0-3]\d\s*(?:\||}}))", "$1$2-0$3-$4"),
            new RegexReplacement(CitAccessdate + @")(20[01]\d)[/_\-\.]?([1-9])[/_\-\.]0?([1-9]\s*(?:\||}}))", "$1$2-0$3-0$4"),
            new RegexReplacement(CitAccessdate + @")(20[01]\d)[/_\-\.]0?([1-9])[/_\-\.]([1-9]\s*(?:\||}}))", "$1$2-0$3-0$4"),
            new RegexReplacement(CitAccessdate + @")(20[01]\d)[/_\.]?([01]\d)[/_\.]?([0-3]\d\s*(?:\||}}))", "$1$2-$3-$4"),

            new RegexReplacement(CitAccessdate + @")([2-3]\d)[/_\-\.](1[0-2])[/_\-\.]?(?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$4-$3-$2"),
            new RegexReplacement(CitAccessdate + @")([2-3]\d)[/_\-\.]0?([1-9])[/_\-\.](?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$4-0$3-$2"),
            new RegexReplacement(CitAccessdate + @")0?([1-9])[/_\-\.]?(1[3-9]|[2-3]\d)[/_\-\.]?(?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$4-0$2-$3"),
            new RegexReplacement(CitAccessdate + @")0?([1-9])[/_\-\.]?0?\2[/_\-\.]?(?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$3-0$2-0$2"), // n-n-2004 and n-n-04 to ISO format (both n the same)
        };

        private static readonly Regex CiteTemplateArchiveAirDate = new Regex(@"{{\s*cit[^{}]*\|\s*(?:archive|air)?date2?\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly RegexReplacement[] CiteTemplateIncorrectISODates = new[] {
            new RegexReplacement(CitDate + @"\[?\[?)(20\d\d|19[7-9]\d)[/_]?([0-1]\d)[/_]?([0-3]\d\s*(?:\||}}))", "$1$2-$3-$4"),
            new RegexReplacement(CitDate + @"\[?\[?)(1[0-2])[/_\-\.]?([2-3]\d)[/_\-\.]?(19[7-9]\d)(?=\s*(?:\||}}))", "$1$4-$2-$3"),
            new RegexReplacement(CitDate + @"\[?\[?)0?([1-9])[/_\-\.]?([2-3]\d)[/_\-\.]?(19[7-9]\d)(?=\s*(?:\||}}))", "$1$4-0$2-$3"),
            new RegexReplacement(CitDate + @"\[?\[?)([2-3]\d)[/_\-\.]?0?([1-9])[/_\-\.]?(19[7-9]\d)(?=\s*(?:\||}}))", "$1$4-0$3-$2"),
            new RegexReplacement(CitDate + @"\[?\[?)([2-3]\d)[/_\-\.]?(1[0-2])[/_\-\.]?(19[7-9]\d)(?=\s*(?:\||}}))", "$1$4-$3-$2"),
            new RegexReplacement(CitDate + @"\[?\[?)(1[0-2])[/_\-\.]([2-3]\d)[/_\-\.](?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$4-$2-$3"),
            new RegexReplacement(CitDate + @"\[?\[?)0?([1-9])[/_\-\.]([2-3]\d)[/_\-\.](?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$4-0$2-$3"),
            new RegexReplacement(CitDate + @"\[?\[?)([2-3]\d)[/_\-\.]0?([1-9])[/_\-\.](?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$4-0$3-$2"),
            new RegexReplacement(CitDate + @"\[?\[?)([2-3]\d)[/_\-\.](1[0-2])[/_\-\.]?(?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$4-$3-$2"),
            new RegexReplacement(CitDate + @"\[?\[?)(1[0-2])[/_\-\.]?(1[3-9])[/_\-\.]?(19[7-9]\d)(?=\s*(?:\||}}))", "$1$4-$2-$3"),
            new RegexReplacement(CitDate + @"\[?\[?)0?([1-9])[/_\-\.](1[3-9])[/_\-\.](19[7-9]\d|20\d\d)(?=\s*(?:\||}}))", "$1$4-0$2-$3"),
            new RegexReplacement(CitDate + @"\[?\[?)(1[3-9])[/_\-\.]?0?([1-9])[/_\-\.]?(19[7-9]\d)(?=\s*(?:\||}}))", "$1$4-0$3-$2"),
            new RegexReplacement(CitDate + @"\[?\[?)(1[3-9])[/_\-\.]?(1[0-2])[/_\-\.]?(19[7-9]\d)(?=\s*(?:\||}}))", "$1$4-$3-$2"),
            new RegexReplacement(CitDate + @"\[?\[?)(1[0-2])[/_\-\.](1[3-9])[/_\-\.](?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$4-$2-$3"),
            new RegexReplacement(CitDate + @"\[?\[?)([1-9])[/_\-\.](1[3-9])[/_\-\.](?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$4-0$2-$3"),
            new RegexReplacement(CitDate + @"\[?\[?)(1[3-9])[/_\-\.]([1-9])[/_\-\.](?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$4-0$3-$2"),
            new RegexReplacement(CitDate + @"\[?\[?)(1[3-9])[/_\-\.](1[0-2])[/_\-\.](?:20)?([01]\d)(?=\s*(?:\||}}))", "${1}20$4-$3-$2"),
            new RegexReplacement(CitDate + @")0?([1-9])[/_\-\.]0?\2[/_\-\.](20\d\d|19[7-9]\d)(?=\s*(?:\||}}))", "$1$3-0$2-0$2"), // n-n-2004 and n-n-1980 to ISO format (both n the same)
            new RegexReplacement(CitDate + @")0?([1-9])[/_\-\.]0?\2[/_\-\.]([01]\d)(?=\s*(?:\||}}))", "${1}20$3-0$2-0$2"), // n-n-04 to ISO format (both n the same)
            new RegexReplacement(CitDate + @")(1[0-2])[/_\-\.]\2[/_\-\.]?(20\d\d|19[7-9]\d)(?=\s*(?:\||}}))", "$1$3-$2-$2"), // nn-nn-2004 and nn-nn-1980 to ISO format (both nn the same)
            new RegexReplacement(CitDate + @")(1[0-2])[/_\-\.]\2[/_\-\.]([01]\d)(?=\s*(?:\||}}))", "${1}20$3-$2-$2"), // nn-nn-04 to ISO format (both nn the same)
            new RegexReplacement(CitDate + @")((?:\[\[)?20\d\d|1[5-9]\d{2})[/_\-\.]([1-9])[/_\-\.]0?([1-9](?:\]\])?\s*(?:\||}}))", "$1$2-0$3-0$4"),
            new RegexReplacement(CitDate + @")((?:\[\[)?20\d\d|1[5-9]\d{2})[/_\-\.]0?([1-9])[/_\-\.]([1-9](?:\]\])?\s*(?:\||}}))", "$1$2-0$3-0$4"),
            new RegexReplacement(CitDate + @")((?:\[\[)?20\d\d|1[5-9]\d{2})[/_\-\.]?([0-1]\d)[/_\-\.]?([1-9](?:\]\])?\s*(?:\||}}))", "$1$2-$3-0$4"),
            new RegexReplacement(CitDate + @")((?:\[\[)?20\d\d|1[5-9]\d{2})[/_\-\.]?([1-9])[/_\-\.]?([0-3]\d(?:\]\])?\s*(?:\||}}))", "$1$2-0$3-$4"),
            new RegexReplacement(CitDate + @")((?:\[\[)?20\d\d|1[5-9]\d{2})([0-1]\d)[/_\-\.]([0-3]\d(?:\]\])?\s*(?:\||}}))", "$1$2-$3-$4"),
            new RegexReplacement(CitDate + @")((?:\[\[)?20\d\d|1[5-9]\d{2})[/_\-\.](0[1-9]|1[0-2])0?([0-3]\d(?:\]\])?\s*(?:\||}}))", "$1$2-$3-$4"),
        };

        private static readonly Regex CiteTemplateAbbreviatedMonthISO = new Regex(@"(?si)(\|\s*(?:archive|air|access)?date2?\s*=\s*)(\d{4}[-/\s][A-Z][a-z]+\.?[-/\s][0-3]?\d)(\s*(?:\||}}))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex CiteTemplateDateYYYYDDMMFormat = new Regex(SiCitStart + @"(?:archive|air|access)?date2?\s*=\s*(?:\[\[)?20\d\d)-([2-3]\d|1[3-9])-(0[1-9]|1[0-2])(\]\])?", RegexOptions.Compiled);
        private static readonly Regex CiteTemplateTimeInDateParameter = new Regex(@"(\{\{\s*cite[^\{\}]*\|\s*(?:archive|air|access)?date2?\s*=\s*(?:(?:20\d\d|19[7-9]\d)-[01]?\d-[0-3]?\d|[0-3]?\d\s*\w+,?\s*(?:20\d\d|19[7-9]\d)|\w+\s*[0-3]?\d,?\s*(?:20\d\d|19[7-9]\d)))(\s*[,-:]?\s+[0-2]?\d[:\.]?[0-5]\d(?:\:?[0-5]\d)?\s*(?:[^\|\}]*\[\[[^[\]\n]+(?<!\[\[[A-Z]?[a-z-]{2,}:[^[\]\n]+)\]\][^\|\}]*|[^\|\}]*)?)(?<!.*(?:20|1[7-9])\d+\s*)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex CitePodcast = Tools.NestedTemplateRegex("cite podcast");

        /// <summary>
        /// Corrects common formatting errors in dates in external reference citation templates (doesn't link/delink dates)
        /// note some incorrect date formats such as 3-2-2009 are ambiguous as could be 3-FEB-2009 or MAR-2-2009, these fixes don't address such errors
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public static string CiteTemplateDates(string articleText)
        {
            if (!Variables.IsWikipediaEN)
                return articleText;

            // cite podcast is non-compliant to citation core standards; don't apply fixes when ambiguous dates present
            if (CitePodcast.IsMatch(articleText) || AmbiguousCiteTemplateDates(articleText))
                return articleText;

            string articleTextlocal = "";

            // loop in case a single citation has multiple dates to be fixed
            while (!articleTextlocal.Equals(articleText))
            {
                articleTextlocal = articleText;

                // loop in case a single citation has multiple dates to be fixed
                foreach (Match m in WikiRegexes.CiteTemplate.Matches(articleText))
                {
                    string at = m.Value;

                    // convert invalid date formats like DD-MM-YYYY, MM-DD-YYYY, YYYY-D-M, YYYY-DD-MM, YYYY_MM_DD etc. to iso format of YYYY-MM-DD
                    // for accessdate= and archivedate=
                    // provided no ambiguous ones
                    if (AccessOrArchiveDate.IsMatch(at))
                        foreach (RegexReplacement rr in CiteTemplateIncorrectISOAccessdates)
                            at = rr.Regex.Replace(at, rr.Replacement);

                    // date=, archivedate=, airdate=, date2=
                    if (CiteTemplateArchiveAirDate.IsMatch(at))
                    {
                        foreach (RegexReplacement rr in CiteTemplateIncorrectISODates)
                            at = rr.Regex.Replace(at, rr.Replacement);

                        // date = YYYY-Month-DD fix, not for cite journal PubMed date format
                        if (Tools.GetTemplateParameterValue(m.Value, "journal").Length == 0)
                            at = CiteTemplateAbbreviatedMonthISO.Replace(at, m2 => m2.Groups[1].Value + Tools.ConvertDate(m2.Groups[2].Value.Replace(".", ""), DateLocale.ISO) + m2.Groups[3].Value);
                    }

                    if (!at.Equals(m.Value))
                        articleText = articleText.Replace(m.Value, at);
                }

                // all citation dates
                articleText = CiteTemplateDateYYYYDDMMFormat.Replace(articleText, "$1-$3-$2$4"); // YYYY-DD-MM to YYYY-MM-DD
                articleText = CiteTemplateTimeInDateParameter.Replace(articleText, "$1<!--$2-->"); // Removes time from date fields
            }
            return articleText;
        }

        private static readonly Regex PossibleAmbiguousCiteDate = new Regex(@"(?<={{\s*[Cc]it[ae][^{}]+?\|\s*(?:access|archive|air)?date2?\s*=\s*)(0?[1-9]|1[0-2])[/_\-\.](0?[1-9]|1[0-2])[/_\-\.](20\d\d|19[7-9]\d|[01]\d)\b");

        /// <summary>
        /// Returns whether the input article text contains ambiguous cite template dates in XX-XX-YYYY or XX-XX-YY format
        /// </summary>
        /// <param name="articleText">the article text to search</param>
        /// <returns>If any matches were found</returns>
        public static bool AmbiguousCiteTemplateDates(string articleText)
        {
            return AmbigCiteTemplateDates(articleText).Count > 0;
        }

        /// <summary>
        /// Checks position of See also section relative to Notes, references, external links sections
        /// </summary>
        /// <param name="articleText">The article text</param>
        /// <returns>Whether 'see also' is after any of the sections</returns>
        public static bool HasSeeAlsoAfterNotesReferencesOrExternalLinks(string articleText)
        {
            int seeAlso = WikiRegexes.SeeAlso.Match(articleText).Index;
            if (seeAlso <= 0)
                return false;

            int externalLinks = WikiRegexes.ExternalLinksHeaderRegex.Match(articleText).Index;
            if (externalLinks > 0 && seeAlso > externalLinks)
                return true;

            int references = WikiRegexes.ReferencesRegex.Match(articleText).Index;
            if (references > 0 && seeAlso > references)
                return true;

            int notes = WikiRegexes.NotesHeading.Match(articleText).Index;
            if (notes > 0 && seeAlso > notes)
                return true;

            return false;
        }

        private static readonly Regex MathSourceCodeNowikiPreTag = new Regex(@"<\s*/?\s*(?:math|(?:source|ref)\b[^>]*|code|nowiki|pre|small)\s*(?:>|$)", RegexOptions.Compiled | RegexOptions.Multiline);
        /// <summary>
        ///  Searches for any unclosed &lt;math&gt;, &lt;source&gt;, &lt;ref&gt;, &lt;code&gt;, &lt;nowiki&gt;, &lt;small&gt; or &lt;pre&gt; tags
        /// </summary>
        /// <param name="articleText">The article text</param>
        /// <returns>dictionary of the index and length of any unclosed tags</returns>
        public static Dictionary<int, int> UnclosedTags(string articleText)
        {
            Dictionary<int, int> back = new Dictionary<int, int>();

            // clear out all the matched tags
            articleText = Tools.ReplaceWithSpaces(articleText, WikiRegexes.UnformattedText);
            articleText = Tools.ReplaceWithSpaces(articleText, WikiRegexes.Code);
            articleText = Tools.ReplaceWithSpaces(articleText, WikiRegexes.Source);
            articleText = Tools.ReplaceWithSpaces(articleText, WikiRegexes.Small);
            articleText = Tools.ReplaceWithSpaces(articleText, WikiRegexes.Refs);

            foreach (Match m in MathSourceCodeNowikiPreTag.Matches(articleText))
            {
                back.Add(m.Index, m.Length);
            }
            return back;
        }

        /// <summary>
        /// Returns whether the input article text contains ambiguous cite template dates in XX-XX-YYYY or XX-XX-YY format
        /// </summary>
        /// <param name="articleText">The article text to search</param>
        /// <returns>A dictionary of matches (index and length)</returns>
        public static Dictionary<int, int> AmbigCiteTemplateDates(string articleText)
        {
            Dictionary<int, int> ambigDates = new Dictionary<int, int>();

            foreach (Match m in PossibleAmbiguousCiteDate.Matches(articleText))
            {
                // for YYYY-AA-BB date, ambiguous if AA and BB not the same
                if (m.Groups[1].Value != m.Groups[2].Value)
                    ambigDates.Add(m.Index, m.Length);
            }

            return ambigDates;
        }

        private static readonly Regex PageRangeIncorrectMdash = new Regex(@"(pages\s*=\s*|pp\.?\s*)((?:&nbsp;)?\d+\s*)(?:\-\-?|—|&mdash;|&#8212;)(\s*\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // avoid dimensions in format 55-66-77
        private static readonly Regex UnitTimeRangeIncorrectMdash = new Regex(@"(?<!-)(\b[1-9]?\d+\s*)(?:-|—|&mdash;|&#8212;)(\s*[1-9]?\d+)(\s+|&nbsp;)((?:years|months|weeks|days|hours|minutes|seconds|[km]g|kb|[ckm]?m|[Gk]?Hz|miles|mi\.|%|feet|foot|ft|met(?:er|re)s)\b|in\))", RegexOptions.Compiled);
        private static readonly Regex DollarAmountIncorrectMdash = new Regex(@"(\$[1-9]?\d{1,3}\s*)(?:-|—|&mdash;|&#8212;)(\s*\$?[1-9]?\d{1,3})", RegexOptions.Compiled);
        private static readonly Regex AMPMIncorrectMdash = new Regex(@"([01]?\d:[0-5]\d\s*([AP]M)\s*)(?:-|—|&mdash;|&#8212;)(\s*[01]?\d:[0-5]\d\s*([AP]M))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex AgeIncorrectMdash = new Regex(@"([Aa]ge[sd])\s([1-9]?\d\s*)(?:-|—|&mdash;|&#8212;)(\s*[1-9]?\d)", RegexOptions.Compiled);
        private static readonly Regex SentenceClauseIncorrectMdash = new Regex(@"(?!xn--)(\w{2}|⌊⌊⌊⌊M\d+⌋⌋⌋⌋)\s*--\s*(\w)", RegexOptions.Compiled);
        private static readonly Regex SuperscriptMinus = new Regex(@"(?<=<sup>)(?:-|–|—)(?=\d+</sup>)", RegexOptions.Compiled);

        // Covered by: FormattingTests.TestMdashes()
        /// <summary>
        /// Replaces hyphens and em-dashes with en-dashes, per [[WP:DASH]]
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">The article's title</param>
        /// <returns>The modified article text.</returns>
        public string Mdashes(string articleText, string articleTitle)
        {
            articleText = HideMoreText(articleText);

            // replace hyphen with dash and convert Pp. to pp.
            foreach (Match m in PageRangeIncorrectMdash.Matches(articleText))
            {
                string pagespart = m.Groups[1].Value;
                if (pagespart.Contains(@"Pp"))
                    pagespart = pagespart.ToLower();

                articleText = articleText.Replace(m.Value, pagespart + m.Groups[2].Value + @"–" + m.Groups[3].Value);
            }

            articleText = PageRangeIncorrectMdash.Replace(articleText, @"$1$2–$3");
            articleText = UnitTimeRangeIncorrectMdash.Replace(articleText, @"$1–$2$3$4");
            articleText = DollarAmountIncorrectMdash.Replace(articleText, @"$1–$2");
            articleText = AMPMIncorrectMdash.Replace(articleText, @"$1–$3");
            articleText = AgeIncorrectMdash.Replace(articleText, @"$1 $2–$3");

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Match_en_dashes.2Femdashs_from_titles_with_those_in_the_text
            // if title has en or em dashes, apply them to strings matching article title but with hyphens
            if (articleTitle.Contains(@"–") || articleTitle.Contains(@"—"))
                articleText = Regex.Replace(articleText, Regex.Escape(articleTitle.Replace(@"–", @"-").Replace(@"—", @"-")), articleTitle);

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests/Archive_5#Change_--_.28two_dashes.29_to_.E2.80.94_.28em_dash.29
            // convert two dashes to emdash if surrouned by alphanumeric characters, except convert to endash if surrounded by numbers
            if (Namespace.Determine(articleTitle) == Namespace.Mainspace)
                articleText = SentenceClauseIncorrectMdash.Replace(articleText, m => m.Groups[1].Value + ((Regex.IsMatch(m.Groups[1].Value, @"^\d+$") && Regex.IsMatch(m.Groups[2].Value, @"^\d+$")) ? @"–" : @"—") + m.Groups[2].Value);

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#minuses
            // replace hyphen or en-dash or emdash with Unicode minus (&minus;)
            // [[Wikipedia:MOSNUM#Common_mathematical_symbols]]
            articleText = SuperscriptMinus.Replace(articleText, "−");

            return AddBackMoreText(articleText);
        }

        // Covered by: FootnotesTests.TestFixReferenceListTags()
        private static string ReflistMatchEvaluator(Match m)
        {
            // don't change anything if div tags mismatch
            if (DivStart.Matches(m.Value).Count != DivEnd.Matches(m.Value).Count)
                return m.Value;

            if (m.Value.Contains("references-2column") || m.Value.Contains("column-count:2"))
                return "{{Reflist|2}}";

            return "{{Reflist}}";
        }

        /// <summary>
        /// Main regex for {{Reflist}} converter
        /// </summary>
        private static readonly Regex ReferenceListTags = new Regex(@"(<(span|div)( class=""(references-small|small|references-2column)|)?""(?:\s*style\s*=\s*""[^<>""]+?""\s*)?>[\r\n\s]*){1,2}[\r\n\s]*<references[\s]?/>([\r\n\s]*</(span|div)>){1,2}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex DivStart = new Regex(@"<div\b.*?>", RegexOptions.Compiled);
        private static readonly Regex DivEnd = new Regex(@"< ?/ ?div\b.*?>", RegexOptions.Compiled);

        // Covered by: FootnotesTests.TestFixReferenceListTags()
        /// <summary>
        /// Replaces various old reference tag formats, with the new {{Reflist}}
        /// </summary>
        /// <param name="articleText">The wiki text of the article</param>
        /// <returns>The updated article text</returns>
        public static string FixReferenceListTags(string articleText)
        {
            return ReferenceListTags.Replace(articleText, new MatchEvaluator(ReflistMatchEvaluator));
        }

        private static readonly Regex EmptyReferences = new Regex(@"(<ref\s+name\s*=\s*(?:""|')?[^<>=\r\n]+?(?:""|')?)\s*(?:/\s*)?>\s*<\s*/\s*ref\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Covered by: FootnotesTests.TestSimplifyReferenceTags()
        /// <summary>
        /// Replaces reference tags in the form &lt;ref name="blah">&lt;/ref> with &lt;ref name="blah" />
        /// Removes some of the MW errors that occur from the prior
        /// </summary>
        /// <param name="articleText">The wiki text of the article</param>
        /// <returns>The updated article text</returns>
        public static string SimplifyReferenceTags(string articleText)
        {
            return EmptyReferences.Replace(articleText, @"$1 />");
        }

        private static readonly Regex LinksHeading = new Regex(@"(?sim)(==+\s*)Links(\s*==+\s*(?:^(?:\*|\d\.?)?\s*\[?\s*http://))", RegexOptions.Compiled);
        private static readonly Regex ReferencesHeadingLevel2 = new Regex(@"(?i)==\s*'*\s*References?\s*'*\s*==", RegexOptions.Compiled);
        private static readonly Regex ReferencesHeadingLevelLower = new Regex(@"(?i)(==+\s*'*\s*References?\s*'*\s*==+)", RegexOptions.Compiled);
        private static readonly Regex ExternalLinksHeading = new Regex(@"(?im)(^\s*=+\s*(?:External\s+link|Source|Web\s*link)s?\s*=)", RegexOptions.Compiled);
        private static readonly Regex ExternalLinksToReferences = new Regex(@"(?sim)(^\s*=+\s*(?:External\s+link|Source|Web\s*link)s?\s*=+.*?)(\r\n==+References==+\r\n{{Reflist}})", RegexOptions.Compiled);
        private static readonly Regex Category = new Regex(@"(?im)(^\s*\[\[\s*Category\s*:)", RegexOptions.Compiled);
        private static readonly Regex CategoryToReferences = new Regex(@"(?sim)((?:^\{\{(?![Tt]racklist\b)[^{}]+?\}\}\s*)*)(^\s*\[\[\s*Category\s*:.*?)(\r\n==+References==+\r\n{{Reflist}})", RegexOptions.Compiled);

        private static readonly Regex ReferencesMissingSlash = new Regex(@"<\s*[Rr]eferences\s*>", RegexOptions.Compiled);

        /// <summary>
        /// First checks for a &lt;references&lt; missing '/' to correct, otherwise:
        /// if the article uses cite references but has no recognised template to display the references, add {{Reflist}} in the appropriate place
        /// </summary>
        /// <param name="articleText">The wiki text of the article</param>
        /// <returns>The updated article text</returns>
        public static string AddMissingReflist(string articleText)
        {
            if (!IsMissingReferencesDisplay(articleText) || !Variables.LangCode.Equals("en"))
                return articleText;

            if (ReferencesMissingSlash.IsMatch(articleText))
                return ReferencesMissingSlash.Replace(articleText, @"<references/>");

            // Rename ==Links== to ==External links==
            articleText = LinksHeading.Replace(articleText, "$1External links$2");

            // add to any existing references section if present
            if (ReferencesHeadingLevel2.IsMatch(articleText))
                articleText = ReferencesHeadingLevelLower.Replace(articleText, "$1\r\n{{Reflist}}");
            else
            {
                articleText += "\r\n==References==\r\n{{Reflist}}";

                // try to move just above external links
                if (ExternalLinksHeading.IsMatch(articleText))
                    articleText = ExternalLinksToReferences.Replace(articleText, "$2\r\n$1");
                else if (Category.IsMatch(articleText))
                    // try to move just above categories
                    articleText = CategoryToReferences.Replace(articleText, "$3\r\n$1$2");
            }

            return articleText;
        }

        private static readonly RegexReplacement[] RefWhitespace = new[] {
            // whitespace cleaning
            new RegexReplacement(new Regex(@"<\s*(?:\s+ref\s*|\s*ref\s+)>", RegexOptions.Compiled | RegexOptions.Singleline), "<ref>"),
            new RegexReplacement(new Regex(@"<(?:\s*/(?:\s+ref\s*|\s*ref\s+)|\s+/\s*ref\s*)>", RegexOptions.Compiled | RegexOptions.Singleline), "</ref>"),
            
            // remove any spaces between consecutive references -- WP:REFPUNC
            new RegexReplacement(new Regex(@"(</ref>|<ref\s*name\s*=[^{}<>]+?\s*\/\s*>) +(?=<ref(?:\s*name\s*=[^{}<>]+?\s*\/?\s*)?>)", RegexOptions.Compiled), "$1"),
            // ensure a space between a reference and text (reference within a paragraph) -- WP:REFPUNC
            new RegexReplacement(new Regex(@"(</ref>|<ref\s*name\s*=[^{}<>]+?\s*\/\s*>)(\w)", RegexOptions.Compiled), "$1 $2"),
            // remove spaces between punctuation and references -- WP:REFPUNC
            new RegexReplacement(new Regex(@"([,\.:;]) +(?=<ref(?:\s*name\s*=[^{}<>]+?\s*\/?\s*)?>)", RegexOptions.Compiled | RegexOptions.IgnoreCase), "$1"),

            // <ref name="Fred" /ref> --> <ref name="Fred"/>
            new RegexReplacement(new Regex(@"(<\s*ref\s+name\s*=\s*""[^<>=""\/]+?"")\s*/\s*(?:ref|/)\s*>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase), "$1/>"),

            // <ref name="Fred""> --> <ref name="Fred">
            new RegexReplacement(new Regex(@"(<\s*ref\s+name\s*=\s*""[^<>=""\/]+?"")["">]\s*(/?)>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase), "$1$2>"),

            // <ref name = ”Fred”> --> <ref name="Fred">
            new RegexReplacement(new Regex(@"(<\s*ref\s+name\s*=\s*)[“‘”’]*([^<>=""\/]+?)[“‘”’]+(\s*/?>)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase), @"$1""$2""$3"),
            new RegexReplacement(new Regex(@"(<\s*ref\s+name\s*=\s*)[“‘”’]+([^<>=""\/]+?)[“‘”’]*(\s*/?>)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase), @"$1""$2""$3"),

            // <ref name = ''Fred'> --> <ref name="Fred"> (two apostrophes)
            new RegexReplacement(new Regex(@"(<\s*ref\s+name\s*=\s*)''+([^<>=""\/]+?)'+(\s*/?>)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase), @"$1""$2""$3"),

            // <ref name = 'Fred''> --> <ref name="Fred"> (two apostrophes)
            new RegexReplacement(new Regex(@"(<\s*ref\s+name\s*=\s*)'+([^<>=""\/]+?)''+(\s*/?>)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase), @"$1""$2""$3"),

            // <ref name=foo bar> --> <ref name="foo bar">, match when spaces
            new RegexReplacement(new Regex(@"(<\s*ref\s+name\s*=\s*)([^<>=""'\/]+?\s+[^<>=""'\/\s]+?)(\s*/?>)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase), @"$1""$2""$3"),

            // <ref name=foo bar> --> <ref name="foo bar">, match when non-ASCII characters ([\x00-\xff]*)
            new RegexReplacement(new Regex(@"(<\s*ref\s+name\s*=\s*)([^<>=""'\/]*?[^\x00-\xff]+?[^<>=""'\/]*?)(\s*/?>)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase), @"$1""$2""$3"),

            // <ref name=foo bar"> --> <ref name="foo bar">
            new RegexReplacement(new Regex(@"(<\s*ref\s+name\s*=\s*)['`”]*([^<>=""\/]+?)""(\s*/?>)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase), @"$1""$2""$3"),

            // <ref name="foo bar> --> <ref name="foo bar">
            new RegexReplacement(new Regex(@"(<\s*ref\s+name\s*=\s*)""([^<>=""\/]+?)['`”]*(\s*/?>)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase), @"$1""$2""$3"),

            // <ref name "foo bar"> --> <ref name="foo bar">
            new RegexReplacement(new Regex(@"(<\s*ref\s+name\s*)[\+\-]?(\s*""[^<>=""\/]+?""\s*/?>)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase), @"$1=$2"),

            // <ref "foo bar"> --> <ref name="foo bar">
            new RegexReplacement(new Regex(@"(<\s*ref\s+)(""[^<>=""\/]+?""\s*/?>)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase), "$1name=$2"),

            // ref name typos
            new RegexReplacement(new Regex(@"(<\s*ref\s+n)(me\s*=)", RegexOptions.Compiled | RegexOptions.IgnoreCase), "$1a$2"),

            // <ref>...<ref/> --> <ref>...</ref>
            new RegexReplacement(new Regex(@"(<\s*ref(?:\s+name\s*=[^<>]*?)?\s*>[^<>""]+?)<\s*ref\s*/\s*>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase), "$1</ref>"),

            // <ref>...</red> --> <ref>...</ref>
            new RegexReplacement(new Regex(@"(<\s*ref(?:\s+name\s*=[^<>]*?)?\s*>[^<>""]+?)<\s*/\s*red\s*>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase), "$1</ref>"),

            // <REF> and <Ref> to <ref>
            new RegexReplacement(new Regex(@"(<\s*\/?\s*)(?:R[Ee][Ff]|r[Ee]F)(\s*(?:>|name\s*=))"), "$1ref$2"),

            // trailing spaces at the end of a reference, within the reference
            new RegexReplacement(new Regex(@" +</ref>"), "</ref>"),
            
            // Trailing spaces at the beginning of a reference, within the reference
            new RegexReplacement(new Regex(@"(<ref[^<>\{\}\/]*>) +"), "$1"),

            // empty <ref>...</ref> tags
            new RegexReplacement(new Regex(@"<ref>\s*</ref>"), ""),
            
            // <ref name="Fred" Smith> --> <ref name="Fred Smith">
            new RegexReplacement(new Regex(@"(<\s*ref\s+name\s*=\s*""[^<>=""\/]+?)""([^<>=""\/]{2,}?)(?<!\s+)(?=\s*/?>)", RegexOptions.Compiled | RegexOptions.IgnoreCase), @"$1$2"""),
            
            // <ref name-"Fred"> --> <ref name="Fred">
            new RegexReplacement(new Regex(@"(<\s*ref\s+name\s*)-", RegexOptions.Compiled), "$1="),
            
            // <ref NAME= --> <ref name=
            new RegexReplacement(new Regex(@"<\s*ref\s+NAME(\s*=)", RegexOptions.Compiled), "<ref name$1"),
            
            // empty ref name: <ref name=>
            new RegexReplacement(new Regex(@"<\s*ref\s+name\s*=\s*>", RegexOptions.Compiled), "<ref>"),
        };
        // Covered by TestFixReferenceTags
        /// <summary>
        /// Various fixes to the formatting of &lt;ref&gt; reference tags
        /// </summary>
        /// <param name="articleText">The wiki text of the article</param>
        /// <returns>The modified article text.</returns>
        public static string FixReferenceTags(string articleText)
        {
            foreach (RegexReplacement rr in RefWhitespace)
                articleText = rr.Regex.Replace(articleText, rr.Replacement);

            return articleText;
        }

        // don't match on 'in the June of 2007', 'on the 11th May 2008' etc. as these won't read well if changed
        private static readonly Regex OfBetweenMonthAndYear = new Regex(@"\b" + WikiRegexes.Months + @" +of +(20\d\d|1[89]\d\d)\b(?<!\b[Tt]he {1,5}\w{3,15} {1,5}of {1,5}(20\d\d|1[89]\d\d))", RegexOptions.Compiled);
        private static readonly Regex OrdinalsInDatesAm = new Regex(@"(?<!\b[1-3]\d +)\b" + WikiRegexes.Months + @" +([0-3]?\d)(?:st|nd|rd|th)\b(?<!\b[Tt]he +\w{3,10} +(?:[0-3]?\d)(?:st|nd|rd|th)\b)(?:( *(?:to|and|.|&.dash;) *[0-3]?\d)(?:st|nd|rd|th)\b)?", RegexOptions.Compiled);
        private static readonly Regex OrdinalsInDatesInt = new Regex(@"(?:\b([0-3]?\d)(?:st|nd|rd|th)( *(?:to|and|.|&.dash;) *))?\b([0-3]?\d)(?:st|nd|rd|th) +" + WikiRegexes.Months + @"\b(?<!\b[Tt]he +(?:[0-3]?\d)(?:st|nd|rd|th) +\w{3,10})", RegexOptions.Compiled);
        private static readonly Regex DateLeadingZerosAm = new Regex(@"(?<!\b[0-3]?\d *)\b" + WikiRegexes.Months + @" +0([1-9])" + @"\b", RegexOptions.Compiled);
        private static readonly Regex DateLeadingZerosInt = new Regex(@"\b" + @"0([1-9]) +" + WikiRegexes.Months + @"\b", RegexOptions.Compiled);
        private static readonly Regex MonthsRegex = new Regex(@"\b" + WikiRegexes.MonthsNoGroup + @"\b", RegexOptions.Compiled);
        private static readonly Regex DayOfMonth = new Regex(@"(?<![Tt]he +)\b([1-9]|[12][0-9]|3[01])(?:st|nd|rd|th) +of +" + WikiRegexes.Months, RegexOptions.Compiled);

        // Covered by TestFixDateOrdinalsAndOf
        /// <summary>
        /// Removes ordinals, leading zeros from dates and 'of' between a month and a year, per [[WP:MOSDATE]]; on en wiki only
        /// </summary>
        /// <param name="articleText">The wiki text of the article</param>
        /// <param name="articleTitle">The article's title</param>
        /// <returns>The modified article text.</returns>
        public string FixDateOrdinalsAndOf(string articleText, string articleTitle)
        {
            if (!Variables.LangCode.Equals("en"))
                return articleText;

            // hide items in quotes etc., though this may also hide items within infoboxes etc.
            articleText = HideMoreText(articleText);

            articleText = OfBetweenMonthAndYear.Replace(articleText, "$1 $2");

            // don't apply if article title has a month in it (e.g. [[6th of October City]])
            if (!MonthsRegex.IsMatch(articleTitle))
            {
                articleText = OrdinalsInDatesAm.Replace(articleText, "$1 $2$3");
                articleText = OrdinalsInDatesInt.Replace(articleText, "$1$2$3 $4");
                articleText = DayOfMonth.Replace(articleText, "$1 $2");
            }

            articleText = DateLeadingZerosAm.Replace(articleText, "$1 $2");
            articleText = DateLeadingZerosInt.Replace(articleText, "$1 $2");

            // catch after any other fixes
            articleText = NoCommaAmericanDates.Replace(articleText, @"$1, $2");

            articleText = IncorrectCommaInternationalDates.Replace(articleText, @"$1 $2");

            return AddBackMoreText(articleText);
        }

        private static readonly Regex BrTwoNewlines = new Regex("(?:<br */?>)+\r\n\r\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ThreeOrMoreNewlines = new Regex("\r\n(\r\n)+", RegexOptions.Compiled);
        //private static readonly Regex TwoNewlinesInBlankSection = new Regex("== ? ?\r\n\r\n==", RegexOptions.Compiled);
        private static readonly Regex NewlinesBelowExternalLinks = new Regex(@"==External links==[\r\n\s]*\*", RegexOptions.Compiled);
        private static readonly Regex NewlinesBeforeUrl = new Regex(@"\r\n\r\n(\* ?\[?http)", RegexOptions.Compiled);
        private static readonly Regex HorizontalRule = new Regex("----+$", RegexOptions.Compiled);
        private static readonly Regex MultipleTabs = new Regex("  +", RegexOptions.Compiled);
        private static readonly Regex SpaceThenNewline = new Regex(" \r\n", RegexOptions.Compiled);
        private static readonly Regex WikiListWithMultipleSpaces = new Regex(@"^([\*#]+) +", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex SpacedHeadings = new Regex("^(={1,4}) ?(.*?) ?(={1,4})$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex SpacedDashes = new Regex(" (—|&#15[01];|&mdash;|&#821[12];|&#x201[34];) ", RegexOptions.Compiled);

        /// <summary>
        /// Applies/removes some excess whitespace from the article
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public static string RemoveWhiteSpace(string articleText)
        {
            return RemoveWhiteSpace(articleText, true);
        }

        // Covered by: FormattingTests.TestFixWhitespace(), incomplete
        /// <summary>
        /// Applies/removes some excess whitespace from the article
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="fixOptionalWhitespace">Whether to remove cosmetic whitespace</param>
        /// <returns>The modified article text.</returns>
        public static string RemoveWhiteSpace(string articleText, bool fixOptionalWhitespace)
        {
            //Remove <br /> if followed by double newline, NOT in blockquotes
            while(BrTwoNewlines.IsMatch(articleText) && !WikiRegexes.Blockquote.IsMatch(articleText))
                articleText = BrTwoNewlines.Replace(articleText.Trim(), "\r\n\r\n");

            if (!WikiRegexes.Stub.IsMatch(articleText))
                articleText = ThreeOrMoreNewlines.Replace(articleText, "\r\n\r\n");

            articleText = NewlinesBelowExternalLinks.Replace(articleText, "==External links==\r\n*");
            articleText = NewlinesBeforeUrl.Replace(articleText, "\r\n$1");

            articleText = HorizontalRule.Replace(articleText.Trim(), "");

            if (articleText.Contains("\r\n|\r\n\r\n"))
                articleText = articleText.Replace("\r\n|\r\n\r\n", "\r\n|\r\n");

            return articleText.Trim();
        }

        // covered by RemoveAllWhiteSpaceTests
        /// <summary>
        /// Applies removes all excess whitespace from the article
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public static string RemoveAllWhiteSpace(string articleText)
        {
            articleText = articleText.Replace("\t", " ");
            articleText = RemoveWhiteSpace(articleText);

            articleText = articleText.Replace("\r\n\r\n*", "\r\n*");

            articleText = MultipleTabs.Replace(articleText, " ");
            articleText = SpaceThenNewline.Replace(articleText, "\r\n");

            articleText = articleText.Replace("==\r\n\r\n", "==\r\n");
            articleText = NewlinesBelowExternalLinks.Replace(articleText, "==External links==\r\n*");

            // fix bullet points – one space after them not multiple
            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Remove_arbitrary_spaces_after_bullet
            articleText = WikiListWithMultipleSpaces.Replace(articleText, "$1 ");

            //fix heading space
            articleText = SpacedHeadings.Replace(articleText, "$1$2$3");

            //fix dash spacing
            articleText = SpacedDashes.Replace(articleText, "$1");

            return articleText.Trim();
        }

        /// <summary>
        /// Fixes and improves syntax (such as html markup)
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="noChange">Value that indicated whether no change was made.</param>
        /// <returns>The modified article text.</returns>
        public static string FixSyntax(string articleText, out bool noChange)
        {
            string newText = FixSyntax(articleText);

            noChange = (newText.Equals(articleText));
            return newText;
        }

        // regexes for external link match on balanced bracket
        private static readonly Regex DoubleBracketAtStartOfExternalLink = new Regex(@"\[\[ *(https?:/(?>[^\[\]]+|\[(?<DEPTH>)|\](?<-DEPTH>))*(?(DEPTH)(?!))\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DoubleBracketAtEndOfExternalLink = new Regex(@"(\[ *https?:/(?>[^\[\]]+|\[(?<DEPTH>)|\](?<-DEPTH>))*(?(DEPTH)(?!))\])\](?!\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DoubleBracketAtEndOfExternalLinkWithinImage = new Regex(@"(\[https?:/(?>[^\[\]]+|\[(?<DEPTH>)|\](?<-DEPTH>))*(?(DEPTH)(?!)))\](?=\]{3})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ListExternalLinkEndsCurlyBrace = new Regex(@"^(\* *\[https?://[^<>\[\]]+?)\)\s*$", RegexOptions.Multiline | RegexOptions.Compiled);

        private const string TemEnd = @"(\s*(?:\||\}\}))";
        private const string CitUrl = @"(\{\{\s*cit[ae][^{}]*?\|\s*url\s*=\s*)";
        private static readonly Regex BracketsAtBeginCiteTemplateURL = new Regex(CitUrl + @"\[+\s*((?:(?:ht|f)tp://)?[^\[\]<>""\s]+?\s*)\]?" + TemEnd, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BracketsAtEndCiteTemplateURL = new Regex(CitUrl + @"\[?\s*((?:(?:ht|f)tp://)?[^\[\]<>""\s]+?\s*)\]+" + TemEnd, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SyntaxRegexWikilinkMissingClosingBracket = new Regex(@"\[\[([^][]*?)\](?=[^\]]*?(?:$|\[|\n))", RegexOptions.Compiled);
        private static readonly Regex SyntaxRegexWikilinkMissingOpeningBracket = new Regex(@"(?<=(?:^|\]|\n)[^\[]*?)\[([^][]*?)\]\](?!\])", RegexOptions.Compiled);

        private static readonly Regex SyntaxRegexExternalLinkToImageURL = new Regex("\\[?\\[image:(http:\\/\\/.*?)\\]\\]?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ExternalLinksNewline = new Regex(@"([^\[]\[ *(?:https?|ftp|mailto|irc|gopher|telnet|nntp|worldwind|news|svn)://[^\[\]]+?)" + "\r\n" + @"([^\[\]<>{}]*?\])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SyntaxRegexSimpleWikilinkStartsWithSpaces = new Regex("\\[\\[ (.*)?\\]\\]", RegexOptions.Compiled);
        private static readonly Regex SyntaxRegexSimpleWikilinkEndsWithSpaces = new Regex("\\[\\[([A-Za-z]*) \\]\\]", RegexOptions.Compiled);
        private static readonly Regex SyntaxRegexSectionLinkUnnecessaryUnderscore = new Regex("\\[\\[(.*)?_#(.*)\\]\\]", RegexOptions.Compiled);

        private static readonly Regex SyntaxRegexListRowBrTag = new Regex(@"^([#\*:;]+.*?) *(?:<[/\\]?br ?[/\\]?>)* *\r\n", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SyntaxRegexListRowBrTagStart = new Regex(@"<[/\\]?br ?[/\\]?> *(\r\n[#\*:;]+.*?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // make double spaces within wikilinks just single spaces
        private static readonly Regex SyntaxRegexMultipleSpacesInWikilink = new Regex(@"(\[\[[^\[\]]+?) {2,}([^\[\]]+\]\])", RegexOptions.Compiled);

        private static readonly Regex SyntaxRegexItalic = new Regex(@"< *(i|em) *>(.*?)< */ *\1 *>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SyntaxRegexBold = new Regex("< *b *>(.*?)< */ *b *>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches <p> tags only if current line does not start from ! or | (indicator of table cells)
        private static readonly Regex SyntaxRemoveParagraphs = new Regex(@"(?<!^[!\|].*)</? ?[Pp]>", RegexOptions.Multiline | RegexOptions.Compiled);
        // Match execss <br> tags only if current line does not start from ! or | (indicator of table cells)
        private static readonly Regex SyntaxRemoveBr = new Regex(@"(?<!^[!\|].*)(?:(?:<br[\s/]*> *){2,}|\r\n<br[\s/]*>\r\n<br[\s/]*>\r\n)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex MultipleHttpInLink = new Regex(@"([\s\[>=])((?:ht|f)tp(?::?/+|:/*))(\2)+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex PipedExternalLink = new Regex(@"(\[\w+://[^\]\[<>\""\s]*?\s*)(?: +\||\|([ ']))(?=[^\[\]\|]*\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex MissingColonInHttpLink = new Regex(@"([\s\[>=](?:ht|f))tp(?://?:?|:(?::+//)?)(\w+)", RegexOptions.Compiled);
        private static readonly Regex SingleTripleSlashInHttpLink = new Regex(@"([\s\[>=](?:ht|f))tp:(?:/|///)(\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex CellpaddingTypo = new Regex(@"({\s*\|\s*class\s*=\s*""wikitable[^}]*?)cel(?:lpa|pad?)ding\b", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        //https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Remove_.3Cfont.3E_tags
        private static readonly Regex RemoveNoPropertyFontTags = new Regex(@"<font>([^<>]+)</font>", RegexOptions.IgnoreCase);

        // for fixing unbalanced brackets
        private static readonly Regex RefTemplateIncorrectBracesAtEnd = new Regex(@"(?<=<ref(?:\s*name\s*=[^{}<>/]+?\s*)?>\s*)({{\s*[Cc]it[ae][^{}]+?)(?:}\]?|\)\))?(?=\s*</ref>)", RegexOptions.Compiled);
        private static readonly Regex RefExternalLinkUsingBraces = new Regex(@"(?<=<ref(?:\s*name\s*=[^{}<>]+?\s*)?>\s*){{(\s*https?://[^{}\s\r\n]+)(\s+[^{}]+\s*)?}}(\s*</ref>)", RegexOptions.Compiled);
        private static readonly Regex TemplateIncorrectBracesAtStart = new Regex(@"(?:{\[|\[{)([^{}\[\]]+}})", RegexOptions.Compiled);
        private static readonly Regex CitationTemplateSingleBraceAtStart = new Regex(@"(?<=[^{])({\s*[Cc]it[ae])", RegexOptions.Compiled);
        private static readonly Regex ReferenceTemplateQuadBracesAtEnd = new Regex(@"(?<=<ref(?:\s*name\s*=[^{}<>]+?\s*)?>\s*{{[^{}]+)}}(}}\s*</ref>)", RegexOptions.Compiled);
        private static readonly Regex CitationTemplateIncorrectBraceAtStart = new Regex(@"(?<=<ref(?:\s*name\s*=[^{}<>]+?\s*)?>){\[([Cc]it[ae])", RegexOptions.Compiled);
        private static readonly Regex CitationTemplateIncorrectBracesAtEnd = new Regex(@"(<ref(?:\s*name\s*=[^{}<>]+?\s*)?>\s*{{[Cc]it[ae][^{}]+?)(?:}\]|\]}|{})(?=\s*</ref>)", RegexOptions.Compiled);
        private static readonly Regex RefExternalLinkMissingStartBracket = new Regex(@"(?<=<ref(?:\s*name\s*=[^{}<>]+?\s*)?>[^{}\[\]<>]*?){?((?:ht|f)tps?://[^{}\[\]<>]+\][^{}\[\]<>]*</ref>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RefExternalLinkMissingEndBracket = new Regex(@"(?<=<ref(?:\s*name\s*=[^{}<>]+?\s*)?>[^{}\[\]<>]*?\[\s*(?:ht|f)tps?://[^{}\[\]<>]+)}?(</ref>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RefCitationMissingOpeningBraces = new Regex(@"(?<=<\s*ref(?:\s+name\s*=[^<>]*?)?\s*>\s*)\(?\(?(?=[Cc]it[ae][^{}]+}}\s*</ref>)", RegexOptions.Compiled);
        private static readonly Regex BracesWithinDefaultsort = new Regex(@"(?<={{DEFAULTSORT[^{}\[\]]+)[\]\[]+}}", RegexOptions.Compiled);

        // refs with wording and bare link: combine the two
        private static readonly Regex WordingIntoBareExternalLinks = new Regex(@"(?<=<ref(?:\s*name\s*=[^{}<>]+?\s*)?>\s*)([^<>{}\[\]\r\n]{3,70}?)[\.,::]?\s*\[\s*((?:[Hh]ttp|[Hh]ttps|[Ff]tp|[Mm]ailto)://[^\ \n\r<>]+)\s*\](?=\s*</ref>)", RegexOptions.Compiled);

        // space needed between word and external link
        private static readonly Regex ExternalLinkWordSpacingBefore = new Regex(@"(\w)(?=\[(?:https?|ftp|mailto|irc|gopher|telnet|nntp|worldwind|news|svn)://.*?\])", RegexOptions.Compiled);
        private static readonly Regex ExternalLinkWordSpacingAfter = new Regex(@"(?<=\[(?:https?|ftp|mailto|irc|gopher|telnet|nntp|worldwind|news|svn)://[^\]\[<>]*?\])(\w)", RegexOptions.Compiled);

        private static readonly Regex WikilinkEndsBr = new Regex(@"<br[\s/]*>\]\]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // for correcting square brackets within external links
        private static readonly Regex SquareBracketsInExternalLinks = new Regex(@"(\[https?://(?>[^\[\]<>]+|\[(?<DEPTH>)|\](?<-DEPTH>))*(?(DEPTH)(?!))\])", RegexOptions.Compiled);

        // CHECKWIKI error 2: fix incorrect <br> of <br.>, <\br>, <br\> and <br./>
        private static readonly Regex IncorrectBr = new Regex(@"< *br\. *>|<\\ *br *>|< *br *\\ *>|< *br\. */>|< *br */[v|r]>|< *br *\?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SyntaxRegexHorizontalRule = new Regex("^<hr>|^----+", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex SyntaxRegexHeadingWithHorizontalRule = new Regex("(^==?[^=]*==?)\r\n(\r\n)?----+", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex SyntaxRegexHTTPNumber = new Regex(@"HTTP/\d\.", RegexOptions.Compiled);
        private static readonly Regex SyntaxRegexISBN = new Regex(@"ISBN(?:\-1[03])?: *([0-9])", RegexOptions.Compiled);
        private static readonly Regex SyntaxRegexPMID = new Regex(@"(PMID): *([0-9]+)", RegexOptions.Compiled);
        private static readonly Regex ISBNTemplates = Tools.NestedTemplateRegex(new[] { "ISBN-10", "ISBN-13" });
        private static readonly Regex SyntaxRegexExternalLinkOnWholeLine = new Regex(@"^\[(\s*http.*?)\]$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex SyntaxRegexClosingBracket = new Regex(@"([^]])\]([^]]|$)", RegexOptions.Compiled);
        private static readonly Regex SyntaxRegexOpeningBracket = new Regex(@"([^[]|^)\[([^[])", RegexOptions.Compiled);
        private static readonly Regex SyntaxRegexImageWithHTTP = new Regex("\\[\\[[Ii]mage:[^]]*http", RegexOptions.Compiled);

        /// <summary>
        /// Matches double piped links e.g. [[foo||bar]] (CHECKWIKI error 32)
        /// </summary>
        private static readonly Regex DoublePipeInWikiLink = new Regex(@"(?<=\[\[[^\[\[\r\n\|{}]+)\|\|(?=[^\[\[\r\n\|{}]+\]\])", RegexOptions.Compiled);

        /// <summary>
        /// Matches empty gallery tags (zero or more whitespace)
        /// </summary>
        private static readonly Regex EmptyGallery = new Regex(@"<\s*[Gg]allery\s*>\s*<\s*/\s*[Gg]allery\s*>", RegexOptions.Compiled);

        private static readonly System.Globalization.CultureInfo BritishEnglish = new System.Globalization.CultureInfo("en-GB");

        // Covered by: LinkTests.TestFixSyntax(), incomplete
        /// <summary>
        /// Fixes and improves syntax (such as html markup)
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public static string FixSyntax(string articleText)
        {
            // DEFAULTSORT whitespace fix
            if (Variables.LangCode.Equals("en"))
                articleText = WikiRegexes.Defaultsort.Replace(articleText, DefaultsortME);

            articleText = Tools.TemplateToMagicWord(articleText);

            articleText = articleText.Replace(@"<small/>", @"</small>");

            // remove empty <gallery> tags
            articleText = EmptyGallery.Replace(articleText, "");

            // fix italic html tags
            // <b /> may refer to </b> or <br />
            articleText = articleText.Replace("<i />", "</i>");

            //replace html with wiki syntax
            articleText = SyntaxRegexItalic.Replace(articleText, "''$2''");

            articleText = SyntaxRegexBold.Replace(articleText, "'''$1'''");

            articleText = SyntaxRegexHorizontalRule.Replace(articleText, "----");

            //remove appearance of double line break
            articleText = SyntaxRegexHeadingWithHorizontalRule.Replace(articleText, "$1");

            // remove unnecessary namespace
            articleText = RemoveTemplateNamespace(articleText);

            // remove <br> from lists (end of list line) CHECKWIKI error 54
            articleText = SyntaxRegexListRowBrTag.Replace(articleText, "$1\r\n");

            articleText = MultipleHttpInLink.Replace(articleText, "$1$2");

            articleText = PipedExternalLink.Replace(articleText, "$1 $2");

            //repair bad external links
            articleText = SyntaxRegexExternalLinkToImageURL.Replace(articleText, "[$1]");

            //repair bad internal links
            articleText = SyntaxRegexSimpleWikilinkStartsWithSpaces.Replace(articleText, "[[$1]]");
            articleText = SyntaxRegexSimpleWikilinkEndsWithSpaces.Replace(articleText, "[[$1]]");
            articleText = SyntaxRegexSectionLinkUnnecessaryUnderscore.Replace(articleText, "[[$1#$2]]");

            while (SyntaxRegexMultipleSpacesInWikilink.IsMatch(articleText))
                articleText = SyntaxRegexMultipleSpacesInWikilink.Replace(articleText, @"$1 $2");

            if (!SyntaxRegexHTTPNumber.IsMatch(articleText))
            {
                articleText = MissingColonInHttpLink.Replace(articleText, "$1tp://$2");
                articleText = SingleTripleSlashInHttpLink.Replace(articleText, "$1tp://$2");
            }

            if (!ISBNTemplates.IsMatch(articleText))
                articleText = SyntaxRegexISBN.Replace(articleText, "ISBN $1");

            articleText = SyntaxRegexPMID.Replace(articleText, "$1 $2");
            articleText = CellpaddingTypo.Replace(articleText, "$1cellpadding");

            articleText = RemoveNoPropertyFontTags.Replace(articleText, "$1");

            // {{Category:foo]] or {{Category:foo}}
            articleText = CategoryCurlyBrackets.Replace(articleText, @"[[$1]]");

            // fixes for missing/unbalanced brackets
            articleText = RefCitationMissingOpeningBraces.Replace(articleText, @"{{");
            articleText = RefTemplateIncorrectBracesAtEnd.Replace(articleText, @"$1}}");
            articleText = RefExternalLinkUsingBraces.Replace(articleText, @"[$1$2]$3");
            articleText = TemplateIncorrectBracesAtStart.Replace(articleText, @"{{$1");
            articleText = CitationTemplateSingleBraceAtStart.Replace(articleText, @"{$1");
            articleText = ReferenceTemplateQuadBracesAtEnd.Replace(articleText, @"$1");
            articleText = CitationTemplateIncorrectBraceAtStart.Replace(articleText, @"{{$1");
            articleText = CitationTemplateIncorrectBracesAtEnd.Replace(articleText, @"$1}}");
            articleText = RefExternalLinkMissingStartBracket.Replace(articleText, @"[$1");
            articleText = RefExternalLinkMissingEndBracket.Replace(articleText, @"]$1");
            articleText = BracesWithinDefaultsort.Replace(articleText, @"}}");

            // fixes for square brackets used within external links
            foreach (Match m in SquareBracketsInExternalLinks.Matches(articleText))
            {
                // strip off leading [ and trailing ]
                string externalLink = SyntaxRegexExternalLinkOnWholeLine.Replace(m.Value, "$1");

                // if there are some brackets left then they need fixing; the mediawiki parser finishes the external link
                // at the first ] found
                if (externalLink.Contains("]") || externalLink.Contains("["))
                {
                    // replace single ] with &#93; when used for brackets in the link description
                    if (externalLink.Contains("]"))
                        externalLink = SyntaxRegexClosingBracket.Replace(externalLink, @"$1&#93;$2");

                    if (externalLink.Contains("["))
                        externalLink = SyntaxRegexOpeningBracket.Replace(externalLink, @"$1&#91;$2");

                    articleText = articleText.Replace(m.Value, @"[" + externalLink + @"]");
                }
            }

            // needs to be applied after SquareBracketsInExternalLinks
            if (!SyntaxRegexImageWithHTTP.IsMatch(articleText))
            {
                articleText = SyntaxRegexWikilinkMissingClosingBracket.Replace(articleText, "[[$1]]");
                articleText = SyntaxRegexWikilinkMissingOpeningBracket.Replace(articleText, "[[$1]]");
            }

            // if there are some unbalanced brackets, see whether we can fix them
            articleText = FixUnbalancedBrackets(articleText);
            
            //fix uneven bracketing on links
            articleText = DoubleBracketAtStartOfExternalLink.Replace(articleText, "[$1");
            articleText = DoubleBracketAtEndOfExternalLink.Replace(articleText, "$1");
            articleText = DoubleBracketAtEndOfExternalLinkWithinImage.Replace(articleText, "$1");
            articleText = ListExternalLinkEndsCurlyBrace.Replace(articleText, "$1]");
            
            // (part) wikilinked/external linked URL in cite template, uses MediaWiki regex of [^\[\]<>""\s] for URL bit after http://
            articleText = BracketsAtBeginCiteTemplateURL.Replace(articleText, "$1$2$3");
            articleText = BracketsAtEndCiteTemplateURL.Replace(articleText, "$1$2$3");

            // fix newline(s) in external link description
            while (ExternalLinksNewline.IsMatch(articleText))
                articleText = ExternalLinksNewline.Replace(articleText, "$1 $2");

            // double piped links e.g. [[foo||bar]]
            articleText = DoublePipeInWikiLink.Replace(articleText, "|");

            // https://en.wikipedia.org/wiki/Wikipedia:WikiProject_Check_Wikipedia#Article_with_false_.3Cbr.2F.3E_.28AutoEd.29
            // fix incorrect <br> of <br.>, <\br> and <br\>
            articleText = IncorrectBr.Replace(articleText, "<br />");

            articleText = FixSmallTags(articleText);

            articleText = WordingIntoBareExternalLinks.Replace(articleText, @"[$2 $1]");

            articleText = ExternalLinkWordSpacingBefore.Replace(articleText, "$1 ");
            articleText = ExternalLinkWordSpacingAfter.Replace(articleText, " $1");

            // WP:CHECKWIKI 065: Image description ends with break – http://toolserver.org/~sk/cgi-bin/checkwiki/checkwiki.cgi?project=enwiki&view=only&id=65
            foreach (Match m in WikiRegexes.FileNamespaceLink.Matches(articleText))
            {
                if (WikilinkEndsBr.IsMatch(m.Value))
                    articleText = articleText.Replace(m.Value, WikilinkEndsBr.Replace(m.Value, @"]]"));
            }

            // workaround for bugzilla 2700: {{subst:}} doesn't work within ref tags
            articleText = FixSyntaxSubstRefTags(articleText);

            return articleText.Trim();
        }

        /// <summary>
        /// Trims whitespace around DEFAULTSORT value, ensures 'whitepace only' DEFAULTSORT left unchanged
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private static string DefaultsortME(Match m)
        {
            string returned = @"{{DEFAULTSORT:", key = m.Groups["key"].Value;

            // avoid changing a defaultsort key value that is only whitespace: wrong before, would still be wrong after
            if (key.Trim().Length == 0)
                return m.Value;

            returned += (key.Trim() + @"}}");

            // handle case where defaultsort ended by newline, preserve newline at end of defaultort returned
            string end = m.Groups["end"].Value;

            if (!end.Equals(@"}}"))
                returned += end;

            return returned;
        }

        /// <summary>
        /// Performs fixes to redirect pages:
        /// * removes newline between #REDIRECT and link
        /// </summary>
        /// <param name="articleText"></param>
        /// <returns></returns>
        public static string FixSyntaxRedirects(string articleText)
        {
            Match m = WikiRegexes.Redirect.Match(articleText);

            if (m.Success)
                articleText = articleText.Replace(m.Value, m.Value.Replace("\r\n", " "));

            return articleText;
        }

        /// <summary>
        /// workaround for bugzilla 2700: {{subst:}} doesn't work within ref tags
        /// </summary>
        /// <param name="articleText"></param>
        /// <returns></returns>
        public static string FixSyntaxSubstRefTags(string articleText)
        {
            if (Variables.LangCode.Equals("en") && articleText.Contains(@"{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}"))
            {
                foreach (Match m in WikiRegexes.Refs.Matches(articleText))
                {
                    if (m.Value.Contains(@"{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}"))
                        articleText = articleText.Replace(m.Value, m.Value.Replace(@"{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}", System.DateTime.UtcNow.ToString("MMMM yyyy", BritishEnglish)));
                }

                foreach (Match m in WikiRegexes.Images.Matches(articleText))
                {
                    if (m.Value.Contains(@"{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}"))
                        articleText = articleText.Replace(m.Value, m.Value.Replace(@"{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}", System.DateTime.UtcNow.ToString("MMMM yyyy", BritishEnglish)));
                }

                 foreach (Match m in WikiRegexes.GalleryTag.Matches(articleText))
                {
                    if (m.Value.Contains(@"{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}"))
                        articleText = articleText.Replace(m.Value, m.Value.Replace(@"{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}", System.DateTime.UtcNow.ToString("MMMM yyyy", BritishEnglish)));
                }
            }

            return articleText;
        }

        /// <summary>
        /// Removes Template: (or equivalent translation) from start of template calls, canonicalizes template names
        /// </summary>
        /// <param name="articleText">The wiki article text</param>
        /// <returns>The updated article text</returns>
        public static string RemoveTemplateNamespace(string articleText)
        {
        	Regex SyntaxRegexTemplate = new Regex(@"(\{\{\s*)" + Variables.NamespacesCaseInsensitive[Namespace.Template] + @"(.*?)\}\}", RegexOptions.Singleline);
        	
        	return (SyntaxRegexTemplate.Replace(articleText, m => m.Groups[1].Value + CanonicalizeTitle(m.Groups[2].Value) + "}}"));
        }

        private static List<Regex> SmallTagRegexes = new List<Regex>();
        private static readonly Regex LegendTemplate = Tools.NestedTemplateRegex("legend");

        /// <summary>
        /// remove &lt;small> in small, ref, sup, sub tags and images, but not within {{legend}} template
        /// CHECKWIKI errors 55, 63, 66, 77
        /// </summary>
        /// <param name="articleText">The article text</param>
        /// <returns>The updated article text</returns>
        private static string FixSmallTags(string articleText)
        {
            // don't apply if there are uncosed tags
            if (!WikiRegexes.Small.IsMatch(articleText) || UnclosedTags(articleText).Count > 0)
                return articleText;

            foreach (Regex rx in SmallTagRegexes)
            {
                foreach (Match m in rx.Matches(articleText))
                {
                    // don't remove <small> tags from within {{legend}} where use is not unreasonable
                    if (LegendTemplate.IsMatch(m.Value))
                        continue;

                    Match s = WikiRegexes.Small.Match(m.Value);
                    if (s.Success)
                    {
                        if (s.Index > 0)
                            articleText = articleText.Replace(m.Value, WikiRegexes.Small.Replace(m.Value, "$1"));
                        else
                            // nested small
                            articleText = articleText.Replace(m.Value, m.Value.Substring(0, 7) + WikiRegexes.Small.Replace(m.Value.Substring(7), "$1"));
                    }
                }

                if (!WikiRegexes.Small.IsMatch(articleText))
                    return articleText;
            }

            return articleText;
        }

        private static readonly Regex CurlyBraceInsteadOfPipeInWikiLink = new Regex(@"(?<=\[\[[^\[\]{}<>\r\n\|]{1,50})}(?=[^\[\]{}<>\r\n\|]{1,50}\]\])", RegexOptions.Compiled);
        private static readonly Regex CurlyBraceInsteadOfBracketClosing = new Regex(@"(?<=\([^{}<>\(\)]+[^{}<>\(\)\|])}(?=[^{}])", RegexOptions.Compiled);
        private static readonly Regex CurlyBraceInsteadOfSquareBracket = new Regex(@"(?<=\[[^{}<>\[\]]+[^{}<>\(\)\|\]])}(?=[^{}])", RegexOptions.Compiled);
        private static readonly Regex CurlyBraceInsteadOfBracketOpening = new Regex(@"(?<=[^{}<>]){(?=[^{}<>\(\)\|][^{}<>\(\)]+\)[^{}\(\)])", RegexOptions.Compiled);
        private static readonly Regex ExtraBracketOnWikilinkOpening = new Regex(@"(?<=[^\[\]{}<>])(?:{\[\[?|\[\[\[)(?=[^\[\]{}<>]+\]\])", RegexOptions.Compiled);
        private static readonly Regex ExtraBracketOnWikilinkOpening2 = new Regex(@"(?<=\[\[){(?=[^{}\[\]<>]+\]\])", RegexOptions.Compiled);
        private static readonly Regex ExternalLinkMissingClosing = new Regex(@"(?<=^ *\* *\[ *(?:ht|f)tps?://[^<>{}\[\]\r\n\s]+[^\[\]\r\n]*)(\s$)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex ExternalLinkMissingOpening = new Regex(@"(?<=^ *\*) *(?=(?:ht|f)tps?://[^<>{}\[\]\r\n\s]+[^\[\]\r\n]*\]\s$)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex TemplateIncorrectClosingBraces = new Regex(@"(?<={{[^{}<>]{1,400}[^{}<>\|\]])(?:\]}|}\]?)(?=[^{}])", RegexOptions.Compiled);
        private static readonly Regex TemplateMissingOpeningBrace = new Regex(@"(?<=[^{}<>\|]){(?=[^{}<>]{1,400}}})", RegexOptions.Compiled);

        private static readonly Regex QuadrupleCurlyBrackets = new Regex(@"(?<=^{{[^{}\r\n]+}})}}(\s)$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex WikiLinkOpeningClosing = new Regex(@"\[\]([^\[\]\r\n]+\]\])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex UnclosedCatInterwiki = new Regex(@"^(\[\[[^\[\]\r\n]+(?<!File|Image|Media)\:[^\[\]\r\n]+)(\s*)$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex RefClosingOpeningBracket = new Regex(@"\[(\s*</ref>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CategoryCurlyBrackets = new Regex(@"{{ *(" + Variables.Namespaces[Namespace.Category] + @"[^{}\[\]]+?)(?:}}|\]\])", RegexOptions.Compiled);
        private static readonly Regex FileImageCurlyBrackets = new Regex(@"{{\s*((?:[Ff]ile|[Ii]mage)\s*:)", RegexOptions.Compiled);
        private static readonly Regex CiteRefEndsTripleClosingBrace = new Regex(@"([^}])\}(\}\}\s*</ref>)", RegexOptions.Compiled);
        private static readonly Regex RefExternalLinkWrongBracket = new Regex(@"(<ref[^<>/]*>)\]", RegexOptions.Compiled);

        /// <summary>
        /// Applies some fixes for unbalanced brackets, applied if there are unbalanced brackets
        /// </summary>
        /// <param name="articleText">the article text</param>
        /// <returns>the corrected article text</returns>
        private static string FixUnbalancedBrackets(string articleText)
        {
            // if there are some unbalanced brackets, see whether we can fix them
            // the fixes applied might damage correct wiki syntax, hence are only applied if there are unbalanced brackets
            // of the right type
            int bracketLength = 0;
            string articleTextTemp = articleText;
            int unbalancedBracket = UnbalancedBrackets(articleText, ref bracketLength);
            if (unbalancedBracket > -1)
            {
                int firstUnbalancedBracket = unbalancedBracket;
                char bracket = articleTextTemp[unbalancedBracket];

                // if it's ]]_]_ then see if removing bracket makes it all balance
                if (bracketLength == 1 && unbalancedBracket > 2
                    && articleTextTemp[unbalancedBracket] == ']'
                    && articleTextTemp[unbalancedBracket - 1] == ']'
                    && articleTextTemp[unbalancedBracket - 2] == ']'
                   )
                {
                    articleTextTemp = articleTextTemp.Remove(unbalancedBracket, 1);
                }

                else if (bracketLength == 1)
                {
                    switch (bracket)
                    {
                        case '}':
                            // if it's [[blah blah}word]]
                            articleTextTemp = CurlyBraceInsteadOfPipeInWikiLink.Replace(articleTextTemp, "|");

                            // if it's (blah} then see if setting the } to a ) makes it all balance, but not |} which could be wikitables
                            articleTextTemp = CurlyBraceInsteadOfBracketClosing.Replace(articleTextTemp, ")");

                            // if it's [blah} then see if setting the } to a ] makes it all balance
                            articleTextTemp = CurlyBraceInsteadOfSquareBracket.Replace(articleTextTemp, "]");

                            // if it's }}}</ref>
                            articleTextTemp = CiteRefEndsTripleClosingBrace.Replace(articleTextTemp, "$1$2");

                            break;

                        case '{':
                            // if it's {blah) then see if setting the { to a ( makes it all balance, but not {| which could be wikitables
                            articleTextTemp = CurlyBraceInsteadOfBracketOpening.Replace(articleTextTemp, "(");

                            // could be [[{link]]
                            articleTextTemp = ExtraBracketOnWikilinkOpening2.Replace(articleTextTemp, "");
                            break;

                        case '(':
                            // if it's ((word) then see if removing the extra opening round bracket makes it all balance
                            if (articleTextTemp.Length > (unbalancedBracket + 1)
                                && articleText[unbalancedBracket + 1] == '('
                               )
                            {
                                articleTextTemp = articleTextTemp.Remove(unbalancedBracket, 1);
                            }
                            break;

                        case '[':
                            // external link missing closing ]
                            articleTextTemp = ExternalLinkMissingClosing.Replace(articleTextTemp, "]$1");

                            // ref with closing [ in error
                            articleTextTemp = RefClosingOpeningBracket.Replace(articleTextTemp, "]$1");

                            break;

                        case ']':
                            // external link missing opening [
                            articleTextTemp = ExternalLinkMissingOpening.Replace(articleTextTemp, " [");

                            // <ref>http://...
                            articleTextTemp = RefExternalLinkWrongBracket.Replace(articleTextTemp, "$1[");

                            break;

                        case '>':
                            // <ref>>
                            articleTextTemp = articleTextTemp.Replace(@"<ref>>", @"<ref>");
                            break;

                        default:
                            // Chinese language brackets（ and ）[ASCII 65288 and 65289]
                            if(Variables.LangCode.Equals("en"))
                            {
                            articleTextTemp = articleTextTemp.Replace("）", ")");
                            articleTextTemp = articleTextTemp.Replace("（", "(");
                            }
                            break;
                    }

                    // if it's {[link]] or {[[link]] or [[[link]] then see if setting to [[ makes it all balance
                    if (!bracket.Equals('>'))
                        articleTextTemp = ExtraBracketOnWikilinkOpening.Replace(articleTextTemp, "[[");
                }

                if (bracketLength == 2)
                {
                    // if it's on double curly brackets, see if one is missing e.g. {{foo} or {{foo]}
                    articleTextTemp = TemplateIncorrectClosingBraces.Replace(articleTextTemp, "}}");

                    // {foo}}
                    articleTextTemp = TemplateMissingOpeningBrace.Replace(articleTextTemp, "{{");

                    // might be [[[[link]] or [[link]]]] so see if removing the two found square brackets makes it all balance
                    if (articleTextTemp.Substring(unbalancedBracket, Math.Min(4, articleTextTemp.Length - unbalancedBracket)) == "[[[["
                        || articleTextTemp.Substring(Math.Max(0, unbalancedBracket - 2), Math.Min(4, articleTextTemp.Length - unbalancedBracket)) == "]]]]")
                    {
                        articleTextTemp = articleTextTemp.Remove(unbalancedBracket, 2);
                    }

                    // wikilink like []foo]]
                    articleTextTemp = WikiLinkOpeningClosing.Replace(articleTextTemp, @"[[$1");

                    articleTextTemp = QuadrupleCurlyBrackets.Replace(articleTextTemp, "$1");

                    // defaultsort missing }} at end
                    string defaultsort = WikiRegexes.Defaultsort.Match(articleTextTemp).Value;
                    if (!string.IsNullOrEmpty(defaultsort) && !defaultsort.Contains("}}"))
                        articleTextTemp = articleTextTemp.Replace(defaultsort.TrimEnd(), defaultsort.TrimEnd() + "}}");

                    // unclosed cat/interwiki
                    articleTextTemp = UnclosedCatInterwiki.Replace(articleTextTemp, @"$1]]$2");

                    // {{File: --> [[File:
                    articleTextTemp = FileImageCurlyBrackets.Replace(articleTextTemp, @"[[$1");
                }

                unbalancedBracket = UnbalancedBrackets(articleTextTemp, ref bracketLength);
                // the change worked if unbalanced bracket location moved considerably (so this one fixed), or all brackets now balance
                if (unbalancedBracket < 0 || Math.Abs(unbalancedBracket - firstUnbalancedBracket) > 300)
                    articleText = articleTextTemp;
            }

            return articleText;
        }

        private static List<string> DateFields = new List<string>(new[] { "date", "accessdate", "archivedate", "airdate" });

        /// <summary>
        /// Updates dates in citation templates to use the strict predominant date format in the article (en wiki only)
        /// </summary>
        /// <param name="articleText">The article text</param>
        /// <returns>The updated article text</returns>
        public static string PredominantDates(string articleText)
        {
            if (!Variables.LangCode.Equals("en"))
                return articleText;

            DateLocale predominantLocale = DeterminePredominantDateLocale(articleText, true, true);

            if (predominantLocale.Equals(DateLocale.Undetermined))
                return articleText;

            foreach (Match m in WikiRegexes.CiteTemplate.Matches(articleText))
            {
                string newValue = m.Value;

                foreach (string field in DateFields)
                {
                    string fvalue = Tools.GetTemplateParameterValue(newValue, field);

                    if (WikiRegexes.ISODates.IsMatch(fvalue) || WikiRegexes.AmericanDates.IsMatch(fvalue) || WikiRegexes.InternationalDates.IsMatch(fvalue))
                        newValue = Tools.UpdateTemplateParameterValue(newValue, field, Tools.ConvertDate(fvalue, predominantLocale));
                }

                // merge changes to article text
                if (!m.Value.Equals(newValue))
                    articleText = articleText.Replace(m.Value, newValue);
            }

            return articleText;
        }

        private static readonly List<string> AccessdateTypos = new List<string>(new[] { "acesdate", "accesdate", "accesssdate", "acessdate", "accessdat", "acccesssdate", "acccessdate", "acccesdate", "accessedate", "accessdare", "accessdaye", "accessate", "accessdaste", "accdessdate", "accessdata", "accesdsate", "accessdaet", "accessdatee", "accessdatge", "accesseddate", "accessedon", "access-date" });
        private static readonly List<string> PublisherTypos = new List<string>(new[] { "punlisher", "puslisher", "poublisher", "publihser", "pub(?:lication)?", "pubslisher", "puablisher", "publicher", "ublisher", "publsiher", "pusliher", "pblisher", "pubi?lsher", "publishet", "puiblisher", "puplisher", "publiisher", "publiser", "pulisher", "publishser", "pulbisher", "publisber", "publoisher", "publishier", "pubhlisher", "publiaher", "publicser", "publicsher", "publidsherr", "publiher", "publihsher", "publilsher", "publiosher", "publisaher", "publischer", "publiseher", "publisehr", "publiserh", "publisger", "publishe?", "publishey", "publlisher", "publusher", "pubsliher" });

        private static readonly Regex AccessdateSynonyms = new Regex(@"(?<={{\s*[Cc]it[ae][^{}]*?\|\s*)(?:\s*date\s*)?(?:retrieved(?:\s+on)?|(?:last|date) *ac+essed|access\s+date)(?=\s*=\s*)", RegexOptions.Compiled);

        private static readonly Regex UppercaseCiteFields = new Regex(@"(\{\{\s*(?:[Cc]ite\s*(?:web|book|news|journal|paper|press release|hansard|encyclopedia)|[Cc]itation)\b\s*[^{}]*\|\s*)(\w*?[A-Z]+\w*)(?<!(?:IS[BS]N|DOI|PMID))(\s*=\s*[^{}\|]{3,})", RegexOptions.Compiled);

        private static readonly Regex CiteUrl = new Regex(@"\|\s*url\s*=\s*([^\[\]<>""\s]+)", RegexOptions.Compiled);

        private static readonly Regex WorkInItalics = new Regex(@"(\|\s*work\s*=\s*)''([^'{}\|]+)''(?=\s*(?:\||}}))", RegexOptions.Compiled);

        private static readonly Regex CiteTemplatePagesPP = new Regex(@"(?<=\|\s*pages?\s*=\s*)p(?:p|gs?)?(?:\.|\b)(?:&nbsp;|\s*)(?=[^{}\|]+(?:\||}}))", RegexOptions.Compiled);
        private static readonly Regex CiteTemplatesJournalVolume = new Regex(@"(?<=\|\s*volume\s*=\s*)vol(?:umes?|\.)?(?:&nbsp;|:)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CiteTemplatesJournalVolumeAndIssue = new Regex(@"(?<=\|\s*volume\s*=\s*[0-9VXMILC]+?)(?:[;,]?\s*(?:no[\.:;]?|(?:numbers?|issues?|iss)\s*[:;]?))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CiteTemplatesJournalIssue = new Regex(@"(?<=\|\s*issue\s*=\s*)(?:issues?|(?:nos?|iss)(?:[\.,;:]|\b)|numbers?[\.,;:]?)(?:&nbsp;)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CiteTemplatesPageRangeName = new Regex(@"(\|\s*)page(\s*=\s*\d+\s*(?:–|, )\s*\d)", RegexOptions.Compiled);

        private static readonly Regex AccessDateYear = new Regex(@"(?<=\|\s*accessdate\s*=\s*(?:[1-3]?\d\s+" + WikiRegexes.MonthsNoGroup + @"|\s*" + WikiRegexes.MonthsNoGroup + @"\s+[1-3]?\d))(\s*)\|\s*accessyear\s*=\s*(20[01]\d)\s*(\||}})", RegexOptions.Compiled);
        private static readonly Regex AccessDayMonthDay = new Regex(@"\|\s*access(?:daymonth|month(?:day)?|year)\s*=\s*(?=\||}})", RegexOptions.Compiled);
        private static readonly Regex DateLeadingZero = new Regex(@"(?<=\|\s*(?:access|archive)?date\s*=\s*)(?:0([1-9]\s+" + WikiRegexes.MonthsNoGroup + @")|(\s*" + WikiRegexes.MonthsNoGroup + @"\s)+0([1-9],?))(\s+(?:20[01]|1[89]\d)\d)?(\s*\||}})", RegexOptions.Compiled);
        private static readonly Regex YearInDate = new Regex(@"(\|\s*)date(\s*=\s*[12]\d{3}\s*)(?=\||}})", RegexOptions.Compiled);

        private static readonly Regex DupeFields = new Regex(@"((\|\s*([a-z\d]+)\s*=\s*([^\{\}\|]*?))\s*(?:\|.*?)?)\|\s*\3\s*=\s*([^\{\}\|]*?)\s*(\||}})", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex UnspacedCommaPageRange = new Regex(@"((?:[ ,–]|^)\d+),(\d+(?:[ ,–]|$))", RegexOptions.Compiled);

        private static readonly List<string> ParametersToDequote = new List<string>(new[] { "title", "trans_title" });

        /// <summary>
        /// Applies various formatting fixes to citation templates
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The updated wiki text</returns>
        public static string FixCitationTemplates(string articleText)
        {
            if (!Variables.LangCode.Equals("en"))
                return articleText;

            // {{cite web}}/{{cite book}} etc. need lower case field names; two loops in case a single template has multiple uppercase fields
            // exceptionally, 'ISBN' is allowed in upper case
            for (; ; )
            {
                string articleText2 = UppercaseCiteFields.Replace(articleText, UppercaseCiteFieldsME);

                if (articleText2.Equals(articleText))
                    break;
                articleText = articleText2;
            }

            articleText = WikiRegexes.CiteTemplate.Replace(articleText, FixCitationTemplatesME);

            // Harvard template fixes: page range dashes and use of pp for page ranges
            foreach (Match h in WikiRegexes.HarvTemplate.Matches(articleText))
            {
                string newValue = FixPageRanges(h.Value);
                string page = Tools.GetTemplateParameterValue(newValue, "p");

                // ignore brackets
                if (page.Contains(@"("))
                    page = page.Substring(0, page.IndexOf(@"("));

                if (Regex.IsMatch(page, @"\d+\s*(?:–|&ndash;|, )\s*\d") && Tools.GetTemplateParameterValue(newValue, "pp").Length == 0)
                    newValue = Tools.RenameTemplateParameter(newValue, "p", "pp");

                // merge changes to article text
                if (!h.Value.Equals(newValue))
                    articleText = articleText.Replace(h.Value, newValue);
            }

            return articleText;
        }

        private static string UppercaseCiteFieldsME(Match m)
        {
            bool urlmatch = CiteUrl.Match(m.Value).Value.Contains(m.Groups[2].Value);

            // check not within URL
            if (!urlmatch)
                return (m.Groups[1].Value + m.Groups[2].Value.ToLower() + m.Groups[3].Value);

            return m.Value;
        }

        private static readonly Regex IdISBN = new Regex(@"^ISBN:?\s*([\d \-]+X?)$", RegexOptions.Compiled);
        private static readonly Regex IdASIN = new Regex(@"^ASIN:?\s*([\d \-]+X?)$", RegexOptions.Compiled);
        private static readonly Regex CiteVideoPodcast = new Regex(@"[Cc]ite (?:video|podcast)\b", RegexOptions.Compiled);
        private static readonly Regex YearOnly = new Regex(@"^[12]\d{3}$", RegexOptions.Compiled);

        private static string FixCitationTemplatesME(Match m)
        {
            string newValue = m.Value;
            string templatename = Tools.GetTemplateName(newValue);
            string theURL = Tools.GetTemplateParameterValue(newValue, "url");
            string id = Tools.GetTemplateParameterValue(newValue, "id");

            newValue = Tools.RenameTemplateParameter(newValue, AccessdateTypos, "accessdate");

            newValue = Tools.RenameTemplateParameter(newValue, PublisherTypos, "publisher");

            newValue = AccessdateSynonyms.Replace(newValue, "accessdate");

            newValue = Tools.RenameTemplateParameter(newValue, "pg", "page");

            // remove the unneeded 'format=HTML' field
            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Remove_.22format.3DHTML.22_in_citation_templates
            // remove format= field with null value when URL is HTML page
            if (Tools.GetTemplateParameterValue(newValue, "format").TrimStart("[]".ToCharArray()).ToUpper().StartsWith("HTM")
                ||
                (Tools.GetTemplateParameterValue(newValue, "format").Length == 0 &&
                 theURL.ToUpper().TrimEnd('L').EndsWith("HTM")))
                newValue = Tools.RemoveTemplateParameter(newValue, "format");

            // newlines to spaces in title field if URL used, otherwise display broken
            if (theURL.Length > 0)
            {
                string theTitle = Tools.GetTemplateParameterValue(newValue, "title");

                if (theTitle.Contains("\r\n"))
                    newValue = Tools.UpdateTemplateParameterValue(newValue, "title", theTitle.Replace("\r\n", " "));
            }

            // remove language=English on en-wiki
            string lang = Tools.GetTemplateParameterValue(newValue, "language").ToLower();
            if (lang.Equals("english") || lang.Equals("en"))
                newValue = Tools.RemoveTemplateParameter(newValue, "language");

            // remove italics for work field for book/periodical, but not website -- auto italicised by template
            if (!Tools.GetTemplateParameterValue(newValue, "work").Contains("."))
                newValue = WorkInItalics.Replace(newValue, "$1$2");

            // remove quotes around title field: are automatically added by template markup
            foreach (string dequoteParam in ParametersToDequote)
            {
                string theTitle = Tools.GetTemplateParameterValue(newValue, dequoteParam);

                // convert curly quotes to straight quotes per [[MOS:PUNCT]], except when »/« is section delimeter
                if ((theTitle.Contains(@"»") && theTitle.Contains(@"«")) || (!theTitle.Contains(@"»") && !theTitle.Contains(@"«")))
                    theTitle = WikiRegexes.CurlyDoubleQuotes.Replace(theTitle, @"""");

                if (theTitle.Contains(@"""") && !theTitle.Trim('"').Contains(@""""))
                    theTitle = theTitle.Trim('"');

                newValue = Tools.SetTemplateParameterValue(newValue, dequoteParam, theTitle);
            }

            // page= and pages= fields don't need p. or pp. in them when nopp not set
            if (Tools.GetTemplateParameterValue(newValue, "nopp").Length == 0 &&
                !templatename.Equals("cite journal", StringComparison.OrdinalIgnoreCase))
                newValue = CiteTemplatePagesPP.Replace(newValue, "");

            // date = YYYY --> year = YYYY; not for {{cite video}}
            if (!CiteVideoPodcast.IsMatch(templatename))
                newValue = YearInDate.Replace(newValue, "$1year$2");

            // year = full date --> date = full date
            string TheYear = Tools.GetTemplateParameterValue(newValue, "year");
            if (WikiRegexes.ISODates.IsMatch(TheYear) || WikiRegexes.InternationalDates.IsMatch(TheYear)
                || WikiRegexes.AmericanDates.IsMatch(TheYear))
                newValue = Tools.RenameTemplateParameter(newValue, "year", "date");

            // author field typos
            if (templatename.Equals("cite web", StringComparison.OrdinalIgnoreCase))
            {
                newValue = Tools.RenameTemplateParameter(newValue, "authors", "author");
                newValue = Tools.RenameTemplateParameter(newValue, "coauthor", "coauthors");
            }

            // remove duplicated fields, ensure the URL is not touched (may have pipes in)
            if (DupeFields.IsMatch(newValue))
                newValue = Tools.RemoveDuplicateTemplateParameters(newValue);

            // year=YYYY and date=...YYYY -> remove year; not for year=YYYYa
            TheYear = Tools.GetTemplateParameterValue(newValue, "year");
            string TheDate = Tools.GetTemplateParameterValue(newValue, "date");

            if (YearOnly.IsMatch(TheYear) && TheDate.Contains(TheYear) && (WikiRegexes.InternationalDates.IsMatch(TheDate)
                                                                           || WikiRegexes.AmericanDates.IsMatch(TheDate)
                                                                           || WikiRegexes.ISODates.IsMatch(TheDate)))
            {
                TheYear = "";
                newValue = Tools.RemoveTemplateParameter(newValue, "year");
            }

            // month=Month and date=...Month... OR month=nn and date=same month
            int num=0;
            string TheMonth = Tools.GetTemplateParameterValue(newValue, "month");
            if ((TheMonth.Length > 2 && TheDate.Contains(TheMonth))
                || (int.TryParse(TheMonth, out num) && Regex.IsMatch(Tools.ConvertDate(TheDate, Parsers.DateLocale.ISO), @"\-0?" + TheMonth + @"\-")))
                newValue = Tools.RemoveTemplateParameter(newValue, "month");

            // date = Month DD and year = YYYY --> date = Month DD, YYYY
            if (!YearOnly.IsMatch(TheDate) && YearOnly.IsMatch(TheYear))
            {
                if (!WikiRegexes.AmericanDates.IsMatch(TheDate) && WikiRegexes.AmericanDates.IsMatch(TheDate + ", " + TheYear))
                {
                    newValue = Tools.SetTemplateParameterValue(newValue, "date", TheDate + ", " + TheYear);
                    newValue = Tools.RemoveTemplateParameter(newValue, "year");
                }
                else if (!WikiRegexes.InternationalDates.IsMatch(TheDate) && WikiRegexes.InternationalDates.IsMatch(TheDate + " " + TheYear))
                {
                    newValue = Tools.SetTemplateParameterValue(newValue, "date", TheDate + " " + TheYear);
                    newValue = Tools.RemoveTemplateParameter(newValue, "year");
                }
            }

            // correct volume=vol 7... and issue=no. 8 for {{cite journal}} only
            if (templatename.Equals("cite journal", StringComparison.OrdinalIgnoreCase))
            {
                newValue = CiteTemplatesJournalVolume.Replace(newValue, "");
                newValue = CiteTemplatesJournalIssue.Replace(newValue, "");

                if (Tools.GetTemplateParameterValue(newValue, "issue").Length == 0)
                    newValue = CiteTemplatesJournalVolumeAndIssue.Replace(newValue, @"| issue = ");
            }

            // {{cite web}} for Google books -> {{Cite book}}
            if (Regex.IsMatch(templatename, @"[Cc]ite ?web") && newValue.Contains("http://books.google.")
                && Tools.GetTemplateParameterValue(newValue, "work").Length == 0)
                newValue = Tools.RenameTemplate(newValue, templatename, "Cite book");

            // remove leading zero in day of month
            newValue = DateLeadingZero.Replace(newValue, @"$1$2$3$4$5");
            newValue = DateLeadingZero.Replace(newValue, @"$1$2$3$4$5");

            if (Regex.IsMatch(templatename, @"[Cc]ite(?: ?web| book| news)"))
            {
                // remove any empty accessdaymonth, accessmonthday, accessmonth and accessyear
                newValue = AccessDayMonthDay.Replace(newValue, "");

                // merge accessdate of 'D Month' or 'Month D' and accessyear of 'YYYY' in cite web
                newValue = AccessDateYear.Replace(newValue, @" $2$1$3");
            }

            // remove accessyear where accessdate is present and contains said year
            string accessyear = Tools.GetTemplateParameterValue(newValue, "accessyear");
            if (accessyear.Length > 0 && Tools.GetTemplateParameterValue(newValue, "accessdate").Contains(accessyear))
                newValue = Tools.RemoveTemplateParameter(newValue, "accessyear");

            // fix unspaced comma ranges, avoid pages=12,345 as could be valid page number
            if (Regex.Matches(Tools.GetTemplateParameterValue(newValue, "pages"), @"\b\d{1,2},\d{3}\b").Count == 0)
            {
                while (UnspacedCommaPageRange.IsMatch(Tools.GetTemplateParameterValue(newValue, "pages")))
                    newValue = Tools.UpdateTemplateParameterValue(newValue, "pages",
                                                                  UnspacedCommaPageRange.Replace(Tools.GetTemplateParameterValue(newValue, "pages"), "$1, $2"));
            }

            // page range should have unspaced en-dash; validate that page is range not section link
            newValue = FixPageRanges(newValue);

            // page range or list should use 'pages' parameter not 'page'
            if (CiteTemplatesPageRangeName.IsMatch(newValue))
            {
                newValue = CiteTemplatesPageRangeName.Replace(newValue, @"$1pages$2");
                newValue = Tools.RemoveDuplicateTemplateParameters(newValue);
            }

            // remove ordinals from dates
            if (OrdinalsInDatesInt.IsMatch(newValue))
            {
                newValue = Tools.UpdateTemplateParameterValue(newValue, "date", OrdinalsInDatesInt.Replace(Tools.GetTemplateParameterValue(newValue, "date"), "$1$2$3 $4"));
                newValue = Tools.UpdateTemplateParameterValue(newValue, "accessdate", OrdinalsInDatesInt.Replace(Tools.GetTemplateParameterValue(newValue, "accessdate"), "$1$2$3 $4"));
            }

            if (OrdinalsInDatesAm.IsMatch(newValue))
            {
                newValue = Tools.UpdateTemplateParameterValue(newValue, "date", OrdinalsInDatesAm.Replace(Tools.GetTemplateParameterValue(newValue, "date"), "$1 $2$3"));
                newValue = Tools.UpdateTemplateParameterValue(newValue, "accessdate", OrdinalsInDatesAm.Replace(Tools.GetTemplateParameterValue(newValue, "accessdate"), "$1 $2$3"));
            }

            // catch after any other fixes
            newValue = NoCommaAmericanDates.Replace(newValue, @"$1, $2");

            // URL starting www. needs http://
            if (theURL.StartsWith("www."))
                newValue = Tools.UpdateTemplateParameterValue(newValue, "url", "http://" + theURL);

            // {{dead link}} should be placed outside citation, not in format field per [[Template:Dead link]]
            string FormatField = Tools.GetTemplateParameterValue(newValue, "format");
            if (WikiRegexes.DeadLink.IsMatch(FormatField))
            {
                string deadLink = WikiRegexes.DeadLink.Match(FormatField).Value;

                if (theURL.ToUpper().TrimEnd('L').EndsWith("HTM") && FormatField.Equals(deadLink))
                    newValue = Tools.RemoveTemplateParameter(newValue, "format");
                else
                    newValue = Tools.UpdateTemplateParameterValue(newValue, "format", FormatField.Replace(deadLink, ""));

                newValue += (" " + deadLink);
            }

            //id=ISBN fix
            if (IdISBN.IsMatch(id) && Tools.GetTemplateParameterValue(newValue, "isbn").Length == 0 && Tools.GetTemplateParameterValue(newValue, "ISBN").Length == 0)
            {
                newValue = Tools.RenameTemplateParameter(newValue, "id", "isbn");
                newValue = Tools.SetTemplateParameterValue(newValue, "isbn", IdISBN.Match(id).Groups[1].Value.Trim());
            }

            //id=ASIN fix
            if (IdASIN.IsMatch(id) && Tools.GetTemplateParameterValue(newValue, "asin").Length == 0 && Tools.GetTemplateParameterValue(newValue, "ASIN").Length == 0)
            {
                newValue = Tools.RenameTemplateParameter(newValue, "id", "asin");
                newValue = Tools.SetTemplateParameterValue(newValue, "asin", IdASIN.Match(id).Groups[1].Value.Trim());
            }

            // origyear --> year when no year/date
            if (TheYear.Length == 0 && TheDate.Length == 0 && Tools.GetTemplateParameterValue(newValue, "origyear").Length == 4)
            {
                newValue = Tools.RenameTemplateParameter(newValue, "origyear", "year");
                newValue = Tools.RemoveDuplicateTemplateParameters(newValue);
            }

            return newValue;
        }

        private static List<string> PageFields = new List<string>(new[] { "page", "pages", "p", "pp" });
        private static readonly Regex PageRange = new Regex(@"\b(\d+)\s*[-—]+\s*(\d+)", RegexOptions.Compiled);
        private static readonly Regex SpacedPageRange = new Regex(@"(\d+) +(–|&ndash;) +(\d)", RegexOptions.Compiled);

        /// <summary>
        /// Converts hyphens in page ranges in citation template fields to endashes
        /// </summary>
        /// <param name="templateCall">The template call</param>
        /// <returns>The updated template call</returns>
        private static string FixPageRanges(string templateCall)
        {
            foreach (string pageField in PageFields)
            {
                string pageRange = Tools.GetTemplateParameterValue(templateCall, pageField);

                // fix spaced page ranges e.g. 15 – 20 --> 15–20
                if (SpacedPageRange.IsMatch(pageRange))
                {
                    pageRange = SpacedPageRange.Replace(pageRange, "$1$2$3");
                    return Tools.SetTemplateParameterValue(templateCall, pageField, pageRange);
                }

                if (pageRange.Length > 2 && !pageRange.Contains(" to "))
                {
                    bool pagerangesokay = true;
                    Dictionary<int, int> PageRanges = new Dictionary<int, int>();

                    foreach (Match pagerange in PageRange.Matches(pageRange))
                    {
                        string page1 = pagerange.Groups[1].Value;
                        string page2 = pagerange.Groups[2].Value;

                        // convert 350-2 into 350-352 etc.
                        if (page1.Length > page2.Length)
                            page2 = page1.Substring(0, page1.Length - page2.Length) + page2;

                        // check a valid range with difference < 999
                        if (Convert.ToInt32(page1) < Convert.ToInt32(page2) &&
                            Convert.ToInt32(page2) - Convert.ToInt32(page1) < 999)
                            pagerangesokay = true;
                        else
                            pagerangesokay = false;

                        // check range doesn't overlap with another range found
                        foreach (KeyValuePair<int, int> kvp in PageRanges)
                        {
                            // check if page 1 or page 2 within existing range
                            if ((Convert.ToInt32(page1) >= kvp.Key && Convert.ToInt32(page1) <= kvp.Value) || (Convert.ToInt32(page2) >= kvp.Key && Convert.ToInt32(page2) <= kvp.Value))
                            {
                                pagerangesokay = false;
                                break;
                            }
                        }

                        if (!pagerangesokay)
                            break;

                        // add to dictionary of ranges found
                        PageRanges.Add(Convert.ToInt32(page1), Convert.ToInt32(page2));
                    }

                    if (pagerangesokay)
                        templateCall = Tools.UpdateTemplateParameterValue(templateCall, pageField, PageRange.Replace(pageRange, @"$1–$2"));
                }
            }

            return templateCall;
        }

        private static readonly Regex CiteWebOrNews = Tools.NestedTemplateRegex(new[] { "cite web", "citeweb", "cite news", "citenews" });
        private static readonly Regex PressPublishers = new Regex(@"(Associated Press|United Press International)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly List<string> WorkParameterAndAliases = new List<string>(new[] { "work", "newspaper", "journal", "periodical", "magazine" });

        /// <summary>
        /// Where the publisher field is used incorrectly instead of the work field in a {{cite web}} or {{cite news}} citation
        /// convert the parameter to be 'work'
        /// Scenarios covered:
        /// * publisher == URL domain, no work= used
        /// </summary>
        /// <param name="citation">the citaiton</param>
        /// <returns>the updated citation</returns>
        public static string CitationPublisherToWork(string citation)
        {
            // only for {{cite web}} or {{cite news}}
            if (!CiteWebOrNews.IsMatch(citation))
                return citation;

            string publisher = Tools.GetTemplateParameterValue(citation, "publisher");

            // nothing to do if no publisher, or publisher is a press publisher
            if (publisher.Length == 0 | PressPublishers.IsMatch(publisher))
                return citation;

            List<string> workandaliases = Tools.GetTemplateParametersValues(citation, WorkParameterAndAliases);

            if (string.Join("", workandaliases.ToArray()).Length == 0)
            {
                citation = Tools.RenameTemplateParameter(citation, "publisher", "work");
                citation = WorkInItalics.Replace(citation, "$1$2");
            }

            return citation;
        }

        /// <summary>
        /// Matches the {{birth date}} family of templates
        /// </summary>
        private static readonly Regex BirthDate = Tools.NestedTemplateRegex(new List<string>(new[] { "birth date", "birth-date", "dob", "bda", "birth date and age", "birthdate and age", "Date of birth and age", "BDA", "Birthdateandage",
                                                                                                 "Birth Date and age", "birthdate" }));

        /// <summary>
        /// Matches the {{death  date}} family of templates
        /// </summary>
        private static readonly Regex DeathDate = Tools.NestedTemplateRegex(new List<string>(new[] { "death date", "death-date", "dda", "death date and age", "deathdateandage", "deathdate" }));

        /// <summary>
        /// * Adds the default {{persondata}} template to en-wiki mainspace pages about a person that don't already have {{persondata}}
        /// * Attempts to complete blank {{persondata}} fields based on infobox values
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <returns></returns>
        public static string PersonData(string articleText, string articleTitle)
        {
            if (!Variables.LangCode.Equals("en")
                || WikiRegexes.Persondata.Matches(articleText).Count > 1)
                return articleText;

            string originalPersonData = "", newPersonData = "";

            // add default persondata if missing
            if (!WikiRegexes.Persondata.IsMatch(articleText))
            {
                if (IsArticleAboutAPerson(articleText, articleTitle, true))
                {
                    articleText = articleText + Tools.Newline(WikiRegexes.PersonDataDefault);
                    newPersonData = originalPersonData = WikiRegexes.PersonDataDefault;
                }
                else
                    return articleText;
            }
            else
            {
                originalPersonData = WikiRegexes.Persondata.Match(articleText).Value;
                newPersonData = originalPersonData;
                // use uppercase parameters if making changes
                newPersonData = Tools.RenameTemplateParameter(newPersonData, "name", "NAME");
                newPersonData = Tools.RenameTemplateParameter(newPersonData, "alternative names", "ALTERNATIVE NAMES");
                newPersonData = Tools.RenameTemplateParameter(newPersonData, "short description", "SHORT DESCRIPTION");
                newPersonData = Tools.RenameTemplateParameter(newPersonData, "date of birth", "DATE OF BIRTH");
                newPersonData = Tools.RenameTemplateParameter(newPersonData, "place of birth", "PLACE OF BIRTH");
                newPersonData = Tools.RenameTemplateParameter(newPersonData, "date of death", "DATE OF DEATH");
                newPersonData = Tools.RenameTemplateParameter(newPersonData, "place of death", "PLACE OF DEATH");
            }

            // attempt completion of some persondata fields

            // name
            if (Tools.GetTemplateParameterValue(newPersonData, "NAME", true).Length == 0)
            {
                string name = WikiRegexes.Defaultsort.Match(articleText).Groups["key"].Value;
                if (name.Contains(" ("))
                    name = name.Substring(0, name.IndexOf(" ("));

                if (name.Length == 0 && Tools.WordCount(articleTitle) == 1)
                    name = articleTitle;

                if (name.Length > 0)
                    newPersonData = Tools.SetTemplateParameterValue(newPersonData, "NAME", name, true);
            }

            // date of birth
            if (Tools.GetTemplateParameterValue(newPersonData, "DATE OF BIRTH", true).Length == 0)
                newPersonData = SetPersonDataDate(newPersonData, "DATE OF BIRTH", GetInfoBoxFieldValue(articleText, WikiRegexes.InfoBoxDOBFields), articleText);

            // as fallback use year from category
            if (Tools.GetTemplateParameterValue(newPersonData, "DATE OF BIRTH", true).Length == 0)
            {
                Match m = WikiRegexes.BirthsCategory.Match(articleText);

                if (m.Success)
                {
                    string year = m.Value.Replace(@"[[Category:", "").TrimEnd(']');
                    if (Regex.IsMatch(year, @"^\d{3,4} (?:BC )?births(\|.*)?$"))
                        newPersonData = Tools.SetTemplateParameterValue(newPersonData, "DATE OF BIRTH", year.Substring(0, year.IndexOf(" births")), true);
                }
            }

            // date of death
            if (Tools.GetTemplateParameterValue(newPersonData, "DATE OF DEATH", true).Length == 0)
                newPersonData = SetPersonDataDate(newPersonData, "DATE OF DEATH", GetInfoBoxFieldValue(articleText, WikiRegexes.InfoBoxDODFields), articleText);

            // as fallback use year from category
            if (Tools.GetTemplateParameterValue(newPersonData, "DATE OF DEATH", true).Length == 0)
            {
                Match m = WikiRegexes.DeathsOrLivingCategory.Match(articleText);

                if (m.Success)
                {
                    string year = m.Value.Replace(@"[[Category:", "").TrimEnd(']');
                    if (Regex.IsMatch(year, @"^\d{3,4} deaths(\|.*)?$"))
                        newPersonData = Tools.SetTemplateParameterValue(newPersonData, "DATE OF DEATH", year.Substring(0, year.IndexOf(" deaths")), true);
                }
            }

            // place of birth
            string ExistingPOB = Tools.GetTemplateParameterValue(newPersonData, "PLACE OF BIRTH", true);
            if (ExistingPOB.Length == 0)
            {
                string POB = GetInfoBoxFieldValue(articleText, WikiRegexes.InfoBoxPOBFields);

                // as fallback look up cityofbirth/countryofbirth
                if (POB.Length == 0)
                {
                    string ib = WikiRegexes.InfoBox.Match(articleText).Value;
                    POB = (Tools.GetTemplateParameterValue(ib, "cityofbirth") + ", " + Tools.GetTemplateParameterValue(ib, "countryofbirth")).Trim(',');
                }

                POB = WikiRegexes.FileNamespaceLink.Replace(POB, "").Trim();

                POB = WikiRegexes.NestedTemplates.Replace(WikiRegexes.Br.Replace(POB, " "), "").Trim();
                POB = WikiRegexes.Small.Replace(WikiRegexes.Refs.Replace(POB, ""), "$1").TrimEnd(',');

                newPersonData = Tools.SetTemplateParameterValue(newPersonData, "PLACE OF BIRTH", POB, true);
            }

            // place of death
            string ExistingPOD = Tools.GetTemplateParameterValue(newPersonData, "PLACE OF DEATH", true);
            if (ExistingPOD.Length == 0)
            {
                string POD = GetInfoBoxFieldValue(articleText, WikiRegexes.InfoBoxPODFields);

                // as fallback look up cityofbirth/countryofbirth
                if (POD.Length == 0)
                {
                    string ib = WikiRegexes.InfoBox.Match(articleText).Value;
                    POD = (Tools.GetTemplateParameterValue(ib, "cityofdeath") + ", " + Tools.GetTemplateParameterValue(ib, "countryofdeath")).Trim(',');
                }

                POD = WikiRegexes.FileNamespaceLink.Replace(POD, "").Trim();
                POD = WikiRegexes.NestedTemplates.Replace(WikiRegexes.Br.Replace(POD, " "), "").Trim();
                POD = WikiRegexes.Small.Replace(WikiRegexes.Refs.Replace(POD, ""), "$1").TrimEnd(',');

                newPersonData = Tools.SetTemplateParameterValue(newPersonData, "PLACE OF DEATH", POD, true);
            }

            // look for full dates matching birth/death categories
            newPersonData = Tools.RemoveDuplicateTemplateParameters(CompletePersonDataDate(newPersonData, articleText));

            // merge changes
            if (!newPersonData.Equals(originalPersonData) && newPersonData.Length > originalPersonData.Length)
                articleText = articleText.Replace(originalPersonData, newPersonData);

            return articleText;
        }

        private static List<string> DfMf = new List<string>(new[] { "df", "mf" });

        /// <summary>
        /// Completes a persondata call with a date of birth/death.
        /// </summary>
        /// <param name="personData"></param>
        /// <param name="field"></param>
        /// <param name="sourceValue"></param>
        /// <param name="articletext"></param>
        /// <returns>The updated persondata call</returns>
        private static string SetPersonDataDate(string personData, string field, string sourceValue, string articletext)
        {
            string dateFound = "";

            if (field.Equals("DATE OF BIRTH") && BirthDate.IsMatch(articletext))
            {
                sourceValue = Tools.RemoveTemplateParameters(BirthDate.Match(articletext).Value, DfMf);
                dateFound = Tools.GetTemplateArgument(sourceValue, 1);

                // first argument is a year, or a full date
                if (dateFound.Length < 5)
                    dateFound += ("-" + Tools.GetTemplateArgument(sourceValue, 2) + "-" + Tools.GetTemplateArgument(sourceValue, 3));
            }
            else if (field.Equals("DATE OF DEATH") && DeathDate.IsMatch(articletext))
            {
                sourceValue = Tools.RemoveTemplateParameters(DeathDate.Match(articletext).Value, DfMf);
                dateFound = Tools.GetTemplateArgument(sourceValue, 1);
                if (dateFound.Length < 5)
                    dateFound += ("-" + Tools.GetTemplateArgument(sourceValue, 2) + "-" + Tools.GetTemplateArgument(sourceValue, 3));
            }
            else if (WikiRegexes.AmericanDates.IsMatch(sourceValue))
                dateFound = WikiRegexes.AmericanDates.Match(sourceValue).Value;
            else if (WikiRegexes.InternationalDates.IsMatch(sourceValue))
                dateFound = WikiRegexes.InternationalDates.Match(sourceValue).Value;
            else if (WikiRegexes.ISODates.IsMatch(sourceValue))
                dateFound = WikiRegexes.ISODates.Match(sourceValue).Value;

            // if date not found yet, fall back to year/month/day of brith fields or birth date in {{dda}}
            if (dateFound.Length == 0)
            {
                if (field.Equals("DATE OF BIRTH"))
                {
                    if (GetInfoBoxFieldValue(articletext, "yearofbirth").Length > 0)
                        dateFound = (GetInfoBoxFieldValue(articletext, "yearofbirth") + "-" + GetInfoBoxFieldValue(articletext, "monthofbirth") + "-" + GetInfoBoxFieldValue(articletext, "dayofbirth")).Trim('-');
                    else if (GetInfoBoxFieldValue(articletext, "yob").Length > 0)
                        dateFound = (GetInfoBoxFieldValue(articletext, "yob") + "-" + GetInfoBoxFieldValue(articletext, "mob") + "-" + GetInfoBoxFieldValue(articletext, "dob")).Trim('-');
                    else if (WikiRegexes.DeathDateAndAge.IsMatch(articletext))
                    {
                        string dda = Tools.RemoveTemplateParameters(WikiRegexes.DeathDateAndAge.Match(articletext).Value, DfMf);
                        dateFound = (Tools.GetTemplateArgument(dda, 4) + "-" + Tools.GetTemplateArgument(dda, 5) + "-" + Tools.GetTemplateArgument(dda, 6)).Trim('-');
                    }
                    else if (GetInfoBoxFieldValue(articletext, "birthyear").Length > 0)
                        dateFound = (GetInfoBoxFieldValue(articletext, "birthyear") + "-" + GetInfoBoxFieldValue(articletext, "birthmonth") + "-" + GetInfoBoxFieldValue(articletext, "birthday")).Trim('-');
                }
                else if (field.Equals("DATE OF DEATH"))
                {
                    if (GetInfoBoxFieldValue(articletext, "yearofdeath").Length > 0)
                        dateFound = (GetInfoBoxFieldValue(articletext, "yearofdeath") + "-" + GetInfoBoxFieldValue(articletext, "monthofdeath") + "-" + GetInfoBoxFieldValue(articletext, "dayofdeath")).Trim('-');
                    else if (GetInfoBoxFieldValue(articletext, "deathyear").Length > 0)
                        dateFound = (GetInfoBoxFieldValue(articletext, "deathyear") + "-" + GetInfoBoxFieldValue(articletext, "deathmonth") + "-" + GetInfoBoxFieldValue(articletext, "deathday")).Trim('-');
                    else if (GetInfoBoxFieldValue(articletext, "yod").Length > 0)
                        dateFound = (GetInfoBoxFieldValue(articletext, "yod") + "-" + GetInfoBoxFieldValue(articletext, "mod") + "-" + GetInfoBoxFieldValue(articletext, "dod")).Trim('-');
                }
            }

            // call parser function for futher date fixes
            dateFound = WikiRegexes.Comments.Replace(CiteTemplateDates(@"{{cite web|date=" + dateFound + @"}}").Replace(@"{{cite web|date=", "").Trim('}'), "");

            dateFound = Tools.ConvertDate(dateFound, DeterminePredominantDateLocale(articletext, false)).Trim('-');

            // check ISO dates valid (in case dda used zeros for month/day)
            if (dateFound.Contains("-") && !WikiRegexes.ISODates.IsMatch(dateFound))
                return personData;

            return Tools.SetTemplateParameterValue(personData, field, dateFound, true);
        }

        private static readonly Regex BracketedBirthDeathDate = new Regex(@"\(([^()]+)\)", RegexOptions.Compiled);
        private static readonly Regex FreeFormatDied = new Regex(@"(?:(?:&nbsp;| )(?:-|–|&ndash;)(?:&nbsp;| )|\b[Dd](?:ied\b|\.))", RegexOptions.Compiled);

        /// <summary>
        /// Sets persondata date of birth/death fields based on unformatted info in zeroth section of article, provided dates match existing birth/death categories
        /// </summary>
        /// <param name="personData">Persondata template call</param>
        /// <param name="articletext">The article text</param>
        /// <returns>The updated persondata template call</returns>
        private static string CompletePersonDataDate(string personData, string articletext)
        {
            // get the existing values
            string existingBirthYear = Tools.GetTemplateParameterValue(personData, "DATE OF BIRTH", true);
            string existingDeathYear = Tools.GetTemplateParameterValue(personData, "DATE OF DEATH", true);

            if (existingBirthYear.Length == 4 || existingDeathYear.Length == 4)
            {
                string birthDateFound = "", deathDateFound = "";
                string zerothSection = WikiRegexes.ZerothSection.Match(articletext).Value;

                // remove references, wikilinks, templates
                zerothSection = WikiRegexes.Refs.Replace(zerothSection, " ");
                zerothSection = WikiRegexes.SimpleWikiLink.Replace(zerothSection, " ");

                if (WikiRegexes.CircaTemplate.IsMatch(zerothSection))
                    zerothSection = zerothSection.Substring(0, WikiRegexes.CircaTemplate.Match(zerothSection).Index);

                zerothSection = Tools.NestedTemplateRegex("ndash").Replace(zerothSection, " &ndash;");
                zerothSection = WikiRegexes.NestedTemplates.Replace(zerothSection, " ");

                // look for date in bracketed text, check date matches existing value (from categories)
                foreach (Match m in BracketedBirthDeathDate.Matches(zerothSection))
                {
                    string bValue = m.Value;

                    if (!UncertainWordings.IsMatch(bValue) && !ReignedRuledUnsure.IsMatch(bValue) && !FloruitTemplate.IsMatch(bValue))
                    {

                        string bBorn = "", bDied = "";
                        // split on died/spaced dash
                        if (FreeFormatDied.IsMatch(bValue))
                        {
                            bBorn = bValue.Substring(0, FreeFormatDied.Match(bValue).Index);
                            bDied = bValue.Substring(FreeFormatDied.Match(bValue).Index);
                        }
                        else
                            bBorn = bValue;

                        // born
                        if (existingBirthYear.Length == 4)
                        {
                            if (WikiRegexes.AmericanDates.Matches(bBorn).Count == 1 && WikiRegexes.AmericanDates.Match(bBorn).Value.Contains(existingBirthYear))
                                birthDateFound = WikiRegexes.AmericanDates.Match(bBorn).Value;
                            else if (WikiRegexes.InternationalDates.Matches(bBorn).Count == 1 && WikiRegexes.InternationalDates.Match(bBorn).Value.Contains(existingBirthYear))
                                birthDateFound = WikiRegexes.InternationalDates.Match(bBorn).Value;
                        }

                        // died
                        if (existingDeathYear.Length == 4)
                        {
                            if (WikiRegexes.AmericanDates.Matches(bDied).Count == 1 && WikiRegexes.AmericanDates.Match(bDied).Value.Contains(existingDeathYear))
                                deathDateFound = WikiRegexes.AmericanDates.Match(bDied).Value;
                            else if (WikiRegexes.InternationalDates.Matches(bDied).Count == 1 && WikiRegexes.InternationalDates.Match(bDied).Value.Contains(existingDeathYear))
                                deathDateFound = WikiRegexes.InternationalDates.Match(bDied).Value;
                        }

                        if (birthDateFound.Length > 0 || deathDateFound.Length > 0)
                            break;
                    }
                }

                if (birthDateFound.Length > 4)
                    personData = Tools.SetTemplateParameterValue(personData, "DATE OF BIRTH", Tools.ConvertDate(birthDateFound, DeterminePredominantDateLocale(articletext, true)), false);

                if (deathDateFound.Length > 4)
                    personData = Tools.SetTemplateParameterValue(personData, "DATE OF DEATH", Tools.ConvertDate(deathDateFound, DeterminePredominantDateLocale(articletext, true)), false);
            }

            return personData;
        }

        /// <summary>
        /// The in-use date formats on the English Wikipedia
        /// </summary>
        public enum DateLocale { International, American, ISO, Undetermined };

        /// <summary>
        /// Determines the predominant date format in the article text (American/International), if available
        /// </summary>
        /// <param name="articleText">the article text</param>
        /// <returns>The date locale determined</returns>
        public static DateLocale DeterminePredominantDateLocale(string articleText)
        {
            return DeterminePredominantDateLocale(articleText, false, false);
        }

        /// <summary>
        /// Determines the predominant date format in the article text (American/International/ISO), if available
        /// </summary>
        /// <param name="articleText">the article text</param>
        /// <param name="considerISO">whether to consider ISO as a possible predominant date format</param>
        /// <returns>The date locale determined</returns>
        public static DateLocale DeterminePredominantDateLocale(string articleText, bool considerISO)
        {
            return DeterminePredominantDateLocale(articleText, considerISO, false);
        }

        /// <summary>
        /// Determines the predominant date format in the article text (American/International/ISO), if available
        /// </summary>
        /// <param name="articleText">the article text</param>
        /// <param name="considerISO">whether to consider ISO as a possible predominant date format</param>
        /// <param name="explicitonly">whether to restrict logic to look at {{use xxx dates}} template only</param>
        /// <returns>The date locale determined</returns>
        public static DateLocale DeterminePredominantDateLocale(string articleText, bool considerISO, bool explicitonly)
        {
            // first check for template telling us the preference
            string DatesT = WikiRegexes.UseDatesTemplate.Match(articleText).Groups[2].Value.ToLower();

            DatesT = DatesT.Replace("iso", "ymd");
            DatesT = Regex.Match(DatesT, @"(ymd|dmy|mdy)").Value;

            if (Variables.LangCode == "en" && DatesT.Length > 0)
                switch (DatesT)
                {
                    case "dmy":
                        return DateLocale.International;
                    case "mdy":
                        return DateLocale.American;
                    case "ymd":
                        return DateLocale.ISO;
                }

            if (explicitonly)
                return DateLocale.Undetermined;

            // secondly count the American and International dates
            int Americans = WikiRegexes.MonthDay.Matches(articleText).Count;
            int Internationals = WikiRegexes.DayMonth.Matches(articleText).Count;

            if (considerISO)
            {
                int ISOs = WikiRegexes.ISODates.Matches(articleText).Count;

                if (ISOs > Americans && ISOs > Internationals)
                    return DateLocale.ISO;
            }

            if (Americans != Internationals)
            {
                if (Americans == 0 && Internationals > 0 || (Internationals / Americans >= 2 && Internationals > 4))
                    return DateLocale.International;
                if (Internationals == 0 && Americans > 0 || (Americans / Internationals >= 2 && Americans > 4))
                    return DateLocale.American;
            }

            // check for explicit df or mf in brith/death templates
            if (Tools.GetTemplateParameterValue(BirthDate.Match(articleText).Value, "df").StartsWith("y")
               || Tools.GetTemplateParameterValue(DeathDate.Match(articleText).Value, "df").StartsWith("y"))
                return DateLocale.International;
            else if (Tools.GetTemplateParameterValue(BirthDate.Match(articleText).Value, "mf").StartsWith("y")
                    || Tools.GetTemplateParameterValue(DeathDate.Match(articleText).Value, "mf").StartsWith("y"))
                return DateLocale.American;

            return DateLocale.Undetermined;
        }

        // Covered by: LinkTests.TestCanonicalizeTitle(), incomplete
        /// <summary>
        /// returns URL-decoded link target
        /// </summary>
        public static string CanonicalizeTitle(string title)
        {
            // visible parts of links may contain crap we shouldn't modify, such as refs and external links
            if (!Tools.IsValidTitle(title) || title.Contains(":/"))
                return title;

            Variables.WaitForDelayedRequests();
            string s = CanonicalizeTitleRaw(title);
            if (Variables.UnderscoredTitles.Contains(Tools.TurnFirstToUpper(s)))
            {
                return HttpUtility.UrlDecode(title.Replace("+", "%2B"))
                    .Trim(new[] { '_' });
            }
            return s;
        }

        /// <summary>
        /// Turns a title into its canonical form, could be slow
        /// </summary>
        public static string CanonicalizeTitleAggressively(string title)
        {
            title = Tools.RemoveHashFromPageTitle(title);
            title = Tools.WikiDecode(title).Trim();
            title = Tools.TurnFirstToUpper(title);

            if (title.StartsWith(":"))
                title = title.Remove(0, 1).Trim();

            var pos = title.IndexOf(':');
            if (pos <= 0)
                return title;

            string titlePart = title.Substring(0, pos + 1);
            foreach (var regex in WikiRegexes.NamespacesCaseInsensitive)
            {
                var m = regex.Value.Match(titlePart);
                if (!m.Success || m.Index != 0)
                    continue;

                title = Variables.Namespaces[regex.Key] + Tools.TurnFirstToUpper(title.Substring(pos + 1).Trim());
                break;
            }
            return title;
        }

        private static readonly Regex SingleCurlyBrackets = new Regex(@"{((?>[^\{\}]+|\{(?<DEPTH>)|\}(?<-DEPTH>))*(?(DEPTH)(?!))})", RegexOptions.Compiled);
        private static readonly Regex DoubleSquareBrackets = new Regex(@"\[\[((?>[^\[\]]+|\[(?<DEPTH>)|\](?<-DEPTH>))*(?(DEPTH)(?!))\]\])", RegexOptions.Compiled);
        private static readonly Regex SingleSquareBrackets = new Regex(@"\[((?>[^\[\]]+|\[(?<DEPTH>)|\](?<-DEPTH>))*(?(DEPTH)(?!))\])", RegexOptions.Compiled);
        private static readonly Regex SingleRoundBrackets = new Regex(@"\(((?>[^\(\)]+|\((?<DEPTH>)|\)(?<-DEPTH>))*(?(DEPTH)(?!))\))", RegexOptions.Compiled);
        private static readonly Regex Tags = new Regex(@"\<((?>[^\<\>]+|\<(?<DEPTH>)|\>(?<-DEPTH>))*(?(DEPTH)(?!))\>)", RegexOptions.Compiled);
        private static readonly Regex HideNestedBrackets = new Regex(@"[^\[\]{}<>]\[[^\[\]{}<>]*?&#93;", RegexOptions.Compiled);
        private static readonly Regex AmountComparison = new Regex(@"[<>]\s*\d", RegexOptions.Compiled);

        /// <summary>
        /// Checks the article text for unbalanced brackets, either square or curly
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="bracketLength">integer to hold length of unbalanced bracket found</param>
        /// <returns>Index of any unbalanced brackets found</returns>
        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Missing_opening_or_closing_brackets.2C_table_and_template_markup_.28WikiProject_Check_Wikipedia_.23_10.2C_28.2C_43.2C_46.2C_47.29
        public static int UnbalancedBrackets(string articleText, ref int bracketLength)
        {
            // &#93; is used to replace the ] in external link text, which gives correct markup
            // replace [...&#93; with spaces to avoid matching as unbalanced brackets
            articleText = HideNestedBrackets.Replace(articleText, " ");

            // remove all <math>, <code> stuff etc. where curly brackets are used in singles and pairs
            articleText = Tools.ReplaceWithSpaces(articleText, WikiRegexes.MathPreSourceCodeComments);

            bracketLength = 2;

            int unbalancedfound = UnbalancedBrackets(articleText, "{{", "}}", WikiRegexes.NestedTemplates);
            if (unbalancedfound > -1)
                return unbalancedfound;

            unbalancedfound = UnbalancedBrackets(articleText, "[[", "]]", DoubleSquareBrackets);
            if (unbalancedfound > -1)
                return unbalancedfound;

            bracketLength = 1;

            unbalancedfound = UnbalancedBrackets(articleText, "{", "}", SingleCurlyBrackets);
            if (unbalancedfound > -1)
                return unbalancedfound;

            unbalancedfound = UnbalancedBrackets(articleText, "[", "]", SingleSquareBrackets);
            if (unbalancedfound > -1)
                return unbalancedfound;

            unbalancedfound = UnbalancedBrackets(articleText, "(", ")", SingleRoundBrackets);
            if (unbalancedfound > -1)
                return unbalancedfound;

            // look for unbalanced tags
            unbalancedfound = UnbalancedBrackets(articleText, "<", ">", Tags);
            if (unbalancedfound > -1)
                return unbalancedfound;

            return -1;
        }

        /// <summary>
        /// Checks the article text for unbalanced brackets of the input type
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="openingBrackets"></param>
        /// <param name="closingBrackets"></param>
        /// <param name="bracketsRegex"></param>
        /// <returns>Index of any unbalanced brackets found</returns>
        private static int UnbalancedBrackets(string articleText, string openingBrackets, string closingBrackets, Regex bracketsRegex)
        {
            //TODO: move everything possible to the parent function, however, it shouldn't be performed blindly,
            //without a performance review

            if (openingBrackets == "[") // need to remove double square brackets first
                articleText = Tools.ReplaceWithSpaces(articleText, DoubleSquareBrackets);


            if (openingBrackets == "{") // need to remove double curly brackets first
                articleText = Tools.ReplaceWithSpaces(articleText, WikiRegexes.NestedTemplates);

            // replace all the valid balanced bracket sets with spaces
            articleText = Tools.ReplaceWithSpaces(articleText, bracketsRegex);

            // now return the unbalanced one that's left
            int open = Regex.Matches(articleText, Regex.Escape(openingBrackets)).Count;
            int closed = Regex.Matches(articleText, Regex.Escape(closingBrackets)).Count;

            // for tags don't mark "> 50 cm" as unbalanced
            if (openingBrackets == "<" && AmountComparison.IsMatch(articleText))
                return -1;

            if (open == 0 && closed >= 1)
                return articleText.IndexOf(closingBrackets);

            if (open >= 1)
                return articleText.IndexOf(openingBrackets);

            return -1;
        }

        private static readonly Regex LinkWhitespace1 = new Regex(@" \[\[ ([^\]]{1,30})\]\]", RegexOptions.Compiled);
        private static readonly Regex LinkWhitespace2 = new Regex(@"(?<=\w)\[\[ ([^\]]{1,30})\]\]", RegexOptions.Compiled);
        private static readonly Regex LinkWhitespace3 = new Regex(@"\[\[([^\]]{1,30}?) {2,10}([^\]]{1,30})\]\]", RegexOptions.Compiled);
        private static readonly Regex LinkWhitespace4 = new Regex(@"\[\[([^\]\|]{1,30}) \]\] ", RegexOptions.Compiled);
        private static readonly Regex LinkWhitespace5 = new Regex(@"\[\[([^\]]{1,30}) \]\](?=\w)", RegexOptions.Compiled);

        private static readonly Regex DateLinkWhitespace1 = new Regex(@"\b(\[\[\d\d? " + WikiRegexes.MonthsNoGroup + @"\]\]),? {0,2}(\[\[\d{1,4}\]\])\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DateLinkWhitespace2 = new Regex(@"\b(\[\[" + WikiRegexes.MonthsNoGroup + @" \d\d?\]\]),? {0,2}(\[\[\d{1,4}\]\])\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SectionLinkWhitespace = new Regex(@"(\[\[[^\[\]\|]+)(?: +# *| *# +)([^\[\]]+\]\])(?<!\[\[[ACFJ]# .*)", RegexOptions.Compiled);
        private static readonly Regex Hash = new Regex(@"#", RegexOptions.Compiled);

        // Covered by LinkTests.TestFixLinkWhitespace()
        /// <summary>
        /// Fix leading, trailing and middle spaces in Wikilinks
        /// </summary>
        /// <param name="articleText">The wiki text of the article</param>
        /// <param name="articleTitle">The article title.</param>
        /// <returns>The modified article text.</returns>
        public static string FixLinkWhitespace(string articleText, string articleTitle)
        {
            //remove undesirable space from beginning of wikilink (space before wikilink) - parse this line first
            articleText = LinkWhitespace1.Replace(articleText, " [[$1]]");

            //remove undesirable space from beginning of wikilink and move it outside link - parse this line second
            articleText = LinkWhitespace2.Replace(articleText, " [[$1]]");

            //remove undesirable double space from middle of wikilink (up to 61 characters in wikilink)
            articleText = LinkWhitespace3.Replace(articleText, "[[$1 $2]]");

            //remove undesirable space from end of wikilink (space after wikilink) - parse this line first
            articleText = LinkWhitespace4.Replace(articleText, "[[$1]] ");

            //remove undesirable space from end of wikilink and move it outside link - parse this line second
            articleText = LinkWhitespace5.Replace(articleText, "[[$1]] ");

            //remove undesirable double space between links in date (day first)
            articleText = DateLinkWhitespace1.Replace(articleText, "$1 $2");

            //remove undesirable double space between links in date (day second)
            articleText = DateLinkWhitespace2.Replace(articleText, "$1 $2");

            // [[link #section]] or [[link# section]] --> [[link#section]], don't change if hash in part of text of section link
            articleText = SectionLinkWhitespace.Replace(articleText, m => (Hash.Matches(m.Value).Count == 1) ? m.Groups[1].Value.TrimEnd() + "#" + m.Groups[2].Value.TrimStart() : m.Value);

            // correct [[page# section]] to [[page#section]]
            if (articleTitle.Length > 0)
            {
                Regex sectionLinkWhitespace = new Regex(@"(\[\[" + Regex.Escape(articleTitle) + @"\#)\s+([^\[\]]+\]\])");

                return sectionLinkWhitespace.Replace(articleText, "$1$2");
            }

            return articleText;
        }

        private static readonly Regex UnderscoreTitles = new Regex(@"[Ss]ize_t|[Mm]od_", RegexOptions.Compiled);
        private static readonly Regex InfoBoxSingleAlbum = Tools.NestedTemplateRegex(new[] { "Infobox Single", "Infobox single", "Infobox album", "Infobox Album" });
        private static readonly Regex TaxoboxColour = Tools.NestedTemplateRegex(new[] { "taxobox colour", "taxobox color" });

        // Partially covered by FixMainArticleTests.SelfLinkRemoval()
        /// <summary>
        /// Fixes link syntax, including removal of self links
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <param name="noChange">Value that indicated whether no change was made.</param>
        /// <returns>The modified article text.</returns>
        public static string FixLinks(string articleText, string articleTitle, out bool noChange)
        {
            string articleTextAtStart = articleText;
            string escTitle = Regex.Escape(articleTitle);

            if (InfoBoxSingleAlbum.IsMatch(articleText))
                articleText = FixLinksInfoBoxSingleAlbum(articleText, articleTitle);

            // clean up wikilinks: replace underscores, percentages and URL encoded accents etc.
            articleText = WikiRegexes.WikiLink.Replace(articleText, new MatchEvaluator(FixLinksWikilinkME));

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_11#Your_code_creates_page_errors_inside_imagemap_tags.
            // don't apply if there's an imagemap on the page or some noinclude transclusion business
            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_11#Includes_and_selflinks
            // TODO, better to not apply to text within imagemaps
            if (!WikiRegexes.ImageMap.IsMatch(articleText)
                && !WikiRegexes.Noinclude.IsMatch(articleText)
                && !WikiRegexes.Includeonly.IsMatch(articleText)
                && !TaxoboxColour.IsMatch(articleText))
            {
                // remove any self-links, but not other links with different capitaliastion e.g. [[Foo]] vs [[FOO]]
                articleText = Regex.Replace(articleText, @"\[\[\s*(" + Tools.CaseInsensitive(escTitle)
                                            + @")\s*\]\]", "$1");

                // remove piped self links by leaving target
                articleText = Regex.Replace(articleText, @"\[\[\s*" + Tools.CaseInsensitive(escTitle)
                                            + @"\s*\|\s*([^\]]+)\s*\]\]", "$1");
            }

            // fix for self interwiki links
            articleText = FixSelfInterwikis(articleText);

            noChange = articleText.Equals(articleTextAtStart);
            return articleText;
        }

        private static string FixLinksWikilinkME(Match m)
        {
            string theTarget = m.Groups[1].Value, y = m.Value;
            // don't convert %27%27 -- https://bugzilla.wikimedia.org/show_bug.cgi?id=8932
            if (theTarget.Length > 0 && !theTarget.Contains("%27%27") && !UnderscoreTitles.IsMatch(theTarget))
                y = y.Replace(theTarget, CanonicalizeTitle(theTarget));

            return y;
        }

        /// <summary>
        /// Reformats self interwikis to be standard links. Only applies to self interwikis before other interwikis (i.e. those in body of article)
        /// </summary>
        /// <param name="articleText">The article text</param>
        /// <returns>The updated article text</returns>
        private static string FixSelfInterwikis(string articleText)
        {
            foreach (Match m in WikiRegexes.PossibleInterwikis.Matches(articleText))
            {
                // interwiki should not be to own wiki – convert to standard wikilink
                if (m.Groups[1].Value.Equals(Variables.LangCode))
                    articleText = articleText.Replace(m.Value, @"[[" + m.Groups[2].Value + @"]]");
                else
                    break;
            }

            return articleText;
        }

        /// <summary>
        /// Converts self links for the 'this single/album' field of 'infobox single/album' to bold
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <returns></returns>
        private static string FixLinksInfoBoxSingleAlbum(string articleText, string articleTitle)
        {
            string escTitle = Regex.Escape(articleTitle);
            string lowerTitle = Tools.TurnFirstToLower(escTitle);
            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_11#.22This_album.2Fsingle.22
            // for this single or this album within the infobox, make bold instead of delinking
            const string infoBoxSingleAlbum = @"(?s)(?<={{[Ii]nfobox (?:[Ss]ingle|[Aa]lbum).*?\|\s*[Tt]his (?:[Ss]ingle|[Aa]lbum)\s*=[^{}]*?)\[\[\s*";
            articleText = Regex.Replace(articleText, infoBoxSingleAlbum + escTitle + @"\s*\]\](?=[^{}\|]*(?:\||}}))", @"'''" + articleTitle + @"'''");
            articleText = Regex.Replace(articleText, infoBoxSingleAlbum + lowerTitle + @"\s*\]\](?=[^{}\|]*(?:\||}}))", @"'''" + lowerTitle + @"'''");
            articleText = Regex.Replace(articleText, infoBoxSingleAlbum + escTitle + @"\s*\|\s*([^\]]+)\s*\]\](?=[^{}\|]*(?:\||}}))", @"'''" + "$1" + @"'''");
            articleText = Regex.Replace(articleText, infoBoxSingleAlbum + lowerTitle + @"\s*\|\s*([^\]]+)\s*\]\](?=[^{}\|]*(?:\||}}))", @"'''" + "$1" + @"'''");

            return articleText;
        }

        // covered by CanonicalizeTitleRawTests
        /// <summary>
        /// Performs URL-decoding of a page title, trimming all whitespace
        /// </summary>
        public static string CanonicalizeTitleRaw(string title)
        {
            return CanonicalizeTitleRaw(title, true);
        }

        // covered by CanonicalizeTitleRawTests
        /// <summary>
        /// performs URL-decoding of a page title
        /// </summary>
        /// <param name="title">title to normalise</param>
        /// <param name="trim">whether whitespace should be trimmed</param>
        public static string CanonicalizeTitleRaw(string title, bool trim)
        {
            title = HttpUtility.UrlDecode(title.Replace("+", "%2B").Replace('_', ' '));
            return trim ? title.Trim() : title;
        }

        // Covered by: LinkTests.TestSimplifyLinks()
        /// <summary>
        /// Simplifies some links in article wiki text such as changing [[Dog|Dogs]] to [[Dog]]s
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The simplified article text.</returns>
        public static string SimplifyLinks(string articleText)
        {
            foreach (Match m in WikiRegexes.PipedWikiLink.Matches(articleText))
            {
                string n = m.Value;
                string a = m.Groups[1].Value.Trim();

                string b = (Namespace.Determine(a) != Namespace.Category)
                    ? m.Groups[2].Value.Trim()
                    : m.Groups[2].Value.TrimEnd(new[] { ' ' });

                if (b.Length == 0)
                    continue;

                if (a == b || Tools.TurnFirstToLower(a) == b)
                {
                    articleText = articleText.Replace(n, "[[" + b + "]]");
                }
                else if (Tools.TurnFirstToLower(b).StartsWith(Tools.TurnFirstToLower(a), StringComparison.Ordinal))
                {
                    bool doBreak = false;
                    foreach (char ch in b.Remove(0, a.Length))
                    {
                        if (!char.IsLower(ch))
                        {
                            doBreak = true;
                            break;
                        }
                    }
                    if (doBreak)
                        continue;
                    articleText = articleText.Replace(n, "[[" + b.Substring(0, a.Length) + "]]" + b.Substring(a.Length));
                }
                else
                {
                    string newlink = "[[" + a + "|" + b + "]]";

                    if (newlink != n)
                        articleText = articleText.Replace(n, newlink);
                }
            }

            return articleText;
        }

        // Covered by: LinkTests.TestStickyLinks()
        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs#Link_simplification_too_greedy_-_eating_spaces -- disabled as genfix
        /// <summary>
        /// Joins nearby words with links
        ///   e.g. "[[Russian literature|Russian]] literature" to "[[Russian literature]]"
        /// </summary>
        /// <param name="articleText">The wiki text of the article</param>
        /// <returns>Processed wikitext</returns>
        public static string StickyLinks(string articleText)
        {
            foreach (Match m in WikiRegexes.PipedWikiLink.Matches(articleText))
            {
                string a = m.Groups[1].Value;
                string b = m.Groups[2].Value;

                if (b.Trim().Length == 0 || a.Contains(","))
                    continue;

                if (Tools.TurnFirstToLower(a).StartsWith(Tools.TurnFirstToLower(b), StringComparison.Ordinal))
                {
                    bool hasSpace = false;

                    if (a.Length > b.Length)
                        hasSpace = a[b.Length] == ' ';

                    string search = @"\[\[" + Regex.Escape(a) + @"\|" + Regex.Escape(b) +
                        @"\]\]" + (hasSpace ? "[ ]+" : "") + Regex.Escape(a.Remove(0,
                                                                                   b.Length + (hasSpace ? 1 : 0))) + @"\b";

                    //first char should be capitalized like in the visible part of the link
                    a = a.Remove(0, 1).Insert(0, b[0] + "");
                    articleText = Regex.Replace(articleText, search, "[[" + a + @"]]");
                }
            }

            return articleText;
        }

        private static readonly Regex RegexMainArticle = new Regex(@"^:?'{0,5}Main article:\s?'{0,5}\[\[([^\|\[\]]*?)(\|([^\[\]]*?))?\]\]\.?'{0,5}\.?\s*?(?=($|[\r\n]))", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex SeeAlsoLink = new Regex(@"^:?'{0,5}See [Aa]lso:\s?'{0,5}\[\[([^\|\[\]]*?)(\|([^\[\]]*?))?\]\]\.?'{0,5}\.?\s*?(?=($|[\r\n]))", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

        // Covered by: FixMainArticleTests
        /// <summary>
        /// Fixes instances of ''Main Article: xxx'' to use {{main|xxx}}
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns></returns>
        public static string FixMainArticle(string articleText)
        {
            articleText = SeeAlsoLink.Replace(articleText,
                                              m => m.Groups[2].Value.Length == 0
                                              ? "{{See also|" + m.Groups[1].Value + "}}"
                                              : "{{See also|" + m.Groups[1].Value + "|l1=" + m.Groups[3].Value + "}}");

            return RegexMainArticle.Replace(articleText,
                                            m => m.Groups[2].Value.Length == 0
                                            ? "{{Main|" + m.Groups[1].Value + "}}"
                                            : "{{Main|" + m.Groups[1].Value + "|l1=" + m.Groups[3].Value + "}}");
        }

        // Covered by LinkTests.TestFixEmptyLinksAndTemplates()
        /// <summary>
        /// Removes Empty Links and Template Links
        /// Will Cater for [[]], [[Image:]], [[:Category:]], [[Category:]] and {{}}
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns></returns>
        public static string FixEmptyLinksAndTemplates(string articleText)
        {
            foreach (Match link in WikiRegexes.EmptyLink.Matches(articleText))
            {
                string trim = link.Groups[2].Value.Trim();
                if (string.IsNullOrEmpty(trim) || trim == "|" + Variables.NamespacesCaseInsensitive[Namespace.Image] ||
                    trim == "|" + Variables.NamespacesCaseInsensitive[Namespace.Category] || trim == "|")
                    articleText = articleText.Replace("[[" + link.Groups[1].Value + link.Groups[2].Value + "]]", "");
            }

            articleText = WikiRegexes.EmptyTemplate.Replace(articleText, "");

            return articleText;
        }

        /// <summary>
        /// Adds bullet points to external links after "external links" header
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="noChange">Value that indicated whether no change was made.</param>
        /// <returns>The modified article text.</returns>
        public static string BulletExternalLinks(string articleText, out bool noChange)
        {
            string newText = BulletExternalLinks(articleText);

            noChange = (newText == articleText);

            return newText;
        }

        private static readonly HideText BulletExternalHider = new HideText(false, true, false);

        private static readonly Regex ExternalLinksSection = new Regex(@"=\s*(?:external)?\s*links\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
        private static readonly Regex NewlinesBeforeHTTP = new Regex("(\r\n|\n)?(\r\n|\n)(\\[?http)", RegexOptions.Compiled);

        // Covered by: LinkTests.TestBulletExternalLinks()
        /// <summary>
        /// Adds bullet points to external links after "external links" header
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public static string BulletExternalLinks(string articleText)
        {
            Match m = ExternalLinksSection.Match(articleText);

            if (!m.Success)
                return articleText;

            int intStart = m.Index;

            string articleTextSubstring = articleText.Substring(intStart);
            articleText = articleText.Substring(0, intStart);
            articleTextSubstring = BulletExternalHider.HideMore(articleTextSubstring);
            articleTextSubstring = NewlinesBeforeHTTP.Replace(articleTextSubstring, "$2* $3");

            return articleText + BulletExternalHider.AddBackMore(articleTextSubstring);
        }

        private static readonly Regex WordWhitespaceEndofline = new Regex(@"(\w+)\s+$", RegexOptions.Compiled);

        // Covered by: LinkTests.TestFixCategories()
        /// <summary>
        /// Fix common spacing/capitalisation errors in categories; remove diacritics and trailing whitespace from sortkeys (not leading whitespace)
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public static string FixCategories(string articleText)
        {
            string cat = "[[" + Variables.Namespaces[Namespace.Category];

            // fix extra brackets: three or more at end
            articleText = Regex.Replace(articleText, @"(?<=" + Regex.Escape(cat) + @"[^\r\n\[\]{}<>]+\]\])\]+", "");
            // three or more at start
            articleText = Regex.Replace(articleText, @"\[+(?=" + Regex.Escape(cat) + @"[^\r\n\[\]{}<>]+\]\])", "");

            foreach (Match m in WikiRegexes.LooseCategory.Matches(articleText))
            {
                if (!Tools.IsValidTitle(m.Groups[1].Value))
                    continue;

                string sortkey = m.Groups[2].Value;

                // diacritic removal in sortkeys on en-wiki only
                if (Variables.LangCode.Equals("en"))
                    sortkey = Tools.RemoveDiacritics(sortkey);

                string x = cat + Tools.TurnFirstToUpper(CanonicalizeTitleRaw(m.Groups[1].Value, false).Trim()) +
                    WordWhitespaceEndofline.Replace(sortkey, "$1") + "]]";
                if (x != m.Value)
                    articleText = articleText.Replace(m.Value, x);
            }

            return articleText;
        }

        /// <summary>
        /// Returns whether the article text has a &lt;noinclude&gt; or &lt;includeonly&gt; or '{{{1}}}' etc. which should not appear on the mainspace
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns></returns>
        public static bool NoIncludeIncludeOnlyProgrammingElement(string articleText)
        {
            return WikiRegexes.Noinclude.IsMatch(articleText) || WikiRegexes.Includeonly.IsMatch(articleText) || Regex.IsMatch(articleText, @"{{{\d}}}");
        }

        // Covered by: ImageTests.BasicImprovements(), incomplete
        /// <summary>
        /// Fix common spacing/capitalisation errors in images
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public static string FixImages(string articleText)
        {
            foreach (Match m in WikiRegexes.LooseImage.Matches(articleText))
            {
                string imageName = m.Groups[2].Value;
                // only apply underscore/URL encoding fixes to image name (group 2)
                // don't convert %27%27 -- https://bugzilla.wikimedia.org/show_bug.cgi?id=8932
                string x = "[[" + Namespace.Normalize(m.Groups[1].Value, 6) + (imageName.Contains("%27%27") ? imageName : CanonicalizeTitle(imageName).Trim()) + m.Groups[3].Value.Trim() + "]]";
                articleText = articleText.Replace(m.Value, x);
            }

            return articleText;
        }

        private static readonly Regex Temperature = new Regex(@"(?:&deg;|&ordm;|º|°)(?:&nbsp;)?\s*([CcFf])(?![A-Za-z])", RegexOptions.Compiled);

        /// <summary>
        /// Fix bad Temperatures
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public static string FixTemperatures(string articleText)
        {
            return Temperature.Replace(articleText, m => "°" + m.Groups[1].Value.ToUpper());
        }

        /// <summary>
        /// Removes space or non-breaking space from percent per [[WP:PERCENT]].
        /// Avoid doing this for more spaces to prevent false positives.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public string FixPercent(string articleText)
        {
        	 // hide items in quotes etc., though this may also hide items within infoboxes etc.
            articleText = HideMoreText(articleText);
            
            articleText = WikiRegexes.Percent.Replace(articleText, " $1%$3");
            
            return AddBackMoreText(articleText);
        }

        	/// <summary>
        /// Apply non-breaking spaces for abbreviated SI units
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public string FixNonBreakingSpaces(string articleText)
        {
            // hide items in quotes etc., though this may also hide items within infoboxes etc.
            articleText = HideMoreText(articleText);

            // only apply um (micrometre) fix on English wiki to avoid German word "um"
            articleText = WikiRegexes.UnitsWithoutNonBreakingSpaces.Replace(articleText, m => (m.Groups[2].Value.StartsWith("um") && !Variables.LangCode.Equals("en")) ? m.Value : m.Groups[1].Value + "&nbsp;" + m.Groups[2].Value);

            articleText = WikiRegexes.ImperialUnitsInBracketsWithoutNonBreakingSpaces.Replace(articleText, "$1&nbsp;$2");

            articleText = WikiRegexes.MetresFeetConversionNonBreakingSpaces.Replace(articleText, @"$1&nbsp;m");

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Pagination
            // add non-breaking space after pp. abbreviation for pages.
            articleText = Regex.Replace(articleText, @"(\b[Pp]?p\.) *(?=[\dIVXCL][^S])", @"$1&nbsp;");

            return AddBackMoreText(articleText);
        }

        /// <summary>
        /// Extracts template using the given match.
        /// </summary>
        private static string ExtractTemplate(string articleText, Match m)
        {
            Regex theTemplate = new Regex(Regex.Escape(m.Groups[1].Value) + @"(?>[^\{\}]+|\{(?<DEPTH>)|\}(?<-DEPTH>))*(?(DEPTH)(?!))}}");

            foreach (Match n in theTemplate.Matches(articleText))
            {
                if (n.Index == m.Index)
                    return theTemplate.Match(articleText).Value;
            }

            return "";
        }

        /// <summary>
        /// Finds first occurrence of a given template in article text.
        /// Handles nested templates correctly.
        /// </summary>
        /// <param name="articleText">Source text</param>
        /// <param name="template">Name of template, can be regex without a group capture</param>
        /// <returns>Template with all params, enclosed in curly brackets</returns>
        public static string GetTemplate(string articleText, string template)
        {
            Regex search = new Regex(@"(\{\{\s*" + Tools.CaseInsensitive(template) + @"\s*)(?:\||\}|<)", RegexOptions.Singleline);

            // remove commented out templates etc. before searching
            string articleTextCleaned = WikiRegexes.UnformattedText.Replace(articleText, "");

            if (search.IsMatch(articleTextCleaned))
            {
                // extract from original article text
                Match m = search.Match(articleText);

                return m.Success ? ExtractTemplate(articleText, m) : "";
            }

            return "";
        }

        /// <summary>
        /// Finds every occurrence of a given template in article text, excludes commented out/nowiki'd templates
        /// Handles nested templates and templates with embedded HTML comments correctly.
        /// </summary>
        /// <param name="articleText">Source text</param>
        /// <param name="template">Name of template</param>
        /// <returns>List of matches found</returns>
        public static List<Match> GetTemplates(string articleText, string template)
        {
            return GetTemplates(articleText, Tools.NestedTemplateRegex(template));
        }

        /// <summary>
        /// Finds all templates in article text excluding commented out/nowiki'd templates.
        /// Handles nested templates and templates with embedded HTML comments correctly.
        /// </summary>
        /// <param name="articleText">Source text</param>
        /// <returns>List of matches found</returns>
        public static List<Match> GetTemplates(string articleText)
        {
            return GetTemplates(articleText, WikiRegexes.NestedTemplates);
        }

        /// <summary>
        /// Finds all templates in article text excluding commented out/nowiki'd templates.
        /// Handles nested templates and templates with embedded HTML comments correctly.
        /// </summary>
        /// <param name="articleText">Source text</param>
        /// <param name="search">nested template regex to use</param>
        /// <returns>List of matches found</returns>
        private static List<Match> GetTemplates(string articleText, Regex search)
        {
            List<Match> templateMatches = new List<Match>();
            string articleTextAtStart = articleText;

            // replace with spaces any commented out templates etc., this means index of real matches remains the same as in actual article text
            articleText = Tools.ReplaceWithSpaces(articleText, WikiRegexes.UnformattedText);

            // return matches found in article text at start, provided they exist in cleaned text
            // i.e. don't include matches for commented out/nowiki'd templates
            foreach (Match m in search.Matches(articleText))
            {
                foreach (Match m2 in search.Matches(articleTextAtStart))
                {
                    if (m2.Index.Equals(m.Index))
                    {
                        templateMatches.Add(m2);
                        break;
                    }
                }
            }

            return templateMatches;
        }

        // Covered by: UtilityFunctionTests.RemoveEmptyComments()
        /// <summary>
        /// Removes comments with nothing/only whitespace between tags
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text (removed empty comments).</returns>
        public static string RemoveEmptyComments(string articleText)
        {
            return WikiRegexes.EmptyComments.Replace(articleText, "");
        }
        #endregion

        #region other functions

        /// <summary>
        /// Performs transformations related to Unicode characters that may cause problems for different clients
        /// </summary>
        public string FixUnicode(string articleText)
        {
            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs#Probably_odd_characters_being_treated_even_more_oddly
            articleText = articleText.Replace('\x2029', ' ');

            // https://en.wikipedia.org/wiki/Wikipedia:AWB/B#Line_break_insertion
            // most browsers handle Unicode line separator as whitespace, so should we
            // looks like paragraph separator is properly converted by RichEdit itself
            return articleText.Replace('\x2028', ' ');
        }

        /// <summary>
        /// Converts HTML entities to unicode, with some deliberate exceptions
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="noChange">Value that indicated whether no change was made.</param>
        /// <returns>The modified article text.</returns>
        public string Unicodify(string articleText, out bool noChange)
        {
            string newText = Unicodify(articleText);

            noChange = (newText == articleText);

            return newText;
        }

        private static readonly Regex NDash = new Regex("&#150;|&#8211;|&#x2013;", RegexOptions.Compiled);
        private static readonly Regex MDash = new Regex("&#151;|&#8212;|&#x2014;", RegexOptions.Compiled);
        private static readonly Regex MathTagStart = new Regex("<[Mm]ath>", RegexOptions.Compiled);

        // Covered by: UnicodifyTests
        /// <summary>
        /// Converts HTML entities to unicode, with some deliberate exceptions
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The modified article text.</returns>
        public string Unicodify(string articleText)
        {
            if (MathTagStart.IsMatch(articleText))
                return articleText;

            articleText = NDash.Replace(articleText, "&ndash;");
            articleText = MDash.Replace(articleText, "&mdash;");
            articleText = articleText.Replace(" &amp; ", " & ");
            articleText = articleText.Replace("&amp;", "&amp;amp;");
            articleText = articleText.Replace("&#153;", "™");
            articleText = articleText.Replace("&#149;", "•");

            foreach (KeyValuePair<Regex, string> k in RegexUnicode)
            {
                articleText = k.Key.Replace(articleText, k.Value);
            }
            try
            {
                articleText = HttpUtility.HtmlDecode(articleText);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }

            return articleText;
        }

        /// <summary>
        /// Delinks all bolded self links in the article
        /// </summary>
        /// <param name="articleTitle">Title of the article</param>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns></returns>
        private static string BoldedSelfLinks(string articleTitle, string articleText)
        {
            string escTitle = Regex.Escape(articleTitle);

            Regex r1 = new Regex(@"'''\[\[\s*" + escTitle + @"\s*\]\]'''");
            Regex r2 = new Regex(@"'''\[\[\s*" + Tools.TurnFirstToLower(escTitle) + @"\s*\]\]'''");
            Regex r3 = new Regex(@"'''\[\[\s*" + escTitle + @"\s*\|\s*([^\[\]]+?)\s*\]\]'''");
            Regex r4 = new Regex(@"'''\[\[\s*" + Tools.TurnFirstToLower(escTitle) + @"\s*\|\s*([^\[\]]+?)\s*\]\]'''");

            if (!WikiRegexes.Noinclude.IsMatch(articleText) && !WikiRegexes.Includeonly.IsMatch(articleText))
            {
                articleText = r1.Replace(articleText, @"'''" + articleTitle + @"'''");
                articleText = r2.Replace(articleText, @"'''" + Tools.TurnFirstToLower(articleTitle) + @"'''");
                articleText = r3.Replace(articleText, @"'''$1'''");
                articleText = r4.Replace(articleText, @"'''$1'''");
            }

            return articleText;
        }

        private static readonly Regex BracketedAtEndOfLine = new Regex(@" \(.*?\)$", RegexOptions.Compiled);
        private static readonly Regex BoldTitleAlready3 = new Regex(@"^\s*({{[^\{\}]+}}\s*)*'''('')?\s*\w", RegexOptions.Compiled);
        private static readonly Regex NihongoTitle = Tools.NestedTemplateRegex("nihongo title");

        // Covered by: BoldTitleTests
        /// <summary>
        /// '''Emboldens''' the first occurrence of the article title, if not already bold
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">The title of the article.</param>
        /// <param name="noChange">Value that indicated whether no change was made.</param>
        /// <returns>The modified article text.</returns>
        public string BoldTitle(string articleText, string articleTitle, out bool noChange)
        {
            HideText Hider2 = new HideText();
            HideText Hider3 = new HideText(true, true, true);
            // clean up bolded self links first
            articleText = BoldedSelfLinks(articleTitle, articleText);

            noChange = true;
            string escTitle = Regex.Escape(articleTitle);
            string escTitleNoBrackets = Regex.Escape(BracketedAtEndOfLine.Replace(articleTitle, ""));

            string articleTextAtStart = articleText;

            string zerothSection = WikiRegexes.ZerothSection.Match(articleText).Value;
            string restOfArticle = articleText.Remove(0, zerothSection.Length);

            // There's a limitation here in that we can't hide image descriptions that may be above lead sentence without hiding the self links we are looking to correct
            string zerothSectionHidden = Hider2.HideMore(zerothSection, false, false, false);
            string zerothSectionHiddenOriginal = zerothSectionHidden;

            // first check for any self links and no bold title, if found just convert first link to bold and return
            Regex r1 = new Regex(@"\[\[\s*" + escTitle + @"\s*\]\]");
            Regex r2 = new Regex(@"\[\[\s*" + Tools.TurnFirstToLower(escTitle) + @"\s*\]\]");
            Regex r3 = new Regex(@"\[\[\s*" + escTitle + @"\s*\|\s*([^\[\]]+?)\s*\]\]");
            Regex r4 = new Regex(@"\[\[\s*" + Tools.TurnFirstToLower(escTitle) + @"\s*\|\s*([^\[\]]+?)\s*\]\]");

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_11#Includes_and_selflinks
            // don't apply if bold in lead section already or some noinclude transclusion business
            if(!WikiRegexes.Noinclude.IsMatch(articleText) && !WikiRegexes.Includeonly.IsMatch(articleText))
            {
                if (!Regex.IsMatch(zerothSection, "'''" + escTitle + "'''"))
                {
                    zerothSectionHidden = r1.Replace(zerothSectionHidden, "'''" + articleTitle + @"'''");
                    zerothSectionHidden = r3.Replace(zerothSectionHidden, "'''$1'''");
                }

                if (zerothSectionHiddenOriginal == zerothSectionHidden && !Regex.IsMatch(zerothSection, @"'''" + Tools.TurnFirstToLower(escTitle) + @"'''"))
                {
                    zerothSectionHidden = r2.Replace(zerothSectionHidden, "'''" + Tools.TurnFirstToLower(articleTitle) + @"'''");
                    zerothSectionHidden = r4.Replace(zerothSectionHidden, "'''$1'''");
                }
            }

            zerothSection = Hider2.AddBackMore(zerothSectionHidden);

            if (zerothSectionHiddenOriginal != zerothSectionHidden)
            {
                noChange = false;
                return (zerothSection + restOfArticle);
            }

            // ignore date articles (date in American or international format), nihongo title
            if (WikiRegexes.Dates2.IsMatch(articleTitle) || WikiRegexes.Dates.IsMatch(articleTitle)
                || NihongoTitle.IsMatch(articleText))
                return articleTextAtStart;

            Regex boldTitleAlready1 = new Regex(@"'''\s*(" + escTitle + "|" + Tools.TurnFirstToLower(escTitle) + @")\s*'''");
            Regex boldTitleAlready2 = new Regex(@"'''\s*(" + escTitleNoBrackets + "|" + Tools.TurnFirstToLower(escTitleNoBrackets) + @")\s*'''");

            string articleTextNoInfobox = Tools.ReplaceWithSpaces(articleText, WikiRegexes.InfoBox.Matches(articleText));

            // if title in bold already exists in article, or page starts with something in bold, don't change anything
            // ignore any bold in infoboxes
            if (boldTitleAlready1.IsMatch(articleTextNoInfobox) || boldTitleAlready2.IsMatch(articleTextNoInfobox)
                || BoldTitleAlready3.IsMatch(articleTextNoInfobox))
                return articleTextAtStart;

            // so no self links to remove, check for the need to add bold
            string articleTextHidden = Hider3.HideMore(articleText);

            // first quick check: ignore articles with some bold in first 5% of hidemore article
            int fivepc = articleTextHidden.Length / 20;

            if (articleTextHidden.Substring(0, fivepc).Contains("'''"))
                return articleTextAtStart;
            
            articleText = Hider3.AddBackMore(articleTextHidden);
            
            zerothSectionHidden = Hider3.HideMore(zerothSection);

            Regex regexBoldNoBrackets = new Regex(@"([^\[]|^)(" + escTitleNoBrackets + "|" + Tools.TurnFirstToLower(escTitleNoBrackets) + ")([ ,.:;])");

            // first try title with brackets removed
            if (regexBoldNoBrackets.IsMatch(zerothSectionHidden))
                zerothSectionHidden = regexBoldNoBrackets.Replace(zerothSectionHidden, "$1'''$2'''$3", 1);

            zerothSection = Hider3.AddBackMore(zerothSectionHidden);
            
            articleText = zerothSection + restOfArticle;

            // check that the bold added is the first bit in bold in the main body of the article
            if (!zerothSectionHiddenOriginal.Equals(zerothSectionHidden) && AddedBoldIsValid(articleText, escTitleNoBrackets))
            {
                noChange = false;
                return articleText;
            }

            return articleTextAtStart;
        }

        private static readonly Regex RegexFirstBold = new Regex(@"^(.*?)'''", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Checks that the bold just added to the article is the first bold in the article, and that it's within the first 5% of the HideMore article OR immediately after the infobox
        /// </summary>
        private bool AddedBoldIsValid(string articleText, string escapedTitle)
        {
            HideText Hider2 = new HideText(true, true, true);
            Regex regexBoldAdded = new Regex(@"^(.*?)'''" + escapedTitle, RegexOptions.Singleline);

            int boldAddedPos = regexBoldAdded.Match(articleText).Length - Regex.Unescape(escapedTitle).Length;

            int firstBoldPos = RegexFirstBold.Match(articleText).Length;

            articleText = WikiRegexes.NestedTemplates.Replace(articleText, "");

            articleText = Hider2.HideMore(articleText);

            // was bold added in first 5% of article?
            bool inFirst5Percent = false;

            if (articleText.Length > 5)
                inFirst5Percent = articleText.Trim().Substring(0, Math.Max(articleText.Length / 20, 5)).Contains("'''");

            // check that the bold added is the first bit in bold in the main body of the article, and in first 5% of HideMore article
            if (inFirst5Percent && boldAddedPos <= firstBoldPos)
                return true;

            return false;
        }

        /// <summary>
        /// Replaces an image in the article.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="oldImage">The old image to replace.</param>
        /// <param name="newImage">The new image.</param>
        /// <param name="noChange">Value that indicated whether no change was made.</param>
        /// <returns>The new article text.</returns>
        public static string ReplaceImage(string oldImage, string newImage, string articleText, out bool noChange)
        {
            string newText = ReplaceImage(oldImage, newImage, articleText);

            noChange = (newText == articleText);

            return newText;
        }

        /// <summary>
        /// Replaces an iamge in the article.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="oldImage">The old image to replace.</param>
        /// <param name="newImage">The new image.</param>
        /// <returns>The new article text.</returns>
        public static string ReplaceImage(string oldImage, string newImage, string articleText)
        {
            articleText = FixImages(articleText);

            //remove image prefix
            oldImage = Tools.WikiDecode(Regex.Replace(oldImage, "^" + Variables.Namespaces[Namespace.File], "", RegexOptions.IgnoreCase));
            newImage = Tools.WikiDecode(Regex.Replace(newImage, "^" + Variables.Namespaces[Namespace.File], "", RegexOptions.IgnoreCase));

            oldImage = Regex.Escape(oldImage).Replace("\\ ", "[ _]");

            oldImage = "((?i:" + WikiRegexes.GenerateNamespaceRegex(Namespace.File, Namespace.Media)
                + @"))\s*:\s*" + Tools.CaseInsensitive(oldImage);
            newImage = "$1:" + newImage;

            return Regex.Replace(articleText, oldImage, newImage);
        }

        /// <summary>
        /// Removes an image from the article.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="image">The image to remove.</param>
        /// <param name="commentOut"></param>
        /// <param name="comment"></param>
        /// <returns>The new article text.</returns>
        public static string RemoveImage(string image, string articleText, bool commentOut, string comment)
        {
            if (image.Length == 0)
                return articleText;

            //remove image prefix
            image = Tools.WikiDecode(Regex.Replace(image, "^"
                                                   + Variables.NamespacesCaseInsensitive[Namespace.File], "", RegexOptions.IgnoreCase));

            // make image name first-letter case insensitive
            image = Tools.CaseInsensitive(HttpUtility.UrlDecode(Regex.Escape(image).Replace("\\ ", "[ _]")));

            articleText = FixImages(articleText);

            // look for standard [[Image:blah...]] links
            Regex r = new Regex(@"\[\[\s*:?\s*(?i:"
                                + WikiRegexes.GenerateNamespaceRegex(Namespace.File, Namespace.Media)
                                + @")\s*:\s*" + image + @"((?>[^\[\]]+|\[\[(?<DEPTH>)|\]\](?<-DEPTH>))*(?(DEPTH)(?!)))\]\]", RegexOptions.Singleline);

            // fall back to Image:blah... syntax used in galleries etc., or just image name (infoboxes etc.)
            if (r.Matches(articleText).Count == 0)
                r = new Regex("(" + Variables.NamespacesCaseInsensitive[Namespace.File] + ")?" + image + @"(?: *\|[^\r\n=]+(?=\s*$))?", RegexOptions.Multiline);

            return r.Replace(articleText, m => (commentOut ? "<!-- " + comment + " " + m.Value + " -->" : ""));
        }

        /// <summary>
        /// Removes an image in the article.
        /// </summary>
        /// <param name="image">The image to remove.</param>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="comment"></param>
        /// <param name="noChange">Value that indicated whether no change was made.</param>
        /// <param name="commentOut"></param>
        /// <returns>The new article text.</returns>
        public static string RemoveImage(string image, string articleText, bool commentOut, string comment, out bool noChange)
        {
            string newText = RemoveImage(image, articleText, commentOut, comment);

            noChange = (newText == articleText);

            return newText;
        }

        /// <summary>
        /// Adds the category to the article.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="newCategory">The new category.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <param name="noChange"></param>
        /// <returns>The article text.</returns>
        public string AddCategory(string newCategory, string articleText, string articleTitle, out bool noChange)
        {
            string newText = AddCategory(newCategory, articleText, articleTitle);

            noChange = (newText == articleText);

            return newText;
        }

        // Covered by: RecategorizerTests.Addition()
        /// <summary>
        /// Adds the category to the article.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="newCategory">The new category.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <returns>The article text.</returns>
        public string AddCategory(string newCategory, string articleText, string articleTitle)
        {
            string oldText = articleText;

            articleText = FixCategories(articleText);

            if (Regex.IsMatch(articleText, @"\[\["
                              + Variables.NamespacesCaseInsensitive[Namespace.Category]
                              + Regex.Escape(newCategory) + @"[\|\]]"))
            {
                return oldText;
            }

            string cat = Tools.Newline("[[" + Variables.Namespaces[Namespace.Category] + newCategory + "]]");
            cat = Tools.ApplyKeyWords(articleTitle, cat);

            if (Namespace.Determine(articleTitle) == Namespace.Template)
                articleText += "<noinclude>" + cat + Tools.Newline("</noinclude>");
            else
                articleText += cat;

            return SortMetaData(articleText, articleTitle, false); //Sort metadata ordering so general fixes don't need to be enabled
        }

        // Covered by: RecategorizerTests.Replacement()
        /// <summary>
        /// Re-categorises the article.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="oldCategory">The old category to replace.</param>
        /// <param name="newCategory">The new category.</param>
        /// <param name="noChange">Value that indicated whether no change was made.</param>
        /// <returns>The re-categorised article text.</returns>
        public static string ReCategoriser(string oldCategory, string newCategory, string articleText, out bool noChange)
        {
            return ReCategoriser(oldCategory, newCategory, articleText, out noChange, false);
        }

        // Covered by: RecategorizerTests.Replacement()
        /// <summary>
        /// Re-categorises the article.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="oldCategory">The old category to replace.</param>
        /// <param name="newCategory">The new category.</param>
        /// <param name="noChange">Value that indicated whether no change was made.</param>
        /// <param name="removeSortKey">If set, any sort key is removed when the category is replaced</param>
        /// <returns>The re-categorised article text.</returns>
        public static string ReCategoriser(string oldCategory, string newCategory, string articleText, out bool noChange, bool removeSortKey)
        {
            //remove category prefix
            oldCategory = Regex.Replace(oldCategory, "^"
                                        + Variables.NamespacesCaseInsensitive[Namespace.Category], "", RegexOptions.IgnoreCase);
            newCategory = Regex.Replace(newCategory, "^"
                                        + Variables.NamespacesCaseInsensitive[Namespace.Category], "", RegexOptions.IgnoreCase);

            //format categories properly
            articleText = FixCategories(articleText);

            string testText = articleText;

            if (Regex.IsMatch(articleText, "\\[\\["
                              + Variables.NamespacesCaseInsensitive[Namespace.Category]
                              + Tools.CaseInsensitive(Regex.Escape(newCategory)) + @"\s*(\||\]\])"))
            {
                bool tmp;
                articleText = RemoveCategory(oldCategory, articleText, out tmp);
            }
            else
            {
                oldCategory = Regex.Escape(oldCategory);
                oldCategory = Tools.CaseInsensitive(oldCategory);

                oldCategory = Variables.Namespaces[Namespace.Category] + oldCategory + @"\s*(\|[^\|\[\]]+\]\]|\]\])";

                // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Replacing_categoring_and_keeping_pipes
                if (!removeSortKey)
                    newCategory = Variables.Namespaces[Namespace.Category] + newCategory + "$1";
                else
                    newCategory = Variables.Namespaces[Namespace.Category] + newCategory + @"]]";

                articleText = Regex.Replace(articleText, oldCategory, newCategory);
            }

            noChange = (testText == articleText);

            return articleText;
        }

        // Covered by: RecategorizerTests.Removal()
        /// <summary>
        /// Removes a category from an article.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="strOldCat">The old category to remove.</param>
        /// <param name="noChange">Value that indicated whether no change was made.</param>
        /// <returns>The article text without the old category.</returns>
        public static string RemoveCategory(string strOldCat, string articleText, out bool noChange)
        {
            articleText = FixCategories(articleText);
            string testText = articleText;

            articleText = RemoveCategory(strOldCat, articleText);

            noChange = (testText == articleText);

            return articleText;
        }

        /// <summary>
        /// Removes a category from an article.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="strOldCat">The old category to remove.</param>
        /// <returns>The article text without the old category.</returns>
        public static string RemoveCategory(string strOldCat, string articleText)
        {
            strOldCat = Tools.CaseInsensitive(Regex.Escape(strOldCat));

            if (!articleText.Contains("<includeonly>"))
                articleText = Regex.Replace(articleText, "\\[\\["
                                            + Variables.NamespacesCaseInsensitive[Namespace.Category] + " ?"
                                            + strOldCat + "( ?\\]\\]| ?\\|[^\\|]*?\\]\\])\r\n", "");

            articleText = Regex.Replace(articleText, "\\[\\["
                                        + Variables.NamespacesCaseInsensitive[Namespace.Category] + " ?"
                                        + strOldCat + "( ?\\]\\]| ?\\|[^\\|]*?\\]\\])", "");

            return articleText;
        }

        /// <summary>
        /// Returns whether the input string matches the name of a category in use in the input article text string, based on a case insensitive match
        /// </summary>
        /// <param name="articleText">the article text</param>
        /// <param name="categoryName">name of the category</param>
        /// <returns></returns>
        public static bool CategoryMatch(string articleText, string categoryName)
        {
            Regex anyCategory = new Regex(@"\[\[\s*" + Variables.NamespacesCaseInsensitive[Namespace.Category] +
                                          @"\s*" + Regex.Escape(categoryName) + @"\s*(?:|\|([^\|\]]*))\s*\]\]", RegexOptions.IgnoreCase);

            return anyCategory.IsMatch(articleText);
        }

        /// <summary>
        /// Changes an article to use defaultsort when all categories use the same sort field / cleans diacritics from defaultsort/categories
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <param name="noChange">If there is no change (True if no Change)</param>
        /// <returns>The article text possibly using defaultsort.</returns>
        public static string ChangeToDefaultSort(string articleText, string articleTitle, out bool noChange)
        {
            return ChangeToDefaultSort(articleText, articleTitle, out noChange, false);
        }

        /// <summary>
        /// Returns the sortkey used by all categories, if
        /// * all categories use the same sortkey
        /// * no {{DEFAULTSORT}} in article
        /// Otherwise returns null
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns></returns>
        public static string GetCategorySort(string articleText)
        {
            if (WikiRegexes.Defaultsort.Matches(articleText).Count == 1)
                return "";

            int matches;
            const string dummy = @"@@@@";

            string sort = GetCategorySort(articleText, dummy, out matches);

            return sort == dummy ? "" : sort;
        }

        /// <summary>
        /// Returns the sortkey used by all categories, if all categories use the same sortkey
        /// Where no sortkey is used for all categories, returns the articletitle
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <param name="matches"></param>
        /// <returns></returns>
        public static string GetCategorySort(string articleText, string articleTitle, out int matches)
        {
            string sort = "";
            bool allsame = true;
            matches = 0;

            articleText = articleText.Replace(@"{{PAGENAME}}", articleTitle);
            articleText = articleText.Replace(@"{{subst:PAGENAME}}", articleTitle);

            foreach (Match m in WikiRegexes.Category.Matches(articleText))
            {
                string explicitKey = m.Groups[2].Value;
                if (explicitKey.Length == 0)
                    explicitKey = articleTitle;

                if (string.IsNullOrEmpty(sort))
                    sort = explicitKey;

                if (sort != explicitKey && !String.IsNullOrEmpty(explicitKey))
                {
                    allsame = false;
                    break;
                }
                matches++;
            }
            if (allsame && matches > 0)
                return sort;
            return "";
        }

        // Covered by: UtilityFunctionTests.ChangeToDefaultSort()
        /// <summary>
        /// Changes an article to use defaultsort when all categories use the same sort field / cleans diacritics from defaultsort/categories
        /// Skips pages using &lt;noinclude&gt;, &lt;includeonly&gt; etc.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <param name="noChange">If there is no change (True if no Change)</param>
        /// <param name="restrictDefaultsortChanges">Prevent insertion of a new {{DEFAULTSORT}} as AWB may not always be right for articles about people</param>
        /// <returns>The article text possibly using defaultsort.</returns>
        public static string ChangeToDefaultSort(string articleText, string articleTitle, out bool noChange, bool restrictDefaultsortChanges)
        {
            string testText = articleText;
            noChange = true;

            if (NoIncludeIncludeOnlyProgrammingElement(articleText))
                return articleText;

            // count categories
            int matches;

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_12#defaultsort_adding_namespace
            if (!Namespace.IsMainSpace(articleTitle))
                articleTitle = Tools.RemoveNamespaceString(articleTitle);

            string sort = GetCategorySort(articleText, articleTitle, out matches);

            MatchCollection ds = WikiRegexes.Defaultsort.Matches(articleText);
            if (ds.Count > 1 || (ds.Count == 1 && !ds[0].Value.ToUpper().Contains("DEFAULTSORT")))
            {
                bool allsame2 = false;
                string lastvalue = "";
                // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Detect_multiple_DEFAULTSORT
                // if all the defaultsorts are the same just remove all but one
                foreach (Match m in WikiRegexes.Defaultsort.Matches(articleText))
                {
                    if (lastvalue.Length == 0)
                    {
                        lastvalue = m.Value;
                        allsame2 = true;
                    }
                    else
                        allsame2 = (m.Value == lastvalue);
                }
                if (allsame2)
                    articleText = WikiRegexes.Defaultsort.Replace(articleText, "", ds.Count - 1);
                else
                    return articleText;
            }

            if (Variables.LangCode.Equals("en"))
                articleText = WikiRegexes.Defaultsort.Replace(articleText, DefaultsortME);

            // match again, after normalisation
            ds = WikiRegexes.Defaultsort.Matches(articleText);

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_9#AWB_didn.27t_fix_special_characters_in_a_pipe
            articleText = FixCategories(articleText);

            if (!restrictDefaultsortChanges)
            {
                bool isArticleAboutAPerson = IsArticleAboutAPerson(articleText, articleTitle, true);
                // AWB's generation of its own sortkey may be incorrect for people, provide option not to insert in this situation
                if (ds.Count == 0)
                {
                    // So that this doesn't get confused by sort keys of "*", " ", etc.
                    // MW bug: DEFAULTSORT doesn't treat leading spaces the same way as categories do
                    // if all existing categories use a suitable sortkey, insert that rather than generating a new one
                    // GetCatSortkey just returns articleTitle if cats don't have sortkey, so don't accept this here
                    if (sort.Length > 4 && matches > 1 && !sort.StartsWith(" "))
                    {
                        // remove keys from categories
                        articleText = WikiRegexes.Category.Replace(articleText, "[["
                                                                   + Variables.Namespaces[Namespace.Category] + "$1]]");

                        // set the defaultsort to the existing unique category sort value
                        // don't add a defaultsort if cat sort was the same as article title, now not case sensitive
                        if (sort != articleTitle && Tools.FixupDefaultSort(sort).ToLower() != articleTitle.ToLower())
                            articleText += Tools.Newline("{{DEFAULTSORT:") + Tools.FixupDefaultSort(sort) + "}}";
                    }

                    // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Add_defaultsort_to_pages_with_special_letters_and_no_defaultsort
                    // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_11#Human_DEFAULTSORT
                    articleText = DefaultsortTitlesWithDiacritics(articleText, articleTitle, matches, isArticleAboutAPerson);
                }
                else if (ds.Count == 1) // already has DEFAULTSORT
                {
                    string s = Tools.FixupDefaultSort(ds[0].Groups[1].Value, isArticleAboutAPerson).Trim();

                    // do not change DEFAULTSORT just for casing
                    if (!s.ToLower().Equals(ds[0].Groups[1].Value.ToLower()) && s.Length > 0 && !restrictDefaultsortChanges)
                        articleText = articleText.Replace(ds[0].Value, "{{DEFAULTSORT:" + s + "}}");

                    // get key value again in case replace above changed it
                    ds = WikiRegexes.Defaultsort.Matches(articleText);
                    string defaultsortKey = ds[0].Groups["key"].Value;

                    //Removes any explicit keys that are case insensitively the same as the default sort (To help tidy up on pages that already have defaultsort)
                    articleText = ExplicitCategorySortkeys(articleText, defaultsortKey);
                }
            }
            noChange = (testText == articleText);
            return articleText;
        }

        /// <summary>
        /// Removes any explicit keys that are case insensitively the same as the default sort OR entirely match the start of the defaultsort (To help tidy up on pages that already have defaultsort)
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="defaultsortKey"></param>
        /// <returns>The article text.</returns>
        private static string ExplicitCategorySortkeys(string articleText, string defaultsortKey)
        {
            foreach (Match m in WikiRegexes.Category.Matches(articleText))
            {
                string explicitKey = m.Groups[2].Value;
                if (explicitKey.Length == 0)
                    continue;

                if (string.Compare(explicitKey, defaultsortKey, StringComparison.OrdinalIgnoreCase) == 0
                    || defaultsortKey.StartsWith(explicitKey) || Tools.NestedTemplateRegex("PAGENAME").IsMatch(explicitKey))
                {
                    articleText = articleText.Replace(m.Value,
                                                      "[[" + Variables.Namespaces[Namespace.Category] + m.Groups[1].Value + "]]");
                }
            }
            return (articleText);
        }

        /// <summary>
        /// if title has diacritics, no defaultsort added yet, adds a defaultsort with cleaned up title as sort key
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <param name="matches">If there is no change (True if no Change)</param>
        /// <param name="articleAboutAPerson">Whether the article is about a person</param>
        /// <returns>The article text possibly using defaultsort.</returns>
        private static string DefaultsortTitlesWithDiacritics(string articleText, string articleTitle, int matches, bool articleAboutAPerson)
        {
            // need some categories and no defaultsort, and a sortkey not the same as the article title
            if (((Tools.FixupDefaultSort(articleTitle) != articleTitle && !articleAboutAPerson) ||
                 (Tools.MakeHumanCatKey(articleTitle, articleText) != articleTitle && articleAboutAPerson))
                && matches > 0 && !WikiRegexes.Defaultsort.IsMatch(articleText))
            {
                // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_11#Human_DEFAULTSORT
                // if article is about a person, attempt to add a surname, forenames sort key rather than the tidied article title
                string sortkey = articleAboutAPerson
                    ? Tools.MakeHumanCatKey(articleTitle, articleText)
                    : Tools.FixupDefaultSort(articleTitle);

                // sorkteys now not case sensitive
                if (!sortkey.ToLower().Equals(articleTitle.ToLower()))
                    articleText += Tools.Newline("{{DEFAULTSORT:") + sortkey + "}}";

                return (ExplicitCategorySortkeys(articleText, sortkey));
            }
            return articleText;
        }

        private static readonly Regex InUniverse = Tools.NestedTemplateRegex(@"In-universe");
        private static readonly Regex CategoryCharacters = new Regex(@"\[\[Category:[^\[\]]*?[Cc]haracters", RegexOptions.Compiled);
        private static readonly Regex SeeAlsoOrMain = Tools.NestedTemplateRegex(new[] { "See also", "Seealso", "Main" });
        private static readonly Regex RefImproveBLP = Tools.NestedTemplateRegex("RefimproveBLP");

        private static readonly Regex IMA = Tools.NestedTemplateRegex(new[]
                                                                      {
                                                                          "Infobox musical artist", "Infobox musical artist 2",
                                                                          "Infobox Musical Artist", "Infobox singer", "Infobox Musician",
                                                                          "Infobox musician", "Music artist",
                                                                          "Infobox Composer", "Infobox composer",
                                                                          "Infobox Musical artist", "Infobox Band"
                                                                      });

        /// <summary>
        /// determines whether the article is about a person by looking for persondata/birth death categories, bio stub etc. for en wiki only
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <returns></returns>
        public static bool IsArticleAboutAPerson(string articleText, string articleTitle)
        {
            return IsArticleAboutAPerson(articleText, articleTitle, false);
        }

        private static readonly Regex BLPUnsourcedSection = Tools.NestedTemplateRegex(new List<string>("BLP unsourced section,BLP sources section".Split(',')));
        private static readonly Regex NotPersonArticles = new Regex(@"(^(((?:First )?(?:Premiership|Presidency|Governor)|Murder|Disappearance|Suicide|Adoption) of|Deaths|[12]\d{3}\b|\d{2,} )|Assembly of|(Birth|Death) rates|(discography|(?:film|bibli)ography| deaths| murders)$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static MetaDataSorter MDS = new MetaDataSorter();
        private static readonly Regex NobleFamilies = new Regex(@"[[Category:[^\[\]\|]*[nN]oble families", RegexOptions.Compiled);
        private static readonly Regex NotAboutAPersonCategories = new Regex(@"\[\[Category:(\d{4} animal|Comedy duos|Articles about multiple people|Married couples|Fictional|Presidencies|Military careers|Parables of|[^\[\]\|\r\n]*musical groups|Internet memes|Military animals)", RegexOptions.Compiled);
        private static readonly Regex CLSAR = Tools.NestedTemplateRegex(@"Infobox Chinese-language singer and actor");
        private static readonly Regex NotPersonInfoboxes = Tools.NestedTemplateRegex(new [] { "Infobox cricketer tour biography", "Infobox political party" } );

        /// <summary>
        /// determines whether the article is about a person by looking for persondata/birth death categories, bio stub etc. for en wiki only
        /// Should only return true if the article is the principle article about the individual (not early life/career/discography etc.)
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <param name="parseTalkPage"></param>
        /// <returns></returns>
        public static bool IsArticleAboutAPerson(string articleText, string articleTitle, bool parseTalkPage)
        {
            if (!Variables.LangCode.Equals("en")
                || Namespace.Determine(articleTitle).Equals(Namespace.Category)
                || NotPersonArticles.IsMatch(articleTitle)
                || ListOf.IsMatch(articleTitle)
                || articleText.Contains(@"[[fictional character")
                || WikiRegexes.Disambigs.IsMatch(articleText)
                || InUniverse.IsMatch(articleText)
                || NotAboutAPersonCategories.IsMatch(articleText)
                || NobleFamilies.IsMatch(articleText)
                || CategoryCharacters.IsMatch(articleText)
                || WikiRegexes.InfoBox.Match(articleText).Groups[1].Value.ToLower().Contains("organization")
                || NotPersonInfoboxes.IsMatch(articleText)
               )
                return false;

            Match m2 = IMA.Match(articleText);

            if (m2.Success)
            {
                string MABackground =
                    Tools.GetTemplateParameterValue(m2.Value,
                                                    "Background", true);

                if (MABackground.Contains("band") || MABackground.Contains("classical_ensemble") || MABackground.Contains("temporary"))
                    return false;
            }

            string CLSA = CLSAR.Match(articleText).Value;
            if (CLSA.Length > 0)
            {
                if (Tools.GetTemplateParameterValue(CLSA, "currentmembers").Length > 0
                    || Tools.GetTemplateParameterValue(CLSA, "pastmembers").Length > 0)
                    return false;
            }

            string zerothSection = WikiRegexes.ZerothSection.Match(articleText).Value;

            // not about a person if it's not the principle article on the subject
            if (SeeAlsoOrMain.IsMatch(zerothSection))
                return false;

            // not about one person if multiple different birth or death date templates
            List<string> BD = new List<string>();
            foreach (Match m in BirthDate.Matches(articleText))
            {
                if (BD.Count > 0 && !BD.Contains(m.Value))
                    return false;

                BD.Add(m.Value);
            }

            List<string> DD = new List<string>();
            foreach (Match m in DeathDate.Matches(articleText))
            {
                if (DD.Count > 0 && !DD.Contains(m.Value))
                    return false;

                DD.Add(m.Value);
            }

            if (WikiRegexes.PeopleInfoboxTemplates.Matches(articleText).Count > 1)
                return false;

            // fix for duplicate living people categories being miscounted as article about multiple people
            string cats = MDS.RemoveCats(ref articleText, articleTitle);
            articleText += cats;

            if (WikiRegexes.DeathsOrLivingCategory.Matches(articleText).Count > 1)
                return false;

            if (WikiRegexes.Persondata.Matches(articleText).Count == 1
                || articleText.Contains(@"-bio-stub}}")
                || articleText.Contains(@"[[Category:Living people")
                || WikiRegexes.PeopleInfoboxTemplates.Matches(zerothSection).Count == 1)
                return true;

            // articles with bold linking to another article may be linking to the main article on the person the article is about
            // e.g. '''military career of [[Napoleon Bonaparte]]'''
            string zerothSectionNoTemplates = WikiRegexes.Template.Replace(zerothSection, "");
            foreach (Match m in WikiRegexes.Bold.Matches(zerothSectionNoTemplates))
            {
                if (WikiRegexes.WikiLink.IsMatch(m.Value))
                    return false;
            }

            int dateBirthAndAgeCount = BirthDate.Matches(zerothSection).Count;
            int dateDeathCount = DeathDate.Matches(zerothSection).Count;

            if (dateBirthAndAgeCount == 1 || dateDeathCount == 1)
                return true;

            if (WikiRegexes.InfoBox.IsMatch(zerothSection) && !WikiRegexes.PeopleInfoboxTemplates.IsMatch(zerothSection))
                return false;

            return WikiRegexes.DeathsOrLivingCategory.IsMatch(articleText)
                || WikiRegexes.LivingPeopleRegex2.IsMatch(articleText)
                || WikiRegexes.BirthsCategory.IsMatch(articleText)
                || WikiRegexes.PeopleFromCategory.IsMatch(articleText)
                || WikiRegexes.BLPSources.IsMatch(BLPUnsourcedSection.Replace(articleText, ""))
                || RefImproveBLP.IsMatch(articleText);
        }

        private static string TryGetArticleText(string title)
        {
            try
            {
                return Variables.MainForm.TheSession.Editor.SynchronousEditor.Clone().Open(title, false);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Adds [[Category:Living people]] to articles with a [[Category:XXXX births]] and no living people/deaths category, taking sortkey from births category if present
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="noChange"></param>
        /// <returns></returns>
        public static string LivingPeople(string articleText, out bool noChange)
        {
            string newText = LivingPeople(articleText);

            noChange = (newText == articleText);

            return newText;
        }

        private static readonly Regex ThreeOrMoreDigits = new Regex(@"\d{3,}", RegexOptions.Compiled);
        private static readonly Regex BirthsSortKey = new Regex(@"\|.*?\]\]", RegexOptions.Compiled);
        /// <summary>
        /// Adds [[Category:Living people]] to articles with a [[Category:XXXX births]] and no living people/deaths category, taking sortkey from births category if present
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The updated article text.</returns>
        public static string LivingPeople(string articleText)
        {
            // don't add living people category if already dead, or thought to be dead
            if (WikiRegexes.DeathsOrLivingCategory.IsMatch(articleText) || WikiRegexes.LivingPeopleRegex2.IsMatch(articleText) ||
                BornDeathRegex.IsMatch(articleText) || DiedDateRegex.IsMatch(articleText))
                return articleText;

            Match m = WikiRegexes.BirthsCategory.Match(articleText);

            // don't add living people category unless 'XXXX births' category is present
            if (!m.Success)
                return articleText;

            string birthCat = m.Value;
            int birthYear = 0;

            string byear = m.Groups[1].Value;

            if (ThreeOrMoreDigits.IsMatch(byear))
                birthYear = int.Parse(byear);

            // per [[:Category:Living people]] and [[WP:BDP]], don't apply if born > 115 years ago
            if (birthYear < (DateTime.Now.Year - 115))
                return articleText;

            // use any sortkey from 'XXXX births' category
            string catKey = birthCat.Contains("|") ? BirthsSortKey.Match(birthCat).Value : "]]";

            return articleText + "[[Category:Living people" + catKey;
        }

        private static readonly Regex PersonYearOfBirth = new Regex(@"(?<='''.{0,100}?)\( *[Bb]orn[^\)\.;]{1,150}?(?<!.*(?:[Dd]ied|&[nm]dash;|—).*)([12]?\d{3}(?: BC)?)\b[^\)]{0,200}", RegexOptions.Compiled);
        private static readonly Regex PersonYearOfDeath = new Regex(@"(?<='''.{0,100}?)\([^\(\)]*?[Dd]ied[^\)\.;]+?([12]?\d{3}(?: BC)?)\b", RegexOptions.Compiled);
        private static readonly Regex PersonYearOfBirthAndDeath = new Regex(@"^.{0,100}?'''\s*\([^\)\r\n]*?(?<![Dd]ied)\b([12]?\d{3})\b[^\)\r\n]*?(-|–|—|&[nm]dash;)[^\)\r\n]*?([12]?\d{3})\b[^\)]{0,200}", RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex UncertainWordings = new Regex(@"(?:\b(about|abt|approx\.?|before|by|or \d+|later|after|near|either|probably|missing|prior to|around|late|[Cc]irca|between|be?tw\.?|[Bb]irth based on age as of date|\d{3,4}(?:\]\])?/(?:\[\[)?\d{1,4}|or +(?:\[\[)?\d{3,})\b|\d{3} *\?|\bca?(?:'')?\.|\b[Cc]a?\b|\b(bef|abt|est)\.|~|/|''fl''\.?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ReignedRuledUnsure = new Regex(@"(?:\?|[Rr](?:uled|eign(?:ed)?\b)|\br\.|(chr|fl(?:\]\])?)\.|\b(?:[Ff]lo(?:urished|ruit)|active|baptized)\b)", RegexOptions.Compiled);

        /// <summary>
        /// Adds [[Category:XXXX births]], [[Category:XXXX deaths]] to articles about people where available, for en-wiki only
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <param name="noChange"></param>
        /// <returns></returns>
        [Obsolete]
        public string FixPeopleCategories(string articleText, string articleTitle, out bool noChange)
        {
            string newText = FixPeopleCategories(articleText, articleTitle);

            noChange = (newText == articleText);

            return newText;
        }

        /// <summary>
        /// Adds [[Category:XXXX births]], [[Category:XXXX deaths]] to articles about people where available, for en-wiki only
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <param name="parseTalkPage"></param>
        /// <param name="noChange"></param>
        /// <returns></returns>
        public string FixPeopleCategories(string articleText, string articleTitle, bool parseTalkPage, out bool noChange)
        {
            string newText = FixPeopleCategories(articleText, articleTitle, parseTalkPage);

            noChange = (newText == articleText);

            return newText;
        }

        private static readonly Regex LongWikilink = new Regex(@"\[\[[^\[\]\|]{11,}(?:\|[^\[\]]+)?\]\]", RegexOptions.Compiled);
        private static readonly Regex YearPossiblyWithBC = new Regex(@"\d{3,4}(?![\ds])(?: BC)?", RegexOptions.Compiled);
        private static readonly Regex ThreeOrFourDigitNumber = new Regex(@"\d{3,4}", RegexOptions.Compiled);
        private static readonly Regex DiedOrBaptised = new Regex(@"(^.*?)((?:&[nm]dash;|—|–|;|[Dd](?:ied|\.)|baptised).*)", RegexOptions.Compiled);
        private static readonly Regex NotCircaTemplate = new Regex(@"{{(?!(?:[Cc]irca|[Ff]l\.?))[^{]*?}}", RegexOptions.Compiled);
        private static readonly Regex AsOfText = new Regex(@"\bas of\b", RegexOptions.Compiled);
        private static readonly Regex FloruitTemplate = Tools.NestedTemplateRegex(new List<string>("fl,fl.".Split(',')));

        /// <summary>
        /// Adds [[Category:XXXX births]], [[Category:XXXX deaths]] to articles about people where available, for en-wiki only
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <returns></returns>
        public static string FixPeopleCategories(string articleText, string articleTitle)
        {
            return FixPeopleCategories(articleText, articleTitle, false);
        }

        /// <summary>
        /// Adds [[Category:XXXX births]], [[Category:XXXX deaths]] to articles about people where available, for en-wiki only
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <param name="parseTalkPage"></param>
        /// <returns></returns>
        public static string FixPeopleCategories(string articleText, string articleTitle, bool parseTalkPage)
        {
            // over 20 references or long and not DOB/DOD categorised at all yet: implausible
            if (!Variables.LangCode.Equals("en") || (articleText.Length > 15000 && !WikiRegexes.BirthsCategory.IsMatch(articleText)
                                                     && !WikiRegexes.DeathsOrLivingCategory.IsMatch(articleText)))
                return YearOfBirthDeathMissingCategory(articleText);

            if (!WikiRegexes.DeathsOrLivingCategory.IsMatch(articleText) && WikiRegexes.Refs.Matches(articleText).Count > 20)
                return YearOfBirthDeathMissingCategory(articleText);

            string articleTextBefore = articleText;
            int catCount = WikiRegexes.Category.Matches(articleText).Count;

            // get the zeroth section (text upto first heading)
            string zerothSection = WikiRegexes.ZerothSection.Match(articleText).Value;

            // remove references and long wikilinks (but allow an ISO date) that may contain false positives of birth/death date
            zerothSection = WikiRegexes.Refs.Replace(zerothSection, " ");
            zerothSection = LongWikilink.Replace(zerothSection, " ");

            // ignore dates from dated maintenance tags etc.
            foreach (Match m2 in WikiRegexes.NestedTemplates.Matches(zerothSection))
            {
                if (Tools.GetTemplateParameterValue(m2.Value, "date").Length > 0)
                    zerothSection = zerothSection.Replace(m2.Value, "");
            }

            foreach (Match m2 in WikiRegexes.TemplateMultiline.Matches(zerothSection))
            {
                if (Tools.GetTemplateParameterValue(m2.Value, "date").Length > 0)
                    zerothSection = zerothSection.Replace(m2.Value, "");
            }

            string yearstring, yearFromInfoBox = "";

            string sort = GetCategorySort(articleText);

            bool alreadyUncertain = false;

            // scrape any infobox for birth year
            string fromInfoBox = GetInfoBoxFieldValue(zerothSection, WikiRegexes.InfoBoxDOBFields);

            // ignore as of dates
            if (AsOfText.IsMatch(fromInfoBox))
                fromInfoBox = fromInfoBox.Substring(0, AsOfText.Match(fromInfoBox).Index);

            if (fromInfoBox.Length > 0 && !UncertainWordings.IsMatch(fromInfoBox))
                yearFromInfoBox = YearPossiblyWithBC.Match(fromInfoBox).Value;

            // birth
            if (!WikiRegexes.BirthsCategory.IsMatch(articleText) && (PersonYearOfBirth.Matches(zerothSection).Count == 1
                                                                     || WikiRegexes.DateBirthAndAge.IsMatch(zerothSection) || WikiRegexes.DeathDateAndAge.IsMatch(zerothSection)
                                                                     || ThreeOrFourDigitNumber.IsMatch(yearFromInfoBox)))
            {
                // look for '{{birth date...' template first
                yearstring = WikiRegexes.DateBirthAndAge.Match(articleText).Groups[1].Value;

                // look for '{{death date and age' template second
                if (String.IsNullOrEmpty(yearstring))
                    yearstring = WikiRegexes.DeathDateAndAge.Match(articleText).Groups[2].Value;

                // thirdly use yearFromInfoBox
                if (ThreeOrFourDigitNumber.IsMatch(yearFromInfoBox))
                    yearstring = yearFromInfoBox;

                // look for '(born xxxx)'
                if (String.IsNullOrEmpty(yearstring))
                {
                    Match m = PersonYearOfBirth.Match(zerothSection);

                    // remove part beyond dash or died
                    string birthpart = DiedOrBaptised.Replace(m.Value, "$1");

                    if (WikiRegexes.CircaTemplate.IsMatch(birthpart))
                        alreadyUncertain = true;

                    birthpart = WikiRegexes.TemplateMultiline.Replace(birthpart, " ");

                    // check born info before any untemplated died info
                    if (!(m.Index > PersonYearOfDeath.Match(zerothSection).Index) || !PersonYearOfDeath.IsMatch(zerothSection))
                    {
                        // when there's only an approximate birth year, add the appropriate cat rather than the xxxx birth one
                        if (UncertainWordings.IsMatch(birthpart) || alreadyUncertain || FloruitTemplate.IsMatch(birthpart))
                        {
                            if (!CategoryMatch(articleText, YearOfBirthMissingLivingPeople) && !CategoryMatch(articleText, YearOfBirthUncertain))
                                articleText += Tools.Newline(@"[[Category:") + YearOfBirthUncertain + CatEnd(sort);
                        }
                        else // after removing dashes, birthpart must still contain year
                            if (!birthpart.Contains(@"?") && Regex.IsMatch(birthpart, @"\d{3,4}"))
                                yearstring = m.Groups[1].Value;
                    }
                }

                // per [[:Category:Living people]], don't apply birth category if born > 121 years ago
                // validate a YYYY date is not in the future
                if (!string.IsNullOrEmpty(yearstring) && yearstring.Length > 2
                    && (!YearOnly.IsMatch(yearstring) || Convert.ToInt32(yearstring) <= DateTime.Now.Year)
                    && !(articleText.Contains(@"[[Category:Living people") && Convert.ToInt32(yearstring) < (DateTime.Now.Year - 121)))
                    articleText += Tools.Newline(@"[[Category:") + yearstring + " births" + CatEnd(sort);
            }

            // scrape any infobox
            yearFromInfoBox = "";
            fromInfoBox = GetInfoBoxFieldValue(articleText, WikiRegexes.InfoBoxDODFields);

            if (fromInfoBox.Length > 0 && !UncertainWordings.IsMatch(fromInfoBox))
                yearFromInfoBox = YearPossiblyWithBC.Match(fromInfoBox).Value;

            if (!WikiRegexes.DeathsOrLivingCategory.IsMatch(RemoveCategory(YearofDeathMissing, articleText)) && (PersonYearOfDeath.IsMatch(zerothSection) || WikiRegexes.DeathDate.IsMatch(zerothSection)
                                                                                                                 || ThreeOrFourDigitNumber.IsMatch(yearFromInfoBox)))
            {
                // look for '{{death date...' template first
                yearstring = WikiRegexes.DeathDate.Match(articleText).Groups[1].Value;

                // secondly use yearFromInfoBox
                if (ThreeOrFourDigitNumber.IsMatch(yearFromInfoBox))
                    yearstring = yearFromInfoBox;

                // look for '(died xxxx)'
                if (string.IsNullOrEmpty(yearstring))
                {
                    Match m = PersonYearOfDeath.Match(zerothSection);

                    // check died info after any untemplated born info
                    if (m.Index >= PersonYearOfBirth.Match(zerothSection).Index || !PersonYearOfBirth.IsMatch(zerothSection))
                    {
                        if (!UncertainWordings.IsMatch(m.Value) && !m.Value.Contains(@"?"))
                            yearstring = m.Groups[1].Value;
                    }
                }

                // validate a YYYY date is not in the future
                if (!string.IsNullOrEmpty(yearstring) && yearstring.Length > 2
                    && (!YearOnly.IsMatch(yearstring) || Convert.ToInt32(yearstring) <= DateTime.Now.Year))
                    articleText += Tools.Newline(@"[[Category:") + yearstring + " deaths" + CatEnd(sort);
            }

            zerothSection = NotCircaTemplate.Replace(zerothSection, " ");
            // birth and death combined
            // if not fully categorised, check it
            if (PersonYearOfBirthAndDeath.IsMatch(zerothSection) && (!WikiRegexes.BirthsCategory.IsMatch(articleText) || !WikiRegexes.DeathsOrLivingCategory.IsMatch(articleText)))
            {
                Match m = PersonYearOfBirthAndDeath.Match(zerothSection);

                string birthyear = m.Groups[1].Value;
                int birthyearint = int.Parse(birthyear);

                string deathyear = m.Groups[3].Value;
                int deathyearint = int.Parse(deathyear);

                // logical valdiation of dates
                if (birthyearint <= deathyearint && (deathyearint - birthyearint) <= 125)
                {
                    string birthpart = zerothSection.Substring(m.Index, m.Groups[2].Index - m.Index),
                    deathpart = zerothSection.Substring(m.Groups[2].Index, (m.Value.Length + m.Index) - m.Groups[2].Index);

                    if (!WikiRegexes.BirthsCategory.IsMatch(articleText))
                    {
                        if (!UncertainWordings.IsMatch(birthpart) && !ReignedRuledUnsure.IsMatch(m.Value) && !Regex.IsMatch(birthpart, @"(?:[Dd](?:ied|\.)|baptised)")
                            && !FloruitTemplate.IsMatch(birthpart))
                            articleText += Tools.Newline(@"[[Category:") + birthyear + @" births" + CatEnd(sort);
                        else
                            if (UncertainWordings.IsMatch(birthpart) && !CategoryMatch(articleText, YearOfBirthMissingLivingPeople) && !CategoryMatch(articleText, YearOfBirthUncertain))
                                articleText += Tools.Newline(@"[[Category:Year of birth uncertain") + CatEnd(sort);
                    }

                    if (!UncertainWordings.IsMatch(deathpart) && !ReignedRuledUnsure.IsMatch(m.Value) && !Regex.IsMatch(deathpart, @"[Bb](?:orn|\.)") && !Regex.IsMatch(birthpart, @"[Dd](?:ied|\.)")
                        && (!WikiRegexes.DeathsOrLivingCategory.IsMatch(articleText) || CategoryMatch(articleText, YearofDeathMissing)))
                        articleText += Tools.Newline(@"[[Category:") + deathyear + @" deaths" + CatEnd(sort);
                }
            }

            // do this check last as IsArticleAboutAPerson can be relatively slow
            if (articleText != articleTextBefore && !IsArticleAboutAPerson(articleTextBefore, articleTitle, parseTalkPage))
                return YearOfBirthDeathMissingCategory(articleTextBefore);

            // {{uncat}} --> {{Cat improve}} if we've added cats
            if (WikiRegexes.Category.Matches(articleText).Count > catCount && WikiRegexes.Uncat.IsMatch(articleText)
               && !WikiRegexes.CatImprove.IsMatch(articleText))
                articleText = Tools.RenameTemplate(articleText, WikiRegexes.Uncat.Match(articleText).Groups[1].Value, "Cat improve");

            return YearOfBirthDeathMissingCategory(articleText);
        }

        private static string CatEnd(string sort)
        {
            return ((sort.Length > 3) ? "|" + sort : "") + "]]";
        }

        private const string YearOfBirthMissingLivingPeople = "Year of birth missing (living people)",
        YearOfBirthMissing = "Year of birth missing",
        YearOfBirthUncertain = "Year of birth uncertain",
        YearofDeathMissing = "Year of death missing";

        private static readonly Regex Cat4YearBirths = new Regex(@"\[\[Category:\d{4} births(?:\s*\|[^\[\]]+)? *\]\]", RegexOptions.Compiled);
        private static readonly Regex Cat4YearDeaths = new Regex(@"\[\[Category:\d{4} deaths(?:\s*\|[^\[\]]+)? *\]\]", RegexOptions.Compiled);

        /// <summary>
        /// Removes birth/death missing categories when xxx births/deaths category also present
        /// </summary>
        /// <param name="articleText"></param>
        /// <returns>The updated article text</returns>
        private static string YearOfBirthDeathMissingCategory(string articleText)
        {
            if (!Variables.LangCode.Equals("en"))
                return articleText;

            // if there is a 'year of birth missing' and a year of birth, remove the 'missing' category
            if (CategoryMatch(articleText, YearOfBirthMissingLivingPeople) && Cat4YearBirths.IsMatch(articleText))
                articleText = RemoveCategory(YearOfBirthMissingLivingPeople, articleText);
            else if (CategoryMatch(articleText, YearOfBirthMissing))
            {
                if (Cat4YearBirths.IsMatch(articleText))
                    articleText = RemoveCategory(YearOfBirthMissing, articleText);
            }

            // if there's a 'year of birth missing' and a 'year of birth uncertain', remove the former
            if (CategoryMatch(articleText, YearOfBirthMissing) && CategoryMatch(articleText, YearOfBirthUncertain))
                articleText = RemoveCategory(YearOfBirthMissing, articleText);

            // if there's a year of death and a 'year of death missing', remove the latter
            if (CategoryMatch(articleText, YearofDeathMissing) && Cat4YearDeaths.IsMatch(articleText))
                articleText = RemoveCategory(YearofDeathMissing, articleText);

            return articleText;
        }

        //private static readonly Regex InfoboxValue = new Regex(@"\s*\|[^{}\|=]+?\s*=\s*.*", RegexOptions.Compiled);

        /// <summary>
        /// Returns the value of the given fields from the page's infobox, where available
        /// Returns a null string if the input article has no infobox, or the input field regex doesn't match on the infobox found
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="fields">List of infobox fields to search</param>
        /// <returns>Field value</returns>
        public static string GetInfoBoxFieldValue(string articleText, List<string> fields)
        {
            string infoBox = WikiRegexes.InfoBox.Match(articleText).Value;

            // clean out references and comments
            infoBox = WikiRegexes.Comments.Replace(infoBox, "");
            infoBox = WikiRegexes.Refs.Replace(infoBox, "");

            List<string> FieldsBack = Tools.GetTemplateParametersValues(infoBox, fields, true);

            foreach (string f in FieldsBack)
            {
                if (f.Length > 0)
                    return f;
            }

            return "";
        }

        /// <summary>
        /// Returns the value of the given field from the page's infobox, where available
        /// Returns a null string if the input article has no infobox, or the input field regex doesn't match on the infobox found
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="field">infobox field to search</param>
        /// <returns>Field value</returns>
        public static string GetInfoBoxFieldValue(string articleText, string field)
        {
            return GetInfoBoxFieldValue(articleText, new List<string>(new[] { field }));
        }

        private static readonly Regex AgeBrackets = new Regex(@"(?:< *[Bb][Rr] */? *> *)?\s*[,;]?\s*\(? *[Aa]ged? +\d{1,3}(?: +or +\d{1,3})? *\)?$", RegexOptions.Compiled);

        /// <summary>
        /// takes input string of date and age e.g. "11 May 1990 (age 21)" and converts to {{birth date and age|1990|5|11}}
        /// </summary>
        /// <param name="dateandage"></param>
        /// <returns></returns>
        public static string FormatToBDA(string dateandage)
        {
            Parsers p = new Parsers();
            string original = dateandage;
            // clean up date format if possible
            dateandage = p.FixDateOrdinalsAndOf(" " + dateandage, "test");

            // remove date wikilinks
            dateandage = WikiRegexes.WikiLinksOnlyPossiblePipe.Replace(dateandage, "$1").Trim();

            // string must end with (age xx)
            if (!AgeBrackets.IsMatch(dateandage))
                return original;

            dateandage = AgeBrackets.Replace(dateandage, "");

            bool AmericanDate = WikiRegexes.AmericanDates.IsMatch(dateandage);

            string ISODate = Tools.ConvertDate(dateandage, DateLocale.ISO);

            if (ISODate.Equals(dateandage) && !WikiRegexes.ISODates.IsMatch(dateandage))
                return original;

            // we have ISO date, convert with {{birth date and age}}, American date, set mf=y
            return @"{{birth date and age|" + (AmericanDate ? "mf=y|" : "df=y|") + ISODate.Replace("-", "|") + @"}}";
        }

        /// <summary>
        /// Replaces legacy/deprecated language codes in interwikis with correct ones
        /// </summary>
        /// <param name="articleText"></param>
        /// <param name="noChange"></param>
        /// <returns></returns>
        public static string InterwikiConversions(string articleText, out bool noChange)
        {
            string newText = InterwikiConversions(articleText);

            noChange = (newText == articleText);

            return newText;
        }

        /// <summary>
        /// Replaces legacy/deprecated language codes in interwikis with correct ones
        /// </summary>
        /// <param name="articleText"></param>
        /// <returns>Page text</returns>
        public static string InterwikiConversions(string articleText)
        {
            //Use proper codes
            //checking first instead of substituting blindly saves some
            //time due to low occurrence rate
            if (articleText.Contains("[[zh-tw:"))
                articleText = articleText.Replace("[[zh-tw:", "[[zh:");
            if (articleText.Contains("[[nb:"))
                articleText = articleText.Replace("[[nb:", "[[no:");
            if (articleText.Contains("[[dk:"))
                articleText = articleText.Replace("[[dk:", "[[da:");
            return articleText;
        }

        /// <summary>
        /// Returns the number of &lt;ref&gt; references in the input text, excluding grouped refs
        /// </summary>
        /// <param name="arcticleText"></param>
        /// <returns></returns>
        private static int TotalRefsNotGrouped(string arcticleText)
        {
            return WikiRegexes.Refs.Matches(arcticleText).Count - WikiRegexes.RefsGrouped.Matches(arcticleText).Count;
        }

        /// <summary>
        /// Converts/subst'd some deprecated templates
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="noChange">Value that indicated whether no change was made.</param>
        /// <returns>The new article text.</returns>
        public static string Conversions(string articleText, out bool noChange)
        {
            string newText = Conversions(articleText);

            noChange = (newText == articleText);

            return newText;
        }

        private static readonly Regex MultipleIssuesUndatedTags = new Regex(@"({{\s*(?:[Aa]rticle|[Mm]ultiple) ?issues\s*(?:\|[^{}]*(?:{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}[^{}]*)?|\|)\s*)(?![Ee]xpert)" + WikiRegexes.MultipleIssuesTemplatesString + @"\s*(\||}})", RegexOptions.Compiled);
        private static readonly Regex MultipleIssuesDateRemoval = new Regex(@"(?<={{\s*(?:[Aa]rticle|[Mm]ultiple) ?issues\s*(?:\|[^{}]*?)?(?:{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}[^{}]*?){0,4}\|[^{}\|]{3,}?)\b(?i)date(?<!.*out of date)", RegexOptions.Compiled);
        private static readonly Regex CiteTemplateDuplicateBars = new Regex(@"(?!{{[Cc]ite ?(?:wikisource|ngall|uscgll|[lL]egislation AU))(\{\{\s*(?:[Cc]it[ae]|(?:[Aa]rticle|[Mm]ultiple) ?issues)[^{}]*)\|\s*(\}\}|\|)", RegexOptions.Compiled);

        /// <summary>
        /// Converts/subst'd some deprecated templates
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The new article text.</returns>
        public static string Conversions(string articleText)
        {
            if (articleText.Contains("{{msg:"))
                articleText = articleText.Replace("{{msg:", "{{");

            foreach (KeyValuePair<Regex, string> k in RegexConversion)
            {
                articleText = k.Key.Replace(articleText, k.Value);
            }

            bool BASEPAGENAMEInRefs = false;
            foreach (Match m in WikiRegexes.Refs.Matches(articleText))
            {
                if (WikiRegexes.BASEPAGENAMETemplates.IsMatch(m.Value))
                {
                    BASEPAGENAMEInRefs = true;
                    break;
                }
            }

            if (!BASEPAGENAMEInRefs)
            {
                foreach (string T in WikiRegexes.BASEPAGENAMETemplatesL)
                    articleText = Tools.RenameTemplate(articleText, T, "subst:" + T);
            }

            // {{no footnotes}} --> {{more footnotes}}, if some <ref>...</ref> or {{sfn}} references in article, uses regex from WikiRegexes.Refs
            // does not change templates with section / reason tags
            if ((TotalRefsNotGrouped(articleText) + Tools.NestedTemplateRegex("sfn").Matches(articleText).Count) > 0)
                articleText = Tools.NestedTemplateRegex("no footnotes").Replace(articleText, m => OnlyArticleBLPTemplateME(m, "more footnotes"));

            // {{foo|section|...}} --> {{foo section|...}} for unreferenced, wikify, refimprove, BLPsources, expand, BLP unsourced
            articleText = SectionTemplates.Replace(articleText, new MatchEvaluator(SectionTemplateConversionsME));

            // {{unreferenced}} --> {{BLP unsourced}} if article has [[Category:Living people]], and no free-text first argument to {{unref}}
            string unref = WikiRegexes.Unreferenced.Match(articleText).Value;
            if (Variables.IsWikipediaEN && WikiRegexes.Unreferenced.Match(articleText).Groups[1].Value.Length > 0 && WikiRegexes.Unreferenced.Matches(articleText).Count == 1 && articleText.Contains(@"[[Category:Living people")
                && (Tools.TurnFirstToLower(Tools.GetTemplateArgument(unref, 1)).StartsWith("date")
                    || Tools.GetTemplateArgumentCount(unref) == 0))
                articleText = Tools.RenameTemplate(articleText, WikiRegexes.Unreferenced.Match(articleText).Groups[1].Value, "BLP unsourced", false);

            // {{unreferenced section}} --> {{BLP unsourced section}} if article has [[Category:Living people]]
            if (Variables.IsWikipediaEN && Tools.NestedTemplateRegex("unreferenced section").IsMatch(articleText) && articleText.Contains(@"[[Category:Living people"))
                articleText = Tools.RenameTemplate(articleText, "unreferenced section", "BLP unsourced section", false);

            articleText = MergePortals(articleText);

            articleText = MergeTemplatesBySection(articleText);

            // tidy up || or |}} (maybe with whitespace between) within templates that don't use null parameters
            while (CiteTemplateDuplicateBars.IsMatch(articleText))
                articleText = CiteTemplateDuplicateBars.Replace(articleText, "$1$2");

            // clean up Template:/underscores in infobox names
            string InfoBox = WikiRegexes.InfoBox.Match(articleText).Groups[1].Value;
            articleText = Tools.RenameTemplate(articleText, InfoBox, CanonicalizeTitle(InfoBox));

            // add date to any undated tags within {{Multiple issues}} (loop due to lookbehind in regex)
            while (MultipleIssuesUndatedTags.IsMatch(articleText))
                articleText = MultipleIssuesUndatedTags.Replace(articleText, "$1$2={{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}$3");

            // clean any 'date' word within {{Multiple issues}} (but not 'update' or 'out of date' fields), place after the date adding rule above (loop due to lookbehind in regex)
            while (MultipleIssuesDateRemoval.IsMatch(articleText))
                articleText = MultipleIssuesDateRemoval.Replace(articleText, "");

            return Dablinks(articleText);
        }

        private static readonly Regex SectionTemplates = Tools.NestedTemplateRegex(new[] { "unreferenced", "wikify", "refimprove", "BLP sources", "expand", "BLP unsourced" });

        /// <summary>
        /// Converts templates such as {{foo|section|...}} to {{foo section|...}}
        /// </summary>
        /// <param name="m">Template call</param>
        /// <returns>The updated emplate call</returns>
        private static string SectionTemplateConversionsME(Match m)
        {
            string newValue = m.Value, existingName = Tools.GetTemplateName(newValue);
            if (Tools.GetTemplateArgument(newValue, 1).Equals("section") || Tools.GetTemplateArgument(newValue, 1).Equals("Section"))
                newValue = Tools.RenameTemplate(Regex.Replace(newValue, @"\|\s*[Ss]ection\s*\|", "|"), existingName + " section");

            // for {{Unreferenced}} auto=yes is deprecated parameter per [[Template:Unreferenced_stub#How_to_use]]
            if (existingName.ToLower().Equals("unreferenced") && Tools.GetTemplateParameterValue(newValue, "auto").ToLower().Equals("yes"))
                newValue = Tools.RemoveTemplateParameter(newValue, "auto");

            return newValue;
        }

        /// <summary>
        /// Renames template if not a section template
        /// </summary>
        /// <param name="m">Template call</param>
        /// /// <param name="newTemplateName">New template name to use</param>
        /// <returns>The updated emplate call</returns>
        private static string NotSectionTemplateME(Match m, string newTemplateName)
        {
            string newValue = m.Value;
            if (Tools.GetTemplateArgument(newValue, 1).Equals("section") || Tools.GetTemplateArgument(newValue, 1).Equals("Section"))
                return m.Value;

            return Tools.RenameTemplate(newValue, newTemplateName);
        }

        /// <summary>
        /// Renames template if the only name arguments are BLP=, date= and article=, or there are no arguments
        /// </summary>
        /// <param name="m"></param>
        /// <param name="newTemplateName"></param>
        /// <returns></returns>
        private static string OnlyArticleBLPTemplateME(Match m, string newTemplateName)
        {
            string newValue = Tools.RemoveTemplateParameter(m.Value, "BLP");
            newValue = Tools.RemoveTemplateParameter(newValue, "article");
            newValue = Tools.RemoveTemplateParameter(newValue, "date");

            if (Tools.GetTemplateArgumentCount(newValue) > 0)
                return m.Value;

            return Tools.RenameTemplate(m.Value, newTemplateName);
        }

        private static readonly Regex TemplateParameter2 = new Regex(@" \{\{\{2\|\}\}\}", RegexOptions.Compiled);

        /// <summary>
        /// Substitutes some user talk templates
        /// </summary>
        /// <param name="talkPageText">The wiki text of the talk page.</param>
        /// <param name="talkPageTitle">The wiki talk page title</param>
        /// <param name="userTalkTemplatesRegex">Dictoinary of regexes matching template calls to substitute</param>
        /// <returns>The updated article text</returns>
        public static string SubstUserTemplates(string talkPageText, string talkPageTitle, Regex userTalkTemplatesRegex)
        {
            if (userTalkTemplatesRegex == null)
                return talkPageText;

            talkPageText = talkPageText.Replace("{{{subst", "REPLACE_THIS_TEXT");
            Dictionary<Regex, string> regexes = new Dictionary<Regex, string> { { userTalkTemplatesRegex, "{{subst:$2}}" } };

            talkPageText = Tools.ExpandTemplate(talkPageText, talkPageTitle, regexes, true);

            talkPageText = TemplateParameter2.Replace(talkPageText, "");
            return talkPageText.Replace("REPLACE_THIS_TEXT", "{{{subst");
        }

        //Covered by TaggerTests
        /// <summary>
        /// If necessary, adds/removes various cleanup tags such as wikify, stub, ibid
        /// </summary>
        public string Tagger(string articleText, string articleTitle, bool restrictOrphanTagging, out bool noChange, ref string summary)
        {
            string newText = Tagger(articleText, articleTitle, restrictOrphanTagging, ref summary);
            newText = TagUpdater(newText);

            noChange = (newText == articleText);

            return newText;
        }

        private static readonly CategoriesOnPageNoHiddenListProvider CategoryProv = new CategoriesOnPageNoHiddenListProvider();

        private readonly List<string> tagsRemoved = new List<string>();
        private readonly List<string> tagsAdded = new List<string>();
        private static readonly Regex ImproveCategories = Tools.NestedTemplateRegex("improve categories");
        private static readonly Regex ProposedDeletionDated = Tools.NestedTemplateRegex("Proposed deletion/dated");
        private static readonly Regex Unreferenced = Tools.NestedTemplateRegex("unreferenced");
        private static readonly Regex Drugbox = Tools.NestedTemplateRegex(new[] { "Drugbox", "Chembox" });

        //TODO:Needs re-write
        /// <summary>
        /// If necessary, adds/removes wikify or stub tag
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">The article title.</param>
        /// <param name="restrictOrphanTagging"></param>
        /// <param name="summary"></param>
        /// <returns>The tagged article.</returns>
        public string Tagger(string articleText, string articleTitle, bool restrictOrphanTagging, ref string summary)
        {
            // don't tag redirects/outside article namespace/no tagging changes
            if (!Namespace.IsMainSpace(articleTitle) || Tools.IsRedirect(articleText) || WikiRegexes.Wi.IsMatch(articleText))
                return articleText;

            tagsRemoved.Clear();
            tagsAdded.Clear();

            string commentsStripped = WikiRegexes.Comments.Replace(articleText, "");
            string commentsCategoriesStripped = WikiRegexes.Category.Replace(commentsStripped, "");
            Sorter.Interwikis(ref commentsStripped);

            // bulleted or indented text should weigh less than simple text.
            // for example, actor stubs may contain large filmographies
            string crapStripped = WikiRegexes.BulletedText.Replace(commentsCategoriesStripped, "");
            int words = (Tools.WordCount(commentsCategoriesStripped) + Tools.WordCount(crapStripped)) / 2;

            // remove stub tags from long articles, don't move section stubs
            if ((words > StubMaxWordCount) && WikiRegexes.Stub.IsMatch(commentsStripped))
            {
                articleText = WikiRegexes.Stub.Replace(articleText, StubChecker).Trim();
                tagsRemoved.Add("stub");
            }

            // refresh
            commentsStripped = WikiRegexes.Comments.Replace(articleText, "");
            commentsCategoriesStripped = WikiRegexes.Category.Replace(commentsStripped, "");

            // on en wiki, remove expand template when a stub template exists
            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests/Archive_5#Remove_.7B.7Bexpand.7D.7D_when_a_stub_template_exists
            if (Variables.LangCode == "en" && WikiRegexes.Stub.IsMatch(commentsCategoriesStripped) &&
                WikiRegexes.Expand.IsMatch(commentsCategoriesStripped))
            {
                articleText = WikiRegexes.Expand.Replace(articleText, "");
                tagsRemoved.Add("expand");
            }

            // refresh
            commentsStripped = WikiRegexes.Comments.Replace(articleText, "");
            commentsCategoriesStripped = WikiRegexes.Category.Replace(commentsStripped, "");

            // do orphan tagging before template analysis for categorisation tags
            articleText = TagOrphans(articleText, articleTitle, restrictOrphanTagging);

            articleText = TagRefsIbid(articleText);

            articleText = TagEmptySection(articleText);

            int totalCategories;
            // ignore commented out wikilinks, and any in {{Proposed deletion/dated}}
            int linkCount = Tools.LinkCount(ProposedDeletionDated.Replace(commentsStripped, ""));

#if DEBUG || UNITTEST
            if (Globals.UnitTestMode)
            {
                totalCategories = Globals.UnitTestIntValue;
            }
            else
#endif
            {
                // stubs add non-hidden stub categories, don't count these in categories count
                // also don't count "Proposed deletion..." cats
                List<Article> Cats = CategoryProv.MakeList(new[] { articleTitle });
                List<Article> CatsNotStubsProd = new List<Article>();

                foreach (Article a in Cats)
                {
                    if (!a.Name.EndsWith(" stubs") && !a.Name.EndsWith(":Stubs") && !a.Name.StartsWith("Proposed deletion"))
                        CatsNotStubsProd.Add(a);
                }
                totalCategories = CatsNotStubsProd.Count;
            }

            if (linkCount > 0 && WikiRegexes.DeadEnd.IsMatch(articleText))
            {
                articleText = WikiRegexes.DeadEnd.Replace(articleText, m => Tools.IsSectionOrReasonTemplate(m.Value, articleText) ? m.Value : m.Groups[1].Value);

                if (!WikiRegexes.DeadEnd.IsMatch(articleText))
                {
                    if (Variables.LangCode.Equals("ar"))
                    {
                        tagsRemoved.Add("نهاية مسدودة");
                    }
                    else
                    {
                        tagsRemoved.Add("deadend");
                    }
                }
            }

            // discount persondata, comments, infoboxes and categories from wikify and stub evaluation
            string lengthtext = commentsCategoriesStripped;
            lengthtext = WikiRegexes.Persondata.Replace(commentsCategoriesStripped, "");
            lengthtext = WikiRegexes.InfoBox.Replace(lengthtext, "");
            lengthtext = Drugbox.Replace(lengthtext, "");

            int length = lengthtext.Length + 1;
            bool underlinked = (linkCount < 0.0025 * length);

            if (length <= 300 && !WikiRegexes.Stub.IsMatch(commentsCategoriesStripped) &&
                !WikiRegexes.Disambigs.IsMatch(commentsCategoriesStripped) && !WikiRegexes.SIAs.IsMatch(commentsCategoriesStripped))
            {
                // add stub tag. Exclude pages their title starts with "List of..."
                if (!ListOf.IsMatch(articleTitle))
                {
                    if (Variables.LangCode.Equals("ar"))
                    {
                        articleText += Tools.Newline("{{بذرة}}", 3);
                        tagsAdded.Add("بذرة");
                    }
                    else
                    {
                        articleText += Tools.Newline("{{stub}}", 3);
                        tagsAdded.Add("stub");
                    }
                    commentsStripped = WikiRegexes.Comments.Replace(articleText, "");
                }
            }

            // rename existing {{improve categories}} else add uncategorized tag
            if (totalCategories == 0 && ImproveCategories.IsMatch(articleText))
                articleText = Tools.RenameTemplate(articleText, "improve categories", "Uncategorized");

            // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Archive_19#AWB_problems
            // nl wiki doesn't use {{Uncategorized}} template
            // prevent wictionary redirects from being tagged as uncategorised
            if (words > 6 && totalCategories == 0
                && !WikiRegexes.Uncat.IsMatch(articleText)
                && Variables.LangCode != "nl"
                // category count is from API; don't add uncat tag if genfixes added person categories
                && !WikiRegexes.DeathsOrLivingCategory.IsMatch(articleText)
                && !WikiRegexes.BirthsCategory.IsMatch(articleText))
            {
                if (WikiRegexes.Stub.IsMatch(commentsStripped))
                {
                    // add uncategorized stub tag
                    if (Variables.LangCode.Equals("ar"))
                    {
                        articleText += Tools.Newline("{{بذرة غير مصنفة|", 2) + WikiRegexes.DateYearMonthParameter + @"}}";
                        tagsAdded.Add("[[تصنيف:مقالات غير مصنفة|غير مصنفة]]");
                    }
                    else
                    {
                        articleText += Tools.Newline("{{Uncategorized stub|", 2) + WikiRegexes.DateYearMonthParameter + @"}}";
                        tagsAdded.Add("[[CAT:UNCATSTUBS|uncategorised]]");
                    }
                }
                else
                {
                    if (Variables.LangCode.Equals("ar"))
                    {
                        articleText += Tools.Newline("{{غير مصنفة|", 2) + WikiRegexes.DateYearMonthParameter + @"}}";
                        tagsAdded.Add("[[CAT:UNCAT|مقالات غير مصنفة]]");
                    }
                    else
                    {
                        articleText += Tools.Newline("{{Uncategorized|", 2) + WikiRegexes.DateYearMonthParameter + @"}}";
                        tagsAdded.Add("[[CAT:UNCAT|uncategorised]]");
                    }
                }
            }

            // remove {{Uncategorized}} if > 0 real categories (stub categories not counted)
            // rename {{Uncategorized}} to {{Uncategorized stub}} if stub with zero categories (stub categories not counted)
            if (WikiRegexes.Uncat.IsMatch(articleText))
            {
                if (totalCategories > 0)
                {
                    articleText = WikiRegexes.Uncat.Replace(articleText, "");
                    tagsRemoved.Add("uncategorised");
                }
                else if (totalCategories == 0 && WikiRegexes.Stub.IsMatch(commentsStripped))
                {
                    string uncatname = WikiRegexes.Uncat.Match(articleText).Groups[1].Value;
                    if (!uncatname.Contains("stub"))
                        articleText = Tools.RenameTemplate(articleText, uncatname, "Uncategorized stub");
                }
            }

            if (linkCount == 0 && Variables.LangCode != "sv" && !WikiRegexes.DeadEnd.IsMatch(articleText) && !WikiRegexes.Centuryinbox.IsMatch(articleText)
                && !Regex.IsMatch(WikiRegexes.MultipleIssues.Match(articleText).Value.ToLower(), @"\bdead ?end\b"))
            {
                // add dead-end tag
                if (Variables.LangCode.Equals("ar"))
                {
                    articleText = "{{نهاية مسدودة|" + WikiRegexes.DateYearMonthParameter + "}}\r\n\r\n" + articleText;
                    tagsAdded.Add("[[:تصنيف:مقالات نهاية مسدودة|نهاية مسدودة]]");
                }
                else
                {
                    articleText = "{{dead end|" + WikiRegexes.DateYearMonthParameter + "}}\r\n\r\n" + articleText;
                    tagsAdded.Add("[[:Category:Dead-end pages|deadend]]");
                }
            }

            if (linkCount < 3 && underlinked && !WikiRegexes.Wikify.IsMatch(articleText)
                && !WikiRegexes.MultipleIssues.Match(articleText).Value.ToLower().Contains("wikify"))
            {
                // add wikify tag
                if (Variables.LangCode.Equals("ar"))
                {
                    articleText = "{{ويكي|" + WikiRegexes.DateYearMonthParameter + "}}\r\n\r\n" + articleText;
                    tagsAdded.Add("[[وب:ويكي|ويكي]]");
                }
                else
                {
                    //articleText = "{{Wikify|reason=It needs more wikilinks. Article has less than 3 wikilinks or the number of wikilinks is smaller than 0.25% of article's size.|" + WikiRegexes.DateYearMonthParameter + "}}\r\n\r\n" + articleText;
                    articleText = "{{Wikify|" + WikiRegexes.DateYearMonthParameter + "}}\r\n\r\n" + articleText;
                    tagsAdded.Add("[[WP:WFY|wikify]]");
                }
            }
            else if (linkCount > 3 && !underlinked &&
                     WikiRegexes.Wikify.IsMatch(articleText))
            {
                // remove wikify, except section templates or wikify tags with reason parameter specified
                articleText = WikiRegexes.Wikify.Replace(articleText, m => Tools.IsSectionOrReasonTemplate(m.Value, articleText) ? m.Value : m.Groups[1].Value);

                if (!WikiRegexes.Wikify.IsMatch(articleText))
                    tagsRemoved.Add("wikify");
            }

            // rename unreferenced --> refimprove if has existing refs, update date
            if (WikiRegexes.Unreferenced.IsMatch(commentsCategoriesStripped)
                && (TotalRefsNotGrouped(commentsCategoriesStripped) + Tools.NestedTemplateRegex("sfn").Matches(articleText).Count) > 0)
            {
                articleText = Unreferenced.Replace(articleText, m2 => Tools.UpdateTemplateParameterValue(Tools.RenameTemplate(m2.Value, "refimprove"), "date", "{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}"));

                Match m = WikiRegexes.MultipleIssues.Match(articleText);
                if (m.Success)
                {
                    string newValue = Tools.RenameTemplateParameter(m.Value, "unreferenced", "refimprove");
                    newValue = Tools.UpdateTemplateParameterValue(newValue, "refimprove", "{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}");
                    if (!newValue.Equals(m.Value))
                        articleText = articleText.Replace(m.Value, newValue);
                }
            }

            if (tagsAdded.Count > 0 || tagsRemoved.Count > 0)
            {
                Parsers p = new Parsers();
                HideText ht = new HideText();

                articleText = ht.HideUnformatted(articleText);

                articleText = p.MultipleIssues(articleText);
                articleText = Conversions(articleText);
                articleText = ht.AddBackUnformatted(articleText);

                // sort again in case tag removal requires whitespace cleanup
                articleText = p.Sorter.Sort(articleText, articleTitle);
            }

            summary = PrepareTaggerEditSummary();

            return articleText;
        }

        private static readonly WhatLinksHereAndPageRedirectsExcludingTheRedirectsListProvider WlhProv = new WhatLinksHereAndPageRedirectsExcludingTheRedirectsListProvider(MinIncomingLinksToBeConsideredAnOrphan);

        private const int MinIncomingLinksToBeConsideredAnOrphan = 3;
        private static readonly Regex Rq = Tools.NestedTemplateRegex("Rq");

        /// <summary>
        /// Tags pages with insufficient incoming page links with the orphan template (localised for ru-wiki).
        /// Removes orphan tag from pages with sufficient incoming page links.
        /// Disambig, SIA pages and soft redirects to Wictionary are never tagged as orphan.
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="articleTitle">Title of the article</param>
        /// <param name="restrictOrphanTagging">Whether to restrict the addition of the orphan tag to pages with zero incoming links only.</param>
        /// <returns>The updated article text</returns>
        private string TagOrphans(string articleText, string articleTitle, bool restrictOrphanTagging)
        {
            // check if not orphaned
            bool orphaned, orphaned2;
            int incomingLinks = 0;
#if DEBUG || UNITTEST
            if (Globals.UnitTestMode)
            {
                orphaned = orphaned2 = Globals.UnitTestBoolValue;
            }
            else
#endif
            {
                try
                {
                    incomingLinks = WlhProv.MakeList(Namespace.Article, articleTitle).Count;
                    orphaned = (incomingLinks < MinIncomingLinksToBeConsideredAnOrphan);
                    orphaned2 = restrictOrphanTagging
                        ? (incomingLinks == 0)
                        : orphaned;
                }

                catch (Exception ex)
                {
                    // don't mark as orphan in case of exception
                    orphaned = orphaned2 = false;
                    ErrorHandler.CurrentPage = articleTitle;
                    ErrorHandler.Handle(ex);
                }
            }

            if (Variables.LangCode == "ru" && incomingLinks == 0 && Rq.Matches(articleText).Count == 1)
            {
                string rqText = Rq.Match(articleText).Value;
                if (!rqText.Contains("linkless"))
                    return articleText.Replace(rqText, rqText.Replace(@"}}", @"|linkless}}"));
            }

            // add orphan tag if applicable, and no disambig nor SIA
            if (orphaned2 && !WikiRegexes.Orphan.IsMatch(articleText) && Tools.GetTemplateParameterValue(WikiRegexes.MultipleIssues.Match(articleText).Value, "orphan").Length == 0
                && !WikiRegexes.Disambigs.IsMatch(articleText) && !WikiRegexes.SIAs.IsMatch(articleText) && !WikiRegexes.Wi.IsMatch(articleText))
            {
                if (Variables.LangCode.Equals("ar"))
                {
                    articleText = "{{يتيمة|" + WikiRegexes.DateYearMonthParameter + "}}\r\n\r\n" + articleText;
                    tagsAdded.Add("[[تصنيف:يتيمة|يتيمة]]");
                }
                else
                {
                    articleText = "{{Orphan|" + WikiRegexes.DateYearMonthParameter + "}}\r\n\r\n" + articleText;
                    tagsAdded.Add("[[CAT:O|orphan]]");
                }
            }
            else if (!orphaned && WikiRegexes.Orphan.IsMatch(articleText))
            {
                articleText = WikiRegexes.Orphan.Replace(articleText, m => m.Groups["MI"].Value);
                if (Variables.LangCode.Equals("ar"))
                {
                    tagsRemoved.Add("يتيمة");
                }
                else
                {
                    tagsRemoved.Add("orphan");
                }
            }
            return articleText;
        }

        private static readonly Regex IbidOpCitRef = new Regex(@"<\s*ref\b[^<>]*>\s*(ibid\.?|op\.?\s*cit\.?|loc\.?\s*cit\.?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// Tags references of 'ibid' with the {{ibid}} cleanup template, en-wiki mainspace only
        /// </summary>
        /// <param name="articleText"></param>
        /// <returns></returns>
        private string TagRefsIbid(string articleText)
        {
            if (Variables.LangCode == "en" && IbidOpCitRef.IsMatch(articleText) && !WikiRegexes.Ibid.IsMatch(articleText))
            {
                tagsAdded.Add("Ibid");
                return @"{{Ibid|" + WikiRegexes.DateYearMonthParameter + @"}}" + articleText;
            }

            return articleText;
        }

        /// <summary>
        /// Tags empty level-2 sections with {{Empty section}}, en-wiki mainspace only
        /// </summary>
        /// <param name="articleText">The article text</param>
        /// <returns>The updated article text</returns>
        private string TagEmptySection(string articleText)
        {
            if (!Variables.LangCode.Equals("en"))
                return articleText;

            string originalarticleText = "";
            int tagsadded = 0;

            while (!originalarticleText.Equals(articleText))
            {
                originalarticleText = articleText;

                int lastpos = -1;
                foreach (Match m in WikiRegexes.HeadingLevelTwo.Matches(Tools.ReplaceWith(articleText, WikiRegexes.UnformattedText, 'x')))
                {
                    // empty setion if only whitespace between two level-2 headings
                    if (lastpos > -1 && articleText.Substring(lastpos, (m.Index - lastpos)).Trim().Length == 0)
                    {
                        articleText = articleText.Insert(m.Index, @"{{Empty section|date={{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}}}" + "\r\n\r\n");
                        tagsadded++;
                        break;
                    }

                    // don't tag single character headings: alpha list where empty section allowed
                    if (m.Groups[1].Length > 1)
                        lastpos = m.Index + m.Length;
                }
            }

            if (tagsadded > 0)
                tagsAdded.Add("Empty section (" + tagsadded + ")");

            return articleText;
        }

        private string PrepareTaggerEditSummary()
        {
            string summary = "";
            if (tagsRemoved.Count > 0)
            {
                // Reverse order of words for arwiki
            	if (Variables.LangCode.Equals("ar"))
                     summary = "وسوم " + Tools.ListToStringCommaSeparator(tagsRemoved) + " أزال";
                else summary = "removed " + Tools.ListToStringCommaSeparator(tagsRemoved) + " tag" +
                    (tagsRemoved.Count == 1 ? "" : "s");
            }

            if (tagsAdded.Count > 0)
            {
                if (!string.IsNullOrEmpty(summary))
                    summary += ", ";

                // Reverse order of words for arwiki
                if (Variables.LangCode.Equals("ar"))
                	 summary += "وسوم " + Tools.ListToStringCommaSeparator(tagsAdded) + " أضاف";
                else summary += "added " + Tools.ListToStringCommaSeparator(tagsAdded) + " tag" +
                    (tagsAdded.Count == 1 ? "" : "s");
            }

            return summary;
        }

        private static readonly HideText ht = new HideText();

        /// <summary>
        /// Sets the date (month & year) for undated cleanup tags that take a date, from https://en.wikipedia.org/wiki/Wikipedia:AWB/Dated_templates
        /// Avoids changing tags in unformatted text areas (wiki comments etc.)
        /// Note: bugzilla 2700 means {{subst:}} within ref tags doesn't work, AWB doesn't do anything about it
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns>The updated article text</returns>
        public static string TagUpdater(string articleText)
        {
            articleText = ht.Hide(articleText);

            foreach (Regex r in WikiRegexes.DatedTemplates)
            {
                articleText = r.Replace(articleText, new MatchEvaluator(TagUpdaterME));
            }

            articleText = FixSyntaxSubstRefTags(articleText);

            return ht.AddBack(articleText);
        }

        private static readonly Regex CurlyBraceEnd = new Regex(@"(?:\| *)?}}$", RegexOptions.Compiled);
        private static readonly Regex MonthYear = new Regex(@"^\s*" + WikiRegexes.MonthsNoGroup + @" +20\d\d\s*$", RegexOptions.Compiled);
        private static readonly Regex DateDash = new Regex(@"(\|\s*[Dd]ate\s*)-", RegexOptions.Compiled);

        /// <summary>
        /// Match evaluator for tag updater
        /// Tags undatd tags, corrects incorrect template parameter names, removes template namespace in template name
        /// </summary>
        private static string TagUpdaterME(Match m)
        {
            string templatecall = m.Value;

            // rename incorrect template parameter names
            if (Variables.LangCode.Equals("en"))
            {
                templatecall = Tools.RenameTemplateParameter(templatecall, "Date", "date");
                templatecall = Tools.RenameTemplateParameter(templatecall, "dates", "date");
                
                // date- or Date- --> date=
                if(Tools.GetTemplateArgument(templatecall, 1).ToLower().Replace(" ", "").StartsWith("date-"))
                    templatecall = DateDash.Replace(templatecall, m2 => m2.Groups[1].Value.ToLower() + "=");
            }

            // remove template namespace in template name
            string TemplateNamespace;
            if (Variables.NamespacesCaseInsensitive.TryGetValue(Namespace.Template, out TemplateNamespace))
            {
                templatecall = Regex.Replace(templatecall, TemplateNamespace, "");
            }

            // check if template already dated (date field, localised for sv-wiki)
            string dateparam = WikiRegexes.DateYearMonthParameter.Substring(0, WikiRegexes.DateYearMonthParameter.IndexOf("="));

            // date tag needed?
            if (Tools.GetTemplateParameterValue(templatecall, dateparam).Length == 0)
            {
                // remove empty 'date='
                templatecall = Tools.RemoveTemplateParameter(templatecall, dateparam);

                // find any dates without date= parameter given, add it
                if (Variables.LangCode.Equals("en") && (Tools.GetTemplateArgumentCount(templatecall) == 1))
                {
                    string firstArg = Tools.GetTemplateArgument(templatecall, 1);

                    if (MonthYear.IsMatch(firstArg))
                        templatecall = templatecall.Insert(templatecall.IndexOf(firstArg), "date=");
                    else if (firstArg.Equals(dateparam))
                    {
                        templatecall = templatecall.Insert(templatecall.IndexOf(firstArg) + firstArg.Length, "=");
                        templatecall = Tools.RemoveTemplateParameter(templatecall, dateparam);
                    }
                }

                if (Tools.GetTemplateParameterValue(templatecall, dateparam).Length == 0)
                    return (CurlyBraceEnd.Replace(templatecall, "|" + WikiRegexes.DateYearMonthParameter + "}}"));
            }
            else
            {
                string dateFieldValue = Tools.GetTemplateParameterValue(templatecall, dateparam);

                // May, 2010 --> May 2010
                if (dateFieldValue.Contains(","))
                {
                    templatecall = Tools.SetTemplateParameterValue(templatecall, dateparam, dateFieldValue.Replace(",", ""));
                    dateFieldValue = Tools.GetTemplateParameterValue(templatecall, dateparam);
                }

                // leading zero removed
                if (dateFieldValue.StartsWith("0"))
                {
                    templatecall = Tools.SetTemplateParameterValue(templatecall, dateparam, dateFieldValue.TrimStart('0'));
                    dateFieldValue = Tools.GetTemplateParameterValue(templatecall, dateparam);
                }

                // full International date?
                if (WikiRegexes.InternationalDates.IsMatch(Regex.Replace(dateFieldValue, @"( [a-z])", u => u.Groups[1].Value.ToUpper())))
                {
                    templatecall = Tools.SetTemplateParameterValue(templatecall, dateparam, dateFieldValue.Substring(dateFieldValue.IndexOf(" ")).Trim());
                    dateFieldValue = Tools.GetTemplateParameterValue(templatecall, dateparam);
                }
                else
                    // ISO date?
                    if (WikiRegexes.ISODates.IsMatch(dateFieldValue))
                    {
                        DateTime dt = Convert.ToDateTime(dateFieldValue, BritishEnglish);
                        dateFieldValue = dt.ToString("MMMM yyyy", BritishEnglish);

                        templatecall = Tools.SetTemplateParameterValue(templatecall, dateparam, dateFieldValue);
                    }

                // date field starts lower case?
                if (!dateFieldValue.Contains(@"CURRENTMONTHNAME") && !dateFieldValue.Equals(Tools.TurnFirstToUpper(dateFieldValue.ToLower())))
                    templatecall = Tools.SetTemplateParameterValue(templatecall, dateparam, Tools.TurnFirstToUpper(dateFieldValue.ToLower()));
            }

            return templatecall;
        }

        private static readonly Regex CommonPunctuation = new Regex(@"[""',\.;:`!\(\)\[\]\?\-–/]", RegexOptions.Compiled);
        /// <summary>
        /// For en-wiki tags redirect pages with one or more of the templates from [[Wikipedia:Template messages/Redirect pages]]
        /// following [[WP:REDCAT]]
        /// </summary>
        /// <param name="articleText">the article text</param>
        /// <param name="articleTitle">the article title</param>
        /// <returns>The updated article text</returns>
        public static string RedirectTagger(string articleText, string articleTitle)
        {
            // only for untagged en-wiki redirects
            if (!Tools.IsRedirect(articleText) || !Variables.IsWikipediaEN || WikiRegexes.Template.IsMatch(articleText))
                return articleText;

            string redirecttarget = Tools.RedirectTarget(articleText);

            // skip self redirects
            if (Tools.TurnFirstToUpperNoProjectCheck(redirecttarget).Equals(Tools.TurnFirstToUpperNoProjectCheck(articleTitle)))
                return articleText;

            // {{R to other namespace}}
            if (!Namespace.IsMainSpace(redirecttarget) && !Tools.NestedTemplateRegex(new[] { "R to other namespace", "R to other namespaces" }).IsMatch(articleText))
                return (articleText + " {{R to other namespace}}");

            // {{R from modification}}
            // difference is extra/removed/changed puntuation
            if (!Tools.NestedTemplateRegex(WikiRegexes.RFromModificationList).IsMatch(articleText)
                && !CommonPunctuation.Replace(redirecttarget, "").Equals(redirecttarget) && CommonPunctuation.Replace(redirecttarget, "").Equals(CommonPunctuation.Replace(articleTitle, "")))
                return (articleText + " " + WikiRegexes.RFromModificationString);

            // {{R from title without diacritics}}

            // title and redirect target the same if dacritics removed from redirect target
            if (redirecttarget != Tools.RemoveDiacritics(redirecttarget) && Tools.RemoveDiacritics(redirecttarget) == articleTitle
                && !Tools.NestedTemplateRegex(WikiRegexes.RFromTitleWithoutDiacriticsList).IsMatch(articleText))
                return (articleText + " " + WikiRegexes.RFromTitleWithoutDiacriticsString);

            // {{R from other capitalisation}}
            if (redirecttarget.ToLower().Equals(articleTitle.ToLower())
                && !Tools.NestedTemplateRegex(WikiRegexes.RFromOtherCapitaliastionList).IsMatch(articleText))
                return (articleText + " " + WikiRegexes.RFromOtherCapitalisationString);

            return articleText;
        }

        private static string StubChecker(Match m)
        {
            // if stub tag is a section stub tag, don't remove from section in article
            return Variables.SectStubRegex.IsMatch(m.Value) ? m.Value : "";
        }

        private static readonly Regex BotsAllow = new Regex(@"{{\s*(?:[Nn]obots|[Bb]ots)\s*\|\s*allow\s*=(.*?)}}", RegexOptions.Singleline | RegexOptions.Compiled);

        // Covered by UtilityFunctionTests.NoBotsTests()
        /// <summary>
        /// checks if a user is allowed to edit this article
        /// using {{bots}} and {{nobots}} tags
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <param name="user">Name of this user</param>
        /// <returns>true if you can edit, false otherwise</returns>
        public static bool CheckNoBots(string articleText, string user)
        {
            Match bot = BotsAllow.Match(articleText);

            if (bot.Success)
            {
                return
                    (Regex.IsMatch(bot.Groups[1].Value,
                                   @"(?:^|,)\s*(?:" + user.Normalize() + @"|awb)\s*(?:,|$)", RegexOptions.IgnoreCase));
            }

            return
                !Regex.IsMatch(articleText,
                               @"\{\{\s*(?:nobots|(nobots|bots)\|(allow=none|deny=(?!none).*(" + user.Normalize() +
                               @"|awb|all)|optout=all))\s*\}\}", RegexOptions.IgnoreCase);
        }

        private static readonly Regex DuplicatePipedLinks = new Regex(@"\[\[([^\]\|]+)\|([^\]]*)\]\](.*[.\n]*)\[\[\1\|\2\]\]", RegexOptions.Compiled);
        private static readonly Regex DuplicateUnpipedLinks = new Regex(@"\[\[([^\]]+)\]\](.*[.\n]*)\[\[\1\]\]", RegexOptions.Compiled);

        /// <summary>
        /// Remove some of the duplicated wikilinks from the article text
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns></returns>
        public static string RemoveDuplicateWikiLinks(string articleText)
        {
            articleText = DuplicatePipedLinks.Replace(articleText, "[[$1|$2]]$3$2");
            return DuplicateUnpipedLinks.Replace(articleText, "[[$1]]$2$1");
        }

        private static readonly Regex ExtToIn = new Regex(@"(?<![*#:;]{2})\[http://([a-z0-9\-]{2})\.(?:(wikt)ionary|wiki(n)ews|wiki(b)ooks|wiki(q)uote|wiki(s)ource|wiki(v)ersity|(w)ikipedia)\.(?:com|net|org)/w(?:iki)?/([^][{|}\s""]*) +([^\n\]]+)\]", RegexOptions.Compiled);
        private static readonly Regex MetaCommonsIncubatorQualityExternalLink = new Regex(@"(?<![*#:;]{2})\[http://(?:(m)eta|(commons)|(incubator)|(quality))\.wikimedia\.(?:com|net|org)/w(?:iki)?/([^][{|}\s""]*) +([^\n\]]+)\]", RegexOptions.Compiled);
        private static readonly Regex WikiaExternalLink = new Regex(@"(?<![*#:;]{2})\[http://([a-z0-9\-]+)\.wikia\.(?:com|net|org)/wiki/([^][{|}\s""]+) +([^\n\]]+)\]", RegexOptions.Compiled);

        // Covered by UtilityFunctionTests.ExternalURLToInternalLink(), incomplete
        /// <summary>
        /// Converts external links to Wikimedia projects into internal links
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns></returns>
        public static string ExternalURLToInternalLink(string articleText)
        {
            // TODO wikitravel support?
            articleText = ExtToIn.Replace(articleText, "[[$2$3$4$5$6$7$8:$1:$9|$10]]");
            articleText = MetaCommonsIncubatorQualityExternalLink.Replace(articleText, "[[$1$2$3$4:$5|$6]]");
            articleText = WikiaExternalLink.Replace(articleText, "[[wikia:$1:$2|$3]]");

            Regex SameLanguageLink = new Regex(@"(\[\[(?:wikt|[nbqsvw]):)" + Variables.LangCode + @":([^\[\]\|]+\|[^\[\]\|]+\]\])");

            return SameLanguageLink.Replace(articleText, "$1$2");
        }

        #endregion

        #region Property checkers
        /// <summary>
        /// Checks if the article has a stub template
        /// </summary>
        public static bool HasStubTemplate(string articleText)
        {
            return WikiRegexes.Stub.IsMatch(articleText);
        }

        /// <summary>
        /// Checks if the article is classible as a 'Stub'
        /// </summary>
        public static bool IsStub(string articleText)
        {
            return (HasStubTemplate(articleText) || articleText.Length < StubMaxWordCount);
        }

        /// <summary>
        /// Checks if the article has an InfoBox (en wiki)
        /// </summary>
        public static bool HasInfobox(string articleText)
        {
            if (!Variables.LangCode.Equals("en"))
                return false;

            return WikiRegexes.InfoBox.IsMatch(WikiRegexes.UnformattedText.Replace(articleText, ""));
        }

        /// <summary>
        /// Check if article has an 'inusetag'
        /// </summary>
        public static bool IsInUse(string articleText)
        {
            return (!Variables.LangCode.Equals("en"))
                ? false
                : WikiRegexes.InUse.IsMatch(WikiRegexes.UnformattedText.Replace(articleText, ""));
        }

        /// <summary>
        /// Check if the article contains a sic template or bracketed wording, indicating the presence of a deliberate typo
        /// </summary>
        public static bool HasSicTag(string articleText)
        {
            return WikiRegexes.SicTag.IsMatch(articleText);
        }

        /// <summary>
        /// Returns whether the input article text contains any {{dead link}} templates, ignoring comments
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns></returns>
        public static bool HasDeadLinks(string articleText)
        {
            return WikiRegexes.DeadLink.IsMatch(WikiRegexes.Comments.Replace(articleText, ""));
        }

        /// <summary>
        /// Check if the article contains a {{no footnotes}} or {{more footnotes}} template but has 5+ &lt;ref>...&lt;/ref> references
        /// </summary>
        public static bool HasMorefootnotesAndManyReferences(string articleText)
        {
            return (WikiRegexes.MoreNoFootnotes.IsMatch(WikiRegexes.Comments.Replace(articleText, "")) && WikiRegexes.Refs.Matches(articleText).Count > 4);
        }

        private static readonly Regex GRTemplateDecimal = new Regex(@"{{GR\|\d}}", RegexOptions.Compiled);

        /// <summary>
        /// Check if the article uses cite references but has no recognised template to display the references; only for en-wiki
        /// </summary>
        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#.28Yet.29_more_reference_related_changes.
        public static bool IsMissingReferencesDisplay(string articleText)
        {
            if (!Variables.LangCode.Equals("en"))
                return false;

            return !WikiRegexes.ReferencesTemplate.IsMatch(articleText) && (TotalRefsNotGrouped(articleText) > 0 | GRTemplateDecimal.IsMatch(articleText));
        }

        /// <summary>
        /// Check if the article contains a &lt;ref>...&lt;/ref> reference after the {{reflist}} to show them
        /// </summary>
        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#.28Yet.29_more_reference_related_changes.
        public static bool HasRefAfterReflist(string articleText)
        {
            articleText = WikiRegexes.Comments.Replace(articleText, "");
            return (WikiRegexes.RefAfterReflist.IsMatch(articleText) &&
                    WikiRegexes.ReferencesTemplate.Matches(articleText).Count == 1);
        }

        /// <summary>
        /// Returns true if the article contains bare external links in the references section (just the URL link on a line with no description/name)
        /// </summary>
        /// <param name="articleText">The wiki text of the article.</param>
        /// <returns></returns>
        // https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Format_references
        public static bool HasBareReferences(string articleText)
        {
            int referencesIndex = WikiRegexes.ReferencesRegex.Match(articleText).Index;

            if (referencesIndex < 2)
                return false;

            int externalLinksIndex =
                WikiRegexes.ExternalLinksHeaderRegex.Match(articleText).Index;

            // get the references section: to external links or end of article, whichever is earlier
            string refsArea = externalLinksIndex > referencesIndex
                ? articleText.Substring(referencesIndex, (externalLinksIndex - referencesIndex))
                : articleText.Substring(referencesIndex);

            return (WikiRegexes.BareExternalLink.IsMatch(refsArea));
        }

        #endregion
    }
}