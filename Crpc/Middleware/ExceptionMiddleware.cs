using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sentry;

namespace Crpc.Middleware
{
	public sealed class ExceptionMiddleware : IMiddleware
	{
		private readonly ILogger _logger;
		private readonly IHub _sentry;
		private static JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
		};

		public ExceptionMiddleware(ILoggerFactory loggerFactory, IHub sentry)
		{
			if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

			_logger = loggerFactory.CreateLogger(nameof(ExceptionMiddleware));
			_sentry = sentry;
		}

		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
		{
			try
			{
				await next.Invoke(context);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.Message);
				_sentry.CaptureException(ex);

				var exception = ex as CrpcException;
				if (!(ex is CrpcException))
				{
					// TODO(0xdeafcafe): How should this be done?
					exception = new CrpcException(CrpcCodes.Unknown);
				}

				var json = JsonConvert.SerializeObject(exception, _jsonSerializerSettings);

				context.Response.StatusCode = exception.StatusCode();
				context.Response.ContentType = "application/json; charset=utf-8";
				await context.Response.WriteAsync(json);
			}
		}
	}
}
