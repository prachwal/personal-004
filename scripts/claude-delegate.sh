#!/usr/bin/env bash
# Run Claude Code non-interactively from another agent or shell.
# `--continue` is scoped by Claude to the current working directory.
set -euo pipefail

usage() {
    cat <<'EOF'
Usage:
  claude-delegate.sh send MSG...                  Continue the latest session in this repo
  claude-delegate.sh send --session ID MSG...     Continue a specific session
  claude-delegate.sh new MSG...                   Start a new session

Env:
  CLAUDE_AGENT           --agent passed to claude
  CLAUDE_MODEL           --model passed to claude
  CLAUDE_PERMISSION_MODE Permission mode (default: bypassPermissions)
EOF
}

claude_run() {
    local -a extra=(
        --print
        --output-format json
        --permission-mode "${CLAUDE_PERMISSION_MODE:-bypassPermissions}"
    )
    [ -n "${CLAUDE_AGENT:-}" ] && extra+=(--agent "$CLAUDE_AGENT")
    [ -n "${CLAUDE_MODEL:-}" ] && extra+=(--model "$CLAUDE_MODEL")
    claude "${extra[@]}" "$@"
}

cmd=${1:-}
[ $# -gt 0 ] && shift || true
[ $# -gt 0 ] || { usage; exit 1; }

case "$cmd" in
    send)
        session=""
        if [ "${1:-}" = "--session" ]; then
            [ $# -ge 2 ] || { echo "send --session requires a session ID" >&2; usage; exit 1; }
            session=$2
            shift 2
        fi
        [ $# -gt 0 ] || { echo "send requires a message" >&2; usage; exit 1; }
        if [ -n "$session" ]; then
            claude_run --resume "$session" "$*"
        else
            claude_run --continue "$*"
        fi
        ;;
    new)
        claude_run "$*"
        ;;
    *)
        usage
        exit 1
        ;;
esac
