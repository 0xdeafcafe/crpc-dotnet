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
	public class CorsMiddlewareTests
	{
		private ILoggerFactory _loggerFactory;

		public CorsMiddlewareTests()
		{
			_loggerFactory = new NullLoggerFactory();
		}
	}
}
