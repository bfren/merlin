// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;

namespace Merlin.Domain;

/// <summary>
/// Creates a route's handler from the application's services. It is called once, when the router is locked
/// at startup, so a handler gets its dependencies (e.g. a database connection factory) by closure.
/// </summary>
/// <remarks>
/// Only singletons can be resolved: a scoped or transient service fails at startup, because the handler would keep
/// one instance for good. For per-request things, resolve a singleton that creates them, such as a
/// <c>DbDataSource</c>, an <c>IDbContextFactory&lt;T&gt;</c> or a <c>Func&lt;T&gt;</c>.
/// </remarks>
/// <param name="sp">The application's services, limited to singletons.</param>
/// <returns>The asynchronous handler used for every request to the route.</returns>
public delegate HandlerAsync HandlerFactory(IServiceProvider sp);
