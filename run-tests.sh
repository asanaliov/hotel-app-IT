#!/usr/bin/env bash
# Runs the test suite and prints ONLY the fancy summary report.
# All build/restore/xUnit runner noise is hidden. Pass extra args straight
# through, e.g.  ./run-tests.sh --filter "FullyQualifiedName~ControllersTests"
set -uo pipefail

# .NET CLI: prefer 'dotnet' on PATH, else the Windows binary used in this repo.
DOTNET="$(command -v dotnet || true)"
[ -z "$DOTNET" ] && DOTNET="/mnt/c/Program Files/dotnet/dotnet.exe"

PROJ="hotel-app.Tests/hotel-app.Tests.csproj"
SUMMARY="TestOutput/test_summary.txt"
LOG="$(mktemp)"

# Stale-guard: remove the previous report so we never print an old one.
rm -f "$SUMMARY"

"$DOTNET" test "$PROJ" --logger "console;verbosity=quiet" "$@" >"$LOG" 2>&1
CODE=$?

if [ -f "$SUMMARY" ]; then
    cat "$SUMMARY"
else
    # No report produced -> almost always a build/restore failure. Show the log.
    echo "No test summary was produced (build or run failed). Full output:"
    echo "------------------------------------------------------------------"
    grep -E "error|Error|Failed" "$LOG" | head -40
fi

rm -f "$LOG"
exit $CODE
