using System;

namespace InterfaceRpc.Client
{
	public class RpcClientExtension
	{
		public Action<RpcClientRequestInfo> PreSendRequestAction { get; set; }

		public Action<RpcClientResponseInfo> PostReceiveResponseAction { get; set; }
	}
}
