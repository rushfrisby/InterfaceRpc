using System;
using System.Net;
using System.Threading.Tasks;

namespace InterfaceRpc.Service
{
	public class RpcServiceExtension
	{
		public Func<HttpListenerContext, Task<bool>> PreHandleRequestAction { get; set; }

		public Func<HttpListenerContext, Task> PostHandleRequestAction { get; set; }
	}
}
