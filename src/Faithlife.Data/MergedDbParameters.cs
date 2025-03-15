using System.Data;

namespace Faithlife.Data;

/// <summary>
/// An immutable list of parameters.
/// </summary>
internal sealed class MergedDbParameters : DbParameters
{
	public MergedDbParameters(IEnumerable<DbParameters> parametersList)
	{
		m_parametersList = parametersList.ToList();
	}

	public override void AddTo(IDbCommand command)
	{
		foreach (var parameters in m_parametersList)
			parameters.AddTo(command);
	}

	public override void ReapplyTo(IDbCommand command, int startIndex)
	{
		foreach (var parameters in m_parametersList)
		{
			parameters.ReapplyTo(command, startIndex);
			startIndex += parameters.Count;
		}
	}

	/// <inheritdoc />
	public override int Count => m_parametersList.Sum(x => x.Count);

	private readonly List<DbParameters> m_parametersList;
}
