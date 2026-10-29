import { inject, Injectable } from '@angular/core';
import { MessageService } from 'primeng/api';
import { toProblemDetails } from '@shared/http/functions/to-problem-details';

@Injectable({
  providedIn: 'root',
})
export class FeedbackService {
  private messageService = inject(MessageService);

  success(message: string) {
    this.messageService.add({ severity: 'success', summary: 'Sucesso', detail: message, life: 3000 });
  }

  error(message: string, summary = 'Erro') {
    this.messageService.add({ severity: 'error', summary, detail: message, life: 6000 });
  }

  /** Exibe o `title`/`detail` do ProblemDetails retornado pela API. */
  apiError(error: unknown) {
    const problem = toProblemDetails(error);
    this.error(problem.detail ?? 'Tente novamente.', problem.title ?? 'Erro');
  }
}
