// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Merlin.Domain;
using Merlin.Extensions;
using Merlin.Replies;
using Merlin.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
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
	public Router Router { get; }

	/// <summary>
	/// JSON options passed to every request, combining the application's source-generated resolvers with Merlin's own.
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
			NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = true,
			TypeInfoResolver = JsonTypeInfoResolver.Combine([.. json, MerlinJson.Default])
		};
		Router = new();
	}

	/// <summary>
	/// Build the host (validating service registrations), resolve each route's handler and lock the routes,
	/// and start handling requests.
	/// </summary>
	/// <returns>A task that completes when the application shuts down.</returns>
	/// <exception cref="System.InvalidOperationException">A handler factory failed, e.g. its service is not registered.</exception>
	public Task StartAsync()
	{
		// validate services on build
		_ = Builder.Host.UseDefaultServiceProvider(opt =>
		{
			opt.ValidateScopes = true;
			opt.ValidateOnBuild = true;
		});

		// build application and lock routes (no mapping once the application has started)
		var app = Builder.Build();
		Router.Lock(app.Services);

		// add request handler middleware
		app.Run(ctx => Adapter(app.Logger, ctx));

		// run app
		return app.RunAsync();
	}

	/// <summary>
	/// Handle one request: match it against the routes, run the handler, log any failure and write the reply.
	/// </summary>
	/// <param name="logger">Application logger.</param>
	/// <param name="ctx">HTTP context of the request.</param>
	/// <returns>A task that completes when the request has been handled.</returns>
	private async Task Adapter(ILogger logger, HttpContext ctx)
	{
		// use Router to match request against registered routes
		var method = Method.Wrap(ctx.Request.Method);
		var path = ctx.Request.Path.Value ?? "/";
		var reply = Router.Match(method, path) switch
		{
			Matched m =>
				await m.Handler(new(ctx, logger, m.Values, Json)),

			MethodNotAllowed n =>
				Reply.MethodNotAllowed(n.Allowed),

			NotFound =>
				Reply.NotFound()
		};

		// if the request failed, log and return error reply
		if (reply is Failure f)
		{
			logger.Log(f);
			if (!ctx.Response.HasStarted)
			{
				// write an error reply
			}
		}

		// write reply to response etc

	}
}
