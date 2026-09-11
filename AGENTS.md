# Repository Instructions

- The repository targets .NET 10 with `PetEmulator.slnx`; shared settings are in `Directory.Build.props` and package versions in `Directory.Packages.props`.
- The solution contains the 6502 CPU, PET, VIC-20, CLI, Desktop, Screenshot, and test projects. Inspect the relevant project before choosing a command, then prefer `dotnet build PetEmulator.slnx --no-restore` and targeted `dotnet test` runs.
- CLI entrypoint: `dotnet run --project src/PetEmulator.Cli -- apps` lists predefined applications; `dotnet run --project src/PetEmulator.Cli -- run status` runs one. CLI settings come from `src/PetEmulator.Cli/appsettings.json` and can be overridden with `--config`, `--profile`, `--roms`, `--steps`, and `--log-level`.

## Project conventions

- Desktop UI follows MVVM: `Views/` contains Avalonia XAML and code-behind, `ViewModels/` contains presentation logic, `Services/` contains UI-bound integrations, and `Views/Controls/` contains reusable controls.
- Keep Avalonia-specific APIs in Views or Services. ViewModels should depend on interfaces such as `IFilePickerService`.
- Use compiled bindings and explicit `x:DataType` declarations in XAML. Put shared UI colors in `App.axaml` resources and reference them with `DynamicResource`.
- The Avalonia previewer may load the Desktop assembly from a temporary directory. ROM lookup also checks the working directory; set `PET_EMULATOR_ROMS` when using a custom ROM location.

## Codex local setup

- Optional machine-local defaults may be kept in `.codex/config.toml`; the file is ignored by Git and must not contain credentials or machine-specific trust/MCP configuration.
- Codex instructions belong in this file or in a more specific nested `AGENTS.md`. Keep repository rules here; keep personal model and approval preferences in `~/.codex/config.toml`.

<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **personal-004** (4147 symbols, 12057 relationships, 347 execution flows).

> Index stale? Run `node .gitnexus/run.cjs analyze --index-only` from the project root — it auto-selects an available runner. No `.gitnexus/run.cjs` yet? Bootstrap with `npx`, `bunx`, or `pnpm dlx` — e.g. `bunx gitnexus@latest analyze` (npm 11 npx crash; #1939).

## Always Do

- **MUST run impact analysis before editing.** Use `impact({target: "symbolName", direction: "upstream"})` (MCP) or `node .gitnexus/run.cjs impact "symbolName" --direction upstream --repo .` (CLI fallback); report callers, processes, and risk. Never substitute grep for graph analysis.
- **MUST analyze graph changes before committing.** Use `detect_changes({scope: "all"})` (MCP) or `node .gitnexus/run.cjs detect-changes --scope all --repo .` (CLI fallback). `partial: true` or `truncated: true` is not a clean check — a zero means unseen, not unaffected; re-run it. For regression review: `detect_changes({scope: "compare", base_ref: "main"})` or `node .gitnexus/run.cjs detect-changes --scope compare --base-ref "main" --repo .`.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- **MUST treat `risk: UNKNOWN` as unresolved, not as low.** An empty caller set is not evidence the symbol is unused — it can also mean the callers are not resolvable by the index (plain-object property access, dynamic dispatch, cross-language calls). `impact` pairs `UNKNOWN` with a `riskNote` saying so. Confirm with a text search before treating the symbol as safe to change or delete; do not proceed on the strength of a zero.
- When exploring unfamiliar code, use `query({search_query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `context({name: "symbolName"})`.
- For security review, `explain({target: "fileOrSymbol"})` lists taint findings (source→sink flows; needs `analyze --pdg`).

## Never Do

- NEVER edit a function, class, or method before MCP/CLI impact analysis.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis, and never read `UNKNOWN` as an all-clear — it means the walk could not answer, which is the one verdict that requires confirming by other means.
- NEVER rename symbols with find-and-replace — use `rename` which understands the call graph.
- NEVER commit before MCP/CLI graph change analysis.

## Resources

| Resource | Use for |
| --- | --- |
| `gitnexus://repo/personal-004/context` | Codebase overview, check index freshness |
| `gitnexus://repo/personal-004/clusters` | All functional areas |
| `gitnexus://repo/personal-004/processes` | All execution flows |
| `gitnexus://repo/personal-004/process/{name}` | Step-by-step execution trace |

## CLI

| Task | Read this skill file |
| --- | --- |
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->
