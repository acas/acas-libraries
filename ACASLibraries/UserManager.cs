using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;

namespace ACASLibraries
{
	/// <summary>
	/// The UserManager class includes functions for use with applications using integrated windows authentication.
	/// <para>Note: when using the impersonation funcionality, code must release the impersonation context when it completes.</para>
	/// </summary>
	public class UserManager
	{
		#region Username
		/// <summary>
		/// Returns the current user's Username by availability first checking if in a web application, then the WindowsIdentity and finally the current thread.  The following details the various sources used when returning the current user's Username in order of priority:
		/// <list type="bullet">
		/// <item>HttpContext.Current.User.Identity.Name</item>
		/// <item>System.Security.Principal.WindowsIdentity.Name</item>
		/// <item>System.Environment.UserName</item>
		/// </list>
		/// </summary>
		public static string Username
		{
			get
			{
				if(HttpContext.Current != null)
					return HttpContext.Current.User.Identity.Name;
				else if(System.Security.Principal.WindowsIdentity.GetCurrent() != null)
					return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
				else
					return System.Environment.UserName;
			}
		}
		#endregion

		#region WindowsIdentity
		/// <summary>
		/// Returns the current user's Username by availability first checking if in a web application, then the WindowsIdentity.  The following details the various sources used when returning the current user's Username in order of priority:
		/// <list type="bullet">
		/// <item>HttpContext.Current.User.Identity</item>
		/// <item>System.Security.Principal.WindowsIdentity</item>
		/// </list>
		/// </summary>
		public static System.Security.Principal.WindowsIdentity WindowsIdentity
		{
			get
			{
				if(HttpContext.Current != null)
					return (System.Security.Principal.WindowsIdentity)HttpContext.Current.User.Identity;
				else
					return System.Security.Principal.WindowsIdentity.GetCurrent();
			}
		}
		#endregion

		#region LastGetAccountError
		public static int LastGetAccountError = -1;
		#endregion

		#region GetAccountName();
		public static string GetAccountName(string AccountSID)
		{
			string sAccountName = null;

			int iError = Win32SecurityAPI.Errors.NO_ERROR;
			IntPtr ptrSid;
			if(!Win32SecurityAPI.ConvertStringSidToSid(AccountSID, out ptrSid))
			{
				iError = Marshal.GetLastWin32Error();
			}
			else
			{
				int iSidSize = Win32SecurityAPI.GetLengthSid(ptrSid);
				byte[] baSid = new byte[iSidSize];
				Marshal.Copy(ptrSid, baSid, 0, iSidSize);

				Win32SecurityAPI.LocalFree(ptrSid);
				//Console.WriteLine(@"Found sid {0} : {1}",sidUse,sidString);

				StringBuilder sbAccountName = new StringBuilder();
				uint cchName = (uint)sbAccountName.Capacity;
				StringBuilder referencedDomainName = new StringBuilder();
				uint cchReferencedDomainName = (uint)referencedDomainName.Capacity;
				Win32SecurityAPI.SID_NAME_USE sidUse;
				// Sid for BUILTIN\Administrators
				///byte[] Sid = new byte[] {1,2,0,0,0,0,0,5,32,0,0,0,32,2};
				//baSid = new byte[] {1,2,0,0,0,0,0,5,32,0,0,0,32,2};

				if(!Win32SecurityAPI.LookupAccountSid(null, baSid, sbAccountName, ref cchName, referencedDomainName, ref cchReferencedDomainName, out sidUse))
				{
					iError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
					if(iError == Win32SecurityAPI.Errors.ERROR_INSUFFICIENT_BUFFER)
					{
						sbAccountName.EnsureCapacity((int)cchName);
						referencedDomainName.EnsureCapacity((int)cchReferencedDomainName);
						iError = Win32SecurityAPI.Errors.NO_ERROR;
						if(!Win32SecurityAPI.LookupAccountSid(null, baSid, sbAccountName, ref cchName, referencedDomainName, ref cchReferencedDomainName, out sidUse))
						{
							iError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
						}
					}
				}
	    
				LastGetAccountError = iError;

				if(referencedDomainName != null && referencedDomainName.Length > 0)
				{
					sAccountName = referencedDomainName+"\\"+sbAccountName.ToString();
				}
				else
				{
					sAccountName = sbAccountName.ToString();
				}
			}
			
			if(!string.IsNullOrEmpty(sAccountName))
			{
				return sAccountName;
			}
			else
			{
				return null;
			}
		}
		#endregion

		#region GetAccountSID();
		public static string GetAccountSID(string AccountName)
		{
			string sSID = null;

			byte [] baSid = null;
			uint cbSid = 0;
			StringBuilder referencedDomainName = new StringBuilder();
			uint cchReferencedDomainName = (uint)referencedDomainName.Capacity;
			Win32SecurityAPI.SID_NAME_USE sidUse;

			int iError = Win32SecurityAPI.Errors.NO_ERROR;
			if(!Win32SecurityAPI.LookupAccountName(null, AccountName, baSid, ref cbSid, referencedDomainName, ref cchReferencedDomainName, out sidUse))
			{
				iError = Marshal.GetLastWin32Error();
				if(iError == Win32SecurityAPI.Errors.ERROR_INSUFFICIENT_BUFFER || iError == Win32SecurityAPI.Errors.ERROR_INVALID_FLAGS)
				{
					baSid = new byte[cbSid];
					referencedDomainName.EnsureCapacity((int)cchReferencedDomainName);
					iError = Win32SecurityAPI.Errors.NO_ERROR;
					if(!Win32SecurityAPI.LookupAccountName(null, AccountName, baSid, ref cbSid, referencedDomainName, ref cchReferencedDomainName, out sidUse))
					{
						iError = Marshal.GetLastWin32Error();
					}
				}
			}
			else
			{
				// Consider throwing an exception since no result was found
			}
            
			if(iError == 0)
			{
				IntPtr ptrSid;
				if(!Win32SecurityAPI.ConvertSidToStringSid(baSid, out ptrSid))
				{
					iError = Marshal.GetLastWin32Error();
					//Console.WriteLine(@"Could not convert sid to string. Error : {0}",err);
				}
				else
				{
					sSID = Marshal.PtrToStringAuto(ptrSid);
					Win32SecurityAPI.LocalFree(ptrSid);
					//Console.WriteLine(@"Found sid {0} : {1}",sidUse,sidString);
				}
			}
			LastGetAccountError = iError;
			
			return sSID;
		}
		#endregion

		#region User Impersonation Functions - SECURITY_IMPERSONATION_LEVEL enum; DuplicateToken(); ImpersonateUser(); ReleaseUserImpersonation();
		/// <summary>
		/// Win32 security impersonization level enumeration
		/// </summary>
		public enum SECURITY_IMPERSONATION_LEVEL : int
		{
			/// <summary>
			/// Anonymous
			/// </summary>
			SecurityAnonymous=0,
			/// <summary>
			/// Identification
			/// </summary>
			SecurityIdentification=1,
			/// <summary>
			/// Impersonation
			/// </summary>
			SecurityImpersonation=2,
			/// <summary>
			/// Delegation
			/// </summary>
			SecurityDelegation=3
		}

		/// <summary>
		/// Creates a duplicate token for the supplied token at the specified ImpersonationLevel.
		/// <para>Requires use of the win32 file &quot;advapi32.dll&quot;</para>
		/// </summary>
		/// <param name="ExistingTokenHandle"></param>
		/// <param name="ImpersonationLevel"></param>
		/// <param name="DuplicateTokenHandle"></param>
		/// <returns></returns>
		[DllImport("advapi32.dll")]
		public static extern bool DuplicateToken(IntPtr ExistingTokenHandle, short ImpersonationLevel, ref IntPtr DuplicateTokenHandle);

		/// <summary>
		/// <para>Impersonates the current user.</para>
		/// <example>
		/// //call with the following--
		/// using System.Security.Principal;
		/// WindowsImpersonationContext oImpUserID = Utility.ImpersonateUser();
		/// //release with the following--
		/// Utility.ReleaseUserImpersonation(oImpUserID);
		/// </example>
		/// </summary>
		/// <returns>The System.Security.Principal.WindowsImpersonationContext of the impersonation performed.</returns>
		public static System.Security.Principal.WindowsImpersonationContext ImpersonateUser()
		{
			IntPtr piDuplicateTokenHandle = new IntPtr(0);
			DuplicateToken((IntPtr)WindowsIdentity.Token, (short)SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, ref piDuplicateTokenHandle);
			System.Security.Principal.WindowsIdentity oWIC = new System.Security.Principal.WindowsIdentity(piDuplicateTokenHandle);
			System.Security.Principal.WindowsImpersonationContext oImpUserID = oWIC.Impersonate();
			oWIC = null;
			if(ACASLibraries.Trace.IsEnabled)
			{
				ACASLibraries.Trace.Write("ACASLibraries.UserManager.ImpersonateUser()", "User identity impersonated.");
			}
			return oImpUserID;
		}

		/// <summary>
		/// Releases an impersonation performed by the ImpersonateUser() method.
		/// </summary>
		/// <param name="ImpersonatedContext">The the System.Security.Principal.WindowsImpersonationContext of the performed impersonation.</param>
		public static void ReleaseUserImpersonation(System.Security.Principal.WindowsImpersonationContext ImpersonatedContext)
		{
			if(ImpersonatedContext != null)
			{
				ImpersonatedContext.Undo();
				ImpersonatedContext = null;
				if(ACASLibraries.Trace.IsEnabled)
				{
					ACASLibraries.Trace.Write("ACASLibraries.UserManager.ReleaseUserImpersonation()", "Impersonation context released.");
				}
			}
		}
		#endregion
	}

	#region Win32SecurityAPI
	public static class Win32SecurityAPI
	{
		[DllImport("advapi32.dll", CharSet=CharSet.Auto, SetLastError=true)]
		public static extern bool LookupAccountName(
			string lpSystemName,
			string lpAccountName,
			[MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
			ref uint cbSid,
			StringBuilder ReferencedDomainName,
			ref uint cchReferencedDomainName,
			out SID_NAME_USE peUse);

		[DllImport("advapi32.dll", CharSet=CharSet.Auto, SetLastError=true)]
		public static extern bool LookupAccountSid(
			string lpSystemName,
			[MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
			System.Text.StringBuilder lpName,
			ref uint cchName,
			System.Text.StringBuilder ReferencedDomainName,
			ref uint cchReferencedDomainName,
			out SID_NAME_USE peUse);

		[DllImport("advapi32", CharSet=CharSet.Auto, SetLastError=true)]
		public static extern bool ConvertSidToStringSid(
			[MarshalAs(UnmanagedType.LPArray)] byte[] pSID,
			out IntPtr ptrSid);

		[DllImport("advapi32.dll", SetLastError=true)]
		public static extern bool ConvertStringSidToSid(
					string StringSid,
					out IntPtr ptrSid);

		[DllImport("kernel32.dll")]
		public static extern IntPtr LocalFree(IntPtr hMem);

		[DllImport("advapi32.dll", EntryPoint="GetLengthSid", CharSet=CharSet.Auto)]
		public static extern int GetLengthSid(IntPtr pSID);

		public enum SID_NAME_USE
		{
			SidTypeUser=1,
			SidTypeGroup,
			SidTypeDomain,
			SidTypeAlias,
			SidTypeWellKnownGroup,
			SidTypeDeletedAccount,
			SidTypeInvalid,
			SidTypeUnknown,
			SidTypeComputer
		}

		public static class Errors
		{
			public const int NO_ERROR = 0;
			public const int ERROR_INVALID_PARAMETER = 87;
			public const int ERROR_INSUFFICIENT_BUFFER = 122;
			public const int ERROR_INVALID_FLAGS = 1004; // On Windows Server 2003 this error is/can be returned, but processing can still continue
			public const int ERROR_INVALID_SID = 1337;
		}
	}
	#endregion
}
