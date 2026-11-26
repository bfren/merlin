// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json;
using Merlin.Replies;

namespace Merlin.App_Tests;

public class Json_Tests
{
	[Fact]
	public void Resolves_Merlin_Types()
	{
		// Arrange
		var app = new App([]);

		// Act
		var result = Record.Exception(() => app.Json.GetTypeInfo<Problem>());

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void Writes_Problem_With_RFC_9457_Names()
	{
		// Arrange
		var app = new App([]);
		var info = app.Json.GetTypeInfo<Problem>();

		// Act
		var result = JsonSerializer.Serialize(new Problem { Title = "Not Found", Status = 404 }, info);

		// Assert
		Assert.Equal("""{"title":"Not Found","status":404}""", result);
	}
}
