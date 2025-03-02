Code parser example used by [cryptoquant](https://cryptoquant.dev) in v1.

It consists of angular frontend app with codemirror6 code editor with live typescript intellisense served on frontend.
Trading DSL is a subset of typescript. Only valid typescript after validating on frontend is sent to backend.

Backend contains AST and AST parser that validates strategy received from frontend. 
There is an indication below code editor on frontend if strategy is valid (it is valid only if it's valid typescript and was validated by backend as well).

To run this, you need both `pnpm` and `dotnet 9`. Ports are pre-configured in both services `3000` for frontend and `3001` for backend.

```bash frontend
cd frontent-angular
pnpm start
```

```bash backend
cd backend-parser
dotnet run
```

![output](https://github.com/user-attachments/assets/f59fa372-2483-48ce-b166-659f23c4922d)

Editor expects these variables to be defined: `entry`, `exit`, `universe`, `name`, `start`, `mode`.

For development I recommend opening both projects in the same editor, jetbrains rider works well for this.
