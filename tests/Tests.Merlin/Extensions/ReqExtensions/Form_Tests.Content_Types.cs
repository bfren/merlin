// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Wrap.Testing;
using static Merlin.Extensions.ReqExtensions_Tests.Helpers;

namespace Merlin.Extensions.ReqExtensions_Tests;

public partial class Form_Tests
{
	public class Content_Types
	{
		[Fact]
		public async Task Accepts_Multipart()
		{
			// Arrange
			var boundary = "merlin-boundary";
			var body = string.Join("\r\n",
				$"--{boundary}",
				"Content-Disposition: form-data; name=\"title\"",
				"",
				"x",
				$"--{boundary}--",
				"");
			var req = NewReq($"multipart/form-data; boundary={boundary}", body);

			// Act
			var result = await req.Form(BinderJson.Default.PostDto);

			// Assert
			Assert.Equal("x", result.AssertOk().Title);
		}

		[Theory]
		[InlineData("application/json")]
		[InlineData("text/plain")]
		[InlineData(null)]
		public async Task Wrong_Content_Type_Is_Invalid(string? contentType)
		{
			// Arrange
			var req = NewReq(contentType, "title=x");

			// Act
			var result = await req.Form(BinderJson.Default.PostDto);

			// Assert
			AssertFailure(result, Wrap.FailureKind.Invalid, NotForm);
		}

		[Fact]
		public async Task Cancelled_Read_Has_Kind_Cancelled()
		{
			// Arrange
			var req = NewReq(Form, "title=x", cancelled: true);

			// Act
			var result = await req.Form(BinderJson.Default.PostDto);

			// Assert
			Assert.Equal(Wrap.FailureKind.Cancelled, result.AssertFailure().Kind);
		}
	}
}
