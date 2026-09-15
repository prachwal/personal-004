<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **personal-004** (1939 symbols, 3613 relationships, 16 execution flows).

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

## Subagents

Work touching `lib/PetEmulator.Cpu6502/` goes through dedicated subagents instead of ad-hoc edits — they carry the file map and known debt so you don't re-derive it each session:

| Task | Agent |
| --- | --- |
| Implement/fix an opcode, migrate a family off the legacy big-switch, add a variant (65C02, 6510), touch flags/BCD/interrupts | `cpu6502-worker` (`.claude/agents/cpu6502-worker.md`) |
| Review a CPU-core diff against `docs/architecture/overview.md` before commit | `cpu6502-reviewer` (`.claude/agents/cpu6502-reviewer.md`) |

Both still obey the gitnexus gates above (impact before edit, detect_changes before commit, `UNKNOWN` risk = unresolved).

## Codex delegation

Delegating to Codex (`codex:codex-rescue` subagent, or `scripts/codex-delegate.sh` for the raw CLI) works, but tune the call to the task or it burns wall-clock on rediscovery instead of the actual work — lessons paid for the hard way in the CPC464 tape/keyboard/benchmark sessions:

- **Split by sub-goal, not by feature.** One prompt covering "verify N keyboard positions AND run a CPU-bound benchmark AND round-trip a tape save" makes the slow, flaky part (an exhaustive matrix sweep = dozens of full ROM boots) block the parts that don't need it. Separate calls also let a stuck one fail without losing the others.
- **Hand over facts, not open questions.** If you already know the exact constant (a tick-timing value, a file path, a verified matrix position, a working directory), give it as a fact ("PressKey holds 25,000 ticks, releases 15,000 - see Cpc464BootTests.cs") not a question to rediscover. Re-deriving something you've already established is the single biggest time sink observed.
- **Point it at prior artifacts.** If an earlier run already built a `tools/*` project or wrote a `build/*.json`, say so explicitly and tell it to reuse/extend that rather than building a fresh scratch harness (`/tmp/...`) from zero - watched this happen twice, including a scratch project whose particular `dotnet build` flag combination (`-m:1 /p:BuildInParallel=false ...`) crashed with SIGABRT repeatedly in this sandbox.
- **Match `--effort` to the task.** A well-specified, narrow fix doesn't need the default reasoning budget; only reach for `high`/`xhigh` when the task is genuinely open-ended (find an unknown key position, diagnose an unexplained failure).
- **`--resume` a live investigation instead of starting fresh** when the follow-up needs the same context (same file, same half-finished sweep) - pass the prior Codex session ID explicitly and say "resume", not just mention it for reference.
- **Never trust a completion report at face value.** Codex is often honest about what it couldn't verify (e.g. "not conclusively decoded from rendered glyphs") - read the report for exactly that kind of hedge, then re-verify anything load-bearing yourself before it lands in production code. A caught example: an "added and verified" keyboard mapping that silently pointed at a matrix column that doesn't exist.
