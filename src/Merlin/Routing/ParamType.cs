// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Diagnostics.CodeAnalysis;
using Wrap;

namespace Merlin.Routing;

public delegate bool TryParseValue(string input, [NotNullWhen(true)] out object? value);

public sealed record class ParamType(string Name, Type Type, int Priority, TryParseValue TryParse)
{
	public static ParamType Create<T>(string name, int priority) where T : IParsable<T> =>
		new(name, typeof(T), priority, (s, [NotNullWhen(true)] out v) =>
		{
			if (T.TryParse(s, F.DefaultCulture, out var t))
			{
				v = t;
				return true;
			}

			v = null;
			return false;
		});
}
