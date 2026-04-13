param(
    [string]$ShellHelperPath = (Join-Path $PSScriptRoot '..\src\ZipZip.ShellExtension\bin\x64\Debug\net8.0-windows10.0.19041.0\ZipZip.ShellExtension.exe')
)

$ErrorActionPreference = 'Stop'
$shellHelper = [System.IO.Path]::GetFullPath($ShellHelperPath)

if (-not (Test-Path -LiteralPath $shellHelper -PathType Leaf)) {
    throw "ZipZip 셸 도우미 파일을 찾을 수 없습니다: $shellHelper"
}

& $shellHelper unregister
