using DDDWithHttp.Orm;
using DDDWithHttp.Orm.Sessions;
using Sinistros.Api.Common;
using Sinistros.Api.Domain;

namespace Sinistros.Api.Infrastructure.Persistencia;

/// <summary>
/// Repositório do agregado Sinistro sobre o DDDWithHttp.Orm. Funciona com Postgres ou SQL Server:
/// o banco é escolhido na configuração (<see cref="PersistenciaOptions"/>).
/// <para>
/// O sinistro ocupa uma única linha, então cada gravação é um único comando — e um comando
/// isolado já é atômico, sem transação explícita. Se o caso de uso abrir uma transação na
/// sessão (para gravar outras coisas junto), estes comandos participam dela automaticamente.
/// </para>
/// Concorrência otimista pela coluna versao: nenhuma linha fica travada entre a leitura e a gravação.
/// </summary>
internal sealed class OrmSinistroRepository(
    IDbSessionProvider sessions,
    DatabaseProvider banco,
    TimeProvider relogio) : ISinistroRepository
{
    private const int VersaoInicial = 1;

    private IDbSession Sessao => sessions.Get(banco);

    public async Task AdicionarAsync(Sinistro sinistro, CancellationToken cancellationToken)
    {
        await Sessao.InsertAsync(TabelaSinistro.Nome, new()
        {
            [TabelaSinistro.Id] = sinistro.Id,
            [TabelaSinistro.ApoliceId] = sinistro.ApoliceId,
            [TabelaSinistro.DataOcorrencia] = sinistro.DataOcorrencia,
            [TabelaSinistro.TipoCobertura] = sinistro.TipoCobertura,
            [TabelaSinistro.Descricao] = sinistro.Descricao,
            [TabelaSinistro.Status] = sinistro.Status,
            [TabelaSinistro.DataRegistro] = sinistro.DataRegistro,
            [TabelaSinistro.Versao] = VersaoInicial,
            [TabelaSinistro.AtualizadoEm] = relogio.GetUtcNow().UtcDateTime
        }, cancellationToken);

        sinistro.Versao = VersaoInicial;
    }

    public async Task<Sinistro?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var linha = await Sessao.QuerySingleOrDefaultAsync(SinistroSql.ObterPorId,
            new() { [TabelaSinistro.Id] = id }, cancellationToken);

        return linha is null ? null : Montar(linha);
    }

    public async Task<IReadOnlyList<Sinistro>> ListarPorApoliceAsync(Guid apoliceId, CancellationToken cancellationToken)
    {
        var linhas = await Sessao.QueryAsync(SinistroSql.ListarPorApolice,
            new() { [TabelaSinistro.ApoliceId] = apoliceId }, cancellationToken);

        return linhas.Select(Montar).ToList();
    }

    public async Task<Result> AtualizarAsync(Sinistro sinistro, int versaoEsperada, CancellationToken cancellationToken)
    {
        try
        {
            // Só as colunas que podem mudar vão no UPDATE
            sinistro.Versao = await Sessao.UpdateVersionedAsync(ChaveVersionada(sinistro.Id, versaoEsperada), new()
            {
                [TabelaSinistro.Descricao] = sinistro.Descricao,
                [TabelaSinistro.AtualizadoEm] = relogio.GetUtcNow().UtcDateTime
            }, cancellationToken);
        }
        catch (OrmException ex) when (ex.Kind == OrmErrorKind.Concurrency)
        {
            return Result.Failure(ErrosDeSinistro.VersaoDesatualizada(sinistro.Id, versaoEsperada));
        }

        return Result.Success();
    }

    public async Task<Result> RemoverAsync(Guid id, int versaoEsperada, CancellationToken cancellationToken)
    {
        try
        {
            await Sessao.DeleteVersionedAsync(ChaveVersionada(id, versaoEsperada), cancellationToken);
        }
        catch (OrmException ex) when (ex.Kind == OrmErrorKind.Concurrency)
        {
            return Result.Failure(ErrosDeSinistro.VersaoDesatualizada(id, versaoEsperada));
        }

        return Result.Success();
    }

    private static VersionedKey ChaveVersionada(Guid id, int versaoEsperada)
        => new(TabelaSinistro.Nome, TabelaSinistro.Id, id, versaoEsperada, TabelaSinistro.Versao);

    private static Sinistro Montar(DbRow linha) => Sinistro.Reconstituir(
        linha.Get<Guid>(TabelaSinistro.Id),
        linha.Get<Guid>(TabelaSinistro.ApoliceId),
        linha.Get<DateOnly>(TabelaSinistro.DataOcorrencia),
        linha.Get<TipoCobertura>(TabelaSinistro.TipoCobertura),
        linha.Get<string>(TabelaSinistro.Descricao),
        linha.Get<StatusSinistro>(TabelaSinistro.Status),
        linha.Get<DateTime>(TabelaSinistro.DataRegistro),
        linha.Get<int>(TabelaSinistro.Versao));
}
