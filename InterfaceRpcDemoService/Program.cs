using InterfaceRpc.Service;
using InterfaceRpcDemoShared;
using System;

namespace InterfaceRpcDemoService
{
	class Program
	{
		public static void Main(string[] args)
		{
			var rpcService = new RpcService<IDemoService>(new DemoService());
			rpcService.Start();

			Console.WriteLine("RPC Demo Service is running - Press any key to stop.");
			Console.ReadKey();

			rpcService.Stop();
		}
	}
}
