using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests;

[TestFixture]
internal sealed class DbParametersTests
{
	[Test]
	public void Empty()
	{
		DbParameters.Empty.Count.Should().Be(0);
	}

	[Test]
	public void CreateSingle()
	{
		DbParameters.Create("one", 1).Enumerate().Should().Equal(("one", 1));
	}

	[Test]
	public void CreateFromPairParams()
	{
		DbParameters.Create().Count.Should().Be(0);
		DbParameters.Create(("one", 1)).Enumerate().Should().Equal(("one", 1));
		DbParameters.Create(("one", 1), ("two", 2L)).Enumerate().Should().Equal(("one", 1L), ("two", 2L));
		DbParameters.Create<object>(("one", 1), ("two", 2L)).Enumerate().Should().Equal(("one", 1), ("two", 2L));
		DbParameters.Create<object?>(("one", 1), ("null", null)).Enumerate().Should().Equal(("one", 1), ("null", null));
	}

	[Test]
	public void CreateFromPairList()
	{
		DbParameters.Create([("one", "1"), ("two", "2")]).Enumerate().Should().Equal(("one", "1"), ("two", "2"));
		DbParameters.Create([("one", 1), ("two", 2L)]).Enumerate().Should().Equal(("one", 1L), ("two", 2L));
		var array1 = new (string, object)[] { ("one", 1), ("two", 2L) };
		DbParameters.Create(array1).Enumerate().Should().Equal(("one", 1), ("two", 2L));
		var array2 = new (string, object?)[] { ("one", 1), ("two", 2L) };
		DbParameters.Create(array2).Enumerate().Should().Equal(("one", 1), ("two", 2L));
		var array3 = new[] { ("one", (object) 1), ("two", 2L) };
		DbParameters.Create(array3).Enumerate().Should().Equal(("one", 1), ("two", 2L));
		var array4 = new[] { ("one", (object?) 1), ("two", 2L) };
		DbParameters.Create(array4).Enumerate().Should().Equal(("one", 1), ("two", 2L));
	}

	[Test]
	public void CreateFromDictionary()
	{
		DbParameters.Create(new Dictionary<string, long> { { "one", 1 }, { "two", 2L } }).Enumerate().Should().Equal(("one", 1L), ("two", 2L));
		DbParameters.Create(new Dictionary<string, int?> { { "one", 1 }, { "null", null } }).Enumerate().Should().Equal(("one", 1), ("null", null));
		DbParameters.Create(new Dictionary<string, object> { { "one", 1 }, { "two", 2L } }).Enumerate().Should().Equal(("one", 1), ("two", 2L));
		DbParameters.Create(new Dictionary<string, object?> { { "one", 1 }, { "null", null } }).Enumerate().Should().Equal(("one", 1), ("null", null));
	}

	[Test]
	public void CreateFromDto()
	{
		DbParameters.Create(DbParameters.FromDto(new { one = 1 }), DbParameters.FromDto(new HasTwo())).Enumerate().Should().Equal(("one", 1), ("Two", 2));
	}

	[Test]
	public void CreateFromDtoNamed()
	{
		DbParameters.FromDto(new { one = 1, Two = 2 }).Named(x => $"it's {x}").Enumerate().Should().Equal(("it's one", 1), ("it's Two", 2));
	}

	[Test]
	public void CreateFromDtoWhere()
	{
		DbParameters.FromDto(new { one = 1, two = 2, three = 3 }).Where(x => x[0] == 't').Enumerate().Should().Equal(("two", 2), ("three", 3));
	}

	[Test]
	public void CreateFromDtoWhereNamed()
	{
		DbParameters.FromDto(new { one = 1, two = 2, three = 3 }).Where(x => x[0] == 't').Named(x => x.ToUpperInvariant()).Enumerate().Should().Equal(("TWO", 2), ("THREE", 3));
	}

	[Test]
	public void CreateFromDtoNamedWhere()
	{
		DbParameters.FromDto(new { one = 1, two = 2, three = 3 }).Named(x => x.ToUpperInvariant()).Where(x => x[0] == 'T').Enumerate().Should().Equal(("TWO", 2), ("THREE", 3));
	}

	[Test]
	public void Count()
	{
		DbParameters.Create(("one", 1)).Count.Should().Be(1);
	}

	[Test]
	public void Nulls()
	{
		Invoking(() => DbParameters.Create(null!)).Should().Throw<ArgumentNullException>();
		Invoking(() => DbParameters.Create(default((string, string)[])!)).Should().Throw<ArgumentNullException>();
		Invoking(() => DbParameters.Create(default(Dictionary<string, string>)!)).Should().Throw<ArgumentNullException>();
		Invoking(() => DbParameters.FromDto(default(object?))).Should().Throw<ArgumentNullException>();
	}

	private sealed class HasTwo
	{
		public int Two { get; } = 2;
	}
}
