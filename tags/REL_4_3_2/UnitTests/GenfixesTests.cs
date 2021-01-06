// This file is only for tests that require more than one transformation functions at the same time, so
//don't add tests for separate functions here

using System;
using System.Collections.Generic;
using System.Text;
using WikiFunctions;
using NUnit.Framework;
using WikiFunctions.Parse;
using WikiFunctions.Plugin;
using WikiFunctions.Logging;

namespace UnitTests
{
    public class GenfixesTestsBase
    {
        Article a = new Article("Test");
        Parsers p = new Parsers();
        HideText h = new HideText();
        MockSkipOptions s = new MockSkipOptions();

        public void GenFixes(bool replaceReferenceTags)
        {
            a.PerformGeneralFixes(p, h, s, replaceReferenceTags);
        }

        public void GenFixes()
        {
            GenFixes(true);
        }

        public GenfixesTestsBase()
        {
            Globals.UnitTestMode = true;
            WikiRegexes.MakeLangSpecificRegexes();
            a.InitialiseLogListener();
        }

        public string ArticleText
        {
            get { return a.ArticleText; }
            set
            {
                a.AWBChangeArticleText("unit testing", value, true);
                a.OriginalArticleText = value;
            }
        }

        public void AssertChange(string text, string expected)
        {
            ArticleText = text;
            GenFixes();
            Assert.AreEqual(expected, ArticleText);
        }

        public void AssertNotChanged(string text)
        {
            ArticleText = text;
            GenFixes();
            Assert.AreEqual(text, ArticleText);
        }
    }

    [TestFixture]
    public class GenfixesTests : GenfixesTestsBase
    {
        [Test]
        // http://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_2#Incorrect_Underscore_removal_in_URL.27s_in_wikilinks
        public void UndersoreRemovalInExternalLink()
        {
            // just in case...
            AssertNotChanged("test http://some_link test");

            AssertNotChanged("[http://some_link]");

            AssertChange("[[http://some_link]]", "[http://some_link]");
        }

        [Test]
        // http://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_2#Incorrect_Underscore_removal_in_URL.27s_in_wikilinks
        public void ExternalLinksInImageCaptions()
        {
            AssertNotChanged("[[Image:foo.jpg|Some http://some_crap.com]]");

            AssertNotChanged("[[Image:foo.jpg|Some [http://some_crap.com]]]");

            ArticleText = "[[Image:foo.jpg|Some [[http://some_crap.com]]]]";
            GenFixes();
            // not performing a full comparison due to a bug that should be tested elsewhere
            StringAssert.StartsWith("[[Image:foo.jpg|Some [http://some_crap.com]]]", ArticleText);
        }

        [Test]
        // superset of LinkTests.TestFixLinkWhitespace() and others, tests in complex
        public void LinkWhitespace()
        {
            AssertChange("[[a ]]b", "[[a]] b");
            AssertChange("a[[ b]]", "a [[b]]");
        }

        [Test]
        // this transformation is currently at Parsers.FixDates()
        public void DoubleBr()
        {
            AssertChange("a<br><br>b", "a\r\nb");
            AssertChange("a<br /><bR>b", "a\r\nb");
            AssertChange("a<BR> <Br/>b", "a\r\nb");
            AssertChange("<br><br>", ""); // \r\n removed as extra whitespace

            AssertNotChanged("a<br/br>b");
            AssertNotChanged("a<br/>\r\n<br>b");

            // http://en.wikipedia.org/wiki/Wikipedia_talk:AutoWikiBrowser/Bugs/Archive_6#General_fixes_problem:_br_tags_inside_templates
            AssertChange("{{foo|bar=a<br><br>b}}<br><br>quux", "{{foo|bar=a<br><br>b}}\r\nquux");

            AssertNotChanged("<blockquote>\r\n<br><br></blockquote>");
        }
    }
}
