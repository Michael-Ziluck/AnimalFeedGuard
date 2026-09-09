param(
    [string]$PackageFile = '',
    [string]$Repository = 'https://thunderstore.io',
    [string]$TeamName = 'REPLACE_WITH_THUNDERSTORE_TEAM_NAME',
    [string]$Community = 'valheim',
    [string]$PackagePageUrl = 'REPLACE_WITH_THUNDERSTORE_PACKAGE_PAGE_URL'
)
$ErrorActionPreference = 'Stop'
if ($TeamName -like 'REPLACE_*' -or $PackagePageUrl -like 'REPLACE_*') {
    throw 'Replace TeamName and PackagePageUrl placeholders in Publish.ps1 or pass them as parameters before publishing.'
}
$token = [Environment]::GetEnvironmentVariable('THUNDERSTORE_API_TOKEN', 'User')
if ([string]::IsNullOrWhiteSpace($token)) { throw 'THUNDERSTORE_API_TOKEN is not set for this Windows user.' }
if (!$PackageFile) {
    $PackageFile = Get-ChildItem (Join-Path $PSScriptRoot 'artifacts') -Filter 'AnimalFeedGuard-*.zip' | Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
}
if (!$PackageFile -or !(Test-Path -LiteralPath $PackageFile)) { throw 'Build a Thunderstore ZIP first, or pass -PackageFile.' }
$tcli = Get-Command tcli -ErrorAction SilentlyContinue
if (!$tcli) { throw 'Thunderstore CLI (tcli) is required. Install it with: dotnet tool install -g tcli' }
$env:TCLI_AUTH_TOKEN = $token
& $tcli.Source publish --file $PackageFile --repository $Repository --package-namespace $TeamName --package-name 'AnimalFeedGuard' --package-version '1.0.0'
if ($LASTEXITCODE -ne 0) { throw "Thunderstore publish failed with exit code $LASTEXITCODE." }
Write-Host "Published AnimalFeedGuard to $Community. Package page: $PackagePageUrl"
