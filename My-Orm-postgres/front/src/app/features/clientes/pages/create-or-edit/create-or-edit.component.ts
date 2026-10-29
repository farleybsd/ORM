import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import {
  email,
  form,
  FormField,
  maxLength,
  required,
  submit,
  TreeValidationResult,
  ValidationError,
} from '@angular/forms/signals';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { SkeletonModule } from 'primeng/skeleton';
import { ClientePayload } from '@shared/cliente/interfaces/cliente';
import { ClientesService } from '@shared/cliente/services/clientes.service';
import { CLIENTE_EMAIL_MAX_LENGTH, CLIENTE_NOME_MAX_LENGTH } from '@shared/cliente/constants/cliente-limits';
import { FeedbackService } from '@shared/feedback/services/feedback.service';
import { toProblemDetails } from '@shared/http/functions/to-problem-details';

type ClienteFormModel = ClientePayload;

@Component({
  selector: 'app-create-or-edit',
  imports: [
    DatePipe,
    RouterLink,
    FormField,
    ButtonModule,
    InputTextModule,
    ToggleSwitchModule,
    SkeletonModule,
  ],
  templateUrl: './create-or-edit.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateOrEditComponent {
  private clientesService = inject(ClientesService);
  private feedbackService = inject(FeedbackService);
  private router = inject(Router);

  /** Parâmetro `:id` da rota; ausente no cadastro. */
  id = input<string>();

  readonly nomeMaxLength = CLIENTE_NOME_MAX_LENGTH;
  readonly emailMaxLength = CLIENTE_EMAIL_MAX_LENGTH;

  isEdit = computed(() => Boolean(this.id()));

  clienteRef = this.clientesService.getByIdWithHttpResource(this.id);

  cliente = computed(() => (this.clienteRef.hasValue() ? this.clienteRef.value() : undefined));

  isLoading = computed(() => this.isEdit() && this.clienteRef.isLoading() && !this.cliente());

  loadError = computed(() => {
    const error = this.clienteRef.error();
    return error ? toProblemDetails(error) : null;
  });

  /** Recomeça com os dados do cliente sempre que ele é (re)carregado. */
  model = linkedSignal<ClienteFormModel>(() => ({
    nome: this.cliente()?.nome ?? '',
    email: this.cliente()?.email ?? '',
    ativo: this.cliente()?.ativo ?? true,
  }));

  // Mesmas regras do Validator da API (CriarCliente/AtualizarCliente).
  form = form(this.model, (path) => {
    required(path.nome, { message: 'O nome do cliente é obrigatório.' });
    maxLength(path.nome, CLIENTE_NOME_MAX_LENGTH, {
      message: `O nome do cliente deve ter no máximo ${CLIENTE_NOME_MAX_LENGTH} caracteres.`,
    });

    required(path.email, { message: 'O e-mail do cliente é obrigatório.' });
    email(path.email, { message: 'O e-mail do cliente é inválido.' });
    maxLength(path.email, CLIENTE_EMAIL_MAX_LENGTH, {
      message: `O e-mail do cliente deve ter no máximo ${CLIENTE_EMAIL_MAX_LENGTH} caracteres.`,
    });
  });

  async save(event: Event) {
    event.preventDefault();

    await submit(this.form, async () => {
      try {
        await this.createOrEdit(this.model());
        this.router.navigate(['/clientes']);
        return undefined;
      } catch (error) {
        return this.handleSaveError(error);
      }
    });
  }

  private async createOrEdit(payload: ClienteFormModel) {
    const cliente = this.cliente();

    if (this.isEdit() && cliente) {
      await this.clientesService.put(cliente.id, { ...payload, versao: cliente.versao });
      this.feedbackService.success('Cliente alterado com sucesso!');
    } else {
      await this.clientesService.post(payload);
      this.feedbackService.success('Cliente cadastrado com sucesso!');
    }
  }

  /** Mostra o erro da API e devolve os erros de campo para o formulário exibir. */
  private handleSaveError(error: unknown): TreeValidationResult {
    const problem = toProblemDetails(error);

    if (problem.code === 'cliente-versao-desatualizada') {
      this.feedbackService.error(
        'Outro usuário alterou este cliente. Os dados atuais foram recarregados: revise e salve novamente.',
        problem.title,
      );
    } else {
      this.feedbackService.apiError(error);
    }

    switch (problem.code) {
      case 'validacao':
        return this.toFieldErrors(problem.errors ?? {});

      case 'cliente-email-ja-cadastrado':
        return { kind: 'server', message: problem.detail, fieldTree: this.form.email };

      case 'cliente-versao-desatualizada':
        // Recarrega o cliente para obter a versão atual (o formulário volta aos dados salvos).
        this.clienteRef.reload();
        return undefined;

      case 'cliente-nao-encontrado':
        this.router.navigate(['/clientes']);
        return undefined;

      default:
        return undefined;
    }
  }

  private toFieldErrors(errors: Record<string, string[]>) {
    const fields = { nome: this.form.nome, email: this.form.email, ativo: this.form.ativo };
    const result: ValidationError.WithOptionalFieldTree[] = [];

    for (const [property, messages] of Object.entries(errors)) {
      const fieldTree = fields[property.toLowerCase() as keyof typeof fields];
      for (const message of messages) {
        result.push({ kind: 'server', message, fieldTree });
      }
    }

    return result;
  }
}
