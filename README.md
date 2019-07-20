# CRPC

Simple plug-and-play CRPC middleware for ASP.NET core. Dependable and Injectable.

## About

CRPC (Cuvva-RPC) is a framework we developed at Cuvva for building out small-service architecture. This package is compatible but isn't feature complete, more about that below.

## Usage

I'll write more documentation on this later. But this should get you going.

```csharp
public class Startup
{
	public IConfiguration Configuration { get; }

	public Startup(IConfiguration configuration)
	{
		// NOTE(0xdeafcafe): Currently this configuration handover is required. I'll look into a way to make it cleaner later.
		Configuration = configuration;
	}

	public void ConfigureServices(IServiceCollection services)
	{
		services.Configure<Config>(Configuration);

		// Register the RPC server layer to it's service interface
		services.AddSingleton<IService, RPC>();

		services.AddCrpc(opts =>
		{
			opts.InternalKeys = Configuration.GetValue<string[]>("InternalKeys");
		});
	}

	public void Configure(IApplicationBuilder app)
	{
		app.UseCrpc("/1", opts => {
			// Register the server type - needed for reflection later
			opts.RegisterServer<RPC>();

			// Register a method. endpoint, method name, date (yyyy-mm-dd or "preview")
			opts.RegisterMethod("get_testing_thingy", "GetTestingThingy", "preview");
		});
	}

	public static async Task Main(string[] args) =>
		await CrpcHost.CreateCrpcHost<Startup>()
			.Build()
			.RunAsync();
}

public class RPC
{
	public async Task<string> GetTestingThingy(GetTestingThingyRequest req)
	{
		await Task.Delay(1);

		return req.Testing ? "true" : "false";
	}

	public static readonly string GetTestingThingySchema = @"
		{
			""type"": ""object"",
			""additionalProperties"": false,

			""required"": [
				""testing""
			],

			""properties"": {
				""testing"": {
					""type"": ""bool""
				}
			}
		}
	";
}

public class GetTestingThingyRequest
{
	public bool Testing { get; set; }
}
```

## What's next?

Just a few things..

- Write the auth middleware
- Inject auth context into the RPC layer
- Prometheus monitoring
- Move `CrpcException` out into a seperate Cher library.

## License

This code is based on (and compatible with) the CRPC (Cuvva-RPC) framework that was developed in-house at [Cuvva](https://github.com/cuvva). It's [licensed](LICENSE) under MIT.
