# CRPC

[![Nuget Version](https://img.shields.io/nuget/v/crpc.svg?style=flat)](//www.nuget.org/packages/crpc)
[![Build Status](https://img.shields.io/travis/0xdeafcafe/crpc-dotnet.svg?style=flat)](//travis-ci.org/0xdeafcafe/crpc-dotnet)

Simple plug-and-play CRPC middleware for ASP.NET core. Dependable and Injectable.

## About

CRPC (Cuvva-RPC) is a framework we developed at Cuvva for building out small-service architecture. This package is compatible but isn't feature complete, more about that below.

## Installation

Available on [NuGet](https://nuget.org/packages/crpc).

Visual Studio:
```powershell
PM> Install-Package Crpc
```

.Net Core CLI:
```bash
$ dotnet add package crpc
```

## Usage

I'll write more documentation on this later. But this should get you going.

```csharp
public class ApplicationHub
{
	private readonly ILogger _logger;
	private readonly IServiceProvider _serviceProvider;

	public ApplicationHub(ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
	{
		_logger = loggerFactory.CreateLogger(nameof(Application));
		_serviceProvider = serviceProvider;
	}

	public async Task<string> GetName()
	{
		return "Alex";
	}
}

public class RpcServer
{
	private ApplicationHub _app;

	public RpcServer(ApplicationHub app)
	{
		_app = app;
	}

	public async Task<string> GetName(HttpContext ctx)
	{
		return await _app.GetName();
	}
}

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

		services.AddCrpc<IService, RPC>(opts =>
		{
			opts.InternalKeys = Configuration.GetValue<string[]>("InternalKeys");
		});
	}

	public void Configure(IApplicationBuilder app)
	{
		app.UseCrpc<RpcServer>("/1", (opts, rpc) => {
			// What kind of auth do we want?
			opts.Authentication = AuthenticationType.AllowInternalAuthentication;

			// Register a method. endpoint name, version (yyyy-mm-dd or "preview"), rpc method, and optionally the json schema string
			opts.Register<string>("get_name", "2020-04-26", rpc.GetName);
		});
	}

	public static async Task Main(string[] args) =>
		await CrpcHost.CreateCrpcHost<Startup>()
			.Build()
			.RunAsync();
}
```

## What's next?

Just a few things..

- Write the auth middleware
- Prometheus monitoring
- Move `CrpcException` out into a seperate Cher library.

## License

This code is based on (and compatible with) the CRPC (Cuvva-RPC) framework that was developed in-house at [Cuvva](https://github.com/cuvva). It's [licensed](LICENSE) under MIT.
