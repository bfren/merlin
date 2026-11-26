// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Wrap.Testing;

namespace Merlin.Extensions.ReqExtensions_Tests;

public partial class Form_Tests
{
	public class Strings
	{
		[Fact]
		public async Task Binds_String()
		{
			// Arrange
			var title = Rnd.Str;

			// Act
			var result = await Bind($"title={title}");

			// Assert
			Assert.Equal(title, result.AssertOk().Title);
		}

		[Fact]
		public async Task Decodes_Value()
		{
			// Arrange

			// Act
			var result = await Bind("title=Hello%2C+world%21");

			// Assert
			Assert.Equal("Hello, world!", result.AssertOk().Title);
		}

		[Fact]
		public async Task Uses_Naming_Policy()
		{
			// Arrange
			var title = Rnd.Str;

			// Act
			var result = await Bind($"Title={title}");

			// Assert
			// the JSON name is "title" (camelCase), so "Title" is not that field
			Assert.Null(result.AssertOk().Title);
		}

		[Fact]
		public async Task Uses_JsonPropertyName()
		{
			// Arrange
			var author = Rnd.Str;

			// Act
			var result = await Bind($"author_name={author}");

			// Assert
			Assert.Equal(author, result.AssertOk().AuthorName);
		}

		[Fact]
		public async Task Ignores_Unknown_Fields()
		{
			// Arrange

			// Act
			var result = await Bind($"title=x&{Rnd.Str}={Rnd.Str}");

			// Assert
			Assert.Equal("x", result.AssertOk().Title);
		}

		[Theory]
		[InlineData("x\",\"views\":99,\"draft\":true,\"a\":\"")]
		[InlineData("}")]
		[InlineData("\\")]
		public async Task Writes_Values_As_Strings_Not_Raw_Json(string title)
		{
			// Arrange
			var body = $"title={Uri.EscapeDataString(title)}";

			// Act
			var result = await Bind(body);

			// Assert
			var post = result.AssertOk();
			Assert.Equal(title, post.Title);
			Assert.Equal(0, post.Views);
			Assert.False(post.Draft);
		}
	}
}
