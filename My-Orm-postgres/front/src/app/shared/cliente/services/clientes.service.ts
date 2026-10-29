import { inject, Injectable, Signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { ApiClient } from '@core/http/api-client.service';
import { Cliente, ClientePayload, ClienteUpdatePayload } from '../interfaces/cliente';

const BASE_URL = '/api/clientes';

@Injectable({
  providedIn: 'root',
})
export class ClientesService {
  private apiClient = inject(ApiClient);

  getAllWithHttpResource() {
    return httpResource<Cliente[]>(() => BASE_URL, { defaultValue: [] });
  }

  /** Não faz requisição enquanto `id` estiver vazio (modo cadastro). */
  getByIdWithHttpResource(id: Signal<string | undefined>) {
    return httpResource<Cliente>(() => (id() ? `${BASE_URL}/${id()}` : undefined));
  }

  post(payload: ClientePayload) {
    return this.apiClient.post<Cliente>(BASE_URL, payload);
  }

  put(id: number, payload: ClienteUpdatePayload) {
    return this.apiClient.put<Cliente>(`${BASE_URL}/${id}`, payload);
  }

  delete(id: number) {
    return this.apiClient.delete(`${BASE_URL}/${id}`);
  }
}
