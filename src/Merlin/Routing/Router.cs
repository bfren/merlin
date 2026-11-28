// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections.Generic;
using System.Linq;
using Merlin.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Merlin.Routing;

/// <summary>
/// The route table: one trie per HTTP method, built from route templates at startup
/// and walked for each request.
/// </summary>
public sealed partial class Router
{
	private bool locked;

	private readonly Dictionary<Method, Node> tries =
		[];

	private readonly Dictionary<string, Template> names =
		[with(StringComparer.Ordinal)];

	/// <summary>
	/// Param types available to route templates - add custom types before mapping routes that use them.
	/// </summary>
	public ParamTypes Types { get; } =
		new();

	/// <summary>
	/// Throw if routes have been locked.
	/// </summary>
	/// <exception cref="InvalidOperationException">The router is locked.</exception>
	private void EnsureUnlocked()
	{
		if (locked)
		{
			throw new InvalidOperationException("Routes are locked - add all routes before the app starts.");
		}
	}

	/// <summary>
	/// Resolve every route's handler to a <see cref="HandlerAsync"/> (calling each <see cref="HandlerFactory"/> once),
	/// then prevent any more routes being added.
	/// </summary>
	/// <param name="sp">The application's root service provider.</param>
	/// <param name="services">The application's service registrations, used to check that handler factories only resolve singletons.</param>
	/// <exception cref="InvalidOperationException">The router is already locked, or a handler factory failed (including resolving a service that is not a singleton).</exception>
	internal void Lock(IServiceProvider sp, IServiceCollection services)
	{
		EnsureUnlocked();

		foreach (var (method, root) in tries)
		{
			Resolve(method, root, new(sp, services));
		}

		locked = true;
	}

	/// <summary>
	/// The service provider passed to handler factories: resolves from the root provider, but throws for a service
	/// registered as scoped or transient, because a factory runs once and its handler would keep that instance for good.
	/// </summary>
	/// <remarks>
	/// A service is checked against its last registration. Types with no registration of their own (e.g. open generics
	/// such as <c>ILogger&lt;T&gt;</c>, or <c>IEnumerable&lt;T&gt;</c>) are passed to the root provider unchecked.
	/// Keyed services are not supported.
	/// </remarks>
	/// <param name="sp">The application's root service provider.</param>
	/// <param name="services">The application's service registrations.</param>
	internal sealed class SingletonProvider(IServiceProvider sp, IServiceCollection services) : IServiceProvider
	{
		/// <summary>
		/// Get a singleton service from the root provider.
		/// </summary>
		/// <param name="type">Service type.</param>
		/// <returns>The service, or null if it is not registered.</returns>
		/// <exception cref="InvalidOperationException">The service is registered as scoped or transient.</exception>
		public object? GetService(Type type) =>
			services.LastOrDefault(d => d.ServiceType == type) switch
			{
				{ Lifetime: not ServiceLifetime.Singleton } d =>
					throw new InvalidOperationException($"{type.Name} is {d.Lifetime}: handler factories can only resolve singletons."),

				_ =>
					sp.GetService(type)
			};
	}
}
