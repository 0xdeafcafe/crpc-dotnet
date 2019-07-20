using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Crpc
{
	public class CrpcHost
	{
		internal CrpcHost() { }

		public static IWebHostBuilder CreateCrpcHost<T>()
			where T : class
		{
			return new WebHostBuilder()
				.UseKestrel()
				.UseContentRoot(Directory.GetCurrentDirectory())
				.ConfigureAppConfiguration((hostingContext, config) =>
				{
					config.AddCrpcConfig(hostingContext.HostingEnvironment);
				})
				.ConfigureServices((hostingContext, services) =>
				{
					// TODO(0xdeafcafe): Should this be injected? 🤔
					var hostingEnvironment = hostingContext.HostingEnvironment;
					var env = hostingEnvironment.EnvironmentName;

					if (hostingEnvironment.IsProduction()) env = "prod";
					if (hostingEnvironment.IsDevelopment()) env = "dev";

					// NOTE(0xdeafcafe): Should create an injectable singleton for KSUID?
					Ksuid.Ksuid.Environment = env;
				})
				.ConfigureLogging((hostingContext, logging) =>
				{
					logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
					logging.AddConsole();
					logging.AddDebug();
					logging.AddEventSourceLogger();
				})
				.UseStartup<T>()
				.UseSentry();
		}
	}
}
