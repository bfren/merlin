// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Wrap.Testing;
using static Merlin.Extensions.ReqExtensions_Tests.Helpers;

namespace Merlin.Extensions.ReqExtensions_Tests;

public partial class Form_Tests
{
	public class Types
	{
		[Fact]
		public async Task Binds_Init_Only_Properties()
		{
			// Arrange
			var title = Rnd.Str;
			var req = NewReq(Form, $"title={title}&views=3");

			// Act
			var result = await req.Form(BinderJson.Default.PostInitDto);

			// Assert
			var post = result.AssertOk();
			Assert.Equal(title, post.Title);
			Assert.Equal(3, post.Views);
		}

		[Fact]
		public async Task Binds_Constructor_Parameters()
		{
			// Arrange

			// Act
			var result = await Bind("title=x&views=2&draft=on&tags=a&author_name=y");

			// Assert
			var post = result.AssertOk();
			Assert.Equal("x", post.Title);
			Assert.Equal(2, post.Views);
			Assert.True(post.Draft);
			Assert.Equal(["a"], post.Tags ?? []);
			Assert.Equal("y", post.AuthorName);
		}
	}
}
