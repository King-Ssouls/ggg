param([switch]$WithAgent)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'SupportTicketsApi.csproj'
$programFolder = if ($env:ProgramW6432) { $env:ProgramW6432 } else { $env:ProgramFiles }
$dotnet = Join-Path $programFolder 'dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet = 'dotnet' }

# Создаёт случайный ключ и сохраняет его вне проекта, не выводя значение на экран
$bytes = New-Object byte[] 48
$random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try { $random.GetBytes($bytes) } finally { $random.Dispose() }
$key = [Convert]::ToBase64String($bytes)
& $dotnet user-secrets set 'Jwt:Secret' $key --project $project
if ($LASTEXITCODE -ne 0) { throw 'Не удалось сохранить JWT в User Secrets' }
$key = $null

# По желанию добавляет пароль агента для проверки роли Agent
if ($WithAgent) {
    $securePassword = Read-Host 'Придумайте пароль агента от 8 до 100 символов' -AsSecureString
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try {
        $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
        if ($password.Length -lt 8 -or $password.Length -gt 100) {
            throw 'Пароль агента должен содержать от 8 до 100 символов'
        }
        & $dotnet user-secrets set 'SeedAgent:Password' $password --project $project
        if ($LASTEXITCODE -ne 0) { throw 'Не удалось сохранить пароль агента' }
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
        $password = $null
    }
}

Write-Host 'Готово — настройки сохранены в User Secrets вне папки проекта'
Write-Host 'Теперь запустите .\run.ps1'
