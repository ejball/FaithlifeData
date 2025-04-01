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
	public static DbParameters FromDto<T>(T dto) =>
		Create(DbDtoInfo.GetInfo<T>().Properties.Select(x => x.CreateParameter(dto, x.Name)));

#if false
	/// <summary>
	/// Creates a list of parameters from the properties of a DTO.
	/// </summary>
	/// <remarks>The name of each parameter is <c>name_prop</c>, where <c>name</c> is as specified and <c>prop</c> is the
	/// name of the corresponding DTO property.</remarks>
	public static DbParameters FromDto(string name, object dto)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		return new StandardDbParameters(DbConnectorReflection.Default.GetProperties((dto ?? throw new ArgumentNullException(nameof(dto))).GetType()).Select(x => ($"{name}_{x.Name}", x.GetValue(dto))));
	}

	/// <summary>
	/// Creates a list of parameters from the properties of a DTO.
	/// </summary>
	/// <remarks>The name of each parameter is determined by calling the function with the name of the corresponding DTO property.</remarks>
	public static DbParameters FromDto(Func<string, string> name, object dto)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		return new StandardDbParameters(DbConnectorReflection.Default.GetProperties((dto ?? throw new ArgumentNullException(nameof(dto))).GetType()).Select(x => (name(x.Name), x.GetValue(dto))));
	}

	/// <summary>
	/// Creates a list of parameters from the properties of a DTO whose names match the specified filter.
	/// </summary>
	/// <remarks>The name of each parameter is the name of the corresponding DTO property.</remarks>
	public static DbParameters FromDtoWhere(object dto, Func<string, bool> filter) =>
		new StandardDbParameters(DbConnectorReflection.Default.GetProperties((dto ?? throw new ArgumentNullException(nameof(dto))).GetType()).Where(x => filter(x.Name)).Select(x => (x.Name, x.GetValue(dto))));

	/// <summary>
	/// Creates a list of parameters from the properties of a DTO whose names match the specified filter.
	/// </summary>
	/// <remarks>The name of each parameter is <c>name_prop</c>, where <c>name</c> is as specified and <c>prop</c> is the
	/// name of the corresponding DTO property.</remarks>
	public static DbParameters FromDtoWhere(string name, object dto, Func<string, bool> filter)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		return new StandardDbParameters(DbConnectorReflection.Default.GetProperties((dto ?? throw new ArgumentNullException(nameof(dto))).GetType()).Where(x => filter(x.Name)).Select(x => ($"{name}_{x.Name}", x.GetValue(dto))));
	}

	/// <summary>
	/// Creates a list of parameters from the properties of a DTO whose names match the specified filter.
	/// </summary>
	/// <remarks>The name of each parameter is determined by calling the function with the name of the corresponding DTO property.</remarks>
	public static DbParameters FromDtoWhere(Func<string, string> name, object dto, Func<string, bool> filter)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		return new StandardDbParameters(DbConnectorReflection.Default.GetProperties((dto ?? throw new ArgumentNullException(nameof(dto))).GetType()).Where(x => filter(x.Name)).Select(x => (name(x.Name), x.GetValue(dto))));
	}

	/// <summary>
	/// Creates a list of parameters from the collective properties of a sequence of DTOs.
	/// </summary>
	/// <remarks>The name of each parameter is <c>prop_index</c>, where <c>prop</c> is the name of the corresponding DTO property
	/// and <c>index</c> is the zero-based index of the DTO.</remarks>
	public static DbParameters FromDtos(IEnumerable dtos)
	{
		var index = 0;
		var parameters = new List<(string, object?)>();
		foreach (var dto in dtos ?? throw new ArgumentNullException(nameof(dtos)))
		{
			parameters.AddRange(DbConnectorReflection.Default.GetProperties((dto ?? throw new ArgumentException("DTO is null.", nameof(dtos))).GetType()).Select(x => ($"{x.Name}_{index}", x.GetValue(dto))));
			index++;
		}
		return new StandardDbParameters(parameters);
	}

	/// <summary>
	/// Creates a list of parameters from the collective properties of a sequence of DTOs.
	/// </summary>
	/// <remarks>The name of each parameter is <c>name_prop_index</c>, where <c>name</c> is as specified and <c>prop</c> is the name
	/// of the corresponding DTO property and <c>index</c> is the zero-based index of the DTO.</remarks>
	public static DbParameters FromDtos(string name, IEnumerable dtos)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		var index = 0;
		var parameters = new List<(string, object?)>();
		foreach (var dto in dtos ?? throw new ArgumentNullException(nameof(dtos)))
		{
			parameters.AddRange(DbConnectorReflection.Default.GetProperties((dto ?? throw new ArgumentException("DTO is null.", nameof(dtos))).GetType()).Select(x => ($"{name}_{x.Name}_{index}", x.GetValue(dto))));
			index++;
		}
		return new StandardDbParameters(parameters);
	}

	/// <summary>
	/// Creates a list of parameters from the collective properties of a sequence of DTOs.
	/// </summary>
	/// <remarks>The name of each parameter is determined by calling the specified function with the name of the corresponding DTO property
	/// and the zero-based index of the DTO.</remarks>
	public static DbParameters FromDtos(Func<string, int, string> name, IEnumerable dtos)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		var index = 0;
		var parameters = new List<(string, object?)>();
		foreach (var dto in dtos ?? throw new ArgumentNullException(nameof(dtos)))
		{
			parameters.AddRange(DbConnectorReflection.Default.GetProperties((dto ?? throw new ArgumentException("DTO is null.", nameof(dtos))).GetType()).Select(x => (name(x.Name, index), x.GetValue(dto))));
			index++;
		}
		return new StandardDbParameters(parameters);
	}
#endif

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
