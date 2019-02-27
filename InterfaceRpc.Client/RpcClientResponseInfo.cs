namespace InterfaceRpc.Client
{
	public class RpcClientResponseInfo : RpcClientRequestInfo
	{
		public RpcClientResponseInfo(RpcClientRequestInfo requestInfo)
		{
			Id = requestInfo.Id;
			BaseAddress = requestInfo.BaseAddress;
			MethodName = requestInfo.MethodName;
			Arguments = requestInfo.Arguments;
		}

		public object Result { get; set; }
	}
}
