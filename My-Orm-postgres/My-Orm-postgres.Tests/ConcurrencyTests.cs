namespace My_Orm_postgres.Tests;

// Uma sessão é de uma requisição só, e de uma operação por vez
[Collection(PostgresCollection.Name)]
public sealed class ConcurrencyTests(PostgresFixture db)
{
    [Fact]
    public async Task Sessoes_diferentes_em_paralelo_funcionam()
    {
        var consultas = Enumerable.Range(0, 20).Select(async _ =>
        {
            await using var orm = db.NewSession();
            return await orm.QueryFirstAsync<int>("SELECT 1 FROM pg_sleep(0.05)");
        });

        Assert.All(await Task.WhenAll(consultas), r => Assert.Equal(1, r));
    }

    [Fact(Skip = "Bug conhecido: a sessão guarda a conexão num campo; duas operações ao mesmo tempo trocam a conexão uma da outra e travam")]
    public async Task Mesma_sessao_em_paralelo_da_erro_claro()
    {
        var orm = db.NewSession();

        // Hoje isso trava para sempre: o limite de tempo transforma o travamento em falha.
        // Sem "await using": o Dispose de uma sessão travada também ficaria preso
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.WhenAll(
            orm.QueryAsync("SELECT pg_sleep(0.2)"),
            orm.QueryAsync("SELECT pg_sleep(0.2)")).WaitAsync(TimeSpan.FromSeconds(10)));

        await orm.DisposeAsync();
    }
}
