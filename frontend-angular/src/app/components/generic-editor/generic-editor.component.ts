import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  input,
  OnDestroy,
  OnInit,
  output,
  viewChild,
} from '@angular/core';

import { basicSetup, EditorView } from 'codemirror';
import { indentWithTab } from '@codemirror/commands';

import { Compartment, EditorState } from '@codemirror/state';
import { VirtualTypeScriptEnvironment } from '@typescript/vfs';

import {
  BehaviorSubject,
  combineLatest,
  debounceTime,
  distinctUntilChanged,
  filter,
  map,
  switchMap,
  tap,
} from 'rxjs';
import { catchEditorError, ParserError } from '../../types/editor';
import { TypeScriptService } from '../../services/typescript.service';
import { fromPromise } from 'rxjs/internal/observable/innerFrom';
import { Diagnostic, linter, lintGutter } from '@codemirror/lint';

import {
  baseTheme,
  createOrUpdateFile,
  customStyledLines,
  readOnlyTwoTopLines,
  renderTooltip,
  templateAutocomplete,
} from '../../utility/codemirror';
import { javascript } from '@codemirror/lang-javascript';
import {
  tsAutocomplete,
  tsFacet,
  tsHover,
  tsLinter,
  tsSync,
} from '@valtown/codemirror-ts';
import { autocompletion } from '@codemirror/autocomplete';
import readOnlyRangesExtension from 'codemirror-readonly-ranges';
import { DiagnosticCategory } from 'typescript';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { BacktestService } from '../../services/backtest.service';
import { equals } from 'ramda';

import { MatSnackBar } from '@angular/material/snack-bar';
import { keymap } from '@codemirror/view';

@Component({
  selector: 'app-generic-editor',
  imports: [],
  templateUrl: './generic-editor.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    #container {
      height: 100%;

      overflow: auto;
      border-width: 2px;
      margin-right: 2rem;
      margin-left: 2rem;
    }
  `,
})
export class GenericEditorComponent implements OnInit, OnDestroy {
  diagnostics: Diagnostic[] = [];
  lintCompartment = new Compartment();

  code = input.required<string>();
  code$ = toObservable(this.code);

  readOnly = input.required<boolean>();

  private container = viewChild.required<ElementRef>('editorContainer');
  private env: VirtualTypeScriptEnvironment | null = null;
  private editor: EditorView | null = null;

  private _tsErrors$ = new BehaviorSubject(false);
  private _staticErrors$ = new BehaviorSubject(false);
  errors$ = output<boolean>();

  private _content$ = new BehaviorSubject('');
  ready$ = output<boolean>();

  constructor(
    private tsService: TypeScriptService,
    private service: BacktestService,
    private snackBar: MatSnackBar,
    private destroyRef: DestroyRef,
  ) {}

  ngOnDestroy(): void {
    this.editor?.destroy();
    this.env?.languageService.dispose();
  }

  ngOnInit() {
    this.code$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((x) => this.updateCode(x));

    if (this.readOnly()) {
      this.createEditor(null, this.code());
      this.ready$.emit(true);
      return;
    }

    fromPromise(this.tsService.getVirtualTypeScriptEnvironment()).subscribe(
      (env) => {
        this.createEditor(env, this.code());
        this.env = env;
        this.ready$.emit(true);
      },
    );

    combineLatest([this._tsErrors$, this._staticErrors$])
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        map(([tsErrors, staticErrors]) => tsErrors || staticErrors),
      )
      .subscribe((x) => this.errors$.emit(x));

    combineLatest([this._content$, this._tsErrors$])
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        filter(([content, tsErrors]) => content.length > 0 && !tsErrors),
        distinctUntilChanged(([prevContent], [currContent]) => {
          return equals(prevContent, currContent);
        }),
        tap(() => this._staticErrors$.next(true)),
        debounceTime(500),
        switchMap(([x]) =>
          this.service.parse(x).pipe(
            tap((_) => this.clearErrors()),

            catchEditorError(this),
            //catchSnackBarError(this.snackBar),
          ),
        ),
      )
      .subscribe();
  }

  getExtensions(env: VirtualTypeScriptEnvironment | null) {
    if (env === null) {
      return [
        basicSetup,
        javascript({ typescript: true, jsx: false }),
        EditorState.readOnly.of(true),
        baseTheme,
      ];
    }

    return [
      basicSetup,
      keymap.of([indentWithTab]),
      javascript({ typescript: true, jsx: false }),
      lintGutter(),
      tsLinter(),
      autocompletion({
        maxRenderedOptions: 50,
        override: [tsAutocomplete(), templateAutocomplete],
      }),
      tsHover({ renderTooltip: renderTooltip }),
      tsFacet.of({ env: env, path: '/index.ts' }),
      tsSync(),
      this.lintCompartment.of(this.linterExtensions()),
      this.createUpdateListenerExtension(env),
      readOnlyRangesExtension(readOnlyTwoTopLines),
      customStyledLines([1, 2]),
    ];
  }

  private createUpdateListenerExtension(env: VirtualTypeScriptEnvironment) {
    return EditorView.updateListener.of((update) => {
      const reconfigured =
        update.transactions.filter((x) => x.reconfigured).length > 0;

      if (!this.editor) {
        return;
      }

      if (!update.docChanged && !reconfigured) {
        return;
      }

      const syntactic = env.languageService
        .getSyntacticDiagnostics('/index.ts')
        .filter((x: any) => x.category === DiagnosticCategory.Error).length;

      const semantic = env.languageService
        .getSemanticDiagnostics('/index.ts')
        .filter((x: any) => x.category === DiagnosticCategory.Error).length;

      this._tsErrors$.next(syntactic + semantic > 0);
      this._content$.next(this.getContent());
    });
  }

  private createEditor(env: VirtualTypeScriptEnvironment | null, code: string) {
    this.editor = new EditorView({
      doc: code,
      extensions: this.getExtensions(env),
      parent: this.container().nativeElement,
    });
  }

  updateCode(code: string) {
    this.editor?.dispatch({
      changes: {
        from: 0,
        to: this.editor.state.doc.length,
        insert: code,
      },
    });
  }

  addExtraLibTs(filename: string, lib: string) {
    if (this.env === null) {
      throw new Error('editor not initialized');
    }

    createOrUpdateFile(this.env, filename, lib);
    this.lint();
  }

  getContent(): string {
    return this.editor?.state.doc.toString() ?? '';
  }

  clearErrors() {
    this._staticErrors$.next(false);
    this.diagnostics.length = 0;
    this.lint();
  }

  setError(error: ParserError) {
    if (!this.editor) return;
    this.diagnostics.length = 0;

    const pos = Math.min(this.editor.state.doc.length, error.index - 1);
    let line = this.editor.state.doc.lineAt(pos).number;

    //if error is on empty line we need to find previous non-empty line
    while (this.editor.state.doc.line(line).length === 0 && line > 0) {
      line = line - 1;
    }

    const editorLine = this.editor.state.doc.line(line);

    this.diagnostics.push({
      source: 'static analysis',
      from: editorLine.from,
      to: editorLine.to,
      severity: 'error',
      message: error.message,
    });
    this.lint();
  }

  lint() {
    this.editor?.dispatch({
      effects: [this.lintCompartment.reconfigure(this.linterExtensions())],
    });
  }
  linterExtensions() {
    return [
      linter((view) => this.diagnostics, {
        autoPanel: true,
      }),
    ];
  }
}