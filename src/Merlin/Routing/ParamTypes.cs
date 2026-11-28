// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Wrap;

namespace Merlin.Routing;

/// <summary>
/// The param types route templates can use, e.g. <c>int</c> in <c>{id:int}</c>.
/// </summary>
/// <remarks>
/// <para>
/// Built in: <c>int</c>, <c>guid</c>, <c>bool</c>, <c>dateonly</c> and <c>timeonly</c> (priority 10), <c>long</c> and
/// <c>datetime</c> (20), and <c>string</c> (100), which an untyped <c>{name}</c> uses. Names ignore case. Values parse with
/// Wrap's <c>F.DefaultCulture</c> (en-GB unless changed).
/// </para>
/// <para>
/// <c>datetime</c> is a <see cref="DateTimeOffset"/>: an offset is kept as sent, and a value without one is UTC, not the
/// server's time zone. Any form <see cref="DateTimeOffset"/> can parse is accepted, so a plain date is midnight UTC (a
/// sibling <c>dateonly</c> is tried first), and a time on its own is that time today.
/// </para>
/// <para>
/// Priority only matters between sibling parameters: routes with a parameter in the same position, under the same path,
/// for the same method.
/// </para>
/// </remarks>
public sealed class ParamTypes
{
	private readonly Dictionary<string, ParamType> types = new(StringComparer.OrdinalIgnoreCase)
	{
		["int"] = ParamType.Create<int>("int", 10),
		["long"] = ParamType.Create<long>("long", 20),
		["guid"] = ParamType.Create<Guid>("guid", 10),
		["bool"] = ParamType.Create<bool>("bool", 10),
		["dateonly"] = ParamType.Create<DateOnly>("dateonly", 10),
		["timeonly"] = ParamType.Create<TimeOnly>("timeonly", 10),
		["datetime"] = new("datetime", typeof(DateTimeOffset), 20, static (s, [NotNullWhen(true)] out v) =>
		{
			var ok = DateTimeOffset.TryParse(s, F.DefaultCulture, DateTimeStyles.AssumeUniversal, out var d);
			v = ok ? d : null;
			return ok;
		}),
		["string"] = ParamType.Create<string>("string", 100),   // the default for {name}
	};

	/// <summary>
	/// Add a param type.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="name">Name used in route templates, e.g. <c>postid</c> for <c>{id:postid}</c>.</param>
	/// <param name="priority">Order among sibling parameters - lower is tried first (built-in types use 10-20, <c>string</c> 100).</param>
	/// <exception cref="InvalidOperationException"><paramref name="name"/> is reserved.</exception>
	/// <exception cref="ArgumentException">A param type called <paramref name="name"/> already exists.</exception>
	public void Add<T>(string name, int priority = 50) where T : IParsable<T>
	{
		// check for reserved names to avoid breaking segment parsing
		var reserved = new[] { "*" };
		if (reserved.Contains(name))
		{
			throw new InvalidOperationException($"Route param types cannot be {string.Join(',', reserved)}.");
		}

		// throws on adding a duplicate
		types.Add(name, ParamType.Create<T>(name, priority));
	}

	/// <summary>
	/// Get a param type by name.
	/// </summary>
	/// <param name="name">Param type name.</param>
	/// <returns>The param type.</returns>
	/// <exception cref="InvalidOperationException">No param type called <paramref name="name"/> exists.</exception>
	internal ParamType Get(string name) =>
		types.TryGetValue(name, out var t)
			? t
			: throw new InvalidOperationException($"Unknown route param type '{name}'.");
}
