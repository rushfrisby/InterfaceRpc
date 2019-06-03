namespace InterfaceRpc.Service
{
    public class RpcServiceOptions
    {
        public string Prefix { get; set; }

        public AuthorizationScope AuthorizationScope { get; set; } = AuthorizationScope.AdHoc;
    }

    public enum AuthorizationScope
    {
        None = 0,
        Required = 1,
        AdHoc = 2
    }
}
