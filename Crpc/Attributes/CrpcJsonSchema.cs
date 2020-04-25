using System;

namespace Crpc.Attributes
{
	[AttributeUsage(AttributeTargets.Method)]
	public class CrpcJsonSchema : Attribute
	{
		internal string Schema;

		public CrpcJsonSchema(string schema)
		{
			Schema = schema;
		}
	}
}
