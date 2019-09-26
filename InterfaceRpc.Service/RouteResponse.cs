namespace InterfaceRpc.Service
{
    internal class RouteResponse
    {
        public RouteResponse()
        {
        }

        public RouteResponse(string contentType)
        {
            ContentType = contentType;
        }

        public string ContentType { get; set; }

        public byte[] Content { get; set; }

        public bool NotAuthorized { get; set; }
    }
}
