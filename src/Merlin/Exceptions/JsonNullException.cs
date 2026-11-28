// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System;

namespace Merlin.Exceptions;

public class JsonNullException : Exception
{
	public JsonNullException() : base() { }

	public JsonNullException(string message) : base(message) { }

	public JsonNullException(string message, Exception innerException) : base(message, innerException) { }
}
