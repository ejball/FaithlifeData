using System.Data;

namespace Faithlife.Data;

/// <summary>
/// A list of sets of parameters.
/// </summary>
public sealed class DbParametersList : DbParameters
{
	/// <summary>
	/// Creates an empty list.
	/// </summary>
	public DbParametersList() => m_parametersList = [];

	/// <summary>
	/// Creates a list from the specified sets of parameters.
	/// </summary>
	public DbParametersList(params IEnumerable<DbParameters> items) => m_parametersList = [.. items];

	/// <inheritdoc />
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

	public override void Apply(IDbCommand command, DbProviderMethods providerMethods)
	{
		m_isReadOnly = true;
		foreach (var parameters in m_parametersList)
			parameters.Apply(command, providerMethods);
	}

	public override void Reapply(IDbCommand command, int startIndex, DbProviderMethods providerMethods)
	{
		m_isReadOnly = true;
		foreach (var parameters in m_parametersList)
		{
			parameters.Reapply(command, startIndex, providerMethods);
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
