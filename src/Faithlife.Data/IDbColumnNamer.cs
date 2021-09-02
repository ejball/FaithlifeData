namespace Faithlife.Data
{
	public interface IDbColumnNamer
	{
		string GetColumnName(string propertyName);
	}
}
