using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace ACASLibraries
{
	/// <summary>
	/// The Utility class encapsulates a number of uncategorized common functions such as escaping and formatting.
	/// </summary>
	/// <remarks>
	/// HISTORY:
	/// <para>1/23/2006 - JZM - Imported into project.  At this time, the class was reorganized and broken into many class making up the initial ACASLibraries namespace.</para>
	/// <para>3/9/2006  - TH  - Added Utility.JavascriptX(UnescapedValue)</para>
	/// <para>4/11/2006  - JZM - XmlUtility.Unescape() to use regular expressions to allow for case insensitivity.</para>
	/// <para>4/24/2006  - JZM - Utility.FormatDateTime() and Utility.FormatDate() to "d" for days instead of "dd".</para>
	/// <para>11/20/2007 - TLH/JZM - Moved many functions out of this class into HtmlUtility, DatabaseUtility and StringUtility classes.</para>
	/// </remarks>
	public class Utility
	{
		#region IsNull();
		/// <summary>
		/// Similar to the Microsoft SQL Server ISNULL() function, this method will return a DefaultValue when the Value inputted is NULL.
		/// </summary>
		/// <param name="Value">The value to be tested.</param>
		/// <param name="DefaultValue">A string to be returned if the tested value is NULL, has a length of zero or is the word NULL itself.</param>
		/// <returns>A non-NULL string value.</returns>
		public static string IsNull(object Value, string DefaultValue)
		{
			if(Value == null || Value.ToString().Length == 0 || Value.ToString().ToUpper() == "NULL")
			{
				return DefaultValue;
			}
			else
			{
				return Value.ToString();
			}
		}

		/// <summary>
		/// Similar to the Microsoft SQL Server ISNULL() function, this method will return a DefaultValue when the Value inputted is NULL.
		/// </summary>
		/// <param name="Value">The value to be tested.</param>
		/// <param name="DefaultValue">The value to be returned if the tested value is NULL, has a length of zero or is the word NULL itself.</param>
		/// <returns>A non-NULL string value.</returns>
		public static object IsNull(object Value, object DefaultValue)
		{
			if (Value == null || Value.ToString().Length == 0 || Value.ToString().ToUpper() == "NULL")
			{
				return DefaultValue;
			}
			else
			{
				return Value;
			}
		}
		#endregion

		#region GetTimeStamp();
		/// <summary>
		///	Obtains a sortable timestamp.
		/// </summary>
		/// <returns>A timestamp in the format YYYYMMDDhhmmssiii (where i is the millisecond).  All element are padded with zero to there appropriate length.</returns>
		/// <example>
		/// <code>
		///  GetTimeStamp() --> "20060310111957364" (on 3/10/2006 at 11:19:57.364 AM)
		/// </code>
		/// </example>
		public static string GetTimeStamp()
		{
			return DateTime.Now.Year.ToString().PadLeft(4, '0') + DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0') + DateTime.Now.Hour.ToString().PadLeft(2, '0') + DateTime.Now.Minute.ToString().PadLeft(2, '0') + DateTime.Now.Second.ToString().PadLeft(2, '0') + DateTime.Now.Millisecond.ToString().PadLeft(3, '0');
		}
		#endregion

		#region GetSortableDateStamp();
		/// <summary>
		///	Obtains a sortable datestamp.
		/// </summary>
		/// <returns>A timestamp in the format YYYY-MM-DD (where i is the millisecond).</returns>
		/// <example>
		/// <code>
		///  GetSortableDateStamp() --> "2006-03-10" (on 3/10/2006)
		/// </code>
		/// </example>
		public static string GetSortableDateStamp()
		{
			return DateTime.Now.Year.ToString().PadLeft(4, '0') + "-" + DateTime.Now.Month.ToString().PadLeft(2, '0') + "-" + DateTime.Now.Day.ToString().PadLeft(2, '0');
		}
		#endregion

		#region IsValueInCommaDelimitedList(); AddValueToCommaDelimitedList();
		/// <summary>
		///	Determines whether a provided value is in a provided CommaDelimitedList.
		/// </summary>
		/// <param name="sCommaDelimitedList">CommaDelimitedList (list of values separated by commas) in which to search for the provided sValueToSearchFor.  Note: No spaces must surround commas in list, e.g. - "x,x,x" not "x, x, x".</param>
		/// <param name="sValueToSearchFor">Value to search for in the provided sCommaDelimitedList.</param>
		/// <returns>Returns true if sValueToSearchFor exists as a value in sCommaDelimitedList.</returns>
		/// <example>
		/// <code>
		///  IsValueInCommaDelimitedList("1,2,3", "3") --> true
		///  IsValueInCommaDelimitedList("1,2,3", "22") --> false
		///  IsValueInCommaDelimitedList("1, 2, 3", "2") --> false (CommaDelimitedList must not contain spaces unless the spaces are part of the value; " 2" would return true)
		/// </code>
		/// </example>
		public static bool IsValueInCommaDelimitedList(string sCommaDelimitedList, string sValueToSearchFor)
		{
			return (("," + sCommaDelimitedList + ",").IndexOf("," + sValueToSearchFor + ",") > -1) ? true : false;
		}

		// 
		// Notes: sCommaDelimitedList can be an empty string or a single value as well as a CommaDelimitedList
		// Notes: sValueToAdd can be a single value or CommaDelimitedList

		/// <summary>
		///	Adds a provided sValueToAdd to the end of a provided CommaDelimitedList.
		/// </summary>
		/// <param name="sCommaDelimitedList">CommaDelimitedList (list of values separated by commas).  Note: No spaces must surround commas in list, e.g. - "x,x,x" not "x, x, x".</param>
		/// <param name="sValueToAdd">Value to be added to sCommaDelimitedList.  Note: sValueToAdd can be a single value or CommaDelimitedList.</param>
		/// <returns>A CommaDelimitedList with the new value added to the end.</returns>
		/// <example>
		/// <code>
		///  AddValueToCommaDelimitedList("1,2,3", "4") --> "1,2,3,4"
		///  AddValueToCommaDelimitedList("1,2,4", "3") --> "1,2,4,3"
		///  AddValueToCommaDelimitedList("1,2", "3,4") --> "1,2,3,4"
		///  AddValueToCommaDelimitedList("1,2", "2") --> "1,2,2"
		///  AddValueToCommaDelimitedList("MD,DC", "VA") --> "MD,DC,VA"
		/// </code>
		/// </example>
		public static string AddValueToCommaDelimitedList(string sCommaDelimitedList, string sValueToAdd)
		{
			if(sCommaDelimitedList.Length > 0)
			{
				return sCommaDelimitedList + "," + sValueToAdd;
			}
			else
			{
				return sValueToAdd;
			}
		}
		#endregion

		#region IfExists();
		/// <summary>
		/// When the Value is not null and has a length greater than zero (when in string form), returns the Value. Numeric zeros will be returned.
		/// </summary>
		/// <param name="Value">Value to test</param>
		/// <returns>Value if test is successful, otherwise NULL</returns>
		public static object IfExists(object Value)
		{
			return IfExists(Value, Value, null, false);
		}
		/// <summary>
		/// When the Value is not null and has a length greater than zero (when in string form), returns the Value. If IgnoreZeros is true, numeric zeros will cause test to fail. If IgnoreZeros is false, numeric zeros will be considered a successful test.
		/// </summary>
		/// <param name="Value">Value to test</param>
		/// <param name="IgnoreZero">When true, zeros are not considered a successful test</param>
		/// <returns>Value if test is successful, otherwise NULL</returns>
		public static object IfExists(object Value, bool IgnoreZero)
		{
			return IfExists(Value, Value, null, IgnoreZero);
		}
		/// <summary>
		/// When the TestValue is not null and has a length greater than zero (when in string form), returns the ResultValue. Numeric zeros will be returned.
		/// </summary>
		/// <param name="TestValue">Value to test</param>
		/// <param name="SuccessValue">Value to return if test is successful</param>
		/// <returns>SuccessValue if test is successful, otherwise NULL</returns>
		public static object IfExists(object TestValue, object SuccessValue)
		{
			return IfExists(TestValue, SuccessValue, null, false);
		}
		/// <summary>
		/// When the TestValue is not null and has a length greater than zero (when in string form), returns the ResultValue. Numeric zeros will be returned.
		/// </summary>
		/// <param name="TestValue">Value to test</param>
		/// <param name="SuccessValue">Value to return if test is successful</param>
		/// <param name="FailValue">Value to return if test fails</param>
		/// <returns>SuccessValue if test is successful, FailValue if test fails</returns>
		public static object IfExists(object TestValue, object SuccessValue, object FailValue)
		{
			return IfExists(TestValue, SuccessValue, FailValue, false);
		}
		/// <summary>
		/// When the TestValue is not null and has a length greater than zero (when in string form), returns the ResultValue. Numeric zeros will be returned.
		/// </summary>
		/// <param name="TestValue">Value to test</param>
		/// <param name="SuccessValue">Value to return if test is successful</param>
		/// <param name="IgnoreZero">When true, zeros are not considered a successful test</param>
		/// <returns>SuccessValue if test is successful, otherwise NULL</returns>
		public static object IfExists(object TestValue, object SuccessValue, bool IgnoreZero)
		{
			return IfExists(TestValue, SuccessValue, null, IgnoreZero);
		}
		/// <summary>
		/// When the TestValue is not null and has a length greater than zero (when in string form), returns the ResultValue. If IgnoreZeros is true, numeric zeros will cause test to fail. If IgnoreZeros is false, numeric zeros will be considered a successful test.
		/// </summary>
		/// <param name="TestValue">Value to test</param>
		/// <param name="SuccessValue">Value to return if test is successful</param>
		/// <param name="IgnoreZero">When true, numeric values of zero are considered a failed test</param>
		/// <param name="FailValue">Value to return if test fails</param>
		/// <returns>SuccessValue if test is successful, FailValue if test fails</returns>
		public static object IfExists(object TestValue, object SuccessValue, object FailValue, bool IgnoreZero)
		{
			if (TestValue != null && TestValue.ToString().Length > 0)
			{
				if (IgnoreZero)
				{
					if (Parser.IsNumeric(TestValue) && Parser.ToDouble(TestValue) == 0)
					{
						return FailValue;
					}
					else
					{
						return SuccessValue;
					}
				}
				else
				{
					return SuccessValue;
				}
			}
			else
			{
				return FailValue;
			}
		}
		#endregion

		#region GetDescription()
		/// <summary>
		/// Returns the value of the Description attribute for the a field or enum value.  Description attribute is defined as System.ComponentModel.DescriptionAttribute.
		/// <example>
		///	enum MyEnum {
		///		[Description("Description of Value1")]
		///		Value1,
		///		[Description("Description of Value2")]	
		///		Value2
		/// }
		/// ...
		/// MyEnum value = MyEnum.Value2;
		/// string valueDescription = GetEnumDescription(value);
		/// //valueDescription now equals "Description of Value2"
		/// </example>
		/// </summary>
		/// <param name="value">Instance or enum value with a Description attribute.</param>
		/// <returns>The text of the Description attribute. Returns the object's value as a string if the description is not defined.</returns>
		public static string GetDescription(object value)
		{
			FieldInfo fi = value.GetType().GetField(value.ToString());
			if(fi != null) {
				DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

				if (attributes != null && attributes.Length > 0) {
					return attributes[0].Description;
				}
			}

			return Parser.ToString(value);
		}
		#endregion
	}
}