using System;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ACASLibraries
{
	/// <summary>
	/// DebugUtility provides a set of static methods useful in debugging code and logging errors.
	/// </summary>
	public class DebugUtility
	{
		#region GetSqlCommandDetails();
		/// <summary>
		/// Generates details of the supplied SqlCommand as text including type, the query or procedure and all supplied parameters.
		/// </summary>
		/// <param name="Command">The subject SqlCommand object</param>
		/// <returns>The command details as text</returns>
		public static string GetSqlCommandDetails(IDbCommand Command)
		{
			return GetSqlCommandDetails(Command, false);
		}
		/// <summary>
		/// Generates details of the supplied SqlCommand as text or html including type, the query or procedure and all supplied parameters.
		/// </summary>
		/// <param name="Command">The subject SqlCommand object</param>
		/// <param name="ReturnAsHtml">When set as True, the command details are returned as Html.  When set as False, the command details are returned as text.</param>
		/// <returns>The command details as text or Html</returns>
		public static string GetSqlCommandDetails(IDbCommand Command, bool ReturnAsHtml)
		{
			StringBuilder oSB = new StringBuilder();
			if(Command != null)
			{

				//command type
				if(ReturnAsHtml)
					oSB.Append("<div><b>");
				switch(Command.CommandType)
				{
					case CommandType.StoredProcedure:
						oSB.Append("Stored Procedure: ");
						break;
					case CommandType.TableDirect:
						oSB.Append("Table Name: ");
						break;
					default:
						oSB.Append("Text Command: ");
						break;
				}
				if(ReturnAsHtml)
					oSB.Append("</b>");

				if(ReturnAsHtml)
					oSB.Append("<br />");
				else
					oSB.Append("\r\n");

				//command text
				if(Command.CommandText != null)
					oSB.Append(Command.CommandText);
				else
					oSB.Append("NULL");

				//command parameters
				if(Command.Parameters != null && Command.Parameters.Count > 0)
				{
					int iParams = 0;
					foreach(IDbDataParameter oParam in Command.Parameters)
					{
						if(ReturnAsHtml)
							oSB.Append("<br />&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
						else
							oSB.Append("\r\n	");

						//command parameter name
						if(oParam != null && oParam.ParameterName != null)
							oSB.Append(oParam.ParameterName);
						else
							oSB.Append("NULL PARAMETER or NO NAME");

						oSB.Append("=");

						//command parameter value
						if(oParam.Value != null && oParam.Value.ToString() != null)
							oSB.Append("'"+oParam.Value.ToString().Replace("'","''")+"'");
						else
							oSB.Append("NULL");

						if(iParams < Command.Parameters.Count-1)
							oSB.Append(",");
					}
				}
				if(ReturnAsHtml)
					oSB.Append("</div>");
			}
			else
			{
				oSB.Append("SqlCommand object is NULL");
			}

			return oSB.ToString();
		}
		#endregion
	
		#region GetCurrentMethodName(); GetMethodName();
		/// <summary>
		/// Returns the class and method name of the currently executing calling method in the format "ClassName.MethodName()".
		/// </summary>
		/// <returns>The method name of the caller or an empty string if this method was name able to determine obtain the calling method.</returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static string GetCurrentMethodName() {
			try {
				return GetMethodName(new StackFrame(1, true).GetMethod());
			} catch {
				return String.Empty;
			}
		}

		/// <summary>
		/// Formats the class and method name of the supplied method in the format "ClassName.MethodName()".
		/// </summary>
		/// <param name="methodInfo">The method to format.</param>
		/// <returns>The method name for the supplied methodInfo.</returns>
		public static string GetMethodName(MethodBase methodInfo) {
			return string.Format("{0}.{1}()",methodInfo.DeclaringType.Name,methodInfo.Name);
		}
		#endregion
	}
}