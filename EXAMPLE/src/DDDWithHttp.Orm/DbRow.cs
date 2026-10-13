using System.Data.Common;
using System.Globalization;

namespace DDDWithHttp.Orm;

/// <summary>
/// Uma linha lida do banco, desacoplada do <see cref="DbDataReader"/> (que já foi fechado).
/// A conversão de tipos fica aqui para que o repositório trate igual Postgres e SQL Server
/// (ex.: coluna DATE vem como <see cref="DateTime"/> nos dois drivers e é lida como <see cref="DateOnly"/>).
/// </summary>
public sealed class DbRow
{
    private readonly Dictionary<string, object?> _values;

    private DbRow(Dictionary<string, object?> values) => _values = values;

    internal static DbRow From(DbDataReader reader)
    {
        // Postgres devolve nomes em minúsculas; SQL Server como foram escritos.
        var values = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < reader.FieldCount; i++)
            values[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

        return new DbRow(values);
    }

    public bool IsNull(string column) => Raw(column) is null;

    /// <summary>Lê a coluna convertendo para <typeparamref name="T"/>. Nulo só é aceito se T for anulável.</summary>
    public T Get<T>(string column)
    {
        object? raw = Raw(column);

        if (raw is null)
        {
            if (default(T) is null)
                return default!;

            throw new OrmException(OrmErrorKind.NotNull,
                $"A coluna '{column}' veio nula, mas o tipo {typeof(T).Name} não aceita nulo.", column);
        }

        Type target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)Convert(raw, target, column);
    }

    private object? Raw(string column)
    {
        if (!_values.TryGetValue(column, out object? value))
            throw new OrmException(OrmErrorKind.ColumnNotFound,
                $"A coluna '{column}' não está no resultado da consulta.", column);

        return value;
    }

    private static object Convert(object raw, Type target, string column)
    {
        if (target.IsInstanceOfType(raw) && target != typeof(DateTime))
            return raw;

        try
        {
            if (target.IsEnum)
                return raw is string text
                    ? Enum.Parse(target, text, ignoreCase: true)
                    : Enum.ToObject(target, raw);

            if (target == typeof(DateOnly))
                return raw switch
                {
                    DateTime dt => DateOnly.FromDateTime(dt),
                    DateTimeOffset dto => DateOnly.FromDateTime(dto.UtcDateTime),
                    _ => DateOnly.Parse(raw.ToString()!, CultureInfo.InvariantCulture)
                };

            // Convenção do ORM: todo DateTime gravado é UTC.
            // O SQL Server devolve Kind=Unspecified; aqui ele volta a ser UTC.
            if (target == typeof(DateTime))
                return raw switch
                {
                    DateTime dt when dt.Kind == DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
                    DateTime dt => dt.ToUniversalTime(),
                    DateTimeOffset dto => dto.UtcDateTime,
                    _ => System.Convert.ToDateTime(raw, CultureInfo.InvariantCulture)
                };

            if (target == typeof(Guid))
                return raw is string s ? Guid.Parse(s) : raw;

            return System.Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or ArgumentException or OverflowException)
        {
            throw new OrmException(OrmErrorKind.Other,
                $"Não foi possível converter a coluna '{column}' ({raw.GetType().Name}) para {target.Name}.",
                column, ex);
        }
    }
}
