import { Injectable } from '@angular/core';
import { ApiError, ProblemDetails } from '@shared/http/interfaces/problem-details';

type HttpMethod = 'POST' | 'PUT' | 'DELETE';

/**
 * Cliente HTTP baseado em Promise (fetch) para as operações de escrita.
 * As leituras usam `httpResource`, que já expõe o estado como signals.
 */
@Injectable({
  providedIn: 'root',
})
export class ApiClient {
  post<T>(url: string, body: unknown) {
    return this.request<T>('POST', url, body);
  }

  put<T>(url: string, body: unknown) {
    return this.request<T>('PUT', url, body);
  }

  delete(url: string) {
    return this.request<void>('DELETE', url);
  }

  private async request<T>(method: HttpMethod, url: string, body?: unknown): Promise<T> {
    let response: Response;

    try {
      response = await fetch(url, {
        method,
        headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
        body: body === undefined ? undefined : JSON.stringify(body),
      });
    } catch {
      // Falha de rede: `toProblemDetails` traduz o status 0 em "Falha de conexão".
      throw new ApiError(0, {});
    }

    if (!response.ok) {
      throw new ApiError(response.status, await this.readProblem(response));
    }

    if (response.status === 204) {
      return undefined as T;
    }

    return (await response.json()) as T;
  }

  private async readProblem(response: Response): Promise<ProblemDetails> {
    try {
      return (await response.json()) as ProblemDetails;
    } catch {
      return { status: response.status, title: response.statusText || 'Erro inesperado' };
    }
  }
}
