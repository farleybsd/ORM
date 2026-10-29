/** Corpo de erro da API (RFC 9457 — application/problem+json). */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  traceId?: string;
  /** Erros de validação por campo (somente quando `code === 'validacao'`). */
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly problem: ProblemDetails,
  ) {
    super(problem.detail ?? problem.title ?? `Erro HTTP ${status}`);
  }
}
