$ErrorActionPreference = "Stop"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1

dotnet run --project "$PSScriptRoot/Build/_build.csproj" -- $args
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
