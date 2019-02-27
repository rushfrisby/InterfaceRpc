using System;

namespace InterfaceRpc.Client
{
	public class RpcClientRequestInfo
	{
		public RpcClientRequestInfo()
		{
			Id = Guid.NewGuid();
		}

		public Guid Id { get; internal set; }

		public string BaseAddress { get; set; }

		public string MethodName { get; set; }

		public object[] Arguments { get; set; }
	}
}
