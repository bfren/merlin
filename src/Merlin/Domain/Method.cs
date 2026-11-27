// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;
using Microsoft.AspNetCore.Http;
using Wrap;

namespace Merlin.Domain;

/// <summary>
/// An HTTP request method, e.g. <c>GET</c>.
/// </summary>
/// <remarks>
/// Methods are case-sensitive (RFC 9110), so <c>get</c> is not <see cref="Get"/>.
/// Not called <c>HttpMethod</c>, which would clash with <see cref="System.Net.Http.HttpMethod"/> under implicit usings.
/// </remarks>
public sealed record class Method : Monad<Method, string>, IComparable<Method>
{
	/// <summary>
	/// <c>CONNECT</c> method.
	/// </summary>
	public static readonly Method Connect =
		Wrap(HttpMethods.Connect);

	/// <summary>
	/// <c>DELETE</c> method.
	/// </summary>
	public static readonly Method Delete =
		Wrap(HttpMethods.Delete);

	/// <summary>
	/// <c>GET</c> method.
	/// </summary>
	public static readonly Method Get =
		Wrap(HttpMethods.Get);

	/// <summary>
	/// <c>HEAD</c> method.
	/// </summary>
	public static readonly Method Head =
		Wrap(HttpMethods.Head);

	/// <summary>
	/// <c>OPTIONS</c> method.
	/// </summary>
	public static readonly Method Options =
		Wrap(HttpMethods.Options);

	/// <summary>
	/// <c>PATCH</c> method.
	/// </summary>
	public static readonly Method Patch =
		Wrap(HttpMethods.Patch);

	/// <summary>
	/// <c>POST</c> method.
	/// </summary>
	public static readonly Method Post =
		Wrap(HttpMethods.Post);

	/// <summary>
	/// <c>PUT</c> method.
	/// </summary>
	public static readonly Method Put =
		Wrap(HttpMethods.Put);

	/// <summary>
	/// Compare method names ordinally, e.g. to sort the methods in an <c>Allow</c> header.
	/// </summary>
	/// <param name="other">Method to compare with.</param>
	/// <returns>Less than zero if this sorts before <paramref name="other"/>, zero if they are the same, greater than zero if it sorts after.</returns>
	public int CompareTo(Method? other) =>
		string.CompareOrdinal(Value, other?.Value);

	/// <summary>
	/// Whether <paramref name="left"/> sorts before <paramref name="right"/>.
	/// </summary>
	/// <param name="left">Left method.</param>
	/// <param name="right">Right method.</param>
	/// <returns>True if <paramref name="left"/> sorts before <paramref name="right"/>.</returns>
	public static bool operator <(Method left, Method right) =>
		left is null ? right is not null : left.CompareTo(right) < 0;

	/// <summary>
	/// Whether <paramref name="left"/> sorts before or the same as <paramref name="right"/>.
	/// </summary>
	/// <param name="left">Left method.</param>
	/// <param name="right">Right method.</param>
	/// <returns>True if <paramref name="left"/> sorts before or the same as <paramref name="right"/>.</returns>
	public static bool operator <=(Method left, Method right) =>
		left is null || left.CompareTo(right) <= 0;

	/// <summary>
	/// Whether <paramref name="left"/> sorts after <paramref name="right"/>.
	/// </summary>
	/// <param name="left">Left method.</param>
	/// <param name="right">Right method.</param>
	/// <returns>True if <paramref name="left"/> sorts after <paramref name="right"/>.</returns>
	public static bool operator >(Method left, Method right) =>
		left is not null && left.CompareTo(right) > 0;

	/// <summary>
	/// Whether <paramref name="left"/> sorts after or the same as <paramref name="right"/>.
	/// </summary>
	/// <param name="left">Left method.</param>
	/// <param name="right">Right method.</param>
	/// <returns>True if <paramref name="left"/> sorts after or the same as <paramref name="right"/>.</returns>
	public static bool operator >=(Method left, Method right) =>
		left is null ? right is null : left.CompareTo(right) >= 0;
}
