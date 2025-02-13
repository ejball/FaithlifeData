using System.Data;

namespace Faithlife.Data;

/// <summary>
/// Maps from data record values to an instance of the specified type.
/// </summary>
public interface IDbTypeMapper
{
	/// <summary>
	/// The type to which the data record values are mapped.
	/// </summary>
	Type Type { get; }

	/// <summary>
	/// The number of fields used by the mapper, or null if the mapper can handle any number of fields.
	/// </summary>
	int? FieldCount { get; }

	/// <summary>
	/// Maps the data record values to an instance of the specified type.
	/// </summary>
	object? Map(IDataRecord record, int index, int count);
}
