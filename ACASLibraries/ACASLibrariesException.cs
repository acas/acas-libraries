﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace ACASLibraries
{
	/// <summary>
	/// Exception generated by a ACASLibraries operation. Inherits System.Exception.
	/// </summary>
	/// <remarks>
	/// It's often useful to generate an error that will be easily identified as having originated inside ACASLibraries instead of the application using it.
	/// </remarks>
	internal class ACASLibrariesException : Exception
	{
		public string SqlCommandDetails { get; set; }

		/// <summary>
		/// Generic error originating in ACASLibraries and bubbling up into a consuming application.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException">The inner exception's message will be appended to the message for convenience when debugging in other applications.</param>
		public ACASLibrariesException(string message, Exception innerException) : base(message + "\r\nInner Exception: -->\r\n" + innerException.Message, innerException) { }

		/// <summary>
		/// Generic error originating in ACASLibraries and bubbling up into a consuming application.
		/// </summary>
		/// <param name="message"></param>		
		public ACASLibrariesException(string message) : base(message) { }

		/// <summary>
		/// An exception thrown because of an error executing a SqlCommand.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="cmd">The SqlCommand that caused the error. To view the details of this SqlCommand, execute the ToString() method on the exception.</param>
		/// <param name="innerException">The inner exception's message will be appended to the message for convenience when debugging in other applications.</param>
		public ACASLibrariesException(string message, IDbCommand cmd, Exception innerException)
			: base(message + "\r\nInner Exception: -->\r\n" + innerException.Message, innerException)
		{
			this.SqlCommandDetails = DebugUtility.GetSqlCommandDetails(cmd);
		}

		public override string ToString()
		{
			return base.ToString() + (this.SqlCommandDetails != null ? "\r\nSqlCommand:\r\n" + this.SqlCommandDetails : "");
		}
	}
}
