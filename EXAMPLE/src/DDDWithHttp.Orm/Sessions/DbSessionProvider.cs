namespace DDDWithHttp.Orm.Sessions;

internal sealed class DbSessionProvider(IDbSessionFactory factory) : IDbSessionProvider, IAsyncDisposable, IDisposable
{
    private readonly Dictionary<DatabaseProvider, IDbSession> _sessions = new();

    public IDbSession Get(DatabaseProvider provider)
    {
        if (!_sessions.TryGetValue(provider, out IDbSession? session))
        {
            session = factory.Create(provider);
            _sessions[provider] = session;
        }

        return session;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (IDbSession session in _sessions.Values)
            await session.DisposeAsync();

        _sessions.Clear();
    }

    // Alguns containers/frameworks descartam escopos de forma síncrona (ex.: o FastEndpoints na
    // subida). Sem transação aberta o DisposeAsync da sessão já termina de forma síncrona.
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
}
