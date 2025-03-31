using System.Data;

namespace Faithlife.Data;

internal sealed class OneDbParameter<T>(string name, T value) : DbParameters
{
	public override int Count => 1;

	public override void Apply(IDbCommand command)
	{
		if (value is not IDbDataParameter dbParameter)
		{
			dbParameter = command.CreateParameter();
			dbParameter.Value = value is null ? DBNull.Value : value;
		}

		dbParameter.ParameterName = name;

		command.Parameters.Add(dbParameter);
	}

	public override void Reapply(IDbCommand command, int startIndex)
	{
		var dbParameter = command.Parameters[startIndex] as IDataParameter;
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

	public override IEnumerable<(string Name, object? Value)> Enumerate()
	{
		yield return (name, value);
	}
}
