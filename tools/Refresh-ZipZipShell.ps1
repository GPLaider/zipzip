[CmdletBinding()]
param(
    [string]$Configuration = "Debug",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Path $PSScriptRoot -Parent
$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

$shellProject = Join-Path $repoRoot "src\ZipZip.ShellExtension\ZipZip.ShellExtension.csproj"
$appExe = Join-Path $repoRoot "src\ZipZip.App\bin\$Platform\$Configuration\net8.0-windows10.0.19041.0\ZipZip.App.exe"
$shellExe = Join-Path $repoRoot "src\ZipZip.ShellExtension\bin\$Platform\$Configuration\net8.0-windows10.0.19041.0\ZipZip.ShellExtension.exe"

Write-Host "Refreshing ZipZip shell helper..."

Get-Process ZipZip.ShellExtension -ErrorAction SilentlyContinue | Stop-Process -Force

& $msbuild $shellProject /t:Build /p:Configuration=$Configuration /p:Platform=$Platform /nologo | Out-Host

if (-not (Test-Path -LiteralPath $appExe -PathType Leaf)) {
    throw "ZipZip app executable not found: $appExe"
}

if (-not (Test-Path -LiteralPath $shellExe -PathType Leaf)) {
    throw "ZipZip shell helper not found: $shellExe"
}

& $shellExe register $appExe

$openArchive = (Get-Item "Registry::HKEY_CURRENT_USER\Software\Classes\SystemFileAssociations\.zip\shell\ZipZipOpen\command").GetValue("")
$extractHere = (Get-Item "Registry::HKEY_CURRENT_USER\Software\Classes\SystemFileAssociations\.zip\shell\ZipZipExtractHere\command").GetValue("")
$extractNewFolder = (Get-Item "Registry::HKEY_CURRENT_USER\Software\Classes\SystemFileAssociations\.zip\shell\ZipZipExtractNewFolder\command").GetValue("")
$compressDialogModel = (Get-Item "Registry::HKEY_CURRENT_USER\Software\Classes\AllFilesystemObjects\shell\ZipZipCompressDialog").GetValue("MultiSelectModel")

Write-Host ""
Write-Host "Registered commands:"
Write-Host "  Open: $openArchive"
Write-Host "  Extract here: $extractHere"
Write-Host "  Extract to new folder: $extractNewFolder"
Write-Host "  Compress multi-select model: $compressDialogModel"
