namespace Faithlife.Data.SqlFormatting;

public sealed class DtoParamNamesSql<T> : Sql
{
	public DtoParamNamesSql<T> Where(Func<string, bool> nameMatches) =>
		new(m_filterName is null ? nameMatches : x => m_filterName(x) && nameMatches(x), m_transformName);

	public DtoParamNamesSql<T> Renamed(Func<string, string> transform) =>
		new(m_filterName is null ? null : x => m_filterName(transform(x)),
			m_transformName is null ? transform : throw new NotImplementedException());

	internal DtoParamNamesSql(Func<string, bool>? filterName = null, Func<string, string>? transformName = null)
	{
		m_filterName = filterName;
		m_transformName = transformName;
	}

	internal override string Render(SqlContext context)
	{
		var properties = DbDtoInfo.GetInfo<T>().Properties;
		if (properties.Count == 0)
			throw new InvalidOperationException($"The specified type has no columns: {typeof(T).FullName}");

		var filteredProperties = properties.AsEnumerable();
		if (m_filterName is not null)
			filteredProperties = filteredProperties.Where(x => m_filterName(x.Name));

		var text = string.Join(", ", filteredProperties.Select(x => context.Syntax.ParameterStart + GetName(x.Name)));
		if (text.Length == 0)
			throw new InvalidOperationException($"The specified type has no remaining columns: {typeof(T).FullName}");
		return text;
	}

	private string GetName(string name) => m_transformName is not null ? m_transformName(name) : name;

	private readonly Func<string, bool>? m_filterName;
	private readonly Func<string, string>? m_transformName;
}
