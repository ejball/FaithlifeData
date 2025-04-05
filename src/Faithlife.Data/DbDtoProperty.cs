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

		m_lazyCreateParameter = new(() =>
		{
			return (string name, T valueSource) =>
			{
				var value = MemberInfo is PropertyInfo propertyInfo ? propertyInfo.GetValue(valueSource) : ((FieldInfo) MemberInfo).GetValue(valueSource);
				return DbParameters.Create<object?>(name, value);
			};
		});
	}

	public MemberInfo MemberInfo { get; }

	public string Name { get; }

	public Type ValueType { get; }

	public string? ColumnName { get; }

	public DbParameters CreateParameter(string name, T valueSource) => m_lazyCreateParameter.Value(name, valueSource);

	private readonly Lazy<Func<string, T, DbParameters>> m_lazyCreateParameter;
}
