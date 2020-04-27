using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Crpc
{
	public class CrpcHost
	{
		internal CrpcHost() { }

		public static IHostBuilder CreateCrpcHost<TS>(object defaultConfig = null)
			where TS : class
		{
			return new HostBuilder()
				.ConfigureWebHostDefaults(builder =>
				{
					builder.UseKestrel();
					builder.UseStartup<TS>();
					builder.UseSentry();

					builder.ConfigureKestrel(o =>
					{
						// NOTE(afr): This should be removed in the future
						o.AllowSynchronousIO = true;
					});
				})
				.UseContentRoot(Directory.GetCurrentDirectory())
				.ConfigureAppConfiguration((hostingContext, config) =>
				{
					if (defaultConfig != null)
					{
						var jsonStr = JsonConvert.SerializeObject(defaultConfig);

						config.AddJsonObject(JsonConvert.DeserializeObject(jsonStr));
					}

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
				});
		}
	}
}
