using static System.FormattableString;

namespace Faithlife.Data.SqlFormatting;

internal sealed class SqlContext
{
	public SqlContext(SqlSyntax syntax, DbConnectorReflection reflection)
	{
		Syntax = syntax;
		Reflection = reflection;
	}

	public SqlSyntax Syntax { get; }

	public DbConnectorReflection Reflection { get; }

	public DbParameters Parameters => m_parametersList ?? DbParameters.Empty;

	public string RenderParam<T>(object? key, T value)
	{
		if (key is not null && m_renderedParams is not null && m_renderedParams.TryGetValue(key, out var rendered))
			return rendered;

		m_parametersList ??= new();
		var name = Invariant($"fdp{m_parametersList.Count}");
		m_parametersList.Add(DbParameters.Create(name, value));
		rendered = Syntax.ParameterPrefix + name;

		if (key is not null)
		{
			m_renderedParams ??= new();
			m_renderedParams.Add(key, rendered);
		}

		return rendered;
	}

	private DbParametersSet? m_parametersList;
	private Dictionary<object, string>? m_renderedParams;
}
