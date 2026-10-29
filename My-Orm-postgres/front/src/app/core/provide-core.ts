import { LOCALE_ID, makeEnvironmentProviders } from '@angular/core';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { registerLocaleData } from '@angular/common';
import ptBr from '@angular/common/locales/pt';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';
import Aura from '@primeuix/themes/aura';

registerLocaleData(ptBr);

export function provideCore() {
  return makeEnvironmentProviders([
    provideHttpClient(withFetch()),
    providePrimeNG({
      // O PrimeNG 22 exige uma chave de licença (há uma Community License gratuita):
      // https://primeui.dev/licenses/community — informe-a aqui com `license: '...'`.
      ripple: true,
      theme: {
        preset: Aura,
        options: {
          darkModeSelector: '.app-dark',
          cssLayer: {
            name: 'primeng',
            order: 'theme, base, primeng',
          },
        },
      },
    }),
    MessageService,
    ConfirmationService,
    { provide: LOCALE_ID, useValue: 'pt-BR' },
  ]);
}
