using InterfaceRpc.Client;
using InterfaceRpcDemoShared;
using SerializerDotNet;
using System;
using System.Diagnostics;

namespace InterfaceRpcDemoClient
{
	class Program
	{
		static void Main(string[] args)
		{
			var client = RpcClient<IDemoService>.Create(new JsonSerializer(), "http://localhost:6000/");

			Console.WriteLine("RPC Demo Client is waiting - Press any key to begin.");
			Console.ReadKey();

			var echo = client.Echo("hello world");
			Console.WriteLine($"Echo: {echo}");

			var now = client.GetDateTime();
			Console.WriteLine($"Now: {now}");

			var sw = new Stopwatch();
			sw.Start();
			client.Wait(3000);
			sw.Stop();
			Console.WriteLine($"Waited: {sw.Elapsed}");

			var person = client.EchoPerson(new Person { Id = 1, FirstName = "Rush", LastName = "Frisby" });
			Console.WriteLine($"Person: Id={person.Id}, FName={person.FirstName}, LName={person.LastName}");

			Console.WriteLine("Done. Press any key to exit.");
			Console.ReadKey();
		}
	}
}
