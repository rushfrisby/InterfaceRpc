using SerializerDotNet;
using System;
using System.Collections.Generic;
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

		private static Type[] _deserializeMethodTypeSelector = new[] { typeof(byte[]) };
		private const string _deserializeMethodName = "Deserialize";
		private readonly List<RpcClientExtension> _extensions;

		private static IDictionary<int, MethodInfo> _cachedGenericValueTupleCreateMethods = new Dictionary<int, MethodInfo>();
		private static IDictionary<int, MethodInfo> _cachedGenericDeserializeMethods = new Dictionary<int, MethodInfo>();
		private static readonly object _locker = new object();

		public RpcClient()
		{
			if (!typeof(T).IsInterface)
			{
				throw new ApplicationException($"{typeof(T).Name} is not an interface. The type of T in RpcClient<T> must be an interface.");
			}
			_extensions = new List<RpcClientExtension>();
		}

		#region Private Methods

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
				if(wex.Response == null)
				{
					throw new WebException($"An exception occurred while requesting {url}", wex);
				}
				string errorMessage;
				using (var stream = wex.Response.GetResponseStream())
				using (var reader = new StreamReader(stream))
				{
					errorMessage = reader.ReadToEnd();
				}
				throw new WebException($"Service threw an exception while requesting {url}: " + errorMessage, wex);
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

		private void SetParameters(RpcClientOptions options)
		{
			if (string.IsNullOrWhiteSpace(options.BaseAddress))
			{
				throw new ArgumentNullException(nameof(options.BaseAddress));
			}
			_serializer = options.Serializer ?? new JsonSerializer();
			_baseAddress = options.BaseAddress;
			_extensions.AddRange(options.Extensions);

			//cache reflection that we will need during Invoke
			_serializerType = _serializer.GetType();
			_deserializeMethod = _serializerType.GetMethod(_deserializeMethodName, _deserializeMethodTypeSelector);
		}

		#endregion Private Methods

		protected override object Invoke(MethodInfo method, object[] args)
		{
			var url = AddUrlPart(_baseAddress, method.Name);
			byte[] response = null;

			var requestInfo = new RpcClientRequestInfo
			{
				Arguments = args,
				BaseAddress = _baseAddress,
				MethodName = method.Name
			};

			foreach (var extension in _extensions)
			{
				extension.PreSendRequestAction?.Invoke(requestInfo);
			}

			if (args.Length == 0)
			{
				response = Post<object>(url, null);
			}
			else if(args.Length == 1)
			{
				response = Post(url, args[0]);
			}
			else
			{
				response = Post(url, GetTuple(method, args));
			}

			object result = null;
			if (response != null && method.ReturnType != typeof(void))
			{
				MethodInfo genericDeserializeMethod;
				var cacheKey = method.MetadataToken;
				if (!_cachedGenericDeserializeMethods.ContainsKey(cacheKey))
				{
					lock (_locker)
					{
						if (!_cachedGenericDeserializeMethods.ContainsKey(cacheKey))
						{
							genericDeserializeMethod = _deserializeMethod.MakeGenericMethod(method.ReturnType);
							_cachedGenericDeserializeMethods.Add(cacheKey, genericDeserializeMethod);
						}
						else
						{
							genericDeserializeMethod = _cachedGenericDeserializeMethods[cacheKey];
						}
					}
				}
				else
				{
					genericDeserializeMethod = _cachedGenericDeserializeMethods[cacheKey];
				}
				result = genericDeserializeMethod.Invoke(_serializer, new object[] { response });
			}

			foreach (var extension in _extensions)
			{
				extension.PostReceiveResponseAction?.Invoke(new RpcClientResponseInfo(requestInfo)
				{
					Result = result
				});
			}

			return result;
		}

		public static T Create(RpcClientOptions options)
		{
			object proxy = Create<T, RpcClient<T>>();
			((RpcClient<T>)proxy).SetParameters(options);
			return (T)proxy;
		}

		public static object GetTuple(MethodInfo method, object[] args)
		{
			MethodInfo genericValueTupleCreateMethod;
			var cacheKey = method.MetadataToken;

			if (!_cachedGenericValueTupleCreateMethods.ContainsKey(cacheKey))
			{
				lock(_locker)
				{
					if (!_cachedGenericValueTupleCreateMethods.ContainsKey(cacheKey))
					{
						var types = method.GetParameters().Select(x => x.ParameterType).ToArray();
						var valueTupleCreateMethod = typeof(ValueTuple).GetMethods().FirstOrDefault(x => x.Name == "Create" && x.GetParameters().Count() == args.Length);
						if (valueTupleCreateMethod == null)
						{
							//could happen if there are more than 8 arguments
							throw new ApplicationException("Cannot serialize this method's arguments. Try cutting the number of arguments down to 8 or less.");
						}
						genericValueTupleCreateMethod = valueTupleCreateMethod.MakeGenericMethod(types);
						_cachedGenericValueTupleCreateMethods.Add(cacheKey, genericValueTupleCreateMethod);
					}
					else
					{
						genericValueTupleCreateMethod = _cachedGenericValueTupleCreateMethods[cacheKey];
					}
				}
			}
			else
			{
				genericValueTupleCreateMethod = _cachedGenericValueTupleCreateMethods[cacheKey];
			}
			
			return genericValueTupleCreateMethod.Invoke(null, args);
		}
	}
}
