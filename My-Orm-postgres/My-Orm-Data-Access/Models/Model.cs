using My_Orm_Data_Access.Core;
using System.Data;
using System.Reflection;

namespace My_Orm_Data_Access.Models
{
    public abstract class Model
    {
        public long Id { get; set; }
        public long InsertedAt { get; set; }
        public long UpdatedAt { get; set; }

        private readonly Dictionary<string, object?> snapshot = new();

        protected virtual string TableName
        {
            get
            {
                var table = GetType().GetCustomAttribute<TableAttribute>();
                if (table != null)
                    return table.Name;

                string name = GetType().Name;
                if (name.EndsWith("Model"))
                    name = name[..^5];

                return Naming.ToSnake(name);
            }
        }

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
                return false;

            Fill(dt.Rows[0]);
            TakeSnapshot();
            return true;
        }

        public bool LoadQuery(string sql, Dictionary<string, object?>? args = null)
        {
            using var orm = new Orm();
            orm.Exec(sql, "query", args);

            DataTable dt = orm.GetDataTable();
            if (dt.Rows.Count == 0)
                return false;

            Fill(dt.Rows[0]);
            TakeSnapshot();
            return true;
        }

        public bool Save()
        {
            var data = new Dictionary<string, object?>();

            foreach (PropertyInfo prop in Columns())
            {
                string column = Naming.ToSnake(prop.Name);

                if (column is "id" or "inserted_at" or "updated_at")
                    continue;

                if (prop.GetCustomAttribute<CalcFieldAttribute>() != null)
                    continue;

                object? value = prop.GetValue(this);

                if (Id != 0 && snapshot.TryGetValue(column, out object? old) && Equals(old, value))
                    continue;

                data[column] = value;
            }

            if (Id != 0 && data.Count == 0)
                return false;

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

            TakeSnapshot();
            return true;
        }

        public void Delete()
        {
            if (Id == 0)
                throw new InvalidOperationException("Não dá para apagar um registro que nunca foi gravado.");

            using var orm = new Orm();
            orm.Delete(TableName, "id = @id", new() { ["id"] = Id });
            Id = 0;
            snapshot.Clear();
        }

        protected void Fill(DataRow row)
        {
            foreach (PropertyInfo prop in Columns())
            {
                string column = Naming.ToSnake(prop.Name);

                if (!row.Table.Columns.Contains(column))
                    continue;

                object value = row[column];

                if (value == DBNull.Value)
                {
                    prop.SetValue(this, null);
                    continue;
                }

                Type target = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                prop.SetValue(this, Convert.ChangeType(value, target));
            }
        }

        private void TakeSnapshot()
        {
            snapshot.Clear();

            foreach (PropertyInfo prop in Columns())
                snapshot[Naming.ToSnake(prop.Name)] = prop.GetValue(this);
        }
    }
}
