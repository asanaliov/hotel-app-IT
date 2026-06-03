# Runs the test suite and prints ONLY the fancy summary report.
# All build/restore/xUnit runner noise is hidden. Pass extra args straight
# through, e.g.  .\run-tests.ps1 --filter "FullyQualifiedName~ControllersTests"
#
# Usage (PowerShell, from the repo root):
#   .\run-tests.ps1
#   .\run-tests.ps1 --filter "FullyQualifiedName~ControllersTests"

$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# .NET CLI: prefer 'dotnet' on PATH, else the Windows binary used in this repo.
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (-not $dotnet) { $dotnet = 'C:\Program Files\dotnet\dotnet.exe' }

$proj    = 'hotel-app.Tests/hotel-app.Tests.csproj'
$summary = 'TestOutput/test_summary.txt'
$log     = [System.IO.Path]::GetTempFileName()

# Stale-guard: remove the previous report so we never print an old one.
if (Test-Path $summary) { Remove-Item $summary -Force }

& $dotnet test $proj --logger "console;verbosity=quiet" @args *> $log
$code = $LASTEXITCODE

if (Test-Path $summary) {
    # Read explicitly as UTF-8 (Windows PowerShell 5.1 otherwise decodes as ANSI
    # and mangles the box-drawing characters).
    $full = (Resolve-Path $summary).Path
    Write-Host ([System.IO.File]::ReadAllText($full, [System.Text.Encoding]::UTF8))
}
else {
    Write-Host "No test summary was produced (build or run failed). Full output:"
    Write-Host "------------------------------------------------------------------"
    Select-String -Path $log -Pattern 'error|Error|Failed' | Select-Object -First 40 | ForEach-Object { $_.Line }
}

Remove-Item $log -Force -ErrorAction SilentlyContinue
exit $code
