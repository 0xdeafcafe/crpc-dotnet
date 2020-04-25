using System;
using Crpc;
using Crpc.Middleware;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class ServicesExtensions
	{
		public static IServiceCollection AddCrpc<TA, TS>(this IServiceCollection services, Action<CrpcOptions> configureOptions)
			where TA : class
			where TS : class
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));

			if (configureOptions == null)
				throw new ArgumentNullException(nameof(configureOptions));

			// Create the application and RPC server
			services.AddSingleton<TA>();
			services.AddSingleton<TS>();

			services.Configure<CrpcOptions>(configureOptions);
			services.AddScoped<ExceptionMiddleware>();
			services.AddSingleton<AuthMiddleware>();
			services.AddSingleton<CorsMiddleware>();
			services.AddSingleton<CrpcMiddleware<TS>>();

			return services;
		}
	}
}
