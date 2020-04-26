using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Crpc.Exceptions;
using Crpc.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using NSubstitute;
using Sentry;
using Xunit;

namespace Crpc.Test.Middleware
{
	public class ExceptionMiddlewareTests
	{
		private ILoggerFactory _loggerFactory;
		private IHub _hub;

		public ExceptionMiddlewareTests()
		{
			_loggerFactory = new NullLoggerFactory();
			_hub = Substitute.For<IHub>();
		}

		[Fact]
		public async Task TestNoExceptionWithoutSentry()
		{
			var middleware = new ExceptionMiddleware(_loggerFactory, null);
			var context = new DefaultHttpContext();

			await middleware.InvokeAsync(context, (ctx) =>
			{
				throw new Exception();
			});
		}

		[Fact]
		public async Task TestUnkownReplacement()
		{
			var middleware = new ExceptionMiddleware(_loggerFactory, null);
			var context = new DefaultHttpContext();
			context.Response.Body = new MemoryStream();

			await middleware.InvokeAsync(context, (ctx) => throw new Exception());

			context.Response.Body.Seek(0, SeekOrigin.Begin);

			var reader = new StreamReader(context.Response.Body);
			var body = reader.ReadToEnd();
			var exception = JsonConvert.DeserializeObject<CrpcException>(body);

			Assert.Equal(CrpcCodes.Unknown, exception.Message);
			Assert.Equal((int) HttpStatusCode.InternalServerError, context.Response.StatusCode);
		}

		[Fact]
		public async Task TestGenericCrpcException()
		{
			var middleware = new ExceptionMiddleware(_loggerFactory, null);
			var context = new DefaultHttpContext();
			var thrownException = new CrpcException(CrpcCodes.ValidationFailed);

			context.Response.Body = new MemoryStream();

			await middleware.InvokeAsync(context, (ctx) => throw thrownException);

			context.Response.Body.Seek(0, SeekOrigin.Begin);

			var reader = new StreamReader(context.Response.Body);
			var body = reader.ReadToEnd();
			var exception = JsonConvert.DeserializeObject<CrpcException>(body);

			Assert.Equal(CrpcCodes.ValidationFailed, exception.Message);
			Assert.Equal(thrownException.StatusCode(), (int)context.Response.StatusCode);
		}
	}
}
