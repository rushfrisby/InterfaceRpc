using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InterfaceRpc.Service
{

    public class RpcMiddleware<T> where T : class
    {
        private readonly RequestDelegate _next;
        private readonly RpcServiceOptions<T> _options;
        private static readonly IDictionary<Type, Tuple<RpcServiceOptions<T>, object>> _routeManagers = new Dictionary<Type, Tuple<RpcServiceOptions<T>, object>>();
        private readonly RouteManager<T> _routeManager;

        public RpcMiddleware(RequestDelegate next, RpcServiceOptions<T> options)
        {
            _next = next;
            _options = options ?? new RpcServiceOptions<T>();

            if (_routeManagers.Values.Any(x => string.Equals(x.Item1.Prefix, options.Prefix, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ApplicationException($"There is already an RPC service which handles requests with the prefix \"{options.Prefix ?? "null"}\"");
            }

            var instanceType = typeof(T);
            if (!_routeManagers.ContainsKey(instanceType))
            {
                _routeManagers.Add(instanceType, new Tuple<RpcServiceOptions<T>, object>(options, new RouteManager<T>(options)));
            }

            var tuple = _routeManagers[instanceType];
            _routeManager = (RouteManager<T>)tuple.Item2;
        }

        public async Task Invoke(HttpContext context)
        {
            string prefix = null;
            string methodName = null;

            var path = context.Request.Path.Value.Split('/');

            if (path.Length > 2)
            {
                prefix = path[1];
                methodName = path[2];
            }
            else if (path.Length > 1)
            {
                methodName = path[1];
            }

            if (!string.Equals(prefix, _options.Prefix, StringComparison.OrdinalIgnoreCase))
            {
                await _next.Invoke(context);
                return;
            }

            if (!_routeManager.ContainsHandler(methodName))
            {
                await _next.Invoke(context);
                return;
            }

            var handler = _routeManager.GetHandler(methodName);
            var instance = _options?.ServiceFactory();

            var response = await handler(methodName, instance, context);

            if (response.NotAuthorized)
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = response.ContentType;
            }
            else
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = response.ContentType;
                var length = response.Content != null ? response.Content.Length : 0;
                context.Response.ContentLength = length;
                if (length > 0)
                {
                    await context.Response.Body.WriteAsync(response.Content, 0, length);
                }
            }
        }
    }

    public static class RpcMiddlewareExtensions
    {
        public static IApplicationBuilder UseRpcService<T>(this IApplicationBuilder builder) where T : class
        {
            return builder.UseMiddleware<RpcMiddleware<T>>(new RpcServiceOptions<T>());
        }

        public static IApplicationBuilder UseRpcService<T>(this IApplicationBuilder builder, RpcServiceOptions<T> options) where T : class
        {
            return builder.UseMiddleware<RpcMiddleware<T>>(options);
        }

        public static IApplicationBuilder UseRpcService<T>(this IApplicationBuilder builder, Action<RpcServiceOptions<T>> configureOptions) where T : class
        {
            var options = new RpcServiceOptions<T>();
            configureOptions?.Invoke(options);
            return builder.UseMiddleware<RpcMiddleware<T>>(options);
        }
    }
}
