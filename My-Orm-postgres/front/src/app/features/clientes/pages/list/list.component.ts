import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { Cliente } from '@shared/cliente/interfaces/cliente';
import { ClientesService } from '@shared/cliente/services/clientes.service';
import { ConfirmationDialogService } from '@shared/dialog/confirmation/services/confirmation-dialog.service';
import { FeedbackService } from '@shared/feedback/services/feedback.service';
import { toProblemDetails } from '@shared/http/functions/to-problem-details';

function normalize(text: string) {
  return text
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .trim();
}

@Component({
  selector: 'app-list',
  imports: [
    DatePipe,
    RouterLink,
    TableModule,
    ButtonModule,
    InputTextModule,
    IconFieldModule,
    InputIconModule,
    TagModule,
    TooltipModule,
  ],
  templateUrl: './list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ListComponent {
  private clientesService = inject(ClientesService);
  private feedbackService = inject(FeedbackService);
  private confirmationDialogService = inject(ConfirmationDialogService);
  private router = inject(Router);

  searchTerm = signal('');

  /** Id do cliente sendo excluído (mostra o loading no botão da linha). */
  removingId = signal<number | null>(null);

  resourceRef = this.clientesService.getAllWithHttpResource();

  isLoading = computed(() => this.resourceRef.isLoading());

  loadError = computed(() => {
    const error = this.resourceRef.error();
    return error ? toProblemDetails(error) : null;
  });

  private allClientes = computed(() => (this.resourceRef.hasValue() ? this.resourceRef.value() : []));

  clientes = computed(() => {
    const term = normalize(this.searchTerm());

    if (!term) {
      return this.allClientes();
    }

    return this.allClientes().filter(
      (cliente) => normalize(cliente.nome).includes(term) || normalize(cliente.email).includes(term),
    );
  });

  total = computed(() => this.allClientes().length);

  onSearch(event: Event) {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  reload() {
    this.resourceRef.reload();
  }

  edit(cliente: Cliente) {
    this.router.navigate(['/clientes', 'editar', cliente.id]);
  }

  async remove(cliente: Cliente) {
    const confirmed = await this.confirmationDialogService.open({
      title: 'Excluir cliente',
      message: `Você realmente quer excluir o cliente "${cliente.nome}"? Esta ação não pode ser desfeita.`,
      confirmLabel: 'Excluir',
    });

    if (!confirmed) {
      return;
    }

    this.removingId.set(cliente.id);

    try {
      await this.clientesService.delete(cliente.id);
      this.removeClienteFromArray(cliente);
      this.feedbackService.success('Cliente excluído com sucesso!');
    } catch (error) {
      // Já excluído por outra pessoa: sai da lista do mesmo jeito.
      if (toProblemDetails(error).status === 404) {
        this.removeClienteFromArray(cliente);
      }
      this.feedbackService.apiError(error);
    } finally {
      this.removingId.set(null);
    }
  }

  private removeClienteFromArray(cliente: Cliente) {
    this.resourceRef.update((clientes) => clientes.filter((item) => item.id !== cliente.id));
  }
}
