using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Xsl;

namespace ACASLibraries
{
	/// <summary>
	/// The XmlUtility class includes functions that are useful when working with Xml.
	/// </summary>
	public class XmlUtility
	{
		#region Escape(); Unescape();
		/// <summary>
		/// Escapes the specified text (or string version of an object) and returns a string safe to embed with an Xml document.
		/// </summary>
		/// <param name="Text">The text to be escaped</param>
		/// <returns>The escaped string version of the text</returns>
		public static string Escape(object Text)
		{
			string sText = Parser.ToString(Text);
			if(sText != null && sText.Length > 0)
			{
				//escape: & (ampersand)
				sText = sText.Replace("&", "&amp;");
				//escape: <> (less than/greater than)
				sText = sText.Replace("<", "&lt;").Replace(">", "&gt;");
				//escape: "' (quote/apostrophe)
				sText = sText.Replace("\"", "&quot;").Replace("'", "&apos;");
			}
			return sText;
		}

		/// <summary>
		/// Unescapes the specified text (or string version of an object) and returns a string in its original form.
		/// </summary>
		/// <param name="Text">The text to be unescaped</param>
		/// <returns>The unescaped string version of the text</returns>
		public static string Unescape(object Text)
		{
			string sText = Parser.ToString(Text);
			if(sText != null && sText.Length > 0)
			{
				Regex oRegX = null;
				//unescape: & (ampersand)
				oRegX = new Regex("&amp;", RegexOptions.IgnoreCase | RegexOptions.Multiline);
				sText = oRegX.Replace(sText, "&"); //sText.Replace("&amp;", "&");
				//unescape: <> (less than/greater than)
				oRegX = new Regex("&lt;", RegexOptions.IgnoreCase | RegexOptions.Multiline);
				sText = oRegX.Replace(sText, "<");
				oRegX = new Regex("&gt;", RegexOptions.IgnoreCase | RegexOptions.Multiline);
				sText = oRegX.Replace(sText, ">");
				//unescape: "' (quote/apostrophe)
				oRegX = new Regex("&quot;", RegexOptions.IgnoreCase | RegexOptions.Multiline);
				sText = oRegX.Replace(sText, "\"");
				oRegX = new Regex("&apos;", RegexOptions.IgnoreCase | RegexOptions.Multiline);
				sText = oRegX.Replace(sText, "'");
				oRegX = null;
			}
			return sText;
		}
		#endregion

		#region GetNodeSum(); GetNodeText();
		/// <summary>
		/// Gets the sum of the double values for all nodes returned by executing the supplied XPath query on the supplied Node
		/// </summary>
		/// <param name="XPath">The string XPath to the nodes containing numeric values to be added</param>
		/// <param name="Node">The XmlNode to be selected from</param>
		/// <returns>The double value containing the sum of all double values in the nodes returned by the XPath query</returns>
		public static double GetNodeSum(string XPath, XmlNode Node)
		{
			double dOutput = 0;
			foreach(XmlNode oChildNode in Node.SelectNodes(XPath))
			{
				dOutput += Parser.ToDouble(oChildNode.InnerText);
			}
			return dOutput;
		}

		/// <summary>
		/// Gets the text value of first node returned by selecting from the supplied Node using the XPath string.
		/// </summary>
		/// <param name="XPath">The string XPath to the node containing the string value to be returned</param>
		/// <param name="Node">The XmlNode to be selected from</param>
		/// <returns>The string value containing the text of the desired node.</returns>
		public static string GetNodeText(string XPath, XmlNode Node)
		{
			return GetNodeText(XPath, Node, null);
		}
		/// <summary>
		/// Gets the text value of first node returned by selecting from the supplied Node using the XPath string.
		/// </summary>
		/// <param name="XPath">The string XPath to the node containing the string value to be returned</param>
		/// <param name="Node">The XmlNode to be selected from</param>
		/// <param name="Default"></param>
		/// <returns>The string value containing the text of the desired node.</returns>
		public static string GetNodeText(string XPath, XmlNode Node, string Default)
		{
			try
			{
				string sOutput = null;
				if(Default != null)
				{
					//if default exists, return if no other unique values are found
					foreach(XmlNode oSubNode in Node.SelectNodes(XPath))
					{
						if(oSubNode != null)
						{
							sOutput = oSubNode.InnerText;
							if(sOutput != null && sOutput != Default)
							{
								break;
							}
						}
					}
				}
				else
				{
					sOutput = Node.SelectSingleNode(XPath).InnerText;
				}
				if(ACASLibraries.Trace.IsEnabled)
				{
					ACASLibraries.Trace.Write("XmlUtility.GetNodeText(\"" + XPath + "\")", sOutput);
				}
				return sOutput;
			}
			catch //else
			{
				return "";
			}
		}
		#endregion

		#region RemoveEmptyNodes();
		/// <summary>
		/// Removes any orphan nodes that have no attributes with values, child nodes or text content.
		/// </summary>
		/// <param name="Node">The Node to be scanned.</param>
		/// <returns>A cleaned XmlNode</returns>
		public static XmlNode RemoveEmptyNodes(XmlNode Node)
		{
			if(Node != null)
			{
				//string sRegXPattern = "\\>[A-Za-z0-9]*\\<";
				//Regex oRegX = new Regex(sRegXPattern,RegexOptions.IgnoreCase & RegexOptions.Multiline);
				int x = 0;
				while(x < Node.Attributes.Count)
				{
					if(ACASLibraries.Trace.IsEnabled)
					{
						ACASLibraries.Trace.Write("XmlUtility.RemoveEmptyNodes()", "Checking Xml attribute: " + Node.Name + "/@" + Node.Attributes[x].Name);
					}
					if(Node.Attributes[x].InnerText == null || Node.Attributes[x].InnerText.Length == 0)
					{
						//remove empty attributes
						Node.Attributes.RemoveAt(x);
						//x--;
					}
					else
					{
						x++;
					}
				}

				if(Node.ChildNodes.Count > 0)
				{
					//iterate through child nodes
					x = 0;
					while(x < Node.ChildNodes.Count)
					{
						if(Node.ChildNodes[x] != null)
						{
							if(Node.ChildNodes[x].NodeType == XmlNodeType.Text || Node.ChildNodes[x].NodeType == XmlNodeType.Attribute)
							{
								//text or attribute node
								if(
									(Node.ChildNodes[x].NodeType == XmlNodeType.Text && Node.ChildNodes[x].InnerText.Length > 0)
									||
									(Node.ChildNodes[x].Attributes.Count > 0)
								)
								{
									//still has text or attributes
									x++;
								}
								else
								{
									if(ACASLibraries.Trace.IsEnabled)
									{
										ACASLibraries.Trace.Write("XmlUtility.RemoveEmptyNodes()", "Node has NO text or attributes, remove it!");
									}
									Node.RemoveChild(Node.ChildNodes[x]);
									//x--;
								}
							}
							else if(Node.ChildNodes[x].ChildNodes.Count > 0 || Node.ChildNodes[x].Attributes.Count > 0)
							{
								//has children or attribute nodes
								Node.ReplaceChild(Node.ChildNodes[x], RemoveEmptyNodes(Node.ChildNodes[x]));
								if(Node.ChildNodes[x].ChildNodes.Count > 0 || Node.ChildNodes[x].Attributes.Count > 0)
								{
									x++;
								}
								else
								{
									Node.RemoveChild(Node.ChildNodes[x]);
								}

							}
							else
							{
								//has no text, attrbiutes or children, remove the node
								Node.RemoveChild(Node.ChildNodes[x]);
								//x--;
							}
						}
						else
						{
							x++;
						}
					}
				}
			}
			return Node;
		}
		#endregion

		#region FormatXml();
		/// <summary>
		/// Formats the supplied Xml to properly indented and formatted Xml.
		/// </summary>
		/// <param name="Xml">The Xml to be formatted.</param>
		/// <returns>The formatted Xml as a string.</returns>
		public static string FormatXml(string Xml)
		{
			string output = null;
			XmlDocument XmlDoc = new XmlDocument();
			try
			{
				XmlDoc.LoadXml(Xml);
				output = FormatXml(XmlDoc, 0);
			}
			catch
			{
				output = Xml;
				if(ACASLibraries.Trace.IsEnabled)
				{
					ACASLibraries.Trace.Write("XmlUtility.FormatXml()", "FAILED to load Xml source!");
				}
			}
			XmlDoc = null;
			return output;
		}
		/// <summary>
		/// Formats the supplied XmlDocument to properly indented and formatted Xml.
		/// </summary>
		/// <param name="XmlDoc">The XmlDocument to be formatted.</param>
		/// <returns>The formatted Xml as a string.</returns>
		public static string FormatXml(XmlDocument XmlDoc)
		{
			return FormatXml(XmlDoc, 0);
		}
		/// <summary>
		/// Internal method for FormatXml.  Handles the intending for a single node and traverses any decendant child nodes.
		/// </summary>
		/// <param name="Node">The XmlNode to be formatted</param>
		/// <param name="StartingIndent">The current level of indenting</param>
		/// <returns>The formatted XmlNode and it's decendants as a string</returns>
		private static string FormatXml(XmlNode Node, int StartingIndent)
		{
			bool bHasOnlyATextNode = false;
			StringBuilder oFormatXmlBuilder = new StringBuilder();
			string sTab = "";
			for(int s = 0;s < StartingIndent;s++)
			{
				sTab = sTab + "\t"; //  adds a \t for TAB
			}

			switch(Node.NodeType)
			{
				case XmlNodeType.XmlDeclaration:
					//declaration node
					oFormatXmlBuilder.Append("<?xml");
					if(((XmlDeclaration)Node).Version != null && ((XmlDeclaration)Node).Version.Length > 0)
						oFormatXmlBuilder.Append(string.Concat(" version=\"", ((XmlDeclaration)Node).Version, "\""));
					if(((XmlDeclaration)Node).Encoding != null && ((XmlDeclaration)Node).Encoding.Length > 0)
						oFormatXmlBuilder.Append(string.Concat(" encoding=\"", ((XmlDeclaration)Node).Encoding, "\""));
					if(((XmlDeclaration)Node).Standalone != null && ((XmlDeclaration)Node).Standalone.Length > 0)
						oFormatXmlBuilder.Append(string.Concat(" standalone=\"", ((XmlDeclaration)Node).Standalone, "\""));
					for(int df=0;df<Node.ChildNodes.Count;df++)
					{
						oFormatXmlBuilder.Append(FormatXml(Node.ChildNodes[df], 0));
					}
					oFormatXmlBuilder.Append("?>\r\n");
					break;
				case XmlNodeType.DocumentType:
					oFormatXmlBuilder.Append(string.Concat(Node.OuterXml, "\r\n"));
					break;
				case XmlNodeType.DocumentFragment:
					//all child nodes of the document should be at the same indent Level
					//just iterate over them and recurse with 0 indent
					for(int df = 0;df < Node.ChildNodes.Count;df++)
					{
						oFormatXmlBuilder.Append(FormatXml(Node.ChildNodes[df], 0));
					}
					break;
				case XmlNodeType.Document:
					//all child nodes of the document should be at the same indent Level
					//just iterate over them and recurse with 0 indent
					for(int d = 0;d < Node.ChildNodes.Count;d++)
					{
						oFormatXmlBuilder.Append(FormatXml(Node.ChildNodes[d], 0));
					}
					break;
				case XmlNodeType.Text:
					//should render the same way the default IE5 stylesheet does for mixed content
					//we're gonna strip out any tabs and carriage returns from the Text
					//** disabled, since it is handled under NODE_ELEMENT
					//** this should be re-enabled if mixed text nodes are to be allowed
					oFormatXmlBuilder.Append(Escape(Node.InnerText.Trim()));
					break;
				case XmlNodeType.Element:
					if(Node.HasChildNodes && (Node.ChildNodes[0].NodeType == XmlNodeType.Text) && (Node.ChildNodes.Count == 1))
					{
						//if the node has only one child and that child is text we won't add carriage return after opening tag
						bHasOnlyATextNode = true;
					}

					//open the start tag
					oFormatXmlBuilder.Append(string.Concat(sTab, "<", Node.Name));

					//recurse over the attributes
					for(int e = 0;e < Node.Attributes.Count;e++)
					{
						oFormatXmlBuilder.Append(FormatXml(Node.Attributes[e], 0));
					}

					//properly close the start tag based on node's contents
					if(!Node.HasChildNodes) //no child nodes so it's an empty element
					{
						oFormatXmlBuilder.Append("/>\r\n");
					}
					else
					{
						if(bHasOnlyATextNode)
						{   //has only text for children - don't add carriage return
							oFormatXmlBuilder.Append(">" + Node.InnerText.Trim());
						}
						else
						{	//has child elements - add carriage return
							oFormatXmlBuilder.Append(">\r\n");
							//recurse if there's children
							for(int ec = 0;ec < Node.ChildNodes.Count;ec++)
							{
								oFormatXmlBuilder.Append(FormatXml(Node.ChildNodes[ec], StartingIndent + 1));
							}
						}
						//properly indent and add the end tag
						if(!bHasOnlyATextNode)
						{
							oFormatXmlBuilder.Append(sTab);
						}
						oFormatXmlBuilder.Append(string.Concat("</", Node.Name, ">\r\n"));
					}
					break;
				case XmlNodeType.Comment:
					//if comment is on more than one line don't indent
					if(Node.OuterXml.IndexOf("\r\n") == 0)
					{
						oFormatXmlBuilder.Append(sTab);
					}
					oFormatXmlBuilder.Append(string.Concat(Node.OuterXml, "\r\n"));
					break;
				case XmlNodeType.CDATA:
					//if comment is on more than one line don't indent
					if(Node.OuterXml.IndexOf("\r\n") == 0)
					{
						oFormatXmlBuilder.Append(sTab);
					}
					oFormatXmlBuilder.Append(string.Concat(Node.OuterXml, "\r\n"));
					break;
				case XmlNodeType.Attribute:
					//if there are double quotes in the attribute use single quotes to surrond the attr value
					oFormatXmlBuilder.Append(string.Concat(" ", Node.Name, "=\"", Escape(Node.InnerText), "\""));
					break;
				case XmlNodeType.Entity:
					//and we would never want entites expanded
					oFormatXmlBuilder.Append(Node.OuterXml);
					break;
				default:
					//all other node types should just return their xml (properly indented)
					//these include - entity refs, pi's, notations, doctypes
					oFormatXmlBuilder.Append(string.Concat(sTab, Node.OuterXml, "\r\n"));
					break;
			}
			return oFormatXmlBuilder.ToString();
		}
		#endregion

		#region TransformXml();
		/// <summary>
		/// Transforms the specified Xml using the specified Xsl.
		/// </summary>
		/// <param name="XmlSource">The Xml source to be transformed.</param>
		/// <param name="XslSource">The Xml source of the Xsl Transform to be applied.</param>
		/// <returns>The transformed Xml.</returns>
		public static string TransformXml(string XmlSource, string XslSource)
		{
			XmlDocument oXmlDocument = new XmlDocument();
			XmlDocument oXslDocument = new XmlDocument();
			string sTransformedXml = null;

			try
			{
				oXmlDocument.LoadXml(XmlSource);
				oXslDocument.LoadXml(XslSource);

				sTransformedXml = TransformXml(oXmlDocument, oXslDocument);
			}
			catch(XsltCompileException oException)
			{
				throw oException;
			}
			catch(XsltException oException)
			{
				throw oException;
			}
			catch(XmlException oException)
			{
				throw oException;
			}
			catch(Exception oException)
			{
				throw oException;
			}
			finally
			{
				oXslDocument = null;
				oXmlDocument = null;
			}

			return sTransformedXml;
		}
		/// <summary>
		/// Transforms the specified Xml using the specified Xsl.
		/// </summary>
		/// <param name="XmlDocument">The Xml document to be transformed.</param>
		/// <param name="XslDocument">The Xml document of the Xsl Transform to be applied.</param>
		/// <returns>The transformed Xml.</returns>
		public static string TransformXml(XmlDocument XmlDocument, XmlDocument XslDocument)
		{
			XslCompiledTransform oXslTransform = null;
			System.IO.Stream oStreamOut = null;
			XmlWriter oXmlWriter = null;
			System.IO.StreamReader oSR = null;
			string sTransformedXml = null;
			try
			{
				oXslTransform = new XslCompiledTransform();
				oXslTransform.Load(XslDocument);

				oStreamOut = new System.IO.MemoryStream();
				oXmlWriter = new XmlTextWriter(oStreamOut, System.Text.Encoding.UTF8);

				oXslTransform.Transform(XmlDocument, null, oXmlWriter);

				oStreamOut.Flush();
				oStreamOut.Position = 0;
				oSR = new System.IO.StreamReader(oStreamOut);

				sTransformedXml = oSR.ReadToEnd();
			}
			catch(XsltCompileException oException)
			{
				throw oException;
			}
			catch(XsltException oException)
			{
				throw oException;
			}
			catch(XmlException oException)
			{
				throw oException;
			}
			catch(Exception oException)
			{
				throw oException;
			}
			finally
			{
				if(oSR != null)
				{
					oSR.Close();
					oSR.Dispose();
					oSR = null;
				}
				if(oXmlWriter != null)
				{
					oXmlWriter.Close();
					oXmlWriter = null;
				}
				if(oStreamOut != null)
				{
					oStreamOut.Close();
					oStreamOut.Dispose();
					oStreamOut = null;
				}
				oXslTransform = null;
			}

			return sTransformedXml;
		}
		#endregion
	}
}
