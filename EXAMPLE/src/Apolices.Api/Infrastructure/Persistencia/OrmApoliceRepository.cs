using Apolices.Api.Common;
using Apolices.Api.Domain;
using DDDWithHttp.Orm;
using DDDWithHttp.Orm.Sessions;

namespace Apolices.Api.Infrastructure.Persistencia;

/// <summary>
/// Repositório do agregado Apólice sobre o DDDWithHttp.Orm. Funciona com Postgres ou SQL Server:
/// o banco é escolhido na configuração (<see cref="PersistenciaOptions"/>).
/// <para>
/// Regras de transação aplicadas aqui:
/// <list type="bullet">
/// <item>Apólice + coberturas são gravadas sempre na mesma transação (o agregado é a unidade de consistência).</item>
/// <item>A raiz (tabela apolice) é sempre a primeira linha tocada. Com a mesma ordem de lock em todas
///   as operações, duas requisições sobre a mesma apólice esperam uma pela outra em vez de entrar em deadlock.</item>
/// <item>Concorrência otimista pela coluna versao: nenhuma linha fica travada entre a leitura (GET) e a
///   gravação (PUT/DELETE); quem gravar com versão velha recebe conflito.</item>
/// <item>Se o caso de uso já abriu uma transação na sessão, o repositório participa dela.</item>
/// </list>
/// </para>
/// </summary>
internal sealed class OrmApoliceRepository(
    IDbSessionProvider sessions,
    DatabaseProvider banco,
    TimeProvider relogio) : IApoliceRepository
{
    private const int VersaoInicial = 1;

    private IDbSession Sessao => sessions.Get(banco);

    public async Task<Apolice?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var linhas = await Sessao.QueryAsync(ApoliceSql.ObterPorId, new() { [TabelaApolice.Id] = id }, cancellationToken);
        return Montar(linhas).FirstOrDefault();
    }

    public async Task<IReadOnlyList<Apolice>> ObterTodasAsync(CancellationToken cancellationToken)
    {
        var linhas = await Sessao.QueryAsync(ApoliceSql.ObterTodas, ct: cancellationToken);
        return Montar(linhas);
    }

    public async Task<Result> AdicionarAsync(Apolice apolice, CancellationToken cancellationToken)
    {
        var agora = relogio.GetUtcNow().UtcDateTime;

        try
        {
            await Sessao.ExecuteInTransactionAsync(async ct =>
            {
                var dados = DadosDaApolice(apolice, agora);
                dados[TabelaApolice.Id] = apolice.Id;
                dados[TabelaApolice.Versao] = VersaoInicial;
                dados[TabelaApolice.CriadoEm] = agora;

                await Sessao.InsertAsync(TabelaApolice.Nome, dados, ct);
                await InserirCoberturasAsync(apolice, ct);
            }, ct: cancellationToken);
        }
        catch (OrmException ex) when (EhNumeroDuplicado(ex))
        {
            return Result.Failure(ErrosDeApolice.NumeroDuplicado(apolice.Numero));
        }

        apolice.Versao = VersaoInicial;
        return Result.Success();
    }

    public async Task<Result> AtualizarAsync(Apolice apolice, int versaoEsperada, CancellationToken cancellationToken)
    {
        var agora = relogio.GetUtcNow().UtcDateTime;
        int novaVersao;

        try
        {
            novaVersao = await Sessao.ExecuteInTransactionAsync(async ct =>
            {
                // 1º a raiz: confere a versão e trava a linha até o fim da transação
                int versao = await Sessao.UpdateVersionedAsync(ChaveVersionada(apolice.Id, versaoEsperada),
                    DadosDaApolice(apolice, agora), ct);

                // 2º os filhos: as coberturas são substituídas inteiras (são value objects)
                await Sessao.DeleteAsync(TabelaCobertura.Nome, ApoliceSql.FiltroCoberturasDaApolice,
                    new() { [TabelaCobertura.ApoliceId] = apolice.Id }, ct);
                await InserirCoberturasAsync(apolice, ct);

                return versao;
            }, ct: cancellationToken);
        }
        catch (OrmException ex) when (ex.Kind == OrmErrorKind.Concurrency)
        {
            return Result.Failure(ErrosDeApolice.VersaoDesatualizada(apolice.Id, versaoEsperada));
        }
        catch (OrmException ex) when (EhNumeroDuplicado(ex))
        {
            return Result.Failure(ErrosDeApolice.NumeroDuplicado(apolice.Numero));
        }

        apolice.Versao = novaVersao;
        return Result.Success();
    }

    public async Task<Result> RemoverAsync(Guid id, int versaoEsperada, CancellationToken cancellationToken)
    {
        var agora = relogio.GetUtcNow().UtcDateTime;

        try
        {
            await Sessao.ExecuteInTransactionAsync(async ct =>
            {
                // A raiz primeiro (mesma ordem de lock do Atualizar): confere a versão e trava a linha.
                // Se a versão estiver velha, nada é apagado.
                await Sessao.UpdateVersionedAsync(ChaveVersionada(id, versaoEsperada),
                    new() { [TabelaApolice.AtualizadoEm] = agora }, ct);

                await Sessao.DeleteAsync(TabelaCobertura.Nome, ApoliceSql.FiltroCoberturasDaApolice,
                    new() { [TabelaCobertura.ApoliceId] = id }, ct);
                await Sessao.DeleteAsync(TabelaApolice.Nome, ApoliceSql.FiltroPorId,
                    new() { [TabelaApolice.Id] = id }, ct);
            }, ct: cancellationToken);
        }
        catch (OrmException ex) when (ex.Kind == OrmErrorKind.Concurrency)
        {
            return Result.Failure(ErrosDeApolice.VersaoDesatualizada(id, versaoEsperada));
        }

        return Result.Success();
    }

    private async Task InserirCoberturasAsync(Apolice apolice, CancellationToken ct)
    {
        foreach (var cobertura in apolice.Coberturas)
        {
            await Sessao.InsertAsync(TabelaCobertura.Nome, new()
            {
                [TabelaCobertura.ApoliceId] = apolice.Id,
                [TabelaCobertura.Tipo] = cobertura.Tipo,
                [TabelaCobertura.LimiteIndenizacao] = cobertura.LimiteIndenizacao
            }, ct);
        }
    }

    private static Dictionary<string, object?> DadosDaApolice(Apolice apolice, DateTime agora) => new()
    {
        [TabelaApolice.Numero] = apolice.Numero,
        [TabelaApolice.SeguradoNome] = apolice.Segurado.Nome,
        [TabelaApolice.SeguradoDocumento] = apolice.Segurado.Documento,
        [TabelaApolice.VigenciaInicio] = apolice.Vigencia.Inicio,
        [TabelaApolice.VigenciaFim] = apolice.Vigencia.Fim,
        [TabelaApolice.AtualizadoEm] = agora
    };

    private static VersionedKey ChaveVersionada(Guid id, int versaoEsperada)
        => new(TabelaApolice.Nome, TabelaApolice.Id, id, versaoEsperada, TabelaApolice.Versao);

    private static bool EhNumeroDuplicado(OrmException ex)
        => ex.Kind == OrmErrorKind.Duplicate && ex.Detail.Equals(TabelaApolice.UqNumero, StringComparison.OrdinalIgnoreCase);

    // O JOIN devolve uma linha por cobertura; agrupa de volta em agregados (GroupBy preserva a ordem).
    private static List<Apolice> Montar(IReadOnlyList<DbRow> linhas) => linhas
        .GroupBy(l => l.Get<Guid>(TabelaApolice.Id))
        .Select(grupo =>
        {
            var raiz = grupo.First();

            var coberturas = grupo
                .Where(l => !l.IsNull(TabelaCobertura.Tipo))
                .Select(l => new Cobertura(
                    l.Get<TipoCobertura>(TabelaCobertura.Tipo),
                    l.Get<decimal>(TabelaCobertura.LimiteIndenizacao)));

            return Apolice.Reconstituir(
                grupo.Key,
                raiz.Get<string>(TabelaApolice.Numero),
                new Segurado(raiz.Get<string>(TabelaApolice.SeguradoNome), raiz.Get<string>(TabelaApolice.SeguradoDocumento)),
                new PeriodoVigencia(raiz.Get<DateOnly>(TabelaApolice.VigenciaInicio), raiz.Get<DateOnly>(TabelaApolice.VigenciaFim)),
                coberturas,
                raiz.Get<int>(TabelaApolice.Versao));
        })
        .ToList();
}
