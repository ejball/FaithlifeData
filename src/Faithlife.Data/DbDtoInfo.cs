using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Faithlife.Data;

internal static class DbDtoInfo
{
	public static DbDtoInfo<T> GetInfo<T>() => DbDtoInfo<T>.Instance;
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Both types have the same name.")]
internal sealed class DbDtoInfo<T>
{
	internal static readonly DbDtoInfo<T> Instance = new();

	private DbDtoInfo()
	{
		var properties = new List<DbDtoProperty<T>>();

		var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
		foreach (var memberInfo in type.GetRuntimeProperties().Where(IsPublicNonStaticProperty).AsEnumerable<MemberInfo>().Concat(type.GetRuntimeFields().Where(IsPublicNonStaticField)))
		{
			// use Name of ColumnAttribute if specified (any namespace)
			var columnName = memberInfo
				.GetCustomAttributes()
				.Where(x => x.GetType().Name == "ColumnAttribute")
				.Select(x => x.GetType().GetRuntimeProperties().FirstOrDefault(p => string.Equals(p.Name, "Name", StringComparison.OrdinalIgnoreCase))?.GetValue(x) as string)
				.FirstOrDefault(x => x is not null);

			properties.Add(new DbDtoProperty<T>(memberInfo, columnName));
		}

		properties.TrimExcess();
		Properties = properties;

		static bool IsPublicNonStaticProperty(PropertyInfo info) => info.GetMethod is { IsPublic: true, IsStatic: false };

		static bool IsPublicNonStaticField(FieldInfo info) => info is { IsPublic: true, IsStatic: false };
	}

	public IReadOnlyList<DbDtoProperty<T>> Properties { get; }
}
