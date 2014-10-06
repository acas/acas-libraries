//#define UseVersion1Types	//use this flag to cause compilation to use the original AzMan objects, for example it will use IAzApplication instead of IAzApplication3
using System;
using System.Configuration;
using System.Security.Principal;
using System.Web;
using Microsoft.Interop.Security.AzRoles;

namespace ACASLibraries.Security
{
	/// <summary>
	/// AzSecurityManager is a wrapper class providing easier functions for interacting with the windows Authorization Manager (Microsoft.Interop.Security.AzRoles.dll)
	/// <remarks>
	/// <para>Error hints: "Access Denied" could mean that the OS is incompatible (see method info).</para>
	/// <para>"Value does not fall within expected range" could mean that one of the parameters being passed is invalid or otherwise in the correct format.</para>
	/// </remarks>
	/// </summary>
	public class AzSecurityManager : IDisposable
	{
		#region constants
		private const string defaultAccessCheckObjectName = "Auditstring";
		#endregion

		#region private properties
		/// <summary>
		/// Internal reference for AzAuthorizationStoreClass
		/// </summary>
		private AzAuthorizationStoreClass azStore = null;
		/// <summary>
		/// Internal reference for IAzApplication.  Instantiated from azStore.
		/// </summary>
		#if UseVersion1Types
		private IAzApplication azApplication = null;
		#else
		private IAzApplication3 azApplication = null;
		#endif
		/// <summary>
		/// Internal reference for IAzClientContext.  Stores the last context or used client context.
		/// </summary>
		#if UseVersion1Types
		private IAzClientContext clientContext = null;
		#else
		private IAzClientContext3 clientContext = null;
		#endif
		/// <summary>
		/// Internal reference for username last used when creating a context with username and domain
		/// </summary>
		private string username = null;
		/// <summary>
		/// Internal reference for domain last used when creating a context with username and domain
		/// </summary>
		private string domain = null;
		/// <summary>
		/// Internal reference required for IDisposable implementation
		/// </summary>
		private bool disposed = false;
		
		private bool traceEnabled = false; 
		private TraceType traceType = TraceType.None;
		private enum TraceType {
			HttpContext,
			SystemDiagnostics,
			None
		}
		#endregion

		#region public properties
		/// <summary>
		/// Get thes active AzAuthorizationStoreClass instance for the SecurityManager
		/// </summary>
		public AzAuthorizationStoreClass AzAuthorizationStore
		{
			get
			{
				return azStore;
			}
		}
		/// <summary>
		/// Get thes active IAzApplication instance for the SecurityManager
		/// </summary>
		#if UseVersion1Types
		public IAzApplication AzApplication
		#else
		public IAzApplication3 AzApplication
		#endif
		{
			get
			{
				return azApplication;
			}
		}
		/// <summary>
		/// Get thes active IAzClientContext instance for the SecurityManager
		/// </summary>
		#if UseVersion1Types
		public IAzClientContext AzClientContext
		#else
		public IAzClientContext3 AzClientContext
		#endif
		{
			get
			{
				if(clientContext == null)
				{
					CreateClientContext();
				}
				return clientContext;
			}
		}
		#endregion

		#region constructor
		/// <summary>
		/// Initializes class.  Will automatically open the AzApplication if values are defined in the appSettings section of the Web.Config for &quot;AzManPolicyStore&quot; and &quot;AzManApplication&quot;
		/// </summary>
		public AzSecurityManager()
		{

			traceEnabled = ((HttpContext.Current != null && HttpContext.Current.Trace.IsEnabled) || (System.Diagnostics.Trace.Listeners.Count > 0));
			if(traceEnabled) {
				if(HttpContext.Current != null && HttpContext.Current.Trace.IsEnabled) {
					traceType = TraceType.HttpContext;
				}
				else if(System.Diagnostics.Trace.Listeners.Count > 0) {
					traceType = TraceType.SystemDiagnostics;
				}
			}


			string azApplicationName = ConfigurationManager.AppSettings["AzManApplication"];
			string azPolicyStore = ConfigurationManager.AppSettings["AzManPolicyStore"];
			if(string.IsNullOrEmpty(azPolicyStore)) {
				if(ConfigurationManager.ConnectionStrings["AzManPolicyStore"] != null) {
					azPolicyStore = ConfigurationManager.ConnectionStrings["AzManPolicyStore"].ConnectionString;
				}
			}

			if(azPolicyStore != null && azApplicationName != null)
			{
				OpenApplication(azPolicyStore, azApplicationName);
			}
		}
		public AzSecurityManager(string PolicyStoreUri, string Application)
		{
			//try
			//{
				OpenApplication(PolicyStoreUri, Application);
			//}
			//catch(exception oException)
			//{
			//    throw oException;
			//}
			//finally
			//{
			//    azStore = null;
			//    azApplication = null;
			//    clientContext = null;
			//}
		}
		#endregion

		#region OpenPolicyStore();
		/// <summary>
		/// Opens an AzMan policy store.
		/// <para>For the policyStoreUri, the value be msxml:// or msldap:// or an xml file name.  If an xml file name is used, the msxml prefix and application root or startup path will be automatically prefixed to the file name (similar to a virtual location).</para>
		/// <para>For the msxml prefix, the following are example values:
		/// <list type="bullet">
		/// <item><description>msxml://c:/abc/test.xml</description></item>
		/// <item><description>msxml://\\server\share\abc.xml</description></item>
		/// <item><description>msxml://d|/dir1/dir2/abc.xml</description></item>
		/// <item><description>msxml://c:/Documents%20and%20Settings/test%2exml</description></item>
		/// </list>
		/// </para>
		/// <para>For the msldap prefix, the following are example values:
		/// <list type="bullet">
		/// <item><description>msldap://ServerName:Port//DistinguishedNameForTheStore</description></item>
		/// <item><description>msldap://MyServer/CN=MyStore,OU=AzMan,DC=MyDomain,DC=Fabrikam,DC=Com</description></item>
		/// </list>
		/// </para>
		/// </summary>
		/// <param name="policyStoreUri">The uri of the policy store.</param>
		public void OpenPolicyStore(string policyStoreUri)
		{
			if(!policyStoreUri.ToLower().StartsWith("msxml:") && !policyStoreUri.ToLower().StartsWith("msldap:") && policyStoreUri.ToLower().EndsWith("xml"))
			{
				//auto-prefix xml file store path to application root
				if(HttpContext.Current != null)
				{
					policyStoreUri = String.Concat("msxml://", HttpContext.Current.Request.PhysicalApplicationPath, policyStoreUri);
				}
				else if(System.Windows.Forms.Application.ExecutablePath != null)
				{
					policyStoreUri = String.Concat("msxml://", System.Windows.Forms.Application.StartupPath, @"\", policyStoreUri);
				}
			}

			TraceWrite("AzSecurityManager.OpenApplication()", "Opening policy store at \""+policyStoreUri+"\"");

			azStore = new AzAuthorizationStoreClass();
			azStore.Initialize(0, policyStoreUri, null);
		}
		#endregion

		#region OpenApplication();
		/// <summary>
		/// Opens an AzMan policy store and application.
		/// <para>For the policyStoreUri, the value be msxml:// or msldap:// or an xml file name.  If an xml file name is used, the msxml prefix and application root or startup path will be automatically prefixed to the file name (similar to a virtual location).</para>
		/// <para>For the msxml prefix, the following are example values:
		/// <list type="bullet">
		/// <item><description>msxml://c:/abc/test.xml</description></item>
		/// <item><description>msxml://\\server\share\abc.xml</description></item>
		/// <item><description>msxml://d|/dir1/dir2/abc.xml</description></item>
		/// <item><description>msxml://c:/Documents%20and%20Settings/test%2exml</description></item>
		/// </list>
		/// </para>
		/// <para>For the msldap prefix, the following are example values:
		/// <list type="bullet">
		/// <item><description>msldap://ServerName:Port//DistinguishedNameForTheStore</description></item>
		/// <item><description>msldap://MyServer/CN=MyStore,OU=AzMan,DC=MyDomain,DC=Fabrikam,DC=Com</description></item>
		/// </list>
		/// </para>
		/// </summary>
		/// <param name="policyStoreUri">The uri of the policy store.</param>
		/// <param name="applicationName">The name of the application to open in the policy store</param>
		public void OpenApplication(string policyStoreUri, string applicationName)
		{
			if(!policyStoreUri.ToLower().StartsWith("msxml:") && !policyStoreUri.ToLower().StartsWith("msldap:") && policyStoreUri.ToLower().EndsWith("xml"))
			{
				//auto-prefix xml file store path to application root
				if(HttpContext.Current != null)
				{
					policyStoreUri = String.Concat("msxml://", HttpContext.Current.Request.PhysicalApplicationPath, policyStoreUri);
				}
				else if(System.Windows.Forms.Application.ExecutablePath != null)
				{
					policyStoreUri = String.Concat("msxml://", System.Windows.Forms.Application.StartupPath, @"\", policyStoreUri);
				}
			}

			TraceWrite("AzSecurityManager.OpenApplication()", "Opening policy store at \""+policyStoreUri+"\" and application \""+applicationName+"\"");

			azStore = new AzAuthorizationStoreClass();
			azStore.Initialize(0, policyStoreUri, null);
			#if UseVersion1Types
			azApplication = azStore.OpenApplication(applicationName, null);
			#else
			azApplication = (IAzApplication3)azStore.OpenApplication2(applicationName, null);
			#endif
		}
		public void OpenApplication(string applicationName)
		{
			TraceWrite("AzSecurityManager.OpenApplication()", "Opening application \""+applicationName+"\"");
			azApplication = (IAzApplication3)azStore.OpenApplication(applicationName, null);
		}
		#endregion

		#region CreateClientContext();
		/// <summary>
		/// <para>Creates a client context using the current user's token (WindowsIdentity.Token)</para>
		/// <para>In the case that the token fails, attempts to use the current user's username and domain</para>
		/// </summary>
		public void CreateClientContext()
		{
			if(azApplication != null)
			{
				WindowsIdentity identity = null;
				if(HttpContext.Current != null)
				{
					identity = (WindowsIdentity)HttpContext.Current.User.Identity;
				}
				else
				{
					identity = WindowsIdentity.GetCurrent();
				}
				bool contextCreated = false;
				try
				{
					#if UseVersion1Types
					clientContext = azApplication.InitializeClientContextFromToken((uint)identity.Token, null);
					#else
					clientContext = (IAzClientContext3)azApplication.InitializeClientContextFromToken2((uint)identity.Token, (uint)0, null);
					#endif
					contextCreated = true;
				}
				catch(Exception ex)
				{
					TraceWarn("AzSecurityManager.CreateClientContext();", ex.Message, ex);
				}
				if(!contextCreated)
				{
					string[] saName = identity.Name.Split("\\".ToCharArray());
					if(saName.Length == 2)
					{
						//username format is DOMAIN\username
						#if UseVersion1Types
						clientContext = azApplication.InitializeClientContextFromName(saName[1], saName[0], null);
						#else
						clientContext = (IAzClientContext3)azApplication.InitializeClientContextFromName(saName[1], saName[0], null);
						#endif
					}
					else
					{
						saName = identity.Name.Split("@".ToCharArray());
						if(saName.Length == 2)
						{
							//username format is username@DOMAIN
							#if UseVersion1Types
							clientContext = azApplication.InitializeClientContextFromName(saName[0], saName[1], null);
							#else
							clientContext = (IAzClientContext3)azApplication.InitializeClientContextFromName(saName[0], saName[1], null);
							#endif
						}
					}
				}
			}
			else
			{
				TraceWrite("AzSecurityManager.CreateClientContext", "IAzApplication object not available");
			}
		}
		/// <summary>
		/// Creates a client context using the specified username and domain.  username/domain functionality not available in Windows XP.
		/// </summary>
		/// <param name="username">The username to create the client context</param>
		/// <param name="domain">The domain of the specified username</param>
		public void CreateClientContext(string username, string domain)
		{
			clientContext = (IAzClientContext3)azApplication.InitializeClientContextFromName(username, domain, null);
		}
		#endregion

		#region RemoveClientContext();
		/// <summary>
		/// Removes the current client context
		/// </summary>
		public void RemoveClientContext()
		{
			clientContext = null;
		}
		#endregion

		#region Debug_CheckState(); - DISABLED
		/*
		/// <summary>
		/// Write to the Trace the status of AzRoles-related objects
		/// </summary>
		public void Debug_CheckState()
		{
			TraceWrite("AzSecurityManager.Debug_CheckState", (azStore!=null?"azStore exists":"azStore is null"));
			TraceWrite("AzSecurityManager.Debug_CheckState", (azApplication!=null?"azApplication exists":"azApplication is null"));
			TraceWrite("AzSecurityManager.Debug_CheckState", (clientContext!=null?"clientContext exists":"clientContext is null"));
		}
		*/
		#endregion

		#region CreateOperation();
		public void CreateOperation(string operationName, int operationID, string operationDescription) {
			#if UseVersion1Types
			IAzOperation operation = azApplication.CreateOperation(operationName);
			#else
			IAzOperation2 operation = (IAzOperation2)azApplication.CreateOperation(operationName);
			#endif
			operation.OperationID = operationID;
			if(!string.IsNullOrEmpty(operationDescription)) {
				operation.Description = operationDescription;
			}
			operation.Submit();
		}
		#endregion

		#region GetOperations();
		#if UseVersion1Types
		public IAzOperation[] GetOperations() {
		#else
		public IAzOperation2[] GetOperations() {
		#endif
			#if UseVersion1Types
			IAzOperation[] operations = new IAzOperation[azApplication.Operations.Count];
			#else
			IAzOperation2[] operations = new IAzOperation2[azApplication.Operations.Count];
			#endif
			int x = 0;
			foreach(var operation in azApplication.Operations) {
				#if UseVersion1Types
				operations[x] = (IAzOperation)operation;
				#else
				operations[x] = (IAzOperation2)operation;
				#endif
			}
			return operations;
		}
		#endregion

		#region VerifyOperation(); - using handle of current user
		/// <summary>
		/// Verifies the operation is allowed for the current client context (user) without a defined scope. Automatically creates the client context if none exists.
		/// </summary>
		/// <param name="operation">The operation to be verified</param>
		/// <returns>True if the operation is allowed, false is the operation is denied</returns>
		public bool VerifyOperation(int operation)
		{
			return VerifyOperation(operation, null);
		}
		/// <summary>
		/// Verifies the operation is allowed for the current client context (user) within the defined scope. Automatically creates the client context if none exists.
		/// </summary>
		/// <param name="operation">The operation to be verified</param>
		/// <param name="scope">The scope of the operation</param>
		/// <returns>True if the operation is allowed, false is the operation is denied</returns>
		public bool VerifyOperation(int operation, string scope)
		{
			bool result = false;

			TraceWrite("AzSecurityManager.VerifyOperation", "Verifying \""+operation.ToString()+"\" operation for scope \""+scope+"\"");

			if(clientContext == null)
			{
				CreateClientContext();
			}
			if(clientContext != null)
			{
				#if UseVersion1Types
				object[] accessCheckResult = (object[])clientContext.AccessCheck(defaultAccessCheckObjectName, new object[1] { (scope!=null?scope:"") }, new object[1] { operation }, null, null, null, null, null);
				if(accessCheckResult != null && (int)accessCheckResult[accessCheckResult.Length-1] == 0)
				#else
				if(clientContext.AccessCheck2(defaultAccessCheckObjectName, (scope!=null?scope:""), operation) == 0)
				#endif
				{
					result = true;
				}
			}
			else
			{
				TraceWrite("AzSecurityManager.VerifyOperation", "Client context not available for access verification");
			}

			if(result)
			{
				TraceWrite("AzSecurityManager.VerifyOperation", "operation granted");
			}
			else
			{
				TraceWrite("AzSecurityManager.VerifyOperation", "operation denied");
			}

			return result;
		}
		#endregion

		#region VerifyOperation(); - using specified username and domain
		/// <summary>
		/// Verifies the operation is allowed for the current client context (user) without a defined scope. Creates the client context using the specified username/domain if one does not exist for that username/domain.
		/// </summary>
		/// <param name="operation">The operation to be verified</param>
		/// <param name="username">The username to create the client context</param>
		/// <param name="domain">The domain of the specified username</param>
		/// <returns>True if the operation is allowed, false is the operation is denied</returns>
		public bool VerifyOperation(int operation, string username, string domain)
		{
			return VerifyOperation(operation, null, username, domain);
		}
		/// <summary>
		/// Verifies the operation is allowed for the current client context (user) within the defined scope. Creates the client context using the specified username/domain if one does not exist for that username/domain.
		/// </summary>
		/// <param name="operation">The operation to be verified</param>
		/// <param name="scope">The scope of the operation</param>
		/// <param name="username">The username to create the client context</param>
		/// <param name="domain">The domain of the specified username</param>
		/// <returns>True if the operation is allowed, false is the operation is denied</returns>
		public bool VerifyOperation(int operation, string scope, string username, string domain)
		{
			bool result = false;

			TraceWrite("AzSecurityManager.VerifyOperation", "Verifying \""+operation.ToString()+"\" operation scope \""+scope+"\"");

			if(azApplication != null)
			{
				if(!(clientContext != null && this.username == username && this.domain == domain))
				{
					//init client context if doesn't exist or username/domain have changed
					CreateClientContext(username, domain);
				}
				if(clientContext != null)
				{
					result = VerifyOperation(operation, scope);
				}
				else
				{
					TraceWrite("AzSecurityManager.VerifyOperation", "Client context not available for access verification");
				}
			}
			else
			{
				TraceWrite("AzSecurityManager.VerifyOperation", "applicationName context not available for access verification");
			}

			return result;
		}
		#endregion

		#region CreateScope();
		public void CreateScope(string scopeName, string scopeDescription) {
			#if UseVersion1Types
			IAzScope scope = azApplication.CreateScope(scopeName);
			#else
			IAzScope2 scope = (IAzScope2)azApplication.CreateScope(scopeName);
			#endif
			if(!string.IsNullOrEmpty(scopeDescription)) {
				scope.Description = scopeDescription;
			}
			scope.Submit();
		}
		#endregion

		#region GetScopeNames();
		/// <summary>
		/// Gets the names for all defined scopes in the policy store and application
		/// </summary>
		/// <returns>A string array of the names of each scope</returns>
		public string[] GetScopeNames()
		{
			string[] scopes = new string[azApplication.Scopes.Count];

			int x = 0;
			#if UseVersion1Types
			foreach(IAzScope scope in azApplication.Scopes)
			#else
			foreach(IAzScope2 scope in azApplication.Scopes)
			#endif
			{
				scopes[x] = scope.Name;
				x++;
			}

			return scopes;
		}
		#endregion

		#region CreateRole();
		public void CreateRole(string roleName, string roleDescription) {
			IAzRole role = azApplication.CreateRole(roleName);
			if(!string.IsNullOrEmpty(roleDescription)) {
				role.Description = roleDescription;
			}
			role.Submit();
		}
		#endregion

		#region GetRoleNames();
		/// <summary>
		/// Retrieves the names of application roles
		/// </summary>
		/// <returns>A string array of all role names</returns>
		public string[] GetRoleNames()
		{
			string[] roles = new string[azApplication.Roles.Count];
			for(int x=0;x<azApplication.Roles.Count;x++)
			{
				roles[x] = ((IAzRole)azApplication.Roles[x]).Name;
			}
			return roles;
		}
		/// <summary>
		/// Retrieves the names of application roles within the specified scope
		/// </summary>
		/// <param name="scopeName">The name of the scope</param>
		/// <returns>A string array of all role names within the specified scope</returns>
		public string[] GetRoleNames(string scopeName)
		{
			#if UseVersion1Types
			IAzScope scope = azApplication.OpenScope(scopeName,null);
			#else
			IAzScope2 scope = azApplication.OpenScope2(scopeName);
			#endif
			string[] roles = new string[scope.Roles.Count];
			for(int x=0;x<scope.Roles.Count;x++)
			{
				roles[x] = ((IAzRole)scope.Roles[x]).Name;
			}
			scope = null;
			return roles;
		}
		#endregion
		
		#region GetRoleMemberNames();
		/// <summary>
		/// Retrieves the names of all NT accounts which are members of the specified role
		/// </summary>
		/// <param name="roleName">The name of the role</param>
		/// <returns>A string array of all member account names</returns>
		public string[] GetRoleMemberNames(string roleName)
		{
			IAzRole azRole = azApplication.OpenRole(roleName, null);
			object[] members = ((object[])azRole.MembersName);

			string[] memberNames = new string[members.Length];
			for(int x=0;x<members.Length;x++)
			{
				memberNames[x] = (string)members[x];
			}

			members = null;
			azRole = null;

			return memberNames;
		}
		#endregion

		#region AddRoleMember(); AddRoleMembers();
		/// <summary>
		/// Adds a user or group to a role
		/// </summary>
		/// <param name="userOrGroupDomainName">The full domain name of the user or group (i.e. ACS\FirstName.LastName)</param>
		/// <param name="roleName">The name of the role</param>
		public void AddRoleMember(string userOrGroupDomainName, string roleName)
		{
			IAzRole azRole = azApplication.OpenRole(roleName, null);
			azRole.AddMemberName(userOrGroupDomainName, null);
			azRole.Submit(0, null);
			azRole = null;
		}

		/// <summary>
		/// Add a list users and/or groups from a role
		/// </summary>
		/// <param name="userOrGroupDomainNames">The full domain name of the user or group (i.e. ACS\FirstName.LastName)</param>
		/// <param name="roleName">The name of the role</param>
		public void AddRoleMembers(string[] userOrGroupDomainNames, string roleName)
		{
			if(userOrGroupDomainNames != null && userOrGroupDomainNames.Length > 0)
			{
				IAzRole azRole = azApplication.OpenRole(roleName, null);
				for(int x=0; x<=userOrGroupDomainNames.Length; x++)
				{
					azRole.AddMemberName(userOrGroupDomainNames[x], null);
				}
				azRole.Submit(0, null);
				azRole = null;
			}
		}
		#endregion

		#region DeleteRoleMember(); DeleteRoleMembers();
		/// <summary>
		/// Deletes a user or group from a role
		/// </summary>
		/// <param name="userOrGroupDomainName">The full domain name of the user or group (i.e. ACS\FirstName.LastName)</param>
		/// <param name="roleName">The name of the role</param>
		public void DeleteRoleMember(string userOrGroupDomainName, string roleName)
		{
			IAzRole azRole = azApplication.OpenRole(roleName, null);
			azRole.DeleteMemberName(userOrGroupDomainName, null);
			azRole.Submit(0, null);
			azRole = null;
		}

		/// <summary>
		/// Deletes a list users and/or groups from a role
		/// </summary>
		/// <param name="userOrGroupDomainNames">The full domain name of the user or group (i.e. ACS\FirstName.LastName)</param>
		/// <param name="roleName">The name of the role</param>
		public void DeleteRoleMembers(string[] userOrGroupDomainNames, string roleName)
		{
			if(userOrGroupDomainNames != null && userOrGroupDomainNames.Length > 0)
			{
				IAzRole azRole = azApplication.OpenRole(roleName, null);
				for(int x=0; x<=userOrGroupDomainNames.Length; x++)
				{
					azRole.DeleteMemberName(userOrGroupDomainNames[x], null);
				}
				azRole.Submit(0, null);
				azRole = null;
			}
		}
		#endregion

		#region GetGroupNames();
		/// <summary>
		/// Retrieves the names of application groups
		/// </summary>
		/// <returns>A string array of all group names</returns>
		public string[] GetGroupNames()
		{
			string[] groups = new string[azApplication.ApplicationGroups.Count];
			for(int x=0; x<azApplication.ApplicationGroups.Count; x++)
			{
				#if UseVersion1Types
				groups[x] = ((IAzApplicationGroup)azApplication.ApplicationGroups[x]).Name;
				#else
				groups[x] = ((IAzApplicationGroup2)azApplication.ApplicationGroups[x]).Name;
				#endif
			}
			return groups;
		}
		/// <summary>
		/// Retrieves the names of application groups within the specified scope
		/// </summary>
		/// <param name="scopeName">The name of the scope</param>
		/// <returns>A string array of all group names within the scope</returns>
		public string[] GetGroupNames(string scopeName)
		{
			#if UseVersion1Types
			IAzScope scope = azApplication.OpenScope(scopeName, null);
			#else
			IAzScope2 scope = azApplication.OpenScope2(scopeName);
			#endif
			string[] groups = new string[scope.ApplicationGroups.Count];
			for(int x=0; x<scope.ApplicationGroups.Count; x++)
			{
				#if UseVersion1Types
				groups[x] = ((IAzApplicationGroup)scope.ApplicationGroups[x]).Name;
				#else
				groups[x] = ((IAzApplicationGroup2)scope.ApplicationGroups[x]).Name;
				#endif
			}
			scope = null;
			return groups;
		}
		#endregion

		#region GetGroupMemberNames();
		/// <summary>
		/// Retrieves the names of all accounts which are members of the specified Group
		/// </summary>
		/// <param name="groupName">The name of the Group</param>
		/// <returns>A string array of all member account names</returns>
		public string[] GetGroupMemberNames(string groupName)
		{
			#if UseVersion1Types
			IAzApplicationGroup azGroup = azApplication.OpenApplicationGroup(groupName, null);
			#else
			IAzApplicationGroup2 azGroup = (IAzApplicationGroup2)azApplication.OpenApplicationGroup(groupName, null);
			#endif
			object[] members = (object[])azGroup.MembersName;

			string[] memberNames = new string[members.Length];
			for(int x=0; x<members.Length; x++)
			{
				memberNames[x] = (string)members[x];
			}

			members = null;
			azGroup = null;

			return memberNames;
		}
		#endregion

		#region AddGroupMember(); AddGroupMembers();
		/// <summary>
		/// Adds a user or group to a Group
		/// </summary>
		/// <param name="userOrGroupDomainName">The full domain name of the user or group (i.e. ACS\FirstName.LastName)</param>
		/// <param name="groupName">The name of the Group</param>
		public void AddGroupMember(string userOrGroupDomainName, string groupName)
		{
			#if UseVersion1Types
			IAzApplicationGroup oGroup = azApplication.OpenApplicationGroup(groupName, null);
			#else
			IAzApplicationGroup2 oGroup = (IAzApplicationGroup2)azApplication.OpenApplicationGroup(groupName, null);
			#endif
			oGroup.AddMemberName(userOrGroupDomainName, null);
			oGroup.Submit(0, null);
			oGroup = null;
		}

		/// <summary>
		/// Add a list users and/or groups from a Group
		/// </summary>
		/// <param name="userOrGroupDomainNames">The full domain name of the user or group (i.e. ACS\FirstName.LastName)</param>
		/// <param name="groupName">The name of the Group</param>
		public void AddGroupMembers(string[] userOrGroupDomainNames, string groupName)
		{
			if(userOrGroupDomainNames != null && userOrGroupDomainNames.Length > 0)
			{
				#if UseVersion1Types
				IAzApplicationGroup oGroup = azApplication.OpenApplicationGroup(groupName, null);
				#else
				IAzApplicationGroup2 oGroup = (IAzApplicationGroup2)azApplication.OpenApplicationGroup(groupName, null);
				#endif
				for(int x=0; x<=userOrGroupDomainNames.Length; x++)
				{
					oGroup.AddMemberName(userOrGroupDomainNames[x], null);
				}
				oGroup.Submit(0, null);
				oGroup = null;
			}
		}
		#endregion

		#region DeleteGroupMember(); DeleteGroupMembers();
		/// <summary>
		/// Deletes a user or group from a Group
		/// </summary>
		/// <param name="userOrGroupDomainName">The full domain name of the user or group (i.e. ACS\FirstName.LastName)</param>
		/// <param name="groupName">The name of the Group</param>
		public void DeleteGroupMember(string userOrGroupDomainName, string groupName)
		{
			#if UseVersion1Types
			IAzApplicationGroup oGroup = azApplication.OpenApplicationGroup(groupName, null);
			#else
			IAzApplicationGroup2 oGroup = (IAzApplicationGroup2)azApplication.OpenApplicationGroup(groupName, null);
			#endif
			oGroup.DeleteMemberName(userOrGroupDomainName, null);
			oGroup.Submit(0, null);
			oGroup = null;
		}

		/// <summary>
		/// Deletes a list users and/or groups from a Group
		/// </summary>
		/// <param name="userOrGroupDomainNames">The full domain name of the user or group (i.e. ACS\FirstName.LastName)</param>
		/// <param name="groupName">The name of the Group</param>
		public void DeleteGroupMembers(string[] userOrGroupDomainNames, string groupName)
		{
			if(userOrGroupDomainNames != null && userOrGroupDomainNames.Length > 0)
			{
				#if UseVersion1Types
				IAzApplicationGroup oGroup = azApplication.OpenApplicationGroup(groupName, null);
				#else
				IAzApplicationGroup2 oGroup = (IAzApplicationGroup2)azApplication.OpenApplicationGroup(groupName, null);
				#endif
				for(int x=0; x<=userOrGroupDomainNames.Length; x++)
				{
					oGroup.DeleteMemberName(userOrGroupDomainNames[x], null);
				}
				oGroup.Submit(0, null);
				oGroup = null;
			}
		}
		#endregion

		#region Dispose(); ~AzSecurityManager();
		/// <summary>
		/// Releases the resources used by the AzSecurityManager
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		/// <summary>
		/// Releases the resources used by the AzSecurityManager (internally)
		/// </summary>
		/// <param name="disposing">Flag if the method is disposing</param>
		private void Dispose(bool disposing)
		{
			if(!this.disposed)
			{
				clientContext = null;
				azApplication = null;
				azStore = null;
			}

			disposed = true;
		}
		/// <summary>
		/// Class destructor if Dispose() is not called
		/// </summary>
		~AzSecurityManager()
		{
			Dispose(false);
		}
		#endregion

		#region TraceWrite(); TraceWarn();
		private void TraceWrite(string category, string message)
		{
			if(traceEnabled) {
				switch(traceType) {
					case TraceType.HttpContext:
						if(category != null)
						{
							HttpContext.Current.Trace.Write(category, message);
						}
						else
						{
							HttpContext.Current.Trace.Write(message);
						}
						break;
					case TraceType.SystemDiagnostics:
						if(category != null)
						{
							System.Diagnostics.Trace.WriteLine(category, message);
						}
						else
						{
							System.Diagnostics.Trace.WriteLine(message);
						}
						break;
				}
			}
		}
		private void TraceWarn(string category, string message, Exception exception)
		{
			if(traceEnabled)
			{
				switch(traceType) {
					case TraceType.HttpContext:
						if(category != null && exception != null)
						{
							HttpContext.Current.Trace.Warn(category, message, exception);
						}
						else if(category != null)
						{
							HttpContext.Current.Trace.Warn(category, message);
						}
						else
						{
							HttpContext.Current.Trace.Warn(message);
						}
						break;
					case TraceType.SystemDiagnostics:
						if(category != null && exception != null)
						{
							System.Diagnostics.Trace.TraceError("{0}.{1}: {2}",category, message, (message==exception.Message?exception.StackTrace:exception.Message));
						}
						else if(category != null)
						{
							System.Diagnostics.Trace.TraceError("{0}.{1}",category, message);
						}
						else
						{
							System.Diagnostics.Trace.TraceError(message);
						}
						break;
				}
			}
		}
		#endregion
	}
}