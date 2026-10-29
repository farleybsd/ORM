using My_Orm_Data_Access.Models;
using System.Collections.Concurrent;
using System.Reflection;

namespace My_Orm_Data_Access.Mapping;

// Uma propriedade da classe e a coluna do banco que corresponde a ela
public sealed class ColumnMap
{
    public PropertyInfo Property { get; }
    public string Name { get; }        // nome da coluna: [Column] ou snake_case da propriedade
    public bool IsCalc { get; }        // [CalcField]: só leitura, nunca vai para INSERT/UPDATE
    public bool CanWrite { get; }      // tem set público (ou init)

    internal ColumnMap(PropertyInfo property)
    {
        Property = property;
        Name = property.GetCustomAttribute<ColumnAttribute>()?.Name ?? Naming.ToSnake(property.Name);
        IsCalc = property.GetCustomAttribute<CalcFieldAttribute>() != null;
        CanWrite = property.SetMethod?.IsPublic == true;
    }
}

// O "cartãozinho" de uma classe: calculado uma vez por tipo e reaproveitado para sempre
public sealed class EntityMetadata
{
    private static readonly ConcurrentDictionary<Type, EntityMetadata> cache = new();

    public static EntityMetadata For(Type type) => cache.GetOrAdd(type, t => new EntityMetadata(t));

    public string TableName { get; }
    public IReadOnlyList<ColumnMap> Columns { get; }

    // Como criar o objeto: construtor sem parâmetros (classes comuns)
    // ou o construtor com mais parâmetros (records posicionais)
    public ConstructorInfo Constructor { get; }

    private readonly Dictionary<string, ColumnMap> byColumn;
    private readonly Dictionary<string, ColumnMap> byProperty;

    private EntityMetadata(Type type)
    {
        TableName = ResolveTableName(type);

        Columns = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Select(p => new ColumnMap(p))
            .ToList();

        byColumn = new(StringComparer.OrdinalIgnoreCase);
        foreach (ColumnMap column in Columns)
            byColumn.TryAdd(column.Name, column);

        byProperty = Columns.ToDictionary(c => c.Property.Name, StringComparer.OrdinalIgnoreCase);

        Constructor = type.GetConstructor(Type.EmptyTypes)
            ?? type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"A classe '{type.Name}' não tem construtor público, então o ORM não consegue criá-la.");
    }

    // Coluna do SELECT -> propriedade (null se a classe não tem essa coluna)
    public ColumnMap? FindByColumn(string column) => byColumn.GetValueOrDefault(column);

    // Parâmetro do construtor do record -> propriedade de mesmo nome
    public ColumnMap? FindByProperty(string property) => byProperty.GetValueOrDefault(property);

    private static string ResolveTableName(Type type)
    {
        var table = type.GetCustomAttribute<TableAttribute>();
        if (table != null)
            return table.Name;

        string name = type.Name;
        if (name.EndsWith("Model"))
            name = name[..^5];

        return Naming.ToSnake(name);
    }
}
