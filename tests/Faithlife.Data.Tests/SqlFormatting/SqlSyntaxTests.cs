using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Faithlife.Data.SqlFormatting;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Faithlife.Data.Tests.SqlFormatting;

#pragma warning disable FL0014 // Interpolated strings for literals

[TestFixture]
[SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Testing.")]
internal sealed class SqlSyntaxTests
{
	[Test]
	public void NullSqlThrows()
	{
		Invoking(() => Render(null!)).Should().Throw<ArgumentNullException>();
	}

	[Test]
	public void EmptySql()
	{
		var (text, parameters) = Render(Sql.Empty);
		text.Should().Be("");
		parameters.Count.Should().Be(0);
	}

	[TestCase("")]
	[TestCase("select * from widgets")]
	public void RawSql(string raw)
	{
		var (text, parameters) = Render(Sql.Raw(raw));
		text.Should().Be(raw);
		parameters.Count.Should().Be(0);
	}

	[Test]
	public void ParamSql()
	{
		var (text, parameters) = Render(Sql.Param("xyzzy"));
		text.Should().Be("@ado0");
		parameters.Enumerate().Should().Equal(("ado0", "xyzzy"));
	}

	[Test]
	public void NamedParamSql()
	{
		var (text, parameters) = Render(Sql.Param("abccb", "xyzzy"));
		text.Should().Be("@abccb");
		parameters.Enumerate().Should().Equal(("abccb", "xyzzy"));
	}

	[Test]
	public void ParamOfSql()
	{
		Invoking(() => Render(Sql.Param(Sql.Raw("xyzzy")))).Should().Throw<ArgumentException>();
		Invoking(() => Render(Sql.Param("abccb", Sql.Raw("xyzzy")))).Should().Throw<ArgumentException>();
	}

	[Test]
	public void ListSql()
	{
		var (text, parameters) = Render(Sql.List(Sql.Param("one"), Sql.Param("two"), Sql.Raw("null")));
		text.Should().Be("@ado0, @ado1, null");
		parameters.Enumerate().Should().Equal(("ado0", "one"), ("ado1", "two"));
	}

	[Test]
	public void ListNone()
	{
		Invoking(() => Render(Sql.List())).Should().Throw<InvalidOperationException>();
		Invoking(() => Render(Sql.List(Sql.Empty))).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void TupleSql()
	{
		var (text, parameters) = Render(Sql.Tuple(Sql.Param("one"), Sql.Param("two"), Sql.Raw("null")));
		text.Should().Be("(@ado0, @ado1, null)");
		parameters.Enumerate().Should().Equal(("ado0", "one"), ("ado1", "two"));
	}

	[Test]
	public void TupleNone()
	{
		Invoking(() => Render(Sql.Tuple())).Should().Throw<InvalidOperationException>();
		Invoking(() => Render(Sql.Tuple(Sql.Empty))).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void ParamListSqlStrings()
	{
		var (text, parameters) = Render(Sql.ParamList(["one", "two", "three"]));
		text.Should().Be("@ado0, @ado1, @ado2");
		parameters.Enumerate().Should().Equal(("ado0", "one"), ("ado1", "two"), ("ado2", "three"));
	}

	[Test]
	public void ParamListSqlNumbers()
	{
		var (text, parameters) = Render(Sql.ParamList([1, 2]));
		text.Should().Be("@ado0, @ado1");
		parameters.Enumerate().Should().Equal(("ado0", 1), ("ado1", 2));
	}

	[Test]
	public void ParamListSqlMixedNumbers()
	{
		var (text, parameters) = Render(Sql.ParamList<object>([1, 2L]));
		text.Should().Be("@ado0, @ado1");
		parameters.Enumerate().Should().Equal(("ado0", 1), ("ado1", 2L));
	}

	[Test]
	public void ParamListSqlMixedObjects()
	{
		var (text, parameters) = Render(Sql.ParamList<object?>(["one", 2, null]));
		text.Should().Be("@ado0, @ado1, @ado2");
		parameters.Enumerate().Should().Equal(("ado0", "one"), ("ado1", 2), ("ado2", null));
	}

	[Test]
	public void ParamListNone()
	{
		Invoking(() => Render(Sql.ParamList<object?>([]))).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void ParamTupleSqlStrings()
	{
		var (text, parameters) = Render(Sql.ParamTuple(["one", "two", "three"]));
		text.Should().Be("(@ado0, @ado1, @ado2)");
		parameters.Enumerate().Should().Equal(("ado0", "one"), ("ado1", "two"), ("ado2", "three"));
	}

	[Test]
	public void ParamTupleSqlNumbers()
	{
		var (text, parameters) = Render(Sql.ParamTuple([1, 2]));
		text.Should().Be("(@ado0, @ado1)");
		parameters.Enumerate().Should().Equal(("ado0", 1), ("ado1", 2));
	}

	[Test]
	public void ParamTupleSqlMixedNumbers()
	{
		var (text, parameters) = Render(Sql.ParamTuple<object>([1, 2L]));
		text.Should().Be("(@ado0, @ado1)");
		parameters.Enumerate().Should().Equal(("ado0", 1), ("ado1", 2L));
	}

	[Test]
	public void ParamTupleSqlMixedObjects()
	{
		var (text, parameters) = Render(Sql.ParamTuple<object?>(["one", 2, null]));
		text.Should().Be("(@ado0, @ado1, @ado2)");
		parameters.Enumerate().Should().Equal(("ado0", "one"), ("ado1", 2), ("ado2", null));
	}

	[Test]
	public void ParamTupleNone()
	{
		Invoking(() => Render(Sql.ParamTuple<object?>([]))).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void FormatEmpty()
	{
		var (text, parameters) = Render(Sql.Format($""));
		text.Should().Be("");
		parameters.Count.Should().Be(0);
	}

	[Test]
	public void FormatNoArgs()
	{
		var (text, parameters) = Render(Sql.Format($"select * from widgets"));
		text.Should().Be("select * from widgets");
		parameters.Count.Should().Be(0);
	}

	[Test]
	public void FormatImplicitParam()
	{
		var (text, parameters) = Render(Sql.Format($"select * from widgets where id in ({42}, {-42})"));
		text.Should().Be("select * from widgets where id in (@ado0, @ado1)");
		parameters.Enumerate().Should().Equal(("ado0", 42), ("ado1", -42));
	}

	[TestCase(null)]
	[TestCase(42)]
	public void FormatSql(int? id)
	{
		var whereSql = id is null ? Sql.Empty : Sql.Format($"where id = {Sql.Param(id)}");
		var limit = 10;
		var (text, parameters) = Render(Sql.Format($"select * from {Sql.Raw("widgets")} {whereSql} limit {limit}"));
		if (id is null)
		{
			text.Should().Be("select * from widgets  limit @ado0");
			parameters.Enumerate().Should().Equal(("ado0", limit));
		}
		else
		{
			text.Should().Be("select * from widgets where id = @ado0 limit @ado1");
			parameters.Enumerate().Should().Equal(("ado0", id), ("ado1", limit));
		}
	}

	[Test]
	public void SameParamTwice()
	{
		var id = 42;
		var name = "xyzzy";
		var desc = "long description";
		var descParam = Sql.Param(desc);
		var (text, parameters) = Render(Sql.Format($"insert into widgets (Id, Name, Desc) values ({id}, {name}, {descParam}) on duplicate key update Name = {name}, Desc = {descParam}"));
		text.Should().Be("insert into widgets (Id, Name, Desc) values (@ado0, @ado1, @ado2) on duplicate key update Name = @ado3, Desc = @ado2");
		parameters.Enumerate().Should().Equal(("ado0", id), ("ado1", name), ("ado2", desc), ("ado3", name));
	}

	[Test]
	public void FormatBadFormat()
	{
#if NETSTANDARD2_0
		var tableName = "widgets";
		Invoking(() => Render(Sql.Format($"select * from {tableName:xyzzy}"))).Should().Throw<FormatException>();
#endif
	}

	[Test]
	public void JoinParams()
	{
		var (text, parameters) = Render(Sql.Join(", ", Sql.Param(42), Sql.Param(-42)));
		text.Should().Be("@ado0, @ado1");
		parameters.Enumerate().Should().Equal(("ado0", 42), ("ado1", -42));
	}

	[Test]
	public void JoinEnumerable()
	{
		Render(CreateSql(42, 24)).Text.Should().Be("select * from widgets where width = @ado0 and height = @ado1;");
		Render(CreateSql(null, 24)).Text.Should().Be("select * from widgets where height = @ado0;");
		Render(CreateSql(null, null)).Text.Should().Be("select * from widgets ;");

		Sql CreateSql(int? width, int? height)
		{
			var sqls = new List<Sql>();
			if (width is not null)
				sqls.Add(Sql.Format($"width = {width}"));
			if (height is not null)
				sqls.Add(Sql.Format($"height = {height}"));
			var whereSql = sqls.Count == 0 ? Sql.Empty : Sql.Format($"where {Sql.Join(" and ", sqls)}");
			return Sql.Format($"select * from widgets {whereSql};");
		}
	}

	[Test]
	public void JoinEmpty()
	{
		var (text, parameters) = Render(Sql.Join("/", Sql.Raw("one"), Sql.Empty, Sql.Raw("two")));
		text.Should().Be("one/two");
		parameters.Count.Should().Be(0);
	}

	[Test]
	public void AddFragments()
	{
		var (text, parameters) = Render(Sql.Format($"select {1};") + Sql.Format($"select {2};"));
		text.Should().Be("select @ado0;select @ado1;");
		parameters.Enumerate().Should().Equal(("ado0", 1), ("ado1", 2));
	}

	[Test]
	public void ConcatParams()
	{
		var (text, parameters) = Render(Sql.Concat(Sql.Format($"select {1};"), Sql.Format($"select {2};")));
		text.Should().Be("select @ado0;select @ado1;");
		parameters.Enumerate().Should().Equal(("ado0", 1), ("ado1", 2));
	}

	[Test]
	public void ConcatEnumerable()
	{
		var (text, parameters) = Render(Sql.Concat(Enumerable.Range(1, 2).Select(x => Sql.Format($"select {x};"))));
		text.Should().Be("select @ado0;select @ado1;");
		parameters.Enumerate().Should().Equal(("ado0", 1), ("ado1", 2));
	}

	[Test]
	public void LikeParamStartsWithSql()
	{
		var (text, parameters) = Render(Sql.LikeParamStartsWith("xy_zy"));
		text.Should().Be("@ado0");
		parameters.Enumerate().Should().Equal(("ado0", "xy\\_zy%"));
	}

	[Test]
	public void NameSql()
	{
		Invoking(() => SqlSyntax.Default.Render(Sql.Name("xyzzy"))).Should().Throw<InvalidOperationException>();
		SqlSyntax.MySql.Render(Sql.Name("x`y[z]z\"y")).Text.Should().Be("`x``y[z]z\"y`");
		SqlSyntax.Postgres.Render(Sql.Name("x`y[z]z\"y")).Text.Should().Be("\"x`y[z]z\"\"y\"");
		SqlSyntax.SqlServer.Render(Sql.Name("x`y[z]z\"y")).Text.Should().Be("[x`y[z]]z\"y]");
		SqlSyntax.Sqlite.Render(Sql.Name("x`y[z]z\"y")).Text.Should().Be("\"x`y[z]z\"\"y\"");
	}

	[Test]
	public void ColumnNamesAndValuesSql()
	{
		var syntax = SqlSyntax.MySql;

		syntax.Render(Sql.ColumnNames<ItemDto>()).Text.Should().Be("`ItemId`, `DisplayName`");

		var item = new ItemDto { Id = 3, DisplayName = "three" };
		var (text, parameters) = syntax.Render(Sql.Format($"insert into Items ({Sql.ColumnNames<ItemDto>()}) values ({Sql.ColumnParams(item)});"));
		text.Should().Be("insert into Items (`ItemId`, `DisplayName`) values (@ado0, @ado1);");
		parameters.Enumerate().Should().Equal(("ado0", item.Id), ("ado1", item.DisplayName));
	}

	[Test]
	public void TableColumnNamesAndValuesSql()
	{
		var syntax = SqlSyntax.MySql;
		syntax.Render(Sql.ColumnNames<ItemDto>("t")).Text.Should().Be("`t`.`ItemId`, `t`.`DisplayName`");
	}

	[Test]
	public void SnakeCaseNamesAndValuesSql()
	{
		var syntax = SqlSyntax.MySql.WithSnakeCaseColumnNames();
		syntax.Render(Sql.ColumnNames<ItemDto>("t")).Text.Should().Be("`t`.`ItemId`, `t`.`display_name`");
	}

	[Test]
	public void ColumnNamesAndValuesWhereSql()
	{
		var syntax = SqlSyntax.MySql;

		syntax.Render(Sql.ColumnNamesWhere<ItemDto>(x => x != nameof(ItemDto.Id))).Text.Should().Be("`DisplayName`");

		var item = new ItemDto { Id = 3, DisplayName = "three" };
		var (text, parameters) = syntax.Render(Sql.Format($@"
				insert into Items ({Sql.ColumnNamesWhere<ItemDto>(x => x is not nameof(ItemDto.Id))})
				values ({Sql.ColumnParamsWhere(item, x => x is not nameof(ItemDto.Id))});"));
		text.Should().Be(@"
				insert into Items (`DisplayName`)
				values (@ado0);");
		parameters.Enumerate().Should().Equal(("ado0", item.DisplayName));
	}

	[Test]
	public void TableColumnNamesAndValuesWhereSql()
	{
		var syntax = SqlSyntax.MySql;

		syntax.Render(Sql.ColumnNamesWhere<ItemDto>(x => x != nameof(ItemDto.Id), "t")).Text.Should().Be("`t`.`DisplayName`");

		var item = new ItemDto { Id = 3, DisplayName = "three" };
		var (text, parameters) = syntax.Render(Sql.Format($@"
				insert into Items ({Sql.ColumnNamesWhere<ItemDto>(x => x is not nameof(ItemDto.Id), "t")})
				values ({Sql.ColumnParamsWhere(item, x => x is not nameof(ItemDto.Id))});"));
		text.Should().Be(@"
				insert into Items (`t`.`DisplayName`)
				values (@ado0);");
		parameters.Enumerate().Should().Equal(("ado0", item.DisplayName));
	}

	[Test]
	public void ColumnNamesAndValuesWhereNoneException()
	{
		var syntax = SqlSyntax.MySql;

		Invoking(() => syntax.Render(Sql.ColumnNamesWhere<ItemDto>(_ => false))).Should().Throw<InvalidOperationException>();
		Invoking(() => syntax.Render(Sql.ColumnParamsWhere(new ItemDto(), _ => false))).Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void DtoParamNamesSql()
	{
		var syntax = SqlSyntax.MySql;

		syntax.Render(Sql.DtoParamNames<ItemDto>()).Text.Should().Be("@Id, @DisplayName");
		syntax.Render(Sql.DtoParamNames<ItemDto>(x => x + "_")).Text.Should().Be("@Id_, @DisplayName_");
	}

	[Test]
	public void DtoParamNamesWhereSql()
	{
		var syntax = SqlSyntax.MySql;

		syntax.Render(Sql.DtoParamNamesWhere<ItemDto>(NotId)).Text.Should().Be("@DisplayName");
		syntax.Render(Sql.DtoParamNamesWhere<ItemDto>(x => x + "_", NotId)).Text.Should().Be("@DisplayName_");

		static bool NotId(string x) => x != nameof(ItemDto.Id);
	}

	[TestCase("", "")]
	[TestCase("one", "one")]
	[TestCase("one,two", "one AND two")]
	[TestCase("one,two,three", "one and two and three", true)]
	public void And(string values, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowerCaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.And(values.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)));
		text.Should().Be(sql);
		parameters.Count.Should().Be(0);
	}

	[TestCase("", "")]
	[TestCase("one", "one")]
	[TestCase("one,two", "one OR two")]
	[TestCase("one,two,three", "one or two or three", true)]
	public void Or(string values, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowerCaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.Or(values.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)));
		text.Should().Be(sql);
		parameters.Count.Should().Be(0);
	}

	[Test]
	public void AndOrAnd()
	{
		var (text, parameters) = Render(Sql.And(Sql.Raw("one"), Sql.Or(Sql.Raw("two"), Sql.And(Sql.Raw("three")))));
		text.Should().Be("one AND (two OR three)");
		parameters.Count.Should().Be(0);
	}

	[TestCase("", "")]
	[TestCase("true", "WHERE true")]
	[TestCase("true", "where true", true)]
	public void Where(string condition, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowerCaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.Where(Sql.Raw(condition)));
		text.Should().Be(sql);
		parameters.Count.Should().Be(0);
	}

	[TestCase("", "")]
	[TestCase("x asc", "ORDER BY x asc")]
	[TestCase("x asc;y desc", "order by x asc, y desc", true)]
	public void OrderBy(string columns, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowerCaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.OrderBy(columns.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)));
		text.Should().Be(sql);
		parameters.Count.Should().Be(0);
	}

	[TestCase("", "")]
	[TestCase("x", "GROUP BY x")]
	[TestCase("x;y", "group by x, y", true)]
	public void GroupBy(string columns, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowerCaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.GroupBy(columns.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(Sql.Raw)));
		text.Should().Be(sql);
		parameters.Count.Should().Be(0);
	}

	[TestCase("", "")]
	[TestCase("x < 1", "HAVING x < 1")]
	[TestCase("x < 1", "having x < 1", true)]
	public void Having(string condition, string sql, bool lowercase = false)
	{
		var syntax = lowercase ? SqlSyntax.Default.WithLowerCaseKeywords() : SqlSyntax.Default;
		var (text, parameters) = syntax.Render(Sql.Having(Sql.Raw(condition)));
		text.Should().Be(sql);
		parameters.Count.Should().Be(0);
	}

	private static (string Text, DbParameters Parameters) Render(Sql sql) => SqlSyntax.Default.Render(sql);

	private sealed class ItemDto
	{
		[Column("ItemId")]
		public int Id { get; set; }

		public string? DisplayName { get; set; }
	}
}
