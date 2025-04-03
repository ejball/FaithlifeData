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

	internal override void ApplyCore(IDbCommand command, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName)
	{
		if (filterName is null || filterName(name))
		{
			var parameterName = transformName is null ? name : transformName(name);
			if (value is IDataParameter dbParameter)
				dbParameter.ParameterName = parameterName;
			else
				dbParameter = providerMethods.CreateParameter(command, parameterName, value);

			command.Parameters.Add(dbParameter);
		}
	}

	internal override void ReapplyCore(IDbCommand command, int startIndex, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName)
	{
		if (filterName is null || filterName(name))
		{
			var parameterName = transformName is null ? name : transformName(name);
			var dbParameter = command.Parameters[startIndex] as IDataParameter;
			if (dbParameter is null || dbParameter.ParameterName != parameterName)
			{
				try
				{
					dbParameter = command.Parameters[parameterName] as IDataParameter;
				}
				catch (Exception exception)
				{
					throw new InvalidOperationException($"Cached commands must always be executed with the same parameters (missing '{parameterName}').", exception);
				}
				if (dbParameter is null)
					throw new InvalidOperationException($"Cached commands must always be executed with the same parameters (missing '{parameterName}').");
			}

			providerMethods.SetParameterValue(dbParameter, value);
		}
	}
}
