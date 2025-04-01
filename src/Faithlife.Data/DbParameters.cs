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
	public abstract int Count { get; }

	/// <summary>
	/// Applies the parameters to the specified command.
	/// </summary>
	public abstract void Apply(IDbCommand command, DbProviderMethods providerMethods);

	/// <summary>
	/// Reapplies the parameters to the specified command.
	/// </summary>
	public abstract void Reapply(IDbCommand command, int startIndex, DbProviderMethods providerMethods);

	/// <summary>
	/// Enumerates the names and values of the parameters.
	/// </summary>
	public abstract IEnumerable<(string Name, object? Value)> Enumerate();

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
		return Create(DbDtoInfo.GetInfo<T>().Properties.Select(x => x.CreateParameter(dto, x.Name)));
	}

	/// <summary>
	/// Creates a list of parameters from the properties of a DTO.
	/// </summary>
	/// <remarks>The name of each parameter is determined by calling the function with the name of the corresponding DTO property.</remarks>
	public static DbParameters FromDto<T>(Func<string, string> name, T dto)
	{
		if (dto is null)
			throw new ArgumentNullException(nameof(dto));
		return Create(DbDtoInfo.GetInfo<T>().Properties.Select(x => x.CreateParameter(dto, name(x.Name))));
	}

	/// <summary>
	/// Creates a list of parameters from the properties of a DTO whose names match the specified filter.
	/// </summary>
	/// <remarks>The name of each parameter is the name of the corresponding DTO property.</remarks>
	public static DbParameters FromDtoWhere<T>(T dto, Func<string, bool> filter)
	{
		if (dto is null)
			throw new ArgumentNullException(nameof(dto));
		if (filter is null)
			throw new ArgumentNullException(nameof(filter));
		return Create(DbDtoInfo.GetInfo<T>().Properties.Where(x => filter(x.Name)).Select(x => x.CreateParameter(dto, x.Name)));
	}

	/// <summary>
	/// Creates a list of parameters from the properties of a DTO whose names match the specified filter.
	/// </summary>
	/// <remarks>The name of each parameter is determined by calling the function with the name of the corresponding DTO property.</remarks>
	public static DbParameters FromDtoWhere<T>(Func<string, string> name, T dto, Func<string, bool> filter)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		if (dto is null)
			throw new ArgumentNullException(nameof(dto));
		if (filter is null)
			throw new ArgumentNullException(nameof(filter));
		return Create(DbDtoInfo.GetInfo<T>().Properties.Where(x => filter(x.Name)).Select(x => x.CreateParameter(dto, name(x.Name))));
	}

	private sealed class EmptyDbParameters : DbParameters
	{
		public override void Apply(IDbCommand command, DbProviderMethods providerMethods)
		{
		}

		public override void Reapply(IDbCommand command, int startIndex, DbProviderMethods providerMethods)
		{
		}

		public override IEnumerable<(string Name, object? Value)> Enumerate() => [];

		public override int Count => 0;
	}
}
