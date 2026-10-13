namespace DDDWithHttp.Orm;

internal sealed class OrmTransaction(DbSession session) : IOrmTransaction
{
    private bool _completed;

    public async Task CommitAsync(CancellationToken ct = default)
    {
        EnsureActive();
        _completed = true;   // mesmo que o COMMIT falhe, a transação acabou (o banco desfaz)
        await session.EndTransactionAsync(commit: true, ct);
    }

    public async Task RollbackAsync()
    {
        EnsureActive();
        _completed = true;
        await session.EndTransactionAsync(commit: false, CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        if (_completed)
            return;

        _completed = true;
        await session.EndTransactionAsync(commit: false, CancellationToken.None);
    }

    private void EnsureActive()
    {
        if (_completed)
            throw new InvalidOperationException("A transação já foi confirmada ou desfeita.");
    }
}
