using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ACASLibraries
{
	/// <summary>
	/// The Parser class includes functions that will parse or convert any object into a simple datatype.
	/// </summary>
	public static class Parser
	{
		#region ToDouble();
		/// <summary>
		///	Returns a "double" version of a provided object.
		/// </summary>
		/// <param name="UnparsedValue">The object to be converted to a "double."</param>
		/// <returns>"double" version of the provided object.  If UnparsedValue cannot be converted, 0.0 will be returned.</returns>
		/// <example>
		/// <code>
		/// ToDouble("123.12") -> 123.12
		/// ToDouble("123") -> 123.0
		/// ToDouble(null) -> 0.0
		/// ToDouble("hello world") -> 0.0
		/// </code>
		/// </example>
		public static double ToDouble(object UnparsedValue)
		{
			if (UnparsedValue != null)
			{
				double dValue;
				if (UnparsedValue.GetType() == typeof(bool))
				{
					dValue = Convert.ToDouble(UnparsedValue);
				}
				else
				{
					double.TryParse(StringUtility.NumberGroupSeparatorsOff(UnparsedValue.ToString()), out dValue);
				}
				return dValue;
			}
			else
			{
				return 0;
			}
		}
		/// <summary>
		///	Returns a "double" version of a provided object.
		/// </summary>
		/// <param name="UnparsedValue">The object to be converted to a "double."</param>
		/// <param name="DefaultValue">Value to be returned in the event the UnparsedValue is null, an empty string, or cannot be parsed.</param>
		/// <returns>"double" version of the provided object.  If UnparsedValue cannot be converted, DefaultValue.</returns>
		/// <example>
		/// <code>
		/// ToDouble("123.12") -> 123.12
		/// ToDouble("123") -> 123.0
		/// ToDouble(null) -> 0.0
		/// ToDouble("hello world") -> 0.0
		/// </code>
		/// </example>
		public static object ToDouble(object UnparsedValue, object DefaultValue)
		{
			if(UnparsedValue != null && UnparsedValue.ToString().Trim() != String.Empty)
			{
				double dOutput;
				if(double.TryParse(UnparsedValue.ToString(), out dOutput))
				{
					return dOutput;
				}
			}
			return DefaultValue;
		}
		#endregion

		#region ToDecimal();
		/// <summary>
		///	Returns a "decimal" version of a provided object.
		/// </summary>
		/// <param name="UnparsedValue">The object to be converted to a "decimal."</param>
		/// <returns>"decimal" version of the provided object.  If UnparsedValue cannot be converted, 0.0 will be returned.</returns>
		/// <example>
		/// <code>
		/// ToDecimal("1,234.12") - > 1234.12
		/// ToDecimal("123.12") -> 123.12
		/// ToDecimal("123") -> 123.0
		/// ToDecimal(null) -> 0.0
		/// ToDecimal("hello world") -> 0.0
		/// </code>
		/// </example>
		public static decimal ToDecimal(object UnparsedValue)
		{
			if (UnparsedValue != null)
			{
				decimal dValue = 0;
				decimal.TryParse(StringUtility.NumberGroupSeparatorsOff(UnparsedValue.ToString()), out dValue);
				return dValue;
			}
			else
			{
				return 0;
			}
		}
		/// <summary>
		///	Returns a "decimal" version of a provided object.
		/// </summary>
		/// <param name="UnparsedValue">The object to be converted to a "decimal."</param>
		/// <param name="DefaultValue">Value to be returned in the event the UnparsedValue is null, an empty string, or cannot be parsed.</param>
		/// <returns>"decimal" version of the provided object.  If UnparsedValue cannot be converted, DefaultValue will be returned.</returns>
		/// <example>
		/// <code>
		/// ToDecimal("1,234.12") - > 1234.12
		/// ToDecimal("123.12") -> 123.12
		/// ToDecimal("123") -> 123.0
		/// ToDecimal(null) -> 0.0
		/// ToDecimal("hello world") -> 0.0
		/// </code>
		/// </example>
		public static object ToDecimal(object UnparsedValue, object DefaultValue)
		{
			if(UnparsedValue != null && UnparsedValue.ToString().Trim() != String.Empty)
			{
				decimal dOutput;
				if(decimal.TryParse(UnparsedValue.ToString(), out dOutput))
				{
					return dOutput;
				}
			}
			return DefaultValue;
		}
		#endregion

		#region ToInt();
		/// <summary>
		///	Returns an "int" version of a provided object.
		/// </summary>
		/// <param name="UnparsedValue">The object to be converted to a "int."</param>
		/// <returns>"int" version of the provided object.  If UnparsedValue cannot be converted, 0 will be returned.</returns>
		/// <example>
		/// <code>
		/// ToInt("123.12") -> 123
		/// ToInt("123.99999") -> 123
		/// ToInt("123") -> 123
		/// ToInt(null) -> 0
		/// ToInt("hello world") -> 0
		/// </code>
		/// </example>
		public static int ToInt(object UnparsedValue)
		{
			if (UnparsedValue != null)
			{
				int iValue;
				int.TryParse(ToDouble(UnparsedValue).ToString("0"), out iValue);
				return iValue;
			}
			else
			{
				return 0;
			}
		}
		/// <summary>
		///	Returns an "int" version of a provided object.
		/// </summary>
		/// <param name="UnparsedValue">The object to be converted to a "int."</param>
		/// <returns>"int" version of the provided object.  If UnparsedValue cannot be converted, DefaultValue will be returned.</returns>
		/// <param name="DefaultValue">Value to be returned in the event the UnparsedValue is null, an empty string, or cannot be parsed.</param>
		/// <example>
		/// <code>
		/// ToInt("123.12") -> 123
		/// ToInt("123.99999") -> 123
		/// ToInt("123") -> 123
		/// ToInt(null) -> 0
		/// ToInt("hello world") -> 0
		/// </code>
		/// </example>
		public static object ToInt(object UnparsedValue, object DefaultValue)
		{
			if(UnparsedValue != null && UnparsedValue.ToString().Trim() != String.Empty)
			{
				int iOutput;
				if(int.TryParse(UnparsedValue.ToString(), out iOutput))
				{
					return iOutput;
				}
			}
			return DefaultValue;
		}
		#endregion

		#region ToLong();
		/// <summary>
		///	Returns a "long" version of a provided object.
		/// </summary>
		/// <param name="UnparsedValue">The object to be converted to a "int."</param>
		/// <returns>"int" version of the provided object.  If UnparsedValue cannot be converted, 0 will be returned.</returns>
		/// <example>
		/// <code>
		/// ToLong("123.12") -> 123
		/// ToLong("123.99999") -> 123
		/// ToLong("123") -> 123
		/// ToLong(null) -> 0
		/// ToLong("hello world") -> 0
		/// </code>
		/// </example>
		public static long ToLong(object UnparsedValue)
		{
			if (UnparsedValue != null)
			{
				long lValue;
				long.TryParse(ToDouble(UnparsedValue).ToString("0"), out lValue);
				return lValue;
			}
			else
			{
				return 0;
			}
		}
		/// <summary>
		///	Returns a "long" version of a provided object.
		/// </summary>
		/// <param name="UnparsedValue">The object to be converted to a "int."</param>
		/// <returns>"int" version of the provided object.  If UnparsedValue cannot be converted, DefaultValue will be returned.</returns>
		/// <param name="DefaultValue">Value to be returned in the event the UnparsedValue is null, an empty string, or cannot be parsed.</param>
		/// <example>
		/// <code>
		/// ToLong("123.12") -> 123
		/// ToLong("123.99999") -> 123
		/// ToLong("123") -> 123
		/// ToLong(null) -> 0
		/// ToLong("hello world") -> 0
		/// </code>
		/// </example>
		public static object ToLong(object UnparsedValue, object DefaultValue)
		{
			if (UnparsedValue != null && UnparsedValue.ToString().Trim() != String.Empty)
			{
				long lOutput;
				if (long.TryParse(UnparsedValue.ToString(), out lOutput))
				{
					return lOutput;
				}
			}
			return DefaultValue;
		}
		#endregion

		#region ToString();
		/// <summary>
		///	Returns a "string" version of a provided object.
		/// </summary>
		/// <param name="UnparsedValue">The object to be converted to a "string."</param>
		/// <returns>"string" version of the provided object.  If UnparsedValue cannot be converted, "" will be returned.</returns>
		/// <example>
		/// <code>
		/// ToString(123) -> "123"
		/// ToString(true) -> "true"
		/// ToString(123.23) -> "123.23"
		/// ToString('c') - > "c"
		/// ToString(null) -> ""
		/// </code>
		/// </example>
		public static string ToString(object UnparsedValue)
		{
			if(UnparsedValue != null)
			{
				return UnparsedValue.ToString();
			}
			else
			{
				return "";
			}
		}
		/// <summary>
		///	Returns a "string" version of a provided object.
		/// </summary>
		/// <param name="UnparsedValue">The object to be converted to a "string."</param>
		/// <param name="DefaultValue">Value to be returned in the event the UnparsedValue is null, an empty string, or cannot be parsed.</param>
		/// <returns>"string" version of the provided object.  If UnparsedValue cannot be converted, DefaultValue will be returned.</returns>
		/// <example>
		/// <code>
		/// ToString(123) -> "123"
		/// ToString(true) -> "true"
		/// ToString(123.23) -> "123.23"
		/// ToString('c') - > "c"
		/// ToString(null) -> ""
		/// </code>
		/// </example>
		public static object ToString(object UnparsedValue, object DefaultValue)
		{
			if(UnparsedValue != null)
			{
				return UnparsedValue.ToString();
			}
			else
			{
				return DefaultValue;
			}
		}
		#endregion

		#region ToFloat();
		/// <summary>
		/// Returns a "float" version of a provided object.
		/// </summary>
		/// <param name="UnparsedValue">The object to be converted to a "float".</param>
		/// <returns>The "float" version of the provided object.  If the UnparsedValue cannot be converted, DefaultValue will be returned.</returns>
		public static float ToFloat(object UnparsedValue)
		{
			if(UnparsedValue != null)
			{
				float fValue;
				float.TryParse(StringUtility.NumberGroupSeparatorsOff(UnparsedValue.ToString()), out fValue);
				return fValue;
			}
			else
			{
				return 0;
			}
		}
		/// <summary>
		/// Returns a "float" version of a provided object.
		/// </summary>
		/// <param name="UnparsedValue">The object to be converted to a "float".</param>
		/// <param name="DefaultValue">Value to be returned in the event the UnparsedValue is null, an empty string, or cannot be parsed.</param>
		/// <returns>The "float" version of the provided object.  If the UnparsedValue cannot be converted, 0 will be returned.</returns>
		public static object ToFloat(object UnparsedValue, object DefaultValue)
		{
			if(UnparsedValue != null && UnparsedValue.ToString().Trim() != String.Empty)
			{
				float fOutput;
				if(float.TryParse(UnparsedValue.ToString(), out fOutput))
				{
					return fOutput;
				}
			}
			return DefaultValue;
		}
		#endregion

		#region ToBool();
		/// <summary>
		/// Returns a boolean version of the supplied object.  If the value of the object in string form is a number not equal to 0 or &quot;true&quot;, &quot;on&quot;, or &quot;yes&quot; (in any case) the return value will be true, otherwise false will be returned.
		/// </summary>
		/// <param name="UnparsedValue">The object to be converted to a float</param>
		/// <returns>A boolean representation of the UnparsedValue</returns>
		public static bool ToBool(object UnparsedValue)
		{
			if(UnparsedValue != null && 
				(
				ToInt(UnparsedValue) != 0 || 
				UnparsedValue.ToString().Equals("TRUE", StringComparison.InvariantCultureIgnoreCase) || 
				UnparsedValue.ToString().Equals("ON", StringComparison.InvariantCultureIgnoreCase) || 
				UnparsedValue.ToString().Equals("YES", StringComparison.InvariantCultureIgnoreCase) 
				)
			)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		/// <summary>
		/// Returns a boolean version of the supplied object.  If the value of the object in string form is a number not equal to 0 or &quot;true&quot;, &quot;on&quot;, or &quot;yes&quot; (in any case) the return value will be true, otherwise false will be returned as long as UnparsedValue is not equal to null or an empty string.
		/// </summary>
		/// <param name="UnparsedValue">The object to be converted to a float</param>
		/// <param name="DefaultValue">Value to be returned in the event the UnparsedValue is null, an empty string, or cannot be parsed.</param>
		/// <returns>A boolean representation of the UnparsedValue</returns>
		public static object ToBool(object UnparsedValue, object DefaultValue)
		{
			if(UnparsedValue != null && UnparsedValue.ToString().Trim() != String.Empty)
			{
				return Parser.ToBool(UnparsedValue);
			}
			return DefaultValue;
		}
		#endregion

		#region ToBit();
		/// <summary>
		/// 
		/// </summary>
		/// <param name="UnparsedValue"></param>
		/// <param name="DefaultValue"></param>
		/// <returns></returns>
		public static int ToBit(object UnparsedValue, int DefaultValue)
		{
			int iReturnValue = ((DefaultValue != 0) ? 1 : 0);
			if(UnparsedValue != null)
			{
				if(UnparsedValue.GetType().ToString() == "System.Boolean")
				{
					if(Parser.ToBool(UnparsedValue) == true)
					{
						iReturnValue = 1;
					}
				}
				else
				{
					iReturnValue = ((Parser.ToInt(UnparsedValue) != 0) ? 1 : 0);
				}
			}
			return iReturnValue;
		}

		/// <summary>
		/// defaults to 0 if null is passed
		/// </summary>
		/// <param name="UnparsedValue"></param>
		/// <returns></returns>
		public static int ToBit(object UnparsedValue)
		{
			return ToBit(UnparsedValue, 0);
		}
		#endregion

		#region ToChar();
		/// <summary>
		/// 
		/// </summary>
		/// <param name="UnparsedValue"></param>
		/// <returns></returns>
		public static char ToChar(object UnparsedValue)
		{
			if(UnparsedValue != null)
			{
				char cValue;
				char.TryParse(UnparsedValue.ToString(), out cValue);
				return cValue;
			}
			else
			{
				return ' ';
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="UnparsedValue"></param>
		/// <param name="DefaultValue">Value to be returned in the event the UnparsedValue is null, an empty string, or cannot be parsed.</param>
		/// <returns></returns>
		public static object ToChar(object UnparsedValue, object DefaultValue)
		{
			if(UnparsedValue != null)
			{
				char cValue;
				if(char.TryParse(UnparsedValue.ToString(), out cValue))
				{
					return cValue;
				}
			}
			return DefaultValue;
		}
		#endregion

		#region ToDateTime();
		/// <summary>
		/// Convers the supplied object to a DateTime.  Returns 1/1/1900 as the date if the supplied object cannot be parsed.
		/// </summary>
		/// <param name="UnparsedValue"></param>
		/// <returns>Returns a DateTime value of the supplied object. Returns 1/1/1900 as the date if the supplied object cannot be parsed.</returns>
		public static DateTime ToDateTime(object UnparsedValue)
		{
			if(UnparsedValue != null)
			{
				DateTime dtOutput;
				if(DateTime.TryParse(UnparsedValue.ToString(), out dtOutput))
				{
					return dtOutput;
				}
				else
				{
					return DateTime.Parse("1/1/1900");
				}
			}
			else
			{
				return DateTime.Parse("1/1/1900");
			}
		}
		/// <summary>
		/// Convers the supplied object to a DateTime.  Returns 1/1/1900 as the date if the supplied object cannot be parsed.
		/// </summary>
		/// <param name="UnparsedValue"></param>
		/// <param name="DefaultValue">Value to be returned in the event the UnparsedValue is null, an empty string, or cannot be parsed.</param>
		/// <returns>Returns a DateTime value of the supplied object. Returns DefaultValue if the supplied object cannot be parsed.</returns>
		public static object ToDateTime(object UnparsedValue, object DefaultValue)
		{
			if(UnparsedValue != null && UnparsedValue.ToString().Trim() != String.Empty)
			{
				DateTime dtOutput;
				if(DateTime.TryParse(UnparsedValue.ToString(), out dtOutput))
				{
					return dtOutput;
				}
			}
			return DefaultValue;
		}
		#endregion

		#region ToEnum();
		/// <summary>
		/// Converts the string reprensentation of an enum value to its enum equivalent. This method is case-insensitive.
		/// </summary>
		/// <typeparam name="T">The type of the enum to be parsed</typeparam>
		/// <param name="value">The string value of the enum value to be parsed</param>
		/// <returns>The parsed enum value</returns>
		public static T ToEnum<T>(object value) where T : struct {
			if(value.GetType() == typeof(T)) {
				return (T)value;
			} else {
				return (T)Enum.Parse(typeof(T),value.ToString(),true);
			}
		}
		/// <summary>
		/// Converts the string reprensentation of an enum value to its enum equivalent.  Returns the defaultValue if the conversion in unsuccessful. This method is case-insensitive.
		/// </summary>
		/// <typeparam name="T">The type of the enum to be parsed</typeparam>
		/// <param name="value">The string value of the enum value to be parsed</param>
		/// <param name="defaultValue">The default value to be returned if the supplied value cannot be parsed</param>
		/// <returns>The parsed enum value if successful or the default value if unsuccessful</returns>
		public static T ToEnum<T>(object value, T defaultValue) where T : struct {
			if(value != null) {
				if(value.GetType() == typeof(T)) {
					return (T)value;
				} else {
					T result;
					if(Enum.TryParse<T>(value.ToString(), out result)) {
						return result;
					} else {
						return defaultValue;
					}
				}
			} else {
				return defaultValue;
			}
		}
		#endregion

		#region IsNumeric();
		/// <summary>
		/// Determines if the supplied Value's string representation is considered numeric.
		/// <para>Return false for nulls, empty strings, percentages, currency formats, scientific notation, and other notations.</para>
		/// <example>123</example>
		/// <example>123.123</example>
		/// <example>-123</example>
		/// <example>-123.123</example>
		/// </summary>
		/// <param name="Value">An object</param>
		/// <returns>Returns true if Value's string representation is numeric.</returns>
		public static bool IsNumeric(object Value)
		{
			// C# equivalent to VB IsNumeric()
			if(Value != null && Value.ToString().Length > 0)
			{
				return new Regex(@"^(-?[0-9]+(\.[0-9]*)?)$").IsMatch(Value.ToString());
			}
			else
			{
				return false;
			}
		}
		#endregion

		#region IsDBNull();
		/// <summary>
		/// Returns true if the supplied object is null or DBNull.Value
		/// </summary>
		/// <param name="value">The value to be tested</param>
		/// <returns>True if the supplied object is null or DBNull.Value, otherwise false.</returns>
		public static bool IsDBNull(object value)
		{
			if(value == null || value == DBNull.Value)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		#endregion

		#region EnumToInt();
		/// <summary>
		/// Returns the int value assigned to an enum member.
		/// </summary>
		/// <typeparam name="TEnum">Enum type where enum values are assigned int values.</typeparam>
		/// <param name="value">The enum value to be converted.</param>
		/// <returns>The int value of the enum member.</returns>
		public static int EnumToInt<TEnum>(TEnum value) where TEnum : struct {
			return (int)Enum.Parse(typeof(TEnum), Enum.GetName(typeof(TEnum), value));
		}
		#endregion
	}
}
