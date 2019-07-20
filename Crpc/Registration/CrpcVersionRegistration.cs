using System;
using System.Reflection;
using Newtonsoft.Json.Schema;

namespace Crpc.Registration
{
	public class CrpcVersionRegistration
	{
		public Type RequestType { get; set; }

		public Type ResponseType { get; set; }

		public MethodInfo Method { get; set; }

		public JSchema Schema { get; set; }

		public string Date { get; set; }

		public bool IsPreview { get { return Date == "preview"; } }
	}
}
