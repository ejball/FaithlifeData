namespace Faithlife.Data;

public abstract class DbConnectorReflection
{
	public static DbConnectorReflection Default { get; } = new StandardDbConnectorReflection();

	public abstract IReadOnlyList<IDbDtoProperty> GetProperties(Type type);

	public abstract IReadOnlyList<IDbDtoProperty<T>> GetProperties<T>();

	public abstract IDbDtoProperty? TryGetProperty(Type type, string name);

	public abstract T CreateNew<T>(IReadOnlyList<(IDbDtoProperty<T> Property, object? Value)> propertyValues);
}
