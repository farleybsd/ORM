namespace Sinistros.Api.Domain;

/// <summary>
/// Resultado da consulta à API de Apólices, já traduzido para o vocabulário de Sinistros.
/// Distingue "apólice não existe" (resposta de negócio) de "serviço indisponível" (falha de
/// infraestrutura), para que o caso de uso trate cada caso com o código HTTP apropriado.
/// </summary>
public abstract record ApoliceConsultaResultado
{
    public sealed record Encontrada(ApoliceReferenciada Apolice) : ApoliceConsultaResultado;

    public sealed record NaoEncontrada : ApoliceConsultaResultado;

    public sealed record Indisponivel(string Motivo) : ApoliceConsultaResultado;
}
