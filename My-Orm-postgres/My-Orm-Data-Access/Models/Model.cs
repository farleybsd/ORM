using My_Orm_Data_Access.Core;
using My_Orm_Data_Access.Mapping;
using System.Data;

namespace My_Orm_Data_Access.Models
{
    public abstract class Model
    {
        public long Id { get; set; }
        public long InsertedAt { get; set; }
        public long UpdatedAt { get; set; }

        private readonly Dictionary<string, object?> snapshot = new();

        // Metadados da classe (tabela, colunas, atributos): calculados uma vez por tipo
        private EntityMetadata Meta => EntityMetadata.For(GetType());

        protected virtual string TableName => Meta.TableName;

        // Colunas que o Model lê e grava (propriedades com get e set públicos)
        private IEnumerable<ColumnMap> Columns() => Meta.Columns.Where(c => c.CanWrite);

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

            foreach (ColumnMap column in Columns())
            {
                if (column.Name is "id" or "inserted_at" or "updated_at")
                    continue;

                if (column.IsCalc)
                    continue;

                object? value = column.Property.GetValue(this);

                if (Id != 0 && snapshot.TryGetValue(column.Name, out object? old) && Equals(old, value))
                    continue;

                data[column.Name] = ValueConverter.ToDb(value);
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
            foreach (ColumnMap column in Columns())
            {
                if (!row.Table.Columns.Contains(column.Name))
                    continue;

                column.Property.SetValue(this,
                    ValueConverter.FromDb(row[column.Name], column.Property.PropertyType));
            }
        }

        // Chamado pelo Query<T> depois de preencher o objeto
        internal void MarkLoaded() => TakeSnapshot();

        private void TakeSnapshot()
        {
            snapshot.Clear();

            foreach (ColumnMap column in Columns())
                snapshot[column.Name] = column.Property.GetValue(this);
        }
    }
}
