# AGENTS.md

Guidance for agentic coding tools operating in this repository.

## Project Overview
- Language: C#.
- Platform target: `netstandard2.0`.
- Solution: `Farsight.Common.slnx`.
- Main projects:
  - `src/Farsight.Common/Farsight.Common.csproj`.
  - `src/Farsight.Common.SharedTypes/Farsight.Common.SharedTypes.csproj`.
- CI release workflow: `.github/workflows/publish-to-nuget.yml`.

## Toolchain
- CI uses .NET SDK `10.x`.
- No `global.json` exists, so local SDK is environment-selected.
- Use repo root (`/workspaces/Farsight.Common`) as default working directory.

## Build / Lint / Test Commands

### Restore
```bash
dotnet restore Farsight.Common.slnx
```

### Build
```bash
dotnet build Farsight.Common.slnx --configuration Release --no-restore
```

### Pack
```bash
dotnet pack src/Farsight.Common --configuration Release --no-build --output ./nupkgs
```

### Lint (analyzer gate)
There is no separate linter script; use build warnings as lint.

```bash
dotnet build Farsight.Common.slnx --configuration Release -warnaserror
```

Format verification:
```bash
dotnet format Farsight.Common.slnx --verify-no-changes
```

### Tests
At the moment, there are no test projects (`*Test*.csproj` not found).

## CI Reference Commands
The NuGet publish workflow executes:
1. `dotnet restore`
2. `dotnet build --configuration Release --no-restore`
3. `dotnet pack src/Farsight.Common --configuration Release --no-build --output ./nupkgs`
4. `dotnet nuget push ./nupkgs/*.nupkg ... --skip-duplicate`

Use these same steps when reproducing CI issues locally.
Never try to release push a package when the user did not explicity tell you to.

## Code Style: Source of Truth
- Primary authority: `.editorconfig`.
- If this file and generic conventions conflict, follow `.editorconfig`.

## Formatting Rules
- Indent with 4 spaces.
- C# files use `CRLF` line endings.
- Ensure final newline at EOF.
- Braces are mandatory (`csharp_prefer_braces = true:error`).
- Use file-scoped namespaces (`csharp_style_namespace_declarations = file_scoped:error`).
- Keep `using` directives outside namespaces.

## Imports and Usings
- Do not force `System.*` first (`dotnet_sort_system_directives_first = false`).
- Do not separate import groups with blank lines (`dotnet_separate_import_directive_groups = false`).
- Remove unused usings.

## Type System and Nullability
- Nullable is enabled (`<Nullable>enable</Nullable>` in both projects).
- Use nullable annotations intentionally.
- Prefer predefined type keywords for locals/parameters/members when appropriate.
- Keep type choices consistent with neighboring code.

## `var` Conventions
- Prefer `var` when the type is apparent.
- Built-in types may remain explicit based on readability (`int`, `string`, etc.).
- Match the established style in the file you are editing.

## Naming Conventions
- Types and members: PascalCase.
- Interfaces: `I` + PascalCase.
- Locals and parameters: camelCase.
- Private/protected/internal fields: `_camelCase`.
- Constants: `ALL_UPPER_CASE`.
- Generic type parameters: `T` prefix (for example `TOptions`).

## Preferred C# Features in This Repo
- Primary constructors are already used.
- Expression-bodied members are common.
- Collection expressions (`[]`, `[.. items]`) are acceptable.
- Pattern matching and null-propagation are encouraged.

## Error Handling Guidelines
- Generator misuse should surface as diagnostics, not silent failures.
- Runtime service failures should be logged with high severity and stop the host when required.
- Respect cancellation tokens and treat cancellation as expected flow.
- Do not swallow exceptions without explicit handling rationale.

## Source Generator Guidance
- Keep generated code deterministic.
- Use incremental generator APIs (`CreateSyntaxProvider`, `Collect`, `Combine`) idiomatically.
- Keep syntax predicates cheap; do semantic analysis in transform stages.
- Report actionable diagnostics via `SourceProductionContext.ReportDiagnostic`.

## Architecture Notes
- `src/Farsight.Common`: source generator logic, diagnostics, code utilities.
- `src/Farsight.Common.SharedTypes`: shared attributes and startup/runtime abstractions.
- `FarsightCommonRegistry` stores registration actions and applies them to the host builder.

## Cursor / Copilot Rules
Checked for repository-specific agent instruction files:
- `.cursorrules`
- `.cursor/rules/`
- `.github/copilot-instructions.md`

Current status: none of these files exist.
If any are added later, treat them as higher-priority instructions and update this document.

## Agent Change Checklist
Before finishing a change:
1. Restore dependencies.
2. Build in Release (prefer `-warnaserror` for analyzer cleanliness).
3. Run tests, or explicitly state that no tests exist yet.
4. For generator edits, validate diagnostics and generated-source assumptions.
5. Ensure naming/formatting align with `.editorconfig`.
