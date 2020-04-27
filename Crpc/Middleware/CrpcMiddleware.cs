using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Crpc.Exceptions;
using Crpc.Registration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Serialization;

namespace Crpc.Middleware
{
	public sealed class CrpcMiddleware<T> : IMiddleware
		where T : class
	{
		private readonly Regex _urlRegex = new Regex(@"^/(?<date>\d{4}-\d{2}-\d{2}|latest|preview)/(?<method>[a-z\d_]+)$", RegexOptions.Compiled);

		private readonly IServiceProvider _services;
		private readonly ILogger _logger;
		private readonly MethodInfo _jsonDeserializeMethod;
		private readonly JsonSerializerSettings _jsonSerializerSettings;
		private readonly JsonSerializer _jsonSerializer;

		private T _server;
		private CrpcRegistrationOptions<T> _registrationOptions;

		public CrpcMiddleware(IServiceProvider services, ILoggerFactory loggerFactory)
		{
			if (services == null) throw new ArgumentNullException(nameof(services));
			if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

			_services = services;
			_logger = loggerFactory.CreateLogger(nameof(CrpcMiddleware<T>));

			// Do initial reflection setup
			_jsonSerializerSettings = new JsonSerializerSettings
			{
				ContractResolver = new DefaultContractResolver
				{
					NamingStrategy = new SnakeCaseNamingStrategy()
				}
			};
			_jsonSerializer = JsonSerializer.Create(_jsonSerializerSettings);
			_jsonDeserializeMethod = typeof(JsonSerializer).GetMethods()
				.Where(i => i.Name == "Deserialize")
				.Where(i => i.IsGenericMethod)
				.Single();
		}

		internal CrpcRegistrationOptions<T> SetRegistrationOptions(Action<CrpcRegistrationOptions<T>, T> opts)
		{
			if (_registrationOptions != null)
				throw new InvalidOperationException("Registration options already set");

			var serverType = typeof(T);
			var server = _services.GetService(serverType) ?? ActivatorUtilities.CreateInstance(_services, serverType);
			var options = new CrpcRegistrationOptions<T>();

			opts.Invoke(options, server as T);

			_registrationOptions = options;
			_server = server as T;

			return options;
		}

		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
		{
			if (context.Request.Method.ToUpper() != "POST")
				throw new CrpcException(CrpcCodes.MethodNotAllowed);

			EnsureAcceptIsAllowed(context);

			var match = _urlRegex.Match(context.Request.Path.ToUriComponent());
			if (match.Groups.Count != 3)
				throw new CrpcException(CrpcCodes.RouteNotFound);

			var urlDate = match.Groups[1].Value;
			var urlMethod = match.Groups[2].Value;

			if (!_registrationOptions.Registrations.ContainsKey(urlMethod))
				throw new CrpcException(CrpcCodes.RouteNotFound);

			var registration = _registrationOptions.Registrations[urlMethod];
			KeyValuePair<string, CrpcVersionRegistration> version;

			switch(urlDate)
			{
				case "preview":
					version = registration.FirstOrDefault(r => r.Value.IsPreview);
					break;

				case "latest":
					var versions = registration.Where(r => !r.Value.IsPreview).Select(r => r.Key);

					// If the method is only in preview, it should only be exposed if
					// preview is explicitly stated
					if (versions.Count() == 0)
						throw new CrpcException(CrpcCodes.RouteNotFound);

					var latestVersion = versions.OrderByDescending(v => DateTime.Parse(v)).First();

					version = registration.First(r => r.Key == latestVersion);
					break;

				default:
					if (!registration.ContainsKey(urlDate))
						throw new CrpcException(CrpcCodes.RouteNotFound);

					version = registration.First(r => r.Key == urlDate);
					break;
			}

			var request = ReadRequest(context, version.Value);
			object[] requestArguments;

			if (request == null)
				requestArguments = new object[] { context };
			else
				requestArguments = new object[] { context, request };

			if (version.Value.ResponseType == null)
			{
				version.Value.MethodInfo.Invoke(_server, requestArguments);
				context.Response.StatusCode = (int)HttpStatusCode.NoContent;

				return;
			}

			var response = await (dynamic) version.Value.MethodInfo.Invoke(_server, requestArguments);
			string json = JsonConvert.SerializeObject(response, _jsonSerializerSettings);

			context.Response.StatusCode = (int)HttpStatusCode.OK;
			context.Response.ContentType = "application/json; charset=utf-8";

			await context.Response.WriteAsync(json);
		}

		internal void EnsureAcceptIsAllowed(HttpContext context)
		{
			if (!context.Request.Headers.TryGetValue("Accept", out var accept))
				return;

			if (!accept.Contains("application/json") && !accept.Contains("*/*"))
				throw new CrpcException(CrpcCodes.UnsupportedAccept);
		}

		internal object ReadRequest(HttpContext context, CrpcVersionRegistration version)
		{
			if (version.RequestType == null)
				return null;

			if ((context.Request.ContentLength ?? 0) == 0)
				throw new CrpcException("invalid_body");

			using (var sr = new HttpRequestStreamReader(context.Request.Body, Encoding.UTF8))
			using (var jtr = new JsonTextReader(sr))
			using (var jsv = new JSchemaValidatingReader(jtr))
			{
				var validationErrors = new List<CrpcException>();

				jsv.Schema = version.Schema;
				jsv.ValidationEventHandler += (o, a) =>
				{
					validationErrors.Add(new CrpcException("validation_error", new Dictionary<string, object>
					{
						{ "message", a.ValidationError.Message },
						{ "value", a.ValidationError.Value },
						{ "path", a.ValidationError.Path },
						{ "location", $"line {a.ValidationError.LineNumber}, position {a.ValidationError.LinePosition}" },
					}));
				};

				object deserialized = null;
				try
				{
					deserialized = _jsonDeserializeMethod
						.MakeGenericMethod(version.RequestType)
						.Invoke(_jsonSerializer, new object[] { jsv });
				}
				catch (TargetInvocationException ex)
				{
					var innerEx = ex.InnerException;

					// If the exception isn't a serialization exception, throw
					if (!(innerEx is JsonSerializationException))
						throw innerEx;
				}

				// Handle errors
				if (validationErrors.Count() > 0)
					throw new CrpcException(CrpcCodes.ValidationFailed, null, validationErrors.AsEnumerable());

				return deserialized;
			}
		}
	}
}
