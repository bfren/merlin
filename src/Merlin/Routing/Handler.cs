// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Threading;
using System.Threading.Tasks;
using Merlin.Replies;
using Wrap;

namespace Merlin.Routing;

public delegate Task<Result<Reply>> Handler(Req req, CancellationToken ct);
