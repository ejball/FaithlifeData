namespace Faithlife.Data;

/// <summary>
/// Saves type mapping state for a record.
/// </summary>
public sealed class DbRecordState
{
	public object? Get(IDbTypeMapper mapper, int index, int count) =>
		m_states?.GetValueOrDefault((mapper, index, count));

	public void Set(IDbTypeMapper mapper, int index, int count, object? state) =>
		(m_states ??= [])[(mapper, index, count)] = state;

	private Dictionary<(IDbTypeMapper Mapper, int Index, int Count), object?>? m_states;
}
