using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace DDDWithHttp.Orm.Dialects;

internal sealed partial class SqlServerDialect : ISqlDialect
{
    // Números de erro do SQL Server (sys.messages)
    private static class ErrorNumber
    {
        public const int Timeout = -2;
        public const int ColumnNotFound = 207;
        public const int TableNotFound = 208;
        public const int NotNull = 515;
        public const int ConstraintConflict = 547;   // FK ou CHECK: a mensagem diz qual
        public const int Deadlock = 1205;
        public const int UniqueIndex = 2601;
        public const int UniqueConstraint = 2627;
        public const int SnapshotConflict = 3960;
        public const int LoginFailed = 18456;
    }

    public DatabaseProvider Provider => DatabaseProvider.SqlServer;

    public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    public void BindValue(DbParameter parameter, object? value)
    {
        switch (value)
        {
            case null:
                parameter.Value = DBNull.Value;
                break;
            case Enum e:
                parameter.Value = e.ToString();   // convenção: enum é gravado como texto
                break;
            case DateOnly d:
                parameter.DbType = DbType.Date;
                parameter.Value = d.ToDateTime(TimeOnly.MinValue);
                break;
            case DateTime dt:
                parameter.DbType = DbType.DateTime2;
                parameter.Value = dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime() : dt;
                break;
            case string s:
                // Tamanho fixo evita um plano de execução por tamanho de string (plan cache pollution)
                parameter.DbType = DbType.String;
                parameter.Size = s.Length <= 4000 ? 4000 : -1;
                parameter.Value = s;
                break;
            default:
                parameter.Value = value;
                break;
        }
    }

    public OrmException Translate(DbException exception)
    {
        if (exception is not SqlException sql)
            return new OrmException(OrmErrorKind.Connection, $"Falha de conexão com o SQL Server: {exception.Message}", inner: exception);

        return sql.Number switch
        {
            ErrorNumber.UniqueIndex or ErrorNumber.UniqueConstraint => new(OrmErrorKind.Duplicate,
                "Registro duplicado.", ExtractConstraint(sql.Message), sql),
            ErrorNumber.ConstraintConflict when sql.Message.Contains("CHECK", StringComparison.OrdinalIgnoreCase) =>
                new(OrmErrorKind.CheckViolation, "Regra do banco violada.", ExtractConstraint(sql.Message), sql),
            ErrorNumber.ConstraintConflict => new(OrmErrorKind.ForeignKey,
                "Referência inválida.", ExtractConstraint(sql.Message), sql),
            ErrorNumber.NotNull => new(OrmErrorKind.NotNull, sql.Message, inner: sql),
            ErrorNumber.TableNotFound => new(OrmErrorKind.TableNotFound, sql.Message, inner: sql),
            ErrorNumber.ColumnNotFound => new(OrmErrorKind.ColumnNotFound, sql.Message, inner: sql),
            ErrorNumber.Deadlock => new(OrmErrorKind.Deadlock, "Deadlock detectado. A transação foi desfeita.", inner: sql),
            ErrorNumber.SnapshotConflict => new(OrmErrorKind.SerializationFailure,
                "Conflito de snapshot. A transação foi desfeita.", inner: sql),
            ErrorNumber.Timeout => new(OrmErrorKind.Timeout, "O comando excedeu o tempo limite.", inner: sql),
            ErrorNumber.LoginFailed => new(OrmErrorKind.Connection, sql.Message, inner: sql),
            _ when sql.Class >= 20 => new(OrmErrorKind.Connection, sql.Message, inner: sql),  // severidade 20+ derruba a conexão
            _ => new(OrmErrorKind.Other, sql.Message, inner: sql)
        };
    }

    // Ex.: ... UNIQUE KEY constraint 'uq_apolice_numero' ...  /  ... FOREIGN KEY constraint "fk_x" ...
    //      ... with unique index 'ix_y' ...
    [GeneratedRegex("""(?:constraint|index)\s+['"](?<name>[^'"]+)['"]""", RegexOptions.IgnoreCase)]
    private static partial Regex ConstraintNameRegex();

    private static string ExtractConstraint(string message)
    {
        Match match = ConstraintNameRegex().Match(message);
        return match.Success ? match.Groups["name"].Value : string.Empty;
    }
}
