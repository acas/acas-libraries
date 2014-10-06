using System.Web.Script.Serialization;

namespace ACASLibraries.WebApi
{
	/// <summary>
	/// Standard result type for POST/PUT/DELETE etc in WebApi or MVC controllers. 
	/// </summary>	
	public class PostResult
	{
		/// <summary>
		/// Result types that POST actions can return. They map to our notification and logging types, so choose a type based on how you want the user
		/// to see the result. (They map closely to bootstrap alert types, too, except that Danger has been replaced with Error).
		/// </summary>
		public enum ResultType { Success, Information, Warning, Error }

		private ResultType _ResultType;
		private string _Message;
		private bool _Notify;
		private object _Data;

		/// <summary>
		/// Creates a PostResult object to be sent back to the client as JSON.  You can specify whether the client-side framework should notify the user
		/// for you, and pass a message and data back to the client.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="message"></param>
		/// <param name="data"></param>
		/// <param name="notify">Should the client side framework automatically notify the user of this result (based on the type, message, and data)? 
		/// If false, you should handle this PostResult yourself in the client-side code.</param>
		public PostResult(ResultType type, string message, object data, bool notify)
			: base()
		{
			_ResultType = type;
			_Message = message;
			_Data = data;
			_Notify = notify;
		}

		/// <summary>
		/// Creates a PostResult object to be sent back to the client as JSON. The client-side framework will use the type, message, and data
		/// parameters to notify the user.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="message"></param>
		/// <param name="data"></param>
		public PostResult(ResultType type, string message, object data)
			: this(type, message, data, true)
		{ }

		/// <summary>
		/// Creates a PostResult object to be sent back to the client as JSON. The client-side framework will use the type and message
		/// parameters to notify the user.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="message"></param>
		public PostResult(ResultType type, string message)
			: this(type, message, null, true)
		{ }

		/// <summary>
		/// Creates a PostResult object to be sent back to the client as JSON. Takes no parameters other than the type, and sets Notify to false.
		/// Use this overload only if you will handle the result yourself in the client-side code, the client-side framework cannot do anything 
		/// with a result that has no message and will not notify the user for you.
		/// </summary>
		/// <param name="type"></param>
		public PostResult(ResultType type)
			: this(type, null, null, false)
		{ }

		/// <summary>
		/// Serializes the PostResult to a JSON string.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			JavaScriptSerializer jss = new JavaScriptSerializer();
			return jss.Serialize(new
			{
				type = this._ResultType.ToString().ToLower(),
				message = this._Message,
				data = this._Data,
				notify = this._Notify
			});
		}
	}

	//TODO add the JsonConverter. The issue is currently version compatibility in Newtonsoft.Json
}
