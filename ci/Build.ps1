param(
    [string]$GamePath = 'E:\Games\SteamLibrary\steamapps\common\Valheim',
    [string]$OutputDirectory = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts'),
    [string]$AutoPickerPath = ''
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    dotnet build AnimalFeedGuard.csproj -c Release "-p:GamePath=$GamePath"
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed' }
    $checkArgs = @('run', '--project', 'tests/checks', '-c', 'Release', '--', $GamePath)
    if ($AutoPickerPath) { $checkArgs += $AutoPickerPath }
    & dotnet @checkArgs
    if ($LASTEXITCODE -ne 0) { throw 'Checks failed' }
    & (Join-Path $PSScriptRoot 'Verify-References.ps1') -GamePath $GamePath -PluginPath (Join-Path $root 'bin/Release/net48/AnimalFeedGuard.dll')
    & (Join-Path $PSScriptRoot 'Package.ps1') -OutputDirectory $OutputDirectory
} finally { Pop-Location }
