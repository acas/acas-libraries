using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Web;

namespace ACASLibraries
{
	#region TraceOutput
	/// <summary>
	/// Enumeration of destinations for trace messages.
	/// </summary>
	public enum TraceOutput
	{
		/// <summary>
		/// Undefined
		/// </summary>
		Undefined = 1,
		/// <summary>
		/// System.Web.HttpContext.Current.Trace
		/// </summary>
		HttpContext = 2,
		/// <summary>
		/// System.Diagnostics.Trace
		/// </summary>
		Diagnostics = 4,
		/// <summary>
		/// System.Console
		/// </summary>
		Console = 8
	}
	#endregion

	#region Trace
	/// <summary>
	/// Provides access to the Trace for the current application and is environment independant (web applications, console applications, windows forms).
	/// &lt;p&gt;For console applications, add TraceEnabled = true to appSettings section of app.config&lt;/p&gt;
	/// </summary>
	public static class Trace
	{
		/// <summary>
		/// The current output used for trace messages
		/// </summary>
		public static TraceOutput Output = TraceOutput.Undefined;
		/// <summary>
		/// Internal (private) designating if Trace messages should be written
		/// </summary>
		private static bool bIsEnabled = false;
		/// <summary>
		///	Internal (private) designating if SetTraceOutput() has been called; executed upon first call to IsEnabled property
		/// </summary>
		private static bool bTraceOutputSet = false;

		#region IsEnabled
		/// <summary>
		/// Gets or sets a value indicating wether tracing is enabled.  Setting this value is only valid within web applications.
		/// </summary>
		public static bool IsEnabled
		{
			get
			{
				if(!bTraceOutputSet)
				{
					SetTraceOutput();
				}
				return bIsEnabled;
			}
			set
			{
				if(bTraceOutputSet == false && value == true)
				{
					SetTraceOutput();
				}
				if(Output != TraceOutput.Undefined)
					bIsEnabled = value;
				else
					bIsEnabled = false;
			}
		}
		#endregion

		#region SetTraceOutput();
		/// <summary>
		/// Determines the destination for trace messages to be written to.
		/// </summary>
		/// <returns>The current TraceOutput destination.</returns>
		public static TraceOutput SetTraceOutput()
		{
			if(HttpContext.Current != null)
				Output = TraceOutput.HttpContext;
			else if(System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0)
				Output = TraceOutput.Diagnostics;
			else if(Parser.ToBool(ConfigurationManager.AppSettings["TraceEnabled"]))
				Output = TraceOutput.Console;

			bTraceOutputSet = true;

			if(Output != TraceOutput.Undefined)
				bIsEnabled = true;
			else
				bIsEnabled = false;

			return Output;
		}
		#endregion

		#region Write();
		/// <summary>
		/// Writes trace information to the trace log, including any user-defined categories, trace messages and error information.
		/// </summary>
		/// <param name="message">The trace message to write to the log</param>
		public static void Write(string message)
		{
			if(IsEnabled)
			{
				if(Output == TraceOutput.HttpContext)
					HttpContext.Current.Trace.Write(message);
				else if(Output == TraceOutput.Diagnostics)
				{
					try
					{
						System.Diagnostics.Trace.Listeners[0].WriteLine(GetTraceTimeStamp() + "	" + (message != null ? message : "NULL"));
						System.Diagnostics.Trace.Listeners[0].Flush();
					}
					catch(Exception oException)
					{
						Logger.LogError("Trace.Write()", oException);
					}
				}
				else if(Output == TraceOutput.Console)
				{
					Console.WriteLine(GetTraceTimeStamp() + "	" + (message != null ? message : "NULL"));
				}
			}
		}
		/// <summary>
		/// Writes trace information to the trace log, including any user-defined categories, trace messages and error information.
		/// </summary>
		/// <param name="category">The trace category that recieves the message</param>
		/// <param name="message">The trace message to write to the log</param>
		public static void Write(string category, string message)
		{
			if(IsEnabled)
			{
				if(Output == TraceOutput.HttpContext)
					HttpContext.Current.Trace.Write(category, message);
				else if(Output == TraceOutput.Diagnostics)
				{
					try
					{
						System.Diagnostics.Trace.Listeners[0].WriteLine(GetTraceTimeStamp() + "	" + (category!=null?category:"NULL"));
						System.Diagnostics.Trace.Listeners[0].WriteLine("			"+(message!=null?message:"NULL"));
						System.Diagnostics.Trace.Listeners[0].Flush();
					}
					catch(Exception oException)
					{
						Logger.LogError("Trace.Write()", oException);
					}
				}
				else if(Output == TraceOutput.Console)
				{
					Console.WriteLine(GetTraceTimeStamp() + "	" + (category!=null?category:"NULL"));
					Console.WriteLine("			"+(message!=null?message:"NULL"));
				}
			}
		}
		/// <summary>
		/// Writes trace information to the trace log, including any user-defined categories, trace messages and error information.
		/// </summary>
		/// <param name="category">The trace category that recieves the message</param>
		/// <param name="message">The trace message to write to the log</param>
		/// <param name="errorInfo">A System.Exception that contains information about the error</param>
		public static void Write(string category, string message, Exception errorInfo)
		{
			if(IsEnabled)
			{
				if(Output == TraceOutput.HttpContext)
					HttpContext.Current.Trace.Write(category, message, errorInfo);
				else if(Output == TraceOutput.Diagnostics)
				{
					try
					{
						System.Diagnostics.Trace.Listeners[0].WriteLine(GetTraceTimeStamp() + "	" + (category!=null?category:"NULL"));
						System.Diagnostics.Trace.Listeners[0].WriteLine(errorInfo.GetType().ToString());
						System.Diagnostics.Trace.Listeners[0].WriteLine(errorInfo.Message);
						System.Diagnostics.Trace.Listeners[0].WriteLine(errorInfo.Source);
						System.Diagnostics.Trace.Listeners[0].WriteLine(errorInfo.StackTrace);
						System.Diagnostics.Trace.Listeners[0].Flush();
					}
					catch(Exception oException)
					{
						Logger.LogError("Trace.Write()", oException);
					}
				}
				else if(Output == TraceOutput.Console)
				{
					Console.WriteLine(GetTraceTimeStamp() + "	" + (category!=null?category:"NULL"));
					Console.WriteLine(errorInfo.GetType().ToString());
					Console.WriteLine(errorInfo.Message);
					Console.WriteLine(errorInfo.Source);
					Console.WriteLine(errorInfo.StackTrace);
				}
			}
		}
		/// <summary>
		/// Writes trace information to the trace log of the supplied error information.
		/// </summary>
		/// <param name="errorInfo">A System.Exception that contains information about the error</param>
		public static void Write(Exception errorInfo)
		{
			if(IsEnabled)
			{
				if(Output == TraceOutput.HttpContext)
					HttpContext.Current.Trace.Write(null, null, errorInfo);
				else if(Output == TraceOutput.Diagnostics)
				{
					try
					{
						System.Diagnostics.Trace.Listeners[0].WriteLine(GetTraceTimeStamp() + "	" + errorInfo.GetType().ToString());
						System.Diagnostics.Trace.Listeners[0].WriteLine(errorInfo.Message);
						System.Diagnostics.Trace.Listeners[0].WriteLine(errorInfo.Source);
						System.Diagnostics.Trace.Listeners[0].WriteLine(errorInfo.StackTrace);
						System.Diagnostics.Trace.Listeners[0].Flush();
					}
					catch(Exception oException)
					{
						Logger.LogError("Trace.Write()", oException);
					}
				}
				else if(Output == TraceOutput.Console)
				{
					Console.WriteLine(GetTraceTimeStamp() + "	" + errorInfo.GetType().ToString());
					Console.WriteLine(errorInfo.Message);
					Console.WriteLine(errorInfo.Source);
					Console.WriteLine(errorInfo.StackTrace);
				}
			}
		}
		#endregion

		#region Warn();
		/// <summary>
		/// Writes trace information to the trace log, including any user-defined categories, trace messages and error information. All warning appear in the log as red text.
		/// </summary>
		/// <param name="message">The trace message to write to the log</param>
		public static void Warn(string message)
		{
			if(IsEnabled)
			{
				if(Output == TraceOutput.HttpContext)
					HttpContext.Current.Trace.Warn(message);
				else
					Write(message);
			}
		}
		/// <summary>
		/// Writes trace information to the trace log, including any user-defined categories, trace messages and error information.
		/// </summary>
		/// <param name="category">The trace category that recieves the message</param>
		/// <param name="message">The trace message to write to the log</param>
		public static void Warn(string category, string message)
		{
			if(IsEnabled)
			{
				if(Output == TraceOutput.HttpContext)
					HttpContext.Current.Trace.Warn(category, message);
				else
					Write(category, message);
			}
		}
		/// <summary>
		/// Writes trace information to the trace log, including any user-defined categories, trace messages and error information.
		/// </summary>
		/// <param name="category">The trace category that recieves the message</param>
		/// <param name="message">The trace message to write to the log</param>
		/// <param name="errorInfo">A System.Exception that contains information about the error</param>
		public static void Warn(string category, string message, Exception errorInfo)
		{
			if(IsEnabled)
			{
				if(Output == TraceOutput.HttpContext)
					HttpContext.Current.Trace.Warn(category, message, errorInfo);
				else
					Write(category, message, errorInfo);
			}
		}
		/// <summary>
		/// Writes trace information to the trace log of the supplied error information.
		/// </summary>
		/// <param name="errorInfo">A System.Exception that contains information about the error</param>
		public static void Warn(Exception errorInfo)
		{
			if(IsEnabled)
			{
				if(Output == TraceOutput.HttpContext)
					HttpContext.Current.Trace.Warn(null, null, errorInfo);
				else
					Warn(errorInfo);
			}
		}
		#endregion

		#region GetTraceTimeStamp();
		/// <summary>
		/// Generates a sortable timestamp for trace non-HttpContext messages (ex. 2006-04-17T14:22:48)
		/// </summary>
		/// <returns>A sortable timestamp string</returns>
		private static string GetTraceTimeStamp()
		{
			return DateTime.Now.ToString("s");
		}
		#endregion
	}
	#endregion
}
