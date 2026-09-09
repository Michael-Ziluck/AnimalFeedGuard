param(
    [string]$GamePath = 'E:\Games\SteamLibrary\steamapps\common\Valheim',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'artifacts'),
    [string]$AutoPickerPath = ''
)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet build AnimalFeedGuard.csproj -c Release "-p:GamePath=$GamePath"
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed' }
    $checkArgs = @('run', '--project', 'tests/Checks', '-c', 'Release', '--', $GamePath)
    if ($AutoPickerPath) { $checkArgs += $AutoPickerPath }
    & dotnet @checkArgs
    if ($LASTEXITCODE -ne 0) { throw 'Checks failed' }
    & (Join-Path $PSScriptRoot 'Package.ps1') -OutputDirectory $OutputDirectory
} finally { Pop-Location }
