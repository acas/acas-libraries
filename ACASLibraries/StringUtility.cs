using System;
using System.Text;
using System.Globalization;

namespace ACASLibraries
{
	/// <summary>
	/// The StringUtility class includes functions for formatting and working with strings.
	/// </summary>
	/// <remarks>
	/// HISTORY:
	/// <para>11/20/2007 - TLH/JZM - Creation date, many functions relocated from Utility class.</para>
	/// <para>1/14/2013 - JZM - Revised Truncate(); Renamed FormatMoney() to FormatCurrency();</para>
	/// </remarks>
	public class StringUtility
	{
		#region NumberGroupSeparatorsOn/Off(); [internal]
		/// <summary>
		/// Removes value Group Separators (commas in the US) from the provided number
		/// </summary>
		/// <param name="value">value which to remove commas from</param>
		/// <returns>Proveded number but without commas.  If the Value is NULL, an empty string will be returned.</returns>
		internal static string NumberGroupSeparatorsOff(string value)
		{
			if (value == null)
			{
				return "";
			}
			return value.Replace(NumberFormatInfo.CurrentInfo.NumberGroupSeparator, "");
		}

		/// <summary>
		/// Adds value Group Separators (commas in the US) to the proviced number while keeping all other aspects of that number intact
		/// </summary>
		/// <param name="value">value which to add commas formatting to</param>
		/// <returns>Provided number with standard comma formatting.  If the Value is NULL, an empty string will be returned.</returns>
		internal static string NumberGroupSeparatorsOn(object value)
		{
			if (value == null)
			{
				return "";
			}
			string valueString = NumberGroupSeparatorsOff(Parser.ToString(value));
			if (valueString.Length < 1)
			{
				return "";
			}
			string wholeNumber = Parser.ToString(Parser.ToLong(Math.Truncate(Parser.ToDouble(valueString))));
			int numberOfCommas = (wholeNumber.Length - 1) / 3;
			StringBuilder sb = new StringBuilder(valueString.Length + numberOfCommas);
			sb.Append((Parser.ToDouble(wholeNumber)).ToString("#,##0"));
			int indexOfDecimal = valueString.LastIndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator);
			if (indexOfDecimal > 0)
			{
				sb.Append(valueString.Substring(indexOfDecimal));
			}
			return sb.ToString();
		}
		#endregion

		#region FormatDate(); FormatDateTime(); FormatDateLong();
		/// <summary>
		/// Format a date in the "M/d/yyyy" format (i.e. 3/15/2006).
		/// </summary>
		/// <param name="unformattedDate">Any object supporting the ToString() method which will return a date in string form.</param>
		/// <returns>The formatted date as a string.  If UnformattedDate is NULL, an empty string will be returned.</returns>
		public static string FormatDate(object unformattedDate)
		{
			string sOutput = "";
			try
			{
				if (unformattedDate != null && unformattedDate.ToString().Length > 0 && !unformattedDate.ToString().StartsWith("1/1/1900"))
				{
					DateTime oDate = DateTime.Parse(unformattedDate.ToString());
					sOutput = oDate.ToString("M/d/yyyy");
				}
			}
			catch
			{ }
			return sOutput;
		}

		/// <summary>
		/// Format a date/time in the "M/d/yyyy h:mm tt" format (i.e. 3/15/2006 4:15 PM).
		/// </summary>
		/// <param name="unformattedDate">Any object supporting the ToString() method which will return a date in string form.</param>
		/// <returns>The formatted date/time as a string.  If UnformattedDate is NULL, an empty string will be returned.</returns>
		public static string FormatDateTime(object unformattedDate)
		{
			string sOutput = "";
			try
			{
				if (unformattedDate != null && unformattedDate.ToString().Length > 0)
				{
					DateTime oDate = DateTime.Parse(unformattedDate.ToString());
					sOutput = oDate.ToString("M/d/yyyy h:mm tt");
				}
			}
			catch
			{ }
			return sOutput;
		}

		/// <summary>
		/// Format a date in long form (i.e. Wednesday, March 15, 2006).
		/// </summary>
		/// <param name="unformattedValue">Any object supporting the ToString() method which will return a date in string form.</param>
		/// <returns>The formatted date/time as a string.  If UnformattedDate is NULL, an empty string will be returned.</returns>
		public static string FormatDateLong(object unformattedValue)
		{
			string sOutput = "";
			try
			{
				if (unformattedValue != null && unformattedValue.ToString().Length > 0)
				{
					DateTime oDate = DateTime.Parse(unformattedValue.ToString());
					sOutput = oDate.DayOfWeek.ToString() + ", " + oDate.ToString("MMMM") + " " + oDate.Day.ToString() + ", " + oDate.Year.ToString();
				}
			}
			catch
			{ }
			return sOutput;
		}
		#endregion

		#region FormatCurrency();
		/// <summary>
		/// Formats a numeric value into a string representation of its value with two decimal places.
		/// </summary>
		/// <param name="unformattedValue">Any object supporting the ToString() method which will return a numeric value in string form.</param>
		/// <returns>The formatted value.  If unformattedValue is NULL, 0.00 will be returned.  Does not return any currency symbol.</returns>
		public static string FormatCurrency(object unformattedValue) {
			return Parser.ToDouble(unformattedValue).ToString("#,##0.00");
		}
		/// <summary>
		/// Formats a numeric value into a string representation of its value with two decimal places.
		/// </summary>
		/// <param name="unformattedValue">Any object supporting the ToString() method which will return a numeric value in string form.</param>
		/// <param name="currencySymbol">Currency symbol such as $.</param>
		/// <returns>The formatted value.  If unformattedValue is NULL, 0.00 will be returned.  Does not return any currency symbol.</returns>
		public static string FormatCurrency(object unformattedValue, object currencySymbol) {
			return string.Concat(currencySymbol,Parser.ToDouble(unformattedValue).ToString("#,##0.00"));
		}
		/// <summary>
		/// Formats a numeric value into a string representation of its value with two decimal places.
		/// </summary>
		/// <param name="unformattedValue">Any object supporting the ToString() method which will return a numeric value in string form.</param>
		/// <param name="returnEmptyIfNull">Any object supporting the ToString() method which will return a numeric value in string form.</param>
		/// <returns>The formatted value.  If unformattedValue is NULL, either 0.00 will be returned if returnEmptyIfNull is false or an empty string will be returned if returnEmptyIfNull is true.  Does not return any currency symbol.</returns>
		public static string FormatCurrency(object unformattedValue, bool returnEmptyIfNull) {
			return FormatCurrency(unformattedValue, returnEmptyIfNull, null);
		}
		/// <summary>
		/// Formats a numeric value into a string representation of its value with two decimal places.
		/// </summary>
		/// <param name="unformattedValue">Any object supporting the ToString() method which will return a numeric value in string form.</param>
		/// <param name="returnEmptyIfNull">Any object supporting the ToString() method which will return a numeric value in string form.</param>
		/// <param name="currencySymbol">Currency symbol such as $.</param>
		/// <returns>The formatted value.  If unformattedValue is NULL, either 0.00 will be returned if returnEmptyIfNull is false or an empty string will be returned if returnEmptyIfNull is true.  Does not return any currency symbol.</returns>
		public static string FormatCurrency(object unformattedValue, bool returnEmptyIfNull, object currencySymbol)
		{
			if(!returnEmptyIfNull) {
				return FormatCurrency(unformattedValue, currencySymbol);
			} else if(unformattedValue == null) {
				string value = Parser.ToString(unformattedValue);
				decimal valueDecimal;
				if(value.Length > 0 && decimal.TryParse(value, out valueDecimal)) {
					return string.Concat(currencySymbol, String.Format("#,##0.00", valueDecimal));
				} else {
					return String.Empty;
				}
			} else {
				return String.Empty;
			}
		}
		#endregion

		#region FormatDecimal();
		/// <summary>
		/// Formats a numeric value into a string representation of its value with a minimum of two decimal places and a maximum of eight decimal places.
		/// </summary>
		/// <param name="unformattedValue">Any object supporting the ToString() method which will return a numeric value in string form.</param>
		/// <returns>The formatted value.  If unformattedValue is NULL, 0.00 will be returned.</returns>
		public static string FormatDecimal(object unformattedValue)
		{
			return (Parser.ToDecimal(unformattedValue)).ToString("0.00######");
		}

		/// <summary>
		///	Returns a string versions of a double with the amount of places after the decimal place specified by iPrecision will truncate excess digits past precision, or will pad with zeros when number is short of precision. If null is passed in, "" will be returned.
		/// </summary>
		/// <param name="oValue">An object supporting the ToString() method which will return a numeric value in string form.</param>
		/// <param name="iPrecision">The number of decimal places to returned in the formatted, 0 will return no decimal point.</param>
		/// <returns>The formatted value.  If the oValue is NULL, an empty string will be returned.  The value will be rounded if need be.</returns>
		/// <example>
		/// <code>
		/// FormatDecimal(123.009, 2) -> "123.01"
		/// FormatDecimal(123, 2) -> "123.00"
		/// FormatDecimal(123, 1) -> "123.0"
		/// FormatDecimal(123, 0) -> "123."
		/// FormatDecimal(123, 0) -> "123"
		/// FormatDecimal(null, 2) -> "" note: NOT "0.00"
		/// </code>
		/// </example>
		public static string FormatDecimal(object oValue, int iPrecision)
		{
			return FormatDecimal(oValue, iPrecision, false);
		}

		/// <summary>
		///	Returns a string versions of a double with the amount of places after the decimal place specified by iPrecision will truncate excess digits past precision, or will pad with zeros when number is short of precision. If null is passed in, "" will be returned.
		/// </summary>
		/// <param name="Value">An object supporting the ToString() method which will return a numeric value in string form.</param>
		/// <param name="Precision">The number of decimal places to returned in the formatted, 0 will return no decimal point.</param>
		/// <param name="Truncate">will truncate value rather than round the value if true</param>
		/// <returns>The formatted value.  If the unformattedValue is NULL, an empty string will be returned</returns>
		/// <example>
		/// <code>
		/// FormatDecimal(123.009, 2, true) -> "123.00"
		/// FormatDecimal(123.009, 2, false) -> "123.01"
		/// FormatDecimal(1234, 2, false) -> "1234.00"
		/// FormatDecimal(123, 1, false) -> "123.0"
		/// FormatDecimal(123, 0, false) -> "123"
		/// FormatDecimal(null, 2, false) -> "" note: NOT "0.00"
		/// </code>
		/// </example>
		public static string FormatDecimal(object Value, int Precision, bool Truncate)
		{
			try
			{
				if (Value != null && Value.ToString().Trim() != "")
				{
					if (!Truncate)
					{
						return (Parser.ToDecimal(Value)).ToString("0" + ((Precision >= 0) ? ("." + "".PadRight(Math.Max(Precision, 0), '0')) : ""));
					}
					else
					{
						if (Precision <= 0)
						{
							Precision = -1;
						}
						string sReturnValue = Parser.ToDecimal(Value).ToString();
						int iDecimalPointIndex = sReturnValue.IndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator);
						if (iDecimalPointIndex == 0)
						{
							sReturnValue = "0" + sReturnValue;
							iDecimalPointIndex++;
						}
						else if (iDecimalPointIndex < 0)
						{
							sReturnValue += NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
							iDecimalPointIndex = sReturnValue.IndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator);
						}
						sReturnValue = sReturnValue + "".PadRight(Math.Max(Precision, 0), '0');
						return sReturnValue.Substring(0, iDecimalPointIndex + Precision + 1);
					}
				}
			}
			catch { }
			return "";
		}

		/// <summary>
		///	Returns a formated versions of a provided object containing at least the amount of places after the decimal place specified by MinimumPrecision, but not more than the number of digits specified by the MaximumPrecision.  The function will truncate excess digits pass MaximumPrecision, or will pad with zeros when number is short of MinimumPrecision.  If null or "" is passed in, "" will be returned.
		/// </summary>
		/// <param name="Value">A object that can readily be translated to a Decimal will return a numeric value in string form.</param>
		/// <param name="MinimumPrecision">The minimum number of decimal places to returned in the formatted.</param>
		/// <param name="MaximumPrecision">The maximum number of decimal places to returned in the formatted.  Must be greater than or equal to MinimumPrecision.</param>
		/// <returns>The formatted value.  If the Value is NULL, an empty string will be returned</returns>
		/// <example>
		/// <code>
		/// FormatDecimal("123", 2, 4) -> "123.00"
		/// FormatDecimal("123.12", 2, 4) -> "123.12"
		/// FormatDecimal("123.12", 3, 4) -> "123.120"
		/// FormatDecimal("123.1234", 2, 4) -> "123.1234"
		/// FormatDecimal("123.123456", 2, 4) -> "123.1234"
		/// FormatDecimal("0.0", 3, 8) -> "0.000"
		/// FormatDecimal("", iAnyInt, iAnyInt) -> "" (note: NOT "0.00")
		/// FormatDecimal(null, iAnyInt, iAnyInt) -> "" (note: NOT "0.00")
		/// </code>
		/// </example>
		public static string FormatDecimal(object Value, int MinimumPrecision, int MaximumPrecision)
		{
			return FormatDecimal(Value, MinimumPrecision, MaximumPrecision, false);
		}

		/// <summary>
		///	Returns a formated versions of a provided object containing at least the amount of places after the decimal place specified by MinimumPrecision, but not more than the number of digits specified by the MaximumPrecision.  The function will truncate excess digits pass MaximumPrecision, or will pad with zeros when number is short of MinimumPrecision.  If null or "" is passed in, "" will be returned.
		/// </summary>
		/// <param name="Value">A object that can readily be translated to a Decimal will return a numeric value in string form.</param>
		/// <param name="MinimumPrecision">The minimum number of decimal places to returned in the formatted.</param>
		/// <param name="MaximumPrecision">The maximum number of decimal places to returned in the formatted.  Must be greater than or equal to MinimumPrecision.</param>
		/// <param name="Truncate">will truncate value rather than round the value if true</param>
		/// <returns>The formatted value.  If the Value is NULL, an empty string will be returned</returns>
		/// <example>
		/// <code>
		/// FormatDecimal("123", 2, 4, false) -> "123.00"
		/// FormatDecimal("123.12", 2, 4, false) -> "123.12"
		/// FormatDecimal("123.12", 3, 4, false) -> "123.120"
		/// FormatDecimal("123.1234", 2, 4, false) -> "123.1234"
		/// FormatDecimal("123.123456", 2, 4, false) -> "123.1234"
		/// FormatDecimal("123.123456", 2, 4, true) -> "123.1235"
		/// FormatDecimal("0.0", 3, 8, false) -> "0.000"
		/// FormatDecimal("", iAnyInt, iAnyInt, false) -> "" (note: NOT "0.00")
		/// FormatDecimal(null, iAnyInt, iAnyInt, false) -> "" (note: NOT "0.00")
		/// </code>
		/// </example>
		public static string FormatDecimal(object Value, int MinimumPrecision, int MaximumPrecision, bool Truncate)
		{
			try
			{
				if (Value != null && Value.ToString() != "")
				{
					if (!Truncate)
					{
						return (Parser.ToDecimal(Value)).ToString("0." + "".PadRight(Math.Max(MinimumPrecision, 0), '0') + "".PadRight(Math.Max(MaximumPrecision - MinimumPrecision, 0), '#'));
					}
					else
					{
						if (MinimumPrecision <= 0)
						{
							MinimumPrecision = -1;
						}
						if (MaximumPrecision <= 0)
						{
							MaximumPrecision = -1;
						}
						if (MinimumPrecision > MaximumPrecision)
						{
							MinimumPrecision = MaximumPrecision;
						}
						string sReturnValue = Parser.ToDecimal(Value).ToString();
						int iDecimalPointIndex = sReturnValue.IndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator);
						if (iDecimalPointIndex == 0)
						{
							sReturnValue = "0" + sReturnValue;
							iDecimalPointIndex++;
						}
						else if (iDecimalPointIndex < 0)
						{
							sReturnValue += NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
							iDecimalPointIndex = sReturnValue.IndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator);
						}
						sReturnValue = sReturnValue + "".PadRight(Math.Max(MaximumPrecision, 0), '0');
						sReturnValue = sReturnValue.Substring(0, iDecimalPointIndex + MaximumPrecision + 1);
						return sReturnValue.Substring(0, sReturnValue.Length - (MaximumPrecision - MinimumPrecision)) + sReturnValue.Substring(sReturnValue.Length - (MaximumPrecision - MinimumPrecision)).TrimEnd("0".ToCharArray());
					}
				}
			}
			catch { }
			return "";
		}
		#endregion

		#region FormatNumber();
		/// <summary>
		///  <para>Returns a standard comma formated version of a provided object containing
		///     at least two places after the decimal place and at most eight places after
		///	    the decimal place.  The function will round the ninth digit into the eight
		///     if unformattedValue has more than eight places, or will pad with zeros
		///	    when number is shorter than two precision places.  If null or "" or null is
		///			passed in, "" will be returned.</para>
		/// </summary>
		/// <param name="unformattedValue">A object that can readily be translated to a Decimal will return a numeric value in string form.</param>
		/// <returns>The formatted value.  If the Value is NULL, an empty string will be returned</returns>
		public static string FormatNumber(object unformattedValue)
		{
			return NumberGroupSeparatorsOn(FormatDecimal(unformattedValue));
		}
		/// <summary>
		///  <para>Returns a standard comma formated version of a provided object containing
		///     the amount of places after the decimal place specified by Precision.  The
		///     function will round excess digits past Precision, or will pad with zeros
		///	    when the number is short of the Precision.  If null or "" or null is passed in, ""
		///	    will be returned.</para>
		/// </summary>
		/// <param name="unformattedValue">A object that can readily be translated to a Decimal will return a numeric value in string form.</param>
		/// <param name="Precision">The number of decimal places to returned in the formatted.</param>
		/// <returns>The formatted value.  If the Value is NULL, an empty string will be returned</returns>
		public static string FormatNumber(object unformattedValue, int Precision)
		{
			return NumberGroupSeparatorsOn(FormatDecimal(unformattedValue, Precision));
		}
		/// <summary>
		///  <para>Returns a standard comma formated version of a provided object containing
		///     the amount of places after the decimal place specified by Precision.  The
		///     function will round excess digits past Precision (depending on the
		///     provided Truncate flag), or will pad with zeros when number is short of the
		///     Precision.  If null or "" or null is passed in, "" will be returned.</para>
		/// </summary>
		/// <param name="unformattedValue">A object that can readily be translated to a Decimal will return a numeric value in string form.</param>
		/// <param name="Precision">The number of decimal places to returned in the formatted.</param>
		/// <param name="Truncate">will truncate value rather than round the value if true</param>
		/// <returns>The formatted value.  If the Value is NULL, an empty string will be returned</returns>
		public static string FormatNumber(object unformattedValue, int Precision, bool Truncate)
		{
			return NumberGroupSeparatorsOn(FormatDecimal(unformattedValue, Precision, Truncate));
		}
		/// <summary>
		///  <para>Returns a standard comma formated version of a provided object containing
		///     at least the amount of places after the decimal place specified by MinimumPrecision,
		///	    but not more than the number of digits specified by the MaximumPrecision.  The
		///     function will truncate or round excess digits past MaximumPrecision, or will pad 
		///	    with zeros when number is short of MinimumPrecision.  If null or "" or null is passed
		///     in, "" will be returned.</para>
		/// </summary>
		/// <param name="unformattedValue">A object that can readily be translated to a Decimal will return a numeric value in string form.</param>
		/// <param name="MinimumPrecision">The minimum number of decimal places to returned in the formatted.</param>
		/// <param name="MaximumPercision">The maximum number of decimal places to returned in the formatted.  Must be greater than or equal to MinimumPrecision.</param>
		/// <returns>The formatted value.  If the Value is NULL, an empty string will be returned</returns>
		public static string FormatNumber(object unformattedValue, int MinimumPrecision, int MaximumPercision)
		{
			return NumberGroupSeparatorsOn(FormatDecimal(unformattedValue, MinimumPrecision, MaximumPercision));
		}
		/// <summary>
		///  <para>Returns a standard comma formated version of a provided object containing
		///     at least the amount of places after the decimal place specified by MinimumPrecision,
		///	    but not more than the number of digits specified by the MaximumPrecision.  The
		///     function will truncate or round excess digits pass MaximumPrecision (depending on the
		///     provided Truncate flag), or will pad with zeros when number is short of
		///     MinimumPrecision.  If null or "" or null is passed in, "" will be returned.</para>
		/// </summary>
		/// <param name="unformattedValue">A object that can readily be translated to a Decimal will return a numeric value in string form.</param>
		/// <param name="MinimumPrecision">The minimum number of decimal places to returned in the formatted.</param>
		/// <param name="MaximumPercision">The maximum number of decimal places to returned in the formatted.  Must be greater than or equal to MinimumPrecision.</param>
		/// <param name="Truncate">will truncate value rather than round the value if true</param>
		/// <returns>The formatted value.  If the Value is NULL, an empty string will be returned</returns>
		public static string FormatNumber(object unformattedValue, int MinimumPrecision, int MaximumPercision, bool Truncate)
		{
			return NumberGroupSeparatorsOn(FormatDecimal(unformattedValue, MinimumPrecision, MaximumPercision, Truncate));
		}
		#endregion

		#region FormatUsername();
		/// <summary>
		/// <para>Returns username with domain information stripped out</para>
		/// </summary>
		/// <param name="unformattedValue">Username or Username Plus Domain.</param>
		/// <returns>returns Username without Domain extension.</returns>
		/// <example>
		///   <code>
		/// 	  "ACS/Vader.Darth"       -> "Vader.Darth"
		///     "Vader.Darth@esops.com" -> "Vader.Darth"
		///     "Vader.Darth"           -> "Vader.Darth"
		///   </code>
		/// </example>
		public static string FormatUsername(string unformattedValue)
		{
			if (unformattedValue == null)
			{
				return "";
			}
			int slashIndex = unformattedValue.LastIndexOf('\\');
			if (slashIndex > -1)
			{
				unformattedValue = unformattedValue.Substring(slashIndex + 1);
			}
			int dotIndex = unformattedValue.LastIndexOf('@');
			if (dotIndex > -1)
			{
				unformattedValue = unformattedValue.Substring(dotIndex + 1);
			}
			unformattedValue = unformattedValue.Replace('.', ' ');
			return unformattedValue;
		}

		/// <summary>
		/// <para>Returns username with domain information stripped out</para>
		/// </summary>
		/// <param name="unformattedValue">Username or Username Plus Domain.</param>
		/// <returns>returns Username without Domain extension.</returns>
		/// <example>
		///   <code>
		/// 	  "ACS/Vader.Darth"       -> "Vader.Darth"
		///     "Vader.Darth@esops.com" -> "Vader.Darth"
		///     "Vader.Darth"           -> "Vader.Darth"
		///   </code>
		/// </example>
		public static string FormatUsername(object unformattedValue)
		{
			return FormatUsername(Parser.ToString(unformattedValue));	
		}
		#endregion

		#region Truncate();
		/// <summary>
		/// Constrains the supplied text to a maximum number of characters and truncates any characters beyond the maximum length.
		/// <example>
		///  Truncate("Truncate Me", 5) --> "Trunc"
		///  Truncate("Truncate Me", 50) --> "Truncate Me"
		/// </example>
		/// </summary>
		/// <param name="text">The text to be constrained</param>
		/// <param name="maxLength">The maximum number of characters to allow before truncation.</param>
		/// <returns>the text truncated to the maximum length, or the original text as a string if less than or equal to the maximum length.</returns>
		public static string Truncate(object text, int maxLength)
		{
			return Truncate(text, maxLength, null);
		}
		/// <summary>
		/// Constrains the supplied text to a maximum number of characters and truncates any characters beyond the maximum length.
		/// <example>
		///  Truncate("Truncate Me", 5, false) --> "Trunc"
		///  Truncate("Truncate Me", 50, false) --> "Truncate Me"
		///  Truncate("Truncate Me", 5, true) --> "Trunc..."
		///  Truncate("Truncate Me", 50, true) --> "Truncate Me"
		/// </example>
		/// </summary>
		/// <param name="text">The text to be constrained</param>
		/// <param name="maxLength">The maximum number of characters to allow before truncation.</param>
		/// <param name="addEllipsis">If true, appends "..." to any truncated text values.</param>
		/// <returns>the text truncated to the maximum length, or the original text as a string if less than or equal to the maximum length.</returns>
		public static string Truncate(object text, int maxLength, bool addEllipsis)
		{
			return Truncate(text, maxLength, "...");
		}
		/// <summary>
		/// Constrains the supplied text to a maximum number of characters and truncates any characters beyond the maximum length, appending the supplied limited text suffix.
		/// <example>
		///  Truncate("Truncate Me", 5, "---") --> "Trunc---"
		///  Truncate("Truncate Me", 50, "---") --> "Truncate Me"
		/// </example>
		/// </summary>
		/// <param name="text">The text to be constrained</param>
		/// <param name="maxLength">The maximum number of characters to allow before truncation, including the number of characters in the supplied suffix.</param>
		/// <param name="limitedTextSuffix">A suffix to be attached to truncated text strings, such as "...".</param>
		/// <returns>the text truncated to the maximum length, or the original text as a string if less than or equal to the maximum length.</returns>
		public static string Truncate(object text, int maxLength, string limitedTextSuffix)
		{
			string textString = Parser.ToString(text);
			
			if (!string.IsNullOrEmpty(textString))
			{
				if (string.IsNullOrEmpty(limitedTextSuffix))
				{
					limitedTextSuffix = String.Empty;
				}
				if (maxLength > textString.Length || maxLength < limitedTextSuffix.Length)
				{
					return textString;
				}
				return textString.Substring(0, maxLength).TrimEnd() + limitedTextSuffix;
			}
			return textString;
		}
		#endregion
	}
}
