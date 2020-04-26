using System;
using System.Net;
using System.Threading.Tasks;
using Crpc.Middleware;
using Crpc.Registration;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
	public static class BuilderExtensions
	{
		public static IApplicationBuilder UseCrpc<T>(this IApplicationBuilder app, PathString baseUrl, Action<CrpcRegistrationOptions<T>, T> opts)
			where T : class
		{
			if (app == null) throw new ArgumentNullException(nameof(app));
			if (opts == null) throw new ArgumentNullException(nameof(opts));

			// Set the registration options on the CrpcMiddleware singleton
			var crpcMiddleware = app.ApplicationServices.GetService(typeof(CrpcMiddleware<T>)) as CrpcMiddleware<T>;
			var options = crpcMiddleware.SetRegistrationOptions(opts);

			// Set the authentication type on the AuthMiddleware singleton
			var authMiddleware = app.ApplicationServices.GetService(typeof(AuthMiddleware)) as AuthMiddleware;
			authMiddleware.SetAuthentication(options.Authentication);

			app.Map(baseUrl, builder =>
			{
				builder.UseMiddleware<ExceptionMiddleware>();
				builder.UseMiddleware<CorsMiddleware>();
				builder.UseMiddleware<AuthMiddleware>();
				builder.UseMiddleware<CrpcMiddleware<T>>();
			});

			return app;
		}

		public static IApplicationBuilder UseCrpcHealthCheck(this IApplicationBuilder app)
		{
			#pragma warning disable CS1998
			app.Map("/system/health", builder => {
				builder.Run(async context =>
				{
					context.Response.StatusCode = (int)HttpStatusCode.NoContent;
				});
			});
			#pragma warning restore CS1998

			return app;
		}
	}
}
