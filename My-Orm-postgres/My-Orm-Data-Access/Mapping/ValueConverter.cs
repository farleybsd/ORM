using System.Globalization;

namespace My_Orm_Data_Access.Mapping;

// Converte valores entre o que o banco devolve e o tipo da propriedade C#
public static class ValueConverter
{
    // Tipos que viram "um valor só" (ex.: QueryFirst<long>("SELECT count(*) ..."))
    public static bool IsScalar(Type type)
    {
        Type t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t.IsEnum
            || t == typeof(string) || t == typeof(decimal) || t == typeof(Guid)
            || t == typeof(DateTime) || t == typeof(DateTimeOffset)
            || t == typeof(DateOnly) || t == typeof(TimeOnly) || t == typeof(TimeSpan)
            || t == typeof(byte[]);
    }

    // Banco -> C#
    public static object? FromDb(object? value, Type target)
    {
        if (value is null or DBNull)
            return Default(target);   // null para referência/Nullable, 0/false para int/bool

        Type t = Nullable.GetUnderlyingType(target) ?? target;

        if (t.IsInstanceOfType(value))
            return value;

        if (t.IsEnum)
            return value is string s ? Enum.Parse(t, s, ignoreCase: true) : Enum.ToObject(t, value);

        if (t == typeof(Guid))
            return value is byte[] bytes ? new Guid(bytes) : Guid.Parse(value.ToString()!);

        // timestamp sem fuso (Kind = Unspecified) é tratado como UTC
        if (t == typeof(DateTimeOffset) && value is DateTime dt)
            return new DateTimeOffset(dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt);

        if (t == typeof(DateTime) && value is DateTimeOffset dto)
            return dto.UtcDateTime;

        if (t == typeof(DateOnly) && value is DateTime d)
            return DateOnly.FromDateTime(d);

        if (t == typeof(TimeOnly) && value is TimeSpan ts)
            return TimeOnly.FromTimeSpan(ts);

        return Convert.ChangeType(value, t, CultureInfo.InvariantCulture);
    }

    // C# -> banco: enum vai como número (coluna int)
    public static object? ToDb(object? value)
    {
        if (value is Enum e)
            return Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType()), CultureInfo.InvariantCulture);

        return value;
    }

    public static object? Default(Type type)
        => type.IsValueType ? Activator.CreateInstance(type) : null;
}
