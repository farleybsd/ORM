namespace DDDWithHttp.Orm.Sessions;

/// <summary>
/// Unit of Work da requisição (scoped). Devolve sempre a MESMA sessão para o mesmo banco dentro
/// da requisição, então todos os repositórios que usam aquele banco compartilham a transação
/// aberta pelo caso de uso. Bancos diferentes têm sessões (e transações) diferentes.
/// <para>
/// Importante: não existe transação atômica entre Postgres e SQL Server. Se um caso de uso
/// gravar nos dois, cada banco confirma separadamente — para consistência entre eles use
/// outbox/eventos, não "duas transações aninhadas".
/// </para>
/// No fim da requisição as sessões são descartadas; transação esquecida aberta sofre Rollback.
/// </summary>
public interface IDbSessionProvider
{
    IDbSession Get(DatabaseProvider provider);
}
