// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Merlin.Domain;
using Merlin.Extensions;
using Merlin.Replies;
using Merlin.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wrap;
using Wrap.Extensions;

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
	/// Register a class as a singleton, so handler factories can resolve it.
	/// </summary>
	/// <remarks>
	/// The same as <c>Builder.Services.AddSingleton&lt;T&gt;()</c>. Handler factories can only resolve singletons.
	/// </remarks>
	/// <typeparam name="T">Service type, created by the container from its public constructor.</typeparam>
	/// <returns>The application's service collection, to add more services.</returns>
	public IServiceCollection Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
		where T : class =>
		Builder.Services.AddSingleton<T>();

	/// <summary>
	/// Register an implementation of a service as a singleton, so handler factories can resolve it.
	/// </summary>
	/// <remarks>
	/// The same as <c>Builder.Services.AddSingleton&lt;TService, TImplementation&gt;()</c>. Handler factories can only resolve singletons.
	/// </remarks>
	/// <typeparam name="TService">Service type that handler factories resolve, e.g. an interface.</typeparam>
	/// <typeparam name="TImplementation">Implementation type, created by the container from its public constructor.</typeparam>
	/// <returns>The application's service collection, to add more services.</returns>
	public IServiceCollection Register<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
		where TService : class
		where TImplementation : class, TService =>
		Builder.Services.AddSingleton<TService, TImplementation>();

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

		// build application and lock routes (no mapping once the application has started) -
		// locking routes also resolves every RequestHandler into a HandlerAsync
		var app = Builder.Build();
		Router.Lock(app.Services, Builder.Services);

		// add request handler middleware
		app.Run(ctx => Adapter(app.Logger, ctx));

		// run app
		return app.RunAsync();
	}

	/// <summary>
	/// Handle one request: match it against the routes, run the handler and write its reply.
	/// </summary>
	/// <remarks>
	/// A failure from the handler, or from writing its reply, is logged and replaced with an error reply
	/// (<see cref="Reply.Error"/>), unless the response has already started. A cancelled request (a <see cref="FailureKind.Cancelled"/>
	/// failure, or a client that has gone) is not logged and gets no reply. A HEAD request gets the headers without the body.
	/// </remarks>
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

		// a cancelled request is not an error, nobody is there to read a reply
		if ((reply is Failure f && f.Kind == FailureKind.Cancelled) || ctx.RequestAborted.IsCancellationRequested)
		{
			return;
		}

		// HEAD reply returns the same headers as GET, but no body
		if (method == Method.Head)
		{
			ctx.Response.Body = Stream.Null;
		}

		// write reply to the HTTP response and log any Failure values
		Task<Result<bool>> write(Reply r) =>
			r.WriteAsync(ctx.Response, Json, ctx.RequestAborted);

		_ = await reply
			.BindAsync(write)
			.IfFailedAsync(async f =>
			{
				logger.Log(f);
				if (!ctx.Response.HasStarted)
				{
					_ = await Reply.Error(f).BindAsync(write);
				}
			});
	}
}
