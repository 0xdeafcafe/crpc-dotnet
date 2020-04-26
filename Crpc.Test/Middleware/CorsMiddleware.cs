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
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Crpc.Test.Middleware
{
	public class CorsMiddlewareTests
	{
		private ILoggerFactory _loggerFactory;

		public CorsMiddlewareTests()
		{
			_loggerFactory = new NullLoggerFactory();
		}

		[Fact]
		public async Task TestCorsHeadersAreSet()
		{
			var middleware = new CorsMiddleware(_loggerFactory);
			var context = new DefaultHttpContext();

			context.Response.Body = new MemoryStream();

			await middleware.InvokeAsync(context, (ctx) =>
			{
				var headers = new string[] { "Authorization", "Content-Type", "Accept" };

				Assert.Equal("*", GetFirstHeaderValue(ctx.Response.Headers, "Access-Control-Allow-Origin"));
				Assert.Equal(headers, GetHeaderValues(ctx.Response.Headers, "Access-Control-Allow-Headers"));
				Assert.Equal("POST", GetFirstHeaderValue(ctx.Response.Headers, "Access-Control-Allow-Methods"));

				Console.WriteLine(GetFirstHeaderValue(ctx.Response.Headers, "Access-Control-Allow-Headers"));

				return Task.CompletedTask;
			});
		}

		[Theory]
		[InlineData("OPTIONS", false)]
		[InlineData("POST", true)]
		public async Task TestSkipsOnOptions(string method, bool shouldBeCalled)
		{
			var middleware = new CorsMiddleware(_loggerFactory);
			var context = new DefaultHttpContext();
			var called = false;

			context.Request.Method = method;
			context.Response.Body = new MemoryStream();

			await middleware.InvokeAsync(context, (ctx) =>
			{
				called = true;

				return Task.CompletedTask;
			});

			Assert.Equal(shouldBeCalled, called);
		}

		private StringValues GetHeaderValues(IHeaderDictionary headers, string header)
		{
			headers.TryGetValue(header, out var values);

			return values;
		}

		private string GetFirstHeaderValue(IHeaderDictionary headers, string header)
		{
			var values = GetHeaderValues(headers, header);

			return values[0];
		}
	}
}
