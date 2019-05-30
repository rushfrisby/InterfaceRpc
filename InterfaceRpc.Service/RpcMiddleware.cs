using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace InterfaceRpc.Service
{

    public class RpcMiddleware<T> where T : class
    {
        private readonly RequestDelegate _next;
        private readonly RpcServiceOptions _options;
        private readonly T _instance;
        private static readonly IDictionary<Type, Tuple<RpcServiceOptions, object>> _routeManagers = new Dictionary<Type, Tuple<RpcServiceOptions, object>>();
        private readonly RouteManager<T> _routeManager;

        public RpcMiddleware(RequestDelegate next, RpcServiceOptions options, T instance)
        {
            _next = next;
            _options = options ?? new RpcServiceOptions();
            _instance = instance ?? throw new ArgumentNullException($"An instance of {typeof(T).Name} is required");

            if(_routeManagers.Values.Any(x => string.Equals(x.Item1.Prefix, options.Prefix, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ApplicationException($"There is already an RPC service which handles requests with the prefix \"{options.Prefix ?? "null"}\"");
            }

            var instanceType = typeof(T);
            if(!_routeManagers.ContainsKey(instanceType))
            {
                _routeManagers.Add(instanceType, new Tuple<RpcServiceOptions, object>(options, new RouteManager<T>(options)));
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

            if(!string.Equals(prefix, _options.Prefix, StringComparison.OrdinalIgnoreCase))
            {
                await _next.Invoke(context);
                return;
            }

            if(!_routeManager.ContainsHandler(methodName))
            {
                await _next.Invoke(context);
                return;
            }

            var handler = _routeManager.GetHandler(methodName);

            try
            {
                var response = await handler(methodName, _instance, context);

                context.Response.StatusCode = 200;
                context.Response.ContentType = response.ContentType;
                context.Response.ContentLength = response.Content.Length;
                await context.Response.Body.WriteAsync(response.Content, 0, response.Content.Length);
            }
            catch(Exception ex)
            {
                var message = ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace;
                var data = Encoding.UTF8.GetBytes(message);
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain";
                context.Response.ContentLength = data.Length;
                await context.Response.Body.WriteAsync(data, 0, data.Length);
            }
        }
    }

    public static class RpcMiddlewareExtensions
    {
        public static IApplicationBuilder UseRpcService<T>(this IApplicationBuilder builder) where T : class
        {
            return builder.UseMiddleware<RpcMiddleware<T>>(new RpcServiceOptions());
        }

        public static IApplicationBuilder UseRpcService<T>(this IApplicationBuilder builder, RpcServiceOptions options) where T : class
        {
            return builder.UseMiddleware<RpcMiddleware<T>>(options);
        }

        public static IApplicationBuilder UseRpcService<T>(this IApplicationBuilder builder, Action<RpcServiceOptions> configureOptions) where T : class
        {
            var options = new RpcServiceOptions();
            configureOptions?.Invoke(options);
            return builder.UseMiddleware<RpcMiddleware<T>>(options);
        }
    }
}
