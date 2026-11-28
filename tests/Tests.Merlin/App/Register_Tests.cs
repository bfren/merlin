// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;
using Merlin.Replies;
using Microsoft.Extensions.DependencyInjection;

namespace Merlin.App_Tests;

public class Register_Tests
{
	public interface IGreeter
	{
		string Text { get; }
	}

	public sealed class Greeter : IGreeter
	{
		public string Text { get; } = Rnd.Str;
	}

	// depends on another registered service, so the container must use its constructor
	public sealed class Welcomer(Greeter greeter)
	{
		public Greeter Greeter { get; } = greeter;
	}

	private static HandlerAsync Handler() =>
		_ => Task.FromResult(Reply.Text(Rnd.Str));

	private static ServiceDescriptor Single(App app, Type type) =>
		Assert.Single(app.Builder.Services, d => d.ServiceType == type);

	public class Single_Type
	{
		[Fact]
		public void Registers_Class_As_Singleton()
		{
			// Arrange
			var app = new App([]);

			// Act
			_ = app.Register<Greeter>();

			// Assert
			var d = Single(app, typeof(Greeter));
			Assert.Equal(ServiceLifetime.Singleton, d.Lifetime);
			Assert.Equal(typeof(Greeter), d.ImplementationType);
		}

		[Fact]
		public void Returns_App_Services()
		{
			// Arrange
			var app = new App([]);

			// Act
			var result = app.Register<Greeter>();

			// Assert
			Assert.Same(app.Builder.Services, result);
		}

		[Fact]
		public async Task Registered_Class_Reaches_Handler_Factory()
		{
			// Arrange
			Greeter? resolved = null;

			// Act
			await using var host = await TestHost.StartAsync(
				r => r.Map("/").Get(sp => { resolved = sp.GetRequiredService<Greeter>(); return Handler(); }),
				setup: app => app.Register<Greeter>()
			);

			// Assert
			Assert.NotNull(resolved);
		}

		[Fact]
		public async Task Registered_Class_Gets_Its_Dependencies()
		{
			// Arrange
			Welcomer? resolved = null;

			// Act
			await using var host = await TestHost.StartAsync(
				r => r.Map("/").Get(sp => { resolved = sp.GetRequiredService<Welcomer>(); return Handler(); }),
				setup: app => app.Register<Greeter>().AddSingleton<Welcomer>()
			);

			// Assert
			Assert.NotNull(resolved?.Greeter);
		}

		[Fact]
		public async Task Is_One_Instance_Across_Factories()
		{
			// Arrange
			Greeter? first = null;
			Greeter? second = null;

			// Act
			await using var host = await TestHost.StartAsync(
				r => r
					.Map("/a").Get(sp => { first = sp.GetRequiredService<Greeter>(); return Handler(); })
					.Map("/b").Get(sp => { second = sp.GetRequiredService<Greeter>(); return Handler(); }),
				setup: app => app.Register<Greeter>()
			);

			// Assert
			Assert.NotNull(first);
			Assert.Same(first, second);
		}
	}

	public class Service_And_Implementation
	{
		[Fact]
		public void Registers_Implementation_As_Singleton_Service()
		{
			// Arrange
			var app = new App([]);

			// Act
			_ = app.Register<IGreeter, Greeter>();

			// Assert
			var d = Single(app, typeof(IGreeter));
			Assert.Equal(ServiceLifetime.Singleton, d.Lifetime);
			Assert.Equal(typeof(Greeter), d.ImplementationType);
		}

		[Fact]
		public void Returns_App_Services()
		{
			// Arrange
			var app = new App([]);

			// Act
			var result = app.Register<IGreeter, Greeter>();

			// Assert
			Assert.Same(app.Builder.Services, result);
		}

		[Fact]
		public async Task Registered_Implementation_Reaches_Handler_Factory_As_Service()
		{
			// Arrange
			IGreeter? resolved = null;

			// Act
			await using var host = await TestHost.StartAsync(
				r => r.Map("/").Get(sp => { resolved = sp.GetRequiredService<IGreeter>(); return Handler(); }),
				setup: app => app.Register<IGreeter, Greeter>()
			);

			// Assert
			_ = Assert.IsType<Greeter>(resolved);
		}
	}
}
