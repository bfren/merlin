// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Diagnostics.CodeAnalysis;
using Wrap;

namespace Merlin.Routing;

/// <summary>
/// Parse route segment text into a typed value.
/// </summary>
/// <param name="input">Segment text.</param>
/// <param name="value">The parsed value, if successful.</param>
/// <returns>True if the text was parsed.</returns>
public delegate bool TryParseValue(string input, [NotNullWhen(true)] out object? value);

/// <summary>
/// A type route parameters can be declared as, e.g. <c>int</c> in <c>{id:int}</c>.
/// </summary>
/// <param name="Name">Name used in route templates.</param>
/// <param name="Type">Type that values are parsed into.</param>
/// <param name="Priority">Order in which sibling parameters are tried during matching - lower first.</param>
/// <param name="TryParse">Parser for segment text.</param>
public sealed record class ParamType(string Name, Type Type, int Priority, TryParseValue TryParse)
{
	/// <summary>
	/// Create a param type from any <see cref="IParsable{TSelf}"/> type, parsing with Wrap's <c>F.DefaultCulture</c>.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="name">Name used in route templates.</param>
	/// <param name="priority">Order in which sibling parameters are tried during matching - lower first.</param>
	/// <returns>A param type.</returns>
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
