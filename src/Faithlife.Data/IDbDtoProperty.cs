using System.Reflection;

namespace Faithlife.Data;

public interface IDbDtoProperty
{
	string Name { get; }
	Type ValueType { get; }
	MemberInfo MemberInfo { get; }
	object? GetValue(object source);
}

public interface IDbDtoProperty<T> : IDbDtoProperty
{
}
