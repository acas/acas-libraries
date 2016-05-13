using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Specialized;

namespace ACASLibraries
{
	/// <summary>
	/// The DatabaseUtility class includes functions for use in working with database objects. 
	/// For a more complete data access implementation to be used in large projects, see <see cref="DataAccess"/>.
	/// </summary>
	public class DatabaseUtility
	{
		#region SqlEscape();
		/// <summary>
		/// The SqlEscape() method will properly escape single quotes for use in a SQL statement.  This method is Microsoft SQL Server compatible.
		/// </summary>
		/// <param name="UnescapedValue">Any object supporting the ToString() method.</param>
		/// <returns>The escaped string.  If the UnescapedValue is NULL, an empty non-NULL string is returned.</returns>
		/// <remarks>This is obsolete, but left here in case it's being used elsewhere. When this version of ACASLibraries
		/// is pulled into an application using this, it should throw an error.</remarks>
		[Obsolete("Please don't use this. It encourages bad practice and shouldn't be necessary.", true)]
		public static string SqlEscape(object UnescapedValue)
		{
			//escape a value so that it can be appended within a SQL statement
			string sOutput = "";
			if(UnescapedValue != null && UnescapedValue.ToString().Length > 0)
			{
				sOutput = UnescapedValue.ToString().Replace("'", "''");
			}
			return sOutput;
		}
		#endregion

		#region DataReaderToStringDictionary();
		/// <summary>
		/// 
		/// </summary>
		/// <param name="DataReader"></param>
		/// <returns></returns>
		public static StringDictionary DataReaderToStringDictionary(IDataReader DataReader)
		{
			return DataReaderToStringDictionary(DataReader, null);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="DataReader"></param>
		/// <param name="StringDictionaryObject"></param>
		/// <returns></returns>
		public static StringDictionary DataReaderToStringDictionary(IDataReader DataReader, StringDictionary StringDictionaryObject)
		{
			//converts a single record query result (as a SqlDataReader) to a string dictionary
			//StringDictionary oDBFields = new StringDictionary();
			if(StringDictionaryObject == null)
			{
				StringDictionaryObject = new StringDictionary();
			}
			if(DataReader != null)
			{
				if(DataReader.Read())
				{
					for(int x = 0;x < DataReader.FieldCount;x++)
					{
						StringDictionaryObject[DataReader.GetName(x)] = (DataReader[x] != null ? DataReader[x].ToString() : "");
						if(ACASLibraries.Trace.IsEnabled)
						{
							ACASLibraries.Trace.Write("DataReaderToStringDictionary", DataReader.GetName(x) + " = " + StringDictionaryObject[DataReader.GetName(x)]);
						}
					}
				}
			}
			return StringDictionaryObject;
		}
		#endregion

		#region ToSqlDouble();
		/// <summary>
		/// Parses the supplied value as a double and returns System.DBNull if the value cannot be parsed.
		/// </summary>
		/// <param name="UnparsedValue">The value double to be parsed.</param>
		/// <returns>A double value or System.DBNull if the UnparsedValue is null or could not be parsed</returns>
		public static object ToSqlDouble(object UnparsedValue)
		{
			object oValue = DBNull.Value;
			try
			{
				oValue = double.Parse(UnparsedValue.ToString());
			}
			catch { }
			return oValue;
		}
		#endregion

		#region ToSqlFloat();
		/// <summary>
		/// Parses the supplied value as a float and returns System.DBNull if the value cannot be parsed.
		/// </summary>
		/// <param name="UnparsedValue">The value float to be parsed.</param>
		/// <returns>A float value or System.DBNull if the UnparsedValue is null or could not be parsed</returns>
		public static object ToSqlFloat(object UnparsedValue)
		{
			object oValue = DBNull.Value;
			if(UnparsedValue != null && UnparsedValue.ToString().Length > 0)
			{
				try
				{
					oValue = float.Parse(UnparsedValue.ToString());
				}
				catch { }
			}
			return oValue;
		}
		#endregion

		#region ToSqlDate();
		/// <summary>
		/// Parses the supplied value as a System.DateTime and returns System.DBNull if the value cannot be parsed.
		/// </summary>
		/// <param name="UnparsedValue">The value datetime to be parsed.</param>
		/// <returns>A System.DateTime value or System.DBNull if the UnparsedValue is null or could not be parsed</returns>
		public static object ToSqlDate(object UnparsedValue)
		{
			object oValue = DBNull.Value;
			if(UnparsedValue != null && UnparsedValue.ToString().Length > 0)
			{
				try
				{
					oValue = DateTime.Parse(UnparsedValue.ToString());
				}
				catch { }
			}
			return oValue;
		}
		#endregion

		#region ExecuteNonQueryStoredProcedure();
		/// <summary>
		/// <para>Executes a NonQuery style StoredProcedure (query not returning a dataset)  e.g. - insert, update, delete</para>
		/// </summary>
		/// <param name="oOpenSqlConnection">An open sql connection to be used for the stored procedure call</param>
		/// <param name="sStoredProcedureName">Name of the stored procedure to be executed</param>
		/// <param name="sArgumentNameValuePairs">SQL Parameter Name (including @), and Parameter Value</param>
		/// <returns>returns number of rows affected or negitive error code if error occurs</returns>
		public static int ExecuteNonQueryStoredProcedure(IDbConnection oOpenSqlConnection, string sStoredProcedureName, params object[] sArgumentNameValuePairs)
		{
			int iNumberOfRowsAffected = -1;
			IDbCommand oCmd = oOpenSqlConnection.CreateCommand();
			oCmd.CommandText = sStoredProcedureName;
			oCmd.CommandType = CommandType.StoredProcedure;
			for (int iArgumentIndex = 1; iArgumentIndex < sArgumentNameValuePairs.Length; iArgumentIndex += 2)
			{
				IDbDataParameter p = oCmd.CreateParameter();
				p.Direction = ParameterDirection.Input;
				p.ParameterName = sArgumentNameValuePairs[iArgumentIndex - 1].ToString();
				p.Value = sArgumentNameValuePairs[iArgumentIndex];
				oCmd.Parameters.Add(p);
			}
			iNumberOfRowsAffected = oCmd.ExecuteNonQuery();
			oCmd.Dispose();
			oCmd = null;
			return iNumberOfRowsAffected;
		}
		#endregion

		#region ExecuteNonQueryStoredProcedureWithReturnedIdentity();
		/// <summary>
		/// <para>Executes a NonQuery style StoredProcedure (query not returning a dataset)  e.g. - insert, update, delete</para>
		/// </summary>
		/// <param name="oOpenSqlConnection">An open sql connection to be used for the stored procedure call</param>
		/// <param name="sStoredProcedureName">Name of the stored procedure to be executed</param>
		/// <param name="sArgumentNameValuePairs">SQL Parameter Name (including @), and Parameter Value</param>
		/// <returns>returns the identity created for a new row in the database</returns>
		public static int ExecuteNonQueryStoredProcedureWithReturnedIdentity(IDbConnection oOpenSqlConnection, string sStoredProcedureName, params object[] sArgumentNameValuePairs)
		{
			int iIdentity = 0;
			IDbCommand oCmd = oOpenSqlConnection.CreateCommand();
			oCmd.CommandText = sStoredProcedureName;
			oCmd.CommandType = CommandType.StoredProcedure;
			for (int iArgumentIndex = 1; iArgumentIndex < sArgumentNameValuePairs.Length; iArgumentIndex += 2)
			{
				IDbDataParameter p = oCmd.CreateParameter();
				p.Direction = ParameterDirection.Input;
				p.ParameterName = sArgumentNameValuePairs[iArgumentIndex - 1].ToString();
				p.Value = sArgumentNameValuePairs[iArgumentIndex];
				oCmd.Parameters.Add(p);
			}
			IDataReader oDR = oCmd.ExecuteReader();
			if (oDR.Read())
			{
				iIdentity = ACASLibraries.Parser.ToInt(oDR[0]);
			}
			oDR.Close();
			oDR = null;
			oCmd.Dispose();
			oCmd = null;
			return iIdentity;
		}
		#endregion

		#region ExecuteNonQueryStoredProcedureWithinTransaction();
		/// <summary>
		///	Executes a non-query stored procedure within the provided transaction against a database, e.g. - insert, update, delete.
		/// </summary>
		/// <param name="oOpenSqlConnection">An open database connection</param>
		/// <param name="oSqlTransaction">Transaction in which to execute the provided stored procedure</param>
		/// <param name="sStoredProcedureName">Name of the stored procedure to execute</param>
		/// <param name="a2oArgumentNameValuePairs">A key/value pair for the stored procedure argument; key must include @.</param>
		/// <returns>the number of rows affected by the query, or when problems occur an SQLException will be raised.</returns>
		/// <example>
		/// <code>
		/// iNumberOfRecords = ExecuteNonQueryStoredProcedure(
		///			oMyOpenConnection, 
		///			oMyTransaction,
		///			"sp_SetMyInformationInTable", 
		///			"@sArgument1", 
		///			"my sample string", 
		///			"@iArgument2", 
		///			42
		///		);
		/// </code>
		/// </example>
		public static int ExecuteNonQueryStoredProcedureWithinTransaction(IDbConnection oOpenSqlConnection, IDbTransaction oSqlTransaction, string sStoredProcedureName, params object[] a2oArgumentNameValuePairs) {
			int iNumberOfRowsAffected = -1;
			IDbCommand oCmd = oOpenSqlConnection.CreateCommand();
			oCmd.CommandText = sStoredProcedureName;
			oCmd.CommandType = CommandType.StoredProcedure;
			oCmd.Transaction = oSqlTransaction;
			for (int iArgumentIndex = 1; iArgumentIndex < a2oArgumentNameValuePairs.Length; iArgumentIndex += 2)
			{
				IDbDataParameter p = oCmd.CreateParameter();
				p.Direction = ParameterDirection.Input;
				p.ParameterName = a2oArgumentNameValuePairs[iArgumentIndex - 1].ToString();
				p.Value = a2oArgumentNameValuePairs[iArgumentIndex];
				oCmd.Parameters.Add(p);
			}
			iNumberOfRowsAffected = oCmd.ExecuteNonQuery();
			oCmd.Dispose();
			oCmd = null;
			return iNumberOfRowsAffected;
		}
		#endregion

		#region ExecuteNonQueryStoredProcedureWithReturnedIdentityWithinTransaction();
		/// <summary>
		///	Executes a non-query stored procedure within the provided transaction against a database, e.g. - insert, update, delete.
		/// </summary>
		/// <param name="oOpenSqlConnection">An open database connection</param>
		/// <param name="oSqlTransaction">Transaction in which to execute the provided stored procedure</param>
		/// <param name="sStoredProcedureName">Name of the stored procedure to execute</param>
		/// <param name="a2oArgumentNameValuePairs">A key/value pair for the stored procedure argument; key must include @.</param>
		/// <returns>the number of rows affected by the query, or when problems occur an SQLException will be raised.</returns>
		/// <example>
		/// <code>
		/// iNumberOfRecords = ExecuteNonQueryStoredProcedureWithReturnedIdentityWithinTransaction(
		///			oMyOpenConnection,
		///			oMyTransaction,
		///			"sp_SetMyInformationInTable", 
		///			"@sArgument1", 
		///			"my sample string", 
		///			"@iArgument2", 
		///			42
		///		);
		/// </code>
		/// </example>
		public static int ExecuteNonQueryStoredProcedureWithReturnedIdentityWithinTransaction(IDbConnection oOpenSqlConnection, IDbTransaction oSqlTransaction, string sStoredProcedureName, params object[] a2oArgumentNameValuePairs)
		{
			int iIdentity = 0;
			IDbCommand oCmd = oOpenSqlConnection.CreateCommand();
			oCmd.CommandText = sStoredProcedureName;
			oCmd.CommandType = CommandType.StoredProcedure;
			oCmd.Transaction = oSqlTransaction;
			for (int iArgumentIndex = 1; iArgumentIndex < a2oArgumentNameValuePairs.Length; iArgumentIndex += 2)
			{
				IDbDataParameter p = oCmd.CreateParameter();
				p.Direction = ParameterDirection.Input;
				p.ParameterName = a2oArgumentNameValuePairs[iArgumentIndex - 1].ToString();
				p.Value = a2oArgumentNameValuePairs[iArgumentIndex];
				oCmd.Parameters.Add(p);
			}
			IDataReader oDR = oCmd.ExecuteReader();
			if (oDR.Read())
			{
				iIdentity = ACASLibraries.Parser.ToInt(oDR[0]);
			}
			oDR.Close();
			oDR = null;
			oCmd.Dispose();
			oCmd = null;
			return iIdentity;
		}
		#endregion

		#region GetDataReaderFromStoredProcedure();
		/// <summary>
		///	Sets up DataReader from a provided stored procdedure.
		/// </summary>
		/// <param name="oOpenSqlConnection">An open database connection</param>
		/// <param name="sStoredProcedureName">Name of the stored procedure from which to retrieve the DataSet</param>
		/// <param name="a2oArgumentNameValuePairs">A key/value pair for the stored proceedure argument; key must include @.</param>
		/// <returns>DataReader retrieved from the stored prcedure, or when problems occur an SQLException will be raised; You MUST free the DataSet yourself or call DataSetCleanUp()</returns>
		/// <example>
		/// <code>
		/// SqlConnection oConn = new SqlConnection([[[sConnectionString]]]);
		/// oConn.Open();
		/// ...
		/// oDataReader = GetDataReaderFromStoredProcedure(oMyOpenConnection, "sp_SetMyInformationInTable", "@sArgument1", "my sample string", "@iArgument2", 42);
		/// ...
		/// oConn.Dispose();
		/// oConn = null;
		/// </code>
		/// </example>
		public static IDataReader GetDataReaderFromStoredProcedure(IDbConnection oOpenSqlConnection, string sStoredProcedureName, params object[] a2oArgumentNameValuePairs)
		{
			IDbCommand oCmd = oOpenSqlConnection.CreateCommand();
			oCmd.CommandText = sStoredProcedureName;
			oCmd.CommandType = CommandType.StoredProcedure;
			for (int iArgumentIndex = 1; iArgumentIndex < a2oArgumentNameValuePairs.Length; iArgumentIndex += 2) {
				IDbDataParameter p = oCmd.CreateParameter();
				p.Direction = ParameterDirection.Input;
				p.ParameterName = a2oArgumentNameValuePairs[iArgumentIndex - 1].ToString();
				p.Value = a2oArgumentNameValuePairs[iArgumentIndex];
				oCmd.Parameters.Add(p);
			}
			IDataReader oDR = oCmd.ExecuteReader();
			oCmd.Dispose();
			oCmd = null;
			return oDR;
		}
		#endregion

		#region GetDataTableFromStoredProcedure();
		/// <summary>
		///	Creates and populates DataSet from a stored procdedure.
		/// </summary>
		/// <param name="oOpenSqlConnection">An open database connection</param>
		/// <param name="sStoredProcedureName">Name of the stored procedure from which to retrieve the DataSet</param>
		/// <param name="a2oArgumentNameValuePairs">A key/value pair for the stored proceedure argument; key must include @.</param>
		/// <returns>DataSet retrieved from the stored prcedure, or when problems occur an SQLException will be raised; You MUST free the DataSet yourself or call DataSetCleanUp()</returns>
		/// <example>
		/// <code>
		/// SqlConnection oConn = new SqlConnection([[[sConnectionString]]]);
		/// oConn.Open();
		/// ...
		/// oDataSet = GetDataSetFromStoredProcedure(oMyOpenConnection, "sp_SetMyInformationInTable", "@sArgument1", "my sample string", "@iArgument2", 42);
		/// ...
		/// oConn.Dispose();
		/// oConn = null;
		/// </code>
		/// </example>
		public static DataTable GetDataTableFromStoredProcedure(SqlConnection oOpenSqlConnection, string sStoredProcedureName, params object[] a2oArgumentNameValuePairs)
		{
			SqlCommand oCmd = new SqlCommand(sStoredProcedureName, oOpenSqlConnection);
			oCmd.CommandType = CommandType.StoredProcedure;
			for (int iArgumentIndex = 1; iArgumentIndex < a2oArgumentNameValuePairs.Length; iArgumentIndex += 2)
			{
				oCmd.Parameters.AddWithValue(a2oArgumentNameValuePairs[iArgumentIndex - 1].ToString(), a2oArgumentNameValuePairs[iArgumentIndex]);
			}
			SqlDataAdapter oDA = new SqlDataAdapter(oCmd);
			DataTable oDT = new DataTable();
			oDA.Fill(oDT);
			oDA.Dispose();
			oDA = null;
			oCmd.Dispose();
			oCmd = null;
			return oDT;
		}
		#endregion

		#region SqlDataReaderToNameValueCollection();
		/// <summary>
		/// Iterates through the first record in the supplied SqlDataReader and returns a NameValueCollection with each column as a key and each column's value as its key's value.
		/// </summary>
		/// <param name="DataReader">An open SqlDataReader</param>
		/// <returns>A NameValueCollection of the DataReader's first record</returns>
		public static NameValueCollection SqlDataReaderToNameValueCollection(IDataReader DataReader)
		{
			return SqlDataReaderToNameValueCollection(DataReader, null);
		}
		/// <summary>
		/// Iterates through the first record in the supplied SqlDataReader and returns a NameValueCollection with each column as a key and each column's value as its key's value.
		/// </summary>
		/// <param name="DataReader">An open SqlDataReader</param>
		/// <param name="NameValueCollection">An existing NameValueCollection to add values to.  Any existing keys will be overwritten.</param>
		/// <returns>A NameValueCollection of the DataReader's first record</returns>
		public static NameValueCollection SqlDataReaderToNameValueCollection(IDataReader DataReader, NameValueCollection NameValueCollection)
		{
			if (NameValueCollection == null)
			{
				NameValueCollection = new NameValueCollection();
			}

			if (DataReader != null && DataReader.Read())
			{
				for (int x = 0; x < DataReader.FieldCount; x++)
				{
					NameValueCollection[DataReader.GetName(x)] = Parser.ToString(DataReader[x]);
				}
			}
			return NameValueCollection;
		}
		#endregion
	}
}
