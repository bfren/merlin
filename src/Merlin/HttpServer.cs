// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Net;
using System.Threading.Tasks;
using Merlin.Routing;

namespace Merlin;

/// <summary>
/// Merlin: Simple HTTP Server
/// </summary>
public sealed class HttpServer : IDisposable
{
	private readonly HttpListener listener = new();

	private readonly Router router = new();

	/// <summary>
	/// Start listening on port <paramref name="port"/>
	/// </summary>
	/// <param name="port">Port to listen on</param>
	public async Task StartAsync(int port = 5000)
	{
		// listen to port
		var listenUrl = $"http://localhost:{port}/";
		Console.WriteLine("Listening on {0}", listenUrl);
		listener.Prefixes.Add(listenUrl);
		listener.Start();

		// handle requests
		await Handle();
	}

	/// <summary>
	/// Stop the HTTP server
	/// </summary>
	public void Stop() =>
		Dispose();

	/// <summary>
	/// Response to a request
	/// </summary>
	private async Task Handle()
	{
		// if the HttpListener is not started, do nothing
		if (!listener.IsListening)
		{
			return;
		}

		// get context
		var context = await listener.GetContextAsync();

		// dispatch request
		var request = context.Request;
		var result = await router.DispatchAsync(request);

		// send result
		using (var response = context.Response)
		{
			// set response values
			response.StatusCode = (int)result.StatusCode;
			response.Headers = result.Headers;

			// write result to output stream
			using var output = response.OutputStream;
			var content = await result.GetContentAsync();
			await output.WriteAsync(content);
		}

		// handle next request
		await Handle();
	}

	/// <inheritdoc/>
	public void Dispose() =>
		listener.Close();
}
