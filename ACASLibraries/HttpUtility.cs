using System;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Web;
using System.Text;
using System.Web;
using System.Web.Script;

namespace ACASLibraries
{
	public static class HttpUtility
	{
		#region EscapeJSONData();
		public static string EscapeJSONData(object Value)
		{
			return EscapeJSONData(Value, false);
		}
		public static string EscapeJSONData(object Value, bool ConvertNullsToEmptyStrings)
		{
			if (ConvertNullsToEmptyStrings && (Value == null || Value == System.DBNull.Value))
			{
				return "\"\"";
			}
			else
			{
				return new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(Value);
			}
		}
		#endregion

		#region SetResponseNoCache();
		public static void SetResponseNoCache(HttpResponse Response)
		{
			Response.Cache.SetCacheability(HttpCacheability.NoCache);
		}
		public static void SetResponseNoCache()
		{
			SetResponseNoCache(HttpContext.Current.Response);
			HttpContext.Current.Response.CacheControl = "no-cache";
			HttpContext.Current.Response.Expires = 0;
			HttpContext.Current.Response.Cache.SetNoStore();
			HttpContext.Current.Response.AddHeader("pragma", "no-cache");
		}
		#endregion
		
		#region SerializeDataContractXml(); SerializeDataContractJson();
		public static string SerializeDataContractXml(Object dataContract)
		{
			DataContractSerializer serializer = null;
			MemoryStream memoryStream = null;
			string serializedDataContract = null;
			try
			{
				serializer = new DataContractSerializer(dataContract.GetType());
				memoryStream = new MemoryStream();
				serializer.WriteObject(memoryStream, dataContract);
				serializedDataContract = Encoding.UTF8.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
			}
			catch (Exception ex)
			{
				serializedDataContract = ex.Message;
			}
			finally
			{
				if (memoryStream != null)
				{
					memoryStream.Close();
					memoryStream.Dispose();
					memoryStream = null;
				}
				serializer = null;
			}
			return serializedDataContract;
		}
		public static string SerializeDataContractJson(Object dataContract)
		{
			DataContractJsonSerializer serializer = null;
			MemoryStream memoryStream = null;
			string serializedDataContract = null;
			try
			{
				serializer = new DataContractJsonSerializer(dataContract.GetType());
				memoryStream = new MemoryStream();
				serializer.WriteObject(memoryStream, dataContract);
				serializedDataContract = Encoding.UTF8.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
			}
			catch (Exception ex)
			{
				serializedDataContract = ex.Message;
			}
			finally
			{
				if (memoryStream != null)
				{
					memoryStream.Close();
					memoryStream.Dispose();
					memoryStream = null;
				}
				serializer = null;
			}
			return serializedDataContract;
		}
		#endregion

		#region DeserializeDataContractXml(); DeserializeDataContractJson();
		public static Object DeserializeDataContractXml(Type dataContractType, Stream stream)
		{
			DataContractSerializer serializer = null;
			Object deserializedDataContract = null;
			Exception innerException = null;
			try
			{
				serializer = new DataContractSerializer(dataContractType);
				deserializedDataContract = serializer.ReadObject(stream);
			}
			catch (Exception ex)
			{
				innerException = ex;
			}
			finally
			{
				serializer = null;
			}
			if (innerException != null)
			{
				throw innerException;
			}
			return deserializedDataContract;
		}
		public static Object DeserializeDataContractXml(Type dataContractType, string data)
		{
			DataContractSerializer serializer = null;
			Object deserializedDataContract = null;
			MemoryStream stream = null;
			StreamWriter sw = null;
			Exception innerException = null;
			try
			{
				serializer = new DataContractSerializer(dataContractType);
				stream = new MemoryStream();
				sw = new StreamWriter(stream);
				sw.Write(data);
				sw.Flush();
				deserializedDataContract = serializer.ReadObject(stream);
			}
			catch (Exception ex)
			{
				innerException = ex;
			}
			finally
			{
				if (sw != null)
				{
					sw.Close();
					sw.Dispose();
					sw = null;
				}
				if (stream != null)
				{
					stream.Close();
					stream.Dispose();
					stream = null;
				}
				serializer = null;
			}
			if (innerException != null)
			{
				throw innerException;
			}
			return deserializedDataContract;
		}
		public static Object DeserializeDataContractJson(Type dataContractType, Stream stream)
		{
			DataContractJsonSerializer serializer = null;
			Object deserializedDataContract = null;
			Exception innerException = null;
			try
			{
				serializer = new DataContractJsonSerializer(dataContractType);
				deserializedDataContract = serializer.ReadObject(stream);
			}
			catch (Exception ex)
			{
				innerException = ex;
			}
			finally
			{
				serializer = null;
			}
			if (innerException != null)
			{
				throw innerException;
			}
			return deserializedDataContract;
		}
		public static Object DeserializeDataContractJson(Type dataContractType, string data)
		{
			DataContractJsonSerializer serializer = null;
			Object deserializedDataContract = null;
			MemoryStream stream = null;
			Exception innerException = null;
			try
			{
				serializer = new DataContractJsonSerializer(dataContractType);
				stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
				deserializedDataContract = serializer.ReadObject(stream);
			}
			catch (Exception ex)
			{
				innerException = ex;
			}
			finally
			{
				if (stream != null)
				{
					stream.Close();
					stream.Dispose();
					stream = null;
				}
				serializer = null;
			}
			if (innerException != null)
			{
				throw innerException;
			}
			return deserializedDataContract;
		}
		#endregion

		#region ParseWebOperationQueryString();
		/// <summary>
		/// Gets the query string parameters from the currently executing incoming request.
		/// </summary>
		/// <returns>A name value collection containing the query string parameters and their values.</returns>
		public static NameValueCollection ParseWebOperationQueryString()
		{
			return System.Web.HttpUtility.ParseQueryString((WebOperationContext.Current != null ? WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri.Query : HttpContext.Current.Request.Url.Query));
		}
		#endregion
	}
}
