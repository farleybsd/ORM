namespace Sinistros.Api.Common;

/// <summary>
/// Erro esperado (faz parte do negócio e o cliente consegue agir sobre ele). Viaja como valor
/// dentro de um <see cref="Result"/>, nunca como exceção.
/// </summary>
/// <param name="Codigo">Identificador estável e publicado do erro. Nunca muda: mudou o significado, crie um código novo.</param>
/// <param name="Titulo">Resumo fixo por código. Dados da ocorrência não entram aqui.</param>
/// <param name="Detalhe">Explicação desta ocorrência, para humanos. Pode mudar; clientes não devem fazer parse dele.</param>
/// <param name="Tipo">Classe do erro, usada para escolher o status HTTP.</param>
/// <param name="Metadados">Dados estruturados da ocorrência, expostos como extensões do ProblemDetails.</param>
public sealed record Erro(
    string Codigo,
    string Titulo,
    string Detalhe,
    TipoErro Tipo,
    IReadOnlyDictionary<string, object?>? Metadados = null);
