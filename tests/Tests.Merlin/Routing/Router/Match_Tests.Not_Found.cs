// Merlin: Unit Tests
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Merlin.Routing.Router_Tests;

public partial class Match_Tests
{
	public class Not_Found
	{
		[Fact]
		public void Returns_NotFound_When_No_Route_Matches()
		{
			// Arrange
			var router = new Router();
			router.Map("/posts").Get(NewHandler());

			router.Lock(Sp, new ServiceCollection());

			// Act
			var result = router.Match(Method.Get, "/other");

			// Assert
			Assert.IsType<NotFound>(result);
		}

		[Fact]
		public void Returns_NotFound_When_No_Routes_Are_Mapped()
		{
			// Arrange
			var router = new Router();

			router.Lock(Sp, new ServiceCollection());

			// Act
			var result = router.Match(Method.Get, "/posts");

			// Assert
			Assert.IsType<NotFound>(result);
		}

		[Fact]
		public void Returns_NotFound_When_Route_Has_No_Methods()
		{
			// Arrange
			var router = new Router();
			router.Map("/posts");

			router.Lock(Sp, new ServiceCollection());

			// Act
			var result = router.Match(Method.Get, "/posts");

			// Assert
			Assert.IsType<NotFound>(result);
		}
	}
}
