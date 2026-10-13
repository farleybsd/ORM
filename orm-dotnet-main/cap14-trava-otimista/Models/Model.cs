using System.Data;
using System.Reflection;
using MeuOrm.Core;

namespace MeuOrm.Models;

public abstract class Model
{
    // Colunas que toda tabela do sistema tem
    public long Id { get; set; }
    public long InsertedAt { get; set; }
    public long UpdatedAt { get; set; }

    // Capítulo 13: valores como estavam no banco na última leitura ou gravação
    private readonly Dictionary<string, object?> snapshot = new();

    // Nome da tabela pela convenção: Cliente -> cliente, PedidoItemModel -> pedido_item
    protected virtual string TableName
    {
        get
        {
            string name = GetType().Name;

            if (name.EndsWith("Model"))
                name = name[..^5];        // tira as 5 letras de "Model"

            return Naming.ToSnake(name);
        }
    }

    // As propriedades que viram colunas: públicas, com get e set
    protected IEnumerable<PropertyInfo> Columns()
    {
        return GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);
    }

    public bool Load(long id)
    {
        using var orm = new Orm();
        DataTable dt = orm.Select(TableName, "*", "id = @id", new() { ["id"] = id });

        if (dt.Rows.Count == 0)
            return false;           // não achou: o objeto continua como estava

        Fill(dt.Rows[0]);
        TakeSnapshot();
        return true;
    }

    // Copia uma linha do banco para as propriedades do objeto
    protected void Fill(DataRow row)
    {
        foreach (PropertyInfo prop in Columns())
        {
            string column = Naming.ToSnake(prop.Name);

            if (!row.Table.Columns.Contains(column))
                continue;           // propriedade sem coluna correspondente

            object value = row[column];

            if (value == DBNull.Value)
            {
                prop.SetValue(this, null);
                continue;
            }

            // int? vira int, long? vira long... e converte o valor para o tipo da propriedade
            Type target = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            prop.SetValue(this, Convert.ChangeType(value, target));
        }
    }

    // Devolve false quando não havia nada para gravar
    public bool Save()
    {
        var data = new Dictionary<string, object?>();

        foreach (PropertyInfo prop in Columns())
        {
            string column = Naming.ToSnake(prop.Name);

            if (column is "id" or "inserted_at" or "updated_at")
                continue;

            object? value = prop.GetValue(this);

            // Em UPDATE, pula o campo que está igual à foto
            if (Id != 0 && snapshot.TryGetValue(column, out object? old) && Equals(old, value))
                continue;

            data[column] = value;
        }

        if (Id != 0 && data.Count == 0)
            return false;                       // nada mudou: nem vai ao banco

        // Capítulo 14: garante que a nova versão é sempre maior que a anterior,
        // mesmo se duas gravações caírem no mesmo milissegundo
        long now = Math.Max(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), UpdatedAt + 1);
        using var orm = new Orm();

        if (Id == 0)
        {
            data["inserted_at"] = now;
            data["updated_at"] = now;
            orm.Insert(TableName, data);

            Id = orm.GetGeneratedId();
            InsertedAt = now;
            UpdatedAt = now;
        }
        else
        {
            data["updated_at"] = now;

            orm.Update(TableName, data,
                "id = @id AND updated_at = @old_updated_at",
                new() { ["id"] = Id, ["old_updated_at"] = UpdatedAt });

            if (orm.GetRowsAffected() == 0)
                throw new OrmException(OrmErrorKind.Concurrency,
                    "Este registro foi alterado ou excluído por outro usuário. " +
                    "Recarregue os dados e tente novamente.");

            UpdatedAt = now;
        }

        TakeSnapshot();                         // a nova foto é o que acabou de ser gravado
        return true;
    }

    private void TakeSnapshot()
    {
        snapshot.Clear();

        foreach (PropertyInfo prop in Columns())
            snapshot[Naming.ToSnake(prop.Name)] = prop.GetValue(this);
    }

    public void Delete()
    {
        if (Id == 0)
            throw new InvalidOperationException("Não dá para apagar um registro que nunca foi gravado.");

        using var orm = new Orm();
        orm.Delete(TableName, "id = @id", new() { ["id"] = Id });
        Id = 0;
    }
}
