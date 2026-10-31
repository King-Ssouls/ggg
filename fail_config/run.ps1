param([ValidateSet('http', 'Production')][string]$Profile = 'http')

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'SupportTicketsApi.csproj'
$programFolder = if ($env:ProgramW6432) { $env:ProgramW6432 } else { $env:ProgramFiles }
$dotnet = Join-Path $programFolder 'dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet = 'dotnet' }

# Запускает именно этот проект через 64-разрядный dotnet
& $dotnet run --project $project --launch-profile $Profile
exit $LASTEXITCODE
