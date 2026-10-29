using My_Orm_Data_Access.Core;
using My_Orm_Data_Access.Driver;

namespace My_Orm_postgres.Tests;

// O driver direto: SQL, parâmetros, atalhos de CRUD e tradução de erros
[Collection(PostgresCollection.Name)]
public sealed class DriverTests(PostgresFixture db)
{
    private static Dictionary<string, object?> NovoCliente(string? email = null) => new()
    {
        ["nome"] = "Maria",
        ["email"] = email ?? Unico.Email(),
        ["ativo"] = true,
        ["inserted_at"] = 1L,
        ["updated_at"] = 1L
    };

    [Fact]
    public async Task Insert_devolve_o_id_gerado()
    {
        await using var orm = db.NewSession();

        long id = await orm.InsertAsync("cliente", NovoCliente());

        Assert.True(id > 0);
        Assert.Equal(1L, await orm.QueryFirstAsync<long>(
            "SELECT count(*) FROM cliente WHERE id = @id", new() { ["id"] = id }));
    }

    [Fact]
    public async Task Valor_com_SQL_malicioso_e_gravado_como_texto()
    {
        await using var orm = db.NewSession();
        const string nome = "Robert'); DROP TABLE cliente; --";
        var dados = NovoCliente();
        dados["nome"] = nome;

        long id = await orm.InsertAsync("cliente", dados);

        Assert.Equal(nome, await orm.QueryFirstAsync<string>(
            "SELECT nome FROM cliente WHERE id = @id", new() { ["id"] = id }));
    }

    [Theory]
    [InlineData("cliente; DROP TABLE cliente")]
    [InlineData("cliente--")]
    [InlineData("1cliente")]
    [InlineData("")]
    public async Task Nome_de_tabela_invalido_e_recusado(string tabela)
    {
        await using var orm = db.NewSession();

        await Assert.ThrowsAsync<ArgumentException>(() => orm.InsertAsync(tabela, NovoCliente()));
    }

    [Fact]
    public async Task Nome_de_coluna_invalido_e_recusado()
    {
        await using var orm = db.NewSession();
        var dados = NovoCliente();
        dados["nome = 'x' --"] = "y";

        await Assert.ThrowsAsync<ArgumentException>(() => orm.InsertAsync("cliente", dados));
    }

    [Fact]
    public async Task Update_e_Delete_sem_WHERE_sao_bloqueados()
    {
        await using var orm = db.NewSession();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => orm.UpdateAsync("cliente", new() { ["ativo"] = false }, ""));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => orm.DeleteAsync("cliente", " "));
    }

    [Fact]
    public async Task Update_e_Delete_devolvem_linhas_afetadas()
    {
        await using var orm = db.NewSession();
        long id = await orm.InsertAsync("cliente", NovoCliente());
        var porId = new Dictionary<string, object?> { ["id"] = id };

        Assert.Equal(1, await orm.UpdateAsync("cliente", new() { ["nome"] = "Ana" }, "id = @id", porId));
        Assert.Equal(1, await orm.DeleteAsync("cliente", "id = @id", porId));
        Assert.Equal(0, await orm.DeleteAsync("cliente", "id = @id", porId));
    }

    [Fact]
    public async Task Query_mapeia_record_e_valor_simples()
    {
        await using var orm = db.NewSession();
        string email = Unico.Email();
        await orm.InsertAsync("cliente", NovoCliente(email));

        var linhas = await orm.QueryAsync<ClienteResumo>(
            "SELECT id, nome, email FROM cliente WHERE email = @email", new() { ["email"] = email });

        var resumo = Assert.Single(linhas);
        Assert.Equal("Maria", resumo.Nome);
        Assert.Equal(email, resumo.Email);

        Assert.Null(await orm.QueryFirstAsync<ClienteResumo>(
            "SELECT id, nome, email FROM cliente WHERE id = -1"));
    }

    private sealed record ClienteResumo(long Id, string Nome, string Email);

    [Fact]
    public async Task Versoes_sincronas_funcionam()
    {
        using var orm = db.NewSession();

        long id = orm.Insert("cliente", NovoCliente());
        var cliente = orm.Select("cliente", "nome", "id = @id", new() { ["id"] = id });

        Assert.Equal("Maria", cliente.Rows[0]["nome"]);
        Assert.Equal(1, orm.Delete("cliente", "id = @id", new() { ["id"] = id }));
    }

    // ===== Tradução de erros =====

    [Fact]
    public async Task Email_repetido_vira_Duplicate()
    {
        await using var orm = db.NewSession();
        string email = Unico.Email();
        await orm.InsertAsync("cliente", NovoCliente(email));

        var erro = await Assert.ThrowsAsync<OrmException>(() => orm.InsertAsync("cliente", NovoCliente(email)));

        Assert.Equal(OrmErrorKind.Duplicate, erro.Kind);
        Assert.Equal("cliente_email_key", erro.Detail);
    }

    [Fact]
    public async Task Campo_obrigatorio_nulo_vira_NotNull()
    {
        await using var orm = db.NewSession();
        var dados = NovoCliente();
        dados["nome"] = null;

        var erro = await Assert.ThrowsAsync<OrmException>(() => orm.InsertAsync("cliente", dados));

        Assert.Equal(OrmErrorKind.NotNull, erro.Kind);
        Assert.Equal("nome", erro.Detail);
    }

    [Fact]
    public async Task Tabela_e_coluna_inexistentes_sao_traduzidas()
    {
        await using var orm = db.NewSession();

        var semTabela = await Assert.ThrowsAsync<OrmException>(() => orm.QueryAsync("SELECT * FROM nao_existe"));
        var semColuna = await Assert.ThrowsAsync<OrmException>(() => orm.QueryAsync("SELECT nao_existe FROM cliente"));

        Assert.Equal(OrmErrorKind.TableNotFound, semTabela.Kind);
        Assert.Equal(OrmErrorKind.ColumnNotFound, semColuna.Kind);
    }

    [Fact]
    public async Task Banco_fora_do_ar_vira_Connection()
    {
        // Porta 1: ninguém escutando, a conexão é recusada na hora
        await using var orm = new Postgres("Host=127.0.0.1;Port=1;Username=x;Password=x;Timeout=3");

        var erro = await Assert.ThrowsAsync<OrmException>(() => orm.QueryAsync("SELECT 1"));

        Assert.Equal(OrmErrorKind.Connection, erro.Kind);
    }
}
