using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;

namespace ACASLibraries.Reporting.BusinessObjects4
{
	#region BORestAdapter
	/// <summary>
	/// Provides methods for sending and receiving data through the Business Objects REST API. Can be called through a specific object instance, some functionality available through static methods.
	/// </summary>
	public class BORestAdapter
	{
		//private member variables
		private string username = null;
		private string password = null;
		private BORestAuthenticationType authenticationType = BORestAuthenticationType.Enterprise;
		private string authenticationToken = null;
		private BORestConfigurationSection configurationSection = null;
		private string webServicesUrl = "http://servername:6405/biprws";

		/// <summary>
		/// Timeout used for all REST requests in milliseconds. Default is 5 minutes.
		/// </summary>
		public int Timeout = 300*1000;	//request timeout in milliseconds

		/// <summary>
		/// Indicates whether pagination is applied by default
		/// </summary>
		public const bool PaginatedDefault = true;

		/// <summary>
		///	<para>Default values for pagination. Format uses HTTP Query String style.  Example is default value.</para>
		///	<example>widthScaling=1&amp;optimized=true&amp;mode=normal</example>
		/// <remarks>See http://help.sap.com/businessobject/product_guides/boexir4/en/xi4sp5_webi_restful_ws_en.pdf page 27 for all pagination options.</remarks>
		/// </summary>
		public const string PaginationOptionsDefault = "widthScaling=1&optimized=true&mode=normal";

		#region BO REST URLs
		/// <summary>
		/// Sets the Business Objects REST web services base URL.
		/// <remarks>The port is usually 6405.</remarks>
		/// <example>http://server:port/biprws</example>
		/// </summary>
		/// <param name="url">The base URL of the web services.</param>
		public void SetWebServicesUrl(string url) {
			if(url.EndsWith("/")) {
				int endIndex = url.Length-1;
				while(url[endIndex] == '/') {
					endIndex--;
				}
				url = url.Substring(0,endIndex+1);
			}
			if(!url.StartsWith("http",StringComparison.InvariantCultureIgnoreCase)) {
				url = "http://"+url;
			}
			webServicesUrl = url;
		}
		/// <summary>
		/// Returns the complete URL used for REST logon requests.
		/// <remarks>Requires that a web services URL has already been defined.</remarks>
		/// </summary>
		/// <returns>The complete URL used for REST logon requests.</returns>
		public string GetLogonUrl() {
			return String.Concat(webServicesUrl,"/logon/long");
		}
		/// <summary>
		/// Returns the complete URL used for REST logoff requests.
		/// <remarks>Requires that a web services URL has already been defined.</remarks>
		/// </summary>
		/// <returns>The complete URL used for REST logoff requests.</returns>
		public string GetLogoffUrl() {
			return String.Concat(webServicesUrl,"/logoff");
		}
		/// <summary>
		/// Returns the complete URL used for REST infostore requests.
		/// <remarks>Requires that a web services URL has already been defined.</remarks>
		/// </summary>
		/// <returns>The complete URL used for REST infostore requests.</returns>
		public string GetInfostoreUrl() {
			return String.Concat(webServicesUrl,"/infostore");
		}
		/// <summary>
		/// Returns the complete URL used for REST document requests.
		/// <remarks>Requires that a web services URL has already been defined.</remarks>
		/// </summary>
		/// <returns>The complete URL used for REST document requests.</returns>
		public string GetDocumentUrl(int documentId) {
			return String.Concat(webServicesUrl,"/raylight/v1/documents/",documentId);
		}
		/// <summary>
		/// Returns the complete URL used for REST paginated document requests.  Documents downloaded using this URL will include pagination.
		/// <remarks>Requires that a web services URL has already been defined.</remarks>
		/// </summary>
		/// <returns>The complete URL used for REST paginated document requests.</returns>
		public string GetDocumentPaginatedUrl(int documentId, string paginationOptions) {
			return String.Concat(GetDocumentUrl(documentId), "/pages", (!string.IsNullOrEmpty(paginationOptions)?"?"+paginationOptions:""));
		}
		/// <summary>
		/// Returns the complete URL used for REST document parameters requests.
		/// <remarks>Requires that a web services URL has already been defined.</remarks>
		/// </summary>
		/// <returns>The complete URL used for REST document parameters requests.</returns>
		public string GetDocumentParametersUrl(int documentId) {
			return String.Concat(GetDocumentUrl(documentId),"/parameters");
		}
		/// <summary>
		/// Returns the complete URL used for REST document export requests.
		/// <remarks>
		/// <para>Requires that a web services URL has already been defined. Export URLs have not been tested. Dependent on Crystal web services.</para>
		/// <para>This method has NOT been tested or verified.</para>
		/// </remarks>
		/// </summary>
		/// <returns>The complete URL used for REST logon requests.</returns>
		public string GetDocumentExportUrl(int documentId, string mimeType) {
			return String.Concat(webServicesUrl,"/infostore/",documentId,"/rpt/export?mime_type=",System.Web.HttpUtility.UrlEncode(mimeType));
		}
		#endregion

		#region constructor
		public BORestAdapter() {}
		/// <summary>
		/// Initializes a new BORestAdapter object using the specified user information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services. The port is usually 6405.</param>
		public BORestAdapter(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl) {
			this.username = username;
			this.password = password;
			this.authenticationType = authenticationType;
			SetWebServicesUrl(webServicesUrl);
		}
		/// <summary>
		/// Initializes a new BORestAdapter object using information in the specified configuration section in the app config.
		/// </summary>
		/// <param name="configurationSectionName">The tag name for the configuration section defined under configuration/configurationSections/section in the application configuration file.</param>
		public BORestAdapter(string configurationSectionName)
		{
			try {
				this.configurationSection = (BORestConfigurationSection)ConfigurationManager.GetSection(configurationSectionName);
				this.username = configurationSection.Username;
				this.password = configurationSection.Password;
				SetWebServicesUrl(configurationSection.WebServicesUrl);
				if(configurationSection.Timeout > 0)
				{
					this.Timeout = configurationSection.Timeout;
				}
				this.authenticationType = configurationSection.AuthenticationType;
			}
			catch(Exception ex) {
				throw new BORestException("BO REST Exception occurred while loading configuration.", ex);
			}
		}
		#endregion

		#region ConvertPathToPathSequence();
		/// <summary>
		/// Converts a path in the form "some/specific/location/specified" to an array in the form [ "some", "specific", "location", "specified" ]. Also handles management of leading and trailing slashes.
		/// </summary>
		/// <param name="path">The path to be converted in the form "some/specific/location/specified".</param>
		/// <returns>An array in the form [ "some", "specific", "location", "specified" ].</returns>
		private string[] ConvertPathToPathSequence(string path) {
			//remove leading and trailing forward slashes
			int startIndex = 0;
			while(path[startIndex] == '/') {
				startIndex++;
			}
			int endIndex = path.Length-1;
			while(path[endIndex] == '/') {
				endIndex--;
			}
			return path.Substring(startIndex,endIndex-startIndex+1).Split('/');
		}
		#endregion

		#region GetXmlNamespaceManager();
		/// <summary>
		/// Initializes a XmlNamespace manager object for use in processing XPath queries.  Adds namespaces "is" for "http://www.w3.org/2005/Atom" and "bo" for "http://www.sap.com/rws/bip".
		/// </summary>
		/// <param name="xmlDocument">The XML document that will be queried.</param>
		/// <returns>A new XmlNamespace manager instance.</returns>
		private static XmlNamespaceManager GetXmlNamespaceManager(XmlDocument xmlDocument) {
			XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
			xmlNamespaceManager.AddNamespace("is","http://www.w3.org/2005/Atom");
			xmlNamespaceManager.AddNamespace("bo","http://www.sap.com/rws/bip");

			return xmlNamespaceManager;
		}
		#endregion

		#region FormatParameterValue();
		/// <summary>
		/// Performs data formatting for the specified value and answer type.  Answer types supported are "dateTime", "date", "time", and "boolean".  Values of type DateTime and Boolean will also be formatted.
		/// </summary>
		/// <param name="value">The value to be formatted.</param>
		/// <param name="answerType">The type string of what format should be applied.</param>
		/// <returns>The formatted value as a string.</returns>
		public static string FormatParameterValue(object value, string answerType) {
			if(value != null) {
				if(value is DateTime) {
					//2000-12-12T12:34:56.403 or possibly datetime'2000-12-12T12:34:56.403' for crystal?
					return ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
				} else if(!string.IsNullOrEmpty(value.ToString())) {
					if(string.Compare(answerType,"dateTime", true) == 0) {
						return Parser.ToDateTime(value).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
					} else if(string.Compare(answerType,"date", true) == 0) {
						return Parser.ToDateTime(value).ToString("yyyy-MM-dd");
					} else if(string.Compare(answerType,"time", true) == 0) {
						return Parser.ToDateTime(value).ToString("HH:mm:ss.fffzzz");
					} else if(string.Compare(answerType,"boolean", true) == 0) {
						return Parser.ToBool(value).ToString().ToLower();
					} else if(value is bool) {
						return value.ToString().ToLower();
					} else {
						return value.ToString();
					}
				}
			}
			return null;
		}
		#endregion

		#region GetRequest();
		/// <summary>
		/// Initializes a HttpWebRequest object and sends the POST request body if specified.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="contentType">The Content Type of the content sent during the request.</param>
		/// <param name="acceptType">The Accept type to be sent during the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		/// <param name="postData">The content to be sent during the request.</param>
		/// <returns></returns>
		private HttpWebRequest GetRequest(string httpMethod, string contentType, string acceptType, string url, string postData) {
			Stream requestStream = null;
			HttpWebRequest request = null;
			try {
				request = (HttpWebRequest)HttpWebRequest.Create(url);
				request.Timeout = Timeout;
				if(!string.IsNullOrEmpty(acceptType)) {
					request.Accept = acceptType;
				}
				if(!string.IsNullOrEmpty(contentType)) {
					request.ContentType = contentType;
				}
				request.Method = httpMethod;
				request.Headers.Add("X-SAP-LogonToken", authenticationToken);

				if(!string.IsNullOrEmpty(postData)) {
					byte[] requestBytes = Encoding.UTF8.GetBytes(postData);
					request.ContentLength = requestBytes.Length;
					requestStream = request.GetRequestStream();
					requestStream.Write(requestBytes, 0, requestBytes.Length);
					requestStream.Close();
					requestStream.Dispose();
					requestStream = null;
				}
			
			} catch(Exception ex) {
				throw new BORestException("BO REST Exception occured while processing HTTP request.", ex, request, postData);
			} finally {
				if(requestStream != null) {
					requestStream.Close();
					requestStream.Dispose();
					requestStream = null;
				}
			}

			return request;
		}
		#endregion

		#region ExecuteRequest(); ExecuteRequestAsBytes(); ExecuteRequestAsString(); ExecuteRequestAsXml(); ExecuteRequestAsStream(); ExecuteRequestAsResponse();
		/// <summary>
		/// Executes a request to the BO REST web services. Does not process or return the response.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="acceptType">The Accept type to be sent during the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		public void ExecuteRequest(string httpMethod, string acceptType, string url) {
			ExecuteRequest(httpMethod, null, acceptType, url, null);
		}
		/// <summary>
		/// Executes a request to the BO REST web services. Does not process or return the response.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		public void ExecuteRequest(string httpMethod, string url) {
			ExecuteRequest(httpMethod, null, null, url, null);
		}
		/// <summary>
		/// Executes a request to the BO REST web services. Does not process or return the response.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="contentType">The Content Type of the content sent during the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		/// <param name="postData">The content to be sent during the request.</param>
		public void ExecuteRequest(string httpMethod, string contentType, string url, string postData) {
			ExecuteRequest(httpMethod, contentType, null, url, postData);
		}
		/// <summary>
		/// Executes a request to the BO REST web services. Does not process or return the response.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="contentType">The Content Type of the content sent during the request.</param>
		/// <param name="acceptType">The Accept type to be sent during the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		/// <param name="postData">The content to be sent during the request.</param>
		public void ExecuteRequest(string httpMethod, string contentType, string acceptType, string url, string postData) {
			HttpWebRequest request = null;
			HttpWebResponse response = null;
			try {
				request = GetRequest(httpMethod, contentType, acceptType, url, postData);
				response = (HttpWebResponse)(request.GetResponse());
			} catch(Exception ex) {
				
				throw new BORestException("BO REST Exception occured while processing HTTP request.", ex, request, postData);
			} finally {
				if(response != null) {
					response.Close();
					response = null;
				}
			}
		}

		/// <summary>
		/// Executes a request to the BO REST web services and returns the response as a string.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="acceptType">The Accept type to be sent during the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		/// <returns>The response body as a string.</returns>
		public byte[] ExecuteRequestAsBytes(string httpMethod, string acceptType, string url) {
			return ExecuteRequestAsBytes(httpMethod, null, acceptType, url, null);
		}
		/// <summary>
		/// Executes a request to the BO REST web services and returns the response as a string.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="contentType">The Content Type of the content sent during the request.</param>
		/// <param name="acceptType">The Accept type to be sent during the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		/// <param name="postData">The content to be sent during the request.</param>
		/// <returns>The response body as a string.</returns>
		public byte[] ExecuteRequestAsBytes(string httpMethod, string contentType, string acceptType, string url, string postData) {
			byte[] output = null;
			HttpWebRequest request = null;
			HttpWebResponse response = null;
			Stream s = null;
			try {
				request = GetRequest(httpMethod, contentType, acceptType, url, null);
				response = (HttpWebResponse)(request.GetResponse());
				s = response.GetResponseStream();
				output = IOUtility.ReadAllBytes(s);
			} catch(Exception ex) {
				throw new BORestException("BO REST Exception occured while processing HTTP request.", ex, request, postData);
			} finally {
				if(s != null) {
					s.Close();
					s.Dispose();
					s = null;
				}
				if(response != null) {
					response.Close();
					response = null;
				}
			}
			return output;
		}
		/// <summary>
		/// Executes a request to the BO REST web services and returns the response as a string.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="acceptType">The Accept type to be sent during the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		/// <returns>The response body as a string.</returns>
		public string ExecuteRequestAsString(string httpMethod, string acceptType, string url) {
			return ExecuteRequestAsString(httpMethod, null, acceptType, url, null);
		}
		/// <summary>
		/// Executes a request to the BO REST web services and returns the response as a string.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="contentType">The Content Type of the content sent during the request.</param>
		/// <param name="acceptType">The Accept type to be sent during the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		/// <param name="postData">The content to be sent during the request.</param>
		/// <returns>The response body as a string.</returns>
		public string ExecuteRequestAsString(string httpMethod, string contentType, string acceptType, string url, string postData) {
			string output = null;
			HttpWebRequest request = null;
			HttpWebResponse response = null;
			StreamReader sr = null;
			try {
				request = GetRequest(httpMethod, contentType, acceptType, url, null);
				response = (HttpWebResponse)(request.GetResponse());
				sr = new StreamReader(response.GetResponseStream());
				output = sr.ReadToEnd();
			} catch(Exception ex) {
				throw new BORestException("BO REST Exception occured while processing HTTP request.", ex, request, postData);
			} finally {
				if(sr != null) {
					sr.Close();
					sr.Dispose();
					sr = null;
				}
				if(response != null) {
					response.Close();
					response = null;
				}
			}
			return output;
		}
		/// <summary>
		/// Executes a request to the BO REST web services and returns the response as a XmlDocument.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		/// <returns>The response body as a XmlDocument.</returns>
		public XmlDocument ExecuteRequestAsXml(string httpMethod, string url) {
			return ExecuteRequestAsXml(httpMethod, null, url, null);
		}
		/// <summary>
		/// Executes a request to the BO REST web services and returns the response as a XmlDocument.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="contentType">The Content Type of the content sent during the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		/// <param name="postData">The content to be sent during the request.</param>
		/// <returns>The response body as a XmlDocument.</returns>
		public XmlDocument ExecuteRequestAsXml(string httpMethod, string contentType, string url, string postData) {
			XmlDocument xml = null;
			HttpWebRequest request = null;
			HttpWebResponse response = null;
			Stream s = null;
			try {
				request = GetRequest(httpMethod, contentType, "application/xml", url, postData);
				response = (HttpWebResponse)(request.GetResponse());
				s = response.GetResponseStream();
				xml = new XmlDocument();
				xml.Load(s);
			} catch(Exception ex) {
				throw new BORestException("BO REST Exception occured while processing HTTP request.", ex, request, postData);
			} finally {
				if(s != null) {
					s.Close();
					s.Dispose();
					s = null;
				}
				if(response != null) {
					response.Close();
					response = null;
				}
			}
			return xml;
		}
		/// <summary>
		/// Executes a request to the BO REST web services and returns the response stream.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="acceptType">The Accept type to be sent during the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		/// <returns>The response stream.</returns>
		public Stream ExecuteRequestAsStream(string httpMethod, string acceptType, string url) {
			return ExecuteRequestAsStream(httpMethod, null, acceptType, url, null);
		}
		/// <summary>
		/// Executes a request to the BO REST web services and returns the response stream.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="contentType">The Content Type of the content sent during the request.</param>
		/// <param name="acceptType">The Accept type to be sent during the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		/// <returns>The response stream.</returns>
		public Stream ExecuteRequestAsStream(string httpMethod, string contentType, string acceptType, string url) {
			return ExecuteRequestAsStream(httpMethod, contentType, acceptType, url, null);
		}
		/// <summary>
		/// Executes a request to the BO REST web services and returns the response stream.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="contentType">The Content Type of the content sent during the request.</param>
		/// <param name="acceptType">The Accept type to be sent during the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		/// <param name="postData">The content to be sent during the request.</param>
		/// <returns>The response stream.</returns>
		public Stream ExecuteRequestAsStream(string httpMethod, string contentType, string acceptType, string url, string postData) {
			return ExecuteRequestAsResponse(httpMethod, contentType, acceptType, url, postData).GetResponseStream();
		}
		/// <summary>
		/// Executes a request to the BO REST web services and returns the HttpWebResponse object that followed the request.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="acceptType">The Accept type to be sent during the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		/// <returns>The HttpWebResponse object that followed from the request.</returns>
		public HttpWebResponse ExecuteRequestAsResponse(string httpMethod, string acceptType, string url) {
			return ExecuteRequestAsResponse(httpMethod, null, acceptType, url, null);
		}
		/// <summary>
		/// Executes a request to the BO REST web services and returns the HttpWebResponse object that followed the request.
		/// </summary>
		/// <param name="httpMethod">The HTTP method to use when sending the request.</param>
		/// <param name="contentType">The Content Type of the content sent during the request.</param>
		/// <param name="acceptType">The Accept type to be sent during the request.</param>
		/// <param name="url">The URL to which to send the request.</param>
		/// <param name="postData">The content to be sent during the request.</param>
		/// <returns>The HttpWebResponse object that followed from the request.</returns>
		public HttpWebResponse ExecuteRequestAsResponse(string httpMethod, string contentType, string acceptType, string url, string postData) {
			HttpWebResponse response = null;
			Stream requestStream = null;
			HttpWebRequest request = null;
			try {
				request = GetRequest(httpMethod, contentType, acceptType, url, postData);
				response = (HttpWebResponse)(request.GetResponse());
			} catch(Exception ex) {
				throw new BORestException("BO REST Exception occured while processing HTTP request.", ex, request, postData);
			} finally {
				if(requestStream != null) {
					requestStream.Close();
					requestStream.Dispose();
					requestStream = null;
				}
			}

			return response;
		}
		#endregion

		#region Logon();
		/// <summary>
		/// Performs a logon REST operation using the known authentication information.
		/// </summary>
		/// <returns>The result of the logon operation.</returns>
		public BORestLogonResult Logon() {
			BORestLogonResult logonResult = Logon(username, password, authenticationType, webServicesUrl);
			authenticationToken = logonResult.Token;
			return logonResult;
		}
		/// <summary>
		/// Performs a logon REST operation using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <returns>The result of the logon operation.</returns>
		public BORestLogonResult Logon(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl) {
			
			//store current instance settings to support this intermediate call
			string previousBOWebServicesBaseUrl = this.webServicesUrl;
			this.webServicesUrl = webServicesUrl;
			
			HttpWebRequest request = null;
			HttpWebResponse response = null;
			XmlDocument xml = null;
			BORestLogonResult result = null;

			string token = null;
			
			try {
				//get logon template
				xml = ExecuteRequestAsXml("GET",GetLogonUrl());

				//add namespace specifier to use xpath against BO xml documents with namespaces
				XmlNamespaceManager xmlNamespaceManager = GetXmlNamespaceManager(xml);

				//populate username, password, and authentication method
				XmlNode usernameNode = xml.SelectSingleNode("//bo:attr[@name='userName']", xmlNamespaceManager);
				usernameNode.InnerText = username;

				XmlNode passwordNode = xml.SelectSingleNode("//bo:attr[@name='password']", xmlNamespaceManager);
				passwordNode.InnerText = password;

				XmlNode authNode = xml.SelectSingleNode("//bo:attr[@name='auth']", xmlNamespaceManager);
				authNode.InnerText = Utility.GetDescription(authenticationType);

				//get token from server
				request = GetRequest("POST","application/xml","application/xml",GetLogonUrl(),xml.OuterXml);
				response = (HttpWebResponse)(request.GetResponse());
				token = response.Headers.Get("X-SAP-LogonToken");

				if(!string.IsNullOrEmpty(token)) {
					result = new BORestLogonResult(token, true, response.StatusCode, null);
				} else {
					result = new BORestLogonResult(token, false, response.StatusCode, new BORestException("Business Objects REST Exception occured. No authentication token was returned.", response.StatusDescription, request, xml.OuterXml));
				}
			}
			catch(Exception ex) {
				result = new BORestLogonResult(token,false,(response!=null?(HttpStatusCode?)(response.StatusCode):null),new BORestException("Business Objects REST Exception occured while processing logon request.", ex, request, (xml!=null?xml.OuterXml:null)));
			} finally {
				if(response != null) {
					response.Close();
					response = null;
				}
			}

			//restore original object instance settings
			this.webServicesUrl = previousBOWebServicesBaseUrl;

			return result;
		}
		#endregion

		#region Logoff();
		/// <summary>
		/// Performs a logoff REST operation using the existing authentication token.
		/// </summary>
		/// <returns>The result of the logoff operation.</returns>
		public BORestLogoffResult Logoff() {
			return Logoff(authenticationToken);
		}
		/// <summary>
		/// Performs a logoff REST operation using the existing authentication token.
		/// </summary>
		/// <param name="token">The authentication token to be logged off.</param>
		/// <returns>The result of the logoff operation.</returns>
		public BORestLogoffResult Logoff(string token) {
			
			//store current instance settings to support this intermediate call
			string previousAuthenticationToken = this.authenticationToken;
			this.authenticationToken = token;

			HttpWebRequest request = null;
			HttpWebResponse response = null;
			BORestLogoffResult result = null;
			try {
				request = GetRequest("POST",null,"application/xml",GetLogoffUrl(),null);
				response = (HttpWebResponse)(request.GetResponse());
				if(response.StatusCode == HttpStatusCode.OK) {
					result = new BORestLogoffResult(true,response.StatusCode,null);

					//clear out object token since it has been logged off
					if(this.authenticationToken == token) {
						this.authenticationToken = null;
					}
				} else {
					result = new BORestLogoffResult(false,response.StatusCode,new BORestException("Business Objects REST Exception occured while processing logoff request.", response.StatusDescription, request, null));
				}
			} catch(Exception ex) {
				result = new BORestLogoffResult(false,(response!=null?(HttpStatusCode?)(response.StatusCode):null),new BORestException("Business Objects REST Exception occured while processing logoff request.", ex, request, null));
			} finally {
				if(response != null) {
					response.Close();
					response = null;
				}
			}

			//restore original object instance settings
			if(previousAuthenticationToken != token) {
				this.authenticationToken = previousAuthenticationToken;
			}
			
			return result;
		}
		#endregion

		#region BrowseInfostore();
		/// <summary>
		/// Returns the infostore XML for the infostore root.
		/// </summary>
		/// <returns>The XML of the infostore location.</returns>
		public XmlDocument BrowseInfostore() {
			return BrowseInfostore(null, false);
		}
		/// <summary>
		/// Returns the infostore XML for the specified object cuid. If browseChildren is true, the XML for children of the object will be returned.
		/// </summary>
		/// <param name="cuid">The cuid of the infostore object.</param>
		/// <param name="browseChildren">If true, reutrns the XML for children of the object.</param>
		/// <returns>The XML of the infostore object.</returns>
		public XmlDocument BrowseInfostore(string cuid, bool browseChildren) {
			string url = string.IsNullOrEmpty(cuid)?GetInfostoreUrl():string.Concat(GetInfostoreUrl(),"/cuid_",cuid);
			if(browseChildren) {
				url += "/children";
			}
			
			return ExecuteRequestAsXml("GET", url);
		}
		/// <summary>
		/// Returns the infostore XML for the specified object id. If browseChildren is true, the XML for children of the object will be returned.
		/// </summary>
		/// <param name="id">The id of the infostore object.</param>
		/// <param name="browseChildren">If true, reutrns the XML for children of the object.</param>
		/// <returns>The XML of the infostore object.</returns>
		public XmlDocument BrowseInfostore(int id, bool browseChildren) {
			string url = string.Concat(GetInfostoreUrl(),"/",id);
			if(browseChildren) {
				url += "/children";
			}

			return ExecuteRequestAsXml("GET", url);
		}
		/// <summary>
		/// Returns the infostore XML for the object at the specified path.
		/// </summary>
		/// <param name="path">Document or folder path separated by forward slashes. Example: "Root Folder/My Documents/Some Document". Path must be unique.</param>
		/// <returns>The XML of the infostore object.</returns>
		public XmlDocument BrowseInfostore(string path) {
			return BrowseInfostore(ConvertPathToPathSequence(path));
		}
		/// <summary>
		/// Returns the infostore XML for the object at the specified path.
		/// </summary>
		/// <param name="pathSequence">An array in the form [ "Root Folder", "My Documents", "Some Document" ].</param>
		/// <returns>The XML of the infostore object.</returns>
		public XmlDocument BrowseInfostore(string[] pathSequence) {
			XmlDocument xml = BrowseInfostore();
			string objectName = null;
			StringBuilder debugName = new StringBuilder();
			int objectLevel = 0;

			if(xml != null) {
				XmlNamespaceManager xmlNamespaceManager = GetXmlNamespaceManager(xml);

				Queue<string> infostoreObjects = new Queue<string>(pathSequence);
				while(infostoreObjects.Count > 0 && xml != null) {
					objectName = infostoreObjects.Dequeue();

					if(objectLevel > 0) {
						debugName.Append("/");
					}
					debugName.Append(objectName);
					
					XmlNode objectNode = xml.SelectSingleNode(string.Concat("//is:entry[is:title='",objectName.Replace("'","&apos;"),"']/is:content/bo:attrs/bo:attr[@name=\"cuid\"]"), xmlNamespaceManager);

					if(objectNode != null) {
						xml = BrowseInfostore(objectNode.InnerText, true);
						if(xml == null) {
							break;
						}
					} else {
						xml = null;
						break;
					}
				}
			}

			if(xml == null) {
				throw new BORestException(string.Concat("BO REST Exception occurred while browsing infostore. Infostore object not found. Failed while locating ",debugName.ToString(),"."));
			}

			return xml;
		}
		#endregion

		#region GetInfostoreObjectCuid();
		/// <summary>
		/// Returns the cuid of an infostore object at the specified path.
		/// </summary>
		/// <param name="path">Document or folder path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <returns>The object's cuid.</returns>
		public string GetInfostoreObjectCuid(string path) {
			return GetInfostoreObjectCuid(ConvertPathToPathSequence(path));
		}
		/// <summary>
		/// Returns the cuid of an infostore object at the specified path sequence.
		/// </summary>
		/// <param name="pathSequence">An array in the form [ "Root Folder", "My Documents", "Some Document" ].</param>
		/// <returns>The object's cuid.</returns>
		public string GetInfostoreObjectCuid(string[] pathSequence) {
			string cuid = null;
			XmlDocument xml = BrowseInfostore();
			string objectName = null;
			StringBuilder debugName = new StringBuilder();
			int objectLevel = 0;

			if(xml != null) {
				XmlNamespaceManager xmlNamespaceManager = GetXmlNamespaceManager(xml);

				Queue<string> infostoreObjects = new Queue<string>(pathSequence);
				while(infostoreObjects.Count > 0 && xml != null) {
					objectName = infostoreObjects.Dequeue();

					if(objectLevel > 0) {
						debugName.Append("/");
					}
					debugName.Append(objectName);

					XmlNode objectNode = xml.SelectSingleNode(string.Concat("//is:entry[is:title='",objectName.Replace("'","&apos;"),"']/is:content/bo:attrs/bo:attr[@name=\"cuid\"]"), xmlNamespaceManager);
					if(objectNode != null) {
						cuid = objectNode.InnerText;

						if(infostoreObjects.Count > 0) {
							xml = BrowseInfostore(cuid, true);
							if(xml == null) {
								cuid = null;
								break;
							}
						}
						
						objectLevel++;
					} else {
						cuid = null;
						break;
					}
				}
			}

			if(cuid == null) {
				throw new BORestException(string.Concat("BO REST Exception occurred while locating infostore object cuid. Could not locate object cuid. Failed while locating ",debugName.ToString(),"."));
			}

			return cuid;
		}
		#endregion

		#region GetInfostoreObjectId();
		/// <summary>
		/// Returns the id of an infostore object at the specified path.
		/// </summary>
		/// <param name="path">Document or folder path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <returns>The object's id.</returns>
		public int GetInfostoreObjectId(string path) {
			return GetInfostoreObjectId(ConvertPathToPathSequence(path));
		}
		/// <summary>
		/// Returns the id of an infostore object at the specified path sequence.
		/// </summary>
		/// <param name="pathSequence">An array in the form [ "Root Folder", "My Documents", "Some Document" ].</param>
		/// <returns>The object's id.</returns>
		public int GetInfostoreObjectId(string[] pathSequence) {
			int id = -1;
			string objectName = null;
			StringBuilder debugName = new StringBuilder();
			int objectLevel = 0;

			XmlDocument xml = BrowseInfostore();
			if(xml != null) {
				XmlNamespaceManager xmlNamespaceManager = GetXmlNamespaceManager(xml);

				Queue<string> infostoreObjects = new Queue<string>(pathSequence);
				while(infostoreObjects.Count > 0 && xml != null) {
					objectName = infostoreObjects.Dequeue();

					if(objectLevel > 0) {
						debugName.Append("/");
					}
					debugName.Append(objectName);

					XmlNode objectNode = xml.SelectSingleNode(string.Concat("//is:entry[is:title='",objectName.Replace("'","&apos;"),"']/is:content/bo:attrs/bo:attr[@name=\"id\"]"), xmlNamespaceManager);
					if(objectNode != null) {
						if(infostoreObjects.Count > 0) {
							xml = BrowseInfostore(Parser.ToInt(objectNode.InnerText), true);
							if(xml == null) {
								break;
							}
						} else {
							id = Parser.ToInt(objectNode.InnerText);
						}
						objectLevel++;
					} else {
						break;
					}
				}
			}

			if(id == -1) {
				throw new BORestException(string.Concat("BO REST Exception occurred while locating infostore object id. Could not locate object id. Failed while locating ",debugName.ToString(),"."));
			}

			return id;
		}
		#endregion

		#region GetDocumentParameters();
		/// <summary>
		/// Returns the parameters of an infostore document at the specified path.
		/// </summary>
		/// <param name="path">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <returns>The XML defining the object's parameters.</returns>
		public XmlDocument GetDocumentParameters(string path) {
			return ExecuteRequestAsXml("GET",GetDocumentParametersUrl(GetInfostoreObjectId(path)));
		}
		/// <summary>
		/// Returns the parameters of an infostore document at the specified path.
		/// </summary>
		/// <param name="pathSequence">An array in the form [ "Root Folder", "My Documents", "Some Document" ].</param>
		/// <returns>The XML defining the object's parameters.</returns>
		public XmlDocument GetDocumentParameters(string[] pathSequence) {
			return ExecuteRequestAsXml("GET",GetDocumentParametersUrl(GetInfostoreObjectId(pathSequence)));
		}
		/// <summary>
		/// Returns the parameters of an infostore document with the specified id.
		/// </summary>
		/// <param name="documentId">The id of the document.</param>
		/// <returns>The XML defining the object's parameters.</returns>
		public XmlDocument GetDocumentParameters(int documentId) {
			return ExecuteRequestAsXml("GET",GetDocumentParametersUrl(documentId));
		}
		#endregion

		#region GetDocumentStream();
		/// <summary>
		/// Downloads a document from BO REST web services at the document path in the requested format using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, string documentPath, BORestDocumentFormat documentFormat) {
			return GetDocumentStream(username, password, authenticationType, webServicesUrl, GetInfostoreObjectId(documentPath), documentFormat, null, 0, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services at the document path with the supplied parameters in the requested format using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, string documentPath, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters) {
			return GetDocumentStream(username, password, authenticationType, webServicesUrl, GetInfostoreObjectId(documentPath), documentFormat, documentParameters, 0, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services at the document path with the supplied parameters in the requested format and DPI using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, string documentPath, BORestDocumentFormat documentFormat, int pdfDpi) {
			return GetDocumentStream(username, password, authenticationType, webServicesUrl, GetInfostoreObjectId(documentPath), documentFormat, null, pdfDpi, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services at the document path with the supplied parameters in the requested format and DPI using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, string documentPath, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi) {
			return GetDocumentStream(username, password, authenticationType, webServicesUrl, GetInfostoreObjectId(documentPath), documentFormat, documentParameters, pdfDpi, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id in the requested format using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, int documentId, BORestDocumentFormat documentFormat) {
			return GetDocumentStream(username, password, authenticationType, webServicesUrl, documentId, documentFormat, null, 0, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id with the supplied parameters in the requested format using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, int documentId, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters) {
			return GetDocumentStream(username, password, authenticationType, webServicesUrl, documentId, documentFormat, documentParameters, 0, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id with the supplied parameters in the requested format and DPI using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, int documentId, BORestDocumentFormat documentFormat, int pdfDpi) {
			return GetDocumentStream(username, password, authenticationType, webServicesUrl, documentId, documentFormat, null, pdfDpi, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id with the supplied parameters in the requested format and DPI using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, int documentId, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi) {
			return GetDocumentStream(username, password, authenticationType, webServicesUrl, documentId, documentFormat, documentParameters, pdfDpi, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id with the supplied parameters in the requested format and DPI using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <param name="paginated">Whether or not the report should be paginated. Pagination is only applied for the PDF document format.</param>
		/// <param name="paginationOptions">Pagination options in query string format.<see cref="PaginationOptionsDefault"/></param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, int documentId, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi, bool paginated, string paginationOptions) {
			//store current instance settings to support this intermediate call
			string previousUsername = this.username;
			string previousPassword = this.password;
			BORestAuthenticationType previousAuthenticationType = this.authenticationType;
			string previousBOWebServicesBaseUrl = this.webServicesUrl;
			string previousAuthenticationToken = this.authenticationToken;
			Stream s = null;

			//perform logon
			BORestLogonResult logonResult = Logon(username, password, authenticationType,webServicesUrl);
			if(logonResult.Successful) {
				s = GetDocumentStream(documentId, documentFormat, documentParameters, pdfDpi, paginated, paginationOptions);
				Logoff();
			}

			//restore original object instance settings
			this.username = previousUsername;
			this.password = previousPassword;
			this.authenticationType = authenticationType;
			this.webServicesUrl = previousBOWebServicesBaseUrl;
			this.authenticationToken = previousAuthenticationToken;

			return s;
		}

		/// <summary>
		/// Downloads a document from BO REST web services at the designated path in the requested format using the current authentication token.
		/// </summary>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(string documentPath, BORestDocumentFormat documentFormat) {
			return GetDocumentStream(GetInfostoreObjectId(documentPath), documentFormat, null, 0, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services at the designated path with the supplied parameters in the requested format using the current authentication token.
		/// </summary>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(string documentPath, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters) {
			return GetDocumentStream(GetInfostoreObjectId(documentPath), documentFormat, documentParameters, 0, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services at the designated path in the requested format and DPI using the current authentication token.
		/// </summary>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(string documentPath, BORestDocumentFormat documentFormat, int pdfDpi) {
			return GetDocumentStream(GetInfostoreObjectId(documentPath), documentFormat, null, pdfDpi, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services at the designated path with the supplied parameters in the requested format and DPI using the current authentication token.
		/// </summary>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(string documentPath, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi) {
			return GetDocumentStream(GetInfostoreObjectId(documentPath), documentFormat, documentParameters, pdfDpi, PaginatedDefault, PaginationOptionsDefault);
		}

		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id in the requested format using the current authentication token.
		/// </summary>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(int documentId, BORestDocumentFormat documentFormat) {
			return GetDocumentStream(documentId, documentFormat, null, 0, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id with the supplied parameters in the requested format using the current authentication token.
		/// </summary>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(int documentId, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters) {
			return GetDocumentStream(documentId, documentFormat, documentParameters, 0, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id in the requested format and DPI using the current authentication token.
		/// </summary>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(int documentId, BORestDocumentFormat documentFormat, int pdfDpi) {
			return GetDocumentStream(documentId, documentFormat, null, pdfDpi, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id with the supplied parameters in the requested format and DPI using the current authentication token.
		/// </summary>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(int documentId, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi) {
			return GetDocumentStream(documentId, documentFormat, documentParameters, pdfDpi, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id with the supplied parameters in the requested format and DPI using the current authentication token.
		/// </summary>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <param name="paginated">Whether or not the report should be paginated. Pagination is only applied for the PDF document format.</param>
		/// <param name="paginationOptions">Pagination options in query string format.<see cref="PaginationOptionsDefault"/></param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetDocumentStream(int documentId, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi, bool paginated, string paginationOptions) {

			string requestUrl = null;
			if(paginated && documentFormat == BORestDocumentFormat.PDF) {
				requestUrl = GetDocumentPaginatedUrl(documentId, paginationOptions);
			}
			else {
				requestUrl = GetDocumentUrl(documentId);
			}
			
			//apply query string parameters
			switch(documentFormat) {
				case BORestDocumentFormat.Excel2003WithFormulas:
				case BORestDocumentFormat.Excel2007WithFormulas:
					if(requestUrl.IndexOf("optimized=",StringComparison.InvariantCultureIgnoreCase) < 0) {
						requestUrl = string.Concat(requestUrl,(requestUrl.IndexOf('?',0)==-1?"?":"&"),"optimized=true");
					}
					break;
				case BORestDocumentFormat.PDF:
					if(pdfDpi > 0 && requestUrl.IndexOf("dpi=") == -1) {
						requestUrl= string.Concat(requestUrl,(requestUrl.IndexOf('?',0)==-1?"?":"&"),"dpi=",pdfDpi);
					}
					break;
			}

			//configure document parameters
			if(documentParameters != null && documentParameters.Count > 0) {
				XmlDocument parametersXml = null;
				try {

					//get parameters xml document as template
					parametersXml = GetDocumentParameters(documentId);

					//iterate through provided parameters
					foreach(string parameterName in documentParameters.Keys) {

						//get the values node for the parameter
						XmlNode answerNode = parametersXml.SelectSingleNode(string.Concat("//parameter[name='",parameterName,"']/answer"));
						if(answerNode != null) {
							XmlNode valuesNode = answerNode.SelectSingleNode("values");

							if(valuesNode != null) {
								string answerType = (answerNode.Attributes["type"]!=null?answerNode.Attributes["type"].InnerText:null);
								object value = documentParameters[parameterName];
								if(value is Array || value is ICollection) {

									//handle multi-value parameter
									valuesNode.RemoveAll();
									foreach(object valueItem in (IEnumerable)value) {
										XmlNode valueNode = parametersXml.CreateElement("value");
										valueNode.InnerText = FormatParameterValue(valueItem,answerType);
										valuesNode.AppendChild(valueNode);
									}
								} else {

									//handle single value parameter
									XmlNode valueNode = valuesNode.SelectSingleNode("value");
									if(valueNode == null) {
										//add value node if it does not exist
										valueNode = parametersXml.CreateNode(XmlNodeType.Element, "value", parametersXml.NamespaceURI);
										valuesNode.AppendChild(valueNode);
									}
									valueNode.InnerText = FormatParameterValue(value,answerType);
								}
							}
						}
					}

				} catch(Exception ex) {
					throw new BORestException("BO REST Exception occurred while downloading document. There was a problem applying the supplied parameters.", ex);
				}

				//send document parameters
				XmlDocument parameterRefreshXml = ExecuteRequestAsXml("PUT", "application/xml", GetDocumentParametersUrl(documentId),parametersXml.OuterXml);
			}

			//get the document as a stream
			return ExecuteRequestAsStream("GET", Utility.GetDescription(documentFormat), requestUrl);
		}
		#endregion

		#region DownloadDocument();
		/// <summary>
		/// Downloads a document from BO REST web services at the document path in the requested format using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, string documentPath, BORestDocumentFormat documentFormat) {
			return DownloadDocument(username, password, authenticationType, webServicesUrl, GetInfostoreObjectId(documentPath), documentFormat, null, 0, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services at the document path with the supplied parameters in the requested format using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, string documentPath, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters) {
			return DownloadDocument(username, password, authenticationType, webServicesUrl, GetInfostoreObjectId(documentPath), documentFormat, documentParameters, 0, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services at the document path with the supplied parameters in the requested format and DPI using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, string documentPath, BORestDocumentFormat documentFormat, int pdfDpi) {
			return DownloadDocument(username, password, authenticationType, webServicesUrl, GetInfostoreObjectId(documentPath), documentFormat, null, pdfDpi, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services at the document path with the supplied parameters in the requested format and DPI using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, string documentPath, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi) {
			return DownloadDocument(username, password, authenticationType, webServicesUrl, GetInfostoreObjectId(documentPath), documentFormat, documentParameters, pdfDpi, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id in the requested format using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, int documentId, BORestDocumentFormat documentFormat) {
			return DownloadDocument(username, password, authenticationType, webServicesUrl, documentId, documentFormat, null, 0, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id with the supplied parameters in the requested format using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, int documentId, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters) {
			return DownloadDocument(username, password, authenticationType, webServicesUrl, documentId, documentFormat, documentParameters, 0, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id with the supplied parameters in the requested format and DPI using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, int documentId, BORestDocumentFormat documentFormat, int pdfDpi) {
			return DownloadDocument(username, password, authenticationType, webServicesUrl, documentId, documentFormat, null, pdfDpi, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id with the supplied parameters in the requested format and DPI using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, int documentId, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi) {
			return DownloadDocument(username, password, authenticationType, webServicesUrl, documentId, documentFormat, documentParameters, pdfDpi, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id with the supplied parameters in the requested format and DPI using the specified authentication information.
		/// </summary>
		/// <param name="username">Username for authentication.</param>
		/// <param name="password">Password for authentication.</param>
		/// <param name="authenticationType">Authentication type for the specified username.</param>
		/// <param name="webServicesUrl">URL for the BO REST web services.</param>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <param name="paginated">Whether or not the report should be paginated. Pagination is only applied for the PDF document format.</param>
		/// <param name="paginationOptions">Pagination options in query string format.<see cref="PaginationOptionsDefault"/></param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(string username, string password, BORestAuthenticationType authenticationType, string webServicesUrl, int documentId, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi, bool paginated, string paginationOptions) {
			//store current instance settings to support this intermediate call
			string previousUsername = this.username;
			string previousPassword = this.password;
			BORestAuthenticationType previousAuthenticationType = this.authenticationType;
			string previousBOWebServicesBaseUrl = this.webServicesUrl;
			string previousAuthenticationToken = this.authenticationToken;
			byte[] data = null;

			//perform logon
			BORestLogonResult logonResult = Logon(username, password, authenticationType,webServicesUrl);
			if(logonResult.Successful) {
				data = DownloadDocument(documentId, documentFormat, documentParameters, pdfDpi, paginated, paginationOptions);
				Logoff();
			}

			//restore original object instance settings
			this.username = previousUsername;
			this.password = previousPassword;
			this.authenticationType = authenticationType;
			this.webServicesUrl = previousBOWebServicesBaseUrl;
			this.authenticationToken = previousAuthenticationToken;

			return data;
		}

		/// <summary>
		/// Downloads a document from BO REST web services at the designated path in the requested format using the current authentication token.
		/// </summary>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(string documentPath, BORestDocumentFormat documentFormat) {
			return DownloadDocument(GetInfostoreObjectId(documentPath), documentFormat, null, 0);
		}
		/// <summary>
		/// Downloads a document from BO REST web services at the designated path with the supplied parameters in the requested format using the current authentication token.
		/// </summary>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(string documentPath, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters) {
			return DownloadDocument(GetInfostoreObjectId(documentPath), documentFormat, documentParameters, 0);
		}
		/// <summary>
		/// Downloads a document from BO REST web services at the designated path in the requested format and DPI using the current authentication token.
		/// </summary>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(string documentPath, BORestDocumentFormat documentFormat, int pdfDpi) {
			return DownloadDocument(GetInfostoreObjectId(documentPath), documentFormat, null, pdfDpi);
		}
		/// <summary>
		/// Downloads a document from BO REST web services at the designated path with the supplied parameters in the requested format and DPI using the current authentication token.
		/// </summary>
		/// <param name="documentPath">Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document"</param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(string documentPath, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi) {
			return DownloadDocument(GetInfostoreObjectId(documentPath), documentFormat, documentParameters, pdfDpi);
		}

		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id in the requested format using the current authentication token.
		/// </summary>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(int documentId, BORestDocumentFormat documentFormat) {
			return DownloadDocument(documentId, documentFormat, null, 0);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id with the supplied parameters in the requested format using the current authentication token.
		/// </summary>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(int documentId, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters) {
			return DownloadDocument(documentId, documentFormat, documentParameters, 0);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id in the requested format and DPI using the current authentication token.
		/// </summary>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(int documentId, BORestDocumentFormat documentFormat, int pdfDpi) {
			return DownloadDocument(documentId, documentFormat, null, pdfDpi);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id with the supplied parameters in the requested format and DPI using the current authentication token.
		/// </summary>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(int documentId, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi) {
			return DownloadDocument(documentId, documentFormat, documentParameters, pdfDpi, PaginatedDefault, PaginationOptionsDefault);
		}
		/// <summary>
		/// Downloads a document from BO REST web services having the supplied document id with the supplied parameters in the requested format and DPI using the current authentication token.
		/// </summary>
		/// <param name="documentId">The id of the document. Can be obtained using <see cref="GetInfostoreObjectId">GetInfostoreObjectId</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <param name="paginated">Whether or not the report should be paginated. Pagination is only applied for the PDF document format.</param>
		/// <param name="paginationOptions">Pagination options in query string format.<see cref="PaginationOptionsDefault"/></param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadDocument(int documentId, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi, bool paginated, string paginationOptions) {
			
			string requestUrl = null;
			if(paginated && documentFormat == BORestDocumentFormat.PDF) {
				requestUrl = GetDocumentPaginatedUrl(documentId, paginationOptions);
			} else {
				requestUrl = GetDocumentUrl(documentId);
			}
			
			//apply query string parameters
			switch(documentFormat) {
				case BORestDocumentFormat.Excel2003WithFormulas:
				case BORestDocumentFormat.Excel2007WithFormulas:
					if(requestUrl.IndexOf("optimized=", StringComparison.InvariantCultureIgnoreCase) == -1) {
						requestUrl = string.Concat(requestUrl,(requestUrl.IndexOf('?')==-1?"?":"&"),"optimized=true");
					}
					break;
				case BORestDocumentFormat.PDF:
					if(pdfDpi > 0 && requestUrl.IndexOf("dpi=") == -1) {
						requestUrl= string.Concat(requestUrl,(requestUrl.IndexOf('?')==-1?"?":"&"),"dpi=",pdfDpi);
					}
					break;
			}

			//configure and refresh document using any supplied parameters
			XmlDocument parametersXml = null;
			try {

				//get parameters xml document as template
				parametersXml = ApplyParameters(GetDocumentParameters(documentId), documentParameters);

			} catch(Exception ex) {
				throw new BORestException("BO REST Exception occurred while downloading document. There was a problem applying the supplied parameters.", ex);
			}

			ExecuteRequest("PUT", "application/xml", "application/xml", GetDocumentParametersUrl(documentId),parametersXml.OuterXml);

			return ExecuteRequestAsBytes("GET", Utility.GetDescription(documentFormat), requestUrl);
		}
		#endregion

		#region ApplyParameters();
		private static XmlDocument ApplyParameters(XmlDocument parametersXml, Dictionary<string,object> documentParameters) {
			//iterate through provided parameters
			if(documentParameters != null && documentParameters.Count > 0) {
				foreach(string parameterName in documentParameters.Keys) {

					//get the values node for the parameter
					XmlNode answerNode = parametersXml.SelectSingleNode(string.Concat("//parameter[name='",parameterName,"']/answer"));
					if(answerNode != null) {
						XmlNode valuesNode = answerNode.SelectSingleNode("values");

						if(valuesNode != null) {
							string answerType = (answerNode.Attributes["type"]!=null?answerNode.Attributes["type"].InnerText:null);
							object value = documentParameters[parameterName];
							if(value is Array || value is ICollection) {

								//handle multi-value parameter
								valuesNode.RemoveAll();
								foreach(object valueItem in (IEnumerable)value) {
									XmlNode valueNode = parametersXml.CreateElement("value");
									valueNode.InnerText = FormatParameterValue(valueItem,answerType);
									valuesNode.AppendChild(valueNode);
								}
							} else {

								//handle single value parameter
								XmlNode valueNode = valuesNode.SelectSingleNode("value");
								if(valueNode == null) {
									//add value node if it does not exist
									valueNode = parametersXml.CreateNode(XmlNodeType.Element, "value", parametersXml.NamespaceURI);
									valuesNode.AppendChild(valueNode);
								}
								valueNode.InnerText = FormatParameterValue(value,answerType);
							}
						}
					}
				}
			}

			//remove unrequired nodes
			XmlNodeList nodesToRemove = parametersXml.SelectNodes("//name | //info");
			foreach(XmlNode node in nodesToRemove) {
				node.ParentNode.RemoveChild(node);
			}

			//remove all attributes
			XmlNodeList nodesWithAttributes = parametersXml.SelectNodes("//*[@*]");
			foreach(XmlNode node in nodesWithAttributes) {
				node.Attributes.RemoveAll();
			}
			
			return parametersXml;		
		}
		#endregion

		#region GetConfiguredDocumentStream();
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id in the requested format using the authentication information in the application configuration .
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetConfiguredDocumentStream(string id, BORestDocumentFormat documentFormat) {
			return GetConfiguredDocumentStream(id, documentFormat, null, 0, null, null);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id with the supplied parameters in the requested format using the authentication information in the application configuration .
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
				/// <returns>A stream containing the requested document.</returns>
		public Stream GetConfiguredDocumentStream(string id, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters) {
			return GetConfiguredDocumentStream(id, documentFormat, documentParameters, 0, null, null);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id in the requested format and DPI using the authentication information in the application configuration .
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetConfiguredDocumentStream(string id, BORestDocumentFormat documentFormat, int pdfDpi) {
			return GetConfiguredDocumentStream(id, documentFormat, null, pdfDpi, null, null);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id with the supplied parameters in the requested format and DPI using the authentication information in the application configuration .
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetConfiguredDocumentStream(string id, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi) {
			return GetConfiguredDocumentStream(id, documentFormat, documentParameters, pdfDpi, null, null);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id with the supplied parameters in the requested format and DPI using the authentication information in the application configuration .
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <param name="paginated">Whether or not the report should be paginated. If null, uses configured pagination setting.</param>
		/// <param name="paginationOptions">Pagination options in query string format. If null, uses configured pagination options<see cref="PaginationOptionsDefault"/></param>. Use String.Empty to override default pagination options.
		/// <returns>A stream containing the requested document.</returns>
		public Stream GetConfiguredDocumentStream(string id, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi, bool paginated, string paginationOptions) {
			return GetConfiguredDocumentStream(id, documentFormat, documentParameters, pdfDpi, (bool?)paginated, PaginationOptionsDefault);
		}
		private Stream GetConfiguredDocumentStream(string id, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi, bool? paginated, string paginationOptions) {
			BORestConfigurationDocumentElement documentElement = null;

			try {
				documentElement = configurationSection.Documents[id];

				if(paginated == null) {
					paginated = documentElement.Paginated;
				}
				if(paginationOptions == null) {
					paginationOptions = documentElement.PaginationOptions;
				}

				//apply default values
				foreach(BORestConfigurationParameterElement parameterElement in documentElement.Parameters) {
					if((!documentParameters.ContainsKey(parameterElement.ID) || documentParameters[parameterElement.ID] == null) && !string.IsNullOrEmpty(parameterElement.DefaultValue)) {
						documentParameters[parameterElement.ID] = parameterElement.DefaultValue;
					}
				}
			} catch(Exception ex) {
				throw new BORestException("BO REST Exception occurred while evaluating configuration document parameters.", ex);
			}

			//download document
			if(documentElement.DocumentID > 0) {
				return GetDocumentStream(documentElement.DocumentID, documentFormat, documentParameters, pdfDpi, paginated.Value, paginationOptions);
			} else {
				return GetDocumentStream(GetInfostoreObjectId(documentElement.DocumentPath), documentFormat, documentParameters, pdfDpi, paginated.Value, paginationOptions);
			}
		}

		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id in the requested format using the authentication information in the application configuration within the specified configuration section.
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="configurationSectionName">The tag name for the configuration section defined under configuration/configurationSections/section in the application configuration file.</param>
		/// <returns>A stream containing the requested document.</returns>
		public static Stream GetConfiguredDocumentStream(string id, BORestDocumentFormat documentFormat, string configurationSectionName) {
			return GetConfiguredDocumentStream(id, documentFormat, null, 0, null, null, configurationSectionName);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id with the supplied parameters in the requested format using the authentication information in the application configuration within the specified configuration section.
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="configurationSectionName">The tag name for the configuration section defined under configuration/configurationSections/section in the application configuration file.</param>
		/// <returns>A stream containing the requested document.</returns>
		public static Stream GetConfiguredDocumentStream(string id, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, string configurationSectionName) {
			return GetConfiguredDocumentStream(id, documentFormat, documentParameters, 0, null, null, configurationSectionName);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id in the requested format and DPI using the authentication information in the application configuration within the specified configuration section.
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <param name="configurationSectionName">The tag name for the configuration section defined under configuration/configurationSections/section in the application configuration file.</param>
		/// <returns>A stream containing the requested document.</returns>
		public static Stream GetConfiguredDocumentStream(string id, BORestDocumentFormat documentFormat, int pdfDpi, string configurationSectionName) {
			return GetConfiguredDocumentStream(id, documentFormat, null, pdfDpi, null, null, configurationSectionName);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id in the requested format and DPI using the authentication information in the application configuration within the specified configuration section.
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="paginated">Whether or not the report should be paginated. Pagination is only applied for the PDF document format.</param>
		/// <param name="paginationOptions">Pagination options in query string format.<see cref="PaginationOptionsDefault"/></param>
		/// <param name="configurationSectionName">The tag name for the configuration section defined under configuration/configurationSections/section in the application configuration file.</param>
		/// <returns>A stream containing the requested document.</returns>
		public static Stream GetConfiguredDocumentStream(string id, BORestDocumentFormat documentFormat, bool paginated, string paginationOptions, string configurationSectionName) {
			return GetConfiguredDocumentStream(id, documentFormat, null, 0, paginated, paginationOptions, configurationSectionName);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id with the supplied parameters in the requested format using the authentication information in the application configuration within the specified configuration section.
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="paginated">Whether or not the report should be paginated. Pagination is only applied for the PDF document format.</param>
		/// <param name="paginationOptions">Pagination options in query string format.<see cref="PaginationOptionsDefault"/></param>
		/// <param name="configurationSectionName">The tag name for the configuration section defined under configuration/configurationSections/section in the application configuration file.</param>
		/// <returns>A stream containing the requested document.</returns>
		public static Stream GetConfiguredDocumentStream(string id, BORestDocumentFormat documentFormat, Dictionary<string, object> documentParameters, bool paginated, string paginationOptions, string configurationSectionName) {
			return GetConfiguredDocumentStream(id, documentFormat, documentParameters, 0, paginated, paginationOptions, configurationSectionName);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id with the supplied parameters in the requested format and DPI using the authentication information in the application configuration within the specified configuration section.
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <param name="paginated">Whether or not the report should be paginated. Pagination is only applied for the PDF document format.</param>
		/// <param name="paginationOptions">Pagination options in query string format.<see cref="PaginationOptionsDefault"/></param>
		/// <param name="configurationSectionName">The tag name for the configuration section defined under configuration/configurationSections/section in the application configuration file.</param>
		/// <returns>A stream containing the requested document.</returns>
		public static Stream GetConfiguredDocumentStream(string id, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi, bool? paginated, string paginationOptions, string configurationSectionName) {
			BORestAdapter restAdapter = new BORestAdapter(configurationSectionName);
			restAdapter.Logon();
			Stream s = restAdapter.GetConfiguredDocumentStream(id, documentFormat, documentParameters, pdfDpi, paginated, paginationOptions);
			restAdapter.Logoff();
			return s;
		}
		#endregion

		#region DownloadConfiguredDocument();
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id in the requested format using the authentication information in the application configuration .
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadConfiguredDocument(string id, BORestDocumentFormat documentFormat) {
			return this.DownloadConfiguredDocument(id, documentFormat, null, 0, null, null, true);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id with the supplied parameters in the requested format using the authentication information in the application configuration .
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadConfiguredDocument(string id, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters) {
			return this.DownloadConfiguredDocument(id, documentFormat, documentParameters, 0, null, null, true);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id in the requested format and DPI using the authentication information in the application configuration .
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadConfiguredDocument(string id, BORestDocumentFormat documentFormat, int pdfDpi) {
			return this.DownloadConfiguredDocument(id, documentFormat, null, pdfDpi, null, null, true);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id with the supplied parameters in the requested format and DPI using the authentication information in the application configuration .
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <param name="paginated">Whether or not the report should be paginated. Pagination is only applied for the PDF document format.</param>
		/// <param name="paginationOptions">Pagination options in query string format.<see cref="PaginationOptionsDefault"/></param>
		/// <returns>A stream containing the requested document.</returns>
		public byte[] DownloadConfiguredDocument(string id, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi, bool paginated, string paginationOptions) {
			return this.DownloadConfiguredDocument(id, documentFormat, documentParameters, pdfDpi, paginated, paginationOptions, true);
		}
		/// <summary>
		/// The arbitraryParameter parameter forces C# to distinguish between the static and non-static methods.  Use of "this" keyword was not providing this as expected.  Can be true or false since it is ignored.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="documentFormat"></param>
		/// <param name="documentParameters"></param>
		/// <param name="pdfDpi"></param>
		/// <param name="paginated"></param>
		/// <param name="paginationOptions">Pagination options in query string format.<see cref="PaginationOptionsDefault"/></param>
		/// <param name="arbitraryParameter">Parameter used to handle conflicts between static and non-static methods.  Since this is a private method, it should not present an issue to users of the API.</param>
		/// <returns></returns>
		private byte[] DownloadConfiguredDocument(string id, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi, bool? paginated, string paginationOptions, bool arbitraryParameter) {
			BORestConfigurationDocumentElement documentElement = null;

			try {
				documentElement = configurationSection.Documents[id];

				if(paginated == null) {
					paginated = documentElement.Paginated;
				}
				if(paginationOptions == null) {
					paginationOptions = documentElement.PaginationOptions;
				}			

				//apply default values
				foreach(BORestConfigurationParameterElement parameterElement in documentElement.Parameters) {
					if((!documentParameters.ContainsKey(parameterElement.ID) || documentParameters[parameterElement.ID] == null) && !string.IsNullOrEmpty(parameterElement.DefaultValue)) {
						documentParameters[parameterElement.ID] = parameterElement.DefaultValue;
					}
				}

			} catch(Exception ex) {
				throw new BORestException("BO REST Exception occurred while evaluating configuration document parameters.", ex);
			}

			//download document
			if(documentElement.DocumentID > 0) {
				return DownloadDocument(documentElement.DocumentID, documentFormat, documentParameters, pdfDpi, paginated.Value, paginationOptions);
			} else {
				return DownloadDocument(GetInfostoreObjectId(documentElement.DocumentPath), documentFormat, documentParameters, pdfDpi, paginated.Value, paginationOptions);
			}
		}

		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id in the requested format using the authentication information in the application configuration within the specified configuration section.
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="configurationSectionName">The tag name for the configuration section defined under configuration/configurationSections/section in the application configuration file.</param>
		/// <returns>A stream containing the requested document.</returns>
		public static byte[] DownloadConfiguredDocument(string id, BORestDocumentFormat documentFormat, string configurationSectionName) {
			return DownloadConfiguredDocument(id, documentFormat, null, 0, null, null, configurationSectionName);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id with the supplied parameters in the requested format using the authentication information in the application configuration within the specified configuration section.
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="configurationSectionName">The tag name for the configuration section defined under configuration/configurationSections/section in the application configuration file.</param>
		/// <returns>A stream containing the requested document.</returns>
		public static byte[] DownloadConfiguredDocument(string id, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, string configurationSectionName) {
			return DownloadConfiguredDocument(id, documentFormat, documentParameters, 0, null, null, configurationSectionName);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id in the requested format and DPI using the authentication information in the application configuration within the specified configuration section.
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <param name="configurationSectionName">The tag name for the configuration section defined under configuration/configurationSections/section in the application configuration file.</param>
		/// <returns>A stream containing the requested document.</returns>
		public static byte[] DownloadConfiguredDocument(string id, BORestDocumentFormat documentFormat, int pdfDpi, string configurationSectionName) {
			return DownloadConfiguredDocument(id, documentFormat, null, pdfDpi, null, null, configurationSectionName);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id with the supplied parameters in the requested format and DPI using the authentication information in the application configuration within the specified configuration section.
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <param name="configurationSectionName">The tag name for the configuration section defined under configuration/configurationSections/section in the application configuration file.</param>
		/// <returns>A stream containing the requested document.</returns>
		public static byte[] DownloadConfiguredDocument(string id, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi, string configurationSectionName) {
			return DownloadConfiguredDocument(id, documentFormat, documentParameters, pdfDpi, null, null, configurationSectionName);
		}
		/// <summary>
		/// Downloads a document from BO REST web services defined in the application configuration file with the designated document element id with the supplied parameters in the requested format and DPI using the authentication information in the application configuration within the specified configuration section.
		/// </summary>
		/// <param name="id">ID matching a defined the document element attribute in the application configuration file. <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see></param>
		/// <param name="documentFormat">Format of the document to be returned.</param>
		/// <param name="documentParameters">Parameters to be used to refresh the requested document.</param>
		/// <param name="pdfDpi">The DPI (i.e. output resolution) if using PDF document format. DPI is an optional parameter.</param>
		/// <param name="paginated">Whether or not the report should be paginated. Pagination is only applied for the PDF document format.</param>
		/// <param name="paginationOptions">Pagination options in query string format.<see cref="PaginationOptionsDefault"/></param>
		/// <param name="configurationSectionName">The tag name for the configuration section defined under configuration/configurationSections/section in the application configuration file.</param>
		/// <returns>A stream containing the requested document.</returns>
		public static byte[] DownloadConfiguredDocument(string id, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi, bool paginated, string paginationOptions, string configurationSectionName) {
			return DownloadConfiguredDocument(id, documentFormat, documentParameters, pdfDpi, (bool?)paginated, paginationOptions, configurationSectionName);
		}
		public static byte[] DownloadConfiguredDocument(string id, BORestDocumentFormat documentFormat, Dictionary<string,object> documentParameters, int pdfDpi, bool? paginated, string paginationOptions, string configurationSectionName) {
			BORestAdapter restAdapter = new BORestAdapter(configurationSectionName);
			restAdapter.Logon();
			byte[] data = restAdapter.DownloadConfiguredDocument(id, documentFormat, documentParameters, pdfDpi, paginated, paginationOptions, true);
			restAdapter.Logoff();
			return data;
		}
		#endregion
	}
	#endregion

	#region BORestLogonResult
	/// <summary>
	/// Result of a logon request
	/// </summary>
	public class BORestLogonResult {
		/// <summary>
		/// If the logon request was successful
		/// </summary>
		public readonly bool Successful;
		/// <summary>
		/// HTTP status code returned by server
		/// </summary>
		public readonly HttpStatusCode? HttpStatusCode;
		/// <summary>
		/// Exception raised during logon process
		/// </summary>
		public readonly Exception Exception;
		/// <summary>
		/// Authentican token for user session returned by server
		/// </summary>
		public readonly string Token;

		public BORestLogonResult(string token, bool success, HttpStatusCode? httpStatusCode, Exception exception) {
			Token = token;
			Successful = success;
			HttpStatusCode = httpStatusCode;
			Exception = exception;
		}
	}
	#endregion

	#region BORestLogoffResult
	/// <summary>
	/// Result of a logoff request.
	/// </summary>
	public class BORestLogoffResult {
		/// <summary>
		/// If the logoff request was successful
		/// </summary>
		public readonly bool Successful;
		/// <summary>
		/// HTTP status code returned by server
		/// </summary>
		public readonly HttpStatusCode? HttpStatusCode;
		/// <summary>
		/// Exception raised during logoff process
		/// </summary>
		public readonly Exception Exception;

		public BORestLogoffResult(bool success, HttpStatusCode? httpStatusCode, Exception exception) {
			Successful = success;
			HttpStatusCode = httpStatusCode;
			Exception = exception;
		}
	}
	#endregion

	#region BORestAuthenticationType
	/// <summary>
	/// Authentication types used to authenticate with Business Objects.  Specific Business Objects strings stored in enum value <see cref="System.ComponentModel.DescriptionAttribute">Description</see> attributes.
	/// </summary>
	public enum BORestAuthenticationType {
		[Description("secEnterprise")]
		Enterprise,
		[Description("secLDAP")]
		LDAP,
		[Description("secWinAD")]
		WinAD,
		[Description("secSAPR3")]
		SAPR3
	}
	#endregion

	#region BORestDocumentFormat
	/// <summary>
	/// Possible document formats that can be used when downloading documents from Business Objects.  Mime format for each document type is stored in enum value <see cref="System.ComponentModel.DescriptionAttribute">Description</see> attributes.
	/// </summary>
	public enum BORestDocumentFormat {
		[Description("text/xml")]
		XML,
		[Description("application/pdf")]
		PDF,
		[Description("application/vnd.ms-excel")]
		Excel2003,
		[Description("application/vnd.ms-excel")]
		Excel2003WithFormulas,
		[Description("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
		Excel2007,
		[Description("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
		Excel2007WithFormulas
	}
	#endregion

	#region BORestException
	/// <summary>
	/// Exception wrapper for BO REST Exceptions. If available, it will include the request and response objects and body.
	/// </summary>
	public class BORestException : Exception {
		/// <summary>
		/// The request object associated with the exception.
		/// </summary>
		public readonly HttpWebRequest Request;
		/// <summary>
		/// The body of the request associated with the exception.
		/// </summary>
		public readonly string RequestBody;
		/// <summary>
		/// The response object associated with the exception.
		/// </summary>
		public HttpWebResponse Response {
			get {
				if(this.InnerException is WebException) {
					return (HttpWebResponse)(((WebException)InnerException).Response);
				} else {
					return null;
				}
			}
		}
		/// <summary>
		/// The body of the response associated with the exception.
		/// </summary>
		public readonly string ResponseBody;

		public BORestException(string message) : base(message) { }
		public BORestException(string message, string responseBody) : base(message) {
			this.ResponseBody = responseBody;
		}
		public BORestException(string message, string responseBody, HttpWebRequest request, string requestBody) : base(message) {
			this.Request = request;
			this.RequestBody = requestBody;
			this.ResponseBody = responseBody;
		}
		public BORestException(string message, Exception innerException, string responseBody) : base(message,innerException) {
			this.ResponseBody = responseBody;
		}
		public BORestException(string message, Exception innerException, string responseBody, HttpWebRequest request, string requestBody) : base(message,innerException) {
			this.Request = request;
			this.RequestBody = requestBody;
			this.ResponseBody = responseBody;
		}
		public BORestException(string message, Exception innerException, HttpWebRequest request, string requestBody) : base(message,innerException) {
			this.Request = request;
			this.RequestBody = requestBody;

			if(innerException is WebException) {
				string responseBody = null;
				HttpWebResponse response = null;
				StreamReader sr = null;

				try {
					response = (HttpWebResponse)((WebException)innerException).Response;
					sr = new StreamReader(response.GetResponseStream());
					responseBody = sr.ReadToEnd();
				} finally {
					if(sr != null) {
						sr.Close();
						sr.Dispose();
						sr = null;
					}
					if(response != null) {
						response.Close();
					}
				}

				this.ResponseBody = responseBody;
			} else {
				this.ResponseBody = null;
			}
		}
		public BORestException(string message, Exception innerException) : base(message,innerException) {
			if(innerException is WebException) {
				string responseBody = null;
				HttpWebResponse response = null;
				StreamReader sr = null;

				try {
					response = (HttpWebResponse)((WebException)innerException).Response;
					sr = new StreamReader(response.GetResponseStream());
					responseBody = sr.ReadToEnd();
				} finally {
					if(sr != null) {
						sr.Close();
						sr.Dispose();
						sr = null;
					}
					if(response != null) {
						response.Close();
					}
				}

				this.ResponseBody = responseBody;
			} else {
				this.ResponseBody = null;
			}
		}
	}
	#endregion


	#region BORestConfigurationSection
	/// <summary>
	/// Configuration wrapper for embedding BO Rest configuration information within an application configuration file. Below is an example BO Rest configuration. Timeout is in milliseconds. The default port is usually 6405.
	/// <example>
	/// 
	/// &lt;configuration&gt;
	/// 
	///		&lt;configSections&gt;
	///			&lt;section name="BORestConfiguration" type="ACASLibraries.Reporting.BusinessObjects4.BORestConfigurationSection,ACASLibraries.Reporting"/&gt;
	///		&lt;/configSections&gt;
	/// 
	/// 	&lt;BORestConfiguration username="user1" password="pass1" timeout="300000" authenticationType="Enterprise" webServicesUrl="http://myserver:port/biprws"&gt;
	/// 		&lt;documents&gt;
	/// 		  &lt;document id="Report1" paginated="true" documentPath="Root Folder/Folder 1/Folder2/Report Document 1"&gt;
	/// 			&lt;parameters&gt;
	/// 			  &lt;parameter id="RecordID" multivalue="true"/&gt;
	/// 			&lt;/parameters&gt;
	/// 		  &lt;/document&gt;
	/// 		  &lt;document id="Report2" documentPath="Root Folder/Folder 1/Folder2/Report Document 2"&gt;
	/// 			&lt;parameters&gt;
	/// 			  &lt;parameter id="CurrencyCode" defaultValue="USD"/&gt;
	/// 			  &lt;parameter id="ShowDetails" defaultValue="0"/&gt;;
	/// 			&lt;/parameters&gt;
	/// 		  &lt;/document&gt;
	/// 		&lt;/documents&gt;		
	/// 	&lt;/BORestConfiguration&gt;
	/// 
	/// &lt;/configuration&gt;
	/// 
	/// </example>
	/// </summary>
	public class BORestConfigurationSection : ConfigurationSection
	{
		/// <summary>
		/// Set of configured documents that include information so that application code can refer to a document's ID and quickly access the document.
		/// </summary>
		[ConfigurationValidator(typeof(BORestConfigurationValidator))]
		[ConfigurationProperty("documents", IsRequired=false)]
		public BORestConfigurationDocumentElementCollection Documents
		{
			get
			{
				return (BORestConfigurationDocumentElementCollection)this["documents"];
			}
			set
			{
				this["documents"] = value;
			}
		}

		/// <summary>
		/// Username of the account used to access reports in Business Objects
		/// </summary>
		[ConfigurationProperty("username", IsRequired=true)]
		public string Username
		{
			get
			{
				return Parser.ToString(this["username"]);
			}
			set
			{
				this["username"] = value;
			}
		}

		/// <summary>
		/// Password of the account used to access reports in Business Objects
		/// </summary>
		[ConfigurationProperty("password", IsRequired=true)]
		public string Password
		{
			get
			{
				return Parser.ToString(this["password"]);
			}
			set
			{
				this["password"] = value;
			}
		}

		/// <summary>
		/// URL of Business Objects REST web services.
		/// <example>http://servername:6405/biprws</example>
		/// </summary>
		[ConfigurationProperty("webServicesUrl", IsRequired=true)]
		public string WebServicesUrl
		{
			get
			{
				return Parser.ToString(this["webServicesUrl"]);
			}
			set
			{
				this["webServicesUrl"] = value;
			}
		}

		/// <summary>
		/// Optional. Timeout used for all REST requests in milliseconds. Default is 5 minutes.
		/// </summary>
		[ConfigurationProperty("timeout", IsRequired=false)]
		public int Timeout
		{
			get
			{
				return Parser.ToInt(this["timeout"]);
			}
			set
			{
				this["timeout"] = value;
			}
		}

		/// <summary>
		/// Authentication method used for authentication of username and password.  Values must match <see cref="BORestAuthenticationType">BORestAuthenticationType</see>:
		/// <list type="bullet">
		/// <item><description>Enterprise</description></item>
		/// <item><description>LDAP</description></item>
		/// <item><description>WinAD</description></item>
		/// <item><description>SAPR3</description></item>
		/// </list>
		/// <example>Enterprise</example>
		/// </summary>
		[ConfigurationProperty("authenticationType", IsRequired=true)]
		public BORestAuthenticationType AuthenticationType
		{
			get
			{
				return Parser.ToEnum<BORestAuthenticationType>(this["authenticationType"], BORestAuthenticationType.Enterprise);
			}
			set
			{
				this["authenticationType"] = value;
			}
		}

		#region GetCurrentValue();
		public static object GetCurrentValue(string PropertyName, string SectionName)
		{
			return ((BORestConfigurationSection)ConfigurationManager.GetSection(SectionName)).Properties[PropertyName];
		}
		#endregion

		#region GetDocument();
		public static BORestConfigurationDocumentElement GetDocument(string id, string configurationSectionName) {
			BORestConfigurationDocumentElement document = null;
			
			BORestConfigurationSection config = (BORestConfigurationSection)ConfigurationManager.GetSection(configurationSectionName);
			if(config != null) {
				document = config.Documents[id];
			}

			return document;
		}
		#endregion

		#region GetDocumentParameters();
		/// <summary>
		/// Returns a dictionary containing the parameters for a configured document.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="configurationSectionName"></param>
		/// <returns></returns>
		public static Dictionary<string,object> GetDocumentParameters(string id, string configurationSectionName) {
			Dictionary<string,object> parameters = null;
			
			BORestConfigurationSection config = (BORestConfigurationSection)ConfigurationManager.GetSection(configurationSectionName);
			if(config != null) {
				parameters = config.GetDocumentParameters(id);
			}

			return parameters;
		}
		/// <summary>
		/// Returns a dictionary containing the parameters for a configured document.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Dictionary<string,object> GetDocumentParameters(string id) {
			Dictionary<string,object> parameters = null;

			BORestConfigurationDocumentElement doc = Documents[id];
			if(doc != null) {
				if(doc.Parameters != null) {
					parameters = new Dictionary<string,object>(doc.Parameters.Count);
					foreach(BORestConfigurationParameterElement param in doc.Parameters) {
						parameters.Add(param.Name, param.DefaultValue);
					}
				}
			}

			return parameters;
		}
		#endregion
	}
	#endregion
	
	#region BORestConfigurationDocumentElement
	/// <summary>
	/// Represents a configured document
	/// </summary>
	public class BORestConfigurationDocumentElement : ConfigurationElement
	{
		/// <summary>
		/// Document id used with <see cref="BORestAdapter.DownloadConfiguredDocument">BORestAdapter.DownloadConfiguredDocument</see> and <see cref="BORestAdapter.GetConfiguredDocumentStream">BORestAdapter.GetConfiguredDocumentStream</see>
		/// </summary>
		[ConfigurationProperty("id", IsRequired=true, IsKey=true)]
		public string ID
		{
			get
			{
				return Parser.ToString(this["id"]);
			}
			set
			{
				this["id"] = value;
			}
		}

		/// <summary>
		/// Optional. Document path separated by forward slashes. Example: "Root Folder/My Documents/Some Document". Path must be unique.
		/// </summary>
		[ConfigurationProperty("documentPath", IsRequired=false)]
		public string DocumentPath
		{
			get
			{
				return Parser.ToString(this["documentPath"]);
			}
			set
			{
				this["documentPath"] = value;
			}
		}

		/// <summary>
		/// Optional. The id of the document. Can be obtained using <see cref="BORestAdapter.GetInfostoreObjectId">BORestAdapter.GetInfostoreObjectId</see>
		/// </summary>
		[ConfigurationProperty("documentId", IsRequired=false)]
		public int DocumentID
		{
			get
			{
				return Parser.ToInt(this["documentId"]);
			}
			set
			{
				this["documentId"] = value;
			}
		}

		/// <summary>
		/// Optional. Whether or not the report should be paginated. Default value is <see cref="BORestAdapter.PaginatedDefault">BORestAdapter.PaginatedDefault</see>
		/// 
		/// </summary>
		[ConfigurationProperty("paginated", IsRequired = false, DefaultValue=BORestAdapter.PaginatedDefault)]
		public bool Paginated {
			get {
				return Parser.ToBool(this["paginated"]);
			}
			set {
				this["paginated"] = value;
			}
		}

		/// <summary>
		/// Pagination options in query string format. Default value is <see cref="BORestAdapter.PaginationOptionsDefault">BORestAdapter.PaginationOptionsDefault</see>
		/// </summary>
		[ConfigurationProperty("paginationOptions", IsRequired = false, DefaultValue=BORestAdapter.PaginationOptionsDefault)]
		public string PaginationOptions {
			get {
				return Parser.ToString(this["paginationOptions"]);
			}
			set {
				this["paginationOptions"] = value;
			}
		}
		/*
		 * NOT IN USE
		[ConfigurationProperty("documentCuid", IsRequired=false)]
		public string DocumentCuid
		{
			get
			{
				return this["documentCuid"].ToString();
			}
			set
			{
				this["documentCuid"] = value;
			}
		}
		*/

		/// <summary>
		/// Known parameters for document.
		/// </summary>
		[ConfigurationProperty("parameters")]
		public BORestConfigurationParameterElementCollection Parameters
		{
			get
			{
				if(this["parameters"] != null) {
					return (BORestConfigurationParameterElementCollection)this["parameters"];
				} else {
					return null;
				}
			}
			set
			{
				this["parameters"] = value;
			}
		}
	}
	#endregion

	#region BORestConfigurationDocumentElementCollection
	/// <summary>
	/// Represents a collection of configured documents.  Child document nodes are of type <see cref="BORestConfigurationDocumentElement">BORestConfigurationDocumentElement</see>
	/// </summary>
	[ConfigurationCollection(typeof(BORestConfigurationDocumentElement), AddItemName="document", CollectionType=ConfigurationElementCollectionType.BasicMap)]
	public class BORestConfigurationDocumentElementCollection : ConfigurationElementCollection
	{
		/// <summary>
		/// Creates a new BORestConfigurationDocumentElement object.
		/// </summary>
		/// <returns>A new BORestConfigurationDocumentElement object.</returns>
		protected override ConfigurationElement CreateNewElement()
		{
			return new BORestConfigurationDocumentElement();
		}
		/// <summary>
		/// Returns the generic key for a valid child element within this collection.  The generic key for child elements within this collection is the Code property of a BORestConfigurationDocumentElement object.
		/// </summary>
		/// <param name="Element">The element retrieve the key from.</param>
		/// <returns>The key value of the element.</returns>
		protected override object GetElementKey(ConfigurationElement Element)
		{
			return ((BORestConfigurationDocumentElement)Element).ID;
		}

		public new BORestConfigurationDocumentElement this[string id]
		{
			get
			{
				return (BORestConfigurationDocumentElement)this.BaseGet(id);
			}
		}

		public BORestConfigurationDocumentElement this[int Index]
		{
			get
			{
				return (BORestConfigurationDocumentElement)this.BaseGet(Index);
			}
		}
	}
	#endregion

	#region BORestConfigurationParameterElementCollection
	/// <summary>
	/// Collection of predefined parameters for a particular document.  This set of parameters need not be complete.  Additional parameters can be added programmatically.
	/// </summary>
	[ConfigurationCollection(typeof(BORestConfigurationParameterElement), AddItemName="parameter", CollectionType=ConfigurationElementCollectionType.BasicMap)]
	public class BORestConfigurationParameterElementCollection : ConfigurationElementCollection
	{
		/// <summary>
		/// Creates a new BORestConfigurationParameterElement object.
		/// </summary>
		/// <returns>A new BORestConfigurationParameterElement object.</returns>
		protected override ConfigurationElement CreateNewElement()
		{
			return new BORestConfigurationParameterElement();
		}
		/// <summary>
		/// Returns the generic key for a valid child element within this collection.  The generic key for child elements within this collection is the Code property of a BORestConfigurationParameterElement object.
		/// </summary>
		/// <param name="Element">The element retrieve the key from.</param>
		/// <returns>The key value of the element.</returns>
		protected override object GetElementKey(ConfigurationElement Element)
		{
			return ((BORestConfigurationParameterElement)Element).ID;
		}

		public new BORestConfigurationParameterElement this[string id]
		{
			get
			{
				return (BORestConfigurationParameterElement)this.BaseGet(id);
			}
		}

		public BORestConfigurationParameterElement this[int Index]
		{
			get
			{
				return (BORestConfigurationParameterElement)this.BaseGet(Index);
			}
		}
	}
	#endregion

	#region BORestConfigurationParameterElement
	/// <summary>
	/// Represents a particular parameter for a document
	/// </summary>
	public class BORestConfigurationParameterElement : ConfigurationElement
	{
		/// <summary>
		/// The ID of the parameter. If used in conjunction with Name, it provides an code reference to the parameter. If Name is not supplied, ID is used as the name of the parameter in the document.
		/// </summary>
		[ConfigurationProperty("id", IsRequired=true,IsKey=true)]
		public string ID
		{
			get
			{
				return Parser.ToString(this["id"]);
			}
			set
			{
				this["id"] = value;
			}
		}

		/// <summary>
		/// Optional. The name given to this parameter in the document. ID will be used if Name is not supplied.
		/// </summary>
		[ConfigurationProperty("name", IsRequired=false)]
		public string Name
		{
			get
			{
				string name = Parser.ToString(this["name"]);
				if(!string.IsNullOrEmpty(name))
				{
					return name;
				}
				else
				{
					return ID;
				}
			}
			set
			{
				this["name"] = value;
			}
		}

		/// <summary>
		/// Optional. Indicates whether the parameter supports multiple values.
		/// </summary>
		[ConfigurationProperty("multivalue", IsRequired=false)]
		public bool Multivalue
		{
			get
			{
				return Parser.ToBool(this["multivalue"]);
			}
			set
			{
				this["multivalue"] = value;
			}
		}

		/// <summary>
		/// Optional. Default value for this parameter.
		/// </summary>
		[ConfigurationProperty("defaultValue", IsRequired=false)]
		public string DefaultValue
		{
			set
			{
				this["defaultValue"] = value;
			}
			get
			{
				return Parser.ToString(this["defaultValue"]);
			}
		}
				
		public object Value
		{
			get
			{
				if(value == null)
				{
					return this["defaultValue"];
				}
				else
				{
					return value;
				}
			}
			set
			{
				this.value = value;
			}
		}

		private object value = null;

		public object[] Values = null;
	}
	#endregion

	#region BORestConfigurationValidator
	/// <summary>
	/// Performs validation of configured documents.
	/// <list type="bullet">
	/// <item><description>Enforces that documents have either documentPath or documentId is required for documents. If supplied, documentId must be greater than 0.</description></item>
	/// <item><description>Enforces that documents with a provided documentPath attribute must be separated by / (forward slashes) not \\ (back slashes).</description></item>
	/// </list>
	/// </summary>
	public class BORestConfigurationValidator : ConfigurationValidatorBase {
		public override bool CanValidate(Type type) {
			if(type == typeof(BORestConfigurationDocumentElementCollection)) {
				return true;
			} else {
				return false;
			}
		}

		public override void Validate(object value) {
			if(value.GetType() == typeof(BORestConfigurationDocumentElementCollection)) {
				BORestConfigurationDocumentElementCollection documentElements = (BORestConfigurationDocumentElementCollection)value;
				foreach(BORestConfigurationDocumentElement documentElement in documentElements) {
					if(string.IsNullOrEmpty(documentElement.DocumentPath) && documentElement.DocumentID <= 0) {
						throw new ConfigurationErrorsException("Either documentPath or documentId is required for documents. If supplied, documentId must be greater than 0.");
					} else if(!string.IsNullOrEmpty(documentElement.DocumentPath)) {
						if(documentElement.DocumentPath.IndexOf("\\") >= 0 && documentElement.DocumentPath.IndexOf("/") < 0) {
							throw new ConfigurationErrorsException("Items in the documentPath attribute must be separated by / (forward slashes) not \\ (back slashes)");
						}
					}
				}
			}
		}
	}
	#endregion
}