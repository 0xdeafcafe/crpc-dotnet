using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace Crpc.Exceptions
{
	using Meta = Dictionary<string, object>;

	[JsonConverter(typeof(CrpcExceptionConverter))]
	public class CrpcException : AggregateException
	{
		public CrpcException() { }

		public CrpcException(string code) : base(code) { }

		public CrpcException(string code, Meta data)
			: base(code)
		{
			if (data == null)
				return;

			foreach (var pair in data)
				Data.Add(pair.Key, pair.Value);
		}

		public CrpcException(string code, Meta data, Exception ex)
			: base(code, ex)
		{
			if (data == null)
				return;

			foreach (var pair in data)
				Data.Add(pair.Key, pair.Value);
		}

		public CrpcException(string code, Meta data, IEnumerable<Exception> exs)
			: base(code, exs)
		{
			if (data == null)
				return;

			foreach (var pair in data)
				Data.Add(pair.Key, pair.Value);
		}

		public int StatusCode()
		{
			switch(Message)
			{
				case CrpcCodes.Unauthorized:
					return (int) HttpStatusCode.Unauthorized;

				case CrpcCodes.AccessDenied:
					return (int) HttpStatusCode.Forbidden;

				case CrpcCodes.RouteNotFound:
				case CrpcCodes.NotFound:
					return (int) HttpStatusCode.NotFound;

				case CrpcCodes.MethodNotAllowed:
					return (int) HttpStatusCode.MethodNotAllowed;

				case CrpcCodes.NoLongerSupported:
					return (int) HttpStatusCode.Gone;

				case CrpcCodes.ValidationFailed:
					return 422;

				case CrpcCodes.TooManyRequests:
					return 429;

				case CrpcCodes.CoercionError:
				case CrpcCodes.Unknown:
					return (int) HttpStatusCode.InternalServerError;

				case CrpcCodes.BadRequest:
				case CrpcCodes.UnsupportedAccept:
				default:
					return (int) HttpStatusCode.BadRequest;
			}
		}
	}
}
