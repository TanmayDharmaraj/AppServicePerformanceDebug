using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using ASEPerfIssue.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ASEPerfIssue
{
	public static class AzureAdServiceCollectionExtensions
	{
		public static AuthenticationBuilder AddAzureAdBearer(this AuthenticationBuilder builder)
			=> builder.AddAzureAdBearer(_ => { });

		public static AuthenticationBuilder AddAzureAdBearer(this AuthenticationBuilder builder, Action<AzureAdOptions> configureOptions)
		{
			builder.Services.Configure(configureOptions);
			builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureAzureOptions>();
			builder.AddJwtBearer();
			return builder;
		}

		private class ConfigureAzureOptions : IConfigureNamedOptions<JwtBearerOptions>
		{
			private readonly AzureAdOptions _azureOptions;

			public ConfigureAzureOptions(IOptions<AzureAdOptions> azureOptions)
			{
				_azureOptions = azureOptions.Value;
			}

			public void Configure(string name, JwtBearerOptions options)
			{
				options.Audience = _azureOptions.ClientId;
				options.Authority = $"{_azureOptions.Instance.TrimEnd('/')}/{_azureOptions.TenantId}";

				options.SaveToken = true;
				var events = new JwtBearerEvents
				{
					OnTokenValidated = context =>
					{
						// Add the access_token as a claim, as we may actually need it
						var accessToken = context.SecurityToken as JwtSecurityToken;
						if (accessToken != null)
						{
							var identity = context.Principal.Identity as ClaimsIdentity;
							if (identity != null)
							{
								identity.AddClaim(new Claim("access_token", accessToken.RawData));
							}
						}

						return Task.CompletedTask;
					},
					OnAuthenticationFailed = context =>
					{
						System.Diagnostics.Debug.WriteLine($"Token error: {context.Exception.Message}");
						return Task.FromException(context.Exception);
					},
				};
				options.Events = events;
			}

			public void Configure(JwtBearerOptions options)
			{
				Configure(Options.DefaultName, options);
			}
		}
	}
}
