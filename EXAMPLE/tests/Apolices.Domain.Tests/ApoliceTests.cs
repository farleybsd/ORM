using Apolices.Api.Domain;

namespace Apolices.Domain.Tests;

public class ApoliceTests
{
    private static readonly Segurado Segurado = new("Ana Souza", "111.111.111-11");
    private static readonly PeriodoVigencia Vigencia = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

    [Fact]
    public void Criar_DeveRetornarSucesso_QuandoDadosValidos()
    {
        var resultado = Apolice.Criar(
            Guid.NewGuid(),
            "AP-0001",
            Segurado,
            Vigencia,
            [new Cobertura(TipoCobertura.Colisao, 50_000m)]);

        Assert.True(resultado.IsSuccess);
    }

    [Fact]
    public void Criar_DeveFalhar_QuandoNumeroVazio()
    {
        var resultado = Apolice.Criar(
            Guid.NewGuid(),
            "",
            Segurado,
            Vigencia,
            [new Cobertura(TipoCobertura.Colisao, 50_000m)]);

        Assert.True(resultado.IsFailure);
    }

    [Fact]
    public void Criar_DeveFalhar_QuandoSemCoberturas()
    {
        var resultado = Apolice.Criar(Guid.NewGuid(), "AP-0001", Segurado, Vigencia, []);

        Assert.True(resultado.IsFailure);
    }

    [Fact]
    public void PossuiCobertura_DeveRetornarTrue_QuandoCoberturaContratada()
    {
        var apolice = Apolice.Criar(
            Guid.NewGuid(),
            "AP-0001",
            Segurado,
            Vigencia,
            [new Cobertura(TipoCobertura.Colisao, 50_000m), new Cobertura(TipoCobertura.Roubo, 30_000m)]).Value;

        Assert.True(apolice.PossuiCobertura(TipoCobertura.Roubo));
    }

    [Fact]
    public void PossuiCobertura_DeveRetornarFalse_QuandoCoberturaNaoContratada()
    {
        var apolice = Apolice.Criar(
            Guid.NewGuid(),
            "AP-0001",
            Segurado,
            Vigencia,
            [new Cobertura(TipoCobertura.Colisao, 50_000m)]).Value;

        Assert.False(apolice.PossuiCobertura(TipoCobertura.Incendio));
    }

    [Fact]
    public void EstaVigenteEm_DeveDelegarParaPeriodoDeVigencia()
    {
        var apolice = Apolice.Criar(
            Guid.NewGuid(),
            "AP-0001",
            Segurado,
            Vigencia,
            [new Cobertura(TipoCobertura.Colisao, 50_000m)]).Value;

        Assert.True(apolice.EstaVigenteEm(new DateOnly(2026, 6, 1)));
        Assert.False(apolice.EstaVigenteEm(new DateOnly(2024, 6, 1)));
    }
}
