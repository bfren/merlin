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

/// <summary>
/// A Merlin application: builds a Kestrel host, holds the <see cref="Routing.Router"/> and JSON options,
/// and dispatches each request to the handler of the route it matches.
/// </summary>
public sealed class App
{
	/// <summary>
	/// The underlying ASP.NET Core builder - use it to configure hosting (ports, logging, services)
	/// before calling <see cref="StartAsync"/>.
	/// </summary>
	public WebApplicationBuilder Builder { get; }

	/// <summary>
	/// The route table - map every route before calling <see cref="StartAsync"/>.
	/// </summary>
	public Router Router { get; } =
		new();

	/// <summary>
	/// JSON options passed to every request, combining the application's source-generated resolvers.
	/// </summary>
	internal JsonSerializerOptions Json { get; }

	/// <summary>
	/// Create a Merlin application.
	/// </summary>
	/// <param name="args">Command-line arguments, passed to the ASP.NET Core builder.</param>
	/// <param name="json">Source-generated JSON type resolvers (e.g. <c>AppJson.Default</c>) for every type the application reads or writes as JSON.</param>
	public App(string[] args, params IJsonTypeInfoResolver[] json)
	{
		Builder = WebApplication.CreateSlimBuilder(args);
		Json = new()
		{
			TypeInfoResolver = JsonTypeInfoResolver.Combine([.. json]),
			NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
		};
	}

	/// <summary>
	/// Build the host, lock the routes, and start handling requests.
	/// </summary>
	/// <returns>A task that completes when the application shuts down.</returns>
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

	/// <summary>
	/// Run a matched route's handler with a <see cref="Req"/> built from the current request.
	/// </summary>
	/// <param name="route">The matched route.</param>
	/// <param name="ctx">The current HTTP context.</param>
	/// <returns>The handler's reply, or a failure.</returns>
	public Task<Result<Reply>> RunAsync(Matched route, HttpContext ctx)
	{
		var req = new Req(ctx, route.Values, Json);
		return route.Handler(req);
	}
}
