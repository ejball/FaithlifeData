using System.Text;
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
		m_connector.Command("create table DataMapperBenchmark (ItemId integer primary key, Number integer not null);").Execute();

		const int recordCount = 50000;
		m_connector
			.Command("insert into DataMapperBenchmark (AnInteger, AReal, AString, ABlob) values (@AnInteger, @AReal, @AString, @ABlob)...;")
			.BulkInsert(Enumerable.Range(1, recordCount)
				.Select(x => DbParameters.Create(
					("AnInteger", x < recordCount ? x : null),
					("AReal", x < recordCount ? 1.0 / x : null),
					("AString", x < recordCount ? $"string{x}" : null),
					("ABlob", x < recordCount ? Encoding.UTF8.GetBytes($"blob{x}") : null))));
	}

	[Benchmark]
	public long Int64() => m_connector.Command("select AnInteger from DataMapperBenchmark;").Enumerate<long>().Last();

	[Benchmark]
	public long? NullableInt64() => m_connector.Command("select AnInteger from DataMapperBenchmark;").Enumerate<long?>().Last();

	public void Dispose() => m_connector.Dispose();

	private readonly DbConnector m_connector;
}
