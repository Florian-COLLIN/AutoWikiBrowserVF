﻿/*
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

namespace WikiFunctions.Plugin
{
    /// <summary>
    /// 
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// Runs custom module code against the current page
        /// </summary>
        /// <param name="articleText">The wiki text of the page</param>
        /// <param name="articleTitle">Title of the page</param>
        /// <param name="namespace">Namespace key of page</param>
        /// <param name="summary">Edit summary to use</param>
        /// <param name="skip">Whether to skip the page</param>
        /// <returns>The updated page text</returns>
        string ProcessArticle(string articleText, string articleTitle, int @namespace, out string summary,
            out bool skip);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface ISkipOptions
    {
        /// <summary>
        /// 
        /// </summary>
        bool SkipNoUnicode
        { get; }

        /// <summary>
        /// 
        /// </summary>
        bool SkipNoTag
        { get; }

        /// <summary>
        /// 
        /// </summary>
        bool SkipNoHeaderError
        { get; }

        /// <summary>
        /// 
        /// </summary>
        bool SkipNoBoldTitle
        { get; }

        /// <summary>
        /// 
        /// </summary>
        bool SkipNoBulletedLink
        { get; }

        /// <summary>
        /// 
        /// </summary>
        bool SkipNoBadLink
        { get; }

        /// <summary>
        /// 
        /// </summary>
        bool SkipNoDefaultSortAdded
        { get; }

        /// <summary>
        /// 
        /// </summary>
        bool SkipNoUserTalkTemplatesSubstd
        { get; }

        /// <summary>
        /// 
        /// </summary>
        bool SkipNoCiteTemplateDatesFixed
        { get; }

        /// <summary>
        /// 
        /// </summary>
        bool SkipNoPeopleCategoriesFixed
        { get; }
    }
}
