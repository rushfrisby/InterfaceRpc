using InterfaceRpc.Client;
using Newtonsoft.Json;
using System;

namespace InterfaceRpcDemoClient
{
	public static class RpcClientExtensions
	{
		public static RpcClientOptions AddConsoleLogger(this RpcClientOptions options)
		{
			options.Extensions.Add(new RpcClientExtension
			{
				PreSendRequestAction = y =>
				{
					Console.WriteLine($"Request: {y.Id}");
					Console.WriteLine($"POST {y.BaseAddress}/{y.MethodName}");
					var json = y.Arguments != null ? JsonConvert.SerializeObject(y.Arguments) : null;
					if (json != null)
					{
						Console.WriteLine($"Args: {json}");
					}
				},
				PostReceiveResponseAction = y =>
				{
					Console.WriteLine($"Response: {y.Id}");
					Console.WriteLine($"POST {y.BaseAddress}/{y.MethodName}");
					var json = y.Arguments != null ? JsonConvert.SerializeObject(y.Arguments) : null;
					if (json != null)
					{
						Console.WriteLine($"Args: {json}");
					}
					var j2 = y.Result != null ? JsonConvert.SerializeObject(y.Result) : null;
					if (j2 != null)
					{
						Console.WriteLine($"Result: {j2}");
					}
				}
			});
			return options;
		}
	}
}
