using InterfaceRpcDemoShared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;

namespace InterfaceRpcDemoService
{
    public class DemoService : IDemoService
	{
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DemoService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
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
	}
}
