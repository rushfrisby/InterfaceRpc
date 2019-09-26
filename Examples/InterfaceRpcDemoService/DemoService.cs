using InterfaceRpcDemoShared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace InterfaceRpcDemoService
{
    public class DemoService : IDemoService
	{
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly int InstanceId;
        private static readonly Random _random = new Random();

        public DemoService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            InstanceId = _random.Next(int.MinValue, int.MaxValue);
        }

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

        [Authorize]
        public string GetUserName()
        {
            var firstNameClaim = _httpContextAccessor.HttpContext.User.FindFirst("FirstName");
            var lastNameClaim = _httpContextAccessor.HttpContext.User.FindFirst("LastName");

            var firstName = firstNameClaim != null && !string.IsNullOrWhiteSpace(firstNameClaim.Value) ? firstNameClaim.Value.Trim() : "First";
            var lastName = lastNameClaim != null && !string.IsNullOrWhiteSpace(lastNameClaim.Value) ? lastNameClaim.Value.Trim() : "Last";

            return $"{firstName} {lastName}";
        }

        public async Task<string> EchoAsync(string input)
        {
            await Task.Delay(1);
            return input;
        }

        public async Task DoNothing()
        {
            await Task.Delay(1);
        }
    }
}
