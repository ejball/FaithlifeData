namespace Faithlife.Data;

internal interface IDbDtoInfo
{
	Type Type { get; }

	string? GetColumnAttributeName(string propertyName);
}
