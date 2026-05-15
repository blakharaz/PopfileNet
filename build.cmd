:; set -eo pipefail
:; SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" &>/dev/null && pwd)"
:; dotnet run --project "$SCRIPT_DIR/Build/_build.csproj" -- "$@"
:; exit $?

@ECHO OFF
powershell -ExecutionPolicy ByPass -NoProfile -File "%~dp0build.ps1" %*
