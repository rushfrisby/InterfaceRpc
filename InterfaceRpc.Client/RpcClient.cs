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

		private static IDictionary<int, MethodInfo> _cachedGenericValueTupleCreateMethods = new Dictionary<int, MethodInfo>();
		private static readonly object _locker = new object();

		public RpcClient()
		{
			if (!typeof(T).IsInterface)
			{
				throw new ApplicationException($"{typeof(T).Name} is not an interface. The type of T in RpcClient<T> must be an interface.");
			}
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
					throw;
				}
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

		private void SetParameters(string baseAddress, ISerializer serializer = null)
		{
			if (string.IsNullOrWhiteSpace(baseAddress))
			{
				throw new ArgumentNullException(nameof(baseAddress));
			}
			_serializer = serializer ?? new JsonSerializer();
			_baseAddress = baseAddress;

			//cache reflection that we will need during Invoke
			_serializerType = _serializer.GetType();
			_deserializeMethod = _serializerType.GetMethod(_deserializeMethodName, _deserializeMethodTypeSelector);
		}

		#endregion Private Methods

		protected override object Invoke(MethodInfo method, object[] args)
		{
			var url = AddUrlPart(_baseAddress, method.Name);
			byte[] result = null;

			if(args.Length == 0)
			{
				result = Post<object>(url, null);
			}
			else if(args.Length == 1)
			{
				result = Post(url, args[0]);
			}
			else
			{
				result = Post(url, GetTuple(method, args));
			}

			if (result != null && method.ReturnType != typeof(void))
			{
				var genericDeserializeMethod = _deserializeMethod.MakeGenericMethod(method.ReturnType);
				return genericDeserializeMethod.Invoke(_serializer, new object[] { result });
			}
			return null;
		}

		public static T Create(string baseAddress, ISerializer serializer = null)
		{
			object proxy = Create<T, RpcClient<T>>();
			((RpcClient<T>)proxy).SetParameters(baseAddress, serializer);
			return (T)proxy;
		}

		public static object GetTuple(MethodInfo method, object[] args)
		{
			MethodInfo genericValueTupleCreateMethod;
			if (!_cachedGenericValueTupleCreateMethods.ContainsKey(method.MetadataToken))
			{
				lock(_locker)
				{
					if (!_cachedGenericValueTupleCreateMethods.ContainsKey(method.MetadataToken))
					{
						var types = method.GetParameters().Select(x => x.ParameterType).ToArray();
						var valueTupleCreateMethod = typeof(ValueTuple).GetMethods().FirstOrDefault(x => x.Name == "Create" && x.GetParameters().Count() == args.Length);
						if (valueTupleCreateMethod == null)
						{
							//could happen if there are more than 8 arguments
							throw new ApplicationException("Cannot serialize this method's arguments. Try cutting the number of arguments down to 8 or less.");
						}
						genericValueTupleCreateMethod = valueTupleCreateMethod.MakeGenericMethod(types);
						_cachedGenericValueTupleCreateMethods.Add(method.MetadataToken, genericValueTupleCreateMethod);
					}
					else
					{
						genericValueTupleCreateMethod = _cachedGenericValueTupleCreateMethods[method.MetadataToken];
					}
				}
			}
			else
			{
				genericValueTupleCreateMethod = _cachedGenericValueTupleCreateMethods[method.MetadataToken];
			}
			
			return genericValueTupleCreateMethod.Invoke(null, args);
		}
	}
}
