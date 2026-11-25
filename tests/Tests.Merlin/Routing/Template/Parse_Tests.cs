// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

namespace Merlin.Routing.Template_Tests;

public class Parse_Tests
{
	public class Valid
	{
		[Theory]
		[InlineData("/")]
		[InlineData("")]
		[InlineData("//")]
		public void Root_Has_No_Segments(string source)
		{
			// Arrange

			// Act
			var result = Template.Parse(source, new());

			// Assert
			Assert.Empty(result.Segments);
		}

		[Fact]
		public void Keeps_Source()
		{
			// Arrange
			var source = $"/{Rnd.Str}/{{id:int}}";

			// Act
			var result = Template.Parse(source, new());

			// Assert
			Assert.Equal(source, result.Source);
		}

		[Fact]
		public void Parses_Literals()
		{
			// Arrange
			var a = Rnd.Str;
			var b = Rnd.Str;

			// Act
			var result = Template.Parse($"/{a}/{b}", new());

			// Assert
			Assert.Equal([new Literal(a), new Literal(b)], result.Segments);
		}

		[Fact]
		public void Ignores_Empty_Parts_And_Whitespace()
		{
			// Arrange

			// Act
			var result = Template.Parse("// posts /", new());

			// Assert
			Assert.Equal(new Literal("posts"), Assert.Single(result.Segments));
		}

		[Fact]
		public void Untyped_Param_Is_String()
		{
			// Arrange
			var types = new ParamTypes();

			// Act
			var result = Template.Parse("/{slug}", types);

			// Assert
			var param = Assert.IsType<Param>(Assert.Single(result.Segments));
			Assert.Equal("slug", param.Name);
			Assert.Equal(types.Get("string"), param.Type);
		}

		[Theory]
		[InlineData("int", typeof(int))]
		[InlineData("long", typeof(long))]
		[InlineData("guid", typeof(Guid))]
		[InlineData("bool", typeof(bool))]
		[InlineData("dateonly", typeof(DateOnly))]
		[InlineData("string", typeof(string))]
		[InlineData("INT", typeof(int))]
		public void Typed_Param_Uses_Param_Type(string type, Type expected)
		{
			// Arrange

			// Act
			var result = Template.Parse($"/{{id:{type}}}", new());

			// Assert
			var param = Assert.IsType<Param>(Assert.Single(result.Segments));
			Assert.Equal("id", param.Name);
			Assert.Equal(expected, param.Type.Type);
		}

		[Fact]
		public void Uses_Custom_Param_Type()
		{
			// Arrange
			var types = new ParamTypes();
			types.Add<decimal>("money");

			// Act
			var result = Template.Parse("/{price:money}", types);

			// Assert
			var param = Assert.IsType<Param>(Assert.Single(result.Segments));
			Assert.Equal(typeof(decimal), param.Type.Type);
		}

		[Fact]
		public void Last_Catch_All_Is_Parsed()
		{
			// Arrange

			// Act
			var result = Template.Parse("/files/{path:*}", new());

			// Assert
			Assert.Equal([new Literal("files"), new CatchAll("path")], result.Segments);
		}

		[Fact]
		public void Different_Param_Names_Are_Allowed()
		{
			// Arrange

			// Act
			var result = Template.Parse("/{year:int}/{slug}/{path:*}", new());

			// Assert
			Assert.Equal(3, result.Segments.Length);
		}
	}

	public class Invalid
	{
		[Fact]
		public void Catch_All_Not_Last_Throws()
		{
			// Arrange

			// Act
			var result = Record.Exception(() => Template.Parse("/{path:*}/x", new()));

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			Assert.Contains("last segment", ex.Message);
		}

		[Theory]
		[InlineData("/{}")]
		[InlineData("/{:int}")]
		[InlineData("/{:*}")]
		[InlineData("/{a:int:x}")]
		public void Invalid_Param_Throws(string source)
		{
			// Arrange

			// Act
			var result = Record.Exception(() => Template.Parse(source, new()));

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			Assert.Contains("invalid parameter", ex.Message);
		}

		[Theory]
		[InlineData("/{id")]
		[InlineData("/id}")]
		[InlineData("/a{id}")]
		[InlineData("/{id}a")]
		public void Unbalanced_Braces_Throw(string source)
		{
			// Arrange

			// Act
			var result = Record.Exception(() => Template.Parse(source, new()));

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			Assert.Contains("unbalanced braces", ex.Message);
		}

		[Fact]
		public void Unknown_Param_Type_Throws()
		{
			// Arrange
			var type = Rnd.Str;

			// Act
			var result = Record.Exception(() => Template.Parse($"/{{id:{type}}}", new()));

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			Assert.Contains(type, ex.Message);
		}

		[Theory]
		[InlineData("/{id}/{id:int}")]
		[InlineData("/{id}/{ID}")]
		[InlineData("/{path}/{path:*}")]
		public void Repeated_Param_Name_Throws(string source)
		{
			// Arrange

			// Act
			var result = Record.Exception(() => Template.Parse(source, new()));

			// Assert
			var ex = Assert.IsType<InvalidOperationException>(result);
			Assert.Contains("repeats a parameter name", ex.Message);
		}
	}
}
