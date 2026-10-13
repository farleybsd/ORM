using DDDWithHttp.Orm;

namespace Apolices.Api.Infrastructure.Persistencia;

/// <summary>Em qual banco as apólices são gravadas. Configurado na seção <see cref="SectionName"/> do appsettings.</summary>
public sealed class PersistenciaOptions
{
    public const string SectionName = "Persistencia";

    public DatabaseProvider Banco { get; set; } = DatabaseProvider.Postgres;
}
