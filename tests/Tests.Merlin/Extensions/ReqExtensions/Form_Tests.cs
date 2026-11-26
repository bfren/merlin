// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Wrap.Testing;
using static Merlin.Extensions.ReqExtensions_Tests.Helpers;

namespace Merlin.Extensions.ReqExtensions_Tests;

public class Form_Tests
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

	public class Numbers
	{
		[Fact]
		public async Task Binds_Number_From_String()
		{
			// Arrange
			var views = Rnd.Int;

			// Act
			var result = await Bind($"views={views}");

			// Assert
			Assert.Equal(views, result.AssertOk().Views);
		}

		[Theory]
		[InlineData("abc")]
		[InlineData("1.5")]
		[InlineData("1,\"draft\":true")]
		public async Task Invalid_Number_Is_Invalid_Field(string views)
		{
			// Arrange

			// Act
			var result = await Bind($"views={Uri.EscapeDataString(views)}");

			// Assert
			AssertFailure(result, Wrap.FailureKind.Invalid, InvalidField, "views");
		}
	}

	public class Checkboxes
	{
		[Theory]
		[InlineData("on")]
		[InlineData("true")]
		[InlineData("1")]
		[InlineData("ON")]
		[InlineData("True")]
		public async Task Checked_Is_True(string value)
		{
			// Arrange

			// Act
			var result = await Bind($"draft={value}");

			// Assert
			Assert.True(result.AssertOk().Draft);
		}

		[Fact]
		public async Task Absent_Is_False()
		{
			// Arrange

			// Act
			var result = await Bind("title=x");

			// Assert
			Assert.False(result.AssertOk().Draft);
		}

		[Theory]
		[InlineData("off")]
		[InlineData("false")]
		[InlineData("0")]
		[InlineData("OFF")]
		[InlineData("False")]
		public async Task Unchecked_Is_False(string value)
		{
			// Arrange

			// Act
			var result = await Bind($"draft={value}");

			// Assert
			Assert.False(result.AssertOk().Draft);
		}

		[Theory]
		[InlineData("yes")]
		[InlineData("2")]
		[InlineData("")]
		public async Task Other_Value_Is_Invalid_Field(string value)
		{
			// Arrange

			// Act
			var result = await Bind($"draft={value}");

			// Assert
			// anything else fails rather than quietly becoming false
			AssertFailure(result, Wrap.FailureKind.Invalid, InvalidField, "draft");
		}
	}

	public class Arrays
	{
		[Fact]
		public async Task Binds_Repeated_Field_As_Array()
		{
			// Arrange

			// Act
			var result = await Bind("tags=a&tags=b&tags=c");

			// Assert
			Assert.Equal(["a", "b", "c"], result.AssertOk().Tags ?? []);
		}

		[Fact]
		public async Task Binds_Single_Value_As_Array()
		{
			// Arrange

			// Act
			var result = await Bind("tags=a");

			// Assert
			Assert.Equal(["a"], result.AssertOk().Tags ?? []);
		}

		[Fact]
		public async Task Absent_Is_Default()
		{
			// Arrange

			// Act
			var result = await Bind("title=x");

			// Assert
			// as in JSON: PostDto sets no default for Tags, so it stays null
			Assert.Null(result.AssertOk().Tags);
		}
	}

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
