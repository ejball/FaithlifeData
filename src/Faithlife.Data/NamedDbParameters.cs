using System.Data;

namespace Faithlife.Data;

internal sealed class NamedDbParameters(DbParameters source, Func<string, string> named) : DbParameters
{
	internal override int CountCore(Func<string, bool>? filterName) =>
		source.CountCore(filterName is null ? null : x => filterName(named(x)));

	internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName) =>
		source.EnumerateCore(filterName is null ? null : x => filterName(named(x)))
			.Select(x => (named(x.Name), x.Value));

	internal override void ApplyCore(IDbCommand command, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName)
	{
		var finalTransform = transformName is null ? named : x => transformName(named(x));
		source.ApplyCore(command, providerMethods, filterName is null ? null : x => filterName(named(x)), finalTransform);
	}

	internal override void ReapplyCore(IDbCommand command, int startIndex, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName)
	{
		var finalTransform = transformName is null ? named : x => transformName(named(x));
		source.ReapplyCore(command, startIndex, providerMethods, filterName is null ? null : x => filterName(named(x)), finalTransform);
	}
}
