using DDDWithHttp.Orm;

namespace Sinistros.Api.Infrastructure.Persistencia;

/// <summary>Em qual banco os sinistros são gravados. Configurado na seção <see cref="SectionName"/> do appsettings.</summary>
public sealed class PersistenciaOptions
{
    public const string SectionName = "Persistencia";

    public DatabaseProvider Banco { get; set; } = DatabaseProvider.SqlServer;
}
