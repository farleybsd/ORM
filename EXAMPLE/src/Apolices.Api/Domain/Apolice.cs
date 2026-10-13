using Apolices.Api.Common;

namespace Apolices.Api.Domain;

public sealed class Apolice
{
    private List<Cobertura> _coberturas;

    public Guid Id { get; }
    public string Numero { get; private set; }
    public Segurado Segurado { get; private set; }
    public PeriodoVigencia Vigencia { get; private set; }
    public IReadOnlyList<Cobertura> Coberturas => _coberturas;

    /// <summary>
    /// Versão para concorrência otimista: 0 enquanto não foi gravada; o repositório atualiza a
    /// cada gravação. O cliente devolve a versão que leu e a gravação falha se ela mudou.
    /// </summary>
    public int Versao { get; internal set; }

    private Apolice(Guid id, string numero, Segurado segurado, PeriodoVigencia vigencia, List<Cobertura> coberturas, int versao)
    {
        Id = id;
        Numero = numero;
        Segurado = segurado;
        Vigencia = vigencia;
        _coberturas = coberturas;
        Versao = versao;
    }

    public static Result<Apolice> Criar(
        Guid id,
        string numero,
        Segurado segurado,
        PeriodoVigencia vigencia,
        IEnumerable<Cobertura> coberturas)
    {
        var listaCoberturas = coberturas.ToList();

        var validacao = Validar(numero, listaCoberturas);
        if (validacao.IsFailure)
            return validacao.Error!;

        return Result.Success(new Apolice(id, numero, segurado, vigencia, listaCoberturas, versao: 0));
    }

    /// <summary>
    /// Recria a apólice a partir de dados já persistidos (e portanto já validados).
    /// Uso exclusivo dos repositórios.
    /// </summary>
    internal static Apolice Reconstituir(
        Guid id,
        string numero,
        Segurado segurado,
        PeriodoVigencia vigencia,
        IEnumerable<Cobertura> coberturas,
        int versao)
        => new(id, numero, segurado, vigencia, coberturas.ToList(), versao);

    public Result Atualizar(string numero, Segurado segurado, PeriodoVigencia vigencia, IEnumerable<Cobertura> coberturas)
    {
        var listaCoberturas = coberturas.ToList();

        var validacao = Validar(numero, listaCoberturas);
        if (validacao.IsFailure)
            return validacao;

        Numero = numero;
        Segurado = segurado;
        Vigencia = vigencia;
        _coberturas = listaCoberturas;

        return Result.Success();
    }

    public bool EstaVigenteEm(DateOnly data) => Vigencia.EstaVigenteEm(data);

    public bool PossuiCobertura(TipoCobertura tipo) => _coberturas.Any(c => c.Tipo == tipo);

    private static Result Validar(string numero, List<Cobertura> coberturas)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return Result.Failure(ErrosDeApolice.NumeroObrigatorio);

        if (coberturas.Count == 0)
            return Result.Failure(ErrosDeApolice.SemCobertura);

        var repetida = coberturas.GroupBy(c => c.Tipo).FirstOrDefault(g => g.Count() > 1);
        if (repetida is not null)
            return Result.Failure(ErrosDeApolice.CoberturaRepetida(repetida.Key));

        return Result.Success();
    }
}
