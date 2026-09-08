#!/usr/bin/env bash
# Helpers for driving Mistral Vibe (the `vibe` CLI, ~/.local/bin/vibe) from
# the shell - same purpose as scripts/opencode-delegate.sh, different tool.
set -euo pipefail

VIBE_HOME=${VIBE_HOME:-$HOME/.vibe}
SESSION_LOG_DIR="$VIBE_HOME/logs/session"

usage() {
    cat <<'EOF'
Usage:
  vibe-delegate.sh list                       List recent session log dirs (oldest first; last line is newest)
  vibe-delegate.sh latest                     Print the session_id (UUID) of the most recent session
  vibe-delegate.sh send MSG…                  Continue the most recent session (global, not repo-scoped) with MSG
  vibe-delegate.sh send --session ID MSG…     Resume a specific session (ID = session_id UUID, not the log dir name)
  vibe-delegate.sh reply                      Print the last assistant message from the most recent session (read-only)
  vibe-delegate.sh new MSG…                   Start a brand new session with MSG

Env:
  VIBE_AGENT     --agent passed to `vibe` (default: unset, Vibe's own default_agent config)
  VIBE_HOME      Vibe's home dir (default: ~/.vibe) - only affects reading session logs here,
                 `vibe` itself also honors this env var for its own state.
EOF
}

vibe_list() {
    ls -1 "$SESSION_LOG_DIR" 2>/dev/null | sort
}

# Prints the session_id (UUID) from the newest log dir's meta.json - this
# is what --resume expects, NOT the log directory name.
vibe_latest_session_id() {
    local dir
    dir=$(vibe_list | tail -1)
    [ -n "$dir" ] || return 1
    jq -r '.session_id' "$SESSION_LOG_DIR/$dir/meta.json"
}

# $@ = message words
vibe_send() {
    local -a extra=(--trust --auto-approve --output json -c)
    [ -n "${VIBE_AGENT:-}" ] && extra+=(--agent "$VIBE_AGENT")
    vibe -p "$*" "${extra[@]}"
}

# $1 = session_id (UUID), rest = message words
vibe_send_session() {
    local session=$1; shift
    local -a extra=(--trust --auto-approve --output json --resume "$session")
    [ -n "${VIBE_AGENT:-}" ] && extra+=(--agent "$VIBE_AGENT")
    vibe -p "$*" "${extra[@]}"
}

vibe_new() {
    local -a extra=(--trust --auto-approve --output json)
    [ -n "${VIBE_AGENT:-}" ] && extra+=(--agent "$VIBE_AGENT")
    vibe -p "$*" "${extra[@]}"
}

# Read-only: the last assistant message from the newest session's log.
vibe_last_reply() {
    local dir
    dir=$(vibe_list | tail -1)
    [ -n "$dir" ] || { echo "vibe-delegate.sh: no session logs found" >&2; return 1; }
    local log="$SESSION_LOG_DIR/$dir/messages.jsonl"
    [ -f "$log" ] || { echo "vibe-delegate.sh: no messages.jsonl in $dir" >&2; return 1; }
    jq -rs '[.[] | select(.role == "assistant")] | last | .content' "$log"
}

cmd=${1:-}
[ $# -gt 0 ] && shift || true

case "$cmd" in
    list)
        vibe_list
        ;;
    latest)
        vibe_latest_session_id
        ;;
    send)
        if [ "${1:-}" = "--session" ]; then
            session=$2; shift 2
            [ $# -gt 0 ] || { echo "vibe-delegate.sh: no message given" >&2; exit 1; }
            vibe_send_session "$session" "$@"
        else
            [ $# -gt 0 ] || { echo "vibe-delegate.sh: no message given" >&2; exit 1; }
            vibe_send "$@"
        fi
        ;;
    reply)
        vibe_last_reply
        ;;
    new)
        [ $# -gt 0 ] || { echo "vibe-delegate.sh: no message given" >&2; exit 1; }
        vibe_new "$@"
        ;;
    *)
        usage
        exit 1
        ;;
esac
