using System.Linq.Expressions;
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
			// Create the expression tree:
			// (string name, T source) => DbParameters.Create(name, memberInfo.GetValue(source))
			var nameParam = Expression.Parameter(typeof(string), "name");
			var sourceParam = Expression.Parameter(typeof(T), "source");

			var getValue = Expression.Convert(
				memberInfo is PropertyInfo propertyInfo
					? Expression.Property(sourceParam, propertyInfo)
					: Expression.Field(sourceParam, (FieldInfo) memberInfo),
				typeof(object));

			var createMethod = typeof(DbParameters).GetMethod(nameof(DbParameters.Create), [typeof(string), typeof(T)]);
			var createCall = Expression.Call(createMethod!, nameParam, getValue);

			return Expression.Lambda<Func<string, T, DbParameters>>(createCall, nameParam, sourceParam).Compile();
		});
	}

	public MemberInfo MemberInfo { get; }

	public string Name { get; }

	public Type ValueType { get; }

	public string? ColumnName { get; }

	public DbParameters CreateParameter(string name, T valueSource) => m_lazyCreateParameter.Value(name, valueSource);

	private readonly Lazy<Func<string, T, DbParameters>> m_lazyCreateParameter;
}
