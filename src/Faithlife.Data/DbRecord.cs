using System.Data;

namespace Faithlife.Data;

/// <summary>
/// Converts the fields of a data record.
/// </summary>
public readonly struct DbRecord
{
	/// <summary>
	/// Converts the record to the specified type.
	/// </summary>
	public T Get<T>() => m_mapper.Map<T>(m_record);

	/// <summary>
	/// Converts the specified record field to the specified type.
	/// </summary>
	public T Get<T>(int index) => m_mapper.Map<T>(m_record, index);

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(int index, int count) => m_mapper.Map<T>(m_record, index, count);

	/// <summary>
	/// Converts the specified record field to the specified type.
	/// </summary>
	public T Get<T>(string name) => m_mapper.Map<T>(m_record, m_record.GetOrdinal(name), 1);

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(string name, int count) => m_mapper.Map<T>(m_record, m_record.GetOrdinal(name), count);

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(string fromName, string toName)
	{
		var fromIndex = m_record.GetOrdinal(fromName);
		var toIndex = m_record.GetOrdinal(toName);
		return m_mapper.Map<T>(m_record, fromIndex, toIndex - fromIndex + 1);
	}

	/// <summary>
	/// Converts the specified record field to the specified type.
	/// </summary>
	public T Get<T>(Index index) => m_mapper.Map<T>(m_record, index.GetOffset(m_record.FieldCount), 1);

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(Range range)
	{
		var (index, count) = range.GetOffsetAndLength(m_record.FieldCount);
		return m_mapper.Map<T>(m_record, index, count);
	}

	internal DbRecord(IDataRecord record, DbDataMapper mapper)
	{
		m_record = record;
		m_mapper = mapper;
	}

	private readonly IDataRecord m_record;
	private readonly DbDataMapper m_mapper;
}
