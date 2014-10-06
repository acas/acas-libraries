using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Xml;

namespace ACASLibraries.Configuration
{
	#region XmlDocumentConfigurationSectionHandler
	/// <summary>
	/// The class allows for complete XML documents to be embedded as a configuration section in the app.config or web.config file for a project.
	/// <para>
	/// When using this class in a configuration file, the section must be defined under &lt;configuration&gt;&lt;configSections&gt; allowing a node to be added with xml
	/// content within the configuration body.  The Xml document must be valid Xml and have a root element.  The following shows an example implementation:
	/// </para>
	///	<example>
	///		&lt;configuration&gt;
	///			&lt;configSections&gt;
	///				&lt;section name=&quot;MyXmlDocument&quot; type=&quot;ACASLibraries.Configuration.XmlDocumentConfigurationSectionHandler,ACASLibraries&quot; /&gt;
	///			&lt;/configSections&gt;
	///			&lt;MyXmlDocument&gt;
	///				&lt;XmlDataTable&gt;
	///					&lt;Row&gt;
	///						&lt;Cell&gt;A1&lt;/Cell&gt;
	///						&lt;Cell&gt;A2&lt;/Cell&gt;
	///						&lt;Cell&gt;A3&lt;/Cell&gt;
	///					&lt;/Row&gt;
	///					&lt;Row&gt;
	///						&lt;Cell&gt;B1&lt;/Cell&gt;
	///						&lt;Cell&gt;B2&lt;/Cell&gt;
	///						&lt;Cell&gt;B3&lt;/Cell&gt;
	///					&lt;/Row&gt;
	///					&lt;Row&gt;
	///						&lt;Cell&gt;C1&lt;/Cell&gt;
	///						&lt;Cell&gt;C2&lt;/Cell&gt;
	///						&lt;Cell&gt;C3&lt;/Cell&gt;
	///					&lt;/Row&gt;
	///				&lt;/XmlDataTable&gt;
	///			&lt;/MyXmlDocument&gt;
	///		&lt;/configuration&gt;
	/// </example>
	/// <para>
	/// From code, this embedded XmlDocument can be easily accessed using the following method...
	/// </para>
	/// <para>
	/// System.Xml.XmlDocument oXmlDataTable = (System.Xml.XmlDocument)System.Configuration.ConfigurationManager.GetSection(&quot;MyXmlDocument&quot;);
	/// </para>
	/// <para>Note: This class implements the .NET 1.0 and 1.1 System.Configuration.IConfigurationSectionHandler interface which is depreciated in .NET 2.0 and .NET 3.0.  Use <see cref="ACASLibraries.Configuration.XmlDocumentConfigurationSection">XmlDocumentConfigurationSection</see> with .NET 2.0 and 3.0 for forward compatibility.</para>
	/// </summary>
	/// <remarks>
	/// HISTORY:
	/// <para>6/28/2006 - JZM - Imported into project.</para>
	/// </remarks>
	public class XmlDocumentConfigurationSectionHandler : System.Configuration.IConfigurationSectionHandler
	{
		object IConfigurationSectionHandler.Create(object parent, object configContext, XmlNode section)
		{
			XmlDocument oXmlDocument = null;
			if(section != null)
			{
				oXmlDocument = new XmlDocument();
				oXmlDocument.LoadXml(section.InnerXml);
			}
			return oXmlDocument;
		}
	}
	#endregion

	#region XmlDocumentConfigurationSection
	/// <summary>
	/// The class allows for complete XML documents to be embedded as a configuration section in the app.config or web.config file for a project.
	/// <para>
	/// When using this class in a configuration file, the section must be defined under &lt;configuration&gt;&lt;configSections&gt; allowing a node to be added with xml
	/// content within the configuration body.  The Xml document must be valid Xml and have a root element.  The following shows an example implementation:
	/// </para>
	///	<example>
	///		&lt;configuration&gt;
	///			&lt;configSections&gt;
	///				&lt;section name=&quot;MyXmlDocument&quot; type=&quot;ACASLibraries.Configuration.XmlDocumentConfigurationSection,ACASLibraries&quot; /&gt;
	///			&lt;/configSections&gt;
	///			&lt;MyXmlDocument SectionElementIsRoot=&quot;false&quot;&gt;
	///				&lt;XmlDataTable&gt;
	///					&lt;Row&gt;
	///						&lt;Cell&gt;A1&lt;/Cell&gt;
	///						&lt;Cell&gt;A2&lt;/Cell&gt;
	///						&lt;Cell&gt;A3&lt;/Cell&gt;
	///					&lt;/Row&gt;
	///					&lt;Row&gt;
	///						&lt;Cell&gt;B1&lt;/Cell&gt;
	///						&lt;Cell&gt;B2&lt;/Cell&gt;
	///						&lt;Cell&gt;B3&lt;/Cell&gt;
	///					&lt;/Row&gt;
	///					&lt;Row&gt;
	///						&lt;Cell&gt;C1&lt;/Cell&gt;
	///						&lt;Cell&gt;C2&lt;/Cell&gt;
	///						&lt;Cell&gt;C3&lt;/Cell&gt;
	///					&lt;/Row&gt;
	///				&lt;/XmlDataTable&gt;
	///			&lt;/MyXmlDocument&gt;
	///		&lt;/configuration&gt;
	/// </example>
	/// <para>
	/// From code, this embedded XmlDocument can be easily accessed using the following method...
	/// </para>
	/// <para>
	/// System.Xml.XmlDocument oXmlDataTable = ((ACASLibraries.Configuration.XmlDocumentConfigurationSection)System.Configuration.ConfigurationManager.GetSection(&quot;MyXmlDocument&quot;)).XmlDocument;
	/// </para>
	/// </summary>
	/// <remarks>
	/// HISTORY:
	/// <para>2/12/2007 - JZM - Created.</para>
	/// </remarks>
	public sealed class XmlDocumentConfigurationSection : System.Configuration.ConfigurationSection
	{
		/// <summary>
		/// Defines wether or not the section's element itself should be treated as the root node (document element) of the Xml document itself.  By default, this value is True and is only required when the section element is not the root node (document element).
		/// </summary>
		[ConfigurationProperty("SectionElementIsRoot", DefaultValue="true", IsRequired=false)]
		public bool SectionElementIsRoot
		{
			get
			{
				return bSectionElementIsRoot;
			}
			set
			{
				bSectionElementIsRoot = value;
			}
		}

		/// <summary>
		/// Internal member storing the value of SectionElementIsRoot
		/// </summary>
		private bool bSectionElementIsRoot = true;
		
		/// <summary>
		/// The Xml document as read from the configuration file.  This is the property that should be used to access the Xml for this section.
		/// </summary>
		public XmlDocument XmlDocument = null;
		
		/// <summary>
		/// DeserializeSection overrides the method in System.Configuration.ConfigurationSection and handles the parsing of the source Xml from the config file.
		/// </summary>
		/// <param name="reader">XmlReader providing access to the section's source Xml</param>
		protected override void DeserializeSection(XmlReader reader)
		{
			this.XmlDocument = new XmlDocument();
			this.XmlDocument.Load(reader);
			if(this.XmlDocument.DocumentElement.HasAttribute("SectionElementIsRoot"))
			{
				bSectionElementIsRoot = bool.Parse(this.XmlDocument.DocumentElement.Attributes["SectionElementIsRoot"].Value);
			}
			if(!bSectionElementIsRoot)
			{
				this.XmlDocument.LoadXml(this.XmlDocument.DocumentElement.InnerXml);
			}
		}
	}
	#endregion
}
