using System.Data;

namespace Faithlife.Data;

/// <summary>
/// An immutable list of parameters.
/// </summary>
internal sealed class StandardDbParameters : DbParameters
{
	public override void Apply(IDbCommand command)
	{
		foreach (var (name, value) in Parameters)
		{
			if (!(value is IDbDataParameter dbParameter))
			{
				dbParameter = command.CreateParameter();
				dbParameter.Value = value ?? DBNull.Value;
			}

			dbParameter.ParameterName = name;

			command.Parameters.Add(dbParameter);
		}
	}

	public override void Reapply(IDbCommand command, int startIndex)
	{
		var parameterCount = Parameters.Count;
		for (var parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
		{
			var (name, value) = Parameters[parameterIndex];
			var dbParameter = command.Parameters[startIndex + parameterIndex] as IDataParameter;
			if (dbParameter is null || dbParameter.ParameterName != name)
			{
				try
				{
					dbParameter = command.Parameters[name] as IDataParameter;
				}
				catch (Exception exception)
				{
					throw new InvalidOperationException($"Cached commands must always be executed with the same parameters (missing '{name}').", exception);
				}
				if (dbParameter is null)
					throw new InvalidOperationException($"Cached commands must always be executed with the same parameters (missing '{name}').");
			}
			dbParameter.Value = value is IDataParameter ddp ? ddp.Value : value;
		}
	}

	/// <inheritdoc />
	public override int Count => Parameters.Count;

	public StandardDbParameters(IEnumerable<(string Name, object? Value)> parameters) => m_parameters = parameters.ToList();

	private IReadOnlyList<(string Name, object? Value)> Parameters => m_parameters ?? [];

	private readonly IReadOnlyList<(string Name, object? Value)>? m_parameters;
}
