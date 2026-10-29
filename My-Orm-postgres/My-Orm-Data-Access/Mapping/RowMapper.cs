using My_Orm_Data_Access.Models;
using System.Data.Common;
using System.Reflection;

namespace My_Orm_Data_Access.Mapping;

// Transforma cada linha do reader em um T, sem passar por DataTable.
// O "plano" (qual coluna do SELECT vai para qual propriedade) é montado uma vez por consulta
// e reaproveitado em todas as linhas.
internal static class RowMapper
{
    public static Func<DbDataReader, T> Create<T>(DbDataReader reader)
    {
        Type type = typeof(T);

        // Tipo simples: a primeira coluna é o valor (ex.: SELECT count(*))
        if (ValueConverter.IsScalar(type))
            return r => (T)ValueConverter.FromDb(r.GetValue(0), type)!;

        EntityMetadata meta = EntityMetadata.For(type);

        // Posição de cada coluna do SELECT (a primeira vence se o nome repetir, ex.: JOIN com dois "id")
        var ordinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++)
            ordinals.TryAdd(reader.GetName(i), i);

        int OrdinalOf(ColumnMap column) => ordinals.GetValueOrDefault(column.Name, -1);

        // Parâmetros do construtor (records): casam com a propriedade de mesmo nome
        ParameterInfo[] parameters = meta.Constructor.GetParameters();
        var byConstructor = new HashSet<ColumnMap>();
        var ctorPlan = parameters.Select(p =>
        {
            ColumnMap? column = meta.FindByProperty(p.Name!);
            if (column != null)
                byConstructor.Add(column);
            return (type: p.ParameterType, ordinal: column == null ? -1 : OrdinalOf(column));
        }).ToArray();

        // Demais propriedades com set público, que tenham coluna no SELECT
        var propertyPlan = meta.Columns
            .Where(c => c.CanWrite && !byConstructor.Contains(c))
            .Select(c => (column: c, ordinal: OrdinalOf(c)))
            .Where(x => x.ordinal >= 0)
            .ToArray();

        return r =>
        {
            object?[] args = ctorPlan
                .Select(p => p.ordinal >= 0 ? ValueConverter.FromDb(r.GetValue(p.ordinal), p.type)
                                            : ValueConverter.Default(p.type))
                .ToArray();

            object obj = meta.Constructor.Invoke(args);

            foreach (var (column, ordinal) in propertyPlan)
                column.Property.SetValue(obj,
                    ValueConverter.FromDb(r.GetValue(ordinal), column.Property.PropertyType));

            // Model já sai pronto para Save: grava só o que mudar depois daqui
            if (obj is Model model)
                model.MarkLoaded();

            return (T)obj;
        };
    }
}
