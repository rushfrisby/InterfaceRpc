using InterfaceRpc.Service;
using System;

namespace InterfaceRpcDemoService
{
	public static class RpcServiceExtensions
	{
		public static RpcService<T> AddUrlConsoleLogger<T>(this RpcService<T> service) where T : class
		{
			return service.AddExtension(new RpcServiceExtension
			{
				PreHandleRequestAction = async y =>
				{
					Console.WriteLine("PRE - " + y.Request.Url);
					return false;
				},
				PostHandleRequestAction = async y =>
				{
					Console.WriteLine("POST - " + y.Request.Url);
				}
			});
		}

		public static RpcService<T> AddAuthentication<T>(this RpcService<T> service) where T : class
		{
			return service.AddExtension(new RpcServiceExtension
			{
				PreHandleRequestAction = async y =>
				{

					return false;
				}
			});
		}
	}
}
