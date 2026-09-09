param(
    [string]$Version = '0.2.2-preview',
    [string]$Dotnet = 'dotnet',
    [string]$ArtifactsRoot = (Join-Path $PSScriptRoot '..\artifacts\release'),
    [string]$SevenZipDirectory = 'C:\Program Files\7-Zip',
    [string]$Iscc = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    [string]$SignThumbprint = ''
)
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?$') { throw 'Invalid version' }
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$ArtifactsRoot = [IO.Path]::GetFullPath($ArtifactsRoot)
if (-not (Test-Path -LiteralPath $Iscc)) { throw "Missing Inno Setup compiler: $Iscc" }
$build = Join-Path $ArtifactsRoot ('build-' + $Version)
& (Join-Path $PSScriptRoot 'Build-ZipZip.ps1') -Dotnet $Dotnet -ArtifactsRoot $build -SevenZipDirectory $SevenZipDirectory -Configuration Release
$stage = Join-Path $build 'run'
$support = Join-Path $stage 'InstallerSupport'
$licenses = Join-Path $stage 'ThirdParty\7-Zip'
New-Item -ItemType Directory -Path $support,$licenses -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'installer\Register-ZipZipFileAssociations.ps1'),(Join-Path $repo 'installer\Unregister-ZipZipFileAssociations.ps1') -Destination $support -Force
foreach ($name in @('License.txt','History.txt')) {
    $source = Join-Path $SevenZipDirectory $name
    if (-not (Test-Path -LiteralPath $source)) { $source = Join-Path $SevenZipDirectory "ThirdParty\7-Zip\$name" }
    Copy-Item -LiteralPath $source -Destination $licenses -Force
}
$dist = Join-Path $ArtifactsRoot 'dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$numericVersion = ($Version -split '-')[0] + '.0'
& $Iscc "/DStageDir=$stage" "/DOutputDir=$dist" "/DAppVersion=$Version" "/DNumericVersion=$numericVersion" "/DOutputBaseFilename=ZipZip-Setup-$Version" (Join-Path $repo 'installer\ZipZip.iss')
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed: $LASTEXITCODE" }
$setup = Join-Path $dist "ZipZip-Setup-$Version.exe"
if ($SignThumbprint) {
    & (Join-Path $PSScriptRoot 'Sign-ZipZipBinary.ps1') -FilePath $setup -Thumbprint $SignThumbprint
}
$hash = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $dist 'SHA256SUMS.txt'), "$hash  $([IO.Path]::GetFileName($setup))" + [Environment]::NewLine)
Write-Output "Installer: $setup"
