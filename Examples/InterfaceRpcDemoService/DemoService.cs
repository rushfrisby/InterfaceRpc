using InterfaceRpcDemoShared;
using System;
using System.Threading.Tasks;

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

		public string GetAge(string name, int age)
		{
			return $"{name} is {age} years old";
		}

		public DateTime GetDateTime()
		{
			return DateTime.Now;
		}

		public string GetPersonAge(Person person, int age)
		{
			return $"{person.FirstName} is {age} years old";
		}

        public void Wait(int milliseconds)
        {
            Task.Delay(milliseconds).Wait();
		}
	}
}
