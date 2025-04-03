using System.Data;

namespace Faithlife.Data;

internal sealed class SingleDbParameter<T>(string name, T value) : DbParameters
{
	internal override int CountCore(Func<string, bool>? filterName) => filterName is null || filterName(name) ? 1 : 0;

	internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName)
	{
		if (filterName is null || filterName(name))
			yield return (name, value);
	}

	internal override void ApplyCore(IDbCommand command, Func<string, bool>? filterName, DbProviderMethods providerMethods)
	{
		if (filterName is null || filterName(name))
		{
			if (value is IDataParameter dbParameter)
				dbParameter.ParameterName = name;
			else
				dbParameter = providerMethods.CreateParameter(command, name, value);

			command.Parameters.Add(dbParameter);
		}
	}

	internal override void ReapplyCore(IDbCommand command, int startIndex, Func<string, bool>? filterName, DbProviderMethods providerMethods)
	{
		if (filterName is null || filterName(name))
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
	}
}
