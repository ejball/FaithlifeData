using System.Reflection;

namespace Faithlife.Data;

public interface IDbDtoProperty
{
	MemberInfo MemberInfo { get; }
	string Name { get; }
	Type ValueType { get; }
	string? ColumnName { get; }
	object? GetValue(object source);
}

public interface IDbDtoProperty<in T> : IDbDtoProperty
{
	object? GetValue(T source);
}
