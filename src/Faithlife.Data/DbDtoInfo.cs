using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Faithlife.Data;

internal static class DbDtoInfo
{
	public static DbDtoInfo<T> GetInfo<T>() => DbDtoInfo<T>.Instance;

	public static IDbDtoInfo GetInfo(Type type) => s_infos.GetOrAdd(type, CreateInfo);

	private static IDbDtoInfo CreateInfo(Type type) =>
		(IDbDtoInfo) typeof(DbDtoInfo<>).MakeGenericType(type).GetTypeInfo().GetDeclaredField("Instance")!.GetValue(null)!;

	private static readonly ConcurrentDictionary<Type, IDbDtoInfo> s_infos = new();
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Both types have the same name.")]
internal sealed class DbDtoInfo<T> : IDbDtoInfo
{
	public Type Type => typeof(T);

	public string? GetColumnAttributeName(string propertyName)
	{
		string? columnAttributeName = null;
		m_columnAttributeNames?.TryGetValue(propertyName, out columnAttributeName);
		return columnAttributeName;
	}

	internal static readonly DbDtoInfo<T> Instance = new();

	private DbDtoInfo()
	{
		var properties = DbConnectorReflection.Default.GetProperties<T>();
		Dictionary<string, string>? columnAttributeNames = null;

		foreach (var property in properties)
		{
			// use Name of ColumnAttribute if specified (any namespace)
			var columnName = property.MemberInfo
				.GetCustomAttributes()
				.Where(x => x.GetType().Name == "ColumnAttribute")
				.Select(x => DbConnectorReflection.Default.TryGetProperty(x.GetType(), "Name")?.GetValue(x) as string)
				.FirstOrDefault(x => x is not null);
			if (columnName is not null)
				(columnAttributeNames ??= new Dictionary<string, string>()).Add(property.Name, columnName);
		}

		m_columnAttributeNames = columnAttributeNames;
	}

	private readonly IReadOnlyDictionary<string, string>? m_columnAttributeNames;
}
