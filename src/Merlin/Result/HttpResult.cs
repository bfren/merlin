// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Net;
using System.Threading.Tasks;

namespace Merlin.Result;

public abstract record class HttpResult
{
	public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;

	public WebHeaderCollection Headers { get; } = new();

	protected HttpResult(string contentType)
	{
		Headers.Set("Content-Type", contentType);
		Headers.Set("Server", "Merlin");
		Headers.Set("X-Clacks-Overhead", "GNU Terry Pratchett");
	}

	public abstract Task<byte[]> GetContentAsync();
}
