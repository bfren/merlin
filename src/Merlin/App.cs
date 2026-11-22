// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Merlin.Extensions;
using Merlin.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Wrap;
using Wrap.Linq;

namespace Merlin;

public sealed class App
{
	public WebApplicationBuilder Builder { get; }

	public Router Router { get; }

	internal JsonSerializerOptions Json { get; }

	public App(string[] args, params IJsonTypeInfoResolver[] json)
	{
		Builder = WebApplication.CreateSlimBuilder(args);
		Json = new() { TypeInfoResolver = JsonTypeInfoResolver.Combine([.. json]) };
		Router = new();
	}

	public Task StartAsync()
	{
		// build application
		var app = Builder.Build();
		app.Run(async ctx =>
		{
			// dispatch requests via the Router and write to Response stream
			var result = await (
				from reply in Router.DispatchAsync(ctx)
				from done in reply.WriteAsync(ctx.Response, ctx.RequestAborted)
				select done
			);

			// log any failures
			if (result is Failure f)
			{
				app.Logger.Log(f.Level.ToMs(), f.Exception, f.Message, f.Args);

				if (!ctx.Response.HasStarted)
				{
					// write an error reply
				}
			}
		});

		// run app
		return app.RunAsync();
	}
}
