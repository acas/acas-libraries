namespace ACASLibraries
{
	using System;
	using System.Configuration;
	using System.IO; // for StreamWriter
	using System.Web;
	using System.Text;
	using System.Diagnostics;
	using System.Data.SqlClient; // for SQLException
	using System.Xml; // for Xml exceptions
	using System.Xml.Xsl; // for Xslt exceptions

	/// <summary>
	/// Logs time-stamped entry (particularly errors) into a  file specified in the web.config AppSettings
	/// External Dependency: AppSettings["DefaultLogFilename"], AppSettings["DefaultLogFileLocation"] (ending with a slash)
	/// Currently there is no way to override the file or path name, the reason for this is to keep the
	///  all the methods static, thus avoided the need for object instantiation.
	/// </summary>
	public class Logger
	{
		// returns the log filename to which the Logger is using
		public static string FilenameIncludingPath
		{
			get
			{
				return (Parser.ToString(ConfigurationManager.AppSettings["DefaultLogFileLocation"]) + IOUtility.GetFilenamePlusPathWithoutExtension(Parser.ToString(ConfigurationManager.AppSettings["DefaultLogFilename"])) + " " + Utility.GetSortableDateStamp() + Path.GetExtension(Parser.ToString(ConfigurationManager.AppSettings["DefaultLogFilename"])));
			}
		}

		#region WriteToLogFile();
		// writes the provided string to the file specified by
		// ConfigurationManager.AppSettings["ErrorLogPath"].ToString() + ConfigurationManager.AppSettings["DefaultErrorFilename"].ToString();
		private static bool WriteToLogFile(string sStringToWrite, string sLogFilenameIncludingExtension, string sLogFileLocationIncludingSlash)
		{
			try
			{
				string sFilenameIncludingPath = sLogFileLocationIncludingSlash + IOUtility.GetFilenamePlusPathWithoutExtension(sLogFilenameIncludingExtension) + " " + Utility.GetSortableDateStamp() + Path.GetExtension(sLogFilenameIncludingExtension);
				StreamWriter sw = new StreamWriter(sFilenameIncludingPath, true);
				if (sw != null)
				{
					sw.WriteLine(sStringToWrite);
					sw.Flush();
					sw.Close();
					sw = null;
					return true;
				}
				else
				{
					return false;
				}
			}
			catch
			{
				return false;
			}
		}
		private static bool WriteToLogFile(string sStringToWrite, string sLogFilenameIncludingExtension)
		{
			return WriteToLogFile(sStringToWrite, sLogFilenameIncludingExtension, Parser.ToString(ConfigurationManager.AppSettings["DefaultLogFileLocation"]));
		}
		private static bool WriteToLogFile(string sStringToWrite)
		{
			return WriteToLogFile(sStringToWrite, Parser.ToString(ConfigurationManager.AppSettings["DefaultLogFilename"]), Parser.ToString(ConfigurationManager.AppSettings["DefaultLogFileLocation"]));
		}
		#endregion

		#region GetDateString();
		private static string GetDateString()
		{
			return DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString();
		}
		#endregion

		#region GetLogMessage();
		private static string GetLogMessage(string sIndent, string sLineDelimiter, string sErrorCode, string sShortDescription, string sAdditionalInfo, object oException)
		{
			StringBuilder sbErrorMessage = new StringBuilder();
			try
			{
				if(sIndent == null)
				{
					sIndent = "     ";
				}
				if(sLineDelimiter == null)
				{
					sLineDelimiter = "\r\n";
				}
				sbErrorMessage.Append(GetDateString() + " ==> ERROR CODE  : " + ((sErrorCode != null && sErrorCode.Length > 0) ? sErrorCode : "N/A") + sLineDelimiter);
				sbErrorMessage.Append(sIndent + "SHORT DESC  : " + ((sShortDescription != null && sShortDescription.Length > 0) ? sShortDescription : "N/A") + sLineDelimiter);
				sbErrorMessage.Append(sIndent + "TECH MSG    : " + ((sAdditionalInfo != null && sAdditionalInfo.Length > 0) ? sAdditionalInfo : "N/A") + sLineDelimiter);
				if(HttpContext.Current != null)
				{
					sbErrorMessage.Append(sIndent + "REQUEST URL : " + HttpContext.Current.Request.RawUrl + sLineDelimiter);
					//sbErrorMessage.Append(sIndent + "USERNAME  : " + UserManager.UserName + sLineDelimiter);
					sbErrorMessage.Append(sIndent + "METHOD      : " + HttpContext.Current.Request.HttpMethod + sLineDelimiter);
					sbErrorMessage.Append(sIndent + "FORM ARGS   : " + HttpContext.Current.Request.Form.Count.ToString() + sLineDelimiter);
					if(HttpContext.Current.Request.Form.Count > 0)
					{
						for(int iFormKeyIndex = 0;iFormKeyIndex < HttpContext.Current.Request.Form.Count;iFormKeyIndex++)
						{
							if(HttpContext.Current.Request.Form[iFormKeyIndex] != null)
							{
								if(HttpContext.Current.Request.Form.Keys[iFormKeyIndex] != null)
								{
									sbErrorMessage.Append(sIndent + sIndent + HttpContext.Current.Request.Form.Keys[iFormKeyIndex] + "=" + HttpContext.Current.Request.Form[iFormKeyIndex] + sLineDelimiter);
								}
								else
								{
									sbErrorMessage.Append(sIndent + sIndent + "{POSTDATA}=" + HttpContext.Current.Request.Form[iFormKeyIndex] + sLineDelimiter);
								}
							}
						}
					}
					sbErrorMessage.Append(sIndent + "COOKIES     : " + HttpContext.Current.Request.Cookies.Keys.Count.ToString() + sLineDelimiter);
					if(HttpContext.Current.Request.Cookies.Keys.Count > 0)
					{
						for(int iCookieKeyIndex = 0;iCookieKeyIndex < HttpContext.Current.Request.Cookies.Keys.Count;iCookieKeyIndex++)
						{
							if(HttpContext.Current.Request.Cookies[iCookieKeyIndex] != null)
							{
								sbErrorMessage.Append(sIndent + sIndent + HttpContext.Current.Request.Cookies.Keys.Get(iCookieKeyIndex).ToString() + "=" + HttpContext.Current.Request.Cookies[iCookieKeyIndex].Value.ToString() + sLineDelimiter);
							}
						}
					}
				}
				if (oException == null)
				{
					oException = HttpContext.Current.Server.GetLastError();
				}
				if (oException == null)
				{
					sbErrorMessage.Append(sIndent + "EXCEPTION   : N/A" + sLineDelimiter);
				}
				else
				{
					sbErrorMessage.Append(sIndent + "EXP TYPE    : " + oException.GetType().ToString() + sLineDelimiter);
					sbErrorMessage.Append(sIndent + "EXP MESSAGE : " + ((Exception)oException).Message.Replace("\r\n", sLineDelimiter + sIndent + sIndent + sIndent) + sLineDelimiter);
					sbErrorMessage.Append(sIndent + "EXP SOURCE  : " + ((Exception)oException).Source + sLineDelimiter);
					sbErrorMessage.Append(sIndent + "EXP INNER   : " + ((Exception)oException).InnerException + sLineDelimiter);
					sbErrorMessage.Append(sIndent + "EXP STACK   : " + ((Exception)oException).StackTrace + sLineDelimiter);
					if(oException.GetType() == typeof(SqlException))
					{
						sbErrorMessage.Append(sIndent + "EXP LINE    : " + ((SqlException)oException).LineNumber + sLineDelimiter);
						sbErrorMessage.Append(sIndent + "EXP PROC    : " + ((SqlException)oException).Procedure + sLineDelimiter);
						sbErrorMessage.Append(sIndent + "EXP SERVER  : " + ((SqlException)oException).Server + sLineDelimiter);
						sbErrorMessage.Append(sIndent + "EXP STATE   : " + ((SqlException)oException).State + sLineDelimiter);
					}
					if(oException.GetType() == typeof(XmlException))
					{
						sbErrorMessage.Append(sIndent + "EXP LINE    : " + ((XmlException)oException).LineNumber + sLineDelimiter);
						sbErrorMessage.Append(sIndent + "EXP POSITION: " + ((XmlException)oException).LinePosition + sLineDelimiter);
					}
					if(oException.GetType() == typeof(XsltException))
					{
						sbErrorMessage.Append(sIndent + "EXP LINE    : " + ((XsltException)oException).LineNumber + sLineDelimiter);
						sbErrorMessage.Append(sIndent + "EXP POSITION: " + ((XsltException)oException).LinePosition + sLineDelimiter);
						sbErrorMessage.Append(sIndent + "EXP SRC URI : " + Parser.ToString(((XsltException)oException).SourceUri) + sLineDelimiter);
					}
					if(oException.GetType() == typeof(XsltCompileException))
					{
						sbErrorMessage.Append(sIndent + "EXP LINE    : " + ((XsltCompileException)oException).LineNumber + sLineDelimiter);
						sbErrorMessage.Append(sIndent + "EXP POSITION: " + ((XsltCompileException)oException).LinePosition + sLineDelimiter);
						sbErrorMessage.Append(sIndent + "EXP SRC URI : " + Parser.ToString(((XsltCompileException)oException).SourceUri) + sLineDelimiter);
					}
				}
				if(HttpContext.Current != null)
				{
					sbErrorMessage.Append(sIndent + "REQUEST IP  : " + HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"] + sLineDelimiter);
					if(HttpContext.Current.Request.ServerVariables["LOGON_USER"] != null && HttpContext.Current.Request.ServerVariables["LOGON_USER"].Length > 0)
					{
						sbErrorMessage.Append(sIndent + "REQUEST USR : " + HttpContext.Current.Request.ServerVariables["LOGON_USER"] + sLineDelimiter);
					}
					else
					{
						sbErrorMessage.Append(sIndent + "REQUEST USR : " + "[could not obtain]" + sLineDelimiter);
					}
					sbErrorMessage.Append(sIndent + "HTTP REFERER: " + HttpContext.Current.Request.ServerVariables["HTTP_REFERER"] + sLineDelimiter);
					//sbErrorMessage.Append(sIndent + "RAW HEADER: " + (HttpContext.Current.Request.ServerVariables["ALL_RAW"]).TrimEnd(("\r\n").ToCharArray()).Replace("\r\n", sLineDelimiter + sIndent + sIndent) + sLineDelimiter);
					if(HttpContext.Current.Session != null)
					{
						sbErrorMessage.Append(sIndent + "SESSION     : " + HttpContext.Current.Session.Count.ToString() + sLineDelimiter);
						if(HttpContext.Current.Session.Keys.Count > 0)
						{
							for(int iSessionKeyIndex = 0;iSessionKeyIndex < HttpContext.Current.Session.Keys.Count;iSessionKeyIndex++)
							{
								if(HttpContext.Current.Session[iSessionKeyIndex] != null)
								{
									sbErrorMessage.Append(sIndent + sIndent + HttpContext.Current.Session.Keys[iSessionKeyIndex] + "=" + HttpContext.Current.Session[iSessionKeyIndex].ToString() + sLineDelimiter);
								}
							}
						}
					}
					else
					{
						sbErrorMessage.Append(sIndent + "SESSION     : N/A" + sLineDelimiter);
					}
				}
			}
			catch(Exception oLocalException)
			{
				LogCustomMessage("Problem in GetLogMessage()\r\n     EXP TYPE    : "+oLocalException.GetType().ToString() + 
									"\r\n     EXP MESSAGE : "+oLocalException.Message + 
									"\r\n     EXP SOURCE  : "+oLocalException.Source + 
									"\r\n     EXP INNER   : "+oLocalException.InnerException + 
									"\r\n     EXP STACK   : "+oLocalException.StackTrace);
			}
			string sErrorMessage = sbErrorMessage.ToString();
			sbErrorMessage = null;
			return sErrorMessage;
		}
		#endregion

		#region LogCustomMessage();
		/// <summary>
		///	Logs the provided message with a time-stamp to the file provided in the AppSettings
		/// </summary>
		/// <param name="sCustomMessage">The message to be entered in the file.</param>
		/// <returns>'true' when the entry is successfully written to the file, otherwise 'false.'</returns>
		/// <example>
		/// <code>
		/// LogCustomMessage("Include this text in log-file entry.") -> bool
		/// </code>
		/// </example>
		public static bool LogCustomMessage(string sCustomMessage)
		{
			return WriteToLogFile(GetDateString() + " ==> " + sCustomMessage + "\r\n");
		}
		#endregion

		#region LogError();
		/// <summary>
		///	Logs the provided error message to the file provided in the AppSettings
		///  log entry will include: time-stamp, system description information, form variables, querystring variables, session variables, user cookies and a stack trace, along with the information provided by the caller
		/// </summary>
		/// <param name="sErrorCode">Error key or code, it would help if it was unique so that when you see Error #38.4.6, you can do a search and go right to the offending code.  Always hard-coded, never computed.</param>
		/// <param name="sShortDescription">The location of the probably and/or a quick description, it will probably help to include the page/class/module name, function/section name, and condition within the function/section.</param>
		/// <param name="sAdditionalInfo">Anything else that might be helpful such as global variables and states, class members values, loop counters or values, query strings/stored procedure data...</param>
		/// <param name="oException">If the error is caught using a try catch statement, pass the exception in.</param>
		/// <returns>'true' when the entry is successfully written to the file, otherwise 'false.'</returns>
		/// <example>
		/// <code>
		/// Logger.LogError("38.4.6", "ComparableTransactions - Page_Load - update", "sQuery=" + sQuery + "; iArrayIndexCounter=" + iArrayIndexCounter.ToString(), oException);
		/// </code>
		/// </example>
		public static bool LogError(string sErrorCode, string sShortDescription, string sAdditionalInfo, object oException)
		{
			const string c_sIndent = "     ";
			const string c_sLineDelimiter = "\r\n";
			return WriteToLogFile(GetLogMessage(c_sIndent, c_sLineDelimiter, sErrorCode, sShortDescription, sAdditionalInfo, oException) + c_sLineDelimiter);
		}
		/// <summary>
		///	Logs the provided error message to the file provided in the AppSettings
		///  log entry will include: time-stamp, system description information, form variables, querystring variables, session variables, user cookies and a stack trace, along with the information provided by the caller
		/// </summary>
		/// <param name="sErrorCode">Error key or code, it would help if it was unique so that when you see Error #38.4.6, you can do a search and go right to the offending code.  Always hard-coded, never computed.</param>
		/// <param name="sShortDescription">The location of the probably and/or a quick description, it will probably help to include the page/class/module name, function/section name, and condition within the function/section.</param>
		/// <param name="sAdditionalInfo">Anything else that might be helpful such as global variables and states, class members values, loop counters or values, query strings/stored procedure data...</param>
		/// <returns>'true' when the entry is successfully written to the file, otherwise 'false.'</returns>
		/// <example>
		/// <code>
		/// Logger.LogError("38.4.6", "ComparableTransactions - Page_Load - update", "sQuery=" + sQuery + "; iArrayIndexCounter=" + iArrayIndexCounter.ToString(), oException);
		/// </code>
		/// </example>
		public static bool LogError(string sErrorCode, string sShortDescription, string sAdditionalInfo)
		{
			return LogError(sErrorCode, sShortDescription, sAdditionalInfo, null);
		}
		/// <summary>
		///	Logs the provided error message to the file provided in the AppSettings
		///  log entry will include: time-stamp, system description information, form variables, querystring variables, session variables, user cookies and a stack trace, along with the information provided by the caller
		/// </summary>
		/// <param name="sErrorCode">Error key or code, it would help if it was unique so that when you see Error #38.4.6, you can do a search and go right to the offending code.  Always hard-coded, never computed.</param>
		/// <param name="sShortDescription">The location of the probably and/or a quick description, it will probably help to include the page/class/module name, function/section name, and condition within the function/section.</param>
		/// <param name="oException">If the error is caught using a try catch statement, pass the exception in.</param>
		/// <returns>'true' when the entry is successfully written to the file, otherwise 'false.'</returns>
		/// <example>
		/// <code>
		/// Logger.LogError("38.4.6", "ComparableTransactions - Page_Load - update", "sQuery=" + sQuery + "; iArrayIndexCounter=" + iArrayIndexCounter.ToString(), oException);
		/// </code>
		/// </example>
		public static bool LogError(string sErrorCode, string sShortDescription, object oException)
		{
			return LogError(sErrorCode, sShortDescription, "", oException);
		}
		/// <summary>
		///	Logs the provided error message to the file provided in the AppSettings
		///  log entry will include: time-stamp, system description information, form variables, querystring variables, session variables, user cookies and a stack trace, along with the information provided by the caller
		/// </summary>
		/// <param name="sErrorCode">Error key or code, it would help if it was unique so that when you see Error #38.4.6, you can do a search and go right to the offending code.  Always hard-coded, never computed.</param>
		/// <param name="sShortDescription">The location of the probably and/or a quick description, it will probably help to include the page/class/module name, function/section name, and condition within the function/section.</param>
		/// <returns>'true' when the entry is successfully written to the file, otherwise 'false.'</returns>
		/// <example>
		/// <code>
		/// Logger.LogError("38.4.6", "ComparableTransactions - Page_Load - update", "sQuery=" + sQuery + "; iArrayIndexCounter=" + iArrayIndexCounter.ToString(), oException);
		/// </code>
		/// </example>
		public static bool LogError(string sErrorCode, string sShortDescription)
		{
			return LogError(sErrorCode, sShortDescription, "", null);
		}
		/// <summary>
		///	Logs the provided error message to the file provided in the AppSettings
		///  log entry will include: time-stamp, system description information, form variables, querystring variables, session variables, user cookies and a stack trace, along with the information provided by the caller
		/// </summary>
		/// <param name="sShortDescription">The location of the probably and/or a quick description, it will probably help to include the page/class/module name, function/section name, and condition within the function/section.</param>
		/// <param name="oException">If the error is caught using a try catch statement, pass the exception in.</param>
		/// <returns>'true' when the entry is successfully written to the file, otherwise 'false.'</returns>
		/// <example>
		/// <code>
		/// Logger.LogError("38.4.6", "ComparableTransactions - Page_Load - update", "sQuery=" + sQuery + "; iArrayIndexCounter=" + iArrayIndexCounter.ToString(), oException);
		/// </code>
		/// </example>
		public static bool LogError(string sShortDescription, object oException)
		{
			return LogError("", sShortDescription, "", oException);
		}
		/// <summary>
		///	Logs the provided error message to the file provided in the AppSettings
		///  log entry will include: time-stamp, system description information, form variables, querystring variables, session variables, user cookies and a stack trace, along with the information provided by the caller
		/// </summary>
		/// <param name="sShortDescription">The location of the probably and/or a quick description, it will probably help to include the page/class/module name, function/section name, and condition within the function/section.</param>
		/// <returns>'true' when the entry is successfully written to the file, otherwise 'false.'</returns>
		/// <example>
		/// <code>
		/// Logger.LogError("38.4.6", "ComparableTransactions - Page_Load - update", "sQuery=" + sQuery + "; iArrayIndexCounter=" + iArrayIndexCounter.ToString(), oException);
		/// </code>
		/// </example>
		public static bool LogError(string sShortDescription)
		{
			return LogError("", sShortDescription, "", null);
		}
		/// <summary>
		///	Logs the provided error message to the file provided in the AppSettings
		///  log entry will include: time-stamp, system description information, form variables, querystring variables, session variables, user cookies and a stack trace, along with the information provided by the caller
		/// </summary>
		/// <param name="oException">If the error is caught using a try catch statement, pass the exception in.</param>
		/// <returns>'true' when the entry is successfully written to the file, otherwise 'false.'</returns>
		/// <example>
		/// <code>
		/// Logger.LogError("38.4.6", "ComparableTransactions - Page_Load - update", "sQuery=" + sQuery + "; iArrayIndexCounter=" + iArrayIndexCounter.ToString(), oException);
		/// </code>
		/// </example>
		public static bool LogError(object oException)
		{
			return LogError("", "", "", oException);
		}
		#endregion
	}
}
