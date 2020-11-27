using System.Collections.Generic;
using static System.FormattableString;

namespace Faithlife.Data.SqlText
{
	internal sealed class SqlContext
	{
		public SqlContext(SqlRenderer renderer)
		{
			Renderer = renderer;
		}

		public SqlRenderer Renderer { get; }

		public DbParameters Parameters => m_parameters is null ? DbParameters.Empty : DbParameters.Create(m_parameters);

		public string RenderParam(object? value)
		{
			m_parameters ??= new List<(string Name, object? Value)>();
			var name = Invariant($"fdp{m_parameters.Count}");
			m_parameters.Add((name, value));
			return Renderer.ParameterPrefix + name;
		}

		private List<(string Name, object? Value)>? m_parameters;
	}
}
