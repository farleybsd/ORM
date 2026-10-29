using My_Orm_Data_Access.Core;
using My_Orm_Data_Access.Models;

namespace My_Orm_postgres.Tests;

// Atomicidade: o que teve Commit fica, o resto é desfeito (inclusive no Dispose)
[Collection(PostgresCollection.Name)]
public sealed class TransactionTests(PostgresFixture db)
{
    private static Cliente NovoCliente(string email) => new() { Nome = "Maria", Email = email };

    private async Task<bool> Existe(string email)
    {
        // Sempre por outra sessão: só enxerga o que foi confirmado no banco
        await using var outra = db.NewSession();
        return await outra.QueryFirstAsync<long>(
            "SELECT count(*) FROM cliente WHERE email = @email", new() { ["email"] = email }) == 1;
    }

    [Fact]
    public async Task Commit_grava()
    {
        string email = Unico.Email();
        await using var orm = db.NewSession();

        await orm.BeginTransactionAsync();
        await NovoCliente(email).SaveAsync(orm);
        await orm.CommitAsync();

        Assert.True(await Existe(email));
    }

    [Fact]
    public async Task Rollback_desfaz()
    {
        string email = Unico.Email();
        await using var orm = db.NewSession();

        await orm.BeginTransactionAsync();
        await NovoCliente(email).SaveAsync(orm);
        await orm.RollbackAsync();

        Assert.False(await Existe(email));
    }

    [Fact]
    public async Task Dispose_sem_Commit_desfaz()
    {
        string email = Unico.Email();

        await using (var orm = db.NewSession())
        {
            await orm.BeginTransactionAsync();
            await NovoCliente(email).SaveAsync(orm);
        }

        Assert.False(await Existe(email));
    }

    [Fact]
    public async Task Antes_do_Commit_outra_sessao_nao_ve_o_registro()
    {
        string email = Unico.Email();
        await using var orm = db.NewSession();

        await orm.BeginTransactionAsync();
        await NovoCliente(email).SaveAsync(orm);

        Assert.False(await Existe(email));

        await orm.CommitAsync();
        Assert.True(await Existe(email));
    }

    [Fact]
    public async Task Varios_comandos_sao_tudo_ou_nada()
    {
        string primeiro = Unico.Email(), repetido = Unico.Email();
        await using (var antes = db.NewSession())
            await NovoCliente(repetido).SaveAsync(antes);

        await using var orm = db.NewSession();
        await orm.BeginTransactionAsync();
        await NovoCliente(primeiro).SaveAsync(orm);

        // O segundo falha: o primeiro não pode ficar gravado sozinho
        await Assert.ThrowsAsync<OrmException>(() => NovoCliente(repetido).SaveAsync(orm));
        await orm.RollbackAsync();

        Assert.False(await Existe(primeiro));
    }

    [Fact]
    public async Task Sessao_volta_a_funcionar_depois_de_erro_e_Rollback()
    {
        string repetido = Unico.Email(), novo = Unico.Email();
        await using var orm = db.NewSession();
        await NovoCliente(repetido).SaveAsync(orm);

        await orm.BeginTransactionAsync();
        await Assert.ThrowsAsync<OrmException>(() => NovoCliente(repetido).SaveAsync(orm));
        await orm.RollbackAsync();

        await orm.BeginTransactionAsync();
        await NovoCliente(novo).SaveAsync(orm);
        await orm.CommitAsync();

        Assert.True(await Existe(novo));
    }

    [Fact]
    public async Task Transacao_dupla_e_Commit_sem_transacao_sao_bloqueados()
    {
        await using var orm = db.NewSession();

        await Assert.ThrowsAsync<InvalidOperationException>(() => orm.CommitAsync());

        await orm.BeginTransactionAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => orm.BeginTransactionAsync());
    }

    [Fact]
    public async Task Rollback_sem_transacao_nao_faz_nada()
    {
        await using var orm = db.NewSession();

        await orm.RollbackAsync();
        orm.Rollback();
    }
}
