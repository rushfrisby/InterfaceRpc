using System;
using System.Security.Principal;

namespace InterfaceRpc.Service.Authentication
{
	public class AuthenticationSettings
	{
		public string Scheme { get; set; }

		public string Domain { get; set; }

		public string Audience { get; set; }

		public bool OnlySetUser { get; set; }

		public Action<IPrincipal> SetUserAction { get; set; }
	}
}
