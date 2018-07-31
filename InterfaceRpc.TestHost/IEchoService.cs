using System;

namespace InterfaceRpc.TestHost
{
	public interface IEchoService
	{
		string Echo(string input);

		DateTime GetDateTime();

		void Wait(int milliseconds);
	}
}
