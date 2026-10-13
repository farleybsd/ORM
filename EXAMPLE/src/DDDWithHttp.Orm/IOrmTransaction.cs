namespace DDDWithHttp.Orm;

/// <summary>
/// Transação explícita. O padrão de uso é:
/// <code>
/// await using var tx = await session.BeginTransactionAsync(ct: ct);
/// ... comandos ...
/// await tx.CommitAsync(ct);
/// </code>
/// Sair do bloco sem Commit (exceção, return antecipado) faz Rollback automaticamente.
/// </summary>
public interface IOrmTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync();
}
