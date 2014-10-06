using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ACASLibraries.Web
{
	/// <summary>
	/// Miscellaneous utility functions useful in Web applications
	/// </summary>
	public static class Utility
	{
		/// <summary>
		/// Return a string containing lots of details about a browser. NOTE: If an error occurs inside the method, 
		/// the exception message is returned and the exception is swallowed. This is because this method is typically used for logging.
		/// </summary>
		/// <param name="browser"></param>
		/// <returns></returns>
		public static string GetBrowserInformation(HttpBrowserCapabilities browser)
		{
			StringBuilder sb = new StringBuilder();
			try
			{
				sb.AppendFormat("Type = {0}", browser.Type);
				sb.AppendFormat("; Name = {0}", browser.Browser);
				sb.AppendFormat("; Version = {0}", browser.Version);
				sb.AppendFormat("; Major Version = {0}", browser.MajorVersion);
				sb.AppendFormat("; Minor Version = {0}", browser.MinorVersion);
				sb.AppendFormat("; Platform = {0}", browser.Platform);
				sb.AppendFormat("; Is Beta = {0}", browser.Beta);
				sb.AppendFormat("; Is Crawler = {0}", browser.Crawler);
				sb.AppendFormat("; Is AOL = {0}", browser.AOL);
				sb.AppendFormat("; Is Win16 = {0}", browser.Win16);
				sb.AppendFormat("; Is Win32 = {0}", browser.Win32);
				sb.AppendFormat("; Supports Frames = {0}", browser.Frames);
				sb.AppendFormat("; Supports Tables = {0}", browser.Tables);
				sb.AppendFormat("; Supports Cookies = {0}", browser.Cookies);
				sb.AppendFormat("; Supports VBScript = {0}", browser.VBScript);
				sb.AppendFormat("; Supports JavaScript = {0}", (browser.EcmaScriptVersion != null ? browser.EcmaScriptVersion.ToString() : "null"));
				sb.AppendFormat("; Supports Java Applets = {0}", browser.JavaApplets);
				sb.AppendFormat("; Supports ActiveX Controls = {0}", browser.ActiveXControls);
				sb.AppendFormat("; Supports JavaScript Version = {0}", browser["JavaScriptVersion"]);
			}
			catch (Exception ex)
			{
				//typically not worth stopping code execution for this error, we'll just return the error in the result
				sb.Append("Eror parsing browser info: ");
				sb.Append(ex.Message);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Return a string containing lots of details about a browser. NOTE: If an error occurs inside the method, 
		/// the exception message is returned and the exception is swallowed. This is because this method is typically used for logging.
		/// </summary>
		/// <param name="browser"></param>
		/// <returns></returns>
		public static string GetBrowserInformation(HttpBrowserCapabilitiesBase browser)
		{
			StringBuilder sb = new StringBuilder();
			try
			{
				sb.AppendFormat("Type = {0}", browser.Type);
				sb.AppendFormat("; Name = {0}", browser.Browser);
				sb.AppendFormat("; Version = {0}", browser.Version);
				sb.AppendFormat("; Major Version = {0}", browser.MajorVersion);
				sb.AppendFormat("; Minor Version = {0}", browser.MinorVersion);
				sb.AppendFormat("; Platform = {0}", browser.Platform);
				sb.AppendFormat("; Is Beta = {0}", browser.Beta);
				sb.AppendFormat("; Is Crawler = {0}", browser.Crawler);
				sb.AppendFormat("; Is AOL = {0}", browser.AOL);
				sb.AppendFormat("; Is Win16 = {0}", browser.Win16);
				sb.AppendFormat("; Is Win32 = {0}", browser.Win32);
				sb.AppendFormat("; Supports Frames = {0}", browser.Frames);
				sb.AppendFormat("; Supports Tables = {0}", browser.Tables);
				sb.AppendFormat("; Supports Cookies = {0}", browser.Cookies);
				sb.AppendFormat("; Supports VBScript = {0}", browser.VBScript);
				sb.AppendFormat("; Supports JavaScript = {0}", (browser.EcmaScriptVersion != null ? browser.EcmaScriptVersion.ToString() : "null"));
				sb.AppendFormat("; Supports Java Applets = {0}", browser.JavaApplets);
				sb.AppendFormat("; Supports ActiveX Controls = {0}", browser.ActiveXControls);
				sb.AppendFormat("; Supports JavaScript Version = {0}", browser["JavaScriptVersion"]);
			}
			catch (Exception ex)
			{
				//typically not worth stopping code execution for this error, we'll just return the error in the result
				sb.Append("Eror parsing browser info: ");
				sb.Append(ex.Message);
			}
			return sb.ToString();
		}		
	}
}
