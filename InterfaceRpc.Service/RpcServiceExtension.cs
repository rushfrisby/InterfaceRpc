using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace InterfaceRpc.Service
{
	public class RpcServiceExtension
	{
		public Func<HttpContext, Task<bool>> PreHandleRequestAction { get; set; }

		public Func<HttpContext, Task> PostHandleRequestAction { get; set; }

		public Func<string, Exception, Task> InternalServerErrorAction { get; set; }
	}
}
