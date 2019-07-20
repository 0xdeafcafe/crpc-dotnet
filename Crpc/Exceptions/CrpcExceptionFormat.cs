using System.Collections.Generic;
using Newtonsoft.Json;

namespace Crpc.Exceptions
{
	internal class CrpcExceptionFormat
	{
		public string Code { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public Dictionary<string, object> Meta { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public IEnumerable<CrpcExceptionFormat> Reasons { get; set; } = null;
	}
}
