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

	internal override int CountCore(Func<string, bool>? filterName) =>
		m_parametersList.Sum(x => x.CountCore(filterName));

	internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName) =>
		m_parametersList.SelectMany(x => x.EnumerateCore(filterName));

	internal override void ApplyCore(IDbCommand command, DbProviderMethods providerMethods, Func<string, bool>? filterName)
	{
		m_isReadOnly = true;
		foreach (var parameters in m_parametersList)
			parameters.ApplyCore(command, providerMethods, filterName);
	}

	internal override void ReapplyCore(IDbCommand command, int startIndex, DbProviderMethods providerMethods, Func<string, bool>? filterName)
	{
		m_isReadOnly = true;
		foreach (var parameters in m_parametersList)
		{
			parameters.ReapplyCore(command, startIndex, providerMethods, filterName);
			startIndex += parameters.Count;
		}
	}

	private void VerifyNotReadOnly()
	{
		if (m_isReadOnly)
			throw new NotSupportedException("This instance is read-only.");
	}

	private readonly List<DbParameters> m_parametersList;
	private bool m_isReadOnly;
}
