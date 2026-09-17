# Repository Instructions

- The repository targets .NET 10 with `PetEmulator.slnx`; shared settings are in `Directory.Build.props` and package versions in `Directory.Packages.props`.
- The solution contains the 6502 CPU, PET, VIC-20, CLI, Desktop, Screenshot, and test projects. Inspect the relevant project before choosing a command, then prefer `dotnet build PetEmulator.slnx --no-restore` and targeted `dotnet test` runs. Long-running real-firmware tests are marked `BinaryBoot` and must stay opt-in; for the default solution run use `dotnet test PetEmulator.slnx --filter "TestCategory!=BinaryBoot"`, and run firmware tests explicitly with `--filter "TestCategory=BinaryBoot"`.
- CLI entrypoint: `dotnet run --project src/PetEmulator.Cli -- apps` lists predefined applications; `dotnet run --project src/PetEmulator.Cli -- run status` runs one. CLI settings come from `src/PetEmulator.Cli/appsettings.json` and can be overridden with `--config`, `--profile`, `--roms`, `--steps`, and `--log-level`.

## Project conventions

- Desktop UI follows MVVM: `Views/` contains Avalonia XAML and code-behind, `ViewModels/` contains presentation logic, `Services/` contains UI-bound integrations, and `Views/Controls/` contains reusable controls.
- Keep Avalonia-specific APIs in Views or Services. ViewModels should depend on interfaces such as `IFilePickerService`.
- Use compiled bindings and explicit `x:DataType` declarations in XAML. Put shared UI colors in `App.axaml` resources and reference them with `DynamicResource`.
- The Avalonia previewer may load the Desktop assembly from a temporary directory. ROM lookup also checks the working directory; set `PET_EMULATOR_ROMS` when using a custom ROM location.

## Documentation layout

- Keep the `docs/` root as an index only; update `docs/README.md` when adding or moving documentation.
- PET/CBM documentation belongs in `docs/pet/`; VIC-20 documentation belongs in `docs/vic20/`.
- Desktop, Avalonia, audio and monitoring documentation belongs in `docs/desktop/`.
- Shared component and chip contracts, maps, checklists and coverage requirements belong in `docs/chips/`, with one Markdown file per chip where practical.
- Architecture decisions belong in `docs/architecture/`; implementation plans remain in `docs/plans/`, and repository scans remain in `docs/scan/`.
- Every documentation subfolder should have a local `README.md` when it contains more than one document.
- Do not duplicate chip implementation checklists in PET/VIC-20 machine documents. Keep machine documents focused on address mapping, wiring, integration and machine-specific limitations; link to `docs/chips/` for chip contracts and tests.

## Codex local setup

- Optional machine-local defaults may be kept in `.codex/config.toml`; the file is ignored by Git and must not contain credentials or machine-specific trust/MCP configuration.
- Codex instructions belong in this file or in a more specific nested `AGENTS.md`. Keep repository rules here; keep personal model and approval preferences in `~/.codex/config.toml`.

<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **personal-004** (12259 symbols, 35833 relationships, 300 execution flows). Use the GitNexus MCP tools to understand code, assess impact, and navigate safely.

> Index stale? Run `node .gitnexus/run.cjs analyze` from the project root — it auto-selects an available runner. No `.gitnexus/run.cjs` yet? `npx gitnexus analyze` (npm 11 crash → `npm i -g gitnexus`; #1939).

## Always Do

- **MUST run impact analysis before editing any symbol.** Before modifying a function, class, or method, run `impact({target: "symbolName", direction: "upstream"})` and report the blast radius (direct callers, affected processes, risk level) to the user.
- **MUST run `detect_changes()` before committing** to verify your changes only affect expected symbols and execution flows. For regression review, compare against the default branch: `detect_changes({scope: "compare", base_ref: "main"})`.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- When exploring unfamiliar code, use `query({search_query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `context({name: "symbolName"})`.
- For security review, `explain({target: "fileOrSymbol"})` lists taint findings (source→sink flows; needs `analyze --pdg`).

## Never Do

- NEVER edit a function, class, or method without first running `impact` on it.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis.
- NEVER rename symbols with find-and-replace — use `rename` which understands the call graph.
- NEVER commit changes without running `detect_changes()` to check affected scope.

## Resources

| Resource | Use for |
|----------|---------|
| `gitnexus://repo/personal-004/context` | Codebase overview, check index freshness |
| `gitnexus://repo/personal-004/clusters` | All functional areas |
| `gitnexus://repo/personal-004/processes` | All execution flows |
| `gitnexus://repo/personal-004/process/{name}` | Step-by-step execution trace |

## CLI

| Task | Read this skill file |
|------|---------------------|
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->
