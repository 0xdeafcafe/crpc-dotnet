using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Crpc.Exceptions;
using Crpc.Middleware;
using Crpc.Registration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using NSubstitute;
using Sentry;
using Xunit;

namespace Crpc.Tests.Middleware
{
	public class CrpcMiddlewareTests
	{
		private ILoggerFactory _loggerFactory;
		private IHub _hub;

		public CrpcMiddlewareTests()
		{
			_loggerFactory = new NullLoggerFactory();
			_hub = Substitute.For<IHub>();
		}

		[Fact]
		public void TestRegistrationOptions()
		{
			var serviceProvider = CreateServiceProvider();
			var middleware = new CrpcMiddleware<TRS>(serviceProvider, _loggerFactory);
			var registrationAction = CreateGeneralRegistrationAction();

			middleware.SetRegistrationOptions(registrationAction);
		}

		[Fact]
		public void TestPreventionOfDoubleRegistration()
		{
			var serviceProvider = CreateServiceProvider();
			var middleware = new CrpcMiddleware<TRS>(serviceProvider, _loggerFactory);
			var registrationAction = CreateGeneralRegistrationAction();

			middleware.SetRegistrationOptions(registrationAction);

			var ex = Assert.Throws<InvalidOperationException>(
				() => middleware.SetRegistrationOptions(registrationAction)
			);

			Assert.Equal("Registration options already set", ex.Message);
		}

		[Theory]
		[InlineData(true,  "application/json")]
		[InlineData(false, "application/xml")]
		[InlineData(true,  "application/xml", "application/json")]
		[InlineData(true,  "application/xml", "*/*")]
		[InlineData(true,  "*/*")]
		public void TestEnsureAcceptAllowed(bool valid, params string[] accept)
		{
			var serviceProvider = CreateServiceProvider();
			var middleware = new CrpcMiddleware<TRS>(serviceProvider, _loggerFactory);
			var context = new DefaultHttpContext();

			context.Request.Headers.Add("Accept", accept);
			context.Request.Method = "POST";

			if (valid)
				middleware.EnsureAcceptIsAllowed(context);
			else
				Assert.Throws<CrpcException>(() => middleware.EnsureAcceptIsAllowed(context));
		}

		[Theory]
		[InlineData(true,  "POST")]
		[InlineData(true,  "post")]
		[InlineData(false, "OPTIONS")]
		[InlineData(false, "get")]
		public async Task TestOnlyPostMethodAllowed(bool valid, string method)
		{
			var serviceProvider = CreateServiceProvider();
			var middleware = new CrpcMiddleware<TRS>(serviceProvider, _loggerFactory);
			var registrationAction = CreateGeneralRegistrationAction();
			var context = new DefaultHttpContext();

			middleware.SetRegistrationOptions(registrationAction);
			context.Request.Method = method;
			context.Request.Path = "/preview/test_endpoint";

			if (valid)
			{
				await middleware.InvokeAsync(context, (ctx) => Task.CompletedTask);

				return;
			}

			var ex = await Assert.ThrowsAsync<CrpcException>(async () =>
			{
				await middleware.InvokeAsync(context, (ctx) => Task.CompletedTask);
			});

			Assert.Equal(CrpcCodes.MethodNotAllowed, ex.Message);
		}

		private IServiceProvider CreateServiceProvider()
		{
			var services = new ServiceCollection()
				.AddSingleton<TRS>();

			return services.BuildServiceProvider();
		}

		private Action<CrpcRegistrationOptions<TRS>, TRS> CreateGeneralRegistrationAction()
		{
			Action<CrpcRegistrationOptions<TRS>, TRS> action = (opts, server) =>
			{
				opts.Register("test_endpoint", "preview", server.TestEndpoint);
			};

			return action;
		}

		internal class TRS
		{
			public async Task<string> TestEndpoint(HttpContext ctx)
			{
				return "Example output string";
			}
		}
	}
}
