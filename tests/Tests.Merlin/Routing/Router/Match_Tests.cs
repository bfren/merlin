// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;

namespace Merlin.Routing.Router_Tests;

public partial class Match_Tests
{
	// Match only sees resolved handlers, so every test locks the router first
	private static IServiceProvider Sp =>
		Substitute.For<IServiceProvider>();

	private static HandlerAsync NewHandler() =>
		Substitute.For<HandlerAsync>();
}
