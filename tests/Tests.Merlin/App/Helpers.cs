// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Collections.Concurrent;
using System.Threading.Channels;
using Merlin.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Merlin.App_Tests;

/// <summary>
/// Runs a real <see cref="App"/> on Kestrel, on a free loopback port, so the adapter in
/// <see cref="App.StartAsync"/> is tested without changing it. Records what is logged and,
/// for each request, how many body bytes the adapter wrote and whether it threw.
/// </summary>
internal sealed class TestHost : IAsyncDisposable
{
	public sealed record class LogEntry(string Category, LogLevel Level, string Message, Exception? Exception);

	/// <summary>
	/// What the server saw of a request once the adapter had finished with it.
	/// </summary>
	public sealed record class Done(string Method, string Path, long BodyBytes, bool Aborted, Exception? Exception);

	private readonly Task run;

	private readonly IHostApplicationLifetime lifetime;

	public App App { get; }

	public HttpClient Client { get; }

	public ConcurrentQueue<LogEntry> Logs { get; }

	/// <summary>
	/// One item per request, written after the adapter returns (or throws).
	/// </summary>
	public ChannelReader<Done> Completed { get; }

	private TestHost(App app, Task run, IHostApplicationLifetime lifetime, Uri address, ConcurrentQueue<LogEntry> logs, ChannelReader<Done> completed) =>
		(App, this.run, this.lifetime, Client, Logs, Completed) = (app, run, lifetime, new() { BaseAddress = address }, logs, completed);

	/// <summary>
	/// Create an app with <see cref="Replies.TestJson"/>, map its routes and start it.
	/// </summary>
	public static async Task<TestHost> StartAsync(Action<Router> map)
	{
		var app = new App([], Replies.TestJson.Default);
		app.Builder.WebHost.UseUrls("http://127.0.0.1:0");

		var logs = new ConcurrentQueue<LogEntry>();
		app.Builder.Logging.ClearProviders().SetMinimumLevel(LogLevel.Trace).AddProvider(new LogProvider(logs));

		var completed = Channel.CreateUnbounded<Done>();
		app.Builder.Services.AddSingleton<IStartupFilter>(new Recorder(completed.Writer));

		var started = new TaskCompletionSource<(Uri, IHostApplicationLifetime)>(TaskCreationOptions.RunContinuationsAsynchronously);
		app.Builder.Services.AddHostedService(sp => new Started(sp, started));

		map(app.Router);
		var run = app.StartAsync();

		// surface a startup exception rather than waiting forever
		await Task.WhenAny(started.Task, run);
		if (run.IsCompleted)
		{
			await run;
			throw new InvalidOperationException("App stopped before it started.");
		}

		var (address, lifetime) = await started.Task;
		return new(app, run, lifetime, address, logs, completed.Reader);
	}

	/// <summary>
	/// Send a request and wait until the adapter has finished with it.
	/// </summary>
	public async Task<(HttpResponseMessage Response, string Body, Done Done)> SendAsync(HttpMethod method, string path)
	{
		var ct = TestContext.Current.CancellationToken;
		var response = await Client.SendAsync(new(method, path), ct);
		var body = await response.Content.ReadAsStringAsync(ct);
		var done = await Completed.ReadAsync(ct);
		return (response, body, done);
	}

	public async ValueTask DisposeAsync()
	{
		Client.Dispose();
		lifetime.StopApplication();
		await run;
	}

	/// <summary>
	/// Wraps the app's pipeline to count body bytes and report each request when it is done.
	/// </summary>
	private sealed class Recorder(ChannelWriter<Done> done) : IStartupFilter
	{
		public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
			app =>
			{
				app.Use(async (HttpContext ctx, RequestDelegate inner) =>
				{
					var body = new CountingStream(ctx.Response.Body);
					ctx.Response.Body = body;
					Exception? exception = null;
					try
					{
						await inner(ctx);
					}
					catch (Exception e)
					{
						exception = e;
						throw;
					}
					finally
					{
						done.TryWrite(new(ctx.Request.Method, ctx.Request.Path, body.Count, ctx.RequestAborted.IsCancellationRequested, exception));
					}
				});
				next(app);
			};
	}

	/// <summary>
	/// Completes <paramref name="started"/> with the bound address once the server is listening.
	/// </summary>
	private sealed class Started(IServiceProvider sp, TaskCompletionSource<(Uri, IHostApplicationLifetime)> started) : IHostedService
	{
		public Task StartAsync(CancellationToken cancellationToken)
		{
			var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
			_ = lifetime.ApplicationStarted.Register(() =>
			{
				var address = sp.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
				_ = started.TrySetResult((new(address), lifetime));
			});
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken) =>
			Task.CompletedTask;
	}

	private sealed class CountingStream(Stream inner) : Stream
	{
		public long Count { get; private set; }

		public override bool CanRead => false;
		public override bool CanSeek => false;
		public override bool CanWrite => true;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

		public override void Flush() => inner.Flush();
		public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);
		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count)
		{
			Count += count;
			inner.Write(buffer, offset, count);
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			Count += buffer.Length;
			return inner.WriteAsync(buffer, cancellationToken);
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
			WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
	}

	private sealed class LogProvider(ConcurrentQueue<LogEntry> logs) : ILoggerProvider
	{
		public ILogger CreateLogger(string categoryName) =>
			new Logger(categoryName, logs);

		public void Dispose() { }

		private sealed class Logger(string category, ConcurrentQueue<LogEntry> logs) : ILogger
		{
			public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
				null;

			public bool IsEnabled(LogLevel logLevel) =>
				true;

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
				logs.Enqueue(new(category, logLevel, formatter(state, exception), exception));
		}
	}
}
