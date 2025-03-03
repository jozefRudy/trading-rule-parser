import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { BacktestService } from '../../services/backtest.service';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { first, map } from 'rxjs';
import { GenericEditorComponent } from '../generic-editor/generic-editor.component';
import {
  helperMethods,
  universeModule,
  code,
  catchSnackbarError,
} from '../../types/editor';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-strategy-editor',
  imports: [GenericEditorComponent],
  templateUrl: './strategy-editor.component.html',
  styles: ``,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StrategyEditorComponent implements OnInit {
  private editor = viewChild.required(GenericEditorComponent);
  protected errors = signal(false);
  private ready = signal(false);
  private ready$ = toObservable(this.ready);
  protected code = code;

  constructor(
    private service: BacktestService,
    private destroyRef: DestroyRef,
    private snackBar: MatSnackBar,
  ) {}

  handleErrors(x: boolean) {
    this.errors.set(x);
  }

  handleReady(x: boolean) {
    this.ready.set(x);
  }

  ngOnInit() {
    this.ready$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        first((x) => x),
      )
      .subscribe((x) => {
        this.addUniverseModule();
        this.editor().addExtraLibTs('/helper-methods.ts', helperMethods);
      });
  }

  addUniverseModule() {
    this.service
      .get_universe()
      .pipe(
        map((x) => x.map((x) => x.ticker)),
        catchSnackbarError(this.snackBar),
      )
      .subscribe((u) => {
        this.editor().addExtraLibTs('/universe.ts', universeModule(u));
      });
  }
}