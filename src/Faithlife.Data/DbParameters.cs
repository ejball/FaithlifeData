using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace Faithlife.Data;

/// <summary>
/// A set of database parameters.
/// </summary>
public abstract class DbParameters
{
	public abstract int Count { get; }

	public abstract void Apply(IDbCommand command);

	public abstract void Reapply(IDbCommand command, int startIndex);

	public abstract IEnumerable<(string? Name, object? Value)> Enumerate();

	/// <summary>
	/// An empty list of parameters.
	/// </summary>
	[SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Intentional API.")]
	public static readonly DbParameters Empty = new EmptyDbParameters();

	/// <summary>
	/// Creates a list of parameters with one parameter.
	/// </summary>
	public static DbParameters Create<T>(string name, T value) =>
		new OneDbParameter<T>(name, value);

	/////// <summary>
	/////// Creates a list of parameters from tuples.
	/////// </summary>
	////public static DbParameters Create(params IEnumerable<(string Name, object? Value)> parameters) =>
	////	Create<object?>(parameters ?? throw new ArgumentNullException(nameof(parameters)));

	/////// <summary>
	/////// Creates a list of parameters from a sequence of tuples.
	/////// </summary>
	////public static DbParameters Create(IEnumerable<(string Name, object? Value)> parameters) =>
	////	new DbParametersList((parameters ?? throw new ArgumentNullException(nameof(parameters))).Select(x => DbParameters.Create(x.Name, x.Value)));

	/// <summary>
	/// Creates a list of parameters from a sequence of tuples.
	/// </summary>
	public static DbParameters Create<T>(params IEnumerable<(string Name, T Value)> parameters) =>
		new DbParametersList((parameters ?? throw new ArgumentNullException(nameof(parameters))).Select(x => Create(x.Name, x.Value)));

	/// <summary>
	/// Creates a list of parameters from a dictionary.
	/// </summary>
	public static DbParameters Create<T>(IEnumerable<KeyValuePair<string, T>> parameters) =>
		Create((parameters ?? throw new ArgumentNullException(nameof(parameters))).Select(x => (x.Key, x.Value)));

#if false
	/// <summary>
	/// Creates a list of parameters from a single name and a collection of values.
	/// </summary>
	/// <remarks>The name of each parameter is <c>name_index</c>, where <c>name</c> is as specified and <c>index</c>
	/// is the zero-based index of the value.</remarks>
	public static DbParameters FromMany(string name, IEnumerable values)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		var index = 0;
		var parameters = new List<(string, object?)>();
		foreach (var value in values ?? throw new ArgumentNullException(nameof(values)))
			parameters.Add(($"{name}_{index++}", value));
		return new StandardDbParameters(parameters);
	}

	/// <summary>
	/// Creates a list of parameters from a collection of values.
	/// </summary>
	/// <remarks>The name of each parameter is determined by calling the specified function with the zero-based index of the value.</remarks>
	public static DbParameters FromMany(Func<int, string> name, IEnumerable values)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		var index = 0;
		var parameters = new List<(string, object?)>();
		foreach (var value in values ?? throw new ArgumentNullException(nameof(values)))
			parameters.Add((name(index++), value));
		return new StandardDbParameters(parameters);
	}
#endif

	/// <summary>
	/// Creates a list of parameters from the properties of a DTO.
	/// </summary>
	/// <remarks>The name of each parameter is the name of the corresponding DTO property.</remarks>
	public static DbParameters FromDto(object dto) => throw new NotImplementedException();
	////new StandardDbParameters(DbConnectorReflection.Default.GetProperties((dto ?? throw new ArgumentNullException(nameof(dto))).GetType()).Select(x => (x.Name, x.GetValue(dto))));

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
		public override void Apply(IDbCommand command)
		{
		}

		public override void Reapply(IDbCommand command, int startIndex)
		{
		}

		public override IEnumerable<(string? Name, object? Value)> Enumerate() => [];

		public override int Count => 0;
	}
}
