using Apolices.Api.Domain;

namespace Apolices.Api.Contracts;

/// <summary>Dados de uma apólice enviados na criação e na atualização.</summary>
public abstract class DadosApoliceRequest
{
    public string Numero { get; set; } = string.Empty;
    public SeguradoRequest Segurado { get; set; } = new();
    public VigenciaRequest Vigencia { get; set; } = new();
    public List<CoberturaRequest> Coberturas { get; set; } = [];

    public Segurado ParaSegurado() => new(Segurado.Nome.Trim(), Segurado.Documento.Trim());

    public PeriodoVigencia ParaVigencia() => new(Vigencia.Inicio, Vigencia.Fim);

    public IEnumerable<Cobertura> ParaCoberturas() => Coberturas.Select(c => new Cobertura(c.Tipo, c.LimiteIndenizacao));
}

public sealed class SeguradoRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
}

public sealed class VigenciaRequest
{
    public DateOnly Inicio { get; set; }
    public DateOnly Fim { get; set; }
}

public sealed class CoberturaRequest
{
    public TipoCobertura Tipo { get; set; }
    public decimal LimiteIndenizacao { get; set; }
}
