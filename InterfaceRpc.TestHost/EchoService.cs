using System;
using System.Threading;

namespace InterfaceRpc.TestHost
{
	public class EchoService : IEchoService
	{
		public string Echo(string input)
		{
			return input;
		}

		public DateTime GetDateTime()
		{
			return DateTime.Now;
		}

		public void Wait(int milliseconds)
		{
			Thread.Sleep(milliseconds);
		}
	}
}
