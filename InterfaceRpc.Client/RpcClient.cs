using SerializerDotNet;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace InterfaceRpc.Client
{
	public class RpcClient<T> : DispatchProxy where T : class
	{
		private string _baseAddress;
		private ISerializer _serializer;
		private Type _serializerType;
		private MethodInfo _deserializeMethod;

		public RpcClient()
		{
			if (!typeof(T).IsInterface)
			{
				throw new ApplicationException($"{typeof(T).Name} is not an interface. The type of T in RpcClient<T> must be an interface.");
			}
		}

		#region Private Methods

		private byte[] Post(string url, object[] source)
		{
			var request = (HttpWebRequest)WebRequest.Create(url);

			byte[] data;
			if (source != null)
			{
				data = _serializer.Serialize(source);
			}
			else
			{
				data = new byte[0];
			}

			request.Method = "POST";
			request.ContentType = _serializer.DefaultContentType;
			request.ContentLength = data.Length;

			using (var stream = request.GetRequestStream())
			{
				stream.Write(data, 0, data.Length);
			}

			byte[] result = null;
			try
			{
				using (var response = (HttpWebResponse)request.GetResponse())
				using (var stream = response.GetResponseStream())
				using (var memoryStream = new MemoryStream())
				{
					stream.CopyTo(memoryStream);
					result = memoryStream.ToArray();
				}
			}
			catch (WebException wex)
			{
				string errorMessage;
				using (var stream = wex.Response.GetResponseStream())
				using (var reader = new StreamReader(stream))
				{
					errorMessage = reader.ReadToEnd();
				}
				throw new WebException("Service threw an exception: " + errorMessage);
			}

			return result;
		}

		private static string AddUrlPart(string baseUrl, string part)
		{
			if (baseUrl.EndsWith("/"))
			{
				baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
			}
			if (part.StartsWith("/"))
			{
				part = part.Substring(1, part.Length - 1);
			}
			return baseUrl + "/" + part;
		}

		private void SetParameters(ISerializer serializer, string baseAddress)
		{
			if (string.IsNullOrWhiteSpace(baseAddress))
			{
				throw new ArgumentNullException(nameof(baseAddress));
			}
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
			_baseAddress = baseAddress;

			//cache reflection that we will need during Invoke
			_serializerType = _serializer.GetType();
			_deserializeMethod = _serializerType.GetMethod("Deserialize", new[] { typeof(byte[]) });
		}

		#endregion Private Methods

		protected override object Invoke(MethodInfo method, object[] args)
		{
			var url = AddUrlPart(_baseAddress, method.Name);
			var result = Post(url, args);
			if(result != null && method.ReturnType != typeof(void))
			{
				var genericDeserializeMethod = _deserializeMethod.MakeGenericMethod(method.ReturnType);
				return genericDeserializeMethod.Invoke(_serializer, new object[] { result });
			}

			return null;
		}

		public static T Create(ISerializer serializer, string baseAddress)
		{
			object proxy = Create<T, RpcClient<T>>();
			((RpcClient<T>)proxy).SetParameters(serializer, baseAddress);
			return (T)proxy;
		}
	}
}
