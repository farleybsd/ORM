namespace My_Orm_postgres.Api.Contracts;

/// <param name="Versao">Versão do registro (concorrência otimista): deve ser reenviada no PUT.</param>
public sealed record ClienteResponse(
    long Id,
    string Nome,
    string Email,
    bool Ativo,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm,
    long Versao);
