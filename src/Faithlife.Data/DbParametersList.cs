using System.Data;

namespace Faithlife.Data;

/// <summary>
/// A list of parameters.
/// </summary>
public sealed class DbParametersList : DbParameters
{
	public DbParametersList() => m_parametersList = [];

	public DbParametersList(params IEnumerable<DbParameters> parametersList) => m_parametersList = [.. parametersList];

	public override int Count => m_parametersList.Sum(x => x.Count);

	public bool IsReadOnly
	{
		get => m_isReadOnly;
		set
		{
			VerifyNotReadOnly();
			m_isReadOnly = value;
		}
	}

	public void Add(DbParameters item)
	{
		VerifyNotReadOnly();
		m_parametersList.Add(item);
	}

	public override void Apply(IDbCommand command)
	{
		m_isReadOnly = true;
		foreach (var parameters in m_parametersList)
			parameters.Apply(command);
	}

	public override void Reapply(IDbCommand command, int startIndex)
	{
		m_isReadOnly = true;
		foreach (var parameters in m_parametersList)
		{
			parameters.Reapply(command, startIndex);
			startIndex += parameters.Count;
		}
	}

	public override IEnumerable<(string Name, object? Value)> Enumerate() =>
		m_parametersList.SelectMany(x => x.Enumerate());

	private void VerifyNotReadOnly()
	{
		if (m_isReadOnly)
			throw new NotSupportedException("This instance is read-only.");
	}

	private readonly List<DbParameters> m_parametersList;
	private bool m_isReadOnly;
}
