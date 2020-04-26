using System;
using System.Threading.Tasks;
using Crpc.Registration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Crpc.Tests.Registration
{
	public class CrpcRegistrationOptionsTests
	{
		[Fact]
		public void TestRegistrationOptions()
		{
			var server = new TRS();
			var options = new CrpcRegistrationOptions<TRS>();

			options.Register("test", "2020-04-26", server.TestEndpoint);

			var ex = Assert.Throws<ArgumentException>(
				() => options.Register("test", "2020-04-26", server.TestEndpoint)
			);

			Assert.StartsWith("Duplicate version found for 2020-04-26/test", ex.Message);
		}

		[Theory]
		[InlineData("preview", true)]
		[InlineData("2020-04-26", true)]
		[InlineData("2020-04-35", false)]
		[InlineData("tswift", false)]
		public void TestValidateVersion(string version, bool valid)
		{
			var options = new CrpcRegistrationOptions<TRS>();

			if (valid)
				options.ValidateVersion(version);
			else
				Assert.ThrowsAny<Exception>(() => options.ValidateVersion(version));
		}

		[Theory]
		[InlineData("good_tings", true)]
		[InlineData("bad tings", false)]
		[InlineData("!!wat!!", false)]
		[InlineData("BAD_TiNGs", false)]
		[InlineData("_wut", false)]
		[InlineData("wut_", false)]
		public void TestValidateEndpoint(string endpoint, bool valid)
		{
			var options = new CrpcRegistrationOptions<TRS>();

			if (valid)
				options.ValidateEndpoint(endpoint);
			else
				Assert.Throws<FormatException>(() => options.ValidateEndpoint(endpoint));
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
			public Task TestEndpoint(HttpContext ctx)
			{
				return Task.CompletedTask;
			}
		}
	}
}
