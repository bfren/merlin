// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Merlin.Extensions;
using Merlin.Replies;
using Merlin.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Wrap;

namespace Merlin;

public sealed class App
{
	public WebApplicationBuilder Builder { get; }

	public Router Router { get; } =
		new();

	internal JsonSerializerOptions Json { get; }

	public App(string[] args, params IJsonTypeInfoResolver[] json)
	{
		Builder = WebApplication.CreateSlimBuilder(args);
		Json = new()
		{
			TypeInfoResolver = JsonTypeInfoResolver.Combine([.. json]),
			NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
		};
	}

	public Task StartAsync()
	{
		// build application and lock routes (no mapping once the application has started)
		var app = Builder.Build();
		Router.Lock();

		// match and execute route for the current request
		app.Run(async ctx =>
		{
			// match
			var reply = Router.Match(ctx.Request.Method, ctx.Request.Path.Value ?? "/") switch
			{
				Matched m =>
					await RunAsync(m, ctx),

				MethodNotAllowed n =>
					Reply.MethodNotAllowed(n.Allowed),

				NotFound =>
					Reply.NotFound()
			};

			// if the request failed, log and return error reply
			if (reply is Failure f)
			{
				app.Logger.Log(f);
				if (!ctx.Response.HasStarted)
				{
					// write an error reply
				}
			}

			// write reply to response etc
		});

		// run app
		return app.RunAsync();
	}

	public Task<Result<Reply>> RunAsync(Matched route, HttpContext ctx)
	{
		var req = new Req(ctx, route.Values, Json);
		return route.Handler(req);
	}
}
