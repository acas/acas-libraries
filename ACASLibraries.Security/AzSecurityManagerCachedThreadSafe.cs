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
	/// <typeparam name="TOperations">Enum of operations where value of each enum member is an integer that matches an operation id in Authorization Manager.</typeparam>
	/// <typeparam name="TScopes">Enum of scope operations where the scope name where the enum member's description attribute or member name matches a defined scope in Authorization Manager.</typeparam>
	public class AzSecurityManagerCachedThreadSafe<TOperations, TScopes>
		where TScopes : struct, IConvertible
		where TOperations : struct, IConvertible {

		#region public properties
		/// <summary>
		/// Specifies (explicitly or via override) the user domain. This is used to obtain the user's principal and email address.
		/// </summary>
		public static string ApplicationDomain = null;
		/// <summary>
		/// Specifies the "none" scope. During operation verifications, if the none scope is passed then the operation will be verified without a specified scope (aka none or root scope).
		/// </summary>
		public TScopes? NoneScope {
			get {
				return noneScope;
			}
			set {
				if(value != null) {
					noneScope = value;
					noneScopeName = Enum.GetName(value.GetType(), value);
				}
				else {
					noneScope = null;
					noneScopeName = null;
				}
			}
		}

		/// <summary>
		/// Specifies whether operation verification results should be cached. Default is FALSE.
		/// </summary>
		public bool CacheOperationVerifications {
			get {
				return cacheOperationVerifications;
			}
			set {
				Monitor.Enter(threadLock);
				if(!cacheOperationVerifications) {
					//tear down cache
					operationVerificationCache.Clear(); //not sure if this step is necessary; added to signal garbage collection
				}
				cacheOperationVerifications = value;
				Monitor.Exit(threadLock);
			}
		}
		/// <summary>
		/// Specifies the length of time that an operation verification should be cached. Default is 20 minutes.
		/// </summary>
		public TimeSpan OperationVerificationCacheExpiration = new TimeSpan(0, 20, 0);
		#endregion

		#region private properties
		private TScopes? noneScope = null;
		private string noneScopeName = null;

		private static Object threadLock = new Object();
		private bool cacheOperationVerifications = false;
		private ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<int,OperationVerificationResult>>> operationVerificationCache = new ConcurrentDictionary<string,ConcurrentDictionary<string, ConcurrentDictionary<int,OperationVerificationResult>>>();
		private AzSecurityManager azSecurityManagerInstance = null;
		#endregion

		#region ClearOperationVerificationCache();
		public void ClearOperationVerificationCache() {
			lock(threadLock) {
				if(cacheOperationVerifications) { 
					operationVerificationCache.Clear();
				}
				else {
					throw new Exception("Operation verification cache not enabled");
				}
			}
		}
		#endregion

		#region GetUsername();
		public static string GetUsername() {
			string username = null;
			if(ServiceSecurityContext.Current != null && ServiceSecurityContext.Current.WindowsIdentity != null) {
				username = ServiceSecurityContext.Current.WindowsIdentity.Name;
			}
			if(string.IsNullOrEmpty(username)) {
				username = UserManager.Username;
			}
			return username;
		}
		#endregion

		#region GetUserPrincipal();
		public static UserPrincipal GetUserPrincipal() {
			try {
				if(!string.IsNullOrEmpty(ApplicationDomain)) {
					return UserPrincipal.FindByIdentity(new PrincipalContext(ContextType.Domain, ApplicationDomain), IdentityType.SamAccountName, GetUsername());
				}
			}
			catch(Exception ex) {
				Logger.LogError(ex);
			}
			return null;
		}
		#endregion

		#region GetUserGuid();
		public static string GetUserGuid() {
			Principal userPrincipal = GetUserPrincipal();
			if(userPrincipal != null) {
				Guid? userGuid = userPrincipal.Guid;
				if(userGuid != null) {
					return userGuid.Value.ToString();
				}
				else {
					return null;
				}
			}
			else {
				return null;
			}
		}
		#endregion

		#region GetUserEmailAddress();
		public static string GetUserEmailAddress() {
			string emailAddress = null;
			try {
				UserPrincipal userPrincipal = GetUserPrincipal();
				if(userPrincipal != null) {
					emailAddress = userPrincipal.EmailAddress;
				}
			}
			catch(Exception ex) {
				Logger.LogError(ex);
			}
			return emailAddress;
		}
		#endregion

		#region RemoveDomain(); VerifyCurrentUser();
		private static string RemoveDomain(string username) {
			string result;
			int startIndex = username.IndexOf("\\");
			if(startIndex > 0) {
				result = username.Substring(startIndex + 1);
			}
			else {
				result = username;
			}
			return result;
		}

		public static bool VerifyCurrentUser(string username) {
			bool result = false;
			if(RemoveDomain(GetUsername()) == RemoveDomain(username)) {
				result = true;
			}
			return result;
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
		public bool[] VerifyOperations(TOperations[] operations, TScopes scope) {
			int[] operationsIntArray = new int[operations.Length];
			for(int x = 0; x < operations.Length; x++) {
				operationsIntArray[x] = Convert.ToInt32(operations[x]);
			}
			return VerifyOperations(operationsIntArray, scope.ToString());
		}
		/// <summary>
		/// Verifies multiple operations for a single scope
		/// </summary>
		/// <param name="operations">operations to be verified</param>
		/// <param name="scope">scope of operations</param>
		/// <returns>Boolean array indexed by same index as operations.</returns>
		public bool[] VerifyOperations(int[] operations, string scope) {
			bool[] authorized = new bool[operations.Length];
			for(int x = 0; x < operations.Length; x++) {
				authorized[x] = ExecuteVerifyOperation(operations[x], scope);
			}
			ReleaseAzSecurityManagerInstance();
			return authorized;
		}
		/// <summary>
		/// Verifies multiple operations for multiple scopes
		/// </summary>
		/// <param name="OperationCollection">Collection of scopes and operations to be verified.  Boolean authoriziation result set to value of operation.</param>
		public void VerifyOperations(ref OperationCollection<TScopes, TOperations> OperationCollection) {
			for(int x = 0; x < OperationCollection.Keys.Count; x++) {
				for(int y = 0; y < OperationCollection[x].Keys.Count; y++) {
					OperationCollection[x][y] = ExecuteVerifyOperation(Convert.ToInt32(OperationCollection[x].GetKeyByIndex(y)), OperationCollection.GetKeyByIndex(x).ToString());
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
		public bool VerifyOperation(TOperations operation, TScopes scope) {
			return VerifyOperation(Convert.ToInt32(operation), scope.ToString());
		}
		/// <summary>
		/// Verifies a single operation for a single scope. Recommended to use VerifyOperations() if verifying more than one operation or scope on a page load.
		/// </summary>
		/// <param name="operation">Integer value of operation to be verified.</param>
		/// <param name="scope">String name of scope the operation is subject.</param>
		/// <returns>True if authorized, false if not.</returns>
		public bool VerifyOperation(int operation, string scope) {
			bool authorized = ExecuteVerifyOperation(operation, scope);
			ReleaseAzSecurityManagerInstance();
			return authorized;
		}
		#endregion

		#region TryVerifyCachedOperation(); CacheOperationVerification();
		private bool TryVerifyCachedOperation(int operation, string scope, out bool authorized) {
			bool hasCachedValue = false;
			authorized = false;
			if(cacheOperationVerifications) {
				ConcurrentDictionary<string, ConcurrentDictionary<int, OperationVerificationResult>> scopeCache;
				if(operationVerificationCache.TryGetValue(GetUsername(), out scopeCache)) {
					ConcurrentDictionary<int, OperationVerificationResult> operationCache;
					if(scopeCache.TryGetValue(scope, out operationCache)) {
						OperationVerificationResult cachedVerificationResult;
						if(operationCache.TryGetValue(operation, out cachedVerificationResult)) {
							if(cachedVerificationResult.Created + OperationVerificationCacheExpiration > DateTime.Now) {
								//cache result is good
								hasCachedValue = true;
								authorized = cachedVerificationResult.IsVerified;
							}
							else {
								//cache is out of date
								operationCache.TryRemove(operation, out cachedVerificationResult);
							}
						}
					}
				}
			}
			return hasCachedValue;
		}
		private bool CacheOperationVerification(int operation, string scope, bool authorized) {
			if(cacheOperationVerifications) {
				ConcurrentDictionary<string, ConcurrentDictionary<int, OperationVerificationResult>> scopeCache = operationVerificationCache.GetOrAdd(GetUsername(), x => new ConcurrentDictionary<string, ConcurrentDictionary<int, OperationVerificationResult>>());
				ConcurrentDictionary<int, OperationVerificationResult> operationCache = scopeCache.GetOrAdd(scope, x => new ConcurrentDictionary<int, OperationVerificationResult>());
				operationCache.AddOrUpdate(operation, x => new OperationVerificationResult(authorized), (x, y) => new OperationVerificationResult(authorized));
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
		private bool ExecuteVerifyOperation(int operation, string scope) {
			bool authorized = false;
			try {
				if(!TryVerifyCachedOperation(operation, scope, out authorized)) {
					if(!string.IsNullOrEmpty(scope) && noneScope != null && string.Compare(scope, noneScopeName) != 0) {
						//add to cache
						authorized = GetAzSecurityManagerInstance().VerifyOperation(operation, scope);
					}
					else {
						//add to cache
						authorized = GetAzSecurityManagerInstance().VerifyOperation(operation);
					}
					CacheOperationVerification(operation, scope, authorized);
				}
			}
			catch(Exception ex) {
				Logger.LogError(ex);
			}
			if(ACASLibraries.Trace.IsEnabled) {
				ACASLibraries.Trace.Write("IAzSecurityStaticVerifier.VerifyOperation", (scope != null ? scope : "NULL") + "." + operation.ToString() + "=" + authorized.ToString());
			}
			return authorized;
		}
		#endregion
	}

	#region OperationCollection
	/// <summary>
	/// Collection of Scopes and operations.  Use AddOperation() method to add new operations to the collection.  For use with WebPort.Security.VerifyOperations().
	/// </summary>
	public class OperationCollection<TScopes, TOperations> : Dictionary<TScopes, OperationAuthorizationDictionary<TOperations>>
		where TScopes : struct, IConvertible
		where TOperations : struct, IConvertible {
		public OperationAuthorizationDictionary<TOperations> this[int index] {
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
		/// Add a single operation for a single scope
		/// </summary>
		/// <param name="operation">operation to be added</param>
		/// <param name="scope">scope for which to add operations</param>
		public void AddOperation(TOperations operation, TScopes scope) {
			if(!this.ContainsKey(scope)) {
				this.Add(scope, new OperationAuthorizationDictionary<TOperations>());
			}
			if(!this[scope].ContainsKey(operation)) {
				this[scope].Add(operation, false);
			}
		}
		/// <summary>
		/// Add multiple operations for a single scope.
		/// </summary>
		/// <param name="operations">operations to be added</param>
		/// <param name="scope">scope for which to add operations</param>
		public void AddOperations(TOperations[] operations, TScopes scope) {
			if(!this.ContainsKey(scope)) {
				this.Add(scope, new OperationAuthorizationDictionary<TOperations>());
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
		public bool IsAuthorized(TOperations operation, TScopes scope) {
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

	#region OperationAuthorizationDictionary
	public class OperationAuthorizationDictionary<TOperations> : Dictionary<TOperations, bool>
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

	#region OperationVerificationResult
	public class OperationVerificationResult {
		public bool IsVerified {
			get;
			private set;
		}
		public DateTime Created {
			get;
			private set;
		}

		public OperationVerificationResult(bool isVerified) {
			IsVerified = isVerified;
			Created = DateTime.Now;
		}
	}
	#endregion
}