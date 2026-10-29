using My_Orm_Data_Access.Core;
using My_Orm_Data_Access.Models;
using My_Orm_postgres.Api.Common;
using My_Orm_postgres.Api.Contracts;
using My_Orm_postgres.Api.Domain;

namespace My_Orm_postgres.Api.Features.AtualizarCliente;

public sealed class Handler(IOrm db)
{
    public async Task<Result<ClienteResponse>> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var cliente = new Cliente();
        if (!await cliente.LoadAsync(db, request.Id, cancellationToken))
            return ErrosDeCliente.ClienteNaoEncontrado(request.Id);

        // Quem enviou leu uma versão antiga: outro usuário alterou o cliente nesse meio-tempo
        if (cliente.UpdatedAt != request.Versao)
            return ErrosDeCliente.VersaoDesatualizada(request.Id);

        cliente.Nome = request.Nome;
        cliente.Email = request.Email;
        cliente.Ativo = request.Ativo;

        try
        {
            // Só grava as colunas que mudaram; se nada mudou, não vai ao banco
            await cliente.SaveAsync(db, cancellationToken);
        }
        catch (OrmException ex) when (ex.Kind == OrmErrorKind.Duplicate)
        {
            return ErrosDeCliente.EmailJaCadastrado(request.Email);
        }
        catch (OrmException ex) when (ex.Kind == OrmErrorKind.Concurrency)
        {
            // Alguém alterou entre o Load e o Save acima
            return ErrosDeCliente.VersaoDesatualizada(request.Id);
        }

        return Result.Success(CriarCliente.Handler.Mapear(cliente));
    }
}
