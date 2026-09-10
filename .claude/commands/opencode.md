---
description: Delegate a task to the current OpenCode session on this repo (or start a fresh one)
argument-hint: [--new] <task description>
allowed-tools: Bash(scripts/opencode-delegate.sh:*), Bash(opencode:*)
---

Delegate to OpenCode using `scripts/opencode-delegate.sh` (helpers documented at the top of that file):

1. Unless the user's arguments below start with `--new`, find the session OpenCode was last working in on this repo: `scripts/opencode-delegate.sh latest` (prints a session ID, or nothing if there's no session yet - fall back to `new` in that case).
2. Send the task into that same session so OpenCode keeps its own context (never `--fork` - the goal is landing in that session, not branching off it):
   - Continuing: `scripts/opencode-delegate.sh send -- <task>`
   - Starting fresh (user passed `--new`, or step 1 found nothing): `scripts/opencode-delegate.sh new -- <task>`
3. `send`/`new` block until OpenCode finishes that turn and print its raw JSON event stream (`--format json`) - skim it for the final assistant message rather than dumping it verbatim to the user.
4. If it's ambiguous whether OpenCode is done (e.g. the command was run in the background, or you want to check on it later without re-sending anything), read back its last reply without adding a new turn: `scripts/opencode-delegate.sh reply`. This can fail with a "still mid-reply" error if OpenCode is actively writing to that session when you read it - that's not a real error, just retry after a few seconds.

Task to delegate: $ARGUMENTS

After delegating, relay OpenCode's actual answer back to the user in your own words - don't just paste raw JSON. If OpenCode's reply is a clarifying question rather than a result, surface that question to the user instead of guessing an answer on its behalf.
