/** Espelha o `ClienteResponse` da API. */
export interface Cliente {
  id: number;
  nome: string;
  email: string;
  ativo: boolean;
  /** ISO 8601 (DateTimeOffset). */
  criadoEm: string;
  /** ISO 8601 (DateTimeOffset). */
  atualizadoEm: string;
  /** Versão do registro (concorrência otimista): deve ser reenviada no PUT. */
  versao: number;
}

/** Corpo do POST /clientes. */
export type ClientePayload = Pick<Cliente, 'nome' | 'email' | 'ativo'>;

/** Corpo do PUT /clientes/{id}. */
export type ClienteUpdatePayload = ClientePayload & Pick<Cliente, 'versao'>;
