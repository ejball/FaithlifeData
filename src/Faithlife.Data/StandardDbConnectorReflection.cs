using System.Reflection;

namespace Faithlife.Data;

internal sealed class StandardDbConnectorReflection : DbConnectorReflection
{
	/// <inheritdoc />
	public override IReadOnlyList<IDbDtoProperty> GetProperties(Type type) => DtoInfo.GetInfo(type).Properties.Select(Map).ToList()!;

	/// <inheritdoc />
	public override IReadOnlyList<IDbDtoProperty<T>> GetProperties<T>() => DtoInfo.GetInfo<T>().Properties.Select(Map).ToList()!;

	/// <inheritdoc />
	public override IDbDtoProperty? TryGetProperty(Type type, string name) => Map(DtoInfo.GetInfo(type).TryGetProperty(name));

	/// <inheritdoc />
	public override T CreateNew<T>(IReadOnlyList<(IDbDtoProperty<T> Property, object? Value)> propertyValues) => DtoInfo.GetInfo<T>().CreateNew(propertyValues.Select(x => (((DbDtoProperty<T>) x.Property).Property, x.Value)).ToList());

	private static IDbDtoProperty? Map(IDtoProperty? property) => property is null ? null : new DbDtoProperty(property);

	private static IDbDtoProperty<T>? Map<T>(IDtoProperty<T>? property) => property is null ? null : new DbDtoProperty<T>(property);

	internal sealed class DbDtoProperty(IDtoProperty property) : IDbDtoProperty
	{
		public string Name => property.Name;
		public Type ValueType => property.ValueType;
		public MemberInfo MemberInfo => property.MemberInfo;
		public object? GetValue(object source) => property.GetValue(source);
	}

	internal sealed class DbDtoProperty<T>(IDtoProperty<T> property) : IDbDtoProperty<T>
	{
		public IDtoProperty<T> Property => property;
		public string Name => property.Name;
		public Type ValueType => property.ValueType;
		public MemberInfo MemberInfo => property.MemberInfo;
		public object? GetValue(object source) => property.GetValue(source);
	}
}
