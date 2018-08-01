using SerializerDotNet;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace InterfaceRpc.Client
{
	public class RpcClient<T> : DispatchProxy
	{
		private const int _blockSize = 4096;
		private string _baseAddress;
		private static ISerializer _serializer;

		public RpcClient()
		{
			if (!typeof(T).IsInterface)
			{
				throw new ApplicationException($"{typeof(T).Name} is not an interface. The type of T in RpcClient<T> must be an interface.");
			}
		}

		#region Private Methods

		//private TResult Post<TResult>(string url)
		//{
		//	var result = Post<object>(url, null);
		//	return _serializer.Deserialize<TResult>(result);
		//}

		//private TResult Post<TSource, TResult>(string url, TSource source)
		//{
		//	var result = Post(url, source);
		//	return _serializer.Deserialize<TResult>(result);
		//}

		private byte[] Post<TSource>(string url, TSource source)
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
				throw new WebException("The Security API threw an exception: " + errorMessage);
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

		#endregion Private Methods

		protected override object Invoke(MethodInfo method, object[] args)
		{
			var url = AddUrlPart(_baseAddress, method.Name);

			object arg = null;
			if(args != null && args.Any())
			{
				arg = args[0];
			}

			var result = Post(url, arg);
			if(result != null && method.ReturnType != typeof(void))
			{
				var serializerType = _serializer.GetType();
				var deserializeMethods = serializerType.GetMethods();
				var deserializeMethod = serializerType.GetMethod("Deserialize", new[] { typeof(byte[]) });
				var genericDeserializeMethod = deserializeMethod.MakeGenericMethod(method.ReturnType);
				return genericDeserializeMethod.Invoke(_serializer, new object[] { result });
			}

			return null;
		}

		private void SetParameters(ISerializer serializer, string baseAddress)
		{
			if (string.IsNullOrWhiteSpace(baseAddress))
			{
				throw new ArgumentNullException(nameof(baseAddress));
			}
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
			_baseAddress = baseAddress;
		}

		public static T Create(ISerializer serializer, string baseAddress)
		{
			object proxy = Create<T, RpcClient<T>>();
			((RpcClient<T>)proxy).SetParameters(serializer, baseAddress);
			return (T)proxy;
		}
	}
}
