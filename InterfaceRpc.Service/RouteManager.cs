using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using SerializerDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace InterfaceRpc.Service
{
    internal class RouteManager<T> where T : class
    {
        private readonly MethodInfo[] _interfaceMethods;
        private readonly Type _interfaceType;
        private readonly IDictionary<string, Func<string, T, HttpContext, Task<RouteResponse>>> _routeHandlers;
        private readonly RpcServiceOptions<T> _options;

        private static readonly MethodInfo _getRequestEntityMethod = typeof(SerializationHelper).GetMethod("GetRequestEntityAsync", BindingFlags.Static | BindingFlags.NonPublic);

        public RouteManager(RpcServiceOptions<T> options)
        {
            _interfaceType = typeof(T);
            _interfaceMethods = GetAllMethods(_interfaceType);
            _routeHandlers = new Dictionary<string, Func<string, T, HttpContext, Task<RouteResponse>>>();
            _options = options ?? throw new ArgumentNullException(nameof(options));

            foreach (var method in _interfaceMethods)
            {
                AddHandler(method.Name, MethodHandler);
            }
        }


        private async Task<RouteResponse> MethodHandler(string methodName, T instance, HttpContext context)
        {
            var method = _interfaceMethods.FirstOrDefault(x => x.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
            if (method == null)
            {
                throw new NullReferenceException(string.Format("Could not find method corresponding to {0}", methodName));
            }

            var contentType = SerializationHelper.GetContentType(context.Request);
            var response = new RouteResponse(contentType);

            if (_options.AuthorizationHandler != null && _options.AuthorizationScope == AuthorizationScope.AdHoc || _options.AuthorizationScope == AuthorizationScope.Required)
            {
                var hasAuthorizeAttribute = false;
                if (_options.AuthorizationScope == AuthorizationScope.AdHoc)
                {
                    //check if the T instance has it
                    var instanceType = instance.GetType();
                    hasAuthorizeAttribute = instanceType.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any();
                    if (!hasAuthorizeAttribute)
                    {
                        //check if the T instance method has it
                        var instanceMethod = instanceType.GetMethods().FirstOrDefault(x => x.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
                        hasAuthorizeAttribute = instanceMethod.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any();
                    }
                }
                if (hasAuthorizeAttribute || _options.AuthorizationScope == AuthorizationScope.Required)
                {
                    var isAuthorized = _options.AuthorizationHandler(methodName, instance, context);
                    if (!isAuthorized)
                    {
                        response.NotAuthorized = true;
                        return response;
                    }
                }
            }

            var parameterValues = new List<object>();
            var methodParameters = method.GetParameters();
            var serializer = Serializer.GetSerializerFor(contentType);

            if (methodParameters.Any())
            {
                object entity = null;
                if (methodParameters.Count() == 1)
                {
                    var greGenericMethod = _getRequestEntityMethod.MakeGenericMethod(methodParameters.First().ParameterType);
                    var task = (Task)greGenericMethod.Invoke(null, new object[] { context });
                    await task.ConfigureAwait(false);
                    var resultProperty = task.GetType().GetProperty("Result");
                    entity = resultProperty.GetValue(task);
                    parameterValues.Add(entity);
                }
                else
                {
                    var types = method.GetParameters().Select(x => x.ParameterType).ToArray();
                    var valueTupleType = GetTupleType(types);
                    var greGenericMethod = _getRequestEntityMethod.MakeGenericMethod(valueTupleType);
                    var task = (Task)greGenericMethod.Invoke(null, new object[] { context });
                    await task.ConfigureAwait(false);
                    var resultProperty = task.GetType().GetProperty("Result");
                    entity = resultProperty.GetValue(task);
                    parameterValues.AddRange(TupleToEnumerable(entity));
                }
            }

            var result = method.Invoke(instance, parameterValues.ToArray());
            var isTask = method.ReturnType == typeof(Task);

            if (method.ReturnType == typeof(Task))
            {
                await (Task)result;
                return response;
            }
            else if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                result = await (dynamic)result;
            }

            if (method.ReturnType != typeof(void))
            {
                if (result != null)
                {
                    response.Content = await serializer.SerializeAsync(result);
                }
                else
                {
                    response.Content = new byte[0];
                }
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

        public void AddHandler(string routeName, Func<string, T, HttpContext, Task<RouteResponse>> handler)
        {
            if (routeName == null)
            {
                throw new ArgumentNullException(nameof(routeName));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var key = routeName.Trim().ToLowerInvariant();
            if (_routeHandlers.ContainsKey(key))
            {
                throw new ApplicationException(string.Format("A handler for the route \"{0}\" has already been added.", routeName));
            }
            _routeHandlers.Add(key, handler);
        }

        public Func<string, T, HttpContext, Task<RouteResponse>> GetHandler(string routeName)
        {
            if (routeName == null)
            {
                throw new ArgumentNullException(nameof(routeName));
            }
            var key = routeName.Trim().ToLowerInvariant();
            return _routeHandlers[key];
        }

        private MethodInfo[] GetAllMethods(Type parentType)
        {
            var parentMethods = parentType.GetMethods();

            var typeFilterFunction = new Func<Type, Object, bool>((t, o) =>
            {
                // bypass filter and always return true, so we get all.
                return true;
            });

            var childInterfaces = parentType.FindInterfaces(new TypeFilter(typeFilterFunction), null);

            if (childInterfaces != null && childInterfaces.Length > 0)
            {
                List<MethodInfo> childInterfaceMethods = new List<MethodInfo>();

                foreach (var childInterface in childInterfaces.Where(o => o != null))
                {
                    var methods = childInterface.GetMethods();

                    if (methods != null && methods.Length > 0)
                    {
                        childInterfaceMethods.AddRange(methods);
                    }
                }

                if (childInterfaceMethods.Count > 0)
                {
                    return parentMethods.Union(childInterfaceMethods).ToArray();
                }
            }

            return parentMethods;
        }
    }
}
