// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;

namespace Merlin.Routing.Router_Tests;

public partial class Match_Tests
{
	private static Handler NewHandler() =>
		Substitute.For<Handler>();
}
