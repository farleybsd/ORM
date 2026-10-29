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

        // ===== Carregar =====
        public bool Load(IOrm db, long id)
            => FillFirst(db.Select(TableName, "*", "id = @id", IdArgs(id)));

        public async Task<bool> LoadAsync(IOrm db, long id, CancellationToken ct = default)
            => FillFirst(await db.SelectAsync(TableName, "*", "id = @id", IdArgs(id), ct: ct));

        public bool LoadQuery(IOrm db, string sql, Dictionary<string, object?>? args = null)
            => FillFirst(db.Query(sql, args));

        public async Task<bool> LoadQueryAsync(IOrm db, string sql, Dictionary<string, object?>? args = null,
                                               CancellationToken ct = default)
            => FillFirst(await db.QueryAsync(sql, args, ct));

        // ===== Gravar =====
        public bool Save(IOrm db)
        {
            var data = ChangedColumns();
            if (Id != 0 && data.Count == 0)
                return false;

            long now = NextTimestamp();
            data["updated_at"] = now;

            if (Id == 0)
            {
                data["inserted_at"] = now;
                Id = db.Insert(TableName, data);
                InsertedAt = now;
            }
            else
            {
                EnsureUpdated(db.Update(TableName, data, VersionWhere, VersionArgs()));
            }

            UpdatedAt = now;
            TakeSnapshot();
            return true;
        }

        public async Task<bool> SaveAsync(IOrm db, CancellationToken ct = default)
        {
            var data = ChangedColumns();
            if (Id != 0 && data.Count == 0)
                return false;

            long now = NextTimestamp();
            data["updated_at"] = now;

            if (Id == 0)
            {
                data["inserted_at"] = now;
                Id = await db.InsertAsync(TableName, data, ct);
                InsertedAt = now;
            }
            else
            {
                EnsureUpdated(await db.UpdateAsync(TableName, data, VersionWhere, VersionArgs(), ct));
            }

            UpdatedAt = now;
            TakeSnapshot();
            return true;
        }

        // ===== Apagar =====
        public void Delete(IOrm db)
        {
            EnsureSaved();
            db.Delete(TableName, "id = @id", IdArgs(Id));
            Forget();
        }

        public async Task DeleteAsync(IOrm db, CancellationToken ct = default)
        {
            EnsureSaved();
            await db.DeleteAsync(TableName, "id = @id", IdArgs(Id), ct);
            Forget();
        }

        // ===== Peças comuns às versões síncrona e Async =====

        // Concorrência otimista: só atualiza se ninguém mexeu desde que carregamos
        private const string VersionWhere = "id = @id AND updated_at = @old_updated_at";

        private Dictionary<string, object?> VersionArgs()
            => new() { ["id"] = Id, ["old_updated_at"] = UpdatedAt };

        private static Dictionary<string, object?> IdArgs(long id)
            => new() { ["id"] = id };

        private long NextTimestamp()
            => Math.Max(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), UpdatedAt + 1);

        // Colunas que vão para o banco: no insert todas, no update só as que mudaram
        private Dictionary<string, object?> ChangedColumns()
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

            return data;
        }

        private static void EnsureUpdated(long rows)
        {
            if (rows == 0)
                throw new OrmException(OrmErrorKind.Concurrency,
                    "Este registro foi alterado ou excluído por outro usuário. " +
                    "Recarregue os dados e tente novamente.");
        }

        private void EnsureSaved()
        {
            if (Id == 0)
                throw new InvalidOperationException("Não dá para apagar um registro que nunca foi gravado.");
        }

        private void Forget()
        {
            Id = 0;
            snapshot.Clear();
        }

        private bool FillFirst(DataTable dt)
        {
            if (dt.Rows.Count == 0)
                return false;

            Fill(dt.Rows[0]);
            TakeSnapshot();
            return true;
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
