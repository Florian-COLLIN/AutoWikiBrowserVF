﻿/*
Copyright (C) 2008 Max Semenik

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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace WikiFunctions.API
{
    /// <summary>
    /// Base class for all API-related exceptions
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>
        /// The ApiEdit object that threw the exception
        /// </summary>
        public ApiEdit Editor { get; private set; }

        /// <summary>
        /// Thread in which the exception was thrown
        /// </summary>
        public Thread ThrowingThread { get; private set; }

        public ApiException(ApiEdit editor, string message)
            : base(message)
        {
            Editor = editor;
            ThrowingThread = Thread.CurrentThread;
        }

        public ApiException(ApiEdit editor, string message, Exception innerException)
            : base(message, innerException)
        {
            Editor = editor;
            ThrowingThread = Thread.CurrentThread;
        }

        public virtual string GetExtraSpecificInformation()
        {
            return "";
        }
    }

    /// <summary>
    /// Thrown when Abort() function is called, or request is otherwise abruptly terminated
    /// </summary>
    public class AbortedException : ApiException
    {
        public AbortedException(ApiEdit editor)
            : base(editor, "API operation aborted")
        {
        }
    }

    /// <summary>
    /// Thrown when an API call returned an &lt;error> tag.
    /// See https://www.mediawiki.org/wiki/API:Errors for details
    /// </summary>
    public class ApiErrorException : ApiException
    {
        private static readonly Regex ExtractErrorVariable = new Regex("``(.*?)''", RegexOptions.Compiled);

        /// <summary>
        /// Short error code
        /// </summary>
        public string ErrorCode { get; private set; }

        /// <summary>
        /// Error message returned by API
        /// </summary>
        public string ApiErrorMessage { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetErrorVariable()
        {
            return ExtractErrorVariable.Match(ApiErrorMessage).Groups[1].Value;
        }

        public ApiErrorException(ApiEdit editor, string errorCode, string errorMessage)
            : base(editor, "Bot API returned the following error: '" + errorMessage + "'")
        {
            ErrorCode = errorCode;
            ApiErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Thrown when a feature in the Api is disabled
    /// </summary>
    public class FeatureDisabledException : ApiErrorException
    {
        public FeatureDisabledException(ApiEdit editor, string errorCode, string errorMessage)
            : base(editor, errorCode, errorMessage)
        {
            DisabledFeature = errorCode.Replace("-disabled", "");
        }

        public string DisabledFeature { get; private set; }
    }

    /// <summary>
    /// Thrown when an operation is ended with result other than "Success"
    /// </summary>
    public class OperationFailedException : ApiException
    {
        public OperationFailedException(ApiEdit editor, string action, string result, string xml)
            : base(editor, "Operation '" + action + "' ended with result '" + result + "'.\r\nXml: '" + xml + "'")
        {
            Action = action;
            Result = result;
            Xml = xml;
        }

        public readonly string Action, Result, Xml;
    }

    /// <summary>
    /// Thrown when an API call returns a zero-size reply. Most likely, this indicates a server internal error.
    /// </summary>
    public class ApiBlankException : ApiException
    {
        public ApiBlankException(ApiEdit editor)
            : base(editor, "The result returned by server was blank")
        {
        }
    }

    /// <summary>
    /// Thrown when an invalid XML output is encountered, or if XML parsing results in error
    /// </summary>
    public class BrokenXmlException : ApiException
    {
        public BrokenXmlException(ApiEdit editor, string message)
            : base(editor, message)
        {
        }

        public BrokenXmlException(ApiEdit editor, string message, Exception innerException)
            : base(editor, message, innerException)
        {
        }

        public BrokenXmlException(ApiEdit editor, Exception innerException)
            : base(editor, "Error parsing data returned by server: " + innerException.Message, innerException)
        {
        }
    }

    /// <summary>
    /// Thrown when logging in failed
    /// </summary>
    public class LoginException : ApiException
    {
        public string StatusCode { get; private set; }

        public LoginException(ApiEdit editor, string status)
            : base(editor, GetErrorMessage(status))
        {
            StatusCode = status;
        }

        protected static string GetErrorMessage(string code)
        {
            switch (code.ToLower())
            {
                case "noname":
                    return "You didn't specify your username";
                case "illegal":
                    return "You provided an illegal username";
                case "notexists":
                    return "The username you provided doesn't exist";
                case "emptypass":
                    return "You must specify your password to log in";
                case "wrongpass":
                    return "The password you provided is incorrect";
                case "wrongpluginpass":
                    return
                        "The password you provided is incorrect. (an authentication plugin rather than MediaWiki itself rejected the password)";
                case "createblocked":
                    return
                        "The wiki tried to automatically create a new account for you, but your IP address has been blocked from account creation";
                case "throttled":
                    return "You've logged in too many times in a short time.";
                        //see https://www.mediawiki.org/wiki/API:Login#Throttling
                case "blocked":
                    return "User is blocked";
                default:
                    return code;
            }
        }
    }

    /// <summary>
    /// Thrown when servers refuse to perform operation due to overloading
    /// </summary>
    /// <remarks>https://www.mediawiki.org/wiki/Manual:Maxlag_parameter</remarks>
    public class MaxlagException : ApiErrorException
    {
        public int Maxlag { get; private set; }

        public int RetryAfter { get; private set; }

        public MaxlagException(ApiEdit editor, int maxlag, int retryAfter)
            : base(editor, "maxlag", "Maxlag exceeded by " + maxlag + " seconds, retry in " + retryAfter + " seconds")
        {
            Maxlag = maxlag;
            RetryAfter = retryAfter;
        }
    }

    /// <summary>
    /// Thrown when assertion in API call fails
    /// </summary>
    public class AssertionFailedException : ApiException
    {
        public AssertionFailedException(ApiEdit editor, string assertion)
            : base(editor, "Assertion '" + assertion + "' failed")
        {
        }
    }

    /// <summary>
    /// Thrown when an error occurs during asynchronous API operations
    /// </summary>
    [Serializable]
    public class InvocationException : Exception
    {
        public InvocationException(string message)
            : base(message)
        {
        }

        public InvocationException(Exception innerException)
            : this("There was a problem with an asynchronous API call", innerException)
        {
        }

        public InvocationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Thrown when edit is blocked by SpamBlacklist extension
    /// </summary>
    public class SpamlistException : ApiException
    {
        /// <summary>
        /// URL which triggered the blacklist
        /// </summary>
        public string URL { get; private set; }

        public SpamlistException(ApiEdit editor, string url)
            : base(editor, "The link '" + url + "' is blocked by spam blacklist")
        {
            URL = url;
        }
    }

    /// <summary>
    /// Thrown when you try to move a file onto a target that exists in a shared repo (e.g. Commons)
    /// </summary>
    public class SharedRepoException : ApiException
    {
        public SharedRepoException(ApiEdit editor, string message)
            : base(editor, message)
        {
        }
    }

    /// <summary>
    /// Thrown when attempted operation requires login
    /// </summary>
    public class LoggedOffException : ApiException
    {
        public LoggedOffException(ApiEdit editor)
            : base(editor, "User is logged off")
        {
        }
    }

    /// <summary>
    /// Perfect from when MediaWiki just says no
    /// Such as hook aborted "The modification you tried to make was aborted by an extension hook";
    /// Thanks AbuseFilter
    /// </summary>
    public class MediaWikiSaysNoException : ApiException
    {
        public MediaWikiSaysNoException(ApiEdit editor, string message)
            : base(editor, message)
        {
        }
    }

    /// <summary>
    /// Thrown when server requests to solve a captcha.
    /// Note: we currently don't support captchas in any way other than stopping and reporting.
    /// </summary>
    public class CaptchaException : ApiException
    {
        public CaptchaException(ApiEdit editor)
            : base(editor, "Captcha required")
        {
        }
    }

    /// <summary>
    /// Thrown when page name provided for an API operation contains interwiki prefix
    /// </summary>
    public class InterwikiException : ApiException
    {
        public InterwikiException(ApiEdit editor)
            : base(editor, "Page title contains interwiki")
        {
        }

        protected InterwikiException(ApiEdit editor, string message)
            : base(editor, message)
        {
        }
    }

    /// <summary>
    /// Thrown when a title is a redirect to an interwiki
    /// </summary>
    public class RedirectToInterwikiException : InterwikiException
    {
        public RedirectToInterwikiException(ApiEdit editor)
            : base(editor, "Redirect target contains interwiki")
        {
        }
    }

    /// <summary>
    /// Thrown when current user has unread talk page messages
    /// </summary>
    public class NewMessagesException : ApiException
    {
        public NewMessagesException(ApiEdit editor)
            : base(editor, "You have new messages")
        {
        }
    }

    /// <summary>
    /// Thrown when the API returns a title is invalid
    /// </summary>
    public class InvalidTitleException : ApiException
    {
        public string InvalidTitle { get; private set; }

        public InvalidTitleException(ApiEdit editor, string title)
            : base(editor, "Invalid title: \"" + title + "\"")
        {
            InvalidTitle = title;
        }
    }

    /// <summary>
    /// Used by ApiEdit.CheckErrors() when we have XML related errors
    /// (Wrapper for XmlException)
    /// </summary>
    public class ApiXmlException : ApiException
    {
        public ApiXmlException(ApiEdit editor, XmlException innerException, string get, string post, string content)
            : base(editor, "XmlException in ApiEdit.CheckForErrors", innerException)
        {
            GetUrl = get;
            PostQuery = post;
            Content = content;
        }

        /// <summary>
        /// GET query string
        /// </summary>
        public string GetUrl { get; private set; }

        /// <summary>
        /// POST query string
        /// </summary>
        public string PostQuery { get; private set; }

        /// <summary>
        /// The XML Content that made this exception be thrown
        /// </summary>
        public string Content { get; private set; }

        public override string GetExtraSpecificInformation()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("<table>");
            builder.AppendFormat("<tr><td>Get</td><td>{0}</td></tr>", GetUrl);
            if (!string.IsNullOrEmpty(PostQuery))
            {
                builder.AppendFormat("<tr><td>Post</td><td>{0}</td></tr>", PostQuery);
            }
#if DEBUG5
            if (!string.IsNullOrEmpty(Content))
            {
                builder.AppendFormat("<tr><td>Content</td><td>{0}</td></tr>", Content);
            }
#endif
            builder.AppendLine("</table>");
            return builder.ToString();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Part of workaround for https://bugzilla.wikimedia.org/show_bug.cgi?id=39492
    /// </remarks>
    public class RedirectToSpecialPageException : ApiException
    {
        public RedirectToSpecialPageException(ApiEdit editor)
            : base(editor, "Redirect target is special page")
        {
        }
    }
}
