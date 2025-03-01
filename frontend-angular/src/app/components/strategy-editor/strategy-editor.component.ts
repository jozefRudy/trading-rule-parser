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
import { MatSnackBar } from '@angular/material/snack-bar';
import { GenericEditorComponent } from '../generic-editor/generic-editor.component';
import { helperMethods, universeModule, code } from '../../types/editor';

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
  private _ready$ = toObservable(this.ready);
  protected code = code;

  constructor(
    private service: BacktestService,
    private snackBar: MatSnackBar,
    private destroyRef: DestroyRef,
  ) {}

  handleErrors(x: boolean) {
    this.errors.set(x);
  }

  handleReady(x: boolean) {
    this.ready.set(x);
  }

  ngOnInit() {
    this._ready$
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
      .pipe(map((x) => x.map((x) => x.ticker)))
      .subscribe((u) => {
        this.editor().addExtraLibTs('/universe.ts', universeModule(u));
      });
  }
}