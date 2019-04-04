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
				object entity = null;
				if (methodParameters.Count() == 1)
				{
					var greGenericMethod = _getRequestEntityMethod.MakeGenericMethod(methodParameters.First().ParameterType);
					entity = greGenericMethod.Invoke(null, new object[] { context });
					parameterValues.Add(entity);
				}
				else
				{
					var types = method.GetParameters().Select(x => x.ParameterType).ToArray();
					var valueTupleType = GetTupleType(types);
					var greGenericMethod = _getRequestEntityMethod.MakeGenericMethod(valueTupleType);
					entity = greGenericMethod.Invoke(null, new object[] { context });
					parameterValues.AddRange(TupleToEnumerable(entity));
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

		private static IEnumerable<object> TupleToEnumerable(object tuple)
		{
			var tupleType = tuple.GetType();
			var tupleTypeProperties = tupleType.GetRuntimeFields();
			var values = tupleTypeProperties.Select(x => x.GetValue(tuple));
			return values;
		}

		public static Type GetTupleType(Type[] types)
		{
			var valueTupleCreateMethod = typeof(ValueTuple).GetMethods().FirstOrDefault(x => x.Name == "Create" && x.GetParameters().Count() == types.Length);
			if (valueTupleCreateMethod == null)
			{
				//could happen if there are more than 8 arguments
				throw new ApplicationException("Cannot deserialize this method's arguments. Try cutting the number of arguments down to 8 or less.");
			}
			var genericValueTupleCreateMethod = valueTupleCreateMethod.MakeGenericMethod(types);
			var dummyValues = new List<object>();
			foreach (var type in types)
			{
				if (type.IsValueType)
				{
					dummyValues.Add(Activator.CreateInstance(type));

				}
				else
				{
					dummyValues.Add(null);
				}
			}
			var valueTuple = genericValueTupleCreateMethod.Invoke(null, dummyValues.ToArray());
			return valueTuple.GetType();
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
