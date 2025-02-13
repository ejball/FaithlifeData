using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Dynamic;
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

			if (typeof(T) == typeof(object))
				return (DbTypeMapper<T>) (object) new ObjectMapper();

			if (TupleInfo.IsTupleType(typeof(T)))
				return new TupleMapper<T>(this);

			if (typeof(T).IsEnum)
				return new EnumMapper<T>();

			var nullableUnderlyingType = Nullable.GetUnderlyingType(typeof(T));
			if (nullableUnderlyingType is not null && nullableUnderlyingType.IsEnum)
				return new NullableEnumMapper<T>(nullableUnderlyingType);

			if (typeof(T) == typeof(Dictionary<string, object?>))
				return (DbTypeMapper<T>) (object) new DictionaryMapper<Dictionary<string, object?>>();
			if (typeof(T) == typeof(IDictionary<string, object?>))
				return (DbTypeMapper<T>) (object) new DictionaryMapper<IDictionary<string, object?>>();
			if (typeof(T) == typeof(IReadOnlyDictionary<string, object?>))
				return (DbTypeMapper<T>) (object) new DictionaryMapper<IReadOnlyDictionary<string, object?>>();
			if (typeof(T) == typeof(IDictionary))
				return (DbTypeMapper<T>) (object) new DictionaryMapper<IDictionary>();

			if (typeof(T) == typeof(Stream))
				return (DbTypeMapper<T>) (object) new StreamMapper();

			return new DtoMapper<T>(this);
		}
	}

	private abstract class TypeMapper<T> : DbTypeMapper<T>
	{
		protected InvalidOperationException BadFieldCount(int count) => new($"{Type.FullName} must be read from {FieldCount} fields but is being read from {count} fields.");

		protected InvalidOperationException NotNullable() => new($"{Type.FullName} cannot be read from a null field.");

		protected InvalidOperationException BadCast(Type? type, Exception exception) => new($"Failed to cast {type?.FullName} to {Type.FullName}.", exception);
	}

	private sealed class DtoMapper<T> : TypeMapper<T>
	{
		public DtoMapper(DbDataMapper mapper)
		{
			var properties = DtoInfo.GetInfo<T>().Properties;
			var dbDtoInfo = DbDtoInfo.GetInfo<T>();

			var propertiesByNormalizedFieldName = new Dictionary<string, (IDtoProperty<T> Dto, IDbTypeMapper Db)>(capacity: properties.Count, StringComparer.OrdinalIgnoreCase);
			foreach (var property in properties)
				propertiesByNormalizedFieldName.Add(NormalizeFieldName(dbDtoInfo.GetColumnAttributeName(property.Name) ?? property.Name), (property, mapper.GetTypeMapper(property.ValueType)));
			m_propertiesByNormalizedFieldName = propertiesByNormalizedFieldName;
		}

		public override int? FieldCount => null;

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
					propertyValues.Add((property.Dto, property.Db.Map(record, i, 1)));
				}
			}
			return propertyValues is not null ? DtoInfo.GetInfo<T>().CreateNew(propertyValues) : default!;
		}

#if !NETSTANDARD2_0
		private static string NormalizeFieldName(string text) => text.Replace("_", "", StringComparison.Ordinal);
#else
		private static string NormalizeFieldName(string text) => text.Replace("_", "");
#endif

		private readonly IReadOnlyDictionary<string, (IDtoProperty<T> Dto, IDbTypeMapper Db)>? m_propertiesByNormalizedFieldName;
		////private readonly IReadOnlyDictionary<string, string>? m_columnAttributeNames;
	}

	private sealed class ObjectMapper : TypeMapper<object>
	{
		public override int? FieldCount => null;

		protected override object MapCore(IDataRecord record, int index, int count)
		{
			if (count == 1)
			{
				var value = record.GetValue(index);
				return value == DBNull.Value ? null! : value;
			}
			else
			{
				IDictionary<string, object?> obj = new ExpandoObject();
				var notNull = false;
				for (var i = index; i < index + count; i++)
				{
					var name = record.GetName(i);
					if (!record.IsDBNull(i))
					{
						obj[name] = record.GetValue(i);
						notNull = true;
					}
					else
					{
						obj[name] = null;
					}
				}
				return notNull ? obj : null!;
			}
		}
	}

	private sealed class DictionaryMapper<T> : TypeMapper<T>
	{
		public override int? FieldCount => null;

		protected override T MapCore(IDataRecord record, int index, int count)
		{
			var dictionary = new Dictionary<string, object?>();
			var notNull = false;
			for (var i = index; i < index + count; i++)
			{
				var name = record.GetName(i);
				if (!record.IsDBNull(i))
				{
					dictionary[name] = record.GetValue(i);
					notNull = true;
				}
				else
				{
					dictionary[name] = null;
				}
			}
			return notNull ? (T) (object) dictionary : default!;
		}
	}

	private sealed class TupleMapper<T> : TypeMapper<T>
	{
		public TupleMapper(DbDataMapper mapper)
		{
			m_tupleInfo = TupleInfo.GetInfo<T>();
			m_tupleTypeMappers = m_tupleInfo.ItemTypes.Select(mapper.GetTypeMapper).ToList();
			FieldCount = m_tupleTypeMappers.Aggregate((int?) 0, (x, y) => x + y.FieldCount);
		}

		public override int? FieldCount { get; }

		protected override T MapCore(IDataRecord record, int index, int count)
		{
			if (FieldCount is not null && count != FieldCount.GetValueOrDefault())
				throw BadFieldCount(count);

			var valueCount = m_tupleTypeMappers!.Count;
			object?[] values = new object[valueCount];
			var recordIndex = index;
			for (var valueIndex = 0; valueIndex < valueCount; valueIndex++)
			{
				var mapper = m_tupleTypeMappers[valueIndex];

				int fieldCount;
				int? nullIndex = null;
				if (mapper.FieldCount is null)
				{
					int? remainingFieldCount = 0;
					var minimumRemainingFieldCount = 0;
					for (var nextValueIndex = valueIndex + 1; nextValueIndex < valueCount; nextValueIndex++)
					{
						var nextFieldCount = m_tupleTypeMappers[nextValueIndex].FieldCount;
						if (nextFieldCount is not null)
						{
							remainingFieldCount += nextFieldCount.Value;
							minimumRemainingFieldCount += nextFieldCount.Value;
						}
						else
						{
							remainingFieldCount = null;
							minimumRemainingFieldCount += 1;
						}
					}

					if (remainingFieldCount is not null)
					{
						fieldCount = count - recordIndex - remainingFieldCount.Value;
					}
					else
					{
						for (var nextRecordIndex = recordIndex + 1; nextRecordIndex < count; nextRecordIndex++)
						{
							if (record.GetName(nextRecordIndex).Equals("NULL", StringComparison.OrdinalIgnoreCase))
							{
								nullIndex = nextRecordIndex;
								break;
							}
						}

						if (nullIndex is not null)
						{
							fieldCount = nullIndex.Value - recordIndex;
						}
						else if (count - (recordIndex + 1) == minimumRemainingFieldCount)
						{
							fieldCount = 1;
						}
						else
						{
							throw new InvalidOperationException($"Tuple item {valueIndex} must be terminated by a field named 'NULL': {Type.FullName}");
						}
					}
				}
				else
				{
					fieldCount = mapper.FieldCount.Value;
				}

				values[valueIndex] = mapper.Map(record, recordIndex, fieldCount);
				recordIndex = nullIndex + 1 ?? recordIndex + fieldCount;
			}

			return m_tupleInfo!.CreateNew(values);
		}

		private readonly TupleInfo<T>? m_tupleInfo;
		private readonly IReadOnlyList<IDbTypeMapper>? m_tupleTypeMappers;
	}

	private abstract class SingleFieldMapper<T> : TypeMapper<T>
	{
		public override int? FieldCount => 1;

		protected sealed override T MapCore(IDataRecord record, int index, int count) =>
			count == 1 ? MapField(record, index) : throw BadFieldCount(count);

		protected abstract T MapField(IDataRecord record, int index);
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

	private sealed class EnumMapper<T> : SingleFieldMapper<T>
	{
		protected override T MapField(IDataRecord record, int index)
		{
			var value = record.GetValue(index);
			return value switch
			{
				_ when value == DBNull.Value => throw new InvalidOperationException($"Failed to cast null to {Type.FullName}."),
				T enumValue => enumValue,
				string stringValue => (T) Enum.Parse(typeof(T), stringValue, ignoreCase: true),
				_ => (T) Enum.ToObject(typeof(T), value),
			};
		}
	}

	private sealed class NullableEnumMapper<T>(Type underlyingType) : SingleFieldMapper<T>
	{
		protected override T MapField(IDataRecord record, int index)
		{
			var value = record.GetValue(index);
			try
			{
				return value switch
				{
					_ when value == DBNull.Value => default!,
					T enumValue => enumValue,
					string stringValue => (T) Enum.Parse(underlyingType, stringValue, ignoreCase: true),
					_ => (T) Enum.ToObject(underlyingType, value),
				};
			}
			catch (Exception exception) when (exception is ArgumentException or InvalidCastException)
			{
				throw BadCast(value?.GetType(), exception);
			}
		}
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

	private sealed class StreamMapper : ReferenceValueMapper<Stream>
	{
		protected override Stream MapNotNullField(IDataRecord record, int index)
		{
			if (record is DbDataReader dbReader)
				return dbReader.GetStream(index);

			var byteCount = (int) record.GetBytes(index, 0, null, 0, 0);
			var bytes = new byte[byteCount];
			record.GetBytes(index, 0, bytes, 0, byteCount);
			return new MemoryStream(bytes, 0, byteCount, writable: false);
		}
	}

	private static readonly ConcurrentDictionary<Type, IDbTypeMapper> s_typeMappers = new();
	private static readonly MethodInfo s_createTypeMapper = typeof(DbDataMapper).GetMethod(nameof(CreateTypeMapper), BindingFlags.NonPublic | BindingFlags.Instance, null, [], null)!;
}
