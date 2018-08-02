using System;

namespace InterfaceRpcDemoShared
{
	public interface IDemoService
	{
		string Echo(string input);

		Person EchoPerson(Person person);

		DateTime GetDateTime();

		void Wait(int milliseconds);

		string GetAge(string name, int age);

		string GetPersonAge(Person person, int age);
	}
}
