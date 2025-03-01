import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () =>
      import('./components/strategy-editor/strategy-editor.component').then(
        (x) => x.StrategyEditorComponent,
      ),
  },
];