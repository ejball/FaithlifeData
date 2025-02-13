using BenchmarkDotNet.Attributes;
using Faithlife.Data;
using Faithlife.Data.BulkInsert;
using Microsoft.Data.Sqlite;

namespace Benchmarks;

[MemoryDiagnoser]
public class DataMapperBenchmark : IDisposable
{
	protected DataMapperBenchmark()
	{
		m_connector = DbConnector.Create(new SqliteConnection("Data Source=:memory:"), new DbConnectorSettings { AutoOpen = true, LazyOpen = true });
		m_connector.Command("drop table if exists DataMapperBenchmark;").Execute();
		m_connector.Command("create table DataMapperBenchmark (ItemId integer primary key, IntegerValue integer not null);").Execute();

		m_connector
			.Command("insert into DataMapperBenchmark (IntegerValue) values (@IntegerValue)...;")
			.BulkInsert(Enumerable.Range(0, 50000).Select(x => DbParameters.Create("IntegerValue", x)));
	}

	[Benchmark]
	public long Int64() => m_connector.Command("select IntegerValue from DataMapperBenchmark;").Enumerate<long>().Last();

	[Benchmark]
	public long? NullableInt64() => m_connector.Command("select IntegerValue from DataMapperBenchmark;").Enumerate<long?>().Last();

	public void Dispose() => m_connector.Dispose();

	private readonly DbConnector m_connector;
}
