using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests;

[TestFixture]
internal sealed class DbDataMapperTests
{
	public DbDataMapper Mapper => DbDataMapper.Default;

	[Test]
	public void Strings()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		// get non-nulls
		reader.Read().Should().BeTrue();

		Mapper.Map<string>(reader, 0, 1).Should().Be(s_dto.TheText);

		// get nulls
		reader.Read().Should().BeTrue();

		Mapper.Map<string>(reader, 0, 1).Should().BeNull();
	}

	[Test]
	public void NonNullableScalars()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		// get non-nulls
		reader.Read().Should().BeTrue();

		Mapper.Map<long>(reader, 1, 1).Should().Be(s_dto.TheInteger);
		Mapper.Map<double>(reader, 2, 1).Should().Be(s_dto.TheReal);

		// get nulls
		reader.Read().Should().BeTrue();

		Invoking(() => Mapper.Map<long>(reader, 1, 1)).Should().Throw<InvalidOperationException>();
		Invoking(() => Mapper.Map<double>(reader, 2, 1)).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void NullableScalars()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		// get non-nulls
		reader.Read().Should().BeTrue();

		Mapper.Map<long?>(reader, 1, 1).Should().Be(s_dto.TheInteger);
		Mapper.Map<double?>(reader, 2, 1).Should().Be(s_dto.TheReal);

		// get nulls
		reader.Read().Should().BeTrue();

		Mapper.Map<long?>(reader, 1, 1).Should().BeNull();
		Mapper.Map<double?>(reader, 2, 1).Should().BeNull();
	}

	[Test]
	public void Enums()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		// get non-nulls
		reader.Read().Should().BeTrue();

		Mapper.Map<Answer>(reader, 1, 1).Should().Be(Answer.FortyTwo);
		Mapper.Map<Answer?>(reader, 1, 1).Should().Be(Answer.FortyTwo);

		// get nulls
		reader.Read().Should().BeTrue();

		Invoking(() => Mapper.Map<Answer>(reader, 1, 1)).Should().Throw<InvalidOperationException>();
		Mapper.Map<Answer?>(reader, 1, 1).Should().BeNull();
	}

	[Test]
	public void BadIndexCount()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		reader.Read().Should().BeTrue();

		Invoking(() => Mapper.Map<ItemDto>(reader, -1, 2)).Should().Throw<ArgumentException>();
		Invoking(() => Mapper.Map<ItemDto>(reader, 2, -1)).Should().Throw<ArgumentException>();
		Invoking(() => Mapper.Map<ItemDto>(reader, 4, 1)).Should().Throw<ArgumentException>();
		Invoking(() => Mapper.Map<ItemDto>(reader, 5, 0)).Should().Throw<ArgumentException>();
	}

	[Test]
	public void BadCast()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		reader.Read().Should().BeTrue();

		// TODO
	}

	[Test]
	public void BadFieldCount()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		reader.Read().Should().BeTrue();

		Invoking(() => Mapper.Map<(string, long)>(reader, 0, 1)).Should().Throw<InvalidOperationException>();
		Mapper.Map<(string?, long)>(reader, 0, 2).Should().Be((s_dto.TheText, s_dto.TheInteger));
		Invoking(() => Mapper.Map<(string, long)>(reader, 0, 3)).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void ByteArrayTests()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		// get non-nulls
		reader.Read().Should().BeTrue();

		Mapper.Map<byte[]>(reader, 3, 1).Should().Equal(s_dto.TheBlob);

		// get nulls
		reader.Read().Should().BeTrue();

		Mapper.Map<byte[]>(reader, 3, 1).Should().BeNull();
	}

	[Test]
	public void StreamTests()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		// get non-nulls
		reader.Read().Should().BeTrue();

		var bytes = new byte[100];
		using (var stream = Mapper.Map<Stream>(reader, 3, 1))
			stream.Read(bytes, 0, bytes.Length).Should().Be(s_dto.TheBlob!.Length);
		bytes.Take(s_dto.TheBlob!.Length).Should().Equal(s_dto.TheBlob);

		// get nulls
		reader.Read().Should().BeTrue();

		Mapper.Map<Stream>(reader, 3, 1).Should().BeNull();
	}

	[Test]
	public void TupleTests()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		// get non-nulls
		reader.Read().Should().BeTrue();

		Mapper.Map<(string?, long, double)>(reader, 0, 3)
			.Should().Be((s_dto.TheText, s_dto.TheInteger, s_dto.TheReal));

		// get nulls
		reader.Read().Should().BeTrue();

		Mapper.Map<(string?, long?, double?)>(reader, 0, 3)
			.Should().Be((null, null, null));
	}

	[Test]
	public void DtoTests()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		// get non-nulls
		reader.Read().Should().BeTrue();

		// DTO
		Mapper.Map<ItemDto>(reader, 0, 4).Should().BeEquivalentTo(s_dto);
		Mapper.Map<ItemDto>(reader, 0, 1).Should().BeEquivalentTo(new ItemDto { TheText = s_dto.TheText });
		Mapper.Map<ItemDto>(reader, 0, 0).Should().BeNull();
		Mapper.Map<ItemDto>(reader, 4, 0).Should().BeNull();

		// tuple with DTO
		var tuple = Mapper.Map<(string, ItemDto, byte[])>(reader, 0, 4);
		tuple.Item1.Should().Be(s_dto.TheText);
		tuple.Item2.Should().BeEquivalentTo(new ItemDto { TheInteger = s_dto.TheInteger, TheReal = s_dto.TheReal });
		tuple.Item3.Should().Equal(s_dto.TheBlob);

		// tuple with two DTOs (needs NULL terminator)
		Invoking(() => Mapper.Map<(ItemDto, ItemDto)>(reader, 0, 3)).Should().Throw<InvalidOperationException>();

		// get nulls
		reader.Read().Should().BeTrue();

		// all nulls returns null DTO
		Mapper.Map<ItemDto>(reader, 0, 4).Should().BeNull();
	}

	[Test]
	public void TwoDtos()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, null, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		// get non-nulls
		reader.Read().Should().BeTrue();

		// two DTOs
		var tuple = Mapper.Map<(ItemDto, ItemDto)>(reader, 0, 5);
		tuple.Item1.Should().BeEquivalentTo(new ItemDto { TheText = s_dto.TheText, TheInteger = s_dto.TheInteger });
		tuple.Item2.Should().BeEquivalentTo(new ItemDto { TheReal = s_dto.TheReal, TheBlob = s_dto.TheBlob });

		// get nulls
		reader.Read().Should().BeTrue();

		// two DTOs
		tuple = Mapper.Map<(ItemDto, ItemDto)>(reader, 0, 5);
		tuple.Item1.Should().BeNull();
		tuple.Item2.Should().BeNull();
	}

	[Test]
	public void TwoOneFieldDtos()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger from items;";
		using var reader = command.ExecuteReader();

		// get non-nulls
		reader.Read().Should().BeTrue();

		// two DTOs
		var tuple = Mapper.Map<(ItemDto, ItemDto)>(reader, 0, 2);
		tuple.Item1.Should().BeEquivalentTo(new ItemDto { TheText = s_dto.TheText });
		tuple.Item2.Should().BeEquivalentTo(new ItemDto { TheInteger = s_dto.TheInteger });

		// get nulls
		reader.Read().Should().BeTrue();

		// two DTOs
		tuple = Mapper.Map<(ItemDto, ItemDto)>(reader, 0, 2);
		tuple.Item1.Should().BeNull();
		tuple.Item2.Should().BeNull();
	}

	[Test]
	public void RecordTests()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		// get non-nulls
		reader.Read().Should().BeTrue();

		// record (compare by members because the TheBlob is compared by reference in .Equals())
		Mapper.Map<ItemRecord>(reader, 0, 4).Should().BeEquivalentTo(s_record, x => x.ComparingByMembers<ItemRecord>());
		Mapper.Map<ItemRecord>(reader, 0, 1).Should().BeEquivalentTo(new ItemRecord(s_record.TheText, default, default, default), x => x.ComparingByMembers<ItemRecord>());
		Mapper.Map<ItemRecord>(reader, 0, 0).Should().BeNull();
		Mapper.Map<ItemRecord>(reader, 4, 0).Should().BeNull();

		// tuple with record
		var tuple = Mapper.Map<(string, ItemRecord, byte[])>(reader, 0, 4);
		tuple.Item1.Should().Be(s_record.TheText);
		tuple.Item2.Should().BeEquivalentTo(new ItemRecord(default, s_record.TheInteger, s_record.TheReal, default), x => x.ComparingByMembers<ItemRecord>());
		tuple.Item3.Should().Equal(s_record.TheBlob);

		// tuple with two records (needs NULL terminator)
		Invoking(() => Mapper.Map<(ItemRecord, ItemRecord)>(reader, 0, 3)).Should().Throw<InvalidOperationException>();

		// get nulls
		reader.Read().Should().BeTrue();

		// all nulls returns null record
		Mapper.Map<ItemRecord>(reader, 0, 4).Should().BeNull();
	}

	[Test]
	public void CaseInsensitivePropertyName()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select thetext, THEinteger from items;";
		using var reader = command.ExecuteReader();

		reader.Read().Should().BeTrue();
		Mapper.Map<ItemDto>(reader, 0, 2)
			.Should().BeEquivalentTo(new ItemDto { TheText = s_dto.TheText, TheInteger = s_dto.TheInteger });
	}

	[Test]
	public void UnderscorePropertyName()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText as the_text, TheInteger as the_integer from items;";
		using var reader = command.ExecuteReader();

		reader.Read().Should().BeTrue();
		Mapper.Map<ItemDto>(reader, 0, 2)
			.Should().BeEquivalentTo(new ItemDto { TheText = s_dto.TheText, TheInteger = s_dto.TheInteger });
	}

	[Test]
	public void BadPropertyName()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger as Nope from items;";
		using var reader = command.ExecuteReader();

		reader.Read().Should().BeTrue();
		Invoking(() => Mapper.Map<ItemDto>(reader, 0, 2)).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void DynamicTests()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		// get non-nulls
		reader.Read().Should().BeTrue();

		// dynamic
		((string) Mapper.Map<dynamic>(reader, 0, 4).TheText).Should().Be(s_dto.TheText);
		((double) ((dynamic) Mapper.Map<object>(reader, 0, 4)).TheReal).Should().Be(s_dto.TheReal);

		// tuple with dynamic
		var tuple = Mapper.Map<(string, dynamic, byte[])>(reader, 0, 4);
		tuple.Item1.Should().Be(s_dto.TheText);
		((long) tuple.Item2.TheInteger).Should().Be(s_dto.TheInteger);
		tuple.Item3.Should().Equal(s_dto.TheBlob);

		// tuple with two dynamics (needs NULL terminator)
		Invoking(() => Mapper.Map<(dynamic, dynamic)>(reader, 0, 3)).Should().Throw<InvalidOperationException>();

		// get nulls
		reader.Read().Should().BeTrue();

		// all nulls returns null dynamic
		((object) Mapper.Map<dynamic>(reader, 0, 4)).Should().BeNull();
		Mapper.Map<object>(reader, 0, 4).Should().BeNull();
	}

	[Test]
	public void DictionaryTests()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		// get non-nulls
		reader.Read().Should().BeTrue();

		// dictionary
		((string) Mapper.Map<Dictionary<string, object?>>(reader, 0, 4)["TheText"]!).Should().Be(s_dto.TheText);
		((long) Mapper.Map<IDictionary<string, object?>>(reader, 0, 4)["TheInteger"]!).Should().Be(s_dto.TheInteger);
		((long) Mapper.Map<IReadOnlyDictionary<string, object?>>(reader, 0, 4)["TheInteger"]!).Should().Be(s_dto.TheInteger);
		((double) Mapper.Map<IDictionary>(reader, 0, 4)["TheReal"]!).Should().Be(s_dto.TheReal);

		// get nulls
		reader.Read().Should().BeTrue();

		// all nulls returns null dictionary
		Mapper.Map<IDictionary>(reader, 0, 4).Should().BeNull();
	}

	[Test]
	public void ObjectTests()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		// get non-nulls
		reader.Read().Should().BeTrue();

		// object/dynamic
		Mapper.Map<object>(reader, 0).Should().Be(s_dto.TheText);
		((double) Mapper.Map<dynamic>(reader, 2)).Should().Be(s_dto.TheReal);

		// tuple with object
		var tuple = Mapper.Map<(string, object, double)>(reader, 0, 3);
		tuple.Item1.Should().Be(s_dto.TheText);
		tuple.Item2.Should().Be(s_dto.TheInteger);
		tuple.Item3.Should().Be(s_dto.TheReal);

		// tuple with three objects (doesn't need NULL terminator when the field count matches exactly)
		var tuple2 = Mapper.Map<(object, object, object)>(reader, 0, 3);
		tuple2.Item1.Should().Be(s_dto.TheText);
		tuple2.Item2.Should().Be(s_dto.TheInteger);
		tuple2.Item3.Should().Be(s_dto.TheReal);

		// get nulls
		reader.Read().Should().BeTrue();

		// all nulls returns null dynamic
		Mapper.Map<object>(reader, 0).Should().BeNull();
		((object) Mapper.Map<dynamic>(reader, 0)).Should().BeNull();
	}

	[Test]
	public void GetExtension()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		reader.Read().Should().BeTrue();
		Mapper.Map<ItemDto>(reader).Should().BeEquivalentTo(s_dto);
	}

	[Test]
	public void GetAtExtension()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		reader.Read().Should().BeTrue();
		Mapper.Map<long>(reader, 1).Should().Be(s_dto.TheInteger);
	}

	[Test]
	public void GetRangeExtension()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		reader.Read().Should().BeTrue();
		Mapper.Map<(long, double)>(reader, 1, 2).Should().Be((s_dto.TheInteger, s_dto.TheReal));
	}

	[Test]
	public void CustomDtoTests()
	{
		using var connection = GetOpenConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "select TheText, TheInteger, TheReal, TheBlob from items;";
		using var reader = command.ExecuteReader();

		reader.Read().Should().BeTrue();
		Mapper.Map<CustomColumnDto>(reader, 0, 1).Should().BeEquivalentTo(new CustomColumnDto { Text = s_dto.TheText });
		reader.Read().Should().BeTrue();
		Mapper.Map<CustomColumnDto>(reader, 0, 1).Should().BeNull();
	}

	private static IDbConnection GetOpenConnection()
	{
		var connection = new SqliteConnection("Data Source=:memory:");
		connection.Open();

		using (var command = connection.CreateCommand())
		{
			command.CommandText = "create table Items (TheText text null, TheInteger integer null, TheReal real null, TheBlob blob null);";
			command.ExecuteNonQuery();
		}

		using (var command = connection.CreateCommand())
		{
			command.CommandText = "insert into Items (TheText, TheInteger, TheReal, TheBlob) values ('hey', 42, 3.1415, X'01FE');";
			command.ExecuteNonQuery();
		}

		using (var command = connection.CreateCommand())
		{
			command.CommandText = "insert into Items (TheText, TheInteger, TheReal, TheBlob) values (null, null, null, null);";
			command.ExecuteNonQuery();
		}

		return connection;
	}

	private sealed class ItemDto
	{
		public string? TheText { get; set; }
		public long TheInteger { get; set; }
		public double TheReal { get; set; }
		public byte[]? TheBlob { get; set; }
	}

	private sealed class CustomColumnDto
	{
		[Column("TheText")]
		public string? Text { get; set; }
	}

#pragma warning disable CA1801, SA1313
	private sealed record ItemRecord(string? TheText, long TheInteger, double TheReal, byte[]? TheBlob, long TheOptionalInteger = 42);
#pragma warning restore CA1801, SA1313

	private sealed record NonPositionalRecord
	{
		public string? TheText { get; set; }
	}

	private sealed class DtoWithConstructors
	{
		public DtoWithConstructors()
		{
		}

		public DtoWithConstructors(string theString)
		{
			TheText = theString;
		}

		public string? TheText { get; set; }
	}

	private enum Answer
	{
		FortyTwo = 42,
	}

	private static readonly ItemDto s_dto = new()
	{
		TheText = "hey",
		TheInteger = 42L,
		TheReal = 3.1415,
		TheBlob = new byte[] { 0x01, 0xFE },
	};

	private static readonly ItemRecord s_record = new(
		TheText: "hey",
		TheInteger: 42L,
		TheReal: 3.1415,
		TheBlob: new byte[] { 0x01, 0xFE });
}
