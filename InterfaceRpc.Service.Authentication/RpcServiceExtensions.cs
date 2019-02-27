using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InterfaceRpc.Service.Authentication
{
	public static class RpcServiceExtensions
	{
		public static RpcService<T> AddAuthentication<T>(this RpcService<T> service, AuthenticationSettings settings) where T : class
		{
			return service.AddExtension(new RpcServiceExtension
			{
				PreHandleRequestAction = async context =>
				{
					var header = context.Request.Headers["Authorization"];
					if(header == null)
					{
						return settings.OnlySetUser ? false : await UnauthorizedResultAsync(context, "Authorization header not present");
					}

					var spot = header.IndexOf(' ');
					if(spot == -1)
					{
						return settings.OnlySetUser ? false : await UnauthorizedResultAsync(context, $"No scheme found in the Authorization header");
					}

					var requestScheme = header.Substring(0, spot);
					if(settings.Scheme != requestScheme)
					{
						return settings.OnlySetUser ? false : await UnauthorizedResultAsync(context, $"The scheme \"{settings.Scheme}\" was not found in the Authorization header");
					}
					if(header.Length <= spot + 1)
					{
						return settings.OnlySetUser ? false : await UnauthorizedResultAsync(context, $"{settings.Scheme} token is empty");
					}

					var token = header.Substring(spot + 1, header.Length - 1 - spot);

					IConfigurationManager<OpenIdConnectConfiguration> configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>($"{settings.Domain}/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
					OpenIdConnectConfiguration openIdConfig = configurationManager.GetConfigurationAsync(CancellationToken.None).Result;

					TokenValidationParameters validationParameters = new TokenValidationParameters
					{
						ValidIssuer = settings.Domain,
						ValidAudiences = new[] { settings.Audience },
						IssuerSigningKeys = openIdConfig.SigningKeys
					};

					JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
					var user = handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

					settings.SetUserAction?.Invoke(user);

					//var field = typeof(HttpListenerContext).GetField("<User>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
					//field.SetValue(context, user);

					return false;
				}
			});
		}

		private static async Task<bool> UnauthorizedResultAsync(HttpListenerContext context, string message)
		{
			var binMessage = Encoding.UTF8.GetBytes(message);
			context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
			context.Response.StatusDescription = "Unauthorized";
			context.Response.ContentType = "text/plain";
			context.Response.ContentLength64 = binMessage.Length;
			await context.Response.OutputStream.WriteAsync(binMessage, 0, binMessage.Length);
			return true;
		}
	}
}
