using System.Linq.Expressions;
using System.Reflection;

namespace Faithlife.Data;

internal sealed class DbDtoProperty<T>
{
	public DbDtoProperty(MemberInfo memberInfo, string? columnName)
	{
		MemberInfo = memberInfo;
		Name = memberInfo.Name;
		ValueType = (memberInfo as PropertyInfo)?.PropertyType ?? ((FieldInfo) memberInfo).FieldType;
		ColumnName = columnName;
		m_lazyCreateParameter = new(CreateParameterCreator);
	}

	private Func<string, T, DbParameters> CreateParameterCreator()
	{
		var nameParam = Expression.Parameter(typeof(string), "name");
		var sourceParam = Expression.Parameter(typeof(T), "source");

		var getValue = MemberInfo is PropertyInfo propertyInfo
			? Expression.Property(sourceParam, propertyInfo)
			: Expression.Field(sourceParam, (FieldInfo) MemberInfo);

		var createMethod = typeof(DbParameters)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Single(x => x is { Name: "Create", IsGenericMethod: true } &&
				x.GetGenericArguments().Length == 1 &&
				x.GetParameters() is [var p0, var p1] &&
				p0.ParameterType == typeof(string) &&
				p1.ParameterType.IsGenericParameter).MakeGenericMethod(ValueType);

		return Expression.Lambda<Func<string, T, DbParameters>>(
			Expression.Call(createMethod, nameParam, getValue), nameParam, sourceParam).Compile();
	}

	public MemberInfo MemberInfo { get; }

	public string Name { get; }

	public Type ValueType { get; }

	public string? ColumnName { get; }

	public DbParameters CreateParameter(string name, T valueSource) => m_lazyCreateParameter.Value(name, valueSource);

	private readonly Lazy<Func<string, T, DbParameters>> m_lazyCreateParameter;
}
