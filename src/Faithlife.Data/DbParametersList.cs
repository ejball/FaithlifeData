using System.Data;

namespace Faithlife.Data;

/// <summary>
/// A list of parameters.
/// </summary>
public sealed class DbParametersList : DbParameters
{
	public DbParametersList() => m_parametersList = [];

	public DbParametersList(params IEnumerable<DbParameters> parametersList) => m_parametersList = [.. parametersList];

	public void Add(DbParameters item)
	{
		VerifyNotReadOnly();
		m_parametersList.Add(item);
	}

	public override void Apply(IDbCommand command)
	{
		IsReadOnly = true;
		foreach (var parameters in m_parametersList)
			parameters.Apply(command);
	}

	public override void Reapply(IDbCommand command, int startIndex)
	{
		IsReadOnly = true;
		foreach (var parameters in m_parametersList)
		{
			parameters.Reapply(command, startIndex);
			startIndex += parameters.Count;
		}
	}

	public override int Count => m_parametersList.Sum(x => x.Count);

	public bool IsReadOnly { get; private set; }

	private void VerifyNotReadOnly()
	{
		if (IsReadOnly)
			throw new NotSupportedException("The list becomes read-only after it has been applied to a command.");
	}

	private readonly List<DbParameters> m_parametersList;
}
