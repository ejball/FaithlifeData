using System.Reflection;

namespace Faithlife.Data;

internal sealed class DbDtoProperty<T>
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
