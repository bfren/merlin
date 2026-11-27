// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;
using Merlin.Replies;
using Microsoft.Extensions.Logging;
using Wrap;

namespace Merlin.App_Tests;

public partial class StartAsync_Tests
{
	public class Cancellation
	{
		[Fact]
		public async Task Cancelled_Kind_Is_Not_Logged_As_Error_And_Writes_No_Reply()
		{
			// Arrange
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(_ => R.Fail(Rnd.Str).WithKind(FailureKind.Cancelled)));

			// Act
			var (_, _, done) = await host.SendAsync(Method.Get, "/");

			// Assert
			Assert.DoesNotContain(host.Logs, l => l.Level >= LogLevel.Error);
			Assert.Equal(0, done.BodyBytes);
			Assert.Null(done.Exception);
		}

		[Fact]
		public async Task Aborted_Request_Is_Not_Logged_As_Error()
		{
			// Arrange
			var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			await using var host = await TestHost.StartAsync(r => r.Map("/").Get(async req =>
			{
				entered.SetResult();
				try
				{
					await Task.Delay(Timeout.Infinite, req.Cancellation);
				}
				catch (OperationCanceledException e)
				{
					return R.Fail(e);
				}

				return Reply.Text(Rnd.Str);
			}));
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
			var send = host.Client.GetAsync("/", cts.Token);
			// if the handler never runs, the request completes on its own and the asserts fail rather than hang
			_ = await Task.WhenAny(entered.Task, send);

			// Act
			await cts.CancelAsync();
			_ = await Record.ExceptionAsync(() => send);
			var done = await host.Completed.ReadAsync(TestContext.Current.CancellationToken);

			// Assert
			Assert.True(done.Aborted);
			Assert.DoesNotContain(host.Logs, l => l.Level >= LogLevel.Error);
			Assert.Equal(0, done.BodyBytes);
			Assert.Null(done.Exception);
		}
	}
}
