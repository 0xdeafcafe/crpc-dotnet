using System;
using System.IO;
using System.Threading.Tasks;
using Crpc.Exceptions;
using Crpc.Middleware;
using Crpc.Registration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Crpc.Test.Middleware
{
	public class AuthMiddlewareTests
	{
		private ILoggerFactory _loggerFactory;

		public AuthMiddlewareTests()
		{
			_loggerFactory = new NullLoggerFactory();
		}

		[Fact]
		public async Task TestNoAuthenticationTypeSet()
		{
			var options = new CrpcOptions();
			var middleware = new AuthMiddleware(_loggerFactory, Options.Create(options));
			var context = new DefaultHttpContext();
			
			context.Response.Body = new MemoryStream();

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			{
				await middleware.InvokeAsync(context, (ctx) => Task.CompletedTask);
			});

			Assert.Equal("Authentication type not set", ex.Message);
		}

		[Theory]
		[InlineData("gucci_key", "gucci_key", true)]
		[InlineData("gucci_key", "bad_key", false)]
		public async Task TestInternalAuth(string goodKey, string requestKey, bool valid)
		{
			var options = new CrpcOptions
			{
				InternalKeys = new string[]{ goodKey },
			};

			var middleware = new AuthMiddleware(_loggerFactory, Options.Create(options));
			var context = new DefaultHttpContext();

			middleware.SetAuthentication(AuthenticationType.AllowInternalAuthentication);
			context.Request.Headers.Add("Authorization", $"bearer {requestKey}");
			context.Response.Body = new MemoryStream();

			if (valid)
			{
				await middleware.InvokeAsync(context, (ctx) => Task.CompletedTask);

				return;
			}

			var ex = await Assert.ThrowsAsync<CrpcException>(async () =>
			{
				await middleware.InvokeAsync(context, (ctx) => Task.CompletedTask);
			});

			Assert.Equal(CrpcCodes.Unauthorized, ex.Message);
		}

		[Theory]
		[InlineData("gucci_key")]
		[InlineData("xxx")]
		[InlineData("rly_doesnt_matter")]
		public async Task TestUnsafeNoAuth(string key)
		{
			var options = new CrpcOptions
			{
				InternalKeys = new string[]{ key },
			};

			var middleware = new AuthMiddleware(_loggerFactory, Options.Create(options));
			var context = new DefaultHttpContext();

			middleware.SetAuthentication(AuthenticationType.UnsafeNoAuthentication);
			context.Request.Headers.Add("Authorization", $"bearer {key}");
			context.Response.Body = new MemoryStream();

			await middleware.InvokeAsync(context, (ctx) => Task.CompletedTask);
		}
	}
}
