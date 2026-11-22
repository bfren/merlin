// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Lifecycle;

public enum Hook
{
	GetContext = 1 << 0,
	GetRequest = 1 << 1,
	GetUser = 1 << 2,
	GetResult = 1 << 3,
}
