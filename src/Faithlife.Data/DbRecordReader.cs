using System.Data;

namespace Faithlife.Data;

/// <summary>
/// Converts the fields of a data record.
/// </summary>
public readonly struct DbRecordReader
{
	/// <summary>
	/// Converts the record to the specified type.
	/// </summary>
	public T Get<T>() => Mapper.Map<T>(Record);

	/// <summary>
	/// Converts the specified record field to the specified type.
	/// </summary>
	public T Get<T>(int index) => Mapper.Map<T>(Record, index);

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(int index, int count) => Mapper.Map<T>(Record, index, count);

	/// <summary>
	/// Converts the specified record field to the specified type.
	/// </summary>
	public T Get<T>(string name) => Mapper.Map<T>(Record, Record.GetOrdinal(name), 1);

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(string name, int count) => Mapper.Map<T>(Record, Record.GetOrdinal(name), count);

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(string fromName, string toName)
	{
		var fromIndex = Record.GetOrdinal(fromName);
		var toIndex = Record.GetOrdinal(toName);
		return Mapper.Map<T>(Record, fromIndex, toIndex - fromIndex + 1);
	}

	/// <summary>
	/// Converts the specified record field to the specified type.
	/// </summary>
	public T Get<T>(Index index) => Mapper.Map<T>(Record, index.GetOffset(Record.FieldCount), 1);

	/// <summary>
	/// Converts the specified record fields to the specified type.
	/// </summary>
	public T Get<T>(Range range)
	{
		var (index, count) = range.GetOffsetAndLength(Record.FieldCount);
		return Mapper.Map<T>(Record, index, count);
	}

	/// <summary>
	/// The underlying data record.
	/// </summary>
	public IDataRecord Record { get; }

	/// <summary>
	/// The data mapper.
	/// </summary>
	public DbDataMapper Mapper { get; }

	internal DbRecordReader(DbDataMapper mapper, IDataRecord record)
	{
		Mapper = mapper;
		Record = record;
	}
}
