using System.Data;

namespace Faithlife.Data;

/// <summary>
/// Maps data record values to an instance of the specified type.
/// </summary>
public abstract class DbTypeMapper<T> : IDbTypeMapper
{
	/// <inheritdoc />
	public Type Type => typeof(T);

	/// <inheritdoc />
	public abstract int? FieldCount { get; }

	/// <summary>
	/// Maps the data record values to an instance of the specified type.
	/// </summary>
	public T Map(IDataRecord record, int index, int count)
	{
		var fieldCount = (record ?? throw new ArgumentNullException(nameof(record))).FieldCount;
		if (index < 0 || count < 0 || index > fieldCount - count)
			throw new ArgumentException($"Index {index} and count {count} are out of range for {fieldCount} fields.");
		return MapCore(record, index, count);
	}

	/// <summary>
	/// Maps the data record value to an instance of the specified type.
	/// </summary>
	public T Map(IDataRecord record, int index)
	{
		var fieldCount = (record ?? throw new ArgumentNullException(nameof(record))).FieldCount;
		if (index < 0 || index >= fieldCount)
			throw new ArgumentException($"Index {index} is out of range for {fieldCount} fields.");
		return MapCore(record, index, count: 1);
	}

	/// <summary>
	/// Maps the data record values to an instance of the specified type.
	/// </summary>
	public T Map(IDataRecord record) =>
		MapCore(record, index: 0, count: (record ?? throw new ArgumentNullException(nameof(record))).FieldCount);

	/// <summary>
	/// Maps the data record values to an instance of the specified type.
	/// </summary>
	protected abstract T MapCore(IDataRecord record, int index, int count);

	/// <inheritdoc />
	object? IDbTypeMapper.Map(IDataRecord record, int index, int count) => Map(record, index, count);
}
