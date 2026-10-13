#!/usr/bin/env bash
# Build Order Desk (sample thick client). Output: app/bin/OrderDesk.exe
#   Windows (Git Bash / MSYS / Cygwin): the C# compiler that ships with Windows (.NET Framework 4.x)
#   Linux / macOS: Mono's compiler (mcs or csc), if Mono is installed - for a look at the app only;
#   UI Automation, and so desktop test automation, needs Windows.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC="${HERE}/OrderDesk.cs"
OUT="${HERE}/bin/OrderDesk.exe"
REFS=(System.dll System.Drawing.dll System.Windows.Forms.dll)
mkdir -p "${HERE}/bin"

case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*)
    win="${WINDIR:-${SYSTEMROOT:-C:/Windows}}"
    csc=""
    for dir in Framework64 Framework; do
      candidate="$(cygpath -u "${win}" 2>/dev/null || echo "${win}")/Microsoft.NET/${dir}/v4.0.30319/csc.exe"
      if [[ -x "${candidate}" ]]; then csc="${candidate}"; break; fi
    done
    if [[ -z "${csc}" ]]; then
      echo "[build] .NET Framework 4.x C# compiler not found." >&2
      exit 1
    fi
    to_win() { cygpath -w "$1" 2>/dev/null || echo "$1"; }
    refs=()
    for ref in "${REFS[@]}"; do refs+=("/r:${ref}"); done
    MSYS2_ARG_CONV_EXCL='*' "${csc}" /nologo /target:winexe /optimize+ "/out:$(to_win "${OUT}")" "${refs[@]}" "$(to_win "${SRC}")"
    ;;
  *)
    compiler="$(command -v mcs || command -v csc || true)"
    if [[ -z "${compiler}" ]]; then
      echo "[build] Needs Windows, or Mono (mcs / csc) on Linux / macOS: https://www.mono-project.com/download/" >&2
      exit 1
    fi
    refs=()
    for ref in "${REFS[@]}"; do refs+=("-r:${ref}"); done
    "${compiler}" -nologo -target:winexe -optimize+ "-out:${OUT}" "${refs[@]}" "${SRC}"
    ;;
esac
echo "[build] ${OUT}"
