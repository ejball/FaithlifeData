namespace Faithlife.Data;

internal interface IDbDtoInfo
{
	Type Type { get; }

	IReadOnlyList<IDbDtoProperty> Properties { get; }
}
