using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace Faithlife.Data;

/// <summary>
/// A set of database parameters.
/// </summary>
public abstract class DbParameters
{
	/// <summary>
	/// The number of parameters.
	/// </summary>
	public int Count => CountCore(null);

	/// <summary>
	/// Enumerates the names and values of the parameters.
	/// </summary>
	public IEnumerable<(string Name, object? Value)> Enumerate() => EnumerateCore(null);

	/// <summary>
	/// An empty list of parameters.
	/// </summary>
	[SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Intentional API.")]
	public static readonly DbParameters Empty = new EmptyDbParameters();

	/// <summary>
	/// Creates one parameter.
	/// </summary>
	public static DbParameters Create<T>(string name, T value) =>
		new SingleDbParameter<T>(name, value);

	/// <summary>
	/// Creates parameters from a sequence of parameters.
	/// </summary>
	public static DbParameters Create(params IEnumerable<DbParameters> parameters) =>
		new DbParametersList(parameters ?? throw new ArgumentNullException(nameof(parameters))) { IsReadOnly = true };

	/// <summary>
	/// Creates parameters from a sequence of name/value pairs.
	/// </summary>
	public static DbParameters Create<T>(params IEnumerable<(string Name, T Value)> parameters) =>
		Create((parameters ?? throw new ArgumentNullException(nameof(parameters))).Select(x => Create(x.Name, x.Value)));

	/// <summary>
	/// Creates parameters from a dictionary.
	/// </summary>
	public static DbParameters Create<T>(IEnumerable<KeyValuePair<string, T>> parameters) =>
		Create((parameters ?? throw new ArgumentNullException(nameof(parameters))).Select(x => Create(x.Key, x.Value)));

	/// <summary>
	/// Creates a list of parameters from the properties of a DTO.
	/// </summary>
	/// <remarks>The name of each parameter is the name of the corresponding DTO property.</remarks>
	public static DbParameters FromDto<T>(T dto)
	{
		if (dto is null)
			throw new ArgumentNullException(nameof(dto));
		return Create(DbDtoInfo.GetInfo<T>().Properties.Select(x => x.CreateParameter(x.Name, dto)));
	}

	/// <summary>
	/// Filters the parameters by name.
	/// </summary>
	public DbParameters Where(Func<string, bool> nameMatches)
	{
		if (nameMatches is null)
			throw new ArgumentNullException(nameof(nameMatches));
		return new WhereDbParameters(this, nameMatches);
	}

	/// <summary>
	/// Transforms the parameter names using the specified function.
	/// </summary>
	public DbParameters Named(Func<string, string> transform)
	{
		if (transform is null)
			throw new ArgumentNullException(nameof(transform));
		return new NamedDbParameters(this, transform);
	}

	internal void Apply(IDbCommand command, DbProviderMethods providerMethods) =>
		ApplyCore(command, providerMethods, null, null);

	internal void Reapply(IDbCommand command, int startIndex, DbProviderMethods providerMethods) =>
		ReapplyCore(command, startIndex, providerMethods, null, null);

	internal abstract int CountCore(Func<string, bool>? filterName);

	internal abstract IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName);

	internal abstract void ApplyCore(IDbCommand command, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName);

	internal abstract void ReapplyCore(IDbCommand command, int startIndex, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName);

	private sealed class EmptyDbParameters : DbParameters
	{
		internal override int CountCore(Func<string, bool>? filterName) => 0;

		internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName) => [];

		internal override void ApplyCore(IDbCommand command, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName)
		{
		}

		internal override void ReapplyCore(IDbCommand command, int startIndex, DbProviderMethods providerMethods, Func<string, bool>? filterName, Func<string, string>? transformName)
		{
		}
	}

	private sealed class WhereDbParameters(DbParameters source, Func<string, bool> where) : DbParameters
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

	private sealed class NamedDbParameters(DbParameters source, Func<string, string> named) : DbParameters
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
}
