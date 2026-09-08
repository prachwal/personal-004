---
description: Delegate a task to Claude Code, continuing the current repo session or starting a new one
---

Delegate the task to Claude Code using `scripts/claude-delegate.sh`.

1. Unless the arguments start with `--new`, continue Claude's most recent session for this repository with `scripts/claude-delegate.sh send <task>`.
2. If the arguments start with `--new`, omit that flag from the delegated task and run `scripts/claude-delegate.sh new <task>`.
3. The wrapper uses Claude's non-interactive JSON output and an unattended permission mode. Use it only in a trusted working tree.
4. Do not run this alongside another agent writing to the same working tree.

Task to delegate: $ARGUMENTS

Relay Claude's actual answer in your own words. If Claude asks a clarifying question, show it to the user instead of guessing.
