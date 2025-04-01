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
		var properties = new List<IDbDtoProperty<T>>();

		var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
		foreach (var memberInfo in type.GetRuntimeProperties().Where(IsPublicNonStaticProperty).AsEnumerable<MemberInfo>().Concat(type.GetRuntimeFields().Where(IsPublicNonStaticField)))
		{
			// use Name of ColumnAttribute if specified (any namespace)
			var columnName = memberInfo
				.GetCustomAttributes()
				.Where(x => x.GetType().Name == "ColumnAttribute")
				.Select(x => x.GetType().GetRuntimeProperties().FirstOrDefault(p => string.Equals(p.Name, "Name", StringComparison.OrdinalIgnoreCase))?.GetValue(x) as string)
				.FirstOrDefault(x => x is not null);

			properties.Add(new DbDtoProperty(memberInfo, columnName));
		}

		properties.TrimExcess();
		Properties = properties;

		static bool IsPublicNonStaticProperty(PropertyInfo info) => info.GetMethod is { IsPublic: true, IsStatic: false };

		static bool IsPublicNonStaticField(FieldInfo info) => info is { IsPublic: true, IsStatic: false };
	}

	public IReadOnlyList<IDbDtoProperty<T>> Properties { get; }

	internal sealed class DbDtoProperty : IDbDtoProperty<T>
	{
		public DbDtoProperty(MemberInfo memberInfo, string? columnName)
		{
			MemberInfo = memberInfo;
			Name = memberInfo.Name;
			ValueType = memberInfo is PropertyInfo propertyInfo ? propertyInfo.PropertyType : ((FieldInfo) memberInfo).FieldType;
			ColumnName = columnName;
		}

		public MemberInfo MemberInfo { get; }

		public string Name { get; }

		public Type ValueType { get; }

		public string? ColumnName { get; }

		public DbParameters CreateParameter(T source, string name) => DbParameters.Create(name, GetValue(source));

		private object? GetValue(T source) => MemberInfo is PropertyInfo propertyInfo ? propertyInfo.GetValue(source) : ((FieldInfo) MemberInfo).GetValue(source);
	}
}
