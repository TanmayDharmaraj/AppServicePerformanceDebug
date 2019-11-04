using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ASEPerfIssue
{
	class Program
	{
		[Obsolete]
		static void Main(string[] args)
		{
			CreateWebHostBuilder(args).Build().Run();
		}

		[Obsolete]
		public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.ConfigureAppConfiguration((webHostBuilderContext, configurationBuilder) =>
				{
					var builtConfig = configurationBuilder.Build();
					configurationBuilder.SetBasePath(webHostBuilderContext.HostingEnvironment.ContentRootPath);
					configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
				})
				.UseApplicationInsights()
				.ConfigureLogging((hostingContext, logging) =>
				{
					logging.AddEventSourceLogger();
				})
				.UseStartup<Startup>();
	}
}
