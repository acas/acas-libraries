using System;
using System.Collections.Generic;
using System.Text;
using ACASLibraries;

namespace ACASLibraries.Security
{
	public class AzUtility
	{
		#region VerifyOperations();
		/// <summary>
		/// Verifies multiple operations for a single scope.
		/// <para>TOperations and TScopes must be an enum type. The TOperations enum must have int values assigned to each item.</para>
		/// <para>Uses the configured authorization manager connection string to perform the validation check.</para>
		/// <remarks>Scopes with an empty description or named "None" will be treated as the root level scope.</remarks>
		/// </summary>
		/// <typeparam name="TOperations">Enum of operations where value of each enum member is an integer that matches an operation id in Authorization Manager.</typeparam>
		/// <typeparam name="TScopes">Enum of scope operations where the scope name where the enum member's description attribute or member name matches a defined scope in Authorization Manager.</typeparam>
		/// <param name="operations">operations enum value to be verified.</param>
		/// <param name="scope">scope of operations. Uses the description attribute of the scope if defined, otherwise uses the string equivalent of the enum value's member name.</param>
		/// <returns>Boolean array indexed by same index as operations.</returns>
		public static bool[] VerifyOperations<TOperations,TScopes>(TOperations[] operations, TScopes scope) where TOperations : struct where TScopes : struct
		{
			bool[] authorized = new bool[operations.Length];
			AzSecurityManager azSecurityManager = new AzSecurityManager();
			for(int x=0;x<operations.Length;x++)
			{
				authorized[x] = VerifyOperation(Parser.EnumToInt(operations[x]), Utility.GetDescription(scope), ref azSecurityManager);
			}
			azSecurityManager.Dispose();
			azSecurityManager = null;
			return authorized;
		}
		/// <summary>
		/// Verifies multiple operations for a single scope.
		/// <para>Uses the configured authorization manager connection string to perform the validation check.</para>
		/// </summary>
		/// <param name="operations">operations to be verified.</param>
		/// <param name="scope">scope of operations.</param>
		/// <returns>Boolean array indexed by same index as operations.</returns>
		public static bool[] VerifyOperations(int[] operations, string scope)
		{
			bool[] authorized = new bool[operations.Length];
			AzSecurityManager azSecurityManager = new AzSecurityManager();
			for(int x=0; x<operations.Length; x++)
			{
				authorized[x] = VerifyOperation(operations[x], scope, ref azSecurityManager);
			}
			azSecurityManager.Dispose();
			azSecurityManager = null;
			return authorized;
		}
		/// <summary>
		/// Verifies multiple operations for multiple scopes.
		/// <para>Uses the configured authorization manager connection string to perform the validation check.</para>
		/// <remarks>Scopes with an empty description or named "None" will be treated as the root level scope.</remarks>
		/// </summary>
		/// <typeparam name="TOperations">Enum of operations where value of each enum member is an integer that matches an operation id in Authorization Manager.</typeparam>
		/// <typeparam name="TScopes">Enum of scope operations where the scope name where the enum member's description attribute or member name matches a defined scope in Authorization Manager.</typeparam>
		/// <param name="operationCollection">Collection of scopes and operations to be verified.  Boolean authoriziation result set to value of operation.</param>
		public static void VerifyOperations<TOperations,TScopes>(ref OperationCollection<TOperations,TScopes> operationCollection) where TOperations : struct where TScopes : struct
		{
			AzSecurityManager azSecurityManager = new AzSecurityManager();
			for(int x=0;x<operationCollection.Keys.Count;x++)
			{
				for(int y=0;y<operationCollection[x].Keys.Count;y++)
				{
					operationCollection[x][y] = VerifyOperation(Parser.EnumToInt(operationCollection[x].GetKeyByIndex(y)), operationCollection.GetKeyByIndex(x).ToString(), ref azSecurityManager);
				}
			}
			azSecurityManager.Dispose();
			azSecurityManager = null;
		}
		#endregion

		#region ExecuteVerifyOperation();
		/// <summary>
		/// Verifies a single operation for a single scope. Recommended to use VerifyOperations() if verifying more than one operation or scope on a page load.
		/// <para>TOperations and TScopes must be an enum type. The TOperations enum must have int values assigned to each item.</para>
		/// <para>Uses the configured authorization manager connection string to perform the validation check.</para>
		/// <remarks>Scopes with an empty description or named "None" will be treated as the root level scope.</remarks>
		/// </summary>
		/// <typeparam name="TOperations">Enum of operations where value of each enum member is an integer that matches an operation id in Authorization Manager.</typeparam>
		/// <typeparam name="TScopes">Enum of scope operations where the scope name where the enum member's description attribute or member name matches a defined scope in Authorization Manager.</typeparam>
		/// <param name="operation">operation enum value to be verified.</param>
		/// <param name="scope">scope the operation is subject.</param>
		/// <returns>True if authorized, false if not.</returns>
		public static bool VerifyOperation<TOperations,TScopes>(TOperations operation, TScopes scope) where TOperations : struct where TScopes : struct
		{
			return VerifyOperation(Parser.EnumToInt(operation), scope.ToString());
		}
		/// <summary>
		/// Verifies a single operation for a single scope. Recommended to use VerifyOperations() if verifying more than one operation or scope on a page load.
		/// <para>Uses the configured authorization manager connection string to perform the validation check.</para>
		/// </summary>
		/// <param name="operation">Integer value of operation to be verified.</param>
		/// <param name="scope">String name of scope the operation is subject.</param>
		/// <returns>True if authorized, false if not.</returns>
		public static bool VerifyOperation(int operation, string scope)
		{
			bool authorized = false;
			AzSecurityManager azSecurityManager = null;
			azSecurityManager = new AzSecurityManager();
			authorized = VerifyOperation(operation, scope, ref azSecurityManager);
			azSecurityManager.Dispose();
			azSecurityManager = null;
			return authorized;
		}
		/// <summary>
		/// Verifies a single operation for a single scope using an open azSecurityManager object. Recommended to use VerifyOperations() if verifying more than one operation or scope on a page load.
		/// </summary>
		/// <param name="operation">Integer value of operation to be verified.</param>
		/// <param name="scope">String name of scope the operation is subject.</param>
		/// <param name="azSecurityManager">An open and connection azSecurityManager object.</param>
		/// <returns>True if authorized, false if not.</returns>
		private static bool VerifyOperation(int operation, string scope, ref AzSecurityManager azSecurityManager)
		{
			bool authorized = false;
			try
			{
				if(!string.IsNullOrEmpty(scope) && scope != "None")
				{
					authorized = azSecurityManager.VerifyOperation(operation, scope);
				}
				else
				{
					authorized = azSecurityManager.VerifyOperation(operation);
				}
			}
			catch(Exception ex)
			{
				if(ACASLibraries.Trace.IsEnabled)
				{
					ACASLibraries.Trace.Write("AzUtility.VerifyOperation", (scope!=null?scope:"NULL")+"."+operation.ToString()+"="+authorized.ToString());
				}
				throw ex;
			}
			return authorized;
		}
		#endregion

		#region OperationCollection
		/// <summary>
		/// Collection of Scopes and operations.  Use AddOperation() method to add new operations to the collection.  For use with WebPort.Security.VerifyOperations().
		/// </summary>
		/// <typeparam name="TOperations">Enum of operations where value of each enum member is an integer that matches an operation id in Authorization Manager.</typeparam>
		/// <typeparam name="TScopes">Enum of scope operations where the scope name where the enum member's description attribute or member name matches a defined scope in Authorization Manager.</typeparam>
		public class OperationCollection<TOperations,TScopes> : Dictionary<TScopes, OperationCollection<TOperations,TScopes>.OperationAuthorizationDictionary> where TOperations : struct where TScopes : struct
		{
			public OperationCollection<TOperations,TScopes>.OperationAuthorizationDictionary this[int index]
			{
				get
				{
					int x=0;
					foreach(TScopes scope in Keys)
					{
						if(x==index)
						{
							return this[scope];
						}
						x++;
					}
					throw new IndexOutOfRangeException("Index "+index.ToString()+" out of range in "+Keys.Count.ToString()+" key collection.");
				}
				set
				{
					int x=0;
					TScopes? oTargetScope = null;
					foreach(TScopes scope in Keys)
					{
						if(x==index)
						{
							oTargetScope = (TScopes?)scope;
							break;
						}
						x++;
					}
					if(oTargetScope != null)
					{
						this[(TScopes)oTargetScope] = value;
					}
					else
					{
						throw new IndexOutOfRangeException("Index "+index.ToString()+" out of range in "+Keys.Count.ToString()+" key collection.");
					}
				}
			}

			/// <summary>
			/// Add a single operation for a single scope
			/// </summary>
			/// <typeparam name="TOperations">Enum of operations where value of each enum member is an integer that matches an operation id in Authorization Manager.</typeparam>
			/// <typeparam name="TScopes">Enum of scope operations where the scope name where the enum member's description attribute or member name matches a defined scope in Authorization Manager.</typeparam>
			/// <param name="operation">operation to be added</param>
			/// <param name="scope">scope for which to add operations</param>
			public void AddOperation(TOperations operation, TScopes scope)
			{
				if(!this.ContainsKey(scope))
				{
					this.Add(scope, new OperationAuthorizationDictionary());
				}
				if(!this[scope].ContainsKey(operation))
				{
					this[scope].Add(operation, false);
				}
			}
			/// <summary>
			/// Add multiple operations for a single scope.
			/// </summary>
			/// <typeparam name="TOperations">Enum of operations where value of each enum member is an integer that matches an operation id in Authorization Manager.</typeparam>
			/// <typeparam name="TScopes">Enum of scope operations where the scope name where the enum member's description attribute or member name matches a defined scope in Authorization Manager.</typeparam>
			/// <param name="operations">operations to be added</param>
			/// <param name="scope">scope for which to add operations</param>
			public void AddOperations(TOperations[] operations, TScopes scope)
			{
				if(!this.ContainsKey(scope))
				{
					this.Add(scope, new OperationAuthorizationDictionary());
				}
				for(int x=0;x<operations.Length;x++)
				{
					if(!this[scope].ContainsKey(operations[x]))
					{
						this[scope].Add(operations[x], false);
					}
				}
			}

			/// <summary>
			/// Determines if the specified operation is authorized under the specified scope
			/// </summary>
			/// <typeparam name="TOperations">Enum of operations where value of each enum member is an integer that matches an operation id in Authorization Manager.</typeparam>
			/// <typeparam name="TScopes">Enum of scope operations where the scope name where the enum member's description attribute or member name matches a defined scope in Authorization Manager.</typeparam>
			/// <param name="operation">The operation for the authorization check</param>
			/// <param name="scope">The scope of the operation</param>
			/// <returns>True if the operation has been authorized. False if the operation is not authorized or if the collection has not been assessed for authorization.</returns>
			public bool IsAuthorized(TOperations operation, TScopes scope)
			{
				if (this.ContainsKey(scope))
				{
					if (this[scope].ContainsKey(operation))
					{
						return this[scope][operation];
					}
				}

				return false;
			}

			/// <summary>
			/// Gets the scope key at the specified collection index.
			/// </summary>
			/// <param name="index">The index of the scope</param>
			/// <returns>The scope at the specified index</returns>
			public TScopes GetKeyByIndex(int index)
			{
				int x=0;
				foreach(TScopes scope in Keys)
				{
					if(x==index)
					{
						return scope;
					}
					x++;
				}
				throw new IndexOutOfRangeException("Index "+index.ToString()+" out of range in "+Keys.Count.ToString()+" key collection.");
			}
			
			public class OperationAuthorizationDictionary : Dictionary<TOperations, bool>
			{
				public bool this[int index]
				{
					get
					{
						int x=0;
						foreach(bool value in Values)
						{
							if(x==index)
							{
								return value;
							}
							x++;
						}
						throw new IndexOutOfRangeException("Index "+index.ToString()+" out of range in "+Values.Count.ToString()+" value collection.");
					}
					set
					{
						int x=0;
						TOperations? targetOperation = null;
						foreach(TOperations operation in Keys)
						{
							if(x==index)
							{
								targetOperation = (TOperations?)operation;
								break;
							}
							x++;
						}
						if(targetOperation != null)
						{
							this[(TOperations)targetOperation] = value;
						}
						else
						{
							throw new IndexOutOfRangeException("Index "+index.ToString()+" out of range in "+Keys.Count.ToString()+" key collection.");
						}
					}
				}

				/// <summary>
				/// Gets the operation key at the specified collection index.
				/// </summary>
				/// <param name="index">The index of the operation</param>
				/// <returns>The operation at the specified index</returns>
				public TOperations GetKeyByIndex(int index)
				{
					int x=0;
					foreach(TOperations operation in Keys)
					{
						if(x==index)
						{
							return operation;
						}
						x++;
					}
					throw new IndexOutOfRangeException("Index "+index.ToString()+" out of range in "+Keys.Count.ToString()+" key collection.");
				}
			}
		}
		#endregion
	}
}
