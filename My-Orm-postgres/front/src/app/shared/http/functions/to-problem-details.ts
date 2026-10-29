import { HttpErrorResponse } from '@angular/common/http';
import { ApiError, ProblemDetails } from '../interfaces/problem-details';

const CONNECTION_FAILURE: ProblemDetails = {
  title: 'Falha de conexão',
  detail: 'Não foi possível se comunicar com a API. Verifique se ela está em execução.',
};

/** Normaliza qualquer erro (fetch, httpResource ou inesperado) para um ProblemDetails. */
export function toProblemDetails(error: unknown): ProblemDetails {
  if (error instanceof ApiError) {
    return withConnectionFallback({ status: error.status, ...error.problem });
  }

  if (error instanceof HttpErrorResponse) {
    const body = typeof error.error === 'object' && error.error !== null ? error.error : {};
    return withConnectionFallback({ status: error.status, title: error.statusText, ...(body as ProblemDetails) });
  }

  return { title: 'Erro inesperado', detail: 'Ocorreu um erro inesperado. Tente novamente.' };
}

/**
 * Sem conexão (status 0) ou API fora do ar atrás do proxy (502/503/504 sem corpo
 * ProblemDetails): troca a mensagem genérica por uma que explica o problema.
 */
function withConnectionFallback(problem: ProblemDetails): ProblemDetails {
  const status = problem.status ?? 0;
  const isGatewayError = status >= 502 && status <= 504 && !problem.type;

  return status === 0 || isGatewayError ? { ...CONNECTION_FAILURE, status } : problem;
}
