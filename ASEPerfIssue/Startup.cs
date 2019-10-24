using ASEPerfIssue.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System.Security.Principal;

namespace ASEPerfIssue
{
	internal class Startup
	{
		public IConfiguration Configuration { get; }
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			app.UseHsts();
			app.UseAuthentication();
			app.UseHttpsRedirection();
			app.UseMvc();
		}

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
			services.AddTransient<IPrincipal>(provider => provider.GetService<IHttpContextAccessor>().HttpContext.User);
			services.AddTransient<IAuthenticationProvider, AzureGraphAuthenticationProvider>();

			services.AddAuthentication(sharedOptions =>
			{
				sharedOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			}).AddAzureAdBearer(options => Configuration.Bind("AzureAd", options));

			var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
			services.AddMvc(options =>
			{
				options.Filters.Add(new AuthorizeFilter(policy));
			}).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
		}
	}
}