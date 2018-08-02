using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using SerializerDotNet;

namespace InterfaceRpc.Service
{
	internal class RouteManager<T> where T : class
	{
		private readonly MethodInfo[] _interfaceMethods;
		private readonly Type _interfaceType;
		private readonly T _implementation;
		private readonly IDictionary<string, Func<HttpListenerContext, Task<RouteResponse>>> _routeHandlers;

		private static readonly MethodInfo _getRequestEntityMethod = typeof(SerializationHelper).GetMethod("GetRequestEntity", BindingFlags.Static | BindingFlags.NonPublic);

		public RouteManager(T implementation)
		{
			_implementation = implementation ?? throw new ArgumentNullException("T implementation is null");
			_interfaceType = typeof(T);
			_interfaceMethods = _interfaceType.GetMethods();
			_routeHandlers = new Dictionary<string, Func<HttpListenerContext, Task<RouteResponse>>>();

			foreach (var method in _interfaceMethods)
			{
				AddHandler(method.Name, MethodHandler);
			}
		}

		private async Task<RouteResponse> MethodHandler(HttpListenerContext context)
		{
			var routeName = context.Request.RawUrl.Substring(1, context.Request.RawUrl.Length - 1);
			var method = _interfaceMethods.FirstOrDefault(x => x.Name.Equals(routeName, StringComparison.OrdinalIgnoreCase));
			if (method == null)
			{
				throw new NullReferenceException(string.Format("Could not find method corresponding to {0}", context.Request.RawUrl));
			}

			var parameterValues = new List<object>();
			var methodParameters = method.GetParameters();
			var serializer = Serializer.GetSerializerFor(context.Request.ContentType);

			if (methodParameters.Any())
			{
				Type genericMethodType; ;
				//if (methodParameters.Count() == 1)
				//{
				//	genericMethodType = methodParameters.First().ParameterType;
				//}
				//else
				//{
					genericMethodType = typeof(object[]);
				//}
				var greGenericMethod = _getRequestEntityMethod.MakeGenericMethod(genericMethodType);
				var entity = greGenericMethod.Invoke(null, new object[] { context });

				object[] tempParameterValues = null;
				if (entity != null && entity.GetType() == typeof(object[]))
				{
					tempParameterValues = (object[])entity;
				}
				else
				{
					tempParameterValues = new object[] { entity };
				}

				for(var i = 0; i < methodParameters.Count(); i++)
				{
					var methodParameter = methodParameters[i];
					var tempParameterValueType = tempParameterValues[i].GetType();
					if (methodParameter.ParameterType != tempParameterValueType)
					{
						if (tempParameterValueType.IsClass)
						{
							//ugly but it works
							var reserialized = await serializer.SerializeAsync(tempParameterValues[i]);
							var deserializeMethod = serializer.GetType().GetMethod("Deserialize", new[] { typeof(byte[]) });
							var genericDeserializeMethod = deserializeMethod.MakeGenericMethod(methodParameter.ParameterType);
							entity = genericDeserializeMethod.Invoke(serializer, new object[] { reserialized });
							parameterValues.Add(entity);
						}
						else
						{
							parameterValues.Add(Convert.ChangeType(tempParameterValues[i], methodParameter.ParameterType));
						}
					}
					else
					{
						parameterValues.Add(tempParameterValues[i]);
					}
				}
			}

			var result = method.Invoke(_implementation, parameterValues.ToArray());

			var response = new RouteResponse(serializer.DefaultContentType);
			if (method.ReturnType != typeof(void))
			{
				response.Content = await serializer.SerializeAsync(result);
			}
			return response;
		}

		public bool ContainsHandler(string routeName)
		{
			if (routeName == null)
			{
				throw new ArgumentNullException(nameof(routeName));
			}
			var key = routeName.Trim().ToLowerInvariant();
			return _routeHandlers.ContainsKey(key);
		}

		public void AddHandler(string routeName, Func<HttpListenerContext, Task<RouteResponse>> handler)
		{
			if (routeName == null)
			{
				throw new ArgumentNullException(nameof(routeName));
			}
			if (handler == null)
			{
				throw new ArgumentNullException(nameof(handler));
			}

			var key = "/" + routeName.Trim().ToLowerInvariant();
			if (_routeHandlers.ContainsKey(key))
			{
				throw new ApplicationException(string.Format("A handler for the route \"{0}\" has already been added.", routeName));
			}
			_routeHandlers.Add(key, handler);
		}

		public Func<HttpListenerContext, Task<RouteResponse>> GetHandler(string routeName)
		{
			if (routeName == null)
			{
				throw new ArgumentNullException(nameof(routeName));
			}
			var key = routeName.Trim().ToLowerInvariant();
			return _routeHandlers[key];
		}
	}
}
