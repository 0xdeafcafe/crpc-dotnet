namespace Crpc.Exceptions
{
	public static class CrpcCodes
	{
		public const string BadRequest = "bad_request";
		public const string Unauthorized = "unauthorized";
		public const string UnsupportedAccept = "unsupported_accept";
		public const string AccessDenied = "access_denied";
		public const string NotFound = "not_found";
		public const string RouteNotFound = "route_not_found";
		public const string MethodNotAllowed = "method_not_allowed";
		public const string Unknown = "unknown";
		public const string NoLongerSupported = "no_longer_supported";
		public const string TooManyRequests = "too_many_requests";
		public const string ValidationFailed = "validation_failed";

		public const string CoercionError = "unable_to_coerce_error";
	}
}
