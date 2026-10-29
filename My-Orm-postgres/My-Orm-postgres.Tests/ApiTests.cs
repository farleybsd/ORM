using Microsoft.AspNetCore.Mvc.Testing;
using My_Orm_postgres.Api.Contracts;
using System.Net;
using System.Net.Http.Json;

namespace My_Orm_postgres.Tests;

// A API de ponta a ponta (HTTP -> handler -> ORM -> Postgres do container)
[Collection(PostgresCollection.Name)]
public sealed class ApiTests : IDisposable
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient http;

    public ApiTests(PostgresFixture db)
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.UseSetting("ConnectionStrings:DefaultConnection", db.ConnectionString));
        http = factory.CreateClient();
    }

    public void Dispose()
    {
        http.Dispose();
        factory.Dispose();
    }

    private async Task<ClienteResponse> Criar(string email)
    {
        var resposta = await http.PostAsJsonAsync("/clientes", new { nome = "Maria", email, ativo = true });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ClienteResponse>())!;
    }

    private Task<HttpResponseMessage> Atualizar(ClienteResponse c, string nome) =>
        http.PutAsJsonAsync($"/clientes/{c.Id}", new { nome, email = c.Email, ativo = c.Ativo, versao = c.Versao });

    [Fact]
    public async Task Criar_consultar_e_listar()
    {
        var criado = await Criar(Unico.Email());

        var consultado = await http.GetFromJsonAsync<ClienteResponse>($"/clientes/{criado.Id}");
        var lista = await http.GetFromJsonAsync<List<ClienteResponse>>("/clientes");

        Assert.Equal(criado, consultado);
        Assert.Contains(lista!, c => c.Id == criado.Id);
    }

    [Fact]
    public async Task Email_repetido_devolve_409()
    {
        string email = Unico.Email();
        await Criar(email);

        var resposta = await http.PostAsJsonAsync("/clientes", new { nome = "Outra", email, ativo = true });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Contains("cliente-email-ja-cadastrado", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Dados_invalidos_devolvem_400_sem_ir_ao_banco()
    {
        var resposta = await http.PostAsJsonAsync("/clientes", new { nome = "", email = "não é e-mail", ativo = true });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_com_versao_atual_e_depois_com_a_antiga()
    {
        var criado = await Criar(Unico.Email());

        var primeira = await Atualizar(criado, "Ana");
        var segunda = await Atualizar(criado, "Bia");   // mesma versão: já ficou velha

        Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
        Assert.Contains("cliente-versao-desatualizada", await segunda.Content.ReadAsStringAsync());

        var atual = await http.GetFromJsonAsync<ClienteResponse>($"/clientes/{criado.Id}");
        Assert.Equal("Ana", atual!.Nome);
    }

    [Fact]
    public async Task Atualizacoes_simultaneas_so_uma_vence()
    {
        var criado = await Criar(Unico.Email());

        // 10 requisições ao mesmo tempo, todas com a mesma versão
        var respostas = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(i => Atualizar(criado, $"Usuário {i}")));

        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.All(respostas, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
    }

    [Fact]
    public async Task Atualizar_e_excluir_inexistente_devolvem_404()
    {
        // Versão válida (> 0) para passar da validação e chegar ao banco
        var fantasma = new ClienteResponse(long.MaxValue, "x", Unico.Email(), true, default, default, 1);

        Assert.Equal(HttpStatusCode.NotFound, (await Atualizar(fantasma, "y")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.DeleteAsync($"/clientes/{long.MaxValue}")).StatusCode);
    }

    [Fact]
    public async Task Excluir_e_depois_consultar()
    {
        var criado = await Criar(Unico.Email());

        Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync($"/clientes/{criado.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync($"/clientes/{criado.Id}")).StatusCode);
    }
}
