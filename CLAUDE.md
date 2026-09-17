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

## Writing implementation plans (`docs/plans/*.md`)

Lesson from the 2026-09-16 CPC6128/composition planning sessions, where five
overlapping plan files with status prose scattered across them turned into a
mess that needed a full consolidation pass. Write plans so a fresh session
(or a weaker model) can tell, at a glance, exactly what's done:

- **Every actionable item is a markdown checkbox**, `- [ ]`/`- [x]`, not a
  numbered step or a prose paragraph claiming completion. Sub-steps of one
  task are nested checkboxes under it, not a separate "status" section
  bolted on later.
- **The plan file itself carries the instruction to check items off.** Put a
  line near the top, e.g. "Check off each box in this file as you complete
  and verify it - do not just report completion in chat." An executing
  session (including a future, context-free one) must not need to be told
  this separately; it has to be part of the document it's reading.
- **Only check a box after verifying, not after writing code.** "Verified"
  means you ran the build/test/grep yourself this session and saw the
  result - a prior session's or subagent's claim of done is a reason to
  re-check, not a reason to check the box.
- **One bounded, independently-committable task per top-level checklist
  section**, with the exact file(s) it touches and the verification command
  for that section inline - not a shared "run everything at the end" step
  that hides which specific task broke.
- **Update the plan in place, don't append a growing "status" log at the
  bottom.** A status paragraph appended after every session is exactly what
  produced today's mess across `cpc6128-integration-plan.md`,
  `cpc6128-remaining-work-plan.md`, `machine-composition-and-reuse-plan.md`,
  and `cpc-composition-cleanup-plan.md` - four files all partially
  describing the same current state. When a plan is fully checked off,
  either fold its still-relevant facts into a status doc for that theme and
  delete the plan file, or leave it checked-off as a closed historical
  record - don't keep editing a "done" plan's prose indefinitely.
