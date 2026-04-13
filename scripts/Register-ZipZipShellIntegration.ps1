param(
    [string]$AppExePath = (Join-Path $PSScriptRoot '..\src\ZipZip.App\bin\x64\Debug\net8.0-windows10.0.19041.0\ZipZip.App.exe'),
    [string]$ShellHelperPath = (Join-Path $PSScriptRoot '..\src\ZipZip.ShellExtension\bin\x64\Debug\net8.0-windows10.0.19041.0\ZipZip.ShellExtension.exe')
)

$ErrorActionPreference = 'Stop'

$appExe = [System.IO.Path]::GetFullPath($AppExePath)
$shellHelper = [System.IO.Path]::GetFullPath($ShellHelperPath)

if (-not (Test-Path -LiteralPath $appExe -PathType Leaf)) {
    throw "ZipZip app executable not found: $appExe"
}

if (-not (Test-Path -LiteralPath $shellHelper -PathType Leaf)) {
    throw "ZipZip shell helper not found: $shellHelper"
}

& $shellHelper register $appExe
