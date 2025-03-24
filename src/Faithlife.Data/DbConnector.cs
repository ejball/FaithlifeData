using System.Data;
using Faithlife.Data.SqlFormatting;

namespace Faithlife.Data;

/// <summary>
/// Encapsulates a database connection and any current transaction.
/// </summary>
public sealed class DbConnector : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// Creates a new DbConnector.
	/// </summary>
	/// <param name="connection">The database connection.</param>
	/// <param name="settings">The settings.</param>
	public static DbConnector Create(IDbConnection connection, DbConnectorSettings? settings = null) =>
		new DbConnector(connection, settings);

	/// <summary>
	/// Creates a new DbConnector.
	/// </summary>
	/// <param name="connection">The database connection.</param>
	/// <param name="settings">The settings.</param>
	public DbConnector(IDbConnection connection, DbConnectorSettings? settings = null)
	{
		settings ??= s_defaultSettings;
		m_connection = connection ?? throw new ArgumentNullException(nameof(connection));
		m_isConnectionOpen = m_connection.State == ConnectionState.Open;
		m_noCloseConnection = m_isConnectionOpen;
		m_transaction = settings.CurrentTransaction;
		m_noDisposeTransaction = m_transaction is not null;
		m_noDisposeConnection = m_noDisposeTransaction || settings.NoDispose;
		m_whenDisposed = settings.WhenDisposed;
		m_providerMethods = settings.ProviderMethods ?? DbProviderMethods.Default;
		m_defaultIsolationLevel = settings.DefaultIsolationLevel;
		SqlSyntax = settings.SqlSyntax ?? SqlSyntax.Default;
		DataMapper = settings.DataMapper ?? DbDataMapper.Default;
	}

	/// <summary>
	/// The database connection.
	/// </summary>
	/// <remarks>Use <see cref="GetOpenConnectionAsync" /> or <see cref="GetOpenConnection" />
	/// to automatically open the connection if necessary.</remarks>
	public IDbConnection Connection => m_connection;

	/// <summary>
	/// The current transaction, if any.
	/// </summary>
	public IDbTransaction? CurrentTransaction => m_transaction;

	/// <summary>
	/// The SQL syntax used when formatting SQL.
	/// </summary>
	public SqlSyntax SqlSyntax { get; }

	/// <summary>
	/// Maps data record values to objects.
	/// </summary>
	public DbDataMapper DataMapper { get; }

	/// <summary>
	/// Returns the database connection, opened if necessary.
	/// </summary>
	/// <returns>The opened database connection.</returns>
	/// <seealso cref="Connection" />
	/// <seealso cref="GetOpenConnectionAsync" />
	public IDbConnection GetOpenConnection()
	{
		VerifyNotDisposed();
		return m_isConnectionOpen ? m_connection : DoOpenConnection();

		IDbConnection DoOpenConnection()
		{
			m_providerMethods.OpenConnection(m_connection);
			m_isConnectionOpen = true;
			return m_connection;
		}
	}

	/// <summary>
	/// Returns the database connection, opened if necessary.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The opened database connection.</returns>
	/// <seealso cref="Connection" />
	/// <seealso cref="GetOpenConnection" />
	public ValueTask<IDbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
	{
		VerifyNotDisposed();
		return m_isConnectionOpen ? new ValueTask<IDbConnection>(m_connection) : DoOpenConnectionAsync();

		async ValueTask<IDbConnection> DoOpenConnectionAsync()
		{
			await m_providerMethods.OpenConnectionAsync(m_connection, cancellationToken).ConfigureAwait(false);
			m_isConnectionOpen = true;
			return m_connection;
		}
	}

	/// <summary>
	/// Opens the connection.
	/// </summary>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the connection should be closed.</returns>
	/// <seealso cref="OpenConnectionAsync" />
	public DbConnectionCloser OpenConnection()
	{
		VerifyNotDisposed();

		if (!m_isConnectionOpen)
		{
			m_providerMethods.OpenConnection(m_connection);
			m_isConnectionOpen = true;
		}

		return new ConnectionCloser(this);
	}

	/// <summary>
	/// Opens the connection.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the connection should be closed.</returns>
	/// <seealso cref="OpenConnection" />
	public async ValueTask<DbConnectionCloser> OpenConnectionAsync(CancellationToken cancellationToken = default)
	{
		VerifyNotDisposed();

		if (!m_isConnectionOpen)
		{
			await m_providerMethods.OpenConnectionAsync(m_connection, cancellationToken).ConfigureAwait(false);
			m_isConnectionOpen = true;
		}

		return new ConnectionCloser(this);
	}

	/// <summary>
	/// Begins a transaction.
	/// </summary>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	/// <seealso cref="BeginTransactionAsync(CancellationToken)" />
	public DbTransactionDisposer BeginTransaction()
	{
		VerifyCanBeginTransaction();
		m_transaction = m_defaultIsolationLevel is { } isolationLevel
			? GetOpenConnection().BeginTransaction(isolationLevel)
			: GetOpenConnection().BeginTransaction();
		return new TransactionDisposer(this);
	}

	/// <summary>
	/// Begins a transaction.
	/// </summary>
	/// <param name="isolationLevel">The isolation level.</param>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	/// <seealso cref="BeginTransactionAsync(IsolationLevel, CancellationToken)" />
	public DbTransactionDisposer BeginTransaction(IsolationLevel isolationLevel)
	{
		VerifyCanBeginTransaction();
		m_transaction = GetOpenConnection().BeginTransaction(isolationLevel);
		return new TransactionDisposer(this);
	}

	/// <summary>
	/// Begins a transaction.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	/// <seealso cref="BeginTransaction()" />
	public async ValueTask<DbTransactionDisposer> BeginTransactionAsync(CancellationToken cancellationToken = default)
	{
		VerifyCanBeginTransaction();
		m_transaction = m_defaultIsolationLevel is { } isolationLevel
			? await m_providerMethods.BeginTransactionAsync(await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false), isolationLevel, cancellationToken).ConfigureAwait(false)
			: await m_providerMethods.BeginTransactionAsync(await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
		return new TransactionDisposer(this);
	}

	/// <summary>
	/// Begins a transaction.
	/// </summary>
	/// <param name="isolationLevel">The isolation level.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	/// <seealso cref="BeginTransaction(IsolationLevel)" />
	public async ValueTask<DbTransactionDisposer> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
	{
		VerifyCanBeginTransaction();
		m_transaction = await m_providerMethods.BeginTransactionAsync(await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false), isolationLevel, cancellationToken).ConfigureAwait(false);
		return new TransactionDisposer(this);
	}

	/// <summary>
	/// Attaches a transaction.
	/// </summary>
	/// <returns>An <see cref="IDisposable" /> that should be disposed when the transaction has been committed or should be rolled back.</returns>
	public DbTransactionDisposer AttachTransaction(IDbTransaction transaction)
	{
		VerifyCanBeginTransaction();
		m_transaction = transaction;
		return new TransactionDisposer(this);
	}

	/// <summary>
	/// Commits the current transaction.
	/// </summary>
	/// <seealso cref="CommitTransactionAsync" />
	public void CommitTransaction()
	{
		VerifyGetTransaction().Commit();
		DisposeTransaction();
	}

	/// <summary>
	/// Commits the current transaction.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <seealso cref="CommitTransaction" />
	public async ValueTask CommitTransactionAsync(CancellationToken cancellationToken = default)
	{
		await m_providerMethods.CommitTransactionAsync(VerifyGetTransaction(), cancellationToken).ConfigureAwait(false);
		await DisposeTransactionAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Rolls back the current transaction.
	/// </summary>
	/// <seealso cref="RollbackTransactionAsync" />
	public void RollbackTransaction()
	{
		VerifyGetTransaction().Rollback();
		DisposeTransaction();
	}

	/// <summary>
	/// Rolls back the current transaction.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <seealso cref="RollbackTransaction" />
	public async ValueTask RollbackTransactionAsync(CancellationToken cancellationToken = default)
	{
		await m_providerMethods.RollbackTransactionAsync(VerifyGetTransaction(), cancellationToken).ConfigureAwait(false);
		await DisposeTransactionAsync().ConfigureAwait(false);
	}

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
	public void CloseConnection()
	{
		VerifyNotDisposed();

		if (m_isConnectionOpen && !m_noCloseConnection)
		{
			m_connection.Close();
			m_isConnectionOpen = false;
		}
	}

	/// <summary>
	/// Closes the connection.
	/// </summary>
	/// <remarks>This method closes the underlying connection, which will be automatically reopened it if it is used again.</remarks>
	public async ValueTask CloseConnectionAsync()
	{
		VerifyNotDisposed();

		if (m_isConnectionOpen && !m_noCloseConnection)
		{
			await m_providerMethods.CloseConnectionAsync(m_connection).ConfigureAwait(false);
			m_isConnectionOpen = false;
		}
	}

	/// <summary>
	/// Disposes the connector.
	/// </summary>
	/// <seealso cref="DisposeAsync" />
	public void Dispose()
	{
		if (ConnectorPool is not null)
		{
			DisposeTransaction();
			ConnectorPool.ReturnConnector(this);
			return;
		}

		if (!m_isDisposed)
		{
			DisposeTransaction();

			DisposeCachedCommands();

			if (!m_noDisposeConnection)
				m_connection.Dispose();

			m_whenDisposed?.Invoke();

			m_isDisposed = true;
		}
	}

	/// <summary>
	/// Disposes the connector.
	/// </summary>
	/// <seealso cref="Dispose" />
	public async ValueTask DisposeAsync()
	{
		if (ConnectorPool is not null)
		{
			ConnectorPool.ReturnConnector(this);
			ConnectorPool = null;
			return;
		}

		if (!m_isDisposed)
		{
			await DisposeTransactionAsync().ConfigureAwait(false);

			await DisposeCachedCommandsAsync().ConfigureAwait(false);

			if (!m_noDisposeConnection)
				await m_providerMethods.DisposeConnectionAsync(m_connection).ConfigureAwait(false);

			m_whenDisposed?.Invoke();

			m_isDisposed = true;
		}
	}

	/// <summary>
	/// Special methods provided by the database provider.
	/// </summary>
	public DbProviderMethods ProviderMethods => m_providerMethods;

	/// <summary>
	/// Gets the command cache, if supported.
	/// </summary>
	public DbCommandCache CommandCache => m_commandCache ??= DbCommandCache.Create();

	internal DbConnectorPool? ConnectorPool { get; set; }

	private void DisposeTransaction()
	{
		VerifyNotDisposed();

		if (!m_noDisposeTransaction)
			m_transaction?.Dispose();
		m_transaction = null;
	}

	private async ValueTask DisposeTransactionAsync()
	{
		VerifyNotDisposed();

		if (!m_noDisposeTransaction && m_transaction is not null)
			await m_providerMethods.DisposeTransactionAsync(m_transaction).ConfigureAwait(false);
		m_transaction = null;
	}

	private void DisposeCachedCommands()
	{
		if (m_commandCache is not null)
		{
			foreach (var command in m_commandCache.GetCommands())
				CachedCommand.Unwrap(command).Dispose();
		}
	}

	private async ValueTask DisposeCachedCommandsAsync()
	{
		if (m_commandCache is not null)
		{
			foreach (var command in m_commandCache.GetCommands())
				await m_providerMethods.DisposeCommandAsync(CachedCommand.Unwrap(command)).ConfigureAwait(false);
		}
	}

	private void VerifyNotDisposed()
	{
		if (m_isDisposed)
			throw new ObjectDisposedException(typeof(DbConnector).ToString());
	}

	private void VerifyCanBeginTransaction()
	{
		VerifyNotDisposed();

		if (m_transaction is not null)
			throw new InvalidOperationException("A transaction is already started.");
	}

	private IDbTransaction VerifyGetTransaction()
	{
		VerifyNotDisposed();

		if (m_transaction is null)
			throw new InvalidOperationException("No transaction available; call BeginTransaction first.");

		return m_transaction;
	}

	private sealed class ConnectionCloser : DbConnectionCloser
	{
		public ConnectionCloser(DbConnector connector) => m_connector = connector;

		public override void Dispose()
		{
			if (m_connector is not null)
			{
				m_connector.CloseConnection();
				m_connector = null;
			}
		}

		public override async ValueTask DisposeAsync()
		{
			if (m_connector is not null)
			{
				await m_connector.CloseConnectionAsync().ConfigureAwait(false);
				m_connector = null;
			}
		}

		private DbConnector? m_connector;
	}

	private sealed class TransactionDisposer : DbTransactionDisposer
	{
		public TransactionDisposer(DbConnector connector) => m_connector = connector;

		public override void Dispose()
		{
			if (m_connector is not null)
			{
				m_connector.DisposeTransaction();
				m_connector = null;
			}
		}

		public override async ValueTask DisposeAsync()
		{
			if (m_connector is not null)
			{
				await m_connector.DisposeTransactionAsync().ConfigureAwait(false);
				m_connector = null;
			}
		}

		private DbConnector? m_connector;
	}

	private static readonly DbConnectorSettings s_defaultSettings = new();

	private readonly bool m_noDisposeConnection;
	private readonly bool m_noDisposeTransaction;
	private readonly bool m_noCloseConnection;
	private readonly DbProviderMethods m_providerMethods;
	private readonly IsolationLevel? m_defaultIsolationLevel;
	private readonly Action? m_whenDisposed;
	private readonly IDbConnection m_connection;
	private IDbTransaction? m_transaction;
	private DbCommandCache? m_commandCache;
	private bool m_isConnectionOpen;
	private bool m_isDisposed;
}
