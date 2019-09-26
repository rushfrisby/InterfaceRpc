using Microsoft.AspNetCore.Http;
using System;

namespace InterfaceRpc.Service
{
    public class RpcServiceOptions<T>
    {
        public string Prefix { get; set; }

        public AuthorizationScope AuthorizationScope { get; set; } = AuthorizationScope.AdHoc;

        public Func<T> ServiceFactory { get; set; }

        public Func<string, T, HttpContext, bool> AuthorizationHandler { get; set; }
    }

    public enum AuthorizationScope
    {
        None = 0,
        Required = 1,
        AdHoc = 2
    }
}
