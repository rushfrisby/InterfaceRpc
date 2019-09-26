using SerializerDotNet;
using System;
using System.Collections.Generic;

namespace InterfaceRpc.Client
{
	public class RpcClientOptions
	{
		public string BaseAddress { get; set; }

		public ISerializer Serializer { get; set; } = new JsonSerializer();

		public List<RpcClientExtension> Extensions { get; set; } = new List<RpcClientExtension>();

        public Func<RpcClientAuthorizationHeader> SetAuthorizationHeaderAction { get; set; }
    }
}
