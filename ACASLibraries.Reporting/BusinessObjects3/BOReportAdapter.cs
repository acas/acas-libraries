using System;
using System.Web;
using System.Configuration;
using BusinessObjects.DSWS;
using BusinessObjects.DSWS.BIPlatform;
using BusinessObjects.DSWS.BIPlatform.Constants;
using BusinessObjects.DSWS.BIPlatform.Desktop;
using BusinessObjects.DSWS.ReportEngine;
using BusinessObjects.DSWS.Session;
using ACASLibraries;

namespace ACASLibraries.Reporting.BusinessObjects3
{
	#region BOReportFormat
	public enum BOReportFormat
	{
		Excel,
		Html,
		Pdf,
		Rtf,
		Word,
		Xml
	}
	#endregion

	#region BOReportPrompt
	public class BOReportPrompt : ConfigurationElement
	{
		[ConfigurationProperty("Code", IsRequired=true,IsKey=true)]
		public string Code
		{
			get
			{
				return this["Code"].ToString();
			}
			set
			{
				this["Code"] = value;
			}
		}
		[ConfigurationProperty("ID", IsRequired=false)]
		public string ID
		{
			get
			{
				if(this["ID"] != null)
				{
					return this["ID"].ToString();
				}
				else
				{
					return null;
				}
			}
			set
			{
				this["ID"] = value;
			}
		}
		public object Value
		{
			get
			{
				if(m_oValue == null)
				{
					return this["DefaultValue"].ToString();
				}
				else
				{
					return m_oValue;
				}
			}
			set
			{
				m_oValue = value;
			}
		}
		[ConfigurationProperty("DefaultValue", IsRequired=false)]
		public string DefaultValue
		{
			set
			{
				m_sDefaultValue = this["DefaultValue"].ToString();
				if(m_oValue == null)
				{
					m_oValue = this["DefaultValue"];
				}
			}
			get
			{
				return m_sDefaultValue;
			}
		}

		[ConfigurationProperty("Name", IsRequired=false)]
		public string Name
		{
			get
			{
				if(this["Name"] != null)
				{
					return this["Name"].ToString();
				}
				else
				{
					return null;
				}
			}
			set
			{
				this["Name"] = value;
			}
		}

		public BOReportPromptType Type = BOReportPromptType.Unknown;
		public string Description = null;
		private object m_oValue = null;
		private string m_sDefaultValue = null;

		public object[] Values = null;
	}
	#endregion

	#region BOReportPromptType
	public enum BOReportPromptType
	{
		Boolean,
		Currency,
		Date,
		Datetime,
		Numeric,
		Text,
		Time,
		Unknown
	}
	#endregion

	#region BOReportPromptCollection
	public class BOReportPromptCollection : System.Collections.Generic.List<BOReportPrompt>
	{
		public void AddWithValue(string ID, object Value)
		{
			BOReportPrompt oPrompt = new BOReportPrompt();
			oPrompt.ID = ID;
			oPrompt.Value = Value;
			this.Add(oPrompt);
		}

		public void AddWithValues(string ID, object[] Values)
		{
			BOReportPrompt oPrompt = new BOReportPrompt();
			oPrompt.ID = ID;
			oPrompt.Values = Values;
			this.Add(oPrompt);
		}
	}
	#endregion

	#region BOAuthenticationType
	public enum BOAuthenticationType
	{
		Enterprise,
		ActiveDirectory
	}
	#endregion

	#region BOReportAdapter
	public class BOReportAdapter
	{
		private global::BusinessObjects.DSWS.Connection m_oSessionConnection = null; //non-ws
		private global::BusinessObjects.DSWS.Connection m_oReportEngineConnection = null; //non-ws
		private Session m_oSession = null;
		private SessionInfo m_oSessionInfo = null;
		private BIPlatform m_oBIPlatform = null;

		public string Username = null;
		public string Password = null;
		public string SessionUrl = null;
		public string ReportEngineUrl = null;
		public int Timeout = 300;
		public BOAuthenticationType AuthenticationType = BOAuthenticationType.Enterprise;

		#region BIPlatform
		public BIPlatform BIPlatform
		{
			get
			{
				if(m_oBIPlatform == null && m_oSession != null)
				{
					m_oBIPlatform = (BIPlatform)m_oSession.GetConsumer("BIPlatform", m_oSession.GetAssociatedServicesURL("BIPlatform")[0]);
				}
				return m_oBIPlatform;
			}
		}
		#endregion

		#region constructor
		public BOReportAdapter(){}
		public BOReportAdapter(string ConfigurationSectionName)
		{
			BOReportAdapterConfigurationSection oSection = (BOReportAdapterConfigurationSection)ConfigurationManager.GetSection(ConfigurationSectionName);
			Username = oSection.Username;
			Password = oSection.Password;
			SessionUrl = oSection.SessionUrl;
			ReportEngineUrl = oSection.ReportEngineUrl;
			if(oSection.Timeout > 0)
			{
				Timeout = oSection.Timeout;
			}
			AuthenticationType = oSection.AuthenticationType;
			oSection = null;
		}
		#endregion

		#region Login();
		public void Login()
		{
			EnterpriseCredential oEC = new EnterpriseCredential();
			oEC.Login = Username;
			oEC.Password = Password;
			switch(AuthenticationType)
			{
				case BOAuthenticationType.ActiveDirectory:
					oEC.AuthType = "secAD";
					break;
				default:
					oEC.AuthType = "secEnterprise";
					break;
			}

			if(HttpContext.Current.Trace.IsEnabled)
			{
				HttpContext.Current.Trace.Write("BOReportAdapter.SessionUrl", SessionUrl);
				HttpContext.Current.Trace.Write("BOReportAdapter.ReportEngineUrl", ReportEngineUrl);
				HttpContext.Current.Trace.Write("BOReportAdapter.Username", Username);
				HttpContext.Current.Trace.Write("BOReportAdapter.Timeout", Timeout.ToString());
				HttpContext.Current.Trace.Write("BOReportAdapter.AuthenticationType", BOReportAdapter.ParseAuthenticationType(AuthenticationType));
			}

			m_oSessionConnection = new global::BusinessObjects.DSWS.Connection(SessionUrl);
			m_oSessionConnection.TimeOut = Timeout*1000;
			m_oSession = new Session(m_oSessionConnection);
			m_oSessionInfo = m_oSession.Login(oEC);

			m_oReportEngineConnection = new global::BusinessObjects.DSWS.Connection(ReportEngineUrl);

			oEC = null;

			if(System.Web.HttpContext.Current.Trace.IsEnabled)
			{
				System.Web.HttpContext.Current.Trace.Write("BOReportAdapter.Login()", m_oSessionInfo.SessionID);
			}
		}
		#endregion

		#region GetReportExcel();
		public byte[] GetReportExcel(string DocumentReferenceEntry)
		{
			return (byte[])GetReport(DocumentReferenceEntry, null, BOReportFormat.Excel);
		}
		public byte[] GetReportExcel(string DocumentReferenceEntry, BOReportPromptCollection Prompts)
		{
			return (byte[])GetReport(DocumentReferenceEntry, Prompts, BOReportFormat.Excel);
		}
		#endregion

		#region GetReportHtml();
		public string GetReportHtml(string DocumentReferenceEntry)
		{
			return (string)GetReport(DocumentReferenceEntry, null, BOReportFormat.Html);
		}
		public string GetReportHtml(string DocumentReferenceEntry, BOReportPromptCollection Prompts)
		{
			return (string)GetReport(DocumentReferenceEntry, Prompts, BOReportFormat.Html);
		}
		#endregion

		#region GetReportPdf();
		public byte[] GetReportPdf(string DocumentReferenceEntry)
		{
			return (byte[])GetReport(DocumentReferenceEntry, null, BOReportFormat.Pdf);
		}
		public byte[] GetReportPdf(string DocumentReferenceEntry, BOReportPromptCollection Prompts)
		{
			return (byte[])GetReport(DocumentReferenceEntry, Prompts, BOReportFormat.Pdf);
		}
		#endregion

		#region GetReportRtf();
		public byte[] GetReportRtf(string DocumentReferenceEntry)
		{
			return (byte[])GetReport(DocumentReferenceEntry, null, BOReportFormat.Rtf);
		}
		public byte[] GetReportRtf(string DocumentReferenceEntry, BOReportPromptCollection Prompts)
		{
			return (byte[])GetReport(DocumentReferenceEntry, Prompts, BOReportFormat.Rtf);
		}
		#endregion

		#region GetReportWord();
		public byte[] GetReportWord(string DocumentReferenceEntry)
		{
			return (byte[])GetReport(DocumentReferenceEntry, null, BOReportFormat.Word);
		}
		public byte[] GetReportWord(string DocumentReferenceEntry, BOReportPromptCollection Prompts)
		{
			return (byte[])GetReport(DocumentReferenceEntry, Prompts, BOReportFormat.Word);
		}
		#endregion

		#region GetReportXml();
		public System.Xml.XmlDocument GetReportXml(string DocumentReferenceEntry)
		{
			return (System.Xml.XmlDocument)GetReport(DocumentReferenceEntry, null, BOReportFormat.Xml);
		}
		public System.Xml.XmlDocument GetReportXml(string DocumentReferenceEntry, BOReportPromptCollection Prompts)
		{
			return (System.Xml.XmlDocument)GetReport(DocumentReferenceEntry, Prompts, BOReportFormat.Xml);
		}
		#endregion

		#region GetReport(); - private
		private object GetReport(string DocumentReferenceEntry, BOReportPromptCollection Prompts, BOReportFormat ReportFormat)
		{
			ReportEngine oEngine = new ReportEngine(m_oReportEngineConnection, m_oSession.ConnectionState);
			global::BusinessObjects.DSWS.ReportEngine.Action[] oActions = null; //new Action[0];
			Navigate oNavigate = new NavigateToFirstPage();
			RetrieveData oRetrieveData = new RetrieveData();

			#region set report format
			oRetrieveData.RetrieveCurrentReportState = new RetrieveCurrentReportState();
			RetrieveView oRetrieveView;
			if(ReportFormat == BOReportFormat.Excel || ReportFormat == BOReportFormat.Pdf || ReportFormat == BOReportFormat.Rtf)
			{
				//binary
				oRetrieveView = new RetrieveBinaryView();
			}
			else if(ReportFormat == BOReportFormat.Xml)
			{
				//xml
				oRetrieveView = new RetrieveXMLView();
			}
			else
			{
				//character
				oRetrieveView = new RetrieveCharacterView();
			}

			oRetrieveView.ViewSupport = new ViewSupport();
			switch(ReportFormat)
			{
				case BOReportFormat.Excel:
					oRetrieveView.ViewSupport.OutputFormat = OutputFormatType.EXCEL;
					oRetrieveView.ViewSupport.ViewMode = ViewModeType.DOCUMENT;
					oRetrieveView.ViewSupport.ViewType = ViewType.BINARY;
					break;
				case BOReportFormat.Html:
					oRetrieveView.ViewSupport.OutputFormat = OutputFormatType.HTML;
					oRetrieveView.ViewSupport.ViewMode = ViewModeType.REPORT;
					oRetrieveView.ViewSupport.ViewType = ViewType.CHARACTER;
					break;
				case BOReportFormat.Pdf:
					oRetrieveView.ViewSupport.OutputFormat = OutputFormatType.PDF;
					oRetrieveView.ViewSupport.ViewMode = ViewModeType.DOCUMENT;
					oRetrieveView.ViewSupport.ViewType = ViewType.BINARY;
					break;
				case BOReportFormat.Rtf:
					oRetrieveView.ViewSupport.OutputFormat = OutputFormatType.RTF;
					oRetrieveView.ViewSupport.ViewMode = ViewModeType.DOCUMENT;
					oRetrieveView.ViewSupport.ViewType = ViewType.BINARY;
					break;
				case BOReportFormat.Word:
					oRetrieveView.ViewSupport.OutputFormat = OutputFormatType.WORD;
					oRetrieveView.ViewSupport.ViewMode = ViewModeType.REPORT;
					oRetrieveView.ViewSupport.ViewType = ViewType.CHARACTER;
					break;
				case BOReportFormat.Xml:
					oRetrieveView.ViewSupport.OutputFormat = OutputFormatType.XML;
					oRetrieveView.ViewSupport.ViewMode = ViewModeType.DOCUMENT;
					oRetrieveView.ViewSupport.ViewType = ViewType.XML;
					break;
			}
			oRetrieveData.RetrieveView = oRetrieveView;
			#endregion

			#region set prompts
			if(Prompts != null)
			{
				string[] saPrompts = new string[Prompts.Count];

				oRetrieveData.RetrieveNavigationMap = new RetrieveNavigationMap();
				oRetrieveData.RetrieveNavigationMap.Depth = 0;
				oRetrieveData.RetrieveReportList = new RetrieveReportList();
				FillPrompts oFillPrompts = new FillPrompts();
				oFillPrompts.FillPromptList = new FillPrompt[Prompts.Count];
				BOReportPromptCollection oReportPrompts = null;
				for(int x=0;x<Prompts.Count;x++)
				{
					string sID = Prompts[x].ID;
					if(sID == null && Prompts[x].Name != null)
					{
						//load report prompts to determine ID from Name
						if(oReportPrompts == null)
						{
							oReportPrompts = GetReportPrompts(DocumentReferenceEntry, ref oEngine);
						}
						if(oReportPrompts != null)
						{
							foreach(BOReportPrompt oPrompt in oReportPrompts)
							{
								if(oPrompt.Name == Prompts[x].Name)
								{
									sID = oPrompt.ID;
									break;
								}
							}
						}
					}
					if(sID != null)
					{
						saPrompts[x] = sID;
						FillPrompt oFillPrompt = new FillPrompt();
						oFillPrompt.ID = sID;
						if(Prompts[x].Values != null && Prompts[x].Values.Length > 0)
						{
							//object array of values
							oFillPrompt.Values = new DiscretePromptValue[Prompts[x].Values.Length];
							for(int y=0;y<Prompts[x].Values.Length;y++)
							{
								oFillPrompt.Values[y] = new DiscretePromptValue();
								((DiscretePromptValue)oFillPrompt.Values[y]).Value = Prompts[x].Values[y].ToString();
							}
						}
						else if(Prompts[x].Value != null)
						{
							//single value
							oFillPrompt.Values = new DiscretePromptValue[1];
							oFillPrompt.Values[0] = new DiscretePromptValue();
							((DiscretePromptValue)oFillPrompt.Values[0]).Value = Prompts[x].Value.ToString();
						}
						if(oFillPrompt.Values != null)
						{
							oFillPrompts.FillPromptList[x] = oFillPrompt;
						}
					}
				}
				oReportPrompts = null;

				oActions = new global::BusinessObjects.DSWS.ReportEngine.Action[2];
				oActions[0] = new Refresh();
				oActions[1] = oFillPrompts;
			}
			#endregion

			#region run report
			DocumentInformation oDoc = oEngine.GetDocumentInformation(DocumentReferenceEntry, null, oActions, null, oRetrieveData); //oRetrieveMustFillInfo, oNavigate

			object oOutput = null;
			if(oDoc != null && oDoc.View != null)
			{
				if(oRetrieveView.GetType() == typeof(RetrieveCharacterView))
				{
					oOutput = ((CharacterView)oDoc.View).Content;
				}
				else if(oRetrieveView.GetType() == typeof(RetrieveXMLView))
				{
					oOutput = new System.Xml.XmlDocument();
					((System.Xml.XmlDocument)oOutput).LoadXml((((XMLView)oDoc.View).Content).ToString());
				}
				else //if(oRetrieveView.GetType() == typeof(RetrieveBinaryView))
				{
					oOutput = ((BinaryView)oDoc.View).Content;
				}
			}
			#endregion

			oDoc = null;
			oRetrieveView = null;
			oRetrieveData = null;
			oNavigate = null;
			oActions = null;

			return oOutput;
		}
		#endregion

		#region GetReportPrompts();
		public BOReportPromptCollection GetReportPrompts(string DocumentReferenceEntry)
		{
			ReportEngine oEngine = new ReportEngine(m_oReportEngineConnection, m_oSession.ConnectionState);

			BOReportPromptCollection oPrompts = GetReportPrompts(DocumentReferenceEntry, ref oEngine);

			oEngine = null;

			return oPrompts;
		}
		public BOReportPromptCollection GetReportPrompts(string DocumentReferenceEntry, ref ReportEngine ReportEngine)
		{
			RetrieveMustFillInfo oRetrieveMustFillInfo = new RetrieveMustFillInfo();
			oRetrieveMustFillInfo.RetrievePromptsInfo = new RetrievePromptsInfo();
			oRetrieveMustFillInfo.RetrievePromptsInfo.PromptLOVRetrievalMode = PromptLOVRetrievalMode.NONE;

			DocumentInformation oDoc = ReportEngine.GetDocumentInformation(DocumentReferenceEntry, oRetrieveMustFillInfo, null, null, null);

			BOReportPromptCollection oPrompts = ConvertPrompts(oDoc.PromptInfo);

			oDoc = null;

			return oPrompts;
		}
		#endregion

		#region ConvertPrompts();
		private BOReportPromptCollection ConvertPrompts(PromptInfo[] Prompts)
		{
			BOReportPromptCollection oPrompts = new BOReportPromptCollection();

			for(int x=0;x<Prompts.Length;x++)
			{
				BOReportPrompt oPrompt = new BOReportPrompt();
				oPrompt.ID = Prompts[x].ID;
				oPrompt.Name = Prompts[x].Name;
				switch(Prompts[x].PromptType)
				{
					case PromptType.BOOLEAN:
						oPrompt.Type = BOReportPromptType.Boolean;
						break;
					case PromptType.CURRENCY:
						oPrompt.Type = BOReportPromptType.Currency;
						break;
					case PromptType.DATE:
						oPrompt.Type = BOReportPromptType.Date;
						break;
					case PromptType.DATETIME:
						oPrompt.Type = BOReportPromptType.Datetime;
						break;
					case PromptType.NUMERIC:
						oPrompt.Type = BOReportPromptType.Numeric;
						break;
					case PromptType.TEXT:
						oPrompt.Type = BOReportPromptType.Text;
						break;
					case PromptType.TIME:
						oPrompt.Type = BOReportPromptType.Time;
						break;
				}
				oPrompt.Description = Prompts[x].Description;
				oPrompts.Add(oPrompt);
			}

			return oPrompts;
		}
		#endregion

		#region Logout();
		public void Logout()
		{
			m_oSession.Logout();
			m_oSessionInfo = null;
			m_oSession = null;
			m_oSessionConnection = null;
		}
		#endregion

		#region GetMimeType();
		public static string GetMimeType(BOReportFormat Format)
		{
			switch(Format)
			{
				case BOReportFormat.Excel:
					return "application/vnd.ms-excel";
				case BOReportFormat.Html:
					return "text/html";
				case BOReportFormat.Pdf:
					return "application/pdf";
				case BOReportFormat.Rtf:
					return "application/rtf";
				case BOReportFormat.Word:
					return "application/msword";
				case BOReportFormat.Xml:
					return "text/xml";
				default:
					return "plain/text";
			}
		}
		#endregion

		#region ParseAuthenticationType();
		public static BOAuthenticationType ParseAuthenticationType(string AuthenticationType)
		{
			switch(AuthenticationType.ToLower())
			{
				case "activedirectory":
					return BOAuthenticationType.ActiveDirectory;
				default:
					return BOAuthenticationType.Enterprise;
			}
		}
		public static string ParseAuthenticationType(BOAuthenticationType AuthenticationType)
		{
			switch(AuthenticationType)
			{
				case BOAuthenticationType.ActiveDirectory:
					return "ActiveDirectory";
				default:
					return "Enterprise";
			}
		}
		#endregion

		#region GetFolder();
		public InfoObject[] GetFolder()
		{
			return GetFolder(null);
		}
		public InfoObject[] GetFolder(string FolderReferenceEntry)
		{
			string sPath = (FolderReferenceEntry!=null&&FolderReferenceEntry.Length>0?String.Concat("cuid://<",FolderReferenceEntry,">/*[NOT SI_ID=49]@*?OrderBy=SI_NAME"):"path://InfoObjects/@*[NOT SI_ID=49]?OrderBy=SI_NAME");
			if(HttpContext.Current.Trace.IsEnabled)
			{
				HttpContext.Current.Trace.Write("BOReportAdapter.GetFolder()", sPath);
			}
			GetOptions oOptions = new GetOptions();
			oOptions.PageSize = 500;
			oOptions.PageSizeSpecified = true;
			ResponseHolder oResponseHolder = BIPlatform.Get(sPath, oOptions);
			InfoObject[] oInfoObjects = oResponseHolder.InfoObjects.InfoObject;
			oResponseHolder = null;
			oOptions = null;
			return oInfoObjects;
		}
		#endregion
	}
	#endregion

	#region BOReportAdapterConfigurationSection
	public class BOReportAdapterConfigurationSection : ConfigurationSection
	{
		[ConfigurationProperty("Reports")]
		public BOReportConfigurationElementCollection Reports
		{
			get
			{
				return (BOReportConfigurationElementCollection)this["Reports"];
			}
			set
			{
				this["Reports"] = value;
			}
		}

		[ConfigurationProperty("Username")]
		public string Username
		{
			get
			{
				return (string)this["Username"];
			}
			set
			{
				this["Username"] = value;
			}
		}
		[ConfigurationProperty("Password")]
		public string Password
		{
			get
			{
				return (string)this["Password"];
			}
			set
			{
				this["Password"] = value;
			}
		}
		[ConfigurationProperty("SessionUrl")]
		public string SessionUrl
		{
			get
			{
				return (string)this["SessionUrl"];
			}
			set
			{
				this["SessionUrl"] = value;
			}
		}
		[ConfigurationProperty("ReportEngineUrl")]
		public string ReportEngineUrl
		{
			get
			{
				return (string)this["ReportEngineUrl"];
			}
			set
			{
				this["ReportEngineUrl"] = value;
			}
		}
		[ConfigurationProperty("Timeout")]
		public int Timeout
		{
			get
			{
				return int.Parse(this["Timeout"].ToString());
			}
			set
			{
				this["Timeout"] = value;
			}
		}
		[ConfigurationProperty("AuthenticationType")]
		public BOAuthenticationType AuthenticationType
		{
			get
			{
				return BOReportAdapter.ParseAuthenticationType(this["AuthenticationType"].ToString());
			}
			set
			{
				this["AuthenticationType"] = BOReportAdapter.ParseAuthenticationType(value);
			}
		}

		#region GetCurrentValue();
		public static object GetCurrentValue(string PropertyName, string SectionName)
		{
			return ((BOReportAdapterConfigurationSection)ConfigurationManager.GetSection(SectionName)).Properties[PropertyName];
		}
		#endregion
	}
	#endregion

	#region BOReportConfigurationElement
	public class BOReportConfigurationElement : ConfigurationElement
	{
		[ConfigurationProperty("Code", IsRequired=true, IsKey=true)]
		public string Code
		{
			get
			{
				return this["Code"].ToString();
			}
			set
			{
				this["Code"] = value;
			}
		}

		[ConfigurationProperty("DocumentReferenceEntry", IsRequired=false)]
		public string DocumentReferenceEntry
		{
			get
			{
				return this["DocumentReferenceEntry"].ToString();
			}
			set
			{
				this["DocumentReferenceEntry"] = value;
			}
		}

		[ConfigurationProperty("DocumentLocation", IsRequired=false)]
		public string DocumentLocation
		{
			get
			{
				return this["DocumentLocation"].ToString();
			}
			set
			{
				this["DocumentLocation"] = value;
			}
		}

		[ConfigurationProperty("DocumentName", IsRequired=false)]
		public string DocumentName
		{
			get
			{
				return this["DocumentName"].ToString();
			}
			set
			{
				this["DocumentName"] = value;
			}
		}

		[ConfigurationProperty("Prompts")]
		public BOReportPromptConfigurationElementCollection Prompts
		{
			get
			{
				return (BOReportPromptConfigurationElementCollection)this["Prompts"];
			}
			set
			{
				this["Prompts"] = value;
			}
		}

		#region GetDocumentReferenceEntry();
		public static string GetDocumentReferenceEntry(string ReportCode, string SectionName)
		{
			if(((BOReportAdapterConfigurationSection)ConfigurationManager.GetSection(SectionName)).Reports[ReportCode].DocumentReferenceEntry != null && ((BOReportAdapterConfigurationSection)ConfigurationManager.GetSection(SectionName)).Reports[ReportCode].DocumentReferenceEntry.Length > 0)
			{
				return ((BOReportAdapterConfigurationSection)ConfigurationManager.GetSection(SectionName)).Reports[ReportCode].DocumentReferenceEntry;
			}
			else
			{
				BOReportAdapter oBOReportAdapter = new BOReportAdapter(SectionName);
				oBOReportAdapter.Login();
				string sDocumentReferenceEntry = GetDocumentReferenceEntry(ReportCode, SectionName, ref oBOReportAdapter);
				oBOReportAdapter.Logout();
				oBOReportAdapter = null;

				return sDocumentReferenceEntry;
			}
		}
		public static string GetDocumentReferenceEntry(string ReportCode, string SectionName, ref BOReportAdapter BOReportAdapter)
		{
			if(((BOReportAdapterConfigurationSection)ConfigurationManager.GetSection(SectionName)).Reports[ReportCode].DocumentReferenceEntry != null && ((BOReportAdapterConfigurationSection)ConfigurationManager.GetSection(SectionName)).Reports[ReportCode].DocumentReferenceEntry.Length > 0)
			{
				return ((BOReportAdapterConfigurationSection)ConfigurationManager.GetSection(SectionName)).Reports[ReportCode].DocumentReferenceEntry;
			}
			else
			{
				string sDocumentName = ((BOReportAdapterConfigurationSection)ConfigurationManager.GetSection(SectionName)).Reports[ReportCode].DocumentName;
				string sDocumentReferenceEntry = null;
				if(sDocumentName != null && sDocumentName.Length > 0)
				{
					string[] saFolders = new string[0];
					string sDocumentLocation = ((BOReportAdapterConfigurationSection)ConfigurationManager.GetSection(SectionName)).Reports[ReportCode].DocumentLocation;
					if(sDocumentLocation != null && sDocumentLocation.Length > 0)
					{
						if(sDocumentLocation.StartsWith("/") || sDocumentLocation.StartsWith(@"\"))
						{
							//ignore initial slash
							sDocumentLocation = sDocumentLocation.Substring(1);
						}
						if(sDocumentLocation.EndsWith("/") || sDocumentLocation.EndsWith(@"\"))
						{
							//ignore trailing slash
							sDocumentLocation = sDocumentLocation.Substring(0, sDocumentLocation.Length-1);
						}
						if(sDocumentLocation.IndexOf("/") > 0)
						{
							//parse using / (forward slash)
							saFolders = sDocumentLocation.Split("/".ToCharArray());
						}
						else if(sDocumentLocation.IndexOf(@"\") > 0)
						{
							//parse using \ (back slash)
							saFolders = sDocumentLocation.Split(@"\".ToCharArray());
						}
						else
						{
							//no slash
							saFolders = new string[1];
							saFolders[0] = sDocumentLocation;
						}
					}

					string sFolderCUID = null;
					InfoObject[] oInfoObjects = null;
					//traverse folders to get to report
					for(int x=0;x<saFolders.Length;x++)
					{
						oInfoObjects = BOReportAdapter.GetFolder(sFolderCUID);
						for(int z=0;z<oInfoObjects.Length;z++)
						{
							if(oInfoObjects[z].Kind.Equals("Folder"))
							{
								if(HttpContext.Current.Trace.IsEnabled)
								{
									HttpContext.Current.Trace.Write("BOReportConfigurationElement.GetDocumentReferenceEntry()", oInfoObjects[z].Name+" = "+oInfoObjects[z].CUID);
								}
								if(oInfoObjects[z].Name == saFolders[x])
								{
									sFolderCUID = oInfoObjects[z].CUID;
									break;
								}
							}
						}
					}
					//find report in folder
					oInfoObjects = BOReportAdapter.GetFolder(sFolderCUID);
					for(int z=0;z<oInfoObjects.Length;z++)
					{
						if(!oInfoObjects[z].Kind.Equals("Folder") && sDocumentName == oInfoObjects[z].Name)
						{
							if(HttpContext.Current.Trace.IsEnabled)
							{
								HttpContext.Current.Trace.Write("BOReportConfigurationElement.GetDocumentReferenceEntry()", oInfoObjects[z].Name+" = "+oInfoObjects[z].CUID);
							}
							sDocumentReferenceEntry = oInfoObjects[z].CUID;
							break;
						}
					}
					oInfoObjects = null;
				}

				return sDocumentReferenceEntry;
			}
		}
		#endregion

		#region GetPromptsCollection();
		public static BOReportPromptCollection GetPromptsCollection(string ReportCode, string SectionName)
		{
			BOReportPromptConfigurationElementCollection oPromptElements = ((BOReportAdapterConfigurationSection)ConfigurationManager.GetSection(SectionName)).Reports[ReportCode].Prompts;
			BOReportPromptCollection oPrompts = new BOReportPromptCollection();
			if(oPromptElements != null)
			{
				for(int x=0;x<oPromptElements.Count;x++)
				{
					oPrompts.Add(oPromptElements[x]);
				}
			}
			return oPrompts;
		}
		#endregion

		#region GetPromptID();
		public static string GetPromptID(string PromptName, string ReportCode, string SectionName)
		{
			BOReportAdapter oBOReportAdapter = new BOReportAdapter(SectionName);
			oBOReportAdapter.Login();
			string sDocumentReferenceEntry = GetDocumentReferenceEntry(ReportCode, SectionName);
			string sPromptID = GetPromptID(PromptName, sDocumentReferenceEntry, ref oBOReportAdapter);
			oBOReportAdapter.Logout();
			oBOReportAdapter = null;

			return sPromptID;
		}
		public static string GetPromptID(string PromptName, string sDocumentReferenceEntry, ref BOReportAdapter BOReportAdapter)
		{
			string sID = null;
			BOReportPromptCollection oReportPrompts = BOReportAdapter.GetReportPrompts(sDocumentReferenceEntry);
			if(oReportPrompts != null)
			{
				foreach(BOReportPrompt oPrompt in oReportPrompts)
				{
					if(oPrompt.Name == PromptName)
					{
						sID = oPrompt.ID;
						break;
					}
				}
			}
			return sID;
		}
		#endregion
	}
	#endregion

	#region BOReportConfigurationElementCollection
	[ConfigurationCollection(typeof(BOReportConfigurationElement), AddItemName="Report", CollectionType=ConfigurationElementCollectionType.BasicMap)]
	public class BOReportConfigurationElementCollection : ConfigurationElementCollection
	{
		/// <summary>
		/// Creates a new BOReportConfigurationElement object.
		/// </summary>
		/// <returns>A new BOReportConfigurationElement object.</returns>
		protected override ConfigurationElement CreateNewElement()
		{
			return new BOReportConfigurationElement();
		}
		/// <summary>
		/// Returns the generic key for a valid child element within this collection.  The generic key for child elements within this collection is the Code property of a BOReportConfigurationElement object.
		/// </summary>
		/// <param name="Element">The element retrieve the key from.</param>
		/// <returns>The key value of the element.</returns>
		protected override object GetElementKey(ConfigurationElement Element)
		{
			return ((BOReportConfigurationElement)Element).Code;
		}

		public new BOReportConfigurationElement this[string Code]
		{
			get
			{
				return (BOReportConfigurationElement)this.BaseGet(Code);
			}
		}

		public BOReportConfigurationElement this[int Index]
		{
			get
			{
				return (BOReportConfigurationElement)this.BaseGet(Index);
			}
		}
	}
	#endregion

	#region BOReportPromptConfigurationElementCollection
	[ConfigurationCollection(typeof(BOReportPrompt), AddItemName="Prompt", CollectionType=ConfigurationElementCollectionType.BasicMap)]
	public class BOReportPromptConfigurationElementCollection : ConfigurationElementCollection
	{
		/// <summary>
		/// Creates a new BOReportPrompt object.
		/// </summary>
		/// <returns>A new BOReportPrompt object.</returns>
		protected override ConfigurationElement CreateNewElement()
		{
			return new BOReportPrompt();
		}
		/// <summary>
		/// Returns the generic key for a valid child element within this collection.  The generic key for child elements within this collection is the Code property of a BOReportPrompt object.
		/// </summary>
		/// <param name="Element">The element retrieve the key from.</param>
		/// <returns>The key value of the element.</returns>
		protected override object GetElementKey(ConfigurationElement Element)
		{
			return ((BOReportPrompt)Element).Code;
		}

		public new BOReportPrompt this[string Code]
		{
			get
			{
				return (BOReportPrompt)this.BaseGet(Code);
			}
		}

		public BOReportPrompt this[int Index]
		{
			get
			{
				return (BOReportPrompt)this.BaseGet(Index);
			}
		}
	}
	#endregion
}
