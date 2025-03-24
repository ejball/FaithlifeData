using System.Data;
using Faithlife.Data.SqlFormatting;

namespace Faithlife.Data;

/// <summary>
/// Encapsulates a database connection and any current transaction.
/// </summary>
public abstract class DbConnector : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// Creates a new DbConnector.
	/// </summary>
	/// <param name="connection">The database connection.</param>
	/// <param name="settings">The settings.</param>
	public static DbConnector Create(IDbConnection connection, DbConnectorSettings? settings = null) =>
		new StandardDbConnector(connection, settings ?? s_defaultSettings);

	/// <summary>
	/// The database connection.
	/// </summary>
	/// <remarks>Use <see cref="GetOpenConnectionAsync" /> or <see cref="GetOpenConnection" />
	/// to automatically open the connection if necessary.</remarks>
	public abstract IDbConnection Connection { get; }

	/// <summary>
	/// The current transaction, if any.
	/// </summary>
	public abstract IDbTransaction? CurrentTransaction { get; }

	/// <summary>
	/// The SQL syntax used when formatting SQL.
	/// </summary>
	public virtual SqlSyntax SqlSyntax => SqlSyntax.Default;

	/// <summary>
	/// Maps data record values to objects.
	/// </summary>
	public virtual DbDataMapper DataMapper => DbDataMapper.Default;

	/// <summary>
	/// Returns the database connection, opened if necessary.
	/// </summary>
	/// <returns>The opened database connection.</returns>
	/// <seealso cref="Connection" />
	/// <seealso cref="GetOpenConnectionAsync" />
	public abstract IDbConnection GetOpenConnection();

	/// <summary>
	/// Returns the database connection, opened if necessary.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The opened database connection.</returns>
	/// <seealso cref="Connection" />
	/// <seealso cref="GetOpenConnection" />
	public abstract ValueTask<IDbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Opens the connection.
	/// </summary>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the connection should be closed.</returns>
	/// <seealso cref="OpenConnectionAsync" />
	public abstract DbConnectionCloser OpenConnection();

	/// <summary>
	/// Opens the connection.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the connection should be closed.</returns>
	/// <seealso cref="OpenConnection" />
	public abstract ValueTask<DbConnectionCloser> OpenConnectionAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Begins a transaction.
	/// </summary>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	/// <seealso cref="BeginTransactionAsync(CancellationToken)" />
	public abstract DbTransactionDisposer BeginTransaction();

	/// <summary>
	/// Begins a transaction.
	/// </summary>
	/// <param name="isolationLevel">The isolation level.</param>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	/// <seealso cref="BeginTransactionAsync(IsolationLevel, CancellationToken)" />
	public abstract DbTransactionDisposer BeginTransaction(IsolationLevel isolationLevel);

	/// <summary>
	/// Begins a transaction.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	/// <seealso cref="BeginTransaction()" />
	public abstract ValueTask<DbTransactionDisposer> BeginTransactionAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Begins a transaction.
	/// </summary>
	/// <param name="isolationLevel">The isolation level.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	/// <seealso cref="BeginTransaction(IsolationLevel)" />
	public abstract ValueTask<DbTransactionDisposer> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

	/// <summary>
	/// Attaches a transaction.
	/// </summary>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	public abstract DbTransactionDisposer AttachTransaction(IDbTransaction transaction);

	/// <summary>
	/// Commits the current transaction.
	/// </summary>
	/// <seealso cref="CommitTransactionAsync" />
	public abstract void CommitTransaction();

	/// <summary>
	/// Commits the current transaction.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <seealso cref="CommitTransaction" />
	public abstract ValueTask CommitTransactionAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Rolls back the current transaction.
	/// </summary>
	/// <seealso cref="RollbackTransactionAsync" />
	public abstract void RollbackTransaction();

	/// <summary>
	/// Rolls back the current transaction.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <seealso cref="RollbackTransaction" />
	public abstract ValueTask RollbackTransactionAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Creates a new command.
	/// </summary>
	/// <param name="text">The text of the command.</param>
	public DbConnectorCommand Command(string text) => Command(text, DbParameters.Empty);

	/// <summary>
	/// Creates a new command.
	/// </summary>
	/// <param name="text">The text of the command.</param>
	/// <param name="parameters">The command parameters.</param>
	public DbConnectorCommand Command(string text, DbParameters parameters) =>
		new DbConnectorCommand(this, text, parameters, CommandType.Text, timeout: null, isCached: false, isPrepared: false);

	/// <summary>
	/// Creates a new command.
	/// </summary>
	/// <param name="text">The text of the command.</param>
	/// <param name="parameters">The command parameters.</param>
	public DbConnectorCommand Command(string text, params (string Name, object? Value)[] parameters) => Command(text, DbParameters.Create(parameters));

	/// <summary>
	/// Creates a new command from parameterized SQL.
	/// </summary>
	/// <param name="sql">The parameterized SQL.</param>
	public DbConnectorCommand Command(Sql sql)
	{
		var (sqlText, sqlParameters) = SqlSyntax.Render(sql);
		return Command(sqlText, sqlParameters);
	}

	/// <summary>
	/// Creates a new command from parameterized SQL.
	/// </summary>
	/// <param name="sql">The parameterized SQL.</param>
	/// <param name="parameters">Additional command parameters.</param>
	public DbConnectorCommand Command(Sql sql, DbParameters parameters)
	{
		var (text, sqlParameters) = SqlSyntax.Render(sql);
		return Command(text, sqlParameters.Add(parameters));
	}

	/// <summary>
	/// Creates a new command from parameterized SQL.
	/// </summary>
	/// <param name="sql">The parameterized SQL.</param>
	/// <param name="parameters">Additional command parameters.</param>
	public DbConnectorCommand Command(Sql sql, params (string Name, object? Value)[] parameters) => Command(sql, DbParameters.Create(parameters));

	/// <summary>
	/// Creates a new command from a formatted SQL string.
	/// </summary>
	/// <param name="sql">The formatted SQL string.</param>
	/// <remarks>Shorthand for <c>Command(Sql.Format($"..."))</c>.</remarks>
	public DbConnectorCommand CommandFormat(SqlFormatStringHandler sql) => Command(Sql.Format(sql));

	/// <summary>
	/// Creates a new command from a formatted SQL string.
	/// </summary>
	/// <param name="sql">The formatted SQL string.</param>
	/// <param name="parameters">Additional command parameters.</param>
	/// <remarks>Shorthand for <c>Command(Sql.Format($"..."), parameters)</c>.</remarks>
	public DbConnectorCommand CommandFormat(SqlFormatStringHandler sql, DbParameters parameters) => Command(Sql.Format(sql), parameters);

	/// <summary>
	/// Creates a new command from a formatted SQL string.
	/// </summary>
	/// <param name="sql">The formatted SQL string.</param>
	/// <param name="parameters">Additional command parameters.</param>
	/// <remarks>Shorthand for <c>Command(Sql.Format($"..."), parameters)</c>.</remarks>
	public DbConnectorCommand CommandFormat(SqlFormatStringHandler sql, params (string Name, object? Value)[] parameters) => Command(Sql.Format(sql), parameters);

	/// <summary>
	/// Creates a new command to access a stored procedure.
	/// </summary>
	/// <param name="name">The name of the stored procedure.</param>
	public DbConnectorCommand StoredProcedure(string name) => StoredProcedure(name, DbParameters.Empty);

	/// <summary>
	/// Creates a new command.
	/// </summary>
	/// <param name="name">The name of the stored procedure.</param>
	/// <param name="parameters">The command parameters.</param>
	public DbConnectorCommand StoredProcedure(string name, DbParameters parameters) =>
		new DbConnectorCommand(this, name, parameters, CommandType.StoredProcedure, timeout: null, isCached: false, isPrepared: false);

	/// <summary>
	/// Creates a new command.
	/// </summary>
	/// <param name="name">The name of the stored procedure.</param>
	/// <param name="parameters">The command parameters.</param>
	public DbConnectorCommand StoredProcedure(string name, params (string Name, object? Value)[] parameters) => StoredProcedure(name, DbParameters.Create(parameters));

	/// <summary>
	/// Closes the connection.
	/// </summary>
	/// <remarks>This method closes the underlying connection, which will be automatically reopened it if it is used again.</remarks>
	public virtual void CloseConnection() => throw new NotImplementedException();

	/// <summary>
	/// Closes the connection.
	/// </summary>
	/// <remarks>This method closes the underlying connection, which will be automatically reopened it if it is used again.</remarks>
	public virtual ValueTask CloseConnectionAsync() => throw new NotImplementedException();

	/// <summary>
	/// Disposes the connector.
	/// </summary>
	/// <seealso cref="DisposeAsync" />
	public abstract void Dispose();

	/// <summary>
	/// Disposes the connector.
	/// </summary>
	/// <seealso cref="Dispose" />
	public abstract ValueTask DisposeAsync();

	/// <summary>
	/// Special methods provided by the database provider.
	/// </summary>
	public abstract DbProviderMethods ProviderMethods { get; }

	/// <summary>
	/// Gets the command cache, if supported.
	/// </summary>
	public abstract DbCommandCache? CommandCache { get; }

	private static readonly DbConnectorSettings s_defaultSettings = new();
}
