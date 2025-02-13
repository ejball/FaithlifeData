using System.Collections.Concurrent;
using System.Data;
using System.Reflection;

namespace Faithlife.Data;

/// <summary>
/// Maps from data record values to objects.
/// </summary>
public abstract class DbDataMapper
{
	/// <summary>
	/// The default data mapper allows the ADO.NET provider to convert values to the expected type.
	/// </summary>
	public static DbDataMapper Default { get; } = new DefaultDbDataMapper();

	/// <summary>
	/// The strict data mapper casts values from the ADO.NET provider to the expected type.
	/// </summary>
	public static DbDataMapper Strict { get; } = new StrictDbDataMapper();

	/// <summary>
	/// Gets a type mapper for the specified type.
	/// </summary>
	public DbTypeMapper<T> GetTypeMapper<T>()
	{
		IDbTypeMapper? mapper;
		while (!s_typeMappers.TryGetValue(typeof(T), out mapper))
			s_typeMappers.TryAdd(typeof(T), CreateTypeMapper<T>());
		return (DbTypeMapper<T>) mapper;
	}

	/// <summary>
	/// Gets a type mapper for the specified type.
	/// </summary>
	public IDbTypeMapper GetTypeMapper(Type type)
	{
		IDbTypeMapper? mapper;
		while (!s_typeMappers.TryGetValue(type, out mapper))
			s_typeMappers.TryAdd(type, (IDbTypeMapper) s_createTypeMapper.MakeGenericMethod(type).Invoke(this, [])!);
		return mapper;
	}

	/// <summary>
	/// Maps the data record values to an instance of the specified type.
	/// </summary>
	public T Map<T>(IDataRecord record, int index, int count) => GetTypeMapper<T>().Map(record, index, count);

	/// <summary>
	/// Maps the data record value to an instance of the specified type.
	/// </summary>
	public T Map<T>(IDataRecord record, int index) => GetTypeMapper<T>().Map(record, index);

	/// <summary>
	/// Maps the data record values to an instance of the specified type.
	/// </summary>
	public T Map<T>(IDataRecord record) => GetTypeMapper<T>().Map(record);

	protected abstract DbTypeMapper<T> CreateTypeMapper<T>();

	private sealed class StrictDbDataMapper : DbDataMapper
	{
		protected override DbTypeMapper<T> CreateTypeMapper<T>()
		{
			var type = typeof(T);
			return type.IsValueType && Nullable.GetUnderlyingType(type) is null ? new NotNullableCastMapper<T>() : new NullableCastMapper<T>();
		}
	}

	private sealed class DefaultDbDataMapper : DbDataMapper
	{
		protected override DbTypeMapper<T> CreateTypeMapper<T>()
		{
			if (typeof(T) == typeof(string))
				return (DbTypeMapper<T>) (object) new StringMapper();

			if (typeof(T) == typeof(long))
				return (DbTypeMapper<T>) (object) new Int64Mapper();
			if (typeof(T) == typeof(long?))
				return (DbTypeMapper<T>) (object) new NullableInt64Mapper();

			if (typeof(T) == typeof(int))
				return (DbTypeMapper<T>) (object) new Int32Mapper();
			if (typeof(T) == typeof(int?))
				return (DbTypeMapper<T>) (object) new NullableInt32Mapper();

			if (typeof(T) == typeof(double))
				return (DbTypeMapper<T>) (object) new DoubleMapper();
			if (typeof(T) == typeof(double?))
				return (DbTypeMapper<T>) (object) new NullableDoubleMapper();

			if (typeof(T) == typeof(byte[]))
				return (DbTypeMapper<T>) (object) new ByteArrayMapper();

			if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition().FullName?.StartsWith("System.ValueTuple", StringComparison.Ordinal) is true)
			{
				var tupleTypes = typeof(T).GetGenericArguments();
				var tupleMapperType = tupleTypes.Length switch
				{
					2 => typeof(TupleMapper<,>),
					3 => typeof(TupleMapper<,,>),
					_ => throw new NotImplementedException("TODO: support bigger tuples"),
				};
				return (DbTypeMapper<T>) Activator.CreateInstance(tupleMapperType.MakeGenericType(tupleTypes), [.. tupleTypes.Select(GetTypeMapper)])!;
			}

			return new DtoMapper<T>();
		}
	}

	private abstract class TypeMapper<T> : DbTypeMapper<T>
	{
		protected InvalidOperationException BadFieldCount(int expected, int actual) => new($"{Type.FullName} must be read from {expected} fields but is being read from {actual} fields.");

		protected InvalidOperationException NotNullable() => new($"{Type.FullName} cannot be read from a null field.");

		protected InvalidOperationException BadCast(Type? type, Exception exception) => new($"Failed to cast {type?.FullName} to {Type.FullName}.", exception);
	}

	private sealed class DtoMapper<T> : DbTypeMapper<T>
	{
		public DtoMapper()
		{
			var properties = DtoInfo.GetInfo<T>().Properties;
			var propertiesByNormalizedFieldName = new Dictionary<string, (IDtoProperty<T> Dto, IDbValueTypeInfo Db)>(capacity: properties.Count, StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string>? columnAttributeNames = null;

			foreach (var property in properties)
			{
				// use Name of ColumnAttribute if specified (any namespace)
				var columnName = property.MemberInfo
					.GetCustomAttributes()
					.Where(x => x.GetType().Name == "ColumnAttribute")
					.Select(x => DtoInfo.GetInfo(x.GetType()).TryGetProperty("Name")?.GetValue(x) as string)
					.FirstOrDefault(x => x is not null);
				if (columnName is not null)
					(columnAttributeNames ??= new Dictionary<string, string>()).Add(property.Name, columnName);

				propertiesByNormalizedFieldName.Add(NormalizeFieldName(columnName ?? property.Name), (property, DbValueTypeInfo.GetInfo(property.ValueType)));
			}

			m_propertiesByNormalizedFieldName = propertiesByNormalizedFieldName;
			////m_columnAttributeNames = columnAttributeNames;
		}

		protected override T MapCore(IDataRecord record, int index, int count)
		{
			List<(IDtoProperty<T> Property, object? Value)>? propertyValues = null;
			for (var i = index; i < index + count; i++)
			{
				if (!record.IsDBNull(i))
				{
					var name = record.GetName(i);
					if (!m_propertiesByNormalizedFieldName!.TryGetValue(NormalizeFieldName(name), out var property))
						throw new InvalidOperationException($"Type does not have a property for '{name}': {Type.FullName}");

					propertyValues ??= new List<(IDtoProperty<T> Property, object? Value)>(capacity: count);
					propertyValues.Add((property.Dto, property.Db.GetValue(record, i, 1)));
				}
			}
			return propertyValues is not null ? DtoInfo.GetInfo<T>().CreateNew(propertyValues) : default!;
		}

#if !NETSTANDARD2_0
		private static string NormalizeFieldName(string text) => text.Replace("_", "", StringComparison.Ordinal);
#else
		private static string NormalizeFieldName(string text) => text.Replace("_", "");
#endif

		private readonly IReadOnlyDictionary<string, (IDtoProperty<T> Dto, IDbValueTypeInfo Db)>? m_propertiesByNormalizedFieldName;
		////private readonly IReadOnlyDictionary<string, string>? m_columnAttributeNames;
	}

	private sealed class ByteArrayMapper : ReferenceValueMapper<byte[]>
	{
		protected override byte[] MapNotNullField(IDataRecord record, int index)
		{
			var byteCount = (int) record.GetBytes(index, fieldOffset: 0, buffer: null, bufferoffset: 0, length: 0);
			var bytes = new byte[byteCount];
			record.GetBytes(index, fieldOffset: 0, buffer: bytes, bufferoffset: 0, length: byteCount);
			return bytes;
		}
	}

	private sealed class TupleMapper<T1, T2>(DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2) : TypeMapper<(T1, T2)>
	{
		protected override (T1, T2) MapCore(IDataRecord record, int index, int count) =>
			count == 2 ? (mapper1.Map(record, index), mapper2.Map(record, index + 1)) : throw BadFieldCount(2, count);
	}

	private sealed class TupleMapper<T1, T2, T3>(DbTypeMapper<T1> mapper1, DbTypeMapper<T2> mapper2, DbTypeMapper<T3> mapper3) : TypeMapper<(T1, T2, T3)>
	{
		protected override (T1, T2, T3) MapCore(IDataRecord record, int index, int count) =>
			count == 3 ? (mapper1.Map(record, index), mapper2.Map(record, index + 1), mapper3.Map(record, index + 2)) : throw BadFieldCount(3, count);
	}

	private abstract class SingleFieldMapper<T> : TypeMapper<T>
	{
		protected sealed override T MapCore(IDataRecord record, int index, int count) =>
			count == 1 ? MapField(record, index) : throw BadFieldCount(1, count);

		protected abstract T MapField(IDataRecord record, int index);
	}

	private sealed class NullableCastMapper<T> : SingleFieldMapper<T>
	{
		protected override T MapField(IDataRecord record, int index)
		{
			var value = record.GetValue(index);
			try
			{
				return value == DBNull.Value ? default! : (T) value;
			}
			catch (Exception exception) when (exception is ArgumentException or InvalidCastException)
			{
				throw BadCast(value?.GetType(), exception);
			}
		}
	}

	private sealed class NotNullableCastMapper<T> : SingleFieldMapper<T>
	{
		protected override T MapField(IDataRecord record, int index)
		{
			var value = record.GetValue(index);
			try
			{
				return value == DBNull.Value ? throw NotNullable() : (T) value;
			}
			catch (Exception exception) when (exception is ArgumentException or InvalidCastException)
			{
				throw BadCast(value?.GetType(), exception);
			}
		}
	}

	private abstract class NonNullableValueMapper<T> : SingleFieldMapper<T>
		where T : struct
	{
		protected sealed override T MapField(IDataRecord record, int index) =>
			!record.IsDBNull(index) ? MapNotNullField(record, index) : throw NotNullable();

		protected abstract T MapNotNullField(IDataRecord record, int index);
	}

	private abstract class NullableValueMapper<T> : SingleFieldMapper<T?>
		where T : struct
	{
		protected sealed override T? MapField(IDataRecord record, int index) =>
			!record.IsDBNull(index) ? MapNotNullField(record, index) : null;

		protected abstract T MapNotNullField(IDataRecord record, int index);
	}

	private abstract class ReferenceValueMapper<T> : SingleFieldMapper<T?>
		where T : class
	{
		protected sealed override T? MapField(IDataRecord record, int index) =>
			!record.IsDBNull(index) ? MapNotNullField(record, index) : null;

		protected abstract T MapNotNullField(IDataRecord record, int index);
	}

	private sealed class StringMapper : ReferenceValueMapper<string>
	{
		protected override string MapNotNullField(IDataRecord record, int index) => record.GetString(index);
	}

	private sealed class Int64Mapper : NonNullableValueMapper<long>
	{
		protected override long MapNotNullField(IDataRecord record, int index) => record.GetInt64(index);
	}

	private sealed class NullableInt64Mapper : NullableValueMapper<long>
	{
		protected override long MapNotNullField(IDataRecord record, int index) => record.GetInt64(index);
	}

	private sealed class Int32Mapper : NonNullableValueMapper<int>
	{
		protected override int MapNotNullField(IDataRecord record, int index) => record.GetInt32(index);
	}

	private sealed class NullableInt32Mapper : NullableValueMapper<int>
	{
		protected override int MapNotNullField(IDataRecord record, int index) => record.GetInt32(index);
	}

	private sealed class DoubleMapper : NonNullableValueMapper<double>
	{
		protected override double MapNotNullField(IDataRecord record, int index) => record.GetDouble(index);
	}

	private sealed class NullableDoubleMapper : NullableValueMapper<double>
	{
		protected override double MapNotNullField(IDataRecord record, int index) => record.GetDouble(index);
	}

	private static readonly ConcurrentDictionary<Type, IDbTypeMapper> s_typeMappers = new();
	private static readonly MethodInfo s_createTypeMapper = typeof(DbDataMapper).GetMethod(nameof(CreateTypeMapper), BindingFlags.NonPublic | BindingFlags.Instance, null, [], null)!;
}
