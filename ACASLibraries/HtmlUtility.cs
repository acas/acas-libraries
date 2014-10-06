using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ACASLibraries
{
	/// <summary>
	/// The HtmlUtility class includes functions for working with Html and the GUI for web applications.
	/// </summary>
	public class HtmlUtility
	{
		#region RemoveTags();
		/// <summary>
		/// Removes all Html &lt;tags&gt; from the provided string.
		/// </summary>
		/// <param name="html">The string from which tags are to be removed.</param>
		/// <returns>The Html string without any Html tags.</returns>
		public static string RemoveTags(string html)
		{
			return Regex.Replace(html, "<[^>]*>", "");
		}
		#endregion

		#region EscapeHtml(); EscapeJavascript();
		/// <summary>
		/// Escape string for embedding in the special characters html strings.
		/// </summary>
		/// <param name="value">Any object supporting the ToString() method.</param>
		/// <returns>The escaped string. If the value is NULL, an empty non-NULL string is returned.</returns>
		public static string EscapeHtml(string value)
		{
			if (value != null && value.Length > 0)
			{

				return System.Web.HttpUtility.HtmlEncode(value).Replace("’", "&#39;").Replace("'", "&#39;").Replace("“", "&quot;").Replace("”", "&quot;").Replace("\"", "&quot;").Trim();
			}
			else
			{
				return "";
			}
		}

		/// <summary>
		/// Escape string for embedding in the special characters Javascript strings.
		/// </summary>
		/// <param name="value">Any object supporting the ToString() method.</param>
		/// <returns>The escaped string. If the value is NULL, an empty non-NULL string is returned.</returns>
		public static string EscapeJavascript(string value)
		{
			if (value != null && value.Length > 0)
			{
				return value.Replace("\\", "\\\\").Replace("&", "&amp;").Replace("\"", "&quot;").Replace("'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n").Trim();
			}
			else
			{
				return "";
			}
		}
		#endregion

		#region DropDown(); GetDropDownItemsHtmlFromParameterList();
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="selectedValue"></param>
		/// <param name="Required"></param>
		/// <param name="tagHtml"></param>
		/// <param name="sqlQuery"></param>
		/// <param name="sqlValueField"></param>
		/// <param name="sqlDisplayField"></param>
		/// <returns></returns>
		public static string DropDown(string name, string selectedValue, bool Required, string tagHtml, string sqlQuery, string sqlValueField, string sqlDisplayField)
		{
			return DropDown(name, selectedValue, Required, null, tagHtml, sqlQuery, sqlValueField, sqlDisplayField);
		}
		/// <summary>
		/// This function depends on the ApplicationDB connection string being set in the web.config connectionStrings section.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="selectedValue"></param>
		/// <param name="Required"></param>
		/// <param name="NotRequiredLabel"></param>
		/// <param name="tagHtml"></param>
		/// <param name="sqlQuery"></param>
		/// <param name="sqlValueField"></param>
		/// <param name="sqlDisplayField"></param>
		/// <returns></returns>
		public static string DropDown(string name, string selectedValue, bool Required, string NotRequiredLabel, string tagHtml, string sqlQuery, string sqlValueField, string sqlDisplayField)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("<SELECT NAME=\"" + name + "\"" + (tagHtml != null && tagHtml.Length > 0 ? " " + tagHtml : "") + ">");
			if(!Required)
			{
				sb.Append("<OPTION VALUE=\"\">" + (NotRequiredLabel != null ? NotRequiredLabel : "") + "</OPTION>");
			}
			SqlConnection oConn = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationDB"].ConnectionString);
			oConn.Open();
			SqlCommand oCmd = oConn.CreateCommand();
			oCmd.CommandType = CommandType.Text;
			oCmd.CommandText = sqlQuery;
			SqlDataReader oDR = oCmd.ExecuteReader();
			while(oDR.Read())
			{
				if(oDR[sqlValueField] != null && oDR[sqlDisplayField] != null)
				{
					sb.Append("<OPTION VALUE=\"" + oDR[sqlValueField].ToString() + "\"" + (oDR[sqlValueField].ToString() == selectedValue ? " SELECTED" : "") + ">" + oDR[sqlDisplayField].ToString() + "</OPTION>");
				}
			}
			oDR.Close();
			oDR = null;
			oCmd.Dispose();
			oCmd = null;
			oConn.Close();
			oConn.Dispose();
			oConn = null;
			sb.Append("</SELECT>");
			return sb.ToString();
		}

		/// <summary>
		///	Creates the option portion of the drop down (select) list.  Does Not create the &lt;select&gt; tags.
		/// </summary>
		/// <param name="sselectedKey">The value of the preselected option within the list.  (This is the key, not the text portion.)</param>
		/// <param name="a_soPair_sOptionKeyValuePairs">Array of string/object pairs representing key and value for each option in the selection list.</param>
		/// <returns>Html consisting of and selection-list option for each key/value pair.</returns>
		/// <example>
		/// <code>
		///  GetDropDownItemsHtmlFromParameterList("", "MD", "Maryland", "VA", "Virginia") --&gt; "&lt;option value="MD"&gt;Maryland&lt;/option&gt;&lt;option value="VA"&gt;Virginia&lt;/option&gt;"
		///  GetDropDownItemsHtmlFromParameterList("VA", "MD", "Maryland", "VA", "Virginia") --&gt; "&lt;option value="MD"&gt;Maryland&lt;/option&gt;&lt;option value="VA" selected="selected"&gt;Virginia&lt;/option&gt;"
		///  GetDropDownItemsHtmlFromParameterList("Virginia", "MD", "Maryland", "VA", "Virginia") --&gt; "&lt;option value="MD"&gt;Maryland&lt;&lt;/option&gt;&lt;option value="VA"&gt;Virginia&lt;/option&gt;" (will not select text, only key; see example 2)
		/// </code>
		/// </example>
		public static string GetDropDownItemsHtmlFromParameterList(string sselectedKey, params object[] a_soPair_sOptionKeyValuePairs)
		{
			string sDropDownItemsHtml = "";
			try
			{
				for(int iArgumentIndex = 0;iArgumentIndex < a_soPair_sOptionKeyValuePairs.Length;iArgumentIndex += 2)
				{
					sDropDownItemsHtml += "<option value=\"" + a_soPair_sOptionKeyValuePairs[iArgumentIndex] + "\"";
					if(Parser.ToString(a_soPair_sOptionKeyValuePairs[iArgumentIndex]) == sselectedKey)
					{
						sDropDownItemsHtml += " selected=\"selected\"";
					}
					sDropDownItemsHtml += ">" + Parser.ToString(a_soPair_sOptionKeyValuePairs[iArgumentIndex + 1]) + "</option>";
				}
			}
			catch
			{
				sDropDownItemsHtml += "<option>ERROR - REPORT THIS ERROR IMMEDIATELY</option>";
			}
			return sDropDownItemsHtml;
		}
		#endregion

		#region GetDropDownOptionsHtml();
		/// <summary>
		/// <para>Returns HTML formatted option list for dropdown list created from result of provided stored procedure call (just options no select elements are returned).</para>
		/// </summary>
		/// <param name="openSqlConnection">An open sql connection to be used for the stored procedure call</param>
		/// <param name="storedProcedureName">name of the stored procedure that will return the dataset to be used to populate the dropdown list</param>
		/// <param name="keyColumn">name of the column in the datatable that will supply the option keys</param>
		/// <param name="valueColumn">name of the column in the datatable that will supply the option values</param>
		/// <param name="selectedKey">The key that should be marked in the dropdown options as selected</param>
		/// <param name="argumentNameValuePairs">Additional argument for the stored procedure paired by argument name (including @) followed by argument value</param>
		/// <returns>string containing HTML dropdown elements of all requested data in the table</returns>
		public static string GetDropDownOptionsHtml(SqlConnection openSqlConnection, string storedProcedureName, string keyColumn, string valueColumn, string selectedKey, params object[] argumentNameValuePairs)
		{
			StringBuilder sb = new StringBuilder();

			SqlCommand oCmd = openSqlConnection.CreateCommand();
			oCmd.CommandType = CommandType.StoredProcedure;
			oCmd.CommandText = storedProcedureName;
			for (int x = 0; x < argumentNameValuePairs.Length; x += 2)
			{
				oCmd.Parameters.AddWithValue(argumentNameValuePairs[x].ToString(), argumentNameValuePairs[x + 1]);
			}
			SqlDataReader oDR = oCmd.ExecuteReader();

			while (oDR.Read())
			{
				sb.Append(String.Concat("<option value=\"", oDR[keyColumn].ToString(), "\""));
				if (selectedKey == oDR[keyColumn].ToString())
				{
					sb.Append(" selected=\"selected\"");
				}
				sb.Append(String.Concat(">", Parser.ToString(oDR[valueColumn]), "</option>"));
			}
			oDR.Close();
			oDR.Dispose();
			oDR = null;
			oCmd.Dispose();
			oCmd = null;
			return sb.ToString();
		}

		/// <summary>
		/// <para>Returns HTML formatted option list for dropdown list created from DataTable content (just options no select elements are returned).</para>
		/// </summary>
		/// <param name="dataTable">DataTable containing dropdown keys and values</param>
		/// <param name="keyColumn">name of the column in the datatable that will supply the option keys</param>
		/// <param name="valueColumn">name of the column in the datatable that will supply the option values</param>
		/// <param name="selectedKey">The key that should be marked in the dropdown options as selected</param>
		/// <returns>string containing HTML dropdown elements of all requested data in the table</returns>
		public static string GetDropDownOptionsHtml(DataTable dataTable, string keyColumn, string valueColumn, string selectedKey)
		{
			StringBuilder sb = new StringBuilder();
			foreach (DataRow oDR in dataTable.Rows)
			{
				sb.Append(String.Concat("<option value=\"", oDR[keyColumn].ToString(), "\""));
				if (selectedKey == oDR[keyColumn].ToString())
				{
					sb.Append(" selected=\"selected\"");
				}
				sb.Append(String.Concat(">", Parser.ToString(oDR[valueColumn]), "</option>"));
			}
			return sb.ToString();
		}

		public static string GetDropDownOptionsHtml(ref SqlDataReader openDataReader, string valueColumn, string labelColumn, string selectedValue, string selectedLabel, bool optional)
		{
			StringBuilder sb = new StringBuilder();

			if (optional)
			{
				sb.Append("<option value=\"\"></option>");
			}

			bool valueSelected = false;
			while (openDataReader.Read())
			{
				sb.AppendFormat("<option value=\"{0}\"", openDataReader["valueColumn"]);
				if (selectedValue == Parser.ToString(openDataReader["valueColumn"]))
				{
					valueSelected = true;
					sb.Append(" selected=\"true\"");
				}
				sb.AppendFormat(">{0}</option>", openDataReader["labelColumn"]);
			}
			if (!valueSelected && (!string.IsNullOrEmpty(selectedValue) || !string.IsNullOrEmpty(selectedLabel)))
			{
				sb.AppendFormat("<option value=\"{0}\" selected=\"true\">{1}</option>", selectedValue, selectedLabel);
			}

			return sb.ToString();
		}
		public static string GetDropDownOptionsHtml(ref NameValueCollection values, string selectedValue, string selectedLabel, bool optional)
		{
			StringBuilder sb = new StringBuilder();

			if (optional)
			{
				sb.Append("<option value=\"\"></option>");
			}

			bool valueSelected = false;
			if (values != null && values.Count > 0)
			{
				for (int x = 0; x < values.Count; x++)
				{
					sb.AppendFormat("<option value=\"{0}\"", values.Keys[x]);
					if (selectedValue == values.Keys[x])
					{
						valueSelected = true;
						sb.Append(" selected=\"true\"");
					}
					sb.AppendFormat(">{0}</option>", values.Get(x));
				}
			}
			if (!valueSelected && (!string.IsNullOrEmpty(selectedValue) || !string.IsNullOrEmpty(selectedLabel)))
			{
				sb.AppendFormat("<option value=\"{0}\" selected=\"true\">{1}</option>", selectedValue, selectedLabel);
			}

			return sb.ToString();
		}
		public static string GetDropDownOptionsHtml(ref IDictionary values, string selectedValue, string selectedLabel, bool optional)
		{
			StringBuilder sb = new StringBuilder();

			if (optional)
			{
				sb.Append("<option value=\"\"></option>");
			}

			bool valueSelected = false;
			if (values != null && values.Count > 0)
			{
				object[] keys = new object[values.Keys.Count];
				for (int x = 0; x < values.Count; x++)
				{
					object key = keys[x];
					sb.AppendFormat("<option value=\"{0}\"", key);
					if (selectedValue == key)
					{
						valueSelected = true;
						sb.Append(" selected=\"true\"");
					}
					sb.AppendFormat(">{0}</option>", values[key]);
				}
			}
			if (!valueSelected && (!string.IsNullOrEmpty(selectedValue) || !string.IsNullOrEmpty(selectedLabel)))
			{
				sb.AppendFormat("<option value=\"{0}\" selected=\"true\">{1}</option>", selectedValue, selectedLabel);
			}

			return sb.ToString();
		}
		public static string GetDropDownOptionsHtml(ref DataTable valuesTable, string valuesColumnName, string labelColumnName, string DefaultColumnName, string selectedValue, bool optional)
		{
			StringBuilder sb = new StringBuilder();

			if (optional)
			{
				sb.Append("<option value=\"\"></option>");
			}

			if (valuesTable != null && valuesTable.Rows.Count > 0)
			{
				for (int x = 0; x < valuesTable.Rows.Count; x++)
				{
					sb.AppendFormat("<option value=\"{0}\"", valuesTable.Rows[x][valuesColumnName]);
					if (((selectedValue == null || selectedValue == string.Empty) && Convert.ToBoolean(valuesTable.Rows[x][DefaultColumnName]))
						|| (selectedValue != null && selectedValue != string.Empty && selectedValue == valuesTable.Rows[x][valuesColumnName].ToString()))
					{
						sb.Append(" selected=\"true\"");
					}
					sb.AppendFormat(">{0}</option>", valuesTable.Rows[x][labelColumnName]);
				}
			}

			return sb.ToString();
		}
		#endregion

		#region GetCheckboxes();
		public static string GetCheckboxes(string checkboxName, ref NameValueCollection values, ref string[] selectedValues)
		{
			return GetCheckboxes(checkboxName, null, ref values, ref selectedValues);
		}
		public static string GetCheckboxes(string checkboxName, string itemStringFormat, ref NameValueCollection values, ref string[] selectedValues)
		{
			//itemStringFormat must include {0} for the target checkbox and {1} for the value label, if itemStringFormat is null, the default "{0} {1}<br/>" will be used
			StringBuilder sb = new StringBuilder();

			if (string.IsNullOrEmpty(itemStringFormat))
			{
				itemStringFormat = "{0} {1}<br />";
			}

			if (values != null && values.Count > 0)
			{
				for (int x = 0; x < values.Count; x++)
				{
					sb.AppendFormat(itemStringFormat, string.Format("<input type=\"checkbox\" name=\"{0}\" value=\"{1}\"" + (selectedValues != null && selectedValues.Contains(values.Keys[x]) ? " checked=\"true\"" : "") + ">", checkboxName, values.Keys[x]), values.Get(x));
				}
			}

			return sb.ToString();
		}
		public static string GetCheckboxes(string checkboxName, ref IDictionary values, ref string[] selectedValues)
		{
			return GetCheckboxes(checkboxName, null, ref values, ref selectedValues);
		}
		public static string GetCheckboxes(string checkboxName, string itemStringFormat, ref IDictionary values, ref string[] selectedValues)
		{
			//itemStringFormat must include {0} for the target checkbox and {1} for the value label, if itemStringFormat is null, the default "{0} {1}<br/>" will be used
			StringBuilder sb = new StringBuilder();

			if (string.IsNullOrEmpty(itemStringFormat))
			{
				itemStringFormat = "{0} {1}<br />";
			}

			if (values != null && values.Count > 0)
			{
				object[] keys = new object[values.Count];
				values.Keys.CopyTo(keys, 0);
				for (int x = 0; x < values.Count; x++)
				{
					object key = keys[x];
					sb.AppendFormat(itemStringFormat, string.Format("<input type=\"checkbox\" name=\"{0}\" value=\"{1}\"" + (selectedValues != null && selectedValues.Contains(key.ToString()) ? " checked=\"true\"" : "") + ">", checkboxName, key), values[key]);
				}
			}

			return sb.ToString();
		}
		#endregion

		#region GetRadioButtons();
		public static string GetRadioButtons(string radioButtonName, ref NameValueCollection values, string selectedValue)
		{
			return GetRadioButtons(radioButtonName, null, ref values, selectedValue);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="radioButtonName"></param>
		/// <param name="itemStringFormat">Must include {0} for the target radio button and {1} for the value label, if itemStringFormat is null, the default "{0} {1}<br/>" will be used</param>
		/// <param name="valuesTable"></param>
		/// <param name="selectedValue"></param>
		/// <param name="html"></param>
		/// <returns></returns>
		public static string GetRadioButtons(string radioButtonName, string itemStringFormat, ref NameValueCollection values, string selectedValue, string html = null)
		{
			//itemStringFormat must include {0} for the target radio button and {1} for the value label, if itemStringFormat is null, the default "{0} {1}<br/>" will be used
			StringBuilder sb = new StringBuilder();

			if (string.IsNullOrEmpty(itemStringFormat))
			{
				itemStringFormat = "{0} {1}<br />";
			}

			if (values != null && values.Count > 0)
			{
				for (int x = 0; x < values.Count; x++)
				{
					sb.AppendFormat(itemStringFormat, string.Format("<input type=\"radio\" name=\"{0}\" value=\"{1}\"" + (selectedValue == values.Keys[x] ? " checked=\"true\"" : "") + (string.IsNullOrEmpty(html) ? "" : " " + html) + ">", radioButtonName, values.Keys[x]), values.Get(x));
				}
			}

			return sb.ToString();
		}
		public static string GetRadioButtons(string radioButtonName, ref IDictionary values, string selectedValue)
		{
			return GetRadioButtons(radioButtonName, null, ref values, selectedValue);
		}
		public static string GetRadioButtons(string radioButtonName, string itemStringFormat, ref IDictionary values, string selectedValue, string html = null)
		{
			//itemStringFormat must include {0} for the target radio button and {1} for the value label, if itemStringFormat is null, the default "{0} {1}<br/>" will be used
			StringBuilder sb = new StringBuilder();

			if (string.IsNullOrEmpty(itemStringFormat))
			{
				itemStringFormat = "{0} {1}<br />";
			}

			if (values != null && values.Count > 0)
			{
				object[] keys = new object[values.Count];
				values.Keys.CopyTo(keys, 0);
				for (int x = 0; x < values.Count; x++)
				{
					object key = keys[x];
					sb.AppendFormat(itemStringFormat, string.Format("<input type=\"radio\" name=\"{0}\" value=\"{1}\"" + (selectedValue == key.ToString() ? " checked=\"true\"" : "") + (string.IsNullOrEmpty(html) ? "" : " " + html) + ">", radioButtonName, key), values[key]);
				}
			}

			return sb.ToString();
		}
		#endregion
	}
}
