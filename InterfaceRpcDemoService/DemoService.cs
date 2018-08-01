using InterfaceRpcDemoShared;
using System;
using System.Threading;

namespace InterfaceRpcDemoService
{
	public class DemoService : IDemoService
	{
		public string Echo(string input)
		{
			return input;
		}

		public Person EchoPerson(Person person)
		{
			return person;
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
