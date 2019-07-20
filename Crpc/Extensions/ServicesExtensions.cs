using System;
using System.Reflection;

using Crpc;
using Crpc.Middleware;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class ServicesExtensions
	{
		public static IServiceCollection AddCrpc(this IServiceCollection services, Action<CrpcOptions> configureOptions)
		{
			if (services == null) throw new ArgumentNullException(nameof(services));
			if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

			services.Configure<CrpcOptions>(configureOptions);
			services.AddScoped<ExceptionMiddleware>();
			services.AddSingleton<AuthMiddleware>();
			services.AddSingleton<CorsMiddleware>();
			services.AddSingleton<CrpcMiddleware>();

			return services;
		}
	}
}
