using InterfaceRpc.Service;
using InterfaceRpcDemoShared;
using System;
using InterfaceRpc.Service.Authentication;
using System.Security.Principal;

namespace InterfaceRpcDemoService
{
	class Program
	{
		private static IPrincipal _user;

		public static void Main(string[] args)
		{
			var rpcService = new RpcService<IDemoService>(new DemoService());

			rpcService
				.AddUrlConsoleLogger()
				.AddAuthentication(new AuthenticationSettings
				{
					Domain = "",
					Audience = "",
					Scheme = "",
					OnlySetUser = true,
					SetUserAction = x => _user = x
				})
				.Start();

			Console.WriteLine("RPC Demo Service is running - Press any key to stop.");
			Console.ReadKey();

			rpcService.Stop();
		}
	}
}
