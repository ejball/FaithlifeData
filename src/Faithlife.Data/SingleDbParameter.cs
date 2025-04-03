using System.Data;

namespace Faithlife.Data;

internal sealed class SingleDbParameter<T>(string name, T value) : DbParameters
{
	public override int Count => 1;

	internal override void Apply(IDbCommand command, DbProviderMethods providerMethods)
	{
		if (value is IDataParameter dbParameter)
			dbParameter.ParameterName = name;
		else
			dbParameter = providerMethods.CreateParameter(command, name, value);

		command.Parameters.Add(dbParameter);
	}

	internal override void Reapply(IDbCommand command, int startIndex, DbProviderMethods providerMethods)
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

		providerMethods.SetParameterValue(dbParameter, value);
	}

	public override IEnumerable<(string Name, object? Value)> Enumerate()
	{
		yield return (name, value);
	}
}
