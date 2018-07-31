using System;

namespace InterfaceRpc.TestHost
{
	class Program
	{
		public static void Main(string[] args)
		{
			var rpcService = new RpcService<IEchoService>(new EchoService());
			rpcService.Start();

			Console.WriteLine("RPC Service is running - Press any key to stop.");
			Console.Read();

			rpcService.Stop();
		}
	}
}
