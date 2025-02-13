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
	/// Maps the data record values to an instance of the specified type.
	/// </summary>
	object? Map(IDataRecord record, int index, int count);
}
