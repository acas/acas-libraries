using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data;

namespace ACASLibraries
{
	/// <summary>
	/// A (mostly) complete data access solution for use in an application. 
	/// Provides methods to get and set SQL data using mostly datatables. 
	/// Must be subclassed in the consuming application, where methods can be overridden or supplemented if necessary.
	/// For simpler, more ad-hoc data access needs, see <see cref="DatabaseUtility"/>.
	/// </summary>	
	public abstract class DataAccess
	{

		#region CreateConnection();
		/// <summary>
		/// This method must be implemented in the subclass inheriting DataAccess. It should return an open SqlConnection object pointing to the application's database.
		/// </summary>
		/// <returns>New open SqlConnection object</returns>
		public abstract SqlConnection CreateConnection();
		#endregion

		#region GetDataTable(); 			/// TODO - what happens in these methods if the proc returns no result sets? More than one?
		/// <summary>
		/// Execute a stored procedure inside an existing connection/transaction and return a single DataTable. The stored procedure should return exactly one result set. 		
		/// Optionally takes an array of SqlParameter objects and/or a timeout setting.
		/// </summary>
		/// <param name="conn">An open connection</param>
		/// <param name="transaction">An open transaction</param>
		/// <param name="procedureName">The name of the stored procedure to execute</param>
		/// <param name="parameters">An array of parameters to execute the stored procedure with.</param>
		/// <param name="timeout">The SqlCommand.CommandTimeout to wait before terminating the command.</param>
		/// <returns>DataTable object containing the resultset</returns>
		/// <exception cref="ACASLibrariesException">Any failure inside this method will throw an ACASLibrariesException containing all the details of the SqlCommand in addition to the standard Exception details.</exception>
		public DataTable GetDataTable(SqlConnection conn, SqlTransaction transaction, string procedureName, SqlParameter[] parameters = null, int? timeout = null)
		{

			SqlCommand cmd = null;
			DataTable dt = new DataTable();
			SqlDataReader sdr = null;
			try
			{
				cmd = conn.CreateCommand();
				cmd.Transaction = transaction;
				cmd.CommandType = CommandType.StoredProcedure;
				if (timeout.HasValue)
				{
					cmd.CommandTimeout = timeout.Value;
				}
				cmd.CommandText = procedureName;
				if (parameters != null)
				{
					cmd.Parameters.AddRange(parameters);
				}
				sdr = cmd.ExecuteReader();
				dt.Load(sdr);
			}
			catch (SqlException ex)
			{
				throw new ACASLibrariesException("An error occurred while retrieving data.", cmd, ex);
			}
			finally
			{
				if (sdr != null)
				{
					sdr.Close();
					sdr.Dispose();
				}
				if (cmd != null)
				{
					cmd.Dispose();
					cmd = null;
				}
			}

			return dt;
		}

		/// <summary>
		/// Execute a stored procedure with a specified IsolationLevel and return a single DataTable. The stored procedure should return exactly one result set. 		
		/// Optionally takes an array of SqlParameter objects and/or a timeout setting.
		/// </summary>
		/// <param name="isolationLevel">The isolation level to use for the transaction</param>
		/// <param name="procedureName">The name of the stored procedure to execute</param>
		/// <param name="parameters">An array of parameters to execute the stored procedure with.</param>
		/// <param name="timeout">The SqlCommand.CommandTimeout to wait before terminating the command.</param>
		/// <returns>DataTable object containing the resultset</returns>		
		/// <exception cref="ACASLibrariesException">Any failure inside this method will throw an ACASLibrariesException containing all the details of the SqlCommand in addition to the standard Exception details.</exception>
		public DataTable GetDataTable(IsolationLevel isolationLevel, string procedureName, SqlParameter[] parameters = null, int? timeout = null)
		{
			SqlConnection conn = null;
			SqlTransaction transaction = null;
			DataTable dt;
			try
			{
				conn = CreateConnection();
				transaction = conn.BeginTransaction(isolationLevel);
				dt = GetDataTable(conn, transaction, procedureName, parameters, timeout);
				transaction.Commit();
			}
			catch (Exception ex)
			{
				if (transaction != null)
				{
					transaction.Rollback();
				}
				throw new ACASLibrariesException("An error occurred while retrieving data.", ex);
			}
			finally
			{
				if (transaction != null)
				{
					transaction.Dispose();
					transaction = null;
				}
				if (conn != null)
				{
					conn.Close();
					conn.Dispose();
					conn = null;
				}
			}
			return dt;
		}

		/// <summary>
		/// Execute a stored procedure and return a single DataTable. The stored procedure should return exactly one result set. 		
		/// Optionally takes an array of SqlParameter objects and/or a timeout setting.
		/// Uses a transaction IsolationLevel of ReadCommitted.
		/// </summary>
		/// <param name="procedureName">The name of the stored procedure to execute</param>
		/// <param name="parameters">An array of parameters to execute the stored procedure with.</param>
		/// <param name="timeout">The SqlCommand.CommandTimeout to wait before terminating the command.</param>
		/// <returns>DataTable object containing the resultset</returns>
		/// <exception cref="ACASLibrariesException">Any failure inside this method will throw an ACASLibrariesException containing all the details of the SqlCommand in addition to the standard Exception details.</exception>
		public DataTable GetDataTable(string procedureName, SqlParameter[] parameters = null, int? timeout = null)
		{
			return GetDataTable(IsolationLevel.ReadCommitted, procedureName, parameters, timeout);
		}

		/// <summary>
		/// Execute a stored procedure and return a single DataTable. The stored procedure should return exactly one result set. 		
		/// Takes a single SqlParameter object. Uses IsolationLevel: ReadCommitted.
		/// </summary>
		/// <param name="procedureName">The name of the stored procedure to execute</param>
		/// <param name="parameter">A single parameter to execute the stored procedure with.</param>		
		/// <returns>DataTable object containing the resultset</returns>
		/// <exception cref="ACASLibrariesException">Any failure inside this method will throw an ACASLibrariesException containing all the details of the SqlCommand in addition to the standard Exception details.</exception>
		public DataTable GetDataTable(string procedureName, SqlParameter parameter)
		{
			return GetDataTable(procedureName, new SqlParameter[1] { parameter });
		}

		/// <summary>
		/// Execute a stored procedure inside an existing connection/transaction and return a single DataTable. The stored procedure should return exactly one result set. 		
		/// Takes a single SqlParameter object. Uses IsolationLevel: ReadCommitted.
		/// </summary>
		/// <param name="conn">An open connection</param>
		/// <param name="transaction">An open transaction</param>
		/// <param name="procedureName">The name of the stored procedure to execute</param>
		/// <param name="parameter">A single parameter to execute the stored procedure with.</param>
		/// <returns>DataTable object containing the resultset</returns>
		/// <exception cref="ACASLibrariesException">Any failure inside this method will throw an ACASLibrariesException containing all the details of the SqlCommand in addition to the standard Exception details.</exception>
		public DataTable GetDataTable(SqlConnection conn, SqlTransaction transaction, string procedureName, SqlParameter parameter)
		{
			return GetDataTable(conn, transaction, procedureName, new SqlParameter[1] { parameter });
		}


		#endregion

		#region GetDataSet();

		/// <summary>
		/// Execute a stored procedure inside a transaction and return a DataSet with multiple tables within it.
		/// Use this when the proc returns multiple result sets. Optionally takes an array of SqlParameter objects
		/// and a timeout. Uses IsolationLevel: ReadCommitted
		/// </summary>
		/// <param name="procedureName">The name of the stored procedure to execute</param>
		/// <param name="parameters">An array of SqlParameter objects</param>
		/// <param name="timeout">Number of milliseconds for the command timeout</param>
		/// <returns></returns>
		public DataSet GetDataSet(string procedureName, SqlParameter[] parameters = null, int? timeout = null)
		{
			SqlConnection conn = CreateConnection();
			SqlTransaction transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
			SqlCommand cmd = conn.CreateCommand();
			SqlDataAdapter da = null;
			DataSet ds = new DataSet(procedureName);
			try
			{
				cmd = conn.CreateCommand();
				cmd.Transaction = transaction;
				cmd.CommandType = CommandType.StoredProcedure;				
				if (timeout.HasValue)
				{
					cmd.CommandTimeout = timeout.Value;
				}
				cmd.CommandText = procedureName;
				if (parameters != null)
				{
					cmd.Parameters.AddRange(parameters);
				}

				da = new SqlDataAdapter(cmd);
				da.Fill(ds);
				transaction.Commit();
				return ds;
			}
			catch (Exception ex)
			{
				transaction.Rollback();
				throw new ACASLibrariesException("An error occurred while retrieving data.", ex);
			}
			finally
			{				
				if (cmd != null)
				{
					cmd.Dispose();
					cmd = null;
				}
				if (transaction != null)
				{
					transaction.Dispose();
					transaction = null;
				}
				if (conn != null)
				{
					conn.Close();
					conn.Dispose();
					conn = null;
				}
			}			
		}

		#endregion

		#region GetScalar

		/// <summary>
		/// Return a single scalar value from either a proc or a scalar-valued function. Uses IsolationLevel: ReadCommitted
		/// Optionally takes an array of parameters and/or a timeout value.
		/// </summary>
		/// <param name="conn">An open sql connection</param>
		/// <param name="transaction">An open transaction</param>
		/// <param name="sqlObjectName">The name of the proc/function</param>
		/// <param name="sqlObjectType">The type of the object (proc or function)</param>
		/// <param name="parameters">An array of input parameters to run the proc or function with</param>
		/// <param name="timeout">The SqlCommand.CommandTimeout to wait before terminating the command.</param>
		/// <returns>The result of the scalar valued function or the first column of the first row of the result set the proc returns.</returns>
		public object GetScalar(SqlConnection conn, SqlTransaction transaction, string sqlObjectName, SqlObjectType sqlObjectType, SqlParameter[] parameters = null, int? timeout = null)
		{
			SqlCommand cmd = null;
			DataTable dt = new DataTable();
			SqlDataReader sdr = null;
			object result;
			try
			{
				cmd = conn.CreateCommand();
				cmd.Transaction = transaction;
				cmd.CommandType = CommandType.StoredProcedure;
				if (timeout.HasValue)
				{
					cmd.CommandTimeout = timeout.Value;
				}
				cmd.CommandText = sqlObjectName;
				if (parameters != null)
				{
					cmd.Parameters.AddRange(parameters);
				}
				if (sqlObjectType == SqlObjectType.ScalarValuedFunction)
				{
					string returnParameterName = "@Result";
					//in case @Result is already used
					int i = 0;
					while (parameters.Any(p => p.ParameterName == returnParameterName))
					{
						returnParameterName = "@Result" + i.ToString();
						i++;
						//escape hatch in case someone is doing something crazy with parameter names
						if (i > 9)
						{
							throw new ACASLibrariesException("The stored procedure/scalar function you are trying to run has too many parameters with terribly uninformative names (@Result, @Result0, @Result1, @Result2...@Result9). Please write code that doesn't make me want to kill myself. Thanks.");
						}
					}
					cmd.Parameters.Add(new SqlParameter(returnParameterName, null) { Direction = ParameterDirection.ReturnValue });
					cmd.ExecuteNonQuery();
					result = cmd.Parameters[returnParameterName].Value;
				}
				else
				{
					sdr = cmd.ExecuteReader();
					dt.Load(sdr);
					result = (dt.Rows.Count == 0 ? null : dt.Rows[0].ItemArray[0]);
				}
				return result;

			}
			catch (SqlException ex)
			{
				throw new ACASLibrariesException("An error occurred while retrieving data.", cmd, ex);
			}
			finally
			{
				if (sdr != null)
				{
					sdr.Close();
					sdr.Dispose();
				}
				if (cmd != null)
				{
					cmd.Dispose();
					cmd = null;
				}
			}

		}

		/// <summary>
		/// Return a single scalar value from either a proc or a scalar-valued function. Uses IsolationLevel: ReadCommitted
		/// Optionally takes an array of parameters and/or a timeout value.
		/// </summary>
		/// <param name="sqlObjectName">The name of the proc/function</param>
		/// <param name="sqlObjectType">The type of the object (proc or function)</param>
		/// <param name="parameters">An array of input parameters to run the proc or function with</param>
		/// <param name="timeout">The SqlCommand.CommandTimeout to wait before terminating the command.</param>
		/// <returns>The result of the scalar valued function or the first column of the first row of the result set the proc returns.</returns>
		public object GetScalar(string sqlObjectName, SqlObjectType sqlObjectType, SqlParameter[] parameters = null, int? timeout = null)
		{
			SqlConnection conn = null;
			SqlTransaction transaction = null;
			object result;
			try
			{
				conn = CreateConnection();
				transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
				result = GetScalar(conn, transaction, sqlObjectName, sqlObjectType, parameters, timeout);
				transaction.Commit();
			}
			catch (Exception ex)
			{
				if (transaction != null)
				{
					transaction.Rollback();
				}
				throw new ACASLibrariesException("An error occurred while retrieving data.", ex);
			}
			finally
			{
				if (transaction != null)
				{
					transaction.Dispose();
					transaction = null;
				}
				if (conn != null)
				{
					conn.Close();
					conn.Dispose();
					conn = null;
				}
			}
			return result;
		}

		/// <summary>
		/// Return a single scalar value from either a proc or a scalar-valued function. Uses IsolationLevel: ReadCommitted
		/// Takes a single parameter and optionally a timeout value.
		/// </summary>
		/// <param name="sqlObjectName">The name of the proc/function</param>
		/// <param name="sqlObjectType">The type of the object (proc or function)</param>
		/// <param name="parameter">A single input parameter to run the proc or function with</param>
		/// <param name="timeout">The SqlCommand.CommandTimeout to wait before terminating the command.</param>
		/// <returns>The result of the scalar valued function or the first column of the first row of the result set the proc returns.</returns>
		public object GetScalar(string sqlObjectName, SqlObjectType sqlObjectType, SqlParameter parameter, int? timeout = null)
		{
			return GetScalar(sqlObjectName, sqlObjectType, new SqlParameter[1] { parameter }, timeout);
		}

		#endregion

		#region SetData();

		/// <summary>
		/// Execute a stored procedure that returns nothing with a specified IsolationLevel.
		/// Optionally takes an array of SqlParameter objects and/or a timeout setting.
		/// It requires connection and transaction objects to be supplied, allowing developers to manager their own transactions where necessary.
		/// </summary>
		/// <param name="isolationLevel">The isolation level to use for the transaction</param>
		/// <param name="procedureName">The name of the stored procedure to execute</param>
		/// <param name="parameters">A parameter to execute the stored procedure with</param>
		/// <param name="timeout">The SqlCommand.CommandTimeout to wait before terminating the command.</param>
		/// <exception cref="ACASLibrariesException">Any failure inside this method will throw an ACASLibrariesException containing all the details of the SqlCommand in addition to the standard Exception details.</exception>
		public void SetData(IsolationLevel isolationLevel, string procedureName, SqlParameter[] parameters = null, int? timeout = null)
		{
			SqlConnection conn = null;
			SqlTransaction transaction = null;
			try
			{
				conn = this.CreateConnection();
				transaction = conn.BeginTransaction(isolationLevel);
				SetData(conn, transaction, procedureName, parameters, timeout);
				transaction.Commit();
			}
			catch (Exception ex)
			{
				if (transaction != null)
				{
					transaction.Rollback();
				}
				throw new ACASLibrariesException("An error occurred while saving data.", ex);
			}
			finally
			{
				if (transaction != null)
				{
					transaction.Dispose();
					transaction = null;
				}
				if (conn != null)
				{
					conn.Close();
					conn.Dispose();
					conn = null;
				}
			}
		}

		/// <summary>
		/// Execute a stored procedure that returns nothing. 
		/// Optionally takes an array of SqlParameter objects and/or a timeout setting.
		/// Uses a serializable isolation level for the transaction.
		/// </summary>
		/// <param name="procedureName">The name of the stored procedure to execute</param>
		/// <param name="parameters">An array of parameters to execute the stored procedure with.</param>
		/// <param name="timeout">The SqlCommand.CommandTimeout to wait before terminating the command.</param>
		/// <exception cref="ACASLibrariesException">Any failure inside this method will throw an ACASLibrariesException containing all the details of the SqlCommand in addition to the standard Exception details.</exception>
		public void SetData(string procedureName, SqlParameter[] parameters = null, int? timeout = null)
		{
			SetData(IsolationLevel.Serializable, procedureName, parameters, timeout);
		}

		/// <summary>
		/// Execute a stored procedure that returns nothing. 
		/// Optionally takes an array of SqlParameter objects and/or a timeout setting.
		/// It requires connection and transaction objects to be supplied, allowing developers to manager their own transactions where necessary.
		/// </summary>
		/// <param name="conn">An open SqlConnection.</param>
		/// <param name="transaction">An open transaction</param>
		/// <param name="procedureName">The name of the stored procedure to execute</param>
		/// <param name="parameters">An array of parameters to execute the stored procedure with</param>
		/// <param name="timeout">The SqlCommand.CommandTimeout to wait before terminating the command.</param>
		/// /// <exception cref="ACASLibrariesException">Any failure inside this method will throw an ACASLibrariesException containing all the details of the SqlCommand in addition to the standard Exception details.</exception>
		public void SetData(SqlConnection conn, SqlTransaction transaction, string procedureName, SqlParameter[] parameters = null, int? timeout = null)
		{
			SqlCommand cmd = null;
			try
			{
				cmd = conn.CreateCommand();
				cmd.Transaction = transaction;
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.CommandText = procedureName;

				if (timeout != null)
				{
					cmd.CommandTimeout = timeout.Value;
				}

				if (parameters != null)
				{
					cmd.Parameters.AddRange(parameters);
				}
				cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				throw new ACASLibrariesException("An error occurred while saving data.", cmd, ex);
			}
			finally
			{
				if (cmd != null)
				{
					cmd.Dispose();
					cmd = null;
				}
			}
		}
		#endregion

		#region SetDataWithReader();
		/// <summary>
		/// Execute a stored procedure inside an existing connection/transaction and return a datareader. Optionally takes an array of SqlParameter objects.
		/// </summary>
		/// <param name="conn">An open connection</param>
		/// <param name="transaction">An open transaction</param>
		/// <param name="procedureName">The name of the stored procedure to execute</param>
		/// <param name="parameters">Optional array of parameters</param>
		/// <returns>Open SqlDataReader object containing the command result</returns>
		/// <exception cref="ACASLibrariesException">Any failure inside this method will throw an ACASLibrariesException containing all the details of the SqlCommand in addition to the standard Exception details.</exception>
		public SqlDataReader SetDataWithReader(SqlConnection conn, SqlTransaction transaction, string procedureName, SqlParameter[] parameters = null)
		{
			SqlCommand cmd = null;
			SqlDataReader dr = null;
			try
			{
				cmd = conn.CreateCommand();
				cmd.Transaction = transaction;
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.CommandText = procedureName;
				if (parameters != null)
				{
					cmd.Parameters.AddRange(parameters);
				}
				dr = cmd.ExecuteReader();
			}
			catch (SqlException ex)
			{
				throw new ACASLibrariesException("An error occurred while saving data.", cmd, ex);
			}
			finally
			{
				if (cmd != null)
				{
					cmd.Dispose();
					cmd = null;
				}
			}
			return dr;
		}
		#endregion

		/// <summary>
		/// Enum containing executable sql objects (procs, functions, etc) supported by DataAccess.
		/// </summary>
		public enum SqlObjectType 
		{ 
			/// <summary>
			/// SQL Stored Procedure
			/// </summary>
			StoredProcedure, 
			/// <summary>
			/// SQL Scalar Valued Function (as opposed to a table-valued function)
			/// </summary>
			ScalarValuedFunction 
		}
	}

}