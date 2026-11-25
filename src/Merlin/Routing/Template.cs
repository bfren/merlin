// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections.Immutable;
using System.Linq;

namespace Merlin.Routing;

/// <summary>
/// A parsed and validated route template.
/// </summary>
/// <param name="Source">Template as written.</param>
/// <param name="Segments">Parsed segments, in path order.</param>
public sealed record class Template(string Source, ImmutableArray<Segment> Segments)
{
	/// <summary>
	/// Parse and validate a route template.
	/// </summary>
	/// <param name="source">Route template, e.g. <c>/posts/{id:int}</c>.</param>
	/// <param name="types">Param types the template can use.</param>
	/// <returns>The parsed template.</returns>
	/// <exception cref="InvalidOperationException">The template is invalid, uses an unknown param type, or repeats a parameter name.</exception>
	public static Template Parse(string source, ParamTypes types)
	{
		// URL route parts are separated by /
		var parts = source.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		// parse each segment 
		var segments = parts
			.Select((p, i) => ParseSegment(p, isLast: i == parts.Length - 1, types, source))
			.ToImmutableArray();

		// ensure each part has a unique name
		var names = segments
			.Select(s => s switch { Param p => p.Name, CatchAll p => p.Name, _ => null })
			.OfType<string>()
			.ToList();

		if (names.Count != names.Distinct(StringComparer.OrdinalIgnoreCase).Count())
		{
			throw new InvalidOperationException($"'{source}' repeats a parameter name.");
		}

		// return the parsed and validated segment
		return new(source, segments);
	}

	/// <summary>
	/// Parse one part of a route template.
	/// </summary>
	/// <param name="part">Template part (between slashes).</param>
	/// <param name="isLast">Whether this is the last part (a catch-all must be).</param>
	/// <param name="types">Param types the template can use.</param>
	/// <param name="source">Route template, for error messages.</param>
	/// <returns>The parsed segment.</returns>
	/// <exception cref="InvalidOperationException">The part is invalid.</exception>
	private static Segment ParseSegment(string part, bool isLast, ParamTypes types, string source) =>
		part switch
		{
			['{', .. var inner, '}'] =>
				inner.Split(':') switch
				{
					[var name] when name.Length > 0 =>
						new Param(name, types.Get("string")),

					[var name, "*"] when name.Length > 0 && isLast =>
						new CatchAll(name),

					[var _, "*"] when !isLast =>
						throw new InvalidOperationException($"'{source}': a catch-all must be the last segment."),

					[var name, var type] when name.Length > 0 =>
						new Param(name, types.Get(type)),

					_ =>
						throw new InvalidOperationException($"'{source}': invalid parameter '{part}'.")
				},

			_ when part.AsSpan().IndexOfAny('{', '}') >= 0 =>
				throw new InvalidOperationException($"'{source}': unbalanced braces in '{part}'."),

			_ =>
				new Literal(part)
		};
}
