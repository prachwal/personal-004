---
description: Delegate a task to Mistral Vibe, or start a fresh Vibe session
---

Delegate the task to Vibe using `scripts/vibe-delegate.sh`.

1. Unless the arguments start with `--new`, run `scripts/vibe-delegate.sh latest` to get Vibe's most recently saved session ID. If no session exists, use `new`.
2. Send the task:
   - Continue the latest session: `scripts/vibe-delegate.sh send <task>`
   - Continue a known session: `scripts/vibe-delegate.sh send --session <uuid> <task>`
   - Start fresh: `scripts/vibe-delegate.sh new <task>`
3. The commands run Vibe unattended with auto-approval and print JSON. Read the final assistant message and summarize it instead of dumping raw JSON.
4. To inspect the latest reply without sending another turn, run `scripts/vibe-delegate.sh reply`.
5. Do not run this alongside another delegated agent against the same working tree. Check for an in-flight `opencode` or `vibe` process first when needed.

Task to delegate: $ARGUMENTS

Relay Vibe's actual answer in your own words. If Vibe asks a clarifying question, show that question to the user instead of guessing.
