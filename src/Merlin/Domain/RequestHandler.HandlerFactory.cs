// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;

namespace Merlin.Domain;

/// <summary>
/// Creates a route's handler from the application's services. It is called once, when the router is locked
/// at startup, so a handler gets its dependencies (e.g. a database connection factory) by closure.
/// </summary>
/// <remarks>
/// Only resolve singletons: the provider is the root one, and resolving a scoped service fails at startup.
/// </remarks>
/// <param name="sp">The application's root service provider.</param>
/// <returns>The asynchronous handler used for every request to the route.</returns>
public delegate HandlerAsync HandlerFactory(IServiceProvider sp);
