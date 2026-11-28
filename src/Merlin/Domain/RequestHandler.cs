// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Domain;

/// <summary>
/// A route's handler as it was mapped: a synchronous <see cref="Handler"/>, an asynchronous <see cref="HandlerAsync"/>,
/// or a <see cref="HandlerFactory"/> that creates one at startup.
/// </summary>
public readonly union RequestHandler(Handler, HandlerAsync, HandlerFactory);
