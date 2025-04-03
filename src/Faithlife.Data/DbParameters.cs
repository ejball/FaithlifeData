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
	/// Creates a list of parameters from the properties of a DTO.
	/// </summary>
	/// <remarks>The name of each parameter is determined by calling the function with the name of the corresponding DTO property.</remarks>
	public static DbParameters FromDto<T>(Func<string, string> name, T dto)
	{
		if (dto is null)
			throw new ArgumentNullException(nameof(dto));
		return Create(DbDtoInfo.GetInfo<T>().Properties.Select(x => x.CreateParameter(name(x.Name), dto)));
	}

	/// <summary>
	/// Filters the parameters by name.
	/// </summary>
	public DbParameters Where(Func<string, bool> filter)
	{
		if (filter is null)
			throw new ArgumentNullException(nameof(filter));
		return new WhereDbParameters(this, filter);
	}

	internal void Apply(IDbCommand command, DbProviderMethods providerMethods) =>
		ApplyCore(command, providerMethods, null);

	internal void Reapply(IDbCommand command, int startIndex, DbProviderMethods providerMethods) =>
		ReapplyCore(command, startIndex, providerMethods, null);

	internal abstract int CountCore(Func<string, bool>? filterName);

	internal abstract IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName);

	internal abstract void ApplyCore(IDbCommand command, DbProviderMethods providerMethods, Func<string, bool>? filterName);

	internal abstract void ReapplyCore(IDbCommand command, int startIndex, DbProviderMethods providerMethods, Func<string, bool>? filterName);

	private sealed class EmptyDbParameters : DbParameters
	{
		internal override int CountCore(Func<string, bool>? filterName) => 0;

		internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName) => [];

		internal override void ApplyCore(IDbCommand command, DbProviderMethods providerMethods, Func<string, bool>? filterName)
		{
		}

		internal override void ReapplyCore(IDbCommand command, int startIndex, DbProviderMethods providerMethods, Func<string, bool>? filterName)
		{
		}
	}

	private sealed class WhereDbParameters(DbParameters source, Func<string, bool> where) : DbParameters
	{
		internal override int CountCore(Func<string, bool>? filterName) =>
			source.CountCore(FilterName(filterName));

		internal override IEnumerable<(string Name, object? Value)> EnumerateCore(Func<string, bool>? filterName) =>
			source.EnumerateCore(FilterName(filterName));

		internal override void ApplyCore(IDbCommand command, DbProviderMethods providerMethods, Func<string, bool>? filterName) =>
			source.ApplyCore(command, providerMethods, FilterName(filterName));

		internal override void ReapplyCore(IDbCommand command, int startIndex, DbProviderMethods providerMethods, Func<string, bool>? filterName) =>
			source.ReapplyCore(command, startIndex, providerMethods, FilterName(filterName));

		private Func<string, bool> FilterName(Func<string, bool>? filterName) => x => where(x) && filterName?.Invoke(x) is not false;
	}
}
