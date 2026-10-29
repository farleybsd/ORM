import { Routes } from '@angular/router';
import { ListComponent } from './pages/list/list.component';
import { CreateOrEditComponent } from './pages/create-or-edit/create-or-edit.component';

export default [
  {
    path: '',
    component: ListComponent,
    title: 'Clientes',
  },
  {
    path: 'novo',
    component: CreateOrEditComponent,
    title: 'Novo cliente',
  },
  {
    // `:id` chega ao componente como input (withComponentInputBinding)
    path: 'editar/:id',
    component: CreateOrEditComponent,
    title: 'Editar cliente',
  },
] as Routes;
