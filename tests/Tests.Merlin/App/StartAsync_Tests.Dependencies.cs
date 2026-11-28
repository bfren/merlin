// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;
using Merlin.Replies;
using Microsoft.Extensions.DependencyInjection;

namespace Merlin.App_Tests;

public partial class StartAsync_Tests
{
	public class Dependencies
	{
		public sealed record class Greeting(string Text);

		public sealed record class Needy(Greeting Greeting);

		public sealed class Scoped;

		// a handler whose dependency is visible in its signature, as the design intends
		private static HandlerAsync Greet(Greeting greeting) =>
			_ => Task.FromResult(Reply.Text(greeting.Text));

		[Fact]
		public async Task Service_Registered_In_Builder_Reaches_Handler()
		{
			// Arrange
			var text = Rnd.Str;
			await using var host = await TestHost.StartAsync(
				r => r.Map("/").Get(sp => Greet(sp.GetRequiredService<Greeting>())),
				s => s.AddSingleton(new Greeting(text))
			);

			// Act
			var (response, body, _) = await host.SendAsync(Method.Get, "/");

			// Assert
			Assert.Equal(200, (int)response.StatusCode);
			Assert.Equal(text, body);
		}

		[Fact]
		public async Task Factory_Runs_Once_Across_Requests()
		{
			// Arrange
			var calls = 0;
			await using var host = await TestHost.StartAsync(
				r => r.Map("/").Get(sp =>
				{
					calls++;
					return Greet(sp.GetRequiredService<Greeting>());
				}),
				s => s.AddSingleton(new Greeting(Rnd.Str))
			);

			// Act
			_ = await host.SendAsync(Method.Get, "/");
			_ = await host.SendAsync(Method.Get, "/");
			_ = await host.SendAsync(Method.Head, "/");

			// Assert
			Assert.Equal(1, calls);
		}

		[Fact]
		public async Task Missing_Service_Fails_StartAsync()
		{
			// Arrange
			var start = () => TestHost.StartAsync(r => r.Map("/greet").Get(sp => Greet(sp.GetRequiredService<Greeting>())));

			// Act
			var result = await Record.ExceptionAsync(start);

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			Assert.Contains("GET", ex.Message);
			Assert.Contains("/greet", ex.Message);
			_ = Assert.IsType<InvalidOperationException>(ex.InnerException);
		}

		[Fact]
		public async Task Scoped_Service_From_Factory_Fails_StartAsync()
		{
			// Arrange
			var start = () => TestHost.StartAsync(
				r => r.Map("/").Get(sp =>
				{
					_ = sp.GetRequiredService<Scoped>();
					return Greet(new(Rnd.Str));
				}),
				s => s.AddScoped<Scoped>()
			);

			// Act
			var result = await Record.ExceptionAsync(start);

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			var inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
			Assert.Contains("scoped", inner.Message, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public async Task Transient_Service_From_Factory_Fails_StartAsync()
		{
			// Arrange
			var start = () => TestHost.StartAsync(
				r => r.Map("/").Get(sp => Greet(sp.GetRequiredService<Greeting>())),
				s => s.AddTransient(_ => new Greeting(Rnd.Str))
			);

			// Act
			var result = await Record.ExceptionAsync(start);

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			var inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
			Assert.Contains(nameof(ServiceLifetime.Transient), inner.Message);
		}

		[Fact]
		public async Task Singleton_Registered_By_Extension_Method_Reaches_Factory()
		{
			// Arrange
			Greeting? resolved = null;
			var greeting = new Greeting(Rnd.Str);

			// Act
			await using var host = await TestHost.StartAsync(
				r => r.Map("/").Get(sp =>
				{
					resolved = sp.GetRequiredService<Greeting>();
					return Greet(resolved);
				}),
				s => s.AddGreeting(greeting)
			);

			// Assert
			Assert.Same(greeting, resolved);
		}

		[Fact]
		public async Task Registration_With_Missing_Dependency_Fails_StartAsync()
		{
			// Arrange
			var start = () => TestHost.StartAsync(
				r => r.Map("/").Get(_ => Reply.Text(Rnd.Str)),
				s => s.AddSingleton<Needy>()
			);

			// Act
			var result = await Record.ExceptionAsync(start);

			// Assert
			_ = Assert.IsType<AggregateException>(result);
		}
	}
}

// stands in for a library's AddX() extension method
internal static class GreetingServiceCollectionExtensions
{
	public static IServiceCollection AddGreeting(this IServiceCollection services, StartAsync_Tests.Dependencies.Greeting greeting) =>
		services.AddSingleton(greeting);
}
