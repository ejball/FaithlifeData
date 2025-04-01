using System.Reflection;

namespace Faithlife.Data;

internal interface IDbDtoProperty<in T>
{
	MemberInfo MemberInfo { get; }

	string Name { get; }

	Type ValueType { get; }

	string? ColumnName { get; }

	DbParameters CreateParameter(T source, string name);
}
