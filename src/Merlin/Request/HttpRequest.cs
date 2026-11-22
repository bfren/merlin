// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Net;

namespace Merlin.Request;

public sealed record class HttpRequest
{
#pragma warning disable IDE0060 // stub: request will be parsed once implemented
	public HttpRequest(HttpListenerRequest request)
	{
		// Use IParsable to parse query string to function argument
	}
#pragma warning restore IDE0060
}
