using Apolices.Api.Domain;

namespace Apolices.Domain.Tests;

public class PeriodoVigenciaTests
{
    [Fact]
    public void EstaVigenteEm_DeveRetornarTrue_QuandoDataDentroDoPeriodo()
    {
        var periodo = new PeriodoVigencia(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        Assert.True(periodo.EstaVigenteEm(new DateOnly(2026, 6, 15)));
    }

    [Theory]
    [InlineData(2026, 1, 1)]
    [InlineData(2026, 12, 31)]
    public void EstaVigenteEm_DeveRetornarTrue_NasBordasDoPeriodo(int ano, int mes, int dia)
    {
        var periodo = new PeriodoVigencia(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        Assert.True(periodo.EstaVigenteEm(new DateOnly(ano, mes, dia)));
    }

    [Fact]
    public void EstaVigenteEm_DeveRetornarFalse_QuandoDataAnteriorAoInicio()
    {
        var periodo = new PeriodoVigencia(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        Assert.False(periodo.EstaVigenteEm(new DateOnly(2025, 12, 31)));
    }

    [Fact]
    public void EstaVigenteEm_DeveRetornarFalse_QuandoDataPosteriorAoFim()
    {
        var periodo = new PeriodoVigencia(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        Assert.False(periodo.EstaVigenteEm(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void Construtor_DeveLancarExcecao_QuandoFimAnteriorAoInicio()
    {
        Assert.Throws<ArgumentException>(() =>
            new PeriodoVigencia(new DateOnly(2026, 12, 31), new DateOnly(2026, 1, 1)));
    }
}
