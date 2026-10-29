using My_Orm_Data_Access.Core;
using My_Orm_Data_Access.Models;
using My_Orm_postgres.Api.Common;
using My_Orm_postgres.Api.Contracts;
using My_Orm_postgres.Api.Domain;

namespace My_Orm_postgres.Api.Features.AtualizarCliente;

public sealed class Handler(IOrm db, ILogger<Handler> logger)
{
    public async Task<Result<ClienteResponse>> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        // Load e Save na mesma transação: ou tudo é gravado, ou nada
        await db.BeginTransactionAsync(cancellationToken);

        var cliente = new Cliente();
        if (!await cliente.LoadAsync(db, request.Id, cancellationToken))
        {
            await db.RollbackAsync();
            logger.LogWarning("Atualização recusada: cliente {ClienteId} não encontrado", request.Id);
            return ErrosDeCliente.ClienteNaoEncontrado(request.Id);
        }

        // Quem enviou leu uma versão antiga: outro usuário alterou o cliente nesse meio-tempo
        if (cliente.UpdatedAt != request.Versao)
        {
            await db.RollbackAsync();
            logger.LogWarning("Atualização recusada: cliente {ClienteId} está na versão {VersaoAtual}, pedido veio com {VersaoEnviada}",
                request.Id, cliente.UpdatedAt, request.Versao);
            return ErrosDeCliente.VersaoDesatualizada(request.Id);
        }

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
            await db.RollbackAsync();
            logger.LogWarning("Atualização recusada: email {Email} já pertence a outro cliente", request.Email);
            return ErrosDeCliente.EmailJaCadastrado(request.Email);
        }
        catch (OrmException ex) when (ex.Kind == OrmErrorKind.Concurrency)
        {
            // Alguém alterou entre o Load e o Save acima
            await db.RollbackAsync();
            logger.LogWarning("Conflito de concorrência ao atualizar o cliente {ClienteId}", request.Id);
            return ErrosDeCliente.VersaoDesatualizada(request.Id);
        }

        await db.CommitAsync(cancellationToken);
        logger.LogInformation("Cliente {ClienteId} atualizado para a versão {Versao}", cliente.Id, cliente.UpdatedAt);
        return Result.Success(CriarCliente.Handler.Mapear(cliente));
    }
}
