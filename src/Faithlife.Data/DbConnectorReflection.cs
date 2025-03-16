namespace Faithlife.Data;

public abstract class DbConnectorReflection
{
	public static DbConnectorReflection Default { get; } = new StandardDbConnectorReflection();

	public abstract bool IsTupleType(Type type);

	public abstract bool IsTupleType<T>();

	public abstract IReadOnlyList<IDbDtoProperty> GetProperties(Type type);

	public abstract IReadOnlyList<IDbDtoProperty<T>> GetProperties<T>();

	public abstract IDbDtoProperty? TryGetProperty(Type type, string name);

	public abstract T CreateNew<T>(IReadOnlyList<(IDbDtoProperty<T> Property, object? Value)> propertyValues);

	public abstract IReadOnlyList<Type> GetTupleItemTypes(Type type);

	public abstract IReadOnlyList<Type> GetTupleItemTypes<T>();

	public abstract T CreateNewTuple<T>(IReadOnlyList<object?> values);
}
