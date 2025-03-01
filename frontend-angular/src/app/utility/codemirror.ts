import { VirtualTypeScriptEnvironment } from '@typescript/vfs';
import { CompletionContext, CompletionResult } from '@codemirror/autocomplete';
import { EditorView } from 'codemirror';
import { EditorState } from '@codemirror/state';
import {
  Decoration,
  DecorationSet,
  ViewPlugin,
  ViewUpdate,
} from '@codemirror/view';
import { RangeSetBuilder } from '@codemirror/state';
import { HoverInfo } from '@valtown/codemirror-ts';

export function createOrUpdateFile(
  env: VirtualTypeScriptEnvironment,
  path: string,
  code: string,
): void {
  if (!env.getSourceFile(path)) {
    env.createFile(path, code);
  } else {
    env.updateFile(path, code);
  }
}

export const readOnlyTwoTopLines = (
  targetState: EditorState,
): Array<{ from: number | undefined; to: number | undefined }> => {
  return [
    {
      from: undefined,
      to: targetState.doc.line(2).to,
    },
  ];
};

export function customStyledLines(lines: number[]) {
  return [baseTheme, styleLines(lines)];
}

export const baseTheme = EditorView.baseTheme({
  '.cm-content': {
    fontSize: '11.5pt',
  },
  '.cm-gutters': {
    fontSize: '11.5pt',
  },
  '.cm-grayBackground': { backgroundColor: '#e5e7eb' },

  '.cm-hover-tooltip': {
    margin: '2px',
  },
  '.cm-hover-signature:not(:last-child)': {
    marginBottom: '4px',
    paddingBottom: '4px',
    borderBottom: '1px solid #e0e0e0',
  },
  '.cm-hover-signature .cm-quick-info-methodName': {
    color: '#0000FF',
    fontWeight: 'bold',
  },
  '.cm-hover-signature .cm-quick-info-text': {},
  '.cm-hover-signature .cm-quick-info-parameterName': {
    color: '#008000',
  },
  '.cm-hover-signature .cm-quick-info-keyword': {
    color: '#0000FF',
  },
  '.cm-hover-tag': {
    color: '#666666',
  },
  '.cm-hover-tag:not(:last-child)': {
    marginBottom: '4px',
    paddingBottom: '4px',
    borderBottom: '1px solid #e0e0e0',
  },
});

const lineDecoration = Decoration.line({
  attributes: { class: 'cm-grayBackground' },
});

function decorateLines(view: EditorView, lines: number[]) {
  let builder = new RangeSetBuilder<Decoration>();
  for (let { from, to } of view.visibleRanges) {
    for (let pos = from; pos <= to; ) {
      let line = view.state.doc.lineAt(pos);

      if (lines.includes(line.number)) {
        builder.add(line.from, line.from, lineDecoration);
      }
      pos = line.to + 1;
    }
  }
  return builder.finish();
}

function styleLines(lines: number[]) {
  return ViewPlugin.fromClass(
    class {
      decorations: DecorationSet;

      constructor(view: EditorView) {
        this.decorations = decorateLines(view, lines);
      }

      update(update: ViewUpdate) {
        if (update.docChanged || update.viewportChanged)
          this.decorations = decorateLines(update.view, lines);
      }
    },
    {
      decorations: (v) => v.decorations,
    },
  );
}

function getTag(info: HoverInfo, name: string) {
  const descriptionTag = info.quickInfo?.tags?.find((tag) => tag.name === name);
  if (descriptionTag?.text) {
    const div = document.createElement('div');
    div.innerHTML =
      `<strong>@${name}</strong><br>` +
      descriptionTag.text
        .map((t) => t.text)
        .join('')
        .replace(/\n/g, '<br>')
        .replace(/- /g, '• ');

    div.className = `cm-hover-tag`;
    return div;
  }
  return undefined;
}

export function renderTooltip(info: HoverInfo) {
  const div = document.createElement('div');
  div.className = 'cm-hover-tooltip';

  if (info.quickInfo?.displayParts) {
    const innerDiv = div.appendChild(document.createElement('div'));
    for (const part of info.quickInfo.displayParts) {
      const span = innerDiv.appendChild(document.createElement('span'));
      span.className = `cm-quick-info-${part.kind}`;
      span.innerText = part.text;
    }
    innerDiv.className = 'cm-hover-signature';
  }

  const desc = getTag(info, 'description');
  if (desc) {
    div.appendChild(desc);
  }

  const example = getTag(info, 'example');
  if (example) {
    div.appendChild(example);
  }

  return { dom: div };
}