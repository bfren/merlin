// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Wrap.Testing;
using static Merlin.Extensions.ReqExtensions_Tests.Helpers;

namespace Merlin.Extensions.ReqExtensions_Tests;

public partial class Form_Tests
{
	private const string Form = "application/x-www-form-urlencoded";

	private const string NotForm = "Request body is not a form.";

	private const string InvalidField = "Form field {Name} is not valid.";

	private static Task<Wrap.Result<PostDto>> Bind(string body) =>
		NewReq(Form, body).Form(BinderJson.Default.PostDto);

	private static void AssertFailure<T>(Wrap.Result<T> result, Wrap.FailureKind kind, string message, params object[] args)
	{
		var failure = result.AssertFailure(message, args);
		Assert.Equal(kind, failure.Kind);
	}
}
