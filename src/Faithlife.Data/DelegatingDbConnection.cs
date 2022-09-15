using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Transactions;
using Faithlife.Data.SqlFormatting;
using IsolationLevel = System.Data.IsolationLevel;
#nullable disable

namespace Faithlife.Data;

/// <summary>
/// Delegates to an inner connection.
/// </summary>
public class DelegatingDbConnection : DbConnection
{
	/// <summary>
	/// Creates an instance that delegates to the specified connection.
	/// </summary>
	public DelegatingDbConnection(DbConnection inner)
	{
		Inner = inner;
	}

	/// <summary>
	/// The inner connection.
	/// </summary>
	protected DbConnection Inner { get; }

	/// <inheritdoc />
	public override Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default) => Inner.ChangeDatabaseAsync(databaseName, cancellationToken);

	/// <inheritdoc />
	public override Task CloseAsync() => Inner.CloseAsync();

	/// <inheritdoc />
	public override void EnlistTransaction(Transaction transaction) => Inner.EnlistTransaction(transaction);

	/// <inheritdoc />
	public override DataTable GetSchema() => Inner.GetSchema();

	/// <inheritdoc />
	public override DataTable GetSchema(string collectionName) => Inner.GetSchema(collectionName);

	/// <inheritdoc />
	public override DataTable GetSchema(string collectionName, string[] restrictionValues) => Inner.GetSchema(collectionName, restrictionValues);

	/// <inheritdoc />
	public override Task OpenAsync(CancellationToken cancellationToken) => Inner.OpenAsync(cancellationToken);

	/// <inheritdoc />
	protected override ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken) => Inner.BeginTransactionAsync(isolationLevel, cancellationToken);

	/// <inheritdoc />
	public override ValueTask DisposeAsync() => Inner.DisposeAsync();

	/// <inheritdoc />
	public override int ConnectionTimeout => Inner.ConnectionTimeout;

	/// <inheritdoc />
	protected override DbProviderFactory DbProviderFactory => Inner.DbProviderFactory;

	/// <inheritdoc />
	public override event StateChangeEventHandler StateChange
	{
		add => Inner.StateChange += value;
		remove => Inner.StateChange -= value;
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing) => Inner.Dispose(disposing);

	/// <inheritdoc />
	protected override object GetService(Type service) => Inner.GetService(service);

	/// <inheritdoc />
	public override string ToString() => Inner.ToString();

	/// <inheritdoc />
	protected override bool CanRaiseEvents => Inner.CanRaiseEvents;

	/// <inheritdoc />
	public override ISite Site
	{
		get => Inner.Site;
		set => Inner.Site = value;
	}

	/// <inheritdoc />
	public override object InitializeLifetimeService() => Inner.InitializeLifetimeService();

	/// <inheritdoc />
	public override bool Equals(object obj) => Inner.Equals(obj);

	/// <inheritdoc />
	public override int GetHashCode() => Inner.GetHashCode();

	/// <inheritdoc />
	protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => Inner.BeginDbTransaction(isolationLevel);

	/// <inheritdoc />
	public override void ChangeDatabase(string databaseName) => Inner.ChangeDatabase(databaseName);

	/// <inheritdoc />
	public override void Close() => Inner.Close();

	/// <inheritdoc />
	public override void Open() => Inner.Open();

	/// <inheritdoc />
	public override string ConnectionString
	{
		get => Inner.ConnectionString;
		set => Inner.ConnectionString = value;
	}

	/// <inheritdoc />
	public override string Database => Inner.Database;

	/// <inheritdoc />
	public override ConnectionState State => Inner.State;

	/// <inheritdoc />
	public override string DataSource => Inner.DataSource;

	/// <inheritdoc />
	public override string ServerVersion => Inner.ServerVersion;

	/// <inheritdoc />
	protected override DbCommand CreateDbCommand() => Inner.CreateCommand();
}
