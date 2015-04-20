using System;
using System.Text;

namespace ACASLibraries
{
	/// <summary>
	/// Pager Control allows you to select page from multipage areas
	/// Note that all item indices and page numbers are 1 based
	/// <code>
	/// string sURL = "xxx";
	/// Pager oPager = new Pager(1, iTotalRecords, 10, [[[maximum number of pages]]], [[[current URL]]]", "");
	/// ...
	/// DataRow oDR;
	/// for (int iDataViewRowIndex = oPager.IndexOfFirstItemOnCurrentPage - 1; iDataViewRowIndex &lt; oPager.IndexOfLastItemOnCurrentPage; iDataViewRowIndex++)
	/// {
	///   oDR = oDS.Tables[0].Rows[iDataViewRowIndex];
	///   ...
	/// }
	/// ...
	/// &lt;table width="100%" cellpadding="3" cellspacing="0" border="0"&gt;
	///   &lt;tr&gt;
	///     &lt;td style="FONT-SIZE: 9pt" nowrap="true"&gt;Items Per Page:
	///       &lt;select id="ItemsPerPage" name="ItemsPerPage" style="FONT-SIZE: 9pt" onchange="javascript:location.replace('&lt;%=[[[this URL]]]%&gt;&amp;btnItemsPerPage=true&amp;ItemsPerPage=' + this.options[this.selectedIndex].value);"&gt;
	///         &lt;%=oPager.GetItemsPrePageSelectBoxOptionsHTML([[[currently selected value]]], [[[values for dropdown list]]])%&gt;
	///       &lt;/select&gt;
	///     &lt;/td&gt;
	///     &lt;td class="RowB" align="center" width="50%" nowrap="true"&gt;&lt;%=oPager.GetPagerControlHTML()%&gt;&lt;/td&gt;
	///     &lt;td align="right" class="TableFooter" width="25%" nowrap="true"&gt;&lt;%=oPager.GetItemCountSummaryText("", "", "Total Items", "Total Items")%&gt;&lt;/td&gt;
	///     &lt;%oPager = null;%&gt;
	///   &lt;/tr&gt;
	/// &lt;/table&gt;
	/// </code>
	/// </summary>
	public class Pager
	{

		public const int INT_MAX = 2147483647;

		#region private properties
		private int m_iItemsPerPage = 20;
		private int m_iMaxNumberOfPages = 20;  // -1 is no page limit
		private int m_iTotalItemCount;
		private int m_iCurrentPageNumber = 1;
		private int m_iTotalNumberOfPages;
		private string m_sURLBeforePageNumber = "";
		private string m_sURLAfterPageNumber = "";
		private string m_sDelimiter = " ";  // e.g. " | " would be 1 | 2 | 3...
		private string m_sPageNumberPrefix = ""; // e.g. "Page " would be Page 1 | Page 2 | ...
		private string m_sSinglePageMessage = "Page 1 of 1";
		private string m_sPagerString = ""; // html control text
		#endregion

		#region public properties
		public string Delimiter
		{
			set { m_sDelimiter = value; }
			get { return m_sDelimiter; }
		}
		public string SinglePageMessage
		{
			set { m_sSinglePageMessage = value; }
			get { return m_sSinglePageMessage; }
		}
		public string PageNumberPrefix
		{
			set { m_sPageNumberPrefix = value; }
			get { return m_sPageNumberPrefix; }
		}

		// 20 per page, current page = 2 ---> 21
		public int IndexOfFirstItemOnCurrentPage
		{
			get { return (m_iItemsPerPage * (m_iCurrentPageNumber - 1)) + 1; }
		}
		// 27 records 20 per page, current page = 2 ---> 27
		public int IndexOfLastItemOnCurrentPage
		{
			get { return Math.Min(m_iTotalItemCount, m_iItemsPerPage * m_iCurrentPageNumber); }
		}
		// 27 records 20 per page, current page = 2 ---> 40
		public int LastIndexOfItemInCurrentPageRange
		{
			get { return m_iItemsPerPage * m_iCurrentPageNumber; }
		}
		public int CurrentPageNumber
		{
			get { return m_iCurrentPageNumber; }
			//set { m_iCurrentPageNumber = value; }
		}
		#endregion

		#region constructor
		public Pager(int iCurrentPageNumber, int iTotalItemCount, int iItemsPerPage, int iMaxNumberOfPages, string sURLBeforePageNumber, string sURLAfterPageNumber)
		{
			m_iTotalItemCount = iTotalItemCount;
			m_iItemsPerPage = iItemsPerPage > 0 ? iItemsPerPage : INT_MAX;
			m_iMaxNumberOfPages = iMaxNumberOfPages > 0 ? iMaxNumberOfPages : INT_MAX;
			m_sURLBeforePageNumber = sURLBeforePageNumber;
			m_sURLAfterPageNumber = sURLAfterPageNumber;
			m_iTotalNumberOfPages = Math.Min((int)Math.Ceiling((double)m_iTotalItemCount / m_iItemsPerPage), m_iMaxNumberOfPages);
			m_iCurrentPageNumber = Math.Max(Math.Min(iCurrentPageNumber, m_iTotalNumberOfPages), 1);
			CreatePagerControl();
		}
		#endregion

		#region CreatePagerControl();
		private void CreatePagerControl()
		{
			if(m_iTotalNumberOfPages < 2)
			{
				m_sPagerString = m_sSinglePageMessage;
			}
			else
			{
				int iNumberOfPages = Math.Min(m_iTotalNumberOfPages, m_iMaxNumberOfPages);
				StringBuilder oSB = new StringBuilder();
				for(int iPageLinkIndex = 1;iPageLinkIndex <= iNumberOfPages;iPageLinkIndex++)
				{
					if(iPageLinkIndex != m_iCurrentPageNumber)
					{
						oSB.Append("<a href=\"" + m_sURLBeforePageNumber + iPageLinkIndex.ToString() + m_sURLAfterPageNumber + "\">" + m_sPageNumberPrefix + iPageLinkIndex.ToString() + "</a>");
					}
					else
					{
						oSB.Append(m_sPagerString += iPageLinkIndex.ToString());
					}
					if(iPageLinkIndex < iNumberOfPages)
					{
						oSB.Append(m_sDelimiter);
					}
				}
				m_sPagerString = oSB.ToString();
				oSB = null;
			}
		}
		#endregion

		#region GetPagerControlHTML();
		public string GetPagerControlHTML()
		{
			return m_sPagerString;
		}
		#endregion

		#region GetItemsPrePageSelectBoxOptionsHTML();
		// option values only, the select elements will not be generated by this function
		public string GetItemsPrePageSelectBoxOptionsHTML(int iSelectedValue, params int[] a_iNumberOfItemsPrePage)
		{
			StringBuilder oSB = new StringBuilder();
			string sReturnValue;
			for (int iItemIndex = 0; iItemIndex < a_iNumberOfItemsPrePage.Length; iItemIndex++)
			{
				oSB.Append("<option value=\"" + a_iNumberOfItemsPrePage[iItemIndex] + "\"" + (a_iNumberOfItemsPrePage[iItemIndex] == iSelectedValue ? " selected=\"selected\"" : "") + ">" + a_iNumberOfItemsPrePage[iItemIndex] + "</option>");
			}
			sReturnValue = oSB.ToString();
			oSB = null;
			return sReturnValue;
		}
		#endregion

		#region GetItemsPrePageSelectBoxOptionsHTMLWithAll();
		public string GetItemsPrePageSelectBoxOptionsHTMLWithAll(int iSelectedValue, params int[] a_iNumberOfItemsPrePage)
		{
			return GetItemsPrePageSelectBoxOptionsHTML(iSelectedValue, a_iNumberOfItemsPrePage) +
								"<option value=\"-1\"" + (iSelectedValue == -1 ? " selected=\"selected\"" : "") + ">ALL</option>";
		}
		#endregion

		#region GetItemCountSummaryText();
		public string GetItemCountSummaryText(string sBeforePlural, string sBeforeSingular, string sAfterPlural, string sAfterSingular)
		{
			return (m_iTotalNumberOfPages > 1 ? (IndexOfFirstItemOnCurrentPage.ToString() + "-" + IndexOfLastItemOnCurrentPage.ToString() + " of ") : "") + 
				(m_iTotalItemCount == 1 ? sBeforeSingular : sBeforePlural) + " " + m_iTotalItemCount + " " + (m_iTotalItemCount == 1 ? sAfterSingular : sAfterPlural);
		}
		#endregion

		#region GetPageNumberText();
		public string GetPageNumberText()
		{
			return "Page " + m_iCurrentPageNumber.ToString() + " of " + m_iTotalNumberOfPages.ToString();
		}
		#endregion

		#region GetPageIndexFromItemIndex();
		// 20 per page, ask for item 27 ---> 2
		public static int GetPageIndexFromItemIndex(int iItemIndex, int iItemsPerPage)
		{
			return (int)Math.Ceiling(((double)iItemIndex) / iItemsPerPage);
		}
		#endregion
	}
}
