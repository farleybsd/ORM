namespace Sinistros.Api.IntegrationTests;

/// <summary>
/// Substitui o handler primário do HttpClient tipado usado para consultar a API de Apólices,
/// permitindo simular respostas (sucesso, erro, atraso) sem qualquer chamada de rede real. O
/// pipeline de resiliência configurado em Program.cs continua ativo por cima deste handler.
/// </summary>
public sealed class ScriptedHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
{
    private int _numeroDeChamadas;

    public int NumeroDeChamadas => _numeroDeChamadas;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _numeroDeChamadas);
        return await responder(request, cancellationToken);
    }
}
