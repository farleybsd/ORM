import { inject, Injectable } from '@angular/core';
import { ConfirmationService } from 'primeng/api';
import { DialogData } from '../interfaces/dialog-data';

@Injectable({
  providedIn: 'root',
})
export class ConfirmationDialogService {
  private confirmationService = inject(ConfirmationService);

  /** Resolve `true` quando o usuário confirma e `false` quando cancela ou fecha o diálogo. */
  open(data: DialogData) {
    return new Promise<boolean>((resolve) => {
      this.confirmationService.confirm({
        header: data.title,
        message: data.message,
        icon: 'pi pi-exclamation-triangle',
        acceptLabel: data.confirmLabel ?? 'Confirmar',
        rejectLabel: 'Cancelar',
        defaultFocus: 'reject',
        acceptButtonProps: { severity: 'danger' },
        rejectButtonProps: { severity: 'secondary', outlined: true },
        accept: () => resolve(true),
        reject: () => resolve(false),
      });
    });
  }
}
