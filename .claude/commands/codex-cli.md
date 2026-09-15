---
description: Delegate a task to the raw Codex CLI on this repo (or start a fresh session) - distinct from the openai-codex plugin's codex:rescue agent
argument-hint: [--new] <task description>
allowed-tools: Bash(scripts/codex-delegate.sh:*), Bash(codex:*)
---

Delegate to the `codex` CLI directly using `scripts/codex-delegate.sh` (helpers documented at the top of that file). This is a separate, lighter mechanism from the `openai-codex` plugin's `codex:rescue` subagent/`codex-companion.mjs` - use THIS when the user asks for the raw Codex CLI by name; use the plugin's own `/codex:*` commands or the `codex:rescue` agent when the user asks for that instead.

1. Unless the user's arguments below start with `--new`, just continue: `codex exec resume --last` (which `scripts/codex-delegate.sh send` wraps) already resolves "the most recent session for this repo's cwd" on its own - there is no separate `latest`/list lookup step the way OpenCode needed one.
2. Send the task:
   - Continuing: `scripts/codex-delegate.sh send -- <task>`
   - Starting fresh (user passed `--new`): `scripts/codex-delegate.sh new -- <task>`
3. Both block until Codex finishes that turn, streaming raw `--json` events to stdout followed by a `=== final message ===` section (written via `-o`, not scraped from the event stream) - read that final section for the actual answer rather than the raw JSONL.
4. If `send` reports a "thread-store conflict: ... already has an active writer" error, another Codex run (very possibly one started through the `codex:rescue` plugin agent, which shares the same on-disk session store) is still actively writing to that same session - do not retry send in a loop; either wait, or check `scripts/codex-delegate.sh reply` for the busy session's progress instead.
5. To read back the last stored assistant message without starting a new turn (e.g. to check on a still-running background delegation), use `scripts/codex-delegate.sh reply`. This is a best-effort raw-JSONL scrape (codex has no `opencode export`-equivalent read command) - safe to call even while another process is actively writing that session, since it only reads.

Task to delegate: $ARGUMENTS

After delegating, relay Codex's actual answer back to the user in your own words - don't just paste raw JSON. If Codex's reply is a clarifying question rather than a result, surface that question to the user instead of guessing an answer on its behalf.
