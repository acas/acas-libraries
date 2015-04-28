using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Web;
using ACASLibraries;

namespace ACASLibraries.Security {
	public class AzOperationVerifier {

		#region public properties
		/// <summary>
		/// Specifies the "none" scope. During operation verifications, if the none scope is passed then the operation will be verified without a specified scope (aka none or root scope).
		/// </summary>
		public static string RootScopeAlias = "None";
		/// <summary>
		/// Specifies whether operation verification results should be cached. Default is FALSE.
		/// </summary>
		public static bool CacheOperationVerifications = true;
		/// <summary>
		/// Specifies the length of time that an operation verification should be cached. Default is 20 minutes.
		/// </summary>
		public static TimeSpan OperationVerificationCacheExpiration = new TimeSpan(0, 20, 0);
		#endregion

		#region private properties
		private static Object threadLock = new Object();
		private static ConcurrentDictionary<Tuple<string, string, int>, AzOperationVerificationResult> operationVerificationCache = new ConcurrentDictionary<Tuple<string, string, int>, AzOperationVerificationResult>();
		private AzSecurityManager azSecurityManagerInstance = null;
		#endregion

		#region ClearOperationVerificationCache();
		public void ClearOperationVerificationCache() {
			Monitor.Enter(threadLock);
			operationVerificationCache.Clear();
			Monitor.Exit(threadLock);
		}
		#endregion

		#region GetAzSecurityManagerInstance(); ReleaseAzSecurityManagerInstance();
		private AzSecurityManager GetAzSecurityManagerInstance() {
			if(!Monitor.IsEntered(threadLock)) {
				Monitor.Enter(threadLock);
				if(azSecurityManagerInstance == null) {
					azSecurityManagerInstance = new AzSecurityManager();
				}
			}
			return azSecurityManagerInstance;
		}

		private void ReleaseAzSecurityManagerInstance() {
			if(Monitor.IsEntered(threadLock)) {
				if(azSecurityManagerInstance != null) {
					azSecurityManagerInstance.Dispose();
					azSecurityManagerInstance = null;
				}
				Monitor.Exit(threadLock);
			}
		}
		#endregion

		#region VerifyOperations();
		/// <summary>
		/// Verifies multiple operations for a single scope
		/// </summary>
		/// <param name="operations">operations to be verified</param>
		/// <param name="scope">scope of operations</param>
		/// <returns>Boolean array indexed by same index as operations.</returns>
		/// <typeparam name="TOperations">Enum of operations where value of each enum member is an integer that matches an operation id in Authorization Manager.</typeparam>
		/// <typeparam name="TScopes">Enum of scope operations where the scope name where the enum member's description attribute or member name matches a defined scope in Authorization Manager.</typeparam>
		public bool[] VerifyOperations<TScopes, TOperations>(TScopes scope, TOperations[] operations)
			where TScopes : struct, IConvertible
			where TOperations : struct, IConvertible {
			int[] operationsIntArray = new int[operations.Length];
			for(int x = 0; x < operations.Length; x++) {
				operationsIntArray[x] = Convert.ToInt32(operations[x]);
			}
			return VerifyOperations(scope.ToString(), operationsIntArray);
		}
		/// <summary>
		/// Verifies multiple operations for a single scope
		/// </summary>
		/// <param name="operations">operations to be verified</param>
		/// <param name="scope">scope of operations</param>
		/// <returns>Boolean array indexed by same index as operations.</returns>
		public bool[] VerifyOperations(string scope, int[] operations) {
			bool[] authorized = new bool[operations.Length];
			for(int x = 0; x < operations.Length; x++) {
				authorized[x] = ExecuteVerifyOperation(scope, operations[x]);
			}
			ReleaseAzSecurityManagerInstance();
			return authorized;
		}
		/// <summary>
		/// Verifies multiple operations for multiple scopes
		/// </summary>
		/// <param name="AzOperationCollection">Collection of scopes and operations to be verified.  Boolean authoriziation result set to value of operation.</param>
		public void VerifyOperations<TScopes, TOperations>(ref AzOperationCollection<TScopes, TOperations> operationCollection)
			where TScopes : struct, IConvertible
			where TOperations : struct, IConvertible {
			for(int x = 0; x < operationCollection.Keys.Count; x++) {
				for(int y = 0; y < operationCollection[x].Keys.Count; y++) {
					operationCollection[x][y] = ExecuteVerifyOperation(operationCollection.GetKeyByIndex(x).ToString(), Convert.ToInt32(operationCollection[x].GetKeyByIndex(y)));
				}
			}
			ReleaseAzSecurityManagerInstance();
		}
		#endregion

		#region VerifyOperation();
		/// <summary>
		/// Verifies a single operation for a single scope. Recommended to use VerifyOperations() if verifying more than one operation or scope on a page load.
		/// </summary>
		/// <param name="operation">Security.operation to be verified.</param>
		/// <param name="scope">Security.scope the operation is subject.</param>
		/// <returns>True if authorized, false if not.</returns>
		/// <typeparam name="TOperations">Enum of operations where value of each enum member is an integer that matches an operation id in Authorization Manager.</typeparam>
		/// <typeparam name="TScopes">Enum of scope operations where the scope name where the enum member's description attribute or member name matches a defined scope in Authorization Manager.</typeparam>
		public bool VerifyOperation<TScopes, TOperations>(TScopes scope, TOperations operation)
			where TScopes : struct, IConvertible
			where TOperations : struct, IConvertible {
			return VerifyOperation(scope.ToString(), Convert.ToInt32(operation));
		}
		/// <summary>
		/// Verifies a single operation for a single scope. Recommended to use VerifyOperations() if verifying more than one operation or scope on a page load.
		/// </summary>
		/// <param name="operation">Integer value of operation to be verified.</param>
		/// <param name="scope">String name of scope the operation is subject.</param>
		/// <returns>True if authorized, false if not.</returns>
		public bool VerifyOperation(string scope, int operation) {
			bool authorized = ExecuteVerifyOperation(scope, operation);
			ReleaseAzSecurityManagerInstance();
			return authorized;
		}
		#endregion

		#region TryVerifyCachedOperation(); CacheOperationVerification();
		private bool TryVerifyCachedOperation(string scope, int operation, out bool authorized) {
			bool hasCachedValue = false;
			authorized = false;
			if(CacheOperationVerifications) {
				AzOperationVerificationResult cachedVerificationResult;
				Tuple<string, string, int> cacheKey = Tuple.Create<string, string, int>(UserManager.Username, scope, operation);
				if(operationVerificationCache.TryGetValue(cacheKey, out cachedVerificationResult)) {
					if(cachedVerificationResult.Created + OperationVerificationCacheExpiration > DateTime.Now) {
						//cache result is good
						hasCachedValue = true;
						authorized = cachedVerificationResult.IsVerified;
					}
					else {
						//cache is out of date
						operationVerificationCache.TryRemove(cacheKey, out cachedVerificationResult);
					}
				}
			}
			return hasCachedValue;
		}
		private bool CacheOperationVerification(string scope, int operation, bool authorized) {
			if(CacheOperationVerifications) {
				operationVerificationCache.AddOrUpdate(Tuple.Create<string, string, int>(UserManager.Username, scope, operation), x => new AzOperationVerificationResult(authorized), (x, y) => new AzOperationVerificationResult(authorized));
			}
			return authorized;
		}
		#endregion

		#region ExecuteVerifyOperation();
		/// <summary>
		/// Verifies a single operation for a single scope using an open azSecurityManager object. Recommended to use VerifyOperations() if verifying more than one operation or scope on a page load.
		/// </summary>
		/// <param name="operation">Integer value of operation to be verified.</param>
		/// <param name="scope">String name of scope the operation is subject.</param>
		/// <param name="azSecurityManager">An open and connection azSecurityManager object.</param>
		/// <returns>True if authorized, false if not.</returns>
		private bool ExecuteVerifyOperation(string scope, int operation) {
			bool authorized = false;
			try {
				if(!TryVerifyCachedOperation(scope, operation, out authorized)) {
					if(!string.IsNullOrEmpty(scope) && (string.IsNullOrEmpty(RootScopeAlias) || string.Compare(scope, RootScopeAlias) != 0)) {
						//add to cache
						authorized = GetAzSecurityManagerInstance().VerifyOperation(operation, scope);
					}
					else {
						//add to cache
						authorized = GetAzSecurityManagerInstance().VerifyOperation(operation);
					}
					CacheOperationVerification(scope, operation, authorized);
					if(ACASLibraries.Trace.IsEnabled) {
						ACASLibraries.Trace.Write("AzOperationVerifier.ExecuteVerifyOperation", (scope != null ? scope : "NULL") + "." + operation.ToString() + "=" + authorized.ToString());
					}
				}
			}
			catch(Exception ex) {
				Logger.LogError(ex);
			}
			return authorized;
		}
		#endregion
	}

	#region AzOperationCollection
	/// <summary>
	/// Collection of Scopes and operations.  Use AddOperation() method to add new operations to the collection.  For use with WebPort.Security.VerifyOperations().
	/// </summary>
	/// <typeparam name="TOperations">Enum of operations where value of each enum member is an integer that matches an operation id in Authorization Manager.</typeparam>
	/// <typeparam name="TScopes">Enum of scope operations where the scope name where the enum member's description attribute or member name matches a defined scope in Authorization Manager.</typeparam>
	public class AzOperationCollection<TScopes, TOperations> : Dictionary<TScopes, AzOperationAuthorizationDictionary<TOperations>>
		where TScopes : struct, IConvertible
		where TOperations : struct, IConvertible {
		public AzOperationAuthorizationDictionary<TOperations> this[int index] {
			get {
				int x = 0;
				foreach(TScopes scope in Keys) {
					if(x == index) {
						return this[scope];
					}
					x++;
				}
				throw new IndexOutOfRangeException("Index " + index.ToString() + " out of range in " + Keys.Count.ToString() + " key collection.");
			}
			set {
				int x = 0;
				TScopes? targetScope = null;
				foreach(TScopes scope in Keys) {
					if(x == index) {
						targetScope = (TScopes?)scope;
						break;
					}
					x++;
				}
				if(targetScope != null) {
					this[(TScopes)targetScope] = value;
				}
				else {
					throw new IndexOutOfRangeException("Index " + index.ToString() + " out of range in " + Keys.Count.ToString() + " key collection.");
				}
			}
		}

		/// <summary>
		/// Add one or more operations for a single scope.
		/// </summary>
		/// <param name="operations">operations to be added</param>
		/// <param name="scope">scope for which to add operations</param>
		/// <typeparam name="TOperations">Enum of operations where value of each enum member is an integer that matches an operation id in Authorization Manager.</typeparam>
		/// <typeparam name="TScopes">Enum of scope operations where the scope name where the enum member's description attribute or member name matches a defined scope in Authorization Manager.</typeparam>
		public void AddOperation(TScopes scope, params TOperations[] operations) {
			if(!this.ContainsKey(scope)) {
				this.Add(scope, new AzOperationAuthorizationDictionary<TOperations>());
			}
			for(int x = 0; x < operations.Length; x++) {
				if(!this[scope].ContainsKey(operations[x])) {
					this[scope].Add(operations[x], false);
				}
			}
		}

		/// <summary>
		/// Determines if the specified operation is authorized under the specified scope
		/// </summary>
		/// <param name="operation">The operation for the authorization check</param>
		/// <param name="scope">The scope of the operation</param>
		/// <returns>True if the operation has been authorized. False if the operation is not authorized or if the collection has not been assessed for authorization.</returns>
		public bool IsAuthorized(TScopes scope, TOperations operation) {
			if(this.ContainsKey(scope)) {
				if(this[scope].ContainsKey(operation)) {
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
		public TScopes GetKeyByIndex(int index) {
			int x = 0;
			foreach(TScopes scope in Keys) {
				if(x == index) {
					return scope;
				}
				x++;
			}
			throw new IndexOutOfRangeException("Index " + index.ToString() + " out of range in " + Keys.Count.ToString() + " key collection.");
		}
	}
	#endregion

	#region AzOperationAuthorizationDictionary
	/// <typeparam name="TOperations">Enum of operations where value of each enum member is an integer that matches an operation id in Authorization Manager.</typeparam>
	public class AzOperationAuthorizationDictionary<TOperations> : Dictionary<TOperations, bool>
		where TOperations : struct, IConvertible {
		public bool this[int index] {
			get {
				int x = 0;
				foreach(bool value in Values) {
					if(x == index) {
						return value;
					}
					x++;
				}
				throw new IndexOutOfRangeException("Index " + index.ToString() + " out of range in " + Values.Count.ToString() + " value collection.");
			}
			set {
				int x = 0;
				TOperations? targetOperation = null;
				foreach(TOperations oOperation in Keys) {
					if(x == index) {
						targetOperation = (TOperations?)oOperation;
						break;
					}
					x++;
				}
				if(targetOperation != null) {
					this[(TOperations)targetOperation] = value;
				}
				else {
					throw new IndexOutOfRangeException("Index " + index.ToString() + " out of range in " + Keys.Count.ToString() + " key collection.");
				}
			}
		}

		/// <summary>
		/// Gets the operation key at the specified collection index.
		/// </summary>
		/// <param name="index">The index of the operation</param>
		/// <returns>The operation at the specified index</returns>
		public TOperations GetKeyByIndex(int index) {
			int x = 0;
			foreach(TOperations operation in Keys) {
				if(x == index) {
					return operation;
				}
				x++;
			}
			throw new IndexOutOfRangeException("Index " + index.ToString() + " out of range in " + Keys.Count.ToString() + " key collection.");
		}
	}
	#endregion

	#region AzOperationVerificationResult
	public class AzOperationVerificationResult {
		public bool IsVerified {
			get;
			private set;
		}
		public DateTime Created {
			get;
			private set;
		}

		public AzOperationVerificationResult(bool isVerified) {
			IsVerified = isVerified;
			Created = DateTime.Now;
		}
	}
	#endregion
}