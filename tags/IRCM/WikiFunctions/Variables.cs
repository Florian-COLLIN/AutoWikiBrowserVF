﻿/*
Copyright (C) 2007 Martin Richards, 2008 Stephen Kennedy

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

// TODO: Please clean me!

/* TODO: Template names and other constant strings used in Logging will need to be moved to Variables and internationalised.
To retain it's ability for easy reuse, it might be best if the classes in WikiFunctions use constants,
and the classes in AWB override those values and call .Variables? */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Windows.Forms;
using WikiFunctions.Browser;
using System.Text.RegularExpressions;
using System.Reflection;
using WikiFunctions.Plugin;
using WikiFunctions.Background;
using System.Net;

namespace WikiFunctions
{
    public enum LangCodeEnum
    {
        en, aa, ab, af, ak, als, am, an, ang, ar, arc, As, ast, av, ay, az,
        ba, bar, /*bat-smg,*/ bcl, be, bg, bh, bi, bn, bm, bo, bpy, br, bs, bug, bxr,
        ca, /*cbk-zam,*/ cdo, ce, ceb, ch, chr, chy, co, cr, crh, cs, csb, cu, cv, cy,
        da, de, diq, dsb, dv, dz,
        ee, el, eml, eo, es, et, eu, ext,
        fa, ff, fi, /*fiu-vro,*/ fj, fo, fr, frp, fur, fy,
        ga, gan, gd, gl, glk, gn, got, gu, gv,
        ha, hak, haw, he, hi, hif, hr, hsb, ht, hu, hy,
        ia, id, ie, ig, ik, ilo, io, Is, it, iu,
        ja, jbo, jv,
        ka, kaa, kab, kg, ki, kk, kl, km, kn, ko, ks, ksh, ku, kv, kw, ky,
        la, lad, lb, lbe, lg, li, lij, lmo, lm, lo, lt, lv,
        /*map-bms,*/ mdf, mg, mh, mi, mk, ml, mn, mr, ms, mt, my, myv, mzn,
        na, nah, nap, nds, /*nds-nl,*/ ne, New, ng, nl, nn, no, nov, nrm, nv, ny,
        oc, om, or, os,
        pa, pag, pam, pap, pdc, pl, pms, ps, pt,
        qu,
        rm, rmy, ro, /*roa-rup,*/ /*roa-tara,*/ ru, rw,
        sa, sah, sc, scn, sco, sd, se, sg, sh, si, simple, sk, sl, sm, sn, so, sq, sr, srn, ss, st, stq, su, sv, sw, szl,
        ta, te, tet, tg, th, ti, tk, tl, tn, to, tpi, tr, ts, tt, tum, tw, ty,
        ug, uk, ur, uz,
        ve, vec, vi, vls, vo,
        wa, war, wo, wuu,
        xal, xh,
        yi, yo,
        za, zae, zh, /*zh-classical,*/ /*zh-min-nan,*/ /*zh-yue,*/ zu
    }
    public enum ProjectEnum { wikipedia, wiktionary, wikisource, wikiquote, wikiversity, wikibooks, wikinews, species, commons, meta, mediawiki, wikia, custom }

    /// <summary>
    /// Holds some deepest-level things to be initialised prior to most other static classes,
    /// including Variables
    /// </summary>
    public static class Globals
    {
        /// <summary>
        /// Set this to true in unit tests, to disable checkpage loading and other slow stuff.
        /// This disables some functions, however.
        /// </summary>
        public static bool UnitTestMode = false;
    }

    /// <summary>
    /// Holds static variables, to allow functionality on different wikis.
    /// </summary>
    public static partial class Variables
    {
        static Variables()
        {
            CanonicalNamespaces[-2] = "Media:";
            CanonicalNamespaces[-1] = "Special:";
            CanonicalNamespaces[1] = "Talk:";
            CanonicalNamespaces[2] = "User:";
            CanonicalNamespaces[3] = "User talk:";
            CanonicalNamespaces[4] = "Project:";
            CanonicalNamespaces[5] = "Project talk:";
            CanonicalNamespaces[6] = "Image:";
            CanonicalNamespaces[7] = "Image talk:";
            CanonicalNamespaces[8] = "MediaWiki:";
            CanonicalNamespaces[9] = "MediaWiki talk:";
            CanonicalNamespaces[10] = "Template:";
            CanonicalNamespaces[11] = "Template talk:";
            CanonicalNamespaces[12] = "Help:";
            CanonicalNamespaces[13] = "Help talk:";
            CanonicalNamespaces[14] = "Category:";
            CanonicalNamespaces[15] = "Category talk:";

            if (!Globals.UnitTestMode)
            {
                SetProject(LangCodeEnum.en, ProjectEnum.wikipedia);
            }
            else
            {
                SetToEnglish("Wikipedia:", "Wikipedia talk:");
                Stub = "[Ss]tub";
                RegenerateRegexes();
            }
        }

        /// <summary>
        /// Gets the SVN revision of the current build and the date it was committed
        /// </summary>
        public static string Revision
        {
            get // see SvnInfo.template.cs for details
            {
                if (!m_Revision.Contains("$")) return m_Revision.Replace("/", "-");
                else return "?"; //fallback in case of failed revision extraction
            }
        }

        public static UserProperties User = new UserProperties();
        public static string RETFPath;

        private static IAutoWikiBrowser mMainForm;
        public static IAutoWikiBrowser MainForm
        {
            get { return Variables.mMainForm; }
            set { Variables.mMainForm = value; }
        }
        public static Profiler Profiler = new Profiler();

        #region project and language settings

        /// <summary>
        /// Provides access to the en namespace keys
        /// </summary>
        public static Dictionary<int, string> CanonicalNamespaces = new Dictionary<int, string>(20);

        /// <summary>
        /// Provides access to the namespace keys
        /// </summary>
        public static Dictionary<int, string> Namespaces = new Dictionary<int, string>(24);

        /// <summary>
        /// Provides access to the namespace keys in a form so the first letter is case insensitive e.g. [Ww]ikipedia:
        /// </summary>
        public static Dictionary<int, string> NamespacesCaseInsensitive = new Dictionary<int, string>(24);

        /// <summary>
        /// Gets a URL of the site, e.g. "http://en.wikipedia.org/w/".
        /// </summary>
        public static string URLLong
        { get { return URL + URLEnd; } }

        /// <summary>
        /// true if current wiki uses right-to-left writing system
        /// </summary>
        public static bool RTL;

        /// <summary>
        /// localized names of months
        /// </summary>
        public static string[] MonthNames;

        public static string[] ENLangMonthNames = new string[12]{"January", "February", "March", "April", "May", "June",
                "July", "August", "September", "October", "November", "December"};

        private static string URLEnd = "/w/";

        static string strURL = "http://en.wikipedia.org";
        /// <summary>
        /// Gets a URL of the site, e.g. "http://en.wikipedia.org".
        /// </summary>
        public static string URL
        {
            get { return strURL; }
            private set { strURL = value; }
        }

        /// <summary>
        /// Returns the script path of the site, e.g. /w
        /// </summary>
        public static string ScriptPath
        {
            get {return URLEnd.Substring(0, URLEnd.LastIndexOf('/')); }
        }

        private static ProjectEnum mProject = ProjectEnum.wikipedia;
        /// <summary>
        /// Gets a name of the project, e.g. "wikipedia".
        /// </summary>
        public static ProjectEnum Project
        { get { return mProject; } }

        private static LangCodeEnum mLangCode = LangCodeEnum.en;
        /// <summary>
        /// Gets the language code, e.g. "en".
        /// </summary>
        public static LangCodeEnum LangCode
        {
            get { return mLangCode; }
            internal set { mLangCode = value; }
        }

        /// <summary>
        /// Returns true if we are currently editing a WMF wiki
        /// </summary>
        public static bool IsWikimediaProject
        { get { return Project <= ProjectEnum.species; } }

        /// <summary>
        /// Returns true if we are currently editing the English Wikipedia
        /// </summary>
        public static bool IsWikipediaEN
        { get { return (Project == ProjectEnum.wikipedia && LangCode == LangCodeEnum.en); } }

        /// <summary>
        /// Returns true if we are currently a monolingual Wikimedia project
        /// </summary>
        public static bool IsWikimediaMonolingualProject
        {
            get
            {
                return (Project == ProjectEnum.commons || Project == ProjectEnum.meta
                  || Project == ProjectEnum.species || Project == ProjectEnum.mediawiki);
            }
        }

        /// <summary>
        /// Returns true if we are currently editing a "custom" wiki
        /// </summary>
        public static bool IsCustomProject
        { get { return Project == ProjectEnum.custom; } }

        /// <summary>
        /// Returns true if we are currently editing a Wikia site
        /// </summary>
        public static bool IsWikia
        { get { return Project == ProjectEnum.wikia; } }

        private static string strcustomproject = "";
        /// <summary>
        /// Gets script path of a custom project or empty string if standard project
        /// </summary>
        public static string CustomProject
        { get { return strcustomproject; } }

        private static string mSummaryTag = " using ";
        private static string strWPAWB = "[[Project:AWB|AWB]]";

        private static string strTypoSummaryTag = ", typos fixed: ";

        public static string TypoSummaryTag
        { get { return strTypoSummaryTag; } }

        /// <summary>
        /// Gets the tag to add to the edit summary, e.g. " using [[Wikipedia:AutoWikiBrowser|AWB]]".
        /// </summary>
        public static string SummaryTag
        { get { return mSummaryTag + strWPAWB; } }

        public static string WPAWB
        { get { return strWPAWB; } }

        private static void AWBDefaultSummaryTag()
        {
            mSummaryTag = " using ";
            strWPAWB = "[[Project:AutoWikiBrowser|AWB]]";
        }

        #region Delayed load stuff
        public static List<string> UnderscoredTitles = new List<string>();
        public static List<BackgroundRequest> DelayedRequests = new List<BackgroundRequest>();

        public static void CancelBackgroundRequests()
        {
            foreach (BackgroundRequest b in DelayedRequests) b.Abort();
            DelayedRequests.Clear();
        }

        public static void LoadUnderscores(params string[] templates)
        {
            BackgroundRequest r = new BackgroundRequest(new BackgroundRequestComplete(UnderscoresLoaded));
            r.HasUI = false;
            DelayedRequests.Add(r);
            r.GetList(new WikiFunctions.Lists.WhatTranscludesPageListMakerProvider(), templates);
        }

        static void UnderscoresLoaded(BackgroundRequest req)
        {
            UnderscoredTitles.Clear();
            foreach (Article a in (List<Article>)req.Result)
            {
                UnderscoredTitles.Add(a.Name);
            }
        }

        #endregion

        #region Proxy support
        static IWebProxy SystemProxy;

        public static HttpWebRequest PrepareWebRequest(string url, string UserAgent)
        {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(url);

            if (SystemProxy != null) r.Proxy = SystemProxy;

            if (string.IsNullOrEmpty(UserAgent))
                r.UserAgent = Tools.DefaultUserAgentString;
            else
                r.UserAgent = UserAgent;

            r.Proxy.Credentials = CredentialCache.DefaultCredentials;

            return r;
        }

        public static HttpWebRequest PrepareWebRequest(string url)
        { return PrepareWebRequest(url, ""); }

        public static void RefreshProxy()
        {
            SystemProxy = WebRequest.GetSystemWebProxy();
            if (SystemProxy.IsBypassed(new Uri(URL)))
            {
                SystemProxy = null;
            }
        }
        #endregion

        // for logging, these will probably need internationalising
        public static string AWBVersionString(string Version)
        {
            return "*" + WPAWB + " version " + Version + System.Environment.NewLine;
        }
        public static string AWBLoggingEditSummary
        { get { return "(" + WPAWB + " Logging) "; } }
        public const string UploadingLogEntryDefaultEditSummary = "Adding log entry";
        public const string UploadingLogDefaultEditSummary = "Uploading log";
        public const string LoggingStartButtonClicked = "Initialising log.";
        public const string StringUser = "User";
        public const string StringUserSkipped = "Clicked ignore";
        public const string StringPlugin = "Plugin";
        public const string StringPluginSkipped = "Plugin sent skip event";

        public static string Stub;
        public static string SectStub;
        public static Regex InUse = new Regex("(?!.*?<!--.*?{{inuse.*?}}.*?-->)(^.*?{{inuse.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Sets different language variables, such as namespaces. Default is english Wikipedia
        /// </summary>
        /// <param name="langCode">The language code, default is en</param>
        /// <param name="projectName">The project name default is Wikipedia</param>
        public static void SetProject(LangCodeEnum langCode, ProjectEnum projectName)
        {
            SetProject(langCode, projectName, "");
        }

        /// <summary>
        /// Sets different language variables, such as namespaces. Default is english Wikipedia
        /// </summary>
        /// <param name="langCode">The language code, default is en</param>
        /// <param name="projectName">The project name default is Wikipedia</param>
        /// <param name="customProject">Script path of a custom project or ""</param>
        public static void SetProject(LangCodeEnum langCode, ProjectEnum projectName, string customProject)
        {
            Namespaces.Clear();
            CancelBackgroundRequests();
            UnderscoredTitles.Clear();

            if (customProject.Contains("traditio.") || customProject.Contains("volgota.com")
                || customProject.Contains("encyclopediadramatica"))
            {
                MessageBox.Show("This software does not work on attack sites.");
                Application.Exit();
            }

            mProject = projectName;
            mLangCode = langCode;
            strcustomproject = customProject;

            RefreshProxy();

            URLEnd = "/w/";

            AWBDefaultSummaryTag();
            Stub = "[Ss]tub";

            MonthNames = ENLangMonthNames;

            SectStub = @"\{\{[Ss]ect";

            if (IsCustomProject)
            {
                mLangCode = LangCodeEnum.en;
                int x = customProject.IndexOf('/');

                if (x > 0)
                {
                    URLEnd = customProject.Substring(x, customProject.Length - x);
                    customProject = customProject.Substring(0, x);
                }

                URL = "http://" + CustomProject;
            }
            else
                URL = "http://" + LangCode.ToString().ToLower() + "." + Project + ".org";

            if (projectName == ProjectEnum.wikipedia)
            {
                #region Interwiki Switch
                //set language variables
                switch (langCode)
                {
                    case LangCodeEnum.en:
                        SetToEnglish();
                        break;

                    case LangCodeEnum.ar:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "خاص:";
                        Namespaces[1] = "نقاش:";
                        Namespaces[2] = "مستخدم:";
                        Namespaces[3] = "نقاش المستخدم:";
                        Namespaces[4] = "ويكيبيديا:";
                        Namespaces[5] = "نقاش ويكيبيديا:";
                        Namespaces[6] = "صورة:";
                        Namespaces[7] = "نقاش الصورة:";
                        Namespaces[8] = "ميدياويكي:";
                        Namespaces[9] = "نقاش ميدياويكي:";
                        Namespaces[10] = "قالب:";
                        Namespaces[11] = "نقاش القالب:";
                        Namespaces[12] = "مساعدة:";
                        Namespaces[13] = "نقاش المساعدة:";
                        Namespaces[14] = "تصنيف:";
                        Namespaces[15] = "نقاش التصنيف:";
                        Namespaces[100] = "بوابة:";
                        Namespaces[101] = "نقاش البوابة:";

                        mSummaryTag = " ";
                        strWPAWB = "باستخدام [[ويكيبيديا:أوب|الأوتوويكي براوزر]]";
                        strTypoSummaryTag = ".الأخطاء المصححة: ";
                        break;

                    case LangCodeEnum.bg:
                        Namespaces[-2] = "Медия:";
                        Namespaces[-1] = "Специални:";
                        Namespaces[1] = "Беседа:";
                        Namespaces[2] = "Потребител:";
                        Namespaces[3] = "Потребител беседа:";
                        Namespaces[4] = "Уикипедия:";
                        Namespaces[5] = "Уикипедия беседа:";
                        Namespaces[6] = "Картинка:";
                        Namespaces[7] = "Картинка беседа:";
                        Namespaces[8] = "МедияУики:";
                        Namespaces[9] = "МедияУики беседа:";
                        Namespaces[10] = "Шаблон:";
                        Namespaces[11] = "Шаблон беседа:";
                        Namespaces[12] = "Помощ:";
                        Namespaces[13] = "Помощ беседа:";
                        Namespaces[14] = "Категория:";
                        Namespaces[15] = "Категория беседа:";
                        Namespaces[100] = "Портал:";
                        Namespaces[101] = "Портал беседа:";

                        mSummaryTag = " редактирано с ";
                        strWPAWB = "AWB";
                        break;

                    case LangCodeEnum.ca:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "Especial:";
                        Namespaces[1] = "Discussió:";
                        Namespaces[2] = "Usuari:";
                        Namespaces[3] = "Usuari Discussió:";
                        Namespaces[4] = "Viquipèdia:";
                        Namespaces[5] = "Viquipèdia Discussió:";
                        Namespaces[6] = "Imatge:";
                        Namespaces[7] = "Imatge Discussió:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "MediaWiki Discussió:";
                        Namespaces[10] = "Plantilla:";
                        Namespaces[11] = "Plantilla Discussió:";
                        Namespaces[12] = "Ajuda:";
                        Namespaces[13] = "Ajuda Discussió:";
                        Namespaces[14] = "Categoria:";
                        Namespaces[15] = "Categoria Discussió:";
                        Namespaces[100] = "Portal:";
                        Namespaces[101] = "Portal Discussió:";
                        Namespaces[102] = "Viquiprojecte:";
                        Namespaces[103] = "Viquiprojecte Discussió:";

                        mSummaryTag = " ";
                        strWPAWB = "[[Viquipèdia:AutoWikiBrowser|AWB]]";
                        break;

                    case LangCodeEnum.da:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "Speciel:";
                        Namespaces[1] = "Diskussion:";
                        Namespaces[2] = "Bruger:";
                        Namespaces[3] = "Brugerdiskussion:";
                        Namespaces[4] = "Wikipedia:";
                        Namespaces[5] = "Wikipedia-diskussion:";
                        Namespaces[6] = "Billede:";
                        Namespaces[7] = "Billeddiskussion:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "MediaWiki-diskussion:";
                        Namespaces[10] = "Skabelon:";
                        Namespaces[11] = "Skabelon-diskussion:";
                        Namespaces[12] = "Hjælp:";
                        Namespaces[13] = "Hjælp-diskussion:";
                        Namespaces[14] = "Kategori:";
                        Namespaces[15] = "Kategoridiskussion:";
                        Namespaces[100] = "Portal:";
                        Namespaces[101] = "Portaldiskussion:";

                        mSummaryTag = " ved brug af ";
                        strWPAWB = "[[en:Wikipedia:AutoWikiBrowser|AWB]]";
                        break;

                    case LangCodeEnum.de:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "Spezial:";
                        Namespaces[1] = "Diskussion:";
                        Namespaces[2] = "Benutzer:";
                        Namespaces[3] = "Benutzer Diskussion:";
                        Namespaces[4] = "Wikipedia:";
                        Namespaces[5] = "Wikipedia Diskussion:";
                        Namespaces[6] = "Bild:";
                        Namespaces[7] = "Bild Diskussion:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "MediaWiki Diskussion:";
                        Namespaces[10] = "Vorlage:";
                        Namespaces[11] = "Vorlage Diskussion:";
                        Namespaces[12] = "Hilfe:";
                        Namespaces[13] = "Hilfe Diskussion:";
                        Namespaces[14] = "Kategorie:";
                        Namespaces[15] = "Kategorie Diskussion:";
                        Namespaces[100] = "Portal:";
                        Namespaces[101] = "Portal Diskussion:";
                        break;

                    case LangCodeEnum.dsb:
                        Namespaces[-2] = "Medija:";
                        Namespaces[-1] = "Specialne:";
                        Namespaces[1] = "Diskusija:";
                        Namespaces[2] = "Wužywaŕ:";
                        Namespaces[3] = "Diskusija wužywarja:";
                        Namespaces[4] = "Wikipedija:";
                        Namespaces[5] = "Wikipedija diskusija:";
                        Namespaces[6] = "Wobraz:";
                        Namespaces[7] = "Diskusija wó wobrazu:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "MediaWiki diskusija:";
                        Namespaces[10] = "Pśedłoga:";
                        Namespaces[11] = "Diskusija wó pśedłoze:";
                        Namespaces[12] = "Pomoc:";
                        Namespaces[13] = "Diskusija wó pomocy:";
                        Namespaces[14] = "Kategorija:";
                        Namespaces[15] = "Diskusija wó kategoriji:";
                        break;

                    case LangCodeEnum.es:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "Especial:";
                        Namespaces[1] = "Discusión:";
                        Namespaces[2] = "Usuario:";
                        Namespaces[3] = "Usuario Discusión:";
                        Namespaces[4] = "Wikipedia:";
                        Namespaces[5] = "Wikipedia Discusión:";
                        Namespaces[6] = "Imagen:";
                        Namespaces[7] = "Imagen Discusión:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "MediaWiki Discusión:";
                        Namespaces[10] = "Plantilla:";
                        Namespaces[11] = "Plantilla Discusión:";
                        Namespaces[12] = "Ayuda:";
                        Namespaces[13] = "Ayuda Discusión:";
                        Namespaces[14] = "Categoría:";
                        Namespaces[15] = "Categoría Discusión:";
                        Namespaces[100] = "Portal:";
                        Namespaces[101] = "Portal Discusión:";
                        Namespaces[102] = "Wikiproyecto:";
                        Namespaces[103] = "Wikiproyecto Discusión:";
                        break;

                    case LangCodeEnum.eo:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "Speciala:";
                        Namespaces[1] = "Diskuto:";
                        Namespaces[2] = "Vikipediisto:";
                        Namespaces[3] = "Vikipediista diskuto:";
                        Namespaces[4] = "Vikipedio:";
                        Namespaces[5] = "Vikipedia diskuto:";
                        Namespaces[6] = "Dosiero:";
                        Namespaces[7] = "Dosiera diskuto:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "MediaWiki diskuto:";
                        Namespaces[10] = "Ŝablono:";
                        Namespaces[11] = "Ŝablona diskuto:";
                        Namespaces[12] = "Helpo:";
                        Namespaces[13] = "Helpa diskuto:";
                        Namespaces[14] = "Kategorio:";
                        Namespaces[15] = "Kategoria diskuto:";
                        Namespaces[100] = "Portalo:";
                        Namespaces[101] = "Portala diskuto:";

                        mSummaryTag = " ";
                        strWPAWB = "[[Vikipedio:AutoWikiBrowser|AWB]]";
                        break;

                    case LangCodeEnum.fi:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "Toiminnot:";
                        Namespaces[1] = "Keskustelu:";
                        Namespaces[2] = "Käyttäjä:";
                        Namespaces[3] = "Keskustelu käyttäjästä:";
                        Namespaces[4] = "Wikipedia:";
                        Namespaces[5] = "Keskustelu Wikipediasta:";
                        Namespaces[6] = "Kuva:";
                        Namespaces[7] = "Keskustelu kuvasta:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "MediaWiki talk:";
                        Namespaces[10] = "Malline:";
                        Namespaces[11] = "Keskustelu mallineesta:";
                        Namespaces[12] = "Ohje:";
                        Namespaces[13] = "Keskustelu ohjeesta:";
                        Namespaces[14] = "Luokka:";
                        Namespaces[15] = "Keskustelu luokasta:";
                        Namespaces[100] = "Teemasivu:";
                        Namespaces[101] = "Keskustelu teemasivusta:";
                        break;

                    case LangCodeEnum.fr:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "Special:";
                        Namespaces[1] = "Discuter:";
                        Namespaces[2] = "Utilisateur:";
                        Namespaces[3] = "Discussion Utilisateur:";
                        Namespaces[4] = "Wikipédia:";
                        Namespaces[5] = "Discussion Wikipédia:";
                        Namespaces[6] = "Image:";
                        Namespaces[7] = "Discussion Image:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "Discussion MediaWiki:";
                        Namespaces[10] = "Modèle:";
                        Namespaces[11] = "Discussion Modèle:";
                        Namespaces[12] = "Aide:";
                        Namespaces[13] = "Discussion Aide:";
                        Namespaces[14] = "Catégorie:";
                        Namespaces[15] = "Discussion Catégorie:";
                        Namespaces[100] = "Portail:";
                        Namespaces[101] = "Discussion Portail:";
                        Namespaces[102] = "Projet:";
                        Namespaces[103] = "Discussion Projet:";
                        Namespaces[104] = "Référence:";
                        Namespaces[105] = "Discussion Référence:";
                        break;

                    case LangCodeEnum.hi:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "विशेष:";
                        Namespaces[1] = "वार्ता:";
                        Namespaces[2] = "सदस्य:";
                        Namespaces[3] = "सदस्य वार्ता:";
                        Namespaces[4] = "विकिपीडिया:";
                        Namespaces[5] = "विकिपीडिया वार्ता:";
                        Namespaces[6] = "चित्र:";
                        Namespaces[7] = "चित्र वार्ता:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "MediaWiki talk:";
                        Namespaces[10] = "साँचा:";
                        Namespaces[11] = "साँचा वार्ता:";
                        Namespaces[12] = "Help:";
                        Namespaces[13] = "Help talk:";
                        Namespaces[14] = "श्रेणी:";
                        Namespaces[15] = "श्रेणी वार्ता:";
                        Namespaces[100] = "प्रवेशद्वार:";
                        Namespaces[101] = "प्रवेशद्वार वार्ता:";
                        break;

                    case LangCodeEnum.hu:
                        Namespaces[-2] = "Média:";
                        Namespaces[-1] = "Speciális:";
                        Namespaces[1] = "Vita:";
                        Namespaces[2] = "Szerkesztő:";
                        Namespaces[3] = "Szerkesztővita:";
                        Namespaces[4] = "Wikipédia:";
                        Namespaces[5] = "Wikipédia-vita:";
                        Namespaces[6] = "Kép:";
                        Namespaces[7] = "Képvita:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "MediaWiki-vita:";
                        Namespaces[10] = "Sablon:";
                        Namespaces[11] = "Sablonvita:";
                        Namespaces[12] = "Segítség:";
                        Namespaces[13] = "Segítségvita:";
                        Namespaces[14] = "Kategória:";
                        Namespaces[15] = "Kategóriavita:";
                        Namespaces[100] = "Portál:";
                        Namespaces[101] = "Portálvita:";

                        mSummaryTag = " ";
                        strWPAWB = "[[Wikipédia:AutoWikiBrowser|AWB]]";
                        break;

                    case LangCodeEnum.Is:
                        Namespaces[-2] = "Miðill:";
                        Namespaces[-1] = "Kerfissíða:";
                        Namespaces[1] = "Spjall:";
                        Namespaces[2] = "Notandi:";
                        Namespaces[3] = "Notandaspjall:";
                        Namespaces[4] = "Wikipedia:";
                        Namespaces[5] = "Wikipediaspjall:";
                        Namespaces[6] = "Mynd:";
                        Namespaces[7] = "Myndaspjall:";
                        Namespaces[8] = "Melding:";
                        Namespaces[9] = "Meldingarspjall:";
                        Namespaces[10] = "Snið:";
                        Namespaces[11] = "Sniðaspjall:";
                        Namespaces[12] = "Hjálp:";
                        Namespaces[13] = "Hjálparspjall:";
                        Namespaces[14] = "Flokkur:";
                        Namespaces[15] = "Flokkaspjall:";
                        Namespaces[100] = "Gátt:";
                        Namespaces[101] = "Gáttaspjall:";
                        break;

                    case LangCodeEnum.it:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "Speciale:";
                        Namespaces[1] = "Discussione:";
                        Namespaces[2] = "Utente:";
                        Namespaces[3] = "Discussioni utente:";
                        Namespaces[4] = "Wikipedia:";
                        Namespaces[5] = "Discussioni Wikipedia:";
                        Namespaces[6] = "Immagine:";
                        Namespaces[7] = "Discussioni immagine:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "Discussioni MediaWiki:";
                        Namespaces[10] = "Template:";
                        Namespaces[11] = "Discussioni template:";
                        Namespaces[12] = "Aiuto:";
                        Namespaces[13] = "Discussioni aiuto:";
                        Namespaces[14] = "Categoria:";
                        Namespaces[15] = "Discussioni categoria:";
                        Namespaces[100] = "Portale:";
                        Namespaces[101] = "Discussioni portale:";
                        break;

                    case LangCodeEnum.ja:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "特別:";
                        Namespaces[1] = "ノート:";
                        Namespaces[2] = "利用者:";
                        Namespaces[3] = "利用者‐会話:";
                        Namespaces[4] = "Wikipedia:";
                        Namespaces[5] = "Wikipedia‐ノート:";
                        Namespaces[6] = "画像:";
                        Namespaces[7] = "画像‐ノート:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "MediaWiki‐ノート:";
                        Namespaces[10] = "Template:";
                        Namespaces[11] = "Template‐ノート:";
                        Namespaces[12] = "Help:";
                        Namespaces[13] = "Help‐ノート:";
                        Namespaces[14] = "Category:";
                        Namespaces[15] = "Category‐ノート:";
                        Namespaces[100] = "Portal:";
                        Namespaces[101] = "Portal‐ノート:";
                        break;

                    case LangCodeEnum.ku:
                        Namespaces[-2] = "Medya:";
                        Namespaces[-1] = "Taybet:";
                        Namespaces[1] = "Nîqaş:";
                        Namespaces[2] = "Bikarhêner:";
                        Namespaces[3] = "Bikarhêner nîqaş:";
                        Namespaces[4] = "Wîkîpediya:";
                        Namespaces[5] = "Wîkîpediya nîqaş:";
                        Namespaces[6] = "Wêne:";
                        Namespaces[7] = "Wêne nîqaş:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "MediaWiki nîqaş:";
                        Namespaces[10] = "Şablon:";
                        Namespaces[11] = "Şablon nîqaş:";
                        Namespaces[12] = "Alîkarî:";
                        Namespaces[13] = "Alîkarî nîqaş:";
                        Namespaces[14] = "Kategorî:";
                        Namespaces[15] = "Kategorî nîqaş:";

                        mSummaryTag = " ";
                        strWPAWB = "[[Wîkîpediya:AutoWikiBrowser|AWB]]";
                        break;

                    case LangCodeEnum.mi:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "Special:";
                        Namespaces[1] = "Talk:";
                        Namespaces[2] = "User:";
                        Namespaces[3] = "User talk:";
                        Namespaces[4] = "Wikipedia:";
                        Namespaces[5] = "Wikipedia talk:";
                        Namespaces[6] = "Image:";
                        Namespaces[7] = "Image talk:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "MediaWiki talk:";
                        Namespaces[10] = "Template:";
                        Namespaces[11] = "Template talk:";
                        Namespaces[12] = "Help:";
                        Namespaces[13] = "Help talk:";
                        Namespaces[14] = "Category:";
                        Namespaces[15] = "Category talk:";
                        break;

                    case LangCodeEnum.nl:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "Speciaal:";
                        Namespaces[1] = "Overleg:";
                        Namespaces[2] = "Gebruiker:";
                        Namespaces[3] = "Overleg gebruiker:";
                        Namespaces[4] = "Wikipedia:";
                        Namespaces[5] = "Overleg Wikipedia:";
                        Namespaces[6] = "Afbeelding:";
                        Namespaces[7] = "Overleg afbeelding:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "Overleg MediaWiki:";
                        Namespaces[10] = "Sjabloon:";
                        Namespaces[11] = "Overleg sjabloon:";
                        Namespaces[12] = "Help:";
                        Namespaces[13] = "Overleg help:";
                        Namespaces[14] = "Categorie:";
                        Namespaces[15] = "Overleg categorie:";
                        Namespaces[100] = "Portaal:";
                        Namespaces[101] = "Overleg portaal:";

                        mSummaryTag = " met ";
                        strWPAWB = "[[Wikipedia:AutoWikiBrowser|AWB]]";
                        break;

                    case LangCodeEnum.no:
                        Namespaces[-2] = "Medium:";
                        Namespaces[-1] = "Spesial:";
                        Namespaces[1] = "Diskusjon:";
                        Namespaces[2] = "Bruker:";
                        Namespaces[3] = "Brukerdiskusjon:";
                        Namespaces[4] = "Wikipedia:";
                        Namespaces[5] = "Wikipedia-diskusjon:";
                        Namespaces[6] = "Bilde:";
                        Namespaces[7] = "Bildediskusjon:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "MediaWiki-diskusjon:";
                        Namespaces[10] = "Mal:";
                        Namespaces[11] = "Maldiskusjon:";
                        Namespaces[12] = "Hjelp:";
                        Namespaces[13] = "Hjelpdiskusjon:";
                        Namespaces[14] = "Kategori:";
                        Namespaces[15] = "Kategoridiskusjon:";
                        Namespaces[100] = "Portal:";
                        Namespaces[101] = "Portaldiskusjon:";
                        break;

                    case LangCodeEnum.pl:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "Specjalna:";
                        Namespaces[1] = "Dyskusja:";
                        Namespaces[2] = "Wikipedysta:";
                        Namespaces[3] = "Dyskusja Wikipedysty:";
                        Namespaces[4] = "Wikipedia:";
                        Namespaces[5] = "Dyskusja Wikipedii:";
                        Namespaces[6] = "Grafika:";
                        Namespaces[7] = "Dyskusja grafiki:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "Dyskusja MediaWiki:";
                        Namespaces[10] = "Szablon:";
                        Namespaces[11] = "Dyskusja szablonu:";
                        Namespaces[12] = "Pomoc:";
                        Namespaces[13] = "Dyskusja pomocy:";
                        Namespaces[14] = "Kategoria:";
                        Namespaces[15] = "Dyskusja kategorii:";
                        Namespaces[100] = "Portal:";
                        Namespaces[101] = "Dyskusja portalu:";

                        SectStub = @"\{\{[Ss]ek";
                        break;

                    case LangCodeEnum.pt:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "Especial:";
                        Namespaces[1] = "Discussão:";
                        Namespaces[2] = "Usuário:";
                        Namespaces[3] = "Usuário Discussão:";
                        Namespaces[4] = "Wikipedia:";
                        Namespaces[5] = "Wikipedia Discussão:";
                        Namespaces[6] = "Imagem:";
                        Namespaces[7] = "Imagem Discussão:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "MediaWiki Discussão:";
                        Namespaces[10] = "Predefinição:";
                        Namespaces[11] = "Predefinição Discussão:";
                        Namespaces[12] = "Ajuda:";
                        Namespaces[13] = "Ajuda Discussão:";
                        Namespaces[14] = "Categoria:";
                        Namespaces[15] = "Categoria Discussão:";
                        Namespaces[100] = "Portal:";
                        Namespaces[101] = "Discussão Portal:";
                        Namespaces[102] = "Anexo:";
                        Namespaces[103] = "Anexo Discussão:";

                        mSummaryTag = " utilizando ";
                        strWPAWB = "[[Wikipedia:AutoWikiBrowser|AWB]]";
                        break;

                    case LangCodeEnum.ro:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "Special:";
                        Namespaces[1] = "Discuţie:";
                        Namespaces[2] = "Utilizator:";
                        Namespaces[3] = "Discuţie Utilizator:";
                        Namespaces[4] = "Wikipedia:";
                        Namespaces[5] = "Discuţie Wikipedia:";
                        Namespaces[6] = "Imagine:";
                        Namespaces[7] = "Discuţie Imagine:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "Discuţie MediaWiki:";
                        Namespaces[10] = "Format:";
                        Namespaces[11] = "Discuţie Format:";
                        Namespaces[12] = "Ajutor:";
                        Namespaces[13] = "Discuţie Ajutor:";
                        Namespaces[14] = "Categorie:";
                        Namespaces[15] = "Discuţie Categorie:";
                        Namespaces[100] = "Portal:";
                        Namespaces[101] = "Discuţie Portal:";
                        break;

                    case LangCodeEnum.ru:
                        Namespaces[-2] = "Медиа:";
                        Namespaces[-1] = "Служебная:";
                        Namespaces[1] = "Обсуждение:";
                        Namespaces[2] = "Участник:";
                        Namespaces[3] = "Обсуждение участника:";
                        Namespaces[4] = "Википедия:";
                        Namespaces[5] = "Обсуждение Википедии:";
                        Namespaces[6] = "Изображение:";
                        Namespaces[7] = "Обсуждение изображения:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "Обсуждение MediaWiki:";
                        Namespaces[10] = "Шаблон:";
                        Namespaces[11] = "Обсуждение шаблона:";
                        Namespaces[12] = "Справка:";
                        Namespaces[13] = "Обсуждение справки:";
                        Namespaces[14] = "Категория:";
                        Namespaces[15] = "Обсуждение категории:";
                        Namespaces[100] = "Портал:";
                        Namespaces[101] = "Обсуждение портала:";

                        mSummaryTag = " с помощью ";
                        strWPAWB = "[[WP:AWB|AWB]]";
                        Stub = "(?:[Ss]tub|[Зз]аготовка)";
                        MonthNames = new string[12] { "января", "февраля", "марта", "апреля",
                            "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря"};
                        break;

                    case LangCodeEnum.si:
                        Namespaces[-2] = "මාධ්‍යය:";
                        Namespaces[-1] = "විශේෂ:";
                        Namespaces[1] = "සාකච්ඡාව:";
                        Namespaces[2] = "පරිශීලක:";
                        Namespaces[3] = "පරිශීලක සාකච්ඡාව:";
                        Namespaces[4] = "Wikipedia:";
                        Namespaces[5] = "Wikipedia සාකච්ඡාව:";
                        Namespaces[6] = "රූපය:";
                        Namespaces[7] = "රූපය සාකච්ඡාව:";
                        Namespaces[8] = "විකිමාධ්‍ය:";
                        Namespaces[9] = "විකිමාධ්‍ය සාකච්ඡාව:";
                        Namespaces[10] = "සැකිල්ල:";
                        Namespaces[11] = "සැකිල සාකච්ඡාව:";
                        Namespaces[12] = "උදවු:";
                        Namespaces[13] = "උදව සාකච්ඡාව:";
                        Namespaces[14] = "ප්‍රවර්ගය:";
                        Namespaces[15] = "ප්‍රවර්ග සාකච්ඡාව:";
                        Namespaces[100] = "Portal:";
                        Namespaces[101] = "Portal talk:";
                        break;

                    case LangCodeEnum.simple:
                        SetToEnglish("Wikipedia:", "Wikipedia talk:");
                        break;

                    case LangCodeEnum.sk:
                        Namespaces[-2] = "Médiá:";
                        Namespaces[-1] = "Špeciálne:";
                        Namespaces[1] = "Diskusia:";
                        Namespaces[2] = "Redaktor:";
                        Namespaces[3] = "Diskusia s redaktorom:";
                        Namespaces[4] = "Wikipédia:";
                        Namespaces[5] = "Diskusia k Wikipédii:";
                        Namespaces[6] = "Obrázok:";
                        Namespaces[7] = "Diskusia k obrázku:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "Diskusia k MediaWiki:";
                        Namespaces[10] = "Šablóna:";
                        Namespaces[11] = "Diskusia k šablóne:";
                        Namespaces[12] = "Pomoc:";
                        Namespaces[13] = "Diskusia k pomoci:";
                        Namespaces[14] = "Kategória:";
                        Namespaces[15] = "Diskusia ku kategórii:";
                        Namespaces[100] = "Portál:";
                        Namespaces[101] = "Diskusia k portálu:";

                        mSummaryTag = " ";
                        strWPAWB = "[[Wikipédia:AutoWikiBrowser|AWB]]";
                        break;

                    case LangCodeEnum.sl:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "Posebno:";
                        Namespaces[1] = "Pogovor:";
                        Namespaces[2] = "Uporabnik:";
                        Namespaces[3] = "Uporabniški pogovor:";
                        Namespaces[4] = "Wikipedija:";
                        Namespaces[5] = "Pogovor o Wikipediji:";
                        Namespaces[6] = "Slika:";
                        Namespaces[7] = "Pogovor o sliki:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "Pogovor o MediaWiki:";
                        Namespaces[10] = "Predloga:";
                        Namespaces[11] = "Pogovor o predlogi:";
                        Namespaces[12] = "Pomoč:";
                        Namespaces[13] = "Pogovor o pomoči:";
                        Namespaces[14] = "Kategorija:";
                        Namespaces[15] = "Pogovor o kategoriji:";

                        mSummaryTag = " ";
                        strWPAWB = "[[Wikipedija:AutoWikiBrowser|AWB]]";
                        Stub = "(?:[Ss]tub|[Šš]krbina)";
                        break;

                    case LangCodeEnum.sr:
                        Namespaces[-2] = "Медија:";
                        Namespaces[-1] = "Посебно:";
                        Namespaces[1] = "Разговор:";
                        Namespaces[2] = "Корисник:";
                        Namespaces[3] = "Разговор са корисником:";
                        Namespaces[4] = "Википедија:";
                        Namespaces[5] = "Разговор о Википедији:";
                        Namespaces[6] = "Слика:";
                        Namespaces[7] = "Разговор о слици:";
                        Namespaces[8] = "МедијаВики:";
                        Namespaces[9] = "Разговор о МедијаВикију:";
                        Namespaces[10] = "Шаблон:";
                        Namespaces[11] = "Разговор о шаблону:";
                        Namespaces[12] = "Помоћ:";
                        Namespaces[13] = "Разговор о помоћи:";
                        Namespaces[14] = "Категорија:";
                        Namespaces[15] = "Разговор о категорији:";
                        Namespaces[100] = "Портал:";
                        Namespaces[101] = "Разговор о порталу:";
                        break;

                    case LangCodeEnum.sv:
                        Namespaces[-2] = "Media:";
                        Namespaces[-1] = "Special:";
                        Namespaces[1] = "Diskussion:";
                        Namespaces[2] = "Användare:";
                        Namespaces[3] = "Användardiskussion:";
                        Namespaces[4] = "Wikipedia:";
                        Namespaces[5] = "Wikipediadiskussion:";
                        Namespaces[6] = "Bild:";
                        Namespaces[7] = "Bilddiskussion:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "MediaWiki diskussion:";
                        Namespaces[10] = "Mall:";
                        Namespaces[11] = "Malldiskussion:";
                        Namespaces[12] = "Hjälp:";
                        Namespaces[13] = "Hjälp diskussion:";
                        Namespaces[14] = "Kategori:";
                        Namespaces[15] = "Kategoridiskussion:";
                        Namespaces[100] = "Portal:";
                        Namespaces[101] = "Portaldiskussion:";
                        break;

                    case LangCodeEnum.te:
                        Namespaces[-2] = "మీడియా:";
                        Namespaces[-1] = "ప్రత్యేక:";
                        Namespaces[1] = "చర్చ:";
                        Namespaces[2] = "సభ్యుడు:";
                        Namespaces[3] = "సభ్యులపై చర్చ:";
                        Namespaces[4] = "వికీపీడియా:";
                        Namespaces[5] = "వికీపీడియా చర్చ:";
                        Namespaces[6] = "బొమ్మ:";
                        Namespaces[7] = "బొమ్మపై చర్చ:";
                        Namespaces[8] = "మీడియావికీ:";
                        Namespaces[9] = "మీడియావికీ చర్చ:";
                        Namespaces[10] = "మూస:";
                        Namespaces[11] = "మూస చర్చ:";
                        Namespaces[12] = "సహాయము:";
                        Namespaces[13] = "సహాయము చర్చ:";
                        Namespaces[14] = "వర్గం:";
                        Namespaces[15] = "వర్గం చర్చ:";
                        break;

                    case LangCodeEnum.uk:
                        Namespaces[-2] = "Медіа:";
                        Namespaces[-1] = "Спеціальні:";
                        Namespaces[1] = "Обговорення:";
                        Namespaces[2] = "Користувач:";
                        Namespaces[3] = "Обговорення користувача:";
                        Namespaces[4] = "Вікіпедія:";
                        Namespaces[5] = "Обговорення Вікіпедії:";
                        Namespaces[6] = "Зображення:";
                        Namespaces[7] = "Обговорення зображення:";
                        Namespaces[8] = "MediaWiki:";
                        Namespaces[9] = "Обговорення MediaWiki:";
                        Namespaces[10] = "Шаблон:";
                        Namespaces[11] = "Обговорення шаблону:";
                        Namespaces[12] = "Довідка:";
                        Namespaces[13] = "Обговорення довідки:";
                        Namespaces[14] = "Категорія:";
                        Namespaces[15] = "Обговорення категорії:";

                        Stub = "(?:[Ss]tub|[Дд]оробити)";
                        mSummaryTag = " з допомогою ";
                        strWPAWB = "[[Вікіпедія:AutoWikiBrowser|AWB]]";
                        break;

                    //http://xx.project.org/wiki/api.php?action=query&meta=siteinfo&siprop=namespaces
                    // case LangCodeEnum.xx:
                    // Namespaces[-2] = ":";
                    // Namespaces[-1] = ":";
                    // Namespaces[1] = ":";
                    // Namespaces[2] = ":";
                    // Namespaces[3] = ":";
                    // Namespaces[4] = ":";
                    // Namespaces[5] = ":";
                    // Namespaces[6] = ":";
                    // Namespaces[7] = ":";
                    // Namespaces[8] = ":";
                    // Namespaces[9] = ":";
                    // Namespaces[10] = ":";
                    // Namespaces[11] = ":";
                    // Namespaces[12] = ":";
                    // Namespaces[13] = ":";
                    // Namespaces[14] = ":";
                    // Namespaces[15] = ":";
                    // Namespaces[100] = ":";
                    // Namespaces[101] = ":";

                    // strsummarytag = " ";
                    // strWPAWB = "";
                    // break;

                    default:
                        LoadProjectOptions();
                        break;
                }

                #endregion
            }
            else if (projectName == ProjectEnum.commons)
            {
                SetToEnglish("Commons:", "Commons talk:");
                Namespaces[100] = "Creator:";
                Namespaces[101] = "Creator talk:";
                URL = "http://commons.wikimedia.org";
                mLangCode = LangCodeEnum.en;
            }
            else if (projectName == ProjectEnum.meta)
            {
                SetToEnglish("Meta:", "Meta talk:");
                Namespaces[100] = "Hilfe:";
                Namespaces[101] = "Hilfe Diskussion:";
                Namespaces[102] = "Aide:";
                Namespaces[103] = "Discussion Aide:";
                Namespaces[104] = "Hjælp:";
                Namespaces[105] = "Hjælp diskussion:";
                Namespaces[106] = "Helpo:";
                Namespaces[107] = "Helpa diskuto:";
                Namespaces[108] = "Hjälp:";
                Namespaces[109] = "Hjälp diskussion:";
                Namespaces[110] = "Ayuda:";
                Namespaces[111] = "Ayuda Discusión:";
                Namespaces[112] = "Aiuto:";
                Namespaces[113] = "Discussioni aiuto:";
                Namespaces[114] = "ヘルプ:";
                Namespaces[115] = "ヘルプ‐ノート:";
                Namespaces[116] = "NL Help:";
                Namespaces[117] = "Overleg help:";
                Namespaces[118] = "Pomoc:";
                Namespaces[119] = "Dyskusja pomocy:";
                Namespaces[120] = "Ajuda:";
                Namespaces[121] = "Ajuda Discussão:";
                Namespaces[122] = "CA Ajuda:";
                Namespaces[123] = "CA Ajuda Discussió:";
                Namespaces[124] = "Hjelp:";
                Namespaces[125] = "Hjelp diskusjon:";
                Namespaces[126] = "帮助:";
                Namespaces[127] = "帮助 对话:";
                Namespaces[128] = "Помощь:";
                Namespaces[129] = "Помощь Дискуссия:";
                Namespaces[130] = "Pomoč:";
                Namespaces[131] = "Pogovor o pomoči:";
                URL = "http://meta.wikimedia.org";
                mLangCode = LangCodeEnum.en;
            }
            else if (projectName == ProjectEnum.mediawiki)
            {
                SetToEnglish("Project:", "Project talk:");
                URL = "http://www.mediawiki.org";
                mLangCode = LangCodeEnum.en;
            }
            else if (projectName == ProjectEnum.species)
            {
                SetToEnglish("Wikispecies:", "Wikispecies talk:");
                URL = "http://species.wikimedia.org";
                mLangCode = LangCodeEnum.en;
            }
            else if (projectName == ProjectEnum.wikia)
            {
                URL = "http://" + customProject + ".wikia.com";
                URLEnd = "/";
                LoadProjectOptions();
            }
            else
            {
                if (projectName == ProjectEnum.custom)
                    URLEnd = "";
                LoadProjectOptions();
            }

            //refresh once more in case project settings were reset due to
            //error with loading
            RefreshProxy();

            RegenerateRegexes();

            RETFPath = Namespaces[4] + "AutoWikiBrowser/Typos";

            foreach (string s in Namespaces.Values)
            {
                System.Diagnostics.Trace.Assert(s.EndsWith(":"), "Internal error: namespace does not end with ':'.",
                    "Please contact a developer.");
            }
            System.Diagnostics.Trace.Assert(!Namespaces.ContainsKey(0), "Internal error: key exists for namespace 0.",
                    "Please contact a developer.");
        }

        private static void RegenerateRegexes()
        {
            NamespacesCaseInsensitive.Clear();
            bool LangNotEnglish = (LangCode != LangCodeEnum.en);
            foreach (KeyValuePair<int, string> k in Namespaces)
            {
                //other languages can use the english syntax
                if (LangNotEnglish && CanonicalNamespaces.ContainsKey(k.Key))
                    NamespacesCaseInsensitive.Add(k.Key, "(?:" + Tools.AllCaseInsensitive(k.Value) + "|" + Tools.AllCaseInsensitive(CanonicalNamespaces[k.Key]).Replace(":", " ?:") + ")");
                else
                    NamespacesCaseInsensitive.Add(k.Key, Tools.AllCaseInsensitive(k.Value).Replace(":", " ?:"));
            }

            WikiRegexes.MakeLangSpecificRegexes();
        }

        /// <summary>
        /// Loads namespaces
        /// </summary>
        public static void LoadProjectOptions()
        {
            string[] months = (string[])ENLangMonthNames.Clone();

            try
            {
                SiteInfo si = new SiteInfo(URLLong);

                for (int i = 0; i < months.Length; i++) months[i] += "-gen";
                Dictionary<string, string> messages = si.GetMessages(months);

                if (messages.Count == 12)
                {
                    for (int i = 0; i < months.Length; i++)
                    {
                        months[i] = messages[months[i]];
                    }
                    MonthNames = months;
                }

                Namespaces = si.Namespaces;
            }
            catch
            {
                MessageBox.Show("An error occured while connecting to the server or loading project information from it. " +
                        "Please make sure that your internet connection works and such combination of project/language exist." +
                        "\r\nEnter the URL in the format \"en.wikipedia.org/w/\"",
                        "Error connecting to wiki", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetDefaults();
                return;
            }
        }

        private static void SetDefaults()
        {
            mProject = ProjectEnum.wikipedia;
            mLangCode = LangCodeEnum.en;
            mSummaryTag = " using ";
            strWPAWB = "[[Project:AWB|AWB]]";

            Namespaces.Clear();
            SetToEnglish();
        }

        public static void SetToEnglish()
        {
            SetToEnglish("Wikipedia:", "Wikipedia talk:");
            Namespaces[100] = "Portal:";
            Namespaces[101] = "Portal talk:";
        }

        private static void SetToEnglish(string Project, string ProjectTalk)
        {
            Namespaces[-2] = "Media:";
            Namespaces[-1] = "Special:";
            Namespaces[1] = "Talk:";
            Namespaces[2] = "User:";
            Namespaces[3] = "User talk:";
            Namespaces[4] = Project;
            Namespaces[5] = ProjectTalk;
            Namespaces[6] = "Image:";
            Namespaces[7] = "Image talk:";
            Namespaces[8] = "MediaWiki:";
            Namespaces[9] = "MediaWiki talk:";
            Namespaces[10] = "Template:";
            Namespaces[11] = "Template talk:";
            Namespaces[12] = "Help:";
            Namespaces[13] = "Help talk:";
            Namespaces[14] = "Category:";
            Namespaces[15] = "Category talk:";

            mSummaryTag = " using ";

            MonthNames = ENLangMonthNames;
            SectStub = @"\{\{[Ss]ect";
            Stub = "[Ss]tub";

            RTL = false;
        }

        public static LangCodeEnum ParseLanguage(string lang)
        {
            if (string.Compare(lang, "is", true) == 0) return LangCodeEnum.Is;
            if (string.Compare(lang, "as", true) == 0) return LangCodeEnum.As;
            if (string.Compare(lang, "new", true) == 0) return LangCodeEnum.New;
            return (LangCodeEnum)Enum.Parse(typeof(LangCodeEnum), lang);
        }

        #endregion

        #region URL Builders
        /// <summary>
        /// returns URL to the given page, depends on project settings
        /// </summary>
        public static string NonPrettifiedURL(string title)
        {
            return URLLong + "index.php?title=" + Tools.WikiEncode(title);
        }

        public static string GetArticleHistoryURL(string title)
        {
            return (NonPrettifiedURL(title) + "&action=history");
        }

        public static string GetEditURL(string title)
        {
            return (NonPrettifiedURL(title) + "&action=edit");
        }

        public static string GetAddToWatchlistURL(string title)
        {
            return (NonPrettifiedURL(title) + "&action=watch");
        }

        public static string GetRemoveFromWatchlistURL(string title)
        {
            return (NonPrettifiedURL(title) + "&action=unwatch");
        }

        public static string GetUserTalkURL(string username)
        {
            return URLLong + "index.php?title=User_talk:" + Tools.WikiEncode(username) + "&action=purge";
        }

        public static string GetUserTalkURL()
        {
            return URLLong + "index.php?title=User_talk:" + Tools.WikiEncode(User.Name) + "&action=purge";
        }

        public static string GetPlainTextURL(string title)
        {
            return NonPrettifiedURL(title) + "&action=raw";
        }
        #endregion
    }

    public enum WikiStatusResult { Error, NotLoggedIn, NotRegistered, OldVersion, Registered, Null }

    #region UserProperties

    public class UserProperties
    {
        public UserProperties()
        {
            if (!Globals.UnitTestMode)
            {
                webBrowserLogin = new WebControl();
                webBrowserLogin.ScriptErrorsSuppressed = true;
            }
        }

        /// <summary>
        /// Occurs when user name changes
        /// </summary>
        public event EventHandler UserNameChanged;

        /// <summary>
        /// Occurs when wiki status changes
        /// </summary>
        public event EventHandler WikiStatusChanged;

        /// <summary>
        /// Occurs when bot status changes
        /// </summary>
        public event EventHandler BotStatusChanged;

        /// <summary>
        /// Occurs when admin status changes
        /// </summary>
        public event EventHandler AdminStatusChanged;

        private string strName = "";
        private bool bWikiStatus;
        private bool bIsAdmin;
        private bool bIsBot;
        private bool bLoggedIn;
        private bool bLoaded;

        public List<string> Groups = new List<string>();

        public WebControl webBrowserLogin;
        private static Boolean WeAskedAboutUpdate;

        /// <summary>
        /// Gets the user name
        /// </summary>
        public string Name
        {
            get { return strName; }
            set
            {
                if (strName != value)
                {
                    strName = value;
                    if (UserNameChanged != null)
                        UserNameChanged(null, null);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the user is enabled to use the software
        /// </summary>
        public bool WikiStatus
        {
            get { return bWikiStatus; }
            set
            {
                if (bWikiStatus != value)
                {
                    bWikiStatus = value;
                    if (WikiStatusChanged != null)
                        WikiStatusChanged(null, null);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether user is an admin
        /// </summary>
        public bool IsAdmin
        {
            get { return bIsAdmin; }
            set
            {
                if (bIsAdmin != value)
                {
                    bIsAdmin = value;
                    if (AdminStatusChanged != null)
                        AdminStatusChanged(null, null);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether user is a bot
        /// </summary>
        public bool IsBot
        {
            get { return bIsBot; }
            set
            {
                if (bIsBot != value)
                {
                    bIsBot = value;
                    if (BotStatusChanged != null)
                        BotStatusChanged(null, null);
                }
            }
        }

        /// <summary>
        /// Checks if the user is classed as 'logged in'
        /// </summary>
        public bool LoggedIn
        {
            get { return bLoggedIn; }
            set
            {
                bLoggedIn = value;
                WikiStatus = (bLoggedIn != false);
            }
        }

        static Regex Message = new Regex("<!--[Mm]essage:(.*?)-->", RegexOptions.Compiled);
        static Regex VersionMessage = new Regex("<!--VersionMessage:(.*?)\\|\\|\\|\\|(.*?)-->", RegexOptions.Compiled);
        //static Regex Underscores = new Regex("<!--[Uu]nderscores:(.*?)-->", RegexOptions.Compiled);

        /// <summary>
        /// Matches <head> on right-to-left wikis
        /// </summary>
        static readonly Regex HeadRTL = new Regex("<html [^>]*? dir=\"rtl\">", RegexOptions.Compiled);

        /// <summary>
        /// Checks log in status, registered and version.
        /// </summary>
        public WikiStatusResult UpdateWikiStatus()
        {
            try
            {
                string strText = String.Empty;
                string strVersionPage;

                //this object loads a local checkpage on Wikia
                //it cannot be used to approve users, but it could be used to set some settings
                //such as underscores and pages to ignore
                WebControl webBrowserWikia = null;
                string typoPostfix = "";
                string userGroups;

                Groups.Clear();

                if (Variables.IsWikia)
                {
                    webBrowserWikia = new WebControl();
                    webBrowserWikia.Navigate(Variables.URLLong + "index.php?title=Project:AutoWikiBrowser/CheckPage&action=edit");
                }

                //load version check page
                BackgroundRequest br = new BackgroundRequest();
                br.GetHTML("http://en.wikipedia.org/w/index.php?title=Wikipedia:AutoWikiBrowser/CheckPage/Version&action=raw");

                //load check page
                if (Variables.IsWikia)
                    webBrowserLogin.Navigate("http://www.wikia.com/wiki/index.php?title=Wikia:AutoWikiBrowser/CheckPage&action=edit");
                else if ((Variables.Project == ProjectEnum.wikipedia) && (Variables.LangCode == LangCodeEnum.ar))
                    webBrowserLogin.Navigate("http://ar.wikipedia.org/w/index.php?title=%D9%88%D9%8A%D9%83%D9%8A%D8%A8%D9%8A%D8%AF%D9%8A%D8%A7:%D9%82%D8%A7%D8%A6%D9%85%D8%A9_%D8%A7%D9%84%D9%88%D9%8A%D9%83%D9%8A%D8%A8%D9%8A%D8%AF%D9%8A%D9%88%D9%86_%D8%A7%D9%84%D9%85%D8%B3%D9%85%D9%88%D8%AD_%D9%84%D9%87%D9%85_%D8%A8%D8%A7%D8%B3%D8%AA%D8%AE%D8%AF%D8%A7%D9%85_%D8%A7%D9%84%D8%A3%D9%88%D8%AA%D9%88_%D9%88%D9%8A%D9%83%D9%8A_%D8%A8%D8%B1%D8%A7%D9%88%D8%B2%D8%B1&action=edit");
                else
                    webBrowserLogin.Navigate(Variables.URLLong + "index.php?title=Project:AutoWikiBrowser/CheckPage&action=edit");

                //wait for both pages to load
                webBrowserLogin.Wait();
                strText = webBrowserLogin.GetArticleText();
                br.Wait();

                Variables.RTL = HeadRTL.IsMatch(webBrowserLogin.ToString());

                if (Variables.IsWikia)
                {
                    webBrowserWikia.Wait();
                    try
                    {
                        Variables.LangCode = Variables.ParseLanguage(webBrowserWikia.GetScriptingVar("wgContentLanguage"));
                    }
                    catch
                    {
                        // use English if language not recognized
                        Variables.LangCode = LangCodeEnum.en;
                    }
                    typoPostfix = "-" + Variables.ParseLanguage(webBrowserWikia.GetScriptingVar("wgContentLanguage"));
                    string s = webBrowserWikia.GetArticleText();

                    // selectively add content of the local checkpage to the global one
                    strText += Message.Match(s).Value
                        /*+ Underscores.Match(s).Value*/
                        + WikiRegexes.NoGeneralFixes.Match(s);

                    userGroups = webBrowserWikia.GetScriptingVar("wgUserGroups");
                }
                else
                    userGroups = webBrowserLogin.GetScriptingVar("wgUserGroups");

                bLoaded = true;

                if (Variables.IsCustomProject)
                {
                    try
                    {
                        Variables.LangCode = Variables.ParseLanguage(webBrowserLogin.GetScriptingVar("wgContentLanguage"));
                    }
                    catch
                    {
                        // use English if language not recognized
                        Variables.LangCode = LangCodeEnum.en;
                    }
                }

                strVersionPage = (string)br.Result;

                //see if this version is enabled
                if (!strVersionPage.Contains(AWBVersion + " enabled"))
                {
                    IsBot = IsAdmin = WikiStatus = false;
                    return WikiStatusResult.OldVersion;
                }

                // else
                if (!WeAskedAboutUpdate && strVersionPage.Contains(AWBVersion + " enabled (old)"))
                {
                    WeAskedAboutUpdate = true;
                    if (MessageBox.Show("This version has been superceeded by a new version.  You may continue to use this version or update to the newest version.\r\n\r\nWould you like to automatically upgrade to the newest version?", "Upgrade?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        Match m_version = Regex.Match(strVersionPage, @"<!-- Current version: (.*?) -->");
                        if (m_version.Success && m_version.Groups[1].Value.Length == 4)
                        {
                            System.Diagnostics.Process.Start(Path.GetDirectoryName(Application.ExecutablePath) + "\\AWBUpdater.exe");
                        }
                        else
                            if (MessageBox.Show("Error automatically updating AWB.  Load the download page instead?", "Load download page?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                Tools.OpenURLInBrowser("http://sourceforge.net/project/showfiles.php?group_id=158332");
                            }
                    }
                }

                CheckPageText = strText;

                //AWB does not support any skin other than Monobook
                string skin = webBrowserLogin.GetScriptingVar("skin");
                if (skin == "cologneblue")
                {
                    MessageBox.Show("This software does not support the Cologne Blue skin." +
                        "\r\nPlease choose another skin in your preferences and relogin.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return WikiStatusResult.Null;
                }

                //see if we are logged in
                this.Name = webBrowserLogin.UserName;
                if (string.IsNullOrEmpty(Name)) // don't run GetInLogInStatus if we don't have the username, we sometimes get 2 error message boxes otherwise
                    LoggedIn = false;
                else
                    LoggedIn = webBrowserLogin.GetLogInStatus();

                if (!LoggedIn)
                {
                    IsBot = IsAdmin = WikiStatus = false;
                    return WikiStatusResult.NotLoggedIn;
                }

                // check if username is globally blacklisted
                foreach (Match m3 in Regex.Matches(strVersionPage, @"badname:\s*(.*)\s*(:?|#.*)$", RegexOptions.IgnoreCase))
                {
                    if (!string.IsNullOrEmpty(m3.Groups[1].Value.Trim()) && Regex.IsMatch(this.Name, m3.Groups[1].Value.Trim(), RegexOptions.IgnoreCase | RegexOptions.Multiline))
                        return WikiStatusResult.NotRegistered;
                }

                //see if there is a message
                Match m = Message.Match(strText);
                if (m.Success && m.Groups[1].Value.Trim().Length > 0)
                    MessageBox.Show(m.Groups[1].Value, "Automated message", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //see if there is a version-specific message
                m = VersionMessage.Match(strText);
                if (m.Success && m.Groups[1].Value.Trim().Length > 0 && m.Groups[1].Value == AWBVersion)
                    MessageBox.Show(m.Groups[2].Value, "Automated message", MessageBoxButtons.OK, MessageBoxIcon.Information);

                m = Regex.Match(strText, "<!--[Tt]ypos" + typoPostfix + ":(.*?)-->");
                if (m.Success && m.Groups[1].Value.Trim().Length > 0)
                    Variables.RETFPath = m.Groups[1].Value.Trim();

                //List<string> us = new List<string>();
                //foreach (Match m1 in Underscores.Matches(strText))
                //{
                //    if (m1.Success && m1.Groups[1].Value.Trim().Length > 0)
                //        us.Add(m1.Groups[1].Value.Trim());
                //}
                //if (us.Count > 0) Variables.LoadUnderscores(us.ToArray());

                Regex r = new Regex("\"([a-z]*)\"[,\\]]");

                foreach (Match m1 in r.Matches(userGroups))
                {
                    Groups.Add(m1.Groups[1].Value);
                }

                //don't require approval if checkpage does not exist.
                if (strText.Length < 1)
                {
                    WikiStatus = true;
                    IsBot = true;
                    return WikiStatusResult.Registered;
                }
                else if (strText.Contains("<!--All users enabled-->"))
                {//see if all users enabled
                    WikiStatus = true;
                    IsBot = true;
                    IsAdmin = Groups.Contains("sysop");
                    return WikiStatusResult.Registered;
                }
                else
                {
                    //see if we are allowed to use this softare
                    strText = Tools.StringBetween(strText, "<!--enabledusersbegins-->", "<!--enabledusersends-->");

                    string strBotUsers = Tools.StringBetween(strText, "<!--enabledbots-->", "<!--enabledbotsends-->");
                    string strAdmins = Tools.StringBetween(strText, "<!--adminsbegins-->", "<!--adminsends-->");
                    Regex username = new Regex(@"^\*\s*" + Tools.CaseInsensitive(Variables.User.Name)
                        + @"\s*$", RegexOptions.Multiline);

                    if (Groups.Contains("sysop") || Groups.Contains("staff"))
                    {
                        WikiStatus = IsAdmin = true;
                        IsBot = username.IsMatch(strBotUsers);
                        return WikiStatusResult.Registered;
                    }

                    if (this.Name.Length > 0 && username.IsMatch(strText))
                    {
                        //enable botmode
                        IsBot = username.IsMatch(strBotUsers);

                        //enable admin features
                        IsAdmin = username.IsMatch(strAdmins);

                        WikiStatus = true;

                        return WikiStatusResult.Registered;
                    }
                    else
                    {
                        IsBot = IsAdmin = WikiStatus = false;
                        return WikiStatusResult.NotRegistered;
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.WriteDebug(this.ToString(), ex.Message);
                Tools.WriteDebug(this.ToString(), ex.StackTrace);
                IsBot = IsAdmin = WikiStatus = false;
                return WikiStatusResult.Error;
            }
        }

        public void EnsureLoaded()
        {
            if (!bLoaded) UpdateWikiStatus();
        }


        /// <summary>
        /// Checks if the current version of AWB is enabled
        /// </summary>
        public WikiStatusResult CheckEnabled()
        {
            try
            {
                string strText = Tools.GetHTML("http://en.wikipedia.org/w/index.php?title=Wikipedia:AutoWikiBrowser/CheckPage/Version&action=raw");

                if (strText.Length == 0)
                {
                    Tools.WriteDebug(this.ToString(), "Empty version checkpage");
                    return WikiStatusResult.Error;
                }

                if (!strText.Contains(AWBVersion + " enabled"))
                    return WikiStatusResult.OldVersion;
                else
                    return WikiStatusResult.Null;
            }
            catch (Exception ex)
            {
                Tools.WriteDebug(this.ToString(), ex.Message);
                Tools.WriteDebug(this.ToString(), ex.StackTrace);
                return WikiStatusResult.Error;
            }
        }

        string strCheckPage = "";
        public string CheckPageText
        {
            get { return strCheckPage; }
            private set { strCheckPage = value; }
        }

        private static string AWBVersion
        { get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); } }
    }
    #endregion
}

