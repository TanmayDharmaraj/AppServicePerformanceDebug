using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace ASEPerfIssue.Services
{
	public class AzureGraphAuthenticationProvider : IAuthenticationProvider
	{
		private readonly IConfiguration _configuration;
		private readonly ClaimsPrincipal _principal;
		public AzureGraphAuthenticationProvider(IConfiguration configuration, IPrincipal principal)
		{
			_principal = principal as ClaimsPrincipal;
			_configuration = configuration;
		}
		public async Task AuthenticateRequestAsync(HttpRequestMessage request)
		{
			var onBehalfOfToken = await GetAccessTokenOnBehalfOfAsync(_principal.Claims, _configuration, "https://graph.microsoft.com/");
			request.Headers.Add("Authorization", "Bearer " + onBehalfOfToken);
		}

		private static async Task<string> GetAccessTokenOnBehalfOfAsync(IEnumerable<Claim> claims, IConfiguration configuration, string resource)
		{
			ClientCredential clientCred = new ClientCredential(configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"]);
			string tenantId = configuration["AzureAd:TenantId"];

			Claim accessToken = claims.FirstOrDefault(p => p.Type == "access_token");

			string tenantClaim = "http://schemas.microsoft.com/identity/claims/tenantid";
			if (claims.Any(p => p.Type == tenantClaim))
			{
				tenantId = claims.First(p => p.Type == tenantClaim)?.Value;
			}

			string configAuthority = configuration["AzureAd:Instance"];
			string authority = $"{configAuthority.TrimEnd('/')}/{tenantId}";

			/* TODO: Implement a Redis token cache or token will be fetched from AAD every time.
			 * E.G. https://blogs.msdn.microsoft.com/mrochon/2016/09/19/using-redis-as-adal-token-cache/
			 * TokenCache tokenCache = new RedisTokenCache(ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier)?.Value);
			 * AuthenticationContext authContext = new AuthenticationContext(authority, tokenCache);
			 */

			AuthenticationContext authContext = new AuthenticationContext(authority);
			Claim grantEmail = claims.FirstOrDefault(p => p.Type == ClaimTypes.Upn);
			if (grantEmail == null)
			{
				grantEmail = claims.FirstOrDefault(p => p.Type == ClaimTypes.Email);
			}

			// TODO: Throw is claim is null. Can be null if comes from allow anonymous.
			UserAssertion userAssertion = new UserAssertion(
				accessToken.Value,
				"urn:ietf:params:oauth:grant-type:jwt-bearer",
				grantEmail?.Value);

			// TODO: May want to handle retry in case of transient errors
			AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred, userAssertion);
			return result?.AccessToken;
		}
	}
}
