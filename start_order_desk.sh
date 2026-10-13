#!/usr/bin/env bash
# Build and launch Order Desk (sample thick client).
#
#   ./start_order_desk.sh                      build, then launch
#   ./start_order_desk.sh --no-build           launch the existing app/bin/OrderDesk.exe
#   ./start_order_desk.sh --password secret    sign-in password for this run (else ORDERDESK_PASSWORD,
#                                              else the default "orderdesk")
#
# Windows (Git Bash / MSYS / Cygwin) runs the exe directly; Linux / macOS run it with Mono, for a look at
# the app only (desktop test automation needs Windows). The build takes about a second, so it always runs.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
EXE="${HERE}/app/bin/OrderDesk.exe"
build=1

while [[ $# -gt 0 ]]; do
  case "$1" in
    --no-build) build=0; shift ;;
    --password) export ORDERDESK_PASSWORD="${2:?--password needs a value}"; shift 2 ;;
    -h|--help) sed -n '2,10p' "${BASH_SOURCE[0]}"; exit 0 ;;
    *) echo "Unknown option: $1 (use --help)" >&2; exit 2 ;;
  esac
done

if [[ "${build}" == 1 ]]; then
  bash "${HERE}/app/build.sh"
fi
if [[ ! -f "${EXE}" ]]; then
  echo "[start] ${EXE} not found - run without --no-build." >&2
  exit 1
fi

case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) "${EXE}" & ;;
  *)
    if ! command -v mono >/dev/null 2>&1; then
      echo "[start] Needs Windows, or Mono on Linux / macOS: https://www.mono-project.com/download/" >&2
      exit 1
    fi
    mono "${EXE}" &
    ;;
esac
disown 2>/dev/null || true
echo "[start] Order Desk launched. Sign in with any user name; password: \$ORDERDESK_PASSWORD or \"orderdesk\"."
