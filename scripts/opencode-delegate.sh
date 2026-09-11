#!/usr/bin/env bash
# Helpers for driving an OpenCode session from the shell: find the
# session OpenCode was last working in on this repo, send it a task
# non-interactively (appending to that same session - not forking a new
# one), and read back its last reply. See .claude/commands/opencode.md
# for the Claude Code slash command built on top of this.
#
# ponytail: no lockfile/concurrency guard - two callers racing to
# "latest" and sending at once will interleave in the same session like
# two humans typing into the same chat. Fine for one operator; add a
# lock file under scripts/ if this ever gets called from parallel jobs.
set -euo pipefail

usage() {
    cat <<'EOF'
Usage:
  opencode-delegate.sh list                      List sessions for this repo (newest first)
  opencode-delegate.sh latest                     Print the most recently updated session ID
  opencode-delegate.sh send [--session ID] MSG…   Continue that session (default: latest) with MSG
  opencode-delegate.sh reply [--session ID]       Print the last assistant text reply from that session
  opencode-delegate.sh new MSG…                   Start a brand new session with MSG

Env:
  OPENCODE_AGENT   --agent passed to `opencode run` (default: unset, OpenCode's own default)
  OPENCODE_MODEL   --model passed to `opencode run` (default: unset, session/OpenCode default)
EOF
}

# First column of the newest session row from `opencode session list`
# (header + separator are the first two lines; rows are already newest-first).
oc_latest_session_id() {
    opencode session list 2>/dev/null | awk 'NR==3 {print $1; exit}'
}

oc_list() {
    opencode session list 2>/dev/null
}

# $1 = session id, rest = message words
oc_send() {
    local session=$1; shift
    [ $# -gt 0 ] || { echo "opencode-delegate.sh: no message given" >&2; exit 1; }
    local -a extra=()
    [ -n "${OPENCODE_AGENT:-}" ] && extra+=(--agent "$OPENCODE_AGENT")
    [ -n "${OPENCODE_MODEL:-}" ] && extra+=(--model "$OPENCODE_MODEL")
    # --session continues that exact session (no --continue needed, and
    # deliberately no --fork: the point is to land in the SAME session,
    # not branch off it).
    opencode run --session "$session" --format json "${extra[@]}" -- "$@"
}

oc_new() {
    local -a extra=()
    [ -n "${OPENCODE_AGENT:-}" ] && extra+=(--agent "$OPENCODE_AGENT")
    [ -n "${OPENCODE_MODEL:-}" ] && extra+=(--model "$OPENCODE_MODEL")
    opencode run --format json "${extra[@]}" -- "$@"
}

# $1 = session id. Exports the session (read-only, adds no turn) and
# prints the last assistant message's concatenated text parts.
#
# ponytail: exporting a session OpenCode is actively writing to (e.g.
# right after `send`, before it has finished replying) can race and
# hand back a torn/incomplete JSON document - jq then fails to parse.
# No retry/wait loop here; on that error just re-run `reply` once
# OpenCode looks idle (`opencode session list`'s Updated column stops
# moving), rather than treating a jq parse error as "no reply".
oc_last_reply() {
    local session=$1
    if ! opencode export "$session" 2>/dev/null | jq -r '
        [.messages[] | select(.info.role == "assistant")] | last
        | (.parts // [])
        | map(select(.type == "text") | .text)
        | join("\n")
    '; then
        echo "opencode-delegate.sh: export didn't parse - session $session is likely still mid-reply, try again shortly" >&2
        return 1
    fi
}

cmd=${1:-}
[ $# -gt 0 ] && shift || true

case "$cmd" in
    list)
        oc_list
        ;;
    latest)
        oc_latest_session_id
        ;;
    send)
        session=""
        if [ "${1:-}" = "--session" ]; then
            session=$2; shift 2
        else
            session=$(oc_latest_session_id)
        fi
        [ -n "$session" ] || { echo "opencode-delegate.sh: no session found - use 'new' to start one" >&2; exit 1; }
        oc_send "$session" "$@"
        ;;
    reply)
        session=""
        if [ "${1:-}" = "--session" ]; then
            session=$2; shift 2
        else
            session=$(oc_latest_session_id)
        fi
        [ -n "$session" ] || { echo "opencode-delegate.sh: no session found" >&2; exit 1; }
        oc_last_reply "$session"
        ;;
    new)
        oc_new "$@"
        ;;
    *)
        usage
        exit 1
        ;;
esac
