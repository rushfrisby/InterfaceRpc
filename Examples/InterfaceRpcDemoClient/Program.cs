using InterfaceRpc.Client;
using InterfaceRpcDemoShared;
using SerializerDotNet;
using System;

namespace InterfaceRpcDemoClient
{
	class Program
	{
		static void Main(string[] args)
		{
			var options = new RpcClientOptions
			{
				BaseAddress = "https://localhost:44318/",
				//Serializer = new ProtobufSerializer()
			}
			.AddConsoleLogger();

			var client = RpcClient<IDemoService>.Create(options);

            RpcClient.SetAuthorization(client, "Bearer", "xyz...");

            Console.WriteLine("RPC Demo Client is waiting - Press any key to begin.");
			Console.ReadKey();

			Console.WriteLine(client.GetAge("Rush", 37));
			Console.WriteLine(client.GetPersonAge(new Person { Id=1, FirstName="Rush", LastName="Frisby" }, 37));

			var echo = client.Echo("hello world");
			Console.WriteLine($"Echo: {echo}");

			var now = client.GetDateTime();
			Console.WriteLine($"Now: {now}");

            var userName = client.GetUserName();
            Console.WriteLine($"User Name: {userName}");

            var person = client.EchoPerson(new Person { Id = 1, FirstName = "Rush", LastName = "Frisby" });
            Console.WriteLine($"Person: Id={person.Id}, FName={person.FirstName}, LName={person.LastName}");

			Console.WriteLine("Done. Press any key to exit.");
			Console.ReadKey();
		}
	}
}
