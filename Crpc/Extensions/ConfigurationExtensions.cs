using System;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Microsoft.Extensions.Configuration
{
	public static class ConfigurationExtensions
	{
		public static IConfigurationBuilder AddCrpcConfig(this IConfigurationBuilder builder, IHostEnvironment environment)
		{
			builder
				.SetBasePath(environment.ContentRootPath)
				.AddJsonFile("appsettings.json", true)
				.AddJsonFile($"appsettings.{environment.EnvironmentName}.json", true);

			// Read config environment variable, if it exists
			var configEnvVariable = Environment.GetEnvironmentVariable("CONFIG");
			if (configEnvVariable != null)
				builder.AddJsonObject(JsonConvert.DeserializeObject(configEnvVariable));

			return builder;
		}
	}
}
