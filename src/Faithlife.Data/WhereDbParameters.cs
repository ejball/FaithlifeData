using System.Data;

namespace Faithlife.Data;

internal sealed class WhereDbParameters(DbParameters source, Func<string, bool> where) : DbParameters
{
	internal override int CountCore(Func<string, bool>? filterName) =>
		source.CountCore(FilterName(filterName));

	internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName) =>
		source.EnumerateCore(FilterName(filterName));

	internal override void ApplyCore(IDbCommand command, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.ApplyCore(command, providerMethods, FilterName(filterName), transformName);

	internal override void ReapplyCore(IDbCommand command, int startIndex, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName) =>
		source.ReapplyCore(command, startIndex, providerMethods, FilterName(filterName), transformName);

	private Func<string, bool> FilterName(Func<string, bool>? filterName) => x => where(x) && filterName?.Invoke(x) is not false;
}
