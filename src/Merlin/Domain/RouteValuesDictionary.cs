// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Merlin.Domain;

/// <summary>
/// Typed values captured from a request path, keyed by parameter name (case-insensitive).
/// </summary>
public sealed record class RouteValuesDictionary : IReadOnlyDictionary<string, object>
{
	/// <summary>
	/// No values.
	/// </summary>
	public static readonly RouteValuesDictionary Empty =
		new(ImmutableDictionary.Create<string, object>(StringComparer.OrdinalIgnoreCase));

	/// <summary>
	/// Values, keyed by parameter name (case-insensitive).
	/// </summary>
	private ImmutableDictionary<string, object> Items { get; }

	private RouteValuesDictionary(ImmutableDictionary<string, object> items) =>
		Items = items;

	/// <summary>
	/// Add a value, returning new route values.
	/// </summary>
	/// <param name="name">Parameter name.</param>
	/// <param name="value">Typed value captured from the path.</param>
	/// <returns>New route values holding the existing values and this one.</returns>
	/// <exception cref="ArgumentException">A value with this name has already been added.</exception>
	public RouteValuesDictionary Add(string name, object value) =>
		new(Items.Add(name, value));

	// IReadOnlyDictionary members delegate to Items

	/// <inheritdoc/>
	public object this[string key] =>
		Items[key];

	/// <inheritdoc/>
	public IEnumerable<string> Keys =>
		Items.Keys;

	/// <inheritdoc/>
	public IEnumerable<object> Values =>
		Items.Values;

	/// <inheritdoc/>
	public int Count =>
		Items.Count;

	/// <inheritdoc/>
	public bool ContainsKey(string key) =>
		Items.ContainsKey(key);

	/// <inheritdoc/>
	public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) =>
		Items.TryGetValue(key, out value);

	/// <inheritdoc/>
	public IEnumerator<KeyValuePair<string, object>> GetEnumerator() =>
		Items.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
