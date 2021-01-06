﻿/*
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
using System.Web;
using System.Collections;
using System.IO;
using System.Windows.Forms;

namespace WikiFunctions
{
    /// <summary>
    /// This class renders MediaWiki-like HTML diffs
    /// </summary>
    public class WikiDiff
    {
        private string[] LeftLines;
        private string[] RightLines;
        private Diff diff;
        private StringBuilder Result;
        private int ContextLines;

        /// <summary>
        /// Renders diff
        /// </summary>
        /// <param name="leftText">Earlier version of the text</param>
        /// <param name="rightText">Later version of the text</param>
        /// <param name="contextLines">Number of unchanged lines to show around changed ones</param>
        /// <returns>HTML diff</returns>
        public string GetDiff(string leftText, string rightText, int contextLines)
        {
            Result = new StringBuilder(500000);
            LeftLines = leftText.Split(new[] {"\r\n"}, StringSplitOptions.None);
            RightLines = rightText.Split(new[] {"\r\n"}, StringSplitOptions.None);
            ContextLines = contextLines;

            diff = new Diff(LeftLines, RightLines, true, true);
            foreach (Diff.Hunk h in diff)
            {
                if (h.Same)
                {
                    RenderContext(h);
                }
                else
                {
                    RenderDifference(h);
                }
            }
            return Result.ToString();
        }

        #region High-level diff stuff

        private void RenderDifference(Diff.Hunk hunk)
        {
            Range left = hunk.Left;
            Range right = hunk.Right;

            if (right.Start == 0) ContextHeader(0, 0);
            int changes = Math.Min(left.Count, right.Count);
            for (int i = 0; i < changes; i++)
            {
                LineChanged(left.Start + i, right.Start + i);
            }
            if (left.Count > right.Count)
            {
                for (int i = changes; i < left.Count; i++)
                {
                    LineDeleted(left.Start + i, right.Start + changes);
                }
            }
            else if (left.Count < right.Count)
            {
                for (int i = changes; i < right.Count; i++)
                {
                    LineAdded(right.Start + i);
                }
            }
        }

        private void RenderContext(Diff.Hunk hunk)
        {
            Range left = hunk.Left;
            Range right = hunk.Right;
            int displayed = 0;

            if (Result.Length > 0) // not the first hunk, adding context for previous change
            {
                displayed = Math.Min(ContextLines, right.Count);
                for (int i = 0; i < displayed; i++) ContextLine(right.Start + i);
            }

            int toDisplay = Math.Min(right.Count - displayed, ContextLines);
            if ((left.End < LeftLines.Length - 1 || right.End < RightLines.Length - 1) && toDisplay > 0)
                // not the last hunk, adding context for next change
            {
                if (right.Count > displayed + toDisplay)
                {
                    ContextHeader(left.End - toDisplay + 1, right.End - toDisplay + 1);
                }
                for (int i = 0; i < toDisplay; i++)
                {
                    ContextLine(right.End - toDisplay + i + 1);
                }
            }
        }

        private void LineChanged(int leftLine, int rightLine)
        {
            // some kind of glitch with the diff engine
            if (LeftLines[leftLine] == RightLines[rightLine])
            {
                ContextLine(rightLine);
                return;
            }

            StringBuilder left = new StringBuilder();
            StringBuilder right = new StringBuilder();

            List<Word> leftList = Word.SplitString(LeftLines[leftLine]);
            List<Word> rightList = Word.SplitString(RightLines[rightLine]);

            diff = new Diff(leftList, rightList, Word.Comparer);

            foreach (Diff.Hunk h in diff)
            {
                if (h.Same)
                {
                    for (int i = 0; i < h.Left.Count; i++)
                    {
                        WhitespaceDiff(left, rightList[h.Right.Start + i], leftList[h.Left.Start + i]);
                        WhitespaceDiff(right, leftList[h.Left.Start + i], rightList[h.Right.Start + i]);
                    }
                }
                else
                {
                    WordDiff(left, h.Left, h.Right, leftList, rightList);

                    WordDiff(right, h.Right, h.Left, rightList, leftList);
                }
            }

            if (Variables.RTL)
            {
                Result.AppendFormat(@"<tr onclick='window.external.GoTo({1})' ondblclick='window.external.UndoChange({0},{1})'>
  <td>+</td>
  <td class='diff-addedline'>", rightLine, leftLine);
                Result.Append(right);
                Result.Append(@"  </td>
  <td>-</td>
  <td class='diff-deletedline'>");
                Result.Append(left);
                Result.Append(@"  </td>
		</tr>");
            }
            else
            {
                Result.AppendFormat(@"<tr onclick='window.external.GoTo({1})' ondblclick='window.external.UndoChange({0},{1})'>
  <td>-</td>
  <td class='diff-deletedline'>", leftLine, rightLine);
                Result.Append(left);
                Result.Append(@"  </td>
  <td>+</td>
  <td class='diff-addedline'>");
                Result.Append(right);
                Result.Append(@"  </td>
		</tr>");
            }
        }

        private static void WordDiff(StringBuilder res, Range range, Range otherRange, IList<Word> words,
                                     IList<Word> otherWords)
        {
            bool open = false;

            for (int i = 0; i < range.Count; i++)
            {
                if (i >= otherRange.Count ||
                    words[range.Start + i].ToString() != otherWords[otherRange.Start + i].ToString())
                {
                    if (!open) res.Append("<span class='diffchange'>");
                    open = true;
                }
                else
                {
                    if (open) res.Append("</span>");
                    open = false;
                }
                res.Append(HttpUtility.HtmlEncode(words[range.Start + i].ToString()));
            }

            if (open)
            {
                res.Append("</span>");
            }
        }

        private static void WhitespaceDiff(StringBuilder res, Word left, Word right)
        {
            if (left.Whitespace == right.Whitespace) res.Append(HttpUtility.HtmlEncode(right.ToString()));
            else
            {
                res.Append(HttpUtility.HtmlEncode(right.TheWord));
                char[] leftChars = left.Whitespace.ToCharArray();
                char[] rightChars = right.Whitespace.ToCharArray();

                Diff diff = new Diff(leftChars, rightChars, Word.Comparer);
                foreach (Diff.Hunk h in diff)
                {
                    if (h.Same)
                    {
                        res.Append(rightChars, h.Right.Start, h.Right.Count);
                    }
                    else
                    {
                        res.Append("<span class='diffchange'>");
                        res.Append('\x00A0', h.Right.Count); // replace spaces with NBSPs to make 'em visible
                        res.Append("</span>");
                    }
                }
            }
        }

        #endregion

        #region Visualisation primitives

        /// <summary>
        /// Renders a context row
        /// </summary>
        /// <param name="line">Number of line in the RIGHT text</param>
        private void ContextLine(int line)
        {
            string html = HttpUtility.HtmlEncode(RightLines[line]);
            Result.AppendFormat(@"<tr onclick='window.external.GoTo({0});'>
  <td class='diff-marker'> </td>
  <td class='diff-context'>", line);
            Result.Append(html);
            Result.Append(@"</td>
  <td class='diff-marker'> </td>
  <td class='diff-context'>");
            Result.Append(html);
            Result.Append(@"</td>
</tr>");
        }

        private void LineDeleted(int left, int right)
        {
            if (Variables.RTL)
            {
                Result.AppendFormat(@"<tr>
  <td> </td>
  <td> </td>
  <td>-</td>
  <td class='diff-deletedline' onclick='window.external.GoTo({1})' ondblclick='window.external.UndoDeletion({0}, {1})'>",
                                    left, right);

                Result.Append(HttpUtility.HtmlEncode(LeftLines[left]));
                Result.Append(@"  </td>
</tr>");
            }

            else
            {
                Result.AppendFormat(@"<tr>
  <td>-</td>
  <td class='diff-deletedline' onclick='window.external.GoTo({1})' ondblclick='window.external.UndoDeletion({0}, {1})'>",
                                    left, right);

                Result.Append(HttpUtility.HtmlEncode(LeftLines[left]));
                Result.Append(@"  </td>
  <td> </td>
  <td> </td>
</tr>");

            }
        }

        private void LineAdded(int line)
        {
            if (Variables.RTL)
            {
                Result.AppendFormat(@"<tr>
  <td>+</td>
  <td class='diff-addedline' onclick='window.external.GoTo({0})' ondblclick='window.external.UndoAddition({0})'>",
                                    line);
                Result.Append(HttpUtility.HtmlEncode(RightLines[line]));

                Result.Append(@"  </td>
  <td> </td>
  <td> </td>
</tr>");
            }
            else
            {
                Result.AppendFormat(@"<tr>
  <td> </td>
  <td> </td>
  <td>+</td>
  <td class='diff-addedline' onclick='window.external.GoTo({0})' ondblclick='window.external.UndoAddition({0})'>",
                                    line);
                Result.Append(HttpUtility.HtmlEncode(RightLines[line]));

                Result.Append(@"  </td>
</tr>");
            }
        }

        private void ContextHeader(int left, int right)
        {
            Result.AppendFormat(@"<tr onclick='window.external.GoTo({2})'>
  <td colspan='2' class='diff-lineno'>Line {0}:</td>
  <td colspan='2' class='diff-lineno'>Line {1}:</td>
</tr>", left + 1, right + 1, right);
        }

        #endregion

        #region Undo

        public string UndoChange(int left, int right)
        {
            RightLines[right] = LeftLines[left];

            return string.Join("\r\n", RightLines);
        }

        public string UndoAddition(int right)
        {
            StringBuilder s = new StringBuilder();

            for (int i = 0; i < RightLines.Length; i++)
                if (i != right)
                {
                    if (s.Length > 0)
                    {
                        s.Append("\r\n");
                    }
                    s.Append(RightLines[i]);
                }

            return s.ToString();
        }

        public string UndoDeletion(int left, int right)
        {
            StringBuilder s = new StringBuilder();

            for (int i = 0; i < RightLines.Length; i++)
            {
                if (i == right)
                {
                    if (s.Length > 0)
                    {
                        s.Append("\r\n");
                    }
                    s.Append(LeftLines[left]);
                }
                if (s.Length > 0)
                {
                    s.Append("\r\n");
                }
                s.Append(RightLines[i]);
            }

            if (left >= RightLines.Length)
            {
                if (s.Length > 0)
                {
                    s.Append("\r\n");
                }
                s.Append(LeftLines[left]);
            }

            return s.ToString();
        }

        #endregion

        #region Static methods

        private const string DiffColumnClasses = @"<table id='wikiDiff' class='diff'>
<col class='diff-marker' />
<col class='diff-content' />
<col class='diff-marker' />
<col class='diff-content' />";

        public static string TableHeader
        {
            get
            {
                string th = @"<p style='margin: 0px;'>Double click on a line to undo all changes on that line, or single click to focus the edit box to that line.</p>
" + DiffColumnClasses + @"
<thead>
  <tr valign='top'>";

                if (Variables.RTL)
                    th = th + @"
    <td colspan='2' width='50%' class='diff-ntitle'>Your text</td>
    <td colspan='2' width='50%' class='diff-otitle'>Current revision</td>";
                else
                    th = th + @"
    <td colspan='2' width='50%' class='diff-otitle'>Current revision</td>
	<td colspan='2' width='50%' class='diff-ntitle'>Your text</td>";

                th = th + @"
  </tr>
</thead>
";

                return th;
            }
        }

        /// <summary>
        /// TableHeader but without help information (double click to undo etc.) and current revision/ your text labels
        /// </summary>
        public static string TableHeaderNoMessages
        {
            get { return DiffColumnClasses; }
        }

        public static string DefaultStyles
        {
            get { return @"
/* suppress whitespace padding by C# WebBrowser, see
http://stackoverflow.com/questions/15033023/white-space-padding-in-webbrowser-control */
html , body {
 margin: 0;
}

/*
** Diff rendering
*/
table.diff, td.diff-otitle, td.diff-ntitle {
	background-color: white;
}

td.diff-otitle,
td.diff-ntitle {
	text-align: center;
}

td.diff-marker {
	text-align: right;
	font-weight: bold;
	font-size: 1.25em;
}

td.diff-lineno {
	font-weight: bold;
}

td.diff-addedline,
td.diff-deletedline,
td.diff-context {
	font-size: 88%;
	vertical-align: top;
	white-space: -moz-pre-wrap;
	white-space: pre-wrap;
}

td.diff-addedline,
td.diff-deletedline {
	border-style: solid;
	border-width: 1px 1px 1px 4px;
	border-radius: 0.33em;
}

td.diff-addedline {
	border-color: #a3d3ff;
}

td.diff-deletedline {
	border-color: #ffe49c;
}

td.diff-context {
	background: #f3f3f3;
	color: #333333;
	border-style: solid;
	border-width: 1px 1px 1px 4px;
	border-color: #e6e6e6;
	border-radius: 0.33em;
}

.diffchange {
	font-weight: bold;
	text-decoration: none;
}

table.diff {
	border: none;
	width: 100%;
	border-spacing: 4px;

	/* Ensure that colums are of equal width */
	table-layout: fixed;
}

td.diff-addedline .diffchange,
td.diff-deletedline .diffchange {
	border-radius: 0.33em;
	padding: 0.25em 0;
}

td.diff-addedline .diffchange {
	background: #d8ecff;
}

td.diff-deletedline .diffchange {
	background: #feeec8;
}

table.diff td {
	padding: 0.25em 0.25em;
}

table.diff col.diff-marker {
	width: 1.5%;
}

table.diff col.diff-content {
	width: 48.5%;
}

table.diff td div {
	/* Force-wrap very long lines such as URLs or page-widening char strings.*/
	word-wrap: break-word;

	/* As fallback (FF<3.5, Opera <10.5), scrollbars will be added for very wide cells
	   instead of text overflowing or widening
	*/
	overflow: auto;
}

"; }
        }

        private static string CustomStyles;

        /// <summary>
        /// Returns style header, from style.css file if present, else default style
        /// </summary>
        /// <returns></returns>
        public static string DiffHead()
        {
            string styles = DefaultStyles;

            if (!string.IsNullOrEmpty(CustomStyles))
                styles = CustomStyles;
            else if (File.Exists(Path.Combine(Application.StartupPath, "style.css")) && CustomStyles == null)
            {
                try
                {
                    StreamReader reader = File.OpenText(Path.Combine(Application.StartupPath, "style.css"));
                    CustomStyles = reader.ReadToEnd();
                    reader.Close();
                    styles = CustomStyles;
                }
                catch
                {
                    CustomStyles = "";
                }
            }

            return "<style type='text/css'>" + styles + "</style>";
        }

        public static void ResetCustomStyles()
        {
            CustomStyles = null;
        }

        #endregion
    }

    public sealed class Word
    {
        private readonly string m_ToString;
        private readonly int m_HashCode;

        public string TheWord { get; private set; }

        public string Whitespace { get; private set; }

        public Word(string word, string white)
        {
            TheWord = word;
            Whitespace = white;
            m_ToString = word + white;
            m_HashCode = (word /* + white*/).GetHashCode();
        }

        public static readonly IEqualityComparer Comparer = new WordComparer();

        /// borrowed from wikidiff2 for consistency
        private static bool IsText(char ch)
        {
            // Standard alphanumeric
            if ((ch >= '0' && ch <= '9') ||
                (ch == '_') ||
                (ch >= 'A' && ch <= 'Z') ||
                (ch >= 'a' && ch <= 'z'))
            {
                return true;
            }
            // Punctuation and control characters
            if (ch < 0xc0) return false;
            // Thai, return false so it gets split up
            if (ch >= 0xe00 && ch <= 0xee7) return false;
            // Chinese/Japanese, same
            if (ch >= 0x3000 && ch <= 0x9fff) return false;
            //if (ch >= 0x20000 && ch <= 0x2a000) return false;
            // Otherwise assume it's from a language that uses spaces
            return true;
        }

        /// borrowed from wikidiff2 for consistency
        private static bool IsSpace(char ch)
        {
            return ch == ' ' || ch == '\t';
        }

        public static List<Word> SplitString(string s)
        {
            List<Word> lst = new List<Word>();

            int pos = 0;
            int len = s.Length;
            while (pos < len)
            {
                char ch = s[pos];
                int i = pos;

                // first group has three different opportunities:
                if (IsSpace(ch))
                {
                    // one or more whitespace characters
                    while (i < len && IsSpace(s[i])) i++;
                }
                else if (IsText(ch))
                {
                    // one or more text characters
                    while (i < len && IsText(s[i])) i++;
                }
                else
                {
                    // one character, no matter what it is
                    i++;
                }

                string wordBody = s.Substring(pos, i - pos);
                pos = i;

                // second group: any whitespace character
                while (i < len && IsSpace(s[i]))
                {
                    i++;
                }
                string trail = s.Substring(pos, i - pos);

                lst.Add(new Word(wordBody, trail));
                pos = i;
            }

            return lst;
        }

        #region Overrides

        public override string ToString()
        {
            return m_ToString;
        }

        public override bool Equals(object obj)
        {
            return TheWord.Equals(((Word) obj).TheWord);
        }

        public override int GetHashCode()
        {
            return m_HashCode;
        }

        #endregion
    }

    internal class WordComparer : IEqualityComparer
    {
        int IEqualityComparer.GetHashCode(object obj)
        {
            return obj.GetHashCode();
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            return x.Equals(y);
        }

    }
}