using System;
using System.Linq;
using System.Threading.Tasks;
using Crpc.Exceptions;
using Crpc.Registration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Crpc.Middleware
{
	public sealed class AuthMiddleware : IMiddleware
	{
		private readonly ILogger _logger;
		private Nullable<AuthenticationType> _authenticationType;
		private string[] _internalKeys;

		public AuthMiddleware(ILoggerFactory loggerFactory, IOptions<CrpcOptions> options)
		{
			if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
			if (options == null) throw new ArgumentNullException(nameof(options));

			_logger = loggerFactory.CreateLogger(nameof(AuthMiddleware));
			_internalKeys = options.Value.InternalKeys;
		}

		internal void SetAuthentication(AuthenticationType type)
		{
			if (_authenticationType.HasValue)
				throw new InvalidOperationException("authentication type already set");

			_authenticationType = type;
		}

		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
		{
			if (!_authenticationType.HasValue)
				throw new InvalidOperationException("Authentication type not set");

			switch(_authenticationType.Value)
			{
				case AuthenticationType.UnsafeNoAuthentication:
					break;

				case AuthenticationType.AllowInternalAuthentication:
					var hasHeader = context.Request.Headers.TryGetValue("Authorization", out var headers);

					if (!hasHeader)
						throw new CrpcException(CrpcCodes.Unauthorized);

					var header = headers[0];

					if (!_internalKeys.Any(k => $"bearer {k}" == header))
						throw new CrpcException(CrpcCodes.Unauthorized);
					break;

				default:
					throw new InvalidOperationException("unknown authentication type");
			}

			await next.Invoke(context);
		}
	}
}
