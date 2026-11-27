// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Threading.Tasks;
using Merlin.Replies;
using Wrap;

namespace Merlin.Domain;

/// <summary>
/// Handles a request that matched a route.
/// </summary>
/// <param name="req">The request.</param>
/// <returns>The reply to send, or a failure.</returns>
public delegate Task<Result<Reply>> Handler(Req req);
