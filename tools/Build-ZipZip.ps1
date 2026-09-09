param(
    [string]$Dotnet = 'dotnet',
    [string]$ArtifactsRoot = (Join-Path $PSScriptRoot '..\artifacts\build'),
    [string]$SevenZipDirectory = 'C:\Program Files\7-Zip',
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug'
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$ArtifactsRoot = [IO.Path]::GetFullPath($ArtifactsRoot)
$SevenZipDirectory = [IO.Path]::GetFullPath($SevenZipDirectory)
foreach ($name in @('7z.exe', '7z.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $SevenZipDirectory $name))) { throw "Missing $name in $SevenZipDirectory" }
}
function Invoke-Dotnet([string[]]$Arguments) {
    if ($Arguments[0] -eq 'build') { $Arguments += @('-c', $Configuration) }
    & $Dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
}

$buildRoot = Join-Path $ArtifactsRoot 'compiled'
$testRoot = Join-Path $ArtifactsRoot 'tests'
Invoke-Dotnet @('build', (Join-Path $repo 'src\ZipZip.App\ZipZip.App.csproj'), '--artifacts-path', $buildRoot, '-p:UseSharedCompilation=false', '-p:Platform=x64', '-p:RuntimeIdentifier=win-x64', '-p:SelfContained=true', '-p:WindowsAppSDKSelfContained=true')
Invoke-Dotnet @('build', (Join-Path $repo 'src\ZipZip.ShellExtension\ZipZip.ShellExtension.csproj'), '--artifacts-path', $buildRoot, '-p:UseSharedCompilation=false', '-p:RuntimeIdentifier=win-x64', '-p:SelfContained=true')
foreach ($project in @('ZipZip.Application.Tests', 'ZipZip.ArchiveAdapters.Tests')) {
    Invoke-Dotnet @('test', (Join-Path $repo "tests\$project\$project.csproj"), '--artifacts-path', $testRoot, '-p:UseSharedCompilation=false')
}
Invoke-Dotnet @('build', (Join-Path $repo 'tests\ZipZip.Smoke\ZipZip.Smoke.csproj'), '--artifacts-path', $testRoot, '-p:UseSharedCompilation=false')
Invoke-Dotnet @((Join-Path $testRoot "bin\ZipZip.Smoke\$($Configuration.ToLowerInvariant())\ZipZip.Smoke.dll"), (Join-Path $SevenZipDirectory '7z.exe'), (Join-Path $ArtifactsRoot 'smoke'))
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Test-ZipZipIcons.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Icon verification failed' }

$runDirectory = Join-Path $ArtifactsRoot 'run'
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $buildRoot "bin\ZipZip.App\$($Configuration.ToLowerInvariant())_win-x64\*") -Destination $runDirectory -Recurse -Force
Copy-Item -Path (Join-Path $buildRoot "bin\ZipZip.ShellExtension\$($Configuration.ToLowerInvariant())_win-x64\*") -Destination $runDirectory -Recurse -Force
Copy-Item -LiteralPath (Join-Path $SevenZipDirectory '7z.exe'),(Join-Path $SevenZipDirectory '7z.dll') -Destination $runDirectory -Force
Write-Output "Verified development build: $(Join-Path $runDirectory 'ZipZip.App.exe')"
