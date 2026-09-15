#!/usr/bin/env bash
# Helpers for driving the raw `codex` CLI from the shell (the codex-cli-runtime plugin
# wrapper in ~/.claude/plugins/cache/openai-codex is a separate, heavier mechanism -
# this script talks to `codex exec` directly, mirroring scripts/opencode-delegate.sh's
# shape for the sibling `opencode` CLI). See .claude/commands/codex-cli.md for the Claude
# Code slash command built on top of this.
#
# Unlike opencode, codex has no scriptable "session list" - `codex exec resume --last`
# already resolves "most recent session for this cwd" internally (see its own --all
# flag doc: "disables cwd filtering", i.e. the default IS cwd-filtered), so `send` needs
# no separate lookup step the way opencode-delegate.sh's `latest` did.
#
# ponytail: no lockfile/concurrency guard, same tradeoff opencode-delegate.sh accepts -
# two callers racing to `send` at once will interleave in the same session like two
# humans typing into the same chat. Fine for one operator.
set -euo pipefail

usage() {
    cat <<'EOF'
Usage:
  codex-delegate.sh new [--model M] MSG…       Start a brand new non-interactive task with MSG
  codex-delegate.sh send [--model M] MSG…      Continue the most recent session for this repo with MSG
  codex-delegate.sh reply                      Best-effort: print the last assistant message stored
                                                in the most recent session's rollout file for this repo
                                                (no new turn - reads history, like opencode-delegate's reply)

Env:
  CODEX_MODEL       --model passed to `codex exec` (default: unset, config/profile default)
  CODEX_SANDBOX     -s/--sandbox passed to `codex exec` (default: workspace-write) - `codex exec`
                     has no -a/--ask-for-approval of its own (that flag only exists on the
                     interactive `codex`/`codex resume`); the sandbox policy IS the approval
                     control for non-interactive exec.
EOF
}

# $@ = MSG words. Runs `codex exec [resume --last] ...`, streaming raw --json events to
# stdout as they arrive (same "eat the event stream yourself" contract as
# opencode-delegate.sh's `oc_send`/`oc_new`), then prints the clean final assistant
# message on its own marked section once codex exits - via -o to a temp file rather
# than opencode's export/jq route, so there is no read-after-write race to retry.
cx_run() {
    local resume=$1; shift
    [ $# -gt 0 ] || { echo "codex-delegate.sh: no message given" >&2; exit 1; }

    local -a extra=()
    if [ -n "${CODEX_MODEL:-}" ]; then extra+=(--model "$CODEX_MODEL"); fi

    local last_message_file
    last_message_file=$(mktemp)
    trap 'rm -f "$last_message_file"' RETURN

    if [ "$resume" = "1" ]; then
        # `codex exec resume` has no -s/--sandbox of its own - the sandbox policy is fixed
        # at session creation and carried over, not re-specifiable per resumed turn.
        codex exec resume --last --json -o "$last_message_file" "${extra[@]}" -- "$*"
    else
        codex exec --json -o "$last_message_file" -s "${CODEX_SANDBOX:-workspace-write}" "${extra[@]}" -- "$*"
    fi

    echo "=== final message ==="
    cat "$last_message_file"
}

# Newest rollout file (by mtime) whose first-line session_meta.cwd matches this repo's
# working directory - the same file `codex exec resume --last` itself would pick.
cx_latest_session_file() {
    local cwd; cwd=$(pwd)
    local f
    for f in $(find "$HOME/.codex/sessions" -type f -name 'rollout-*.jsonl' -printf '%T@ %p\n' 2>/dev/null | sort -rn | cut -d' ' -f2-); do
        if head -1 "$f" 2>/dev/null | grep -qF "\"cwd\":\"$cwd\""; then
            echo "$f"
            return 0
        fi
    done
    return 1
}

# Best-effort: codex has no "export/read a past session" command like `opencode export`,
# so this greps the raw JSONL rollout file directly for the last assistant text this repo
# actually produced. Rollout event shapes can change across codex-cli releases - if this
# stops matching, `jq 'select(.type=="response_item" and .payload.type=="message" and
# .payload.role=="assistant")' <file>` against a real rollout file is the place to look.
cx_last_reply() {
    local file
    file=$(cx_latest_session_file) || { echo "codex-delegate.sh: no session found for $(pwd)" >&2; return 1; }
    python3 -c '
import json, sys
last = None
with open(sys.argv[1]) as fh:
    for line in fh:
        try:
            event = json.loads(line)
        except ValueError:
            continue
        payload = event.get("payload", {})
        if payload.get("type") == "message" and payload.get("role") == "assistant":
            parts = payload.get("content", [])
            text = "\n".join(p.get("text", "") for p in parts if isinstance(p, dict))
            if text.strip():
                last = text
if last is None:
    sys.exit(1)
print(last)
' "$file"
}

cmd=${1:-}
[ $# -gt 0 ] && shift || true

case "$cmd" in
    new)
        if [ "${1:-}" = "--model" ]; then CODEX_MODEL=$2; shift 2; fi
        cx_run 0 "$@"
        ;;
    send)
        if [ "${1:-}" = "--model" ]; then CODEX_MODEL=$2; shift 2; fi
        cx_run 1 "$@"
        ;;
    reply)
        cx_last_reply
        ;;
    *)
        usage
        exit 1
        ;;
esac
