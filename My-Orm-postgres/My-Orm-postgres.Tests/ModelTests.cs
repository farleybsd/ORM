using My_Orm_Data_Access.Core;
using My_Orm_Data_Access.Models;

namespace My_Orm_postgres.Tests;

// O Model: grava, carrega, só atualiza o que mudou e não atropela outro usuário
[Collection(PostgresCollection.Name)]
public sealed class ModelTests(PostgresFixture db)
{
    private async Task<Cliente> ClienteGravado()
    {
        await using var orm = db.NewSession();
        var cliente = new Cliente { Nome = "Maria", Email = Unico.Email() };
        await cliente.SaveAsync(orm);
        return cliente;
    }

    private async Task<Cliente> Carregar(long id)
    {
        await using var orm = db.NewSession();
        var cliente = new Cliente();
        Assert.True(await cliente.LoadAsync(orm, id));
        return cliente;
    }

    [Fact]
    public async Task Save_insere_e_preenche_id_e_datas()
    {
        var antes = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var cliente = await ClienteGravado();

        Assert.True(cliente.Id > 0);
        Assert.InRange(cliente.InsertedAt, antes, antes + 60_000);
        Assert.Equal(cliente.InsertedAt, cliente.UpdatedAt);

        var lido = await Carregar(cliente.Id);
        Assert.Equal(cliente.Email, lido.Email);
        Assert.Equal(cliente.UpdatedAt, lido.UpdatedAt);
    }

    [Fact]
    public async Task Load_de_id_inexistente_devolve_false()
    {
        await using var orm = db.NewSession();

        Assert.False(await new Cliente().LoadAsync(orm, -1));
    }

    [Fact]
    public async Task Save_sem_mudanca_nao_vai_ao_banco()
    {
        var cliente = await Carregar((await ClienteGravado()).Id);
        long versao = cliente.UpdatedAt;
        await using var orm = db.NewSession();

        Assert.False(await cliente.SaveAsync(orm));
        Assert.Equal(versao, (await Carregar(cliente.Id)).UpdatedAt);
    }

    [Fact]
    public async Task Save_atualiza_e_troca_a_versao()
    {
        var cliente = await Carregar((await ClienteGravado()).Id);
        long versao = cliente.UpdatedAt;
        await using var orm = db.NewSession();

        cliente.Nome = "Ana";
        Assert.True(await cliente.SaveAsync(orm));

        var lido = await Carregar(cliente.Id);
        Assert.Equal("Ana", lido.Nome);
        Assert.True(lido.UpdatedAt > versao);
        Assert.Equal(cliente.UpdatedAt, lido.UpdatedAt);
    }

    [Fact]
    public async Task Save_grava_so_as_colunas_que_mudaram()
    {
        long id = (await ClienteGravado()).Id;
        var cliente = await Carregar(id);

        // Alguém muda o e-mail "por fora", sem passar pelo Model (logo, sem trocar a versão)
        await using var orm = db.NewSession();
        string emailDeFora = Unico.Email();
        await orm.ExecuteAsync("UPDATE cliente SET email = @email WHERE id = @id",
            new() { ["email"] = emailDeFora, ["id"] = id });

        cliente.Nome = "Ana";
        await cliente.SaveAsync(orm);

        // O UPDATE só levou "nome": o e-mail de fora continua lá
        var lido = await Carregar(id);
        Assert.Equal("Ana", lido.Nome);
        Assert.Equal(emailDeFora, lido.Email);
    }

    [Fact]
    public async Task Dois_usuarios_editando_o_segundo_recebe_Concurrency()
    {
        long id = (await ClienteGravado()).Id;
        var usuarioA = await Carregar(id);
        var usuarioB = await Carregar(id);
        await using var orm = db.NewSession();

        usuarioA.Nome = "Alteração do A";
        await usuarioA.SaveAsync(orm);

        usuarioB.Nome = "Alteração do B";
        var erro = await Assert.ThrowsAsync<OrmException>(() => usuarioB.SaveAsync(orm));

        Assert.Equal(OrmErrorKind.Concurrency, erro.Kind);
        Assert.Equal("Alteração do A", (await Carregar(id)).Nome);
    }

    [Fact]
    public async Task Delete_remove_e_zera_o_id()
    {
        var cliente = await ClienteGravado();
        long id = cliente.Id;
        await using var orm = db.NewSession();

        await cliente.DeleteAsync(orm);

        Assert.Equal(0, cliente.Id);
        Assert.False(await new Cliente().LoadAsync(orm, id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => cliente.DeleteAsync(orm));
    }

    [Fact]
    public async Task CalcField_e_lido_mas_nunca_gravado()
    {
        long id = (await ClienteGravado()).Id;
        await using var orm = db.NewSession();

        var cliente = await orm.QueryFirstAsync<Cliente>(
            "SELECT c.*, 7 AS total_pedidos FROM cliente c WHERE id = @id", new() { ["id"] = id });

        Assert.Equal(7, cliente!.TotalPedidos);

        // Não existe coluna total_pedidos: se o Save tentasse gravar, daria ColumnNotFound
        cliente.TotalPedidos = 8;
        cliente.Nome = "Ana";
        await cliente.SaveAsync(orm);
    }

    // ===== Tipos de dado: o que vai é o que volta =====

    public enum Situacao { Rascunho = 1, Pago = 2 }

    public sealed class TipoTeste : Model
    {
        public decimal Valor { get; set; }
        public DateTimeOffset Quando { get; set; }
        public DateOnly Dia { get; set; }
        public Guid Codigo { get; set; }
        public Situacao Status { get; set; }
        public byte[]? Dados { get; set; }
        public string? Obs { get; set; }
    }

    [Fact]
    public async Task Tipos_de_dado_fazem_ida_e_volta_sem_perda()
    {
        var original = new TipoTeste
        {
            Valor = 1234567.89m,
            Quando = new DateTimeOffset(2026, 10, 29, 13, 45, 30, 123, TimeSpan.Zero),
            Dia = new DateOnly(2026, 2, 28),
            Codigo = Guid.NewGuid(),
            Status = Situacao.Pago,
            Dados = [0, 1, 2, 255],
            Obs = null
        };
        await using var orm = db.NewSession();
        await original.SaveAsync(orm);

        var lido = new TipoTeste();
        Assert.True(await lido.LoadAsync(orm, original.Id));

        Assert.Equal(original.Valor, lido.Valor);
        Assert.Equal(original.Quando, lido.Quando);
        Assert.Equal(original.Dia, lido.Dia);
        Assert.Equal(original.Codigo, lido.Codigo);
        Assert.Equal(original.Status, lido.Status);
        Assert.Equal(original.Dados, lido.Dados);
        Assert.Null(lido.Obs);
    }

    [Fact]
    public async Task Decimal_com_mais_casas_que_a_coluna_e_arredondado_pelo_banco()
    {
        await using var orm = db.NewSession();
        var tipo = new TipoTeste { Valor = 10.005m, Quando = DateTimeOffset.UnixEpoch, Codigo = Guid.NewGuid() };
        await tipo.SaveAsync(orm);

        var lido = new TipoTeste();
        await lido.LoadAsync(orm, tipo.Id);

        // NUMERIC(12,2): o Postgres arredonda, sem erro. Quem precisa de exatidão valida antes
        Assert.Equal(10.01m, lido.Valor);
    }

    // ===== Bugs conhecidos: tire o Skip quando corrigir =====

    [Fact(Skip = "Bug conhecido: o dirty-check compara byte[] por referência, alteração in-place não é gravada")]
    public async Task Byte_array_alterado_in_place_e_gravado()
    {
        await using var orm = db.NewSession();
        var tipo = new TipoTeste { Valor = 1, Quando = DateTimeOffset.UnixEpoch, Codigo = Guid.NewGuid(), Dados = [1, 2, 3] };
        await tipo.SaveAsync(orm);

        tipo.Dados![0] = 9;
        await tipo.SaveAsync(orm);

        var lido = new TipoTeste();
        await lido.LoadAsync(orm, tipo.Id);
        Assert.Equal(new byte[] { 9, 2, 3 }, lido.Dados);
    }

    [Fact(Skip = "Bug conhecido: o Delete não confere a versão e apaga mesmo depois de outro usuário alterar")]
    public async Task Delete_de_versao_antiga_recebe_Concurrency()
    {
        long id = (await ClienteGravado()).Id;
        var usuarioA = await Carregar(id);
        var usuarioB = await Carregar(id);
        await using var orm = db.NewSession();

        usuarioA.Nome = "Alteração do A";
        await usuarioA.SaveAsync(orm);

        var erro = await Assert.ThrowsAsync<OrmException>(() => usuarioB.DeleteAsync(orm));
        Assert.Equal(OrmErrorKind.Concurrency, erro.Kind);
    }
}
