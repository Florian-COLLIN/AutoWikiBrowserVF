﻿/*

Copyright (C) 2007 Martin Richards, Max Semenik et al.

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
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using WikiFunctions.TalkPages;

namespace WikiFunctions.Parse
{
	/// <summary>
	/// 
	/// </summary>
	public enum InterWikiOrderEnum
	{
		/// <summary>
		/// By order of alphabet, based on local language
		/// </summary>
		LocalLanguageAlpha,

		/// <summary>
		/// By order of alphabet, based on local language (by first word)
		/// </summary>
		LocalLanguageFirstWord,

		/// <summary>
		/// By order of alphabet, based on language code
		/// </summary>
		Alphabetical,

		/// <summary>
		/// English link is first and the rest are sorted alphabetically by language code
		/// </summary>
		AlphabeticalEnFirst
	}

	public class MetaDataSorter
	{
		/// <summary>
		/// 
		/// </summary>
		public List<string> PossibleInterwikis;

		/// <summary>
		/// 
		/// </summary>
		public bool SortInterwikis
		{ get; set; }

		/// <summary>
		/// 
		/// </summary>
		public bool AddCatKey
		{ get; set; }

		public MetaDataSorter()
		{
			SortInterwikis = true;

			if (!LoadInterWikiFromCache())
			{
				LoadInterWikiFromNetwork();
				SaveInterWikiToCache();
			}

			if (InterwikiLocalAlpha == null)
				throw new NullReferenceException("InterwikiLocalAlpha is null");

			//create a comparer
			InterWikiOrder = InterWikiOrderEnum.LocalLanguageAlpha;
		}

		// now will be generated dynamically using Variables.Stub
		private readonly Regex InterLangRegex = new Regex(@"<!--\s*(other languages?|language links?|inter ?(language|wiki)? ?links|inter ?wiki ?language ?links|inter ?wikis?|The below are interlanguage links\.?|interwiki links to this article in other languages, below)\s*-->", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private readonly Regex CatCommentRegex = new Regex("<!-- ?cat(egories)? ?-->", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private List<string> InterwikiLocalAlpha;
		private List<string> InterwikiLocalFirst;
		private List<string> InterwikiAlpha;
		private List<string> InterwikiAlphaEnFirst;
		//List<Regex> InterWikisList = new List<Regex>();

		private InterWikiComparer Comparer;
		private InterWikiOrderEnum Order = InterWikiOrderEnum.LocalLanguageAlpha;

		/// <summary>
		/// 
		/// </summary>
		public InterWikiOrderEnum InterWikiOrder
		{//orders from http://meta.wikimedia.org/wiki/Interwiki_sorting_order
			set
			{
				Order = value;

				List<string> seq;
				switch (Order)
				{
					case InterWikiOrderEnum.Alphabetical:
						seq = InterwikiAlpha;
						break;
					case InterWikiOrderEnum.AlphabeticalEnFirst:
						seq = InterwikiAlphaEnFirst;
						break;
					case InterWikiOrderEnum.LocalLanguageAlpha:
						seq = InterwikiLocalAlpha;
						break;
					case InterWikiOrderEnum.LocalLanguageFirstWord:
						seq = InterwikiLocalFirst;
						break;
					default:
						throw new ArgumentOutOfRangeException("MetaDataSorter.InterWikiOrder",
						                                      (Exception)null);
				}
				PossibleInterwikis = SiteMatrix.GetProjectLanguages(Variables.Project);
				Comparer = new InterWikiComparer(new List<string>(seq), PossibleInterwikis);
			}
			get
			{
				return Order;
			}
		}

		private bool Loaded = true;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="what"></param>
		/// <returns></returns>
		private List<string> Load(string what)
		{
			var result = (List<string>)ObjectCache.Global.Get<List<string>>(Key(what));
			if (result == null)
			{
				Loaded = false;
				return new List<string>();
			}

			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		private void SaveInterWikiToCache()
		{
			ObjectCache.Global.Set(Key("InterwikiLocalAlpha"), InterwikiLocalAlpha);
			ObjectCache.Global.Set(Key("InterwikiLocalFirst"), InterwikiLocalFirst);
			ObjectCache.Global.Set(Key("InterwikiAlpha"), InterwikiAlpha);
			ObjectCache.Global.Set(Key("InterwikiAlphaEnFirst"), InterwikiAlphaEnFirst);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="what"></param>
		/// <returns></returns>
		private static string Key(string what)
		{
			return "MetaDataSorter::" + what;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private bool LoadInterWikiFromCache()
		{
			InterwikiLocalAlpha = Load("InterwikiLocalAlpha");
			InterwikiLocalFirst = Load("InterwikiLocalFirst");
			InterwikiAlpha = Load("InterwikiAlpha");
			InterwikiAlphaEnFirst = Load("InterwikiAlphaEnFirst");

			return Loaded;
		}

		private static readonly CultureInfo EnUsCulture = new CultureInfo("en-US", true);

		/// <summary>
		/// 
		/// </summary>
		private void LoadInterWikiFromNetwork()
		{
			string text = !Globals.UnitTestMode
				? Tools.GetHTML("http://en.wikipedia.org/w/index.php?title=Wikipedia:AutoWikiBrowser/IW&action=raw")
				: @"<!--InterwikiLocalAlphaBegins-->
ru, sq, en
<!--InterwikiLocalAlphaEnds-->
<!--InterwikiLocalFirstBegins-->
en, sq, ru
<!--InterwikiLocalFirstEnds-->";

			string interwikiLocalAlphaRaw =
				RemExtra(Tools.StringBetween(text, "<!--InterwikiLocalAlphaBegins-->", "<!--InterwikiLocalAlphaEnds-->"));
			string interwikiLocalFirstRaw =
				RemExtra(Tools.StringBetween(text, "<!--InterwikiLocalFirstBegins-->", "<!--InterwikiLocalFirstEnds-->"));

			InterwikiLocalAlpha = new List<string>();

			foreach (string s in interwikiLocalAlphaRaw.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
			        )
			{
				InterwikiLocalAlpha.Add(s.Trim().ToLower());
			}

			InterwikiLocalFirst = new List<string>();

			foreach (string s in interwikiLocalFirstRaw.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
			        )
			{
				InterwikiLocalFirst.Add(s.Trim().ToLower());
			}

			InterwikiAlpha = new List<string>(InterwikiLocalFirst);
			InterwikiAlpha.Sort(StringComparer.Create(EnUsCulture, true));

			InterwikiAlphaEnFirst = new List<string>(InterwikiAlpha);
			InterwikiAlphaEnFirst.Remove("en");
			InterwikiAlphaEnFirst.Insert(0, "en");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="input"></param>
		/// <returns>The updated article text</returns>
		private static string RemExtra(string input)
		{
			return input.Replace("\r\n", "").Replace(">", "").Replace("\n", "");
		}
		
		/// <summary>
		/// Sorts article meta data, including optional whitespace fixing
		/// </summary>
		/// <param name="articleText">The wiki text of the article.</param>
		/// <param name="articleTitle">Title of the article</param>
		/// <returns>The updated article text</returns>
		internal string Sort(string articleText, string articleTitle)
		{
			return Sort(articleText, articleTitle, true);
		}

		/// <summary>
		/// Sorts article meta data
		/// </summary>
		/// <param name="articleText">The wiki text of the article.</param>
		/// <param name="articleTitle">Title of the article</param>
		/// <param name="fixOptionalWhitespace">Whether to request optional excess whitespace to be fixed</param>
		/// <returns>The updated article text</returns>
		internal string Sort(string articleText, string articleTitle, bool fixOptionalWhitespace)
		{
			if (Namespace.Determine(articleTitle) == Namespace.Template) // Don't sort on templates
				return articleText;

			string strSave = articleText;
			try
			{
				articleText = Regex.Replace(articleText, "<!-- ?\\[\\[en:.*?\\]\\] ?-->", "");

				string personData = Tools.Newline(RemovePersonData(ref articleText));
				string disambig = Tools.Newline(RemoveDisambig(ref articleText));
				string categories = Tools.Newline(RemoveCats(ref articleText, articleTitle));
				string interwikis = Tools.Newline(Interwikis(ref articleText));

				// Dablinks above orphan tags per [[WP:LAYOUT]]
				if (Variables.LangCode.Equals("en"))
					articleText = MoveMaintenanceTags(articleText);

				articleText = MoveDablinks(articleText);

				if (Variables.LangCode.Equals("en"))
				{
					articleText = MovePortalTemplates(articleText);
					articleText = MoveTemplateToSeeAlsoSection(articleText, WikiRegexes.WikipediaBooks);
					articleText = MoveSisterlinks(articleText);
					articleText = MoveTemplateToReferencesSection(articleText, WikiRegexes.Ibid);
					articleText = MoveExternalLinks(articleText);
					articleText = MoveSeeAlso(articleText);
				}

				// two newlines here per https://en.wikipedia.org/w/index.php?title=Wikipedia_talk:AutoWikiBrowser&oldid=243224092#Blank_lines_before_stubs
				// https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_11#Two_empty_lines_before_stub-templates
				// ru, sl wikis use only one newline
				string strStub = "";
				
				// Category: can use {{Verylargestub}}/{{popstub}} which is not a stub template, don't do stub sorting
				if(!Namespace.Determine(articleTitle).Equals(Namespace.Category))
					strStub = Tools.Newline(RemoveStubs(ref articleText), (Variables.LangCode.Equals("ru") || Variables.LangCode.Equals("sl")) ? 1 : 2);

				//filter out excess white space and remove "----" from end of article
				articleText = Parsers.RemoveWhiteSpace(articleText, fixOptionalWhitespace) + "\r\n";
				articleText += disambig;

				switch (Variables.LangCode)
				{
					case "de":
					case "sl":
						articleText += strStub + categories + personData;

						// https://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser#Removal_of_blank_lines
						// on de wiki a blank line is desired between persondata and interwikis
						if (Variables.LangCode.Equals("de") && personData.Length > 0 && interwikis.Length > 0)
							articleText += "\r\n";
						break;

					case "pl":
					case "ru":
					case "simple":
						articleText += personData + strStub + categories;
						break;
						
					case "it":
						if(Variables.Project == ProjectEnum.wikiquote)
							articleText += personData + strStub + categories;
						else
							articleText += personData + categories + strStub;
						break;
						
					default:
						articleText += personData + categories + strStub;
						break;
				}
				articleText = (articleText + interwikis);
				
				if(Namespace.Determine(articleTitle) == Namespace.Category)
					return articleText.Trim();
				else return articleText.TrimEnd();
			}
			catch (Exception ex)
			{
				if (!ex.Message.Contains("DEFAULTSORT")) ErrorHandler.Handle(ex);
				return strSave;
			}
		}
		
		private static readonly Regex LifeTime = Tools.NestedTemplateRegex("Lifetime");

		/// <summary>
		/// Extracts DEFAULTSORT + categories from the article text; removes duplicate categories, cleans whitespace and underscores
		/// </summary>
		/// <param name="articleText">The wiki text of the article.</param>
		/// <param name="articleTitle">Title of the article</param>
		/// <returns>The cleaned page categories in a single string</returns>
		public string RemoveCats(ref string articleText, string articleTitle)
		{
			// don't pull cat form redirect to a category
			if(WikiRegexes.Category.IsMatch(@"[[" + Tools.RedirectTarget(articleText) + @"]]"))
				return "";
			
			// don't operate on pages with (incorrectly) multiple defaultsorts
			MatchCollection mc = WikiRegexes.Defaultsort.Matches(articleText);
			if (mc.Count > 1)
				return "";
			
			List<string> categoryList = new List<string>();
			string originalArticleText = articleText;

			// allow comments between categories, and keep them in the same place, only grab any comment after the last category if on same line
			Regex r = new Regex(@"<!-- [^<>]*?\[\[\s*" + Variables.NamespacesCaseInsensitive[Namespace.Category]
			                    + @".*?(\]\]|\|.*?\]\]).*?-->|\[\["
			                    + Variables.NamespacesCaseInsensitive[Namespace.Category]
			                    + @".*?(\]\]|\|.*?\]\])(\s*⌊⌊⌊⌊\d{1,4}⌋⌋⌋⌋| *<!--.*?-->|\s*<!--.*?-->(?=\r\n\[\[\s*" + Variables.NamespacesCaseInsensitive[Namespace.Category]
			                    + @"))?", RegexOptions.Singleline);
			
			MatchCollection matches = r.Matches(articleText);
			foreach (Match m in matches)
			{
				if (!Regex.IsMatch(m.Value, @"\[\[Category:(Pages|Categories|Articles) for deletion\]\]"))
					categoryList.Add(m.Value);
			}

			articleText = Tools.RemoveMatches(articleText, matches);
			
			// if category tidying has changed comments/nowikis return with no changes – we've pulled a cat from a comment
			if(!UnformattedTextNotChanged(originalArticleText, articleText))
			{
				articleText = originalArticleText;
				return "";
			}

			if (AddCatKey)
				categoryList = CatKeyer(categoryList, articleTitle);

			if (CatCommentRegex.IsMatch(articleText))
			{
				string catComment = CatCommentRegex.Match(articleText).Value;
				articleText = articleText.Replace(catComment, "");
				categoryList.Insert(0, catComment);
			}

			string defaultSort = "";
			
			if(Variables.LangCode.Equals("sl") && LifeTime.IsMatch(articleText))
			{
				defaultSort = LifeTime.Match(articleText).Value;
			}
			else
			{
				// ignore commented out DEFAULTSORT – http://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs#Moving_DEFAULTSORT_in_HTML_comments
				if (mc.Count > 0 && WikiRegexes.Defaultsort.Matches(WikiRegexes.Comments.Replace(articleText, "")).Count == mc.Count)
					defaultSort = mc[0].Value;
			}

			if (!string.IsNullOrEmpty(defaultSort))
				articleText = articleText.Replace(defaultSort, "");

			if (!string.IsNullOrEmpty(defaultSort) && defaultSort.ToUpper().Contains("DEFAULTSORT"))
				defaultSort = TalkPageFixes.FormatDefaultSort(defaultSort);
			
			if (!string.IsNullOrEmpty(defaultSort)) defaultSort += "\r\n";

			return defaultSort + ListToString(categoryList);
		}
		
		/// <summary>
		/// Returns whether the unformatted text content is the same in the two strings
		/// </summary>
		/// <param name="originalArticleText">the first string to search</param>
		/// <param name="articleText">the second string to search</param>
		/// <returns>whether the unformatted text content is the same in the two strings</returns>
		private static bool UnformattedTextNotChanged(string originalArticleText, string articleText)
		{
			if(WikiRegexes.UnformattedText.Matches(originalArticleText).Count != WikiRegexes.UnformattedText.Matches(articleText).Count)
				return true;
			
			string before = "", after = "";
			foreach(Match m in WikiRegexes.UnformattedText.Matches(originalArticleText))
			{
				before += m.Value;
			}
			
			foreach(Match m in WikiRegexes.UnformattedText.Matches(articleText))
			{
				after += m.Value;
			}
			
			return(before.Length.Equals(after.Length));
		}

		/// <summary>
		/// Extracts the persondata template from the articleText, along with the persondata comment, if present on the line before
		/// </summary>
		/// <param name="articleText">The wiki text of the article.</param>
		/// <returns></returns>
		public static string RemovePersonData(ref string articleText)
		{
			string strPersonData = (Variables.LangCode == "de")
				? Parsers.GetTemplate(articleText, "[Pp]ersonendaten")
				: Parsers.GetTemplate(articleText, "[Pp]ersondata");

			if (!string.IsNullOrEmpty(strPersonData))
			{
				articleText = articleText.Replace(strPersonData, "");
				
				// detection of duplicate persondata template
				if(Variables.LangCode == "en")
				{
					string PersonData2 = Parsers.GetTemplate(articleText, "[Pp]ersondata");
					
					if (!string.IsNullOrEmpty(PersonData2))
					{
						articleText = articleText.Replace(PersonData2, "");
						strPersonData += Tools.Newline(PersonData2);
					}
				}
			}

			// http://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_11#Persondata_comments
			// catch the persondata comment the line before it so that the comment and template aren't separated
			if (articleText.Contains(WikiRegexes.PersonDataCommentEN) && Variables.LangCode == "en")
			{
				articleText = articleText.Replace(WikiRegexes.PersonDataCommentEN, "");
				strPersonData = WikiRegexes.PersonDataCommentEN + strPersonData;
			}

			return strPersonData;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="articleText">The wiki text of the article.</param>
		/// <returns></returns>
		public static string RemoveStubs(ref string articleText)
		{
			// Per http://ru.wikipedia.org/wiki/Википедия:Опросы/Использование_служебных_разделов/Этап_2#.D0.A1.D0.BB.D1.83.D0.B6.D0.B5.D0.B1.D0.BD.D1.8B.D0.B5_.D1.88.D0.B0.D0.B1.D0.BB.D0.BE.D0.BD.D1.8B
			// Russian Wikipedia places stubs before navboxes
			// Category: can use {{Verylargestub}}/{{popstub}} which is not a stub template
			if (Variables.LangCode.Equals("ru") )
				return "";

			List<string> stubList = new List<string>();
			MatchCollection matches = WikiRegexes.PossiblyCommentedStub.Matches(articleText);
			if (matches.Count == 0) return "";

			string x;
			StringBuilder sb = new StringBuilder(articleText);

			for (int i = matches.Count - 1; i >= 0; i--)
			{
				Match m = matches[i];
				x = m.Value;
				if (!Regex.IsMatch(x, Variables.SectStub))
				{
					stubList.Add(x);
					sb.Remove(m.Index, x.Length);
				}
			}
			articleText = sb.ToString();

			stubList.Reverse();
			return (stubList.Count != 0) ? ListToString(stubList) : "";
		}

		/// <summary>
		/// Removes any disambiguation templates from the article text, to be added at bottom later
		/// </summary>
		/// <param name="articleText">The wiki text of the article.</param>
		/// <returns>Article text stripped of disambiguation templates</returns>
		public static string RemoveDisambig(ref string articleText)
		{
			if (Variables.LangCode != "en")
				return "";

			string strDisambig = "";
			
			// don't pull out of comments
			if (WikiRegexes.Disambigs.IsMatch(WikiRegexes.Comments.Replace(articleText, "")))
			{
				strDisambig = WikiRegexes.Disambigs.Match(articleText).Value;
				articleText = articleText.Replace(strDisambig, "");
			}

			return strDisambig;
		}

		/// <summary>
		/// Moves any disambiguation links in the zeroth section to the top of the article (en only)
		/// </summary>
		/// <param name="articleText">The wiki text of the article.</param>
		/// <returns>Article text with disambiguation links at top</returns>
		public static string MoveDablinks(string articleText)
		{
			string originalArticletext = articleText;
			
			// get the zeroth section (text upto first heading)
			string zerothSection = WikiRegexes.ZerothSection.Match(articleText).Value;

			// avoid moving commented out Dablinks
			if (Variables.LangCode != "en" || !WikiRegexes.Dablinks.IsMatch(WikiRegexes.Comments.Replace(zerothSection, "")))
				return articleText;

			// get the rest of the article including first heading (may be null if article has no headings)
			string restOfArticle = articleText.Replace(zerothSection, "");

			string strDablinks = "";

			foreach (Match m in WikiRegexes.Dablinks.Matches(zerothSection))
			{
				strDablinks += m.Value + "\r\n";
				
				// remove colons before dablink
				zerothSection = zerothSection.Replace(":" + m.Value + "\r\n", "");
				
				// additionally, remove whitespace after dablink
				zerothSection = Regex.Replace(zerothSection, Regex.Escape(m.Value) + @" *(?:\r\n)?", "");
			}
			
			articleText = strDablinks + zerothSection + restOfArticle;
			
			// avoid moving commented out Dablinks, round 2
			if(UnformattedTextNotChanged(originalArticletext, articleText))
				return articleText;
			
			return originalArticletext;
		}
		
		private static readonly Regex ExternalLinksSection = new Regex(@"(^== *[Ee]xternal +[Ll]inks? *==.*?)(?=^==+[^=][^\r\n]*?[^=]==+(\r\n?|\n)$)", RegexOptions.Multiline | RegexOptions.Singleline);
		private static readonly Regex ExternalLinksToEnd = new Regex(@"(\s*(==+)\s*[Ee]xternal +links\s*\2 *).*", RegexOptions.IgnoreCase | RegexOptions.Singleline);

		/// <summary>
		/// Moves sisterlinks such as {{wiktionary}} to the external links section
		/// </summary>
		/// <param name="articleText">The article text</param>
		/// <returns>The updated article text</returns>
		public static string MoveSisterlinks(string articleText)
		{
			string originalArticletext = articleText;
			// need to have an 'external links' section to move the sisterlinks to
			if (WikiRegexes.SisterLinks.Matches(articleText).Count >= 1 && WikiRegexes.ExternalLinksHeaderRegex.Matches(articleText).Count == 1)
			{
				foreach (Match m in WikiRegexes.SisterLinks.Matches(articleText))
				{
					string sisterlinkFound = m.Value;
					string ExternalLinksSectionString = ExternalLinksSection.Match(articleText).Value;

					// if ExteralLinksSection didn't match then 'external links' must be last section
					if (ExternalLinksSectionString.Length == 0)
						ExternalLinksSectionString = ExternalLinksToEnd.Match(articleText).Value;

					// check sisterlink NOT currently in 'external links'
					if (!ExternalLinksSectionString.Contains(sisterlinkFound.Trim()))
					{
						articleText = Regex.Replace(articleText, Regex.Escape(sisterlinkFound) + @"\s*(?:\r\n)?", "");
						articleText = WikiRegexes.ExternalLinksHeaderRegex.Replace(articleText, "$0" + "\r\n" + sisterlinkFound);
					}
				}
			}
			
			if(UnformattedTextNotChanged(originalArticletext, articleText))
				return articleText;

			return originalArticletext;
		}

		/// <summary>
		/// Moves maintenance tags to the top of the article text.
		/// Does not move tags when only non-infobox templates are above the last tag
		/// </summary>
		/// <param name="articleText">the article text</param>
		/// <returns>the modified article text</returns>
		public static string MoveMaintenanceTags(string articleText)
		{
			string originalArticleText = articleText;
			bool doMove = false;
			int lastIndex = -1;
			
			// don't pull tags from new-style {{multiple issues}} template
			string articleTextNoMI = Tools.ReplaceWithSpaces(articleText, WikiRegexes.MultipleIssues.Matches(articleText));
			
			// if all templates removed from articletext before last MaintenanceTemplates match are not infoboxes then do not change anything
			foreach(Match m in WikiRegexes.MaintenanceTemplates.Matches(articleTextNoMI))
			{
				lastIndex = m.Index;
			}

			// return if no MaintenanceTemplates to move
			if (lastIndex < 0)
				return articleText;

			string articleTextToCheck = articleText.Substring(0, lastIndex);

			foreach(Match m in WikiRegexes.NestedTemplates.Matches(articleTextToCheck))
			{
				if (Tools.GetTemplateName(m.Value).ToLower().Contains("infobox"))
				{
					doMove = true;
					break;
				}

				articleTextToCheck = articleTextToCheck.Replace(m.Value, "");
			}

			if(articleTextToCheck.Trim().Length > 0)
				doMove = true;

			if(!doMove)
				return articleText;

			string strMaintTags = "";

			foreach (Match m in WikiRegexes.MaintenanceTemplates.Matches(articleText))
			{
				if(!m.Value.Contains("section"))
				{
					strMaintTags = strMaintTags + m.Value + "\r\n";
					articleText = articleText.Replace(m.Value, "");
				}
			}

			articleText = strMaintTags + articleText;
			
			if(!UnformattedTextNotChanged(originalArticleText, articleText))
				return originalArticleText;

			return strMaintTags.Length > 0 ? articleText.Replace(strMaintTags + "\r\n", strMaintTags) : articleText;
		}

		private static readonly Regex SeeAlsoSection = new Regex(@"(^== *[Ss]ee also *==.*?)(?=^==[^=][^\r\n]*?[^=]==(\r\n?|\n)$)", RegexOptions.Multiline | RegexOptions.Singleline);
		private static readonly Regex SeeAlsoToEnd = new Regex(@"(\s*(==+)\s*see\s+also\s*\2 *).*", RegexOptions.IgnoreCase | RegexOptions.Singleline);

		/// <summary>
		/// Moves template calls to the see also section of the article
		/// </summary>
		/// <param name="articleText">The article text</param>
		/// <param name="TemplateToMove">The template calls to move</param>
		/// <returns>The updated article text</returns>
		public static string MoveTemplateToSeeAlsoSection(string articleText, Regex TemplateToMove)
		{
			// need to have a 'see also' section to move the template to
			if(WikiRegexes.SeeAlso.Matches(articleText).Count != 1 || TemplateToMove.Matches(articleText).Count < 1)
				return articleText;
			
			string originalArticletext = articleText;

			foreach (Match m in TemplateToMove.Matches(articleText))
			{
				string TemplateFound = m.Value;
				string seeAlsoSectionString = SeeAlsoSection.Match(articleText).Value;
				int seeAlsoIndex = SeeAlsoSection.Match(articleText).Index;

				// if SeeAlsoSection didn't match then 'see also' must be last section
				if (seeAlsoSectionString.Length == 0)
				{
					seeAlsoSectionString = SeeAlsoToEnd.Match(articleText).Value;
					seeAlsoIndex = SeeAlsoToEnd.Match(articleText).Index;
				}

				// only move templates NOT currently in 'see also'
				if (m.Index < seeAlsoIndex || m.Index > (seeAlsoIndex + seeAlsoSectionString.Length))
				{
					// remove template, also remove newline after template if template on its own line
					articleText = Regex.Replace(articleText, @"^" + Regex.Escape(TemplateFound) + @" *(?:\r\n)?", "", RegexOptions.Multiline);
					if(articleText.Contains(TemplateFound))
						articleText = Regex.Replace(articleText, Regex.Escape(TemplateFound), "", RegexOptions.Multiline);
					
					articleText = WikiRegexes.SeeAlso.Replace(articleText, "$0" + Tools.Newline(TemplateFound));
				}
			}

			if(UnformattedTextNotChanged(originalArticletext, articleText))
				return articleText;

			return originalArticletext;
		}
		
		/// <summary>
		/// Moves any {{XX portal}} templates to the 'see also' section, if present (en only), per Template:Portal
		/// </summary>
		/// <param name="articleText">The wiki text of the article.</param>
		/// <returns>Article text with {{XX portal}} template correctly placed</returns>
		// http://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Placement_of_portal_template
		public static string MovePortalTemplates(string articleText)
		{
			return MoveTemplateToSeeAlsoSection(articleText, WikiRegexes.PortalTemplate);
		}

		private static readonly Regex ReferencesSectionRegex = new Regex(@"^== *[Rr]eferences *==\s*", RegexOptions.Multiline);
		private static readonly Regex NotesSectionRegex = new Regex(@"^== *[Nn]otes(?: and references)? *==\s*", RegexOptions.Multiline);
		private static readonly Regex FootnotesSectionRegex = new Regex(@"^== *(?:[Ff]ootnotes|Sources) *==\s*", RegexOptions.Multiline);

		/// <summary>
		/// Moves given template to the references section from the zeroth section, if present (en only)
		/// </summary>
		/// <param name="articleText">The wiki text of the article.</param>
		/// <param name="TemplateRegex">A Regex to match the template to move</param>
		/// <param name="onlyfromzerothsection">Whether to check only the zeroth section of the article for the template</param>
		/// <returns>Article text with template correctly placed</returns>
		public static string MoveTemplateToReferencesSection(string articleText, Regex TemplateRegex, bool onlyfromzerothsection)
		{
			// no support for more than one of these templates in the article
			if(TemplateRegex.Matches(articleText).Count != 1)
				return articleText;
			
			if(onlyfromzerothsection)
			{
				string zerothSection = WikiRegexes.ZerothSection.Match(articleText).Value;
				if (TemplateRegex.Matches(zerothSection).Count != 1)
					return articleText;
			}

			// find the template position
			int templatePosition = TemplateRegex.Match(articleText).Index;

			// the template must be in one of the 'References', 'Notes' or 'Footnotes' section
			int notesSectionPosition = NotesSectionRegex.Match(articleText).Index;

			if (notesSectionPosition > 0 && templatePosition < notesSectionPosition)
				return MoveTemplateToSection(articleText, TemplateRegex, 2);
			
			int referencesSectionPosition = ReferencesSectionRegex.Match(articleText).Index;

			if (referencesSectionPosition > 0 && templatePosition < referencesSectionPosition)
				return MoveTemplateToSection(articleText, TemplateRegex, 1);

			int footnotesSectionPosition = FootnotesSectionRegex.Match(articleText).Index;

			if (footnotesSectionPosition > 0 && templatePosition < footnotesSectionPosition)
				return MoveTemplateToSection(articleText, TemplateRegex, 3);

			return articleText;
		}
		
		public static string MoveTemplateToReferencesSection(string articleText, Regex templateRegex)
		{
			return MoveTemplateToReferencesSection(articleText, templateRegex, false);
		}

		private static string MoveTemplateToSection(string articleText, Regex templateRegex, int section)
		{
			// extract the template
			string extractedTemplate = templateRegex.Match(articleText).Value;
			articleText = articleText.Replace(extractedTemplate, "");

			switch (section)
			{
				case 1:
					return ReferencesSectionRegex.Replace(articleText, "$0" + extractedTemplate + "\r\n", 1);
				case 2:
					return NotesSectionRegex.Replace(articleText, "$0" + extractedTemplate + "\r\n", 1);
				case 3:
					return FootnotesSectionRegex.Replace(articleText, "$0" + extractedTemplate + "\r\n", 1);
				default:
					return articleText;
			}
		}

		
		private static readonly Regex ReferencesSection = new Regex(@"(^== *[Rr]eferences *==.*?)(?=^==[^=][^\r\n]*?[^=]==(\r\n?|\n)$)", RegexOptions.Multiline | RegexOptions.Singleline);
		private static readonly Regex ReferencesToEnd = new Regex(@"^== *[Rr]eferences *==\s*" + WikiRegexes.ReferencesTemplates + @"\s*(?={{DEFAULTSORT\:|\[\[Category\:)", RegexOptions.Multiline);

		// http://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Feature_requests#Place_.22External_links.22_section_after_.22References.22
		// TODO: only works when there is another section following the references section
		/// <summary>
		/// Ensures the external links section of an article is after the references section
		/// </summary>
		/// <param name="articleText">The wiki text of the article.</param>
		/// <returns>Article text with external links section below the references section</returns>
		public static string MoveExternalLinks(string articleText)
		{
			string articleTextAtStart = articleText;
			// is external links section above references?
			string externalLinks = ExternalLinksSection.Match(articleText).Groups[1].Value;
			string references = ReferencesSection.Match(articleText).Groups[1].Value;

			// references may be last section
			if (references.Length == 0)
				references = ReferencesToEnd.Match(articleText).Value;

			if (articleText.IndexOf(externalLinks) < articleText.IndexOf(references) && references.Length > 0 && externalLinks.Length > 0)
			{
				articleText = articleText.Replace(externalLinks, "");
				articleText = articleText.Replace(references, references + externalLinks);
			}
			
			// newlines are fixed by later logic; validate no <ref> in external links section
			return !Parsers.HasRefAfterReflist(articleText) ? articleText : articleTextAtStart;
		}

		/// <summary>
		/// Moves the 'see also' section to be above the 'references' section, subject to the limitation that the 'see also' section can't be the last level-2 section.
		/// Does not move section when two or more references sections in the same article
		/// </summary>
		/// <param name="articleText">The wiki text of the article.</param>
		/// <returns></returns>
		public static string MoveSeeAlso(string articleText)
		{
			// is 'see also' section below references?
			string references = ReferencesSection.Match(articleText).Groups[1].Value;
			string seealso = SeeAlsoSection.Match(articleText).Groups[1].Value;

			if (articleText.IndexOf(seealso) > articleText.IndexOf(references) && ReferencesSection.Matches(articleText).Count == 1 && seealso.Length > 0)
			{
				articleText = articleText.Replace(seealso, "");
				articleText = articleText.Replace(references, seealso + references);
			}
			// newlines are fixed by later logic
			return articleText;
		}

		/// <summary>
		/// Gets a list of Link FA/GA's from the article
		/// </summary>
		/// <param name="articleText">The wiki text of the article.</param>
		/// <returns>The List of {{Link [FG]A}}'s from the article</returns>
		private static List<string> RemoveLinkFGAs(ref string articleText)
		{
			List<string> linkFGAList = new List<string>();

			MatchCollection matches = WikiRegexes.LinkFGAs.Matches(Tools.ReplaceWithSpaces(articleText, WikiRegexes.UnformattedText.Matches(articleText)));

			if (matches.Count == 0)
				return linkFGAList;

			foreach (Match m in matches)
			{
				string FGAlink = m.Value;
				linkFGAList.Add(FGAlink);
				articleText = articleText.Replace(FGAlink, "");
			}
			
			return linkFGAList;
		}

		/// <summary>
		/// Extracts all of the interwiki featured article and interwiki links from the article text
		/// Ignores interwikis in comments/nowiki tags
		/// </summary>
		/// <param name="articleText">Article text with interwiki and interwiki featured article links removed</param>
		/// <returns>string of interwiki featured article and interwiki links</returns>
		public string Interwikis(ref string articleText)
		{
			string interWikiComment = "";
			if (InterLangRegex.IsMatch(articleText))
			{
				interWikiComment = InterLangRegex.Match(articleText).Value;
				articleText = articleText.Replace(interWikiComment, "");
			}

			string interWikis = ListToString(RemoveLinkFGAs(ref articleText));
			
			if(interWikiComment.Length > 0)
				interWikis += interWikiComment + "\r\n";
			
			interWikis += ListToString(RemoveInterWikis(ref articleText));
			
			return interWikis;
		}

		/// <summary>
		/// Extracts all of the interwiki links from the article text, handles comments beside interwiki links
		/// </summary>
		/// <param name="articleText">Article text with interwikis removed</param>
		/// <returns>List of interwikis</returns>
		private List<string> RemoveInterWikis(ref string articleText)
		{
			List<string> interWikiList = new List<string>();
			MatchCollection matches = WikiRegexes.PossibleInterwikis.Matches(articleText);
			if (matches.Count == 0)
				return interWikiList;
			
			// get all unformatted text in article to avoid taking interwikis from comments etc.
			string unformattedText = "";
			StringBuilder ut = new StringBuilder();
			foreach(Match u in WikiRegexes.UnformattedText.Matches(articleText))
				ut.Append(u.Value);
			
			unformattedText = ut.ToString();

			List<Match> goodMatches = new List<Match>();

			foreach (Match m in matches)
			{
				string site = m.Groups[1].Value.Trim().ToLower();
				
				if (!PossibleInterwikis.Contains(site))
					continue;
				
				if(unformattedText.Contains(m.Value))
				{
					//unformattedText = unformattedText.Replace(m.Value, "");
					Tools.ReplaceOnce(ref unformattedText, m.Value, "");
					continue;
				}
				
				goodMatches.Add(m);
				
				// drop interwikis to own wiki, but not on commons where language = en and en interwikis go to wikipedia
				if(!(m.Groups[1].Value.Equals(Variables.LangCode) && !Variables.IsWikimediaMonolingualProject))
					interWikiList.Add("[[" + site + ":" + m.Groups[2].Value.Trim() + "]]" + m.Groups[3].Value);
			}

			articleText = Tools.RemoveMatches(articleText, goodMatches);

			if (SortInterwikis)
			{
				// sort twice to result in no reordering of two interwikis to same language project
				interWikiList.Sort(Comparer);
				interWikiList.Sort(Comparer);
			}

			return interWikiList;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="match"></param>
		/// <returns></returns>
		public static string IWMatchEval(Match match)
		{
			string[] textArray = new[] { "[[", match.Groups["site"].ToString().ToLower(), ":", match.Groups["text"].ToString(), "]]" };
			return string.Concat(textArray);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="items"></param>
		/// <returns></returns>
		private static string ListToString(ICollection<string> items)
		{//remove duplicates, and return List as string.

			if (items.Count == 0)
				return "";

			List<string> uniqueItems = new List<string>();

			//remove duplicates
			foreach (string s in items)
			{
				if (!uniqueItems.Contains(s))
					uniqueItems.Add(s);
			}

			StringBuilder list = new StringBuilder();
			//add to string
			foreach (string s in uniqueItems)
			{
				list.AppendLine(s);
			}

			return list.ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="list"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		private static List<string> CatKeyer(IEnumerable<string> list, string name)
		{
			name = Tools.MakeHumanCatKey(name, ""); // make key

			//add key to cats that need it
			List<string> newCats = new List<string>();
			foreach (string s in list)
			{
				string z = s;
				if (!z.Contains("|"))
					z = z.Replace("]]", "|" + name + "]]");

				newCats.Add(z);
			}
			return newCats;
		}
	}
}
