using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Schema;
using Microsoft.AspNetCore.Http;

namespace Crpc.Registration
{
	public enum AuthenticationType
	{
		UnsafeNoAuthentication,
		AllowInternalAuthentication,
	}

	public class CrpcRegistrationOptions<T>
		where T : class
	{
		private static readonly Regex _endpointRegex = new Regex(@"^[a-z]{1}[a-z0-9_]+[a-z]{1}$", RegexOptions.Compiled);

		internal T ServerInstance { get; set; }

		internal Dictionary<string, Dictionary<string, CrpcVersionRegistration>> Registrations { get; set; }

		public AuthenticationType Authentication { get; set; }

		public CrpcRegistrationOptions()
		{
			Registrations = new Dictionary<string, Dictionary<string, CrpcVersionRegistration>>();
		}

		public void Register(string endpoint, string version, Func<HttpContext, Task> method)
		{
			MethodRegister(endpoint, version, method.GetMethodInfo());
		}

		public void Register<TReq>(string endpoint, string version, Func<HttpContext, TReq, Task> method, string schema)
		{
			MethodRegister(endpoint, version, method.GetMethodInfo(), schema);
		}

		public void Register<TRes>(string endpoint, string version, Func<HttpContext, Task<TRes>> method)
		{
			MethodRegister(endpoint, version, method.GetMethodInfo());
		}

		public void Register<TReq, TRes>(string endpoint, string version, Func<HttpContext, TReq, Task<TRes>> method, string schema)
		{
			MethodRegister(endpoint, version, method.GetMethodInfo(), schema);
		}

		internal void MethodRegister(string endpoint, string version, MethodInfo methodInfo, string schema = null)
		{
			ValidateVersion(version);
			ValidateEndpoint(endpoint);

			Dictionary<string, CrpcVersionRegistration> registration;
			Type responseType = null;
			var requestTypes = methodInfo.GetParameters();
			var responseTask = methodInfo.ReturnType;

			if (responseTask.GenericTypeArguments.Length > 0)
				responseType = responseTask.GenericTypeArguments[0];

			if (requestTypes.Length > 2)
				throw new InvalidOperationException($"The endpoint {version}/{endpoint} has too many arguments");

			if (Registrations.ContainsKey(endpoint))
				registration = Registrations[endpoint];
			else
				registration = new Dictionary<string, CrpcVersionRegistration>();

			if (registration.ContainsKey(version))
				throw new ArgumentException($"Duplicate version found for {version}/{endpoint}", nameof(version));

			var registrationVersion = new CrpcVersionRegistration
			{
				ResponseType = responseType,
				MethodInfo = methodInfo,
				Version = version,
			};

			// Request types are optional, and we only need to load the schema in if
			// a request as a payload.
			if (requestTypes.Length > 1)
			{
				if (schema == null)
					throw new Exception($"No schema specified for {version}/{endpoint}");

				registrationVersion.Schema = JSchema.Parse(schema);
				registrationVersion.RequestType = requestTypes[0].ParameterType;
			}

			registration.Add(version, registrationVersion);

			Registrations[endpoint] = registration;
		}

		/// <summary>
		/// Validates the format of the input version. Versions are either formatted as
		/// yyyy-MM-dd or be "preview".
		/// </summary>
		/// <param name="version">The version to validate.</param>
		internal void ValidateVersion(string version)
		{
			if (version == "preview")
				return;

			DateTime.ParseExact(version, "yyyy-MM-dd", null);
		}

		/// <summary>
		/// Validates the format of the input endpoint. Endpoints have to be lowercase
		/// and alphanumeric (with underscores allowed). They also have to start and end
		/// with a letter.
		/// </summary>
		/// <param name="endpoint"></param>
		internal void ValidateEndpoint(string endpoint)
		{
			if (!_endpointRegex.Match(endpoint).Success)
				throw new FormatException("endpoint format incorrect");
		}
	}
}
