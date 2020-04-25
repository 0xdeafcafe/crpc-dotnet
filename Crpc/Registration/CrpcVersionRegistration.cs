using System;
using System.Reflection;
using Newtonsoft.Json.Schema;

namespace Crpc.Registration
{
	public class CrpcVersionRegistration
	{
		public Type RequestType { get; set; }

		public Type ResponseType { get; set; }

		public MethodInfo MethodInfo { get; set; }

		public JSchema Schema { get; set; }

		public string Version { get; set; }

		public bool IsPreview { get { return Version == "preview"; } }
	}
}
