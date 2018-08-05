# InterfaceRpc
Turn your interface into an RPC service!

Given the interface:
```csharp
public interface IEchoService
{
  string Echo(string echo);
}
```
You can create and run an RPC-HTTP based service, like so:
```csharp
var svc = new RpcService<IEchoService>(new EchoService());
svc.Start();
```
Then make HTTP POSTs to `http://localhost:6000/Echo` (domain and port are configurable)

You can make a client to do this for you, like so:
```csharp
var client = RpcClient<IEchoService>.Create("http://localhost:6000/");
var result = client.Echo("hello");
```

You can use any of the serializers available in [SerializerDotNet](https://www.nuget.org/packages/SerializerDotNet) - currently JSON and Protobuf
