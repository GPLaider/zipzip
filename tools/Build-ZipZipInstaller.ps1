param(
    [string]$Version = '0.1.0-preview',
    [string]$SignThumbprint = '',
    [string]$ExportPublicCertPath = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$appProject = Join-Path $repoRoot 'src\ZipZip.App\ZipZip.App.csproj'
$shellProject = Join-Path $repoRoot 'src\ZipZip.ShellExtension\ZipZip.ShellExtension.csproj'
$appBuildOutput = Join-Path $repoRoot 'src\ZipZip.App\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64'
$shellPublish = Join-Path $repoRoot 'artifacts\publish\ZipZip.ShellExtension'
$installerRoot = Join-Path $repoRoot 'artifacts\installer'
$stageDir = Join-Path $installerRoot 'stage'
$outputDir = Join-Path $installerRoot 'dist'
$issPath = Join-Path $repoRoot 'installer\ZipZip.iss'
$installerSupportDir = Join-Path $repoRoot 'installer'
$signScriptPath = Join-Path $repoRoot 'tools\Sign-ZipZipBinary.ps1'
$sevenZipDir = 'C:\Program Files\7-Zip'
$isccCandidates = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe'
)

$msbuildPath = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe'
if (-not (Test-Path -LiteralPath $msbuildPath -PathType Leaf)) {
    throw "MSBuild를 찾을 수 없습니다: $msbuildPath"
}

$isccPath = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (-not $isccPath) {
    throw 'ISCC.exe를 찾을 수 없습니다. Inno Setup 6 설치를 확인해 주세요.'
}

if (-not (Test-Path -LiteralPath $sevenZipDir -PathType Container)) {
    throw "7-Zip 설치 폴더를 찾을 수 없습니다: $sevenZipDir"
}

Remove-Item -LiteralPath $installerRoot -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $shellPublish -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $shellPublish -Force | Out-Null
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

& $msbuildPath $appProject /restore /t:Build /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64 /p:SelfContained=true /p:WindowsAppSDKSelfContained=true /nologo
if ($LASTEXITCODE -ne 0) {
    throw "ZipZip.App 빌드가 실패했습니다. 종료 코드: $LASTEXITCODE"
}

$appExecutable = Join-Path $appBuildOutput 'ZipZip.App.exe'
if (-not (Test-Path -LiteralPath $appExecutable -PathType Leaf)) {
    throw "ZipZip.App 실행 파일을 찾을 수 없습니다: $appExecutable"
}

& dotnet publish $shellProject -c Release -r win-x64 --self-contained true -p:UseSharedCompilation=false -o $shellPublish
if ($LASTEXITCODE -ne 0) {
    throw "ZipZip.ShellExtension publish가 실패했습니다. 종료 코드: $LASTEXITCODE"
}

Copy-Item -Path (Join-Path $appBuildOutput '*') -Destination $stageDir -Recurse -Force
Copy-Item -Path (Join-Path $shellPublish '*') -Destination $stageDir -Recurse -Force

New-Item -ItemType Directory -Path (Join-Path $stageDir 'InstallerSupport') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $installerSupportDir 'Register-ZipZipFileAssociations.ps1') -Destination (Join-Path $stageDir 'InstallerSupport\Register-ZipZipFileAssociations.ps1') -Force
Copy-Item -LiteralPath (Join-Path $installerSupportDir 'Unregister-ZipZipFileAssociations.ps1') -Destination (Join-Path $stageDir 'InstallerSupport\Unregister-ZipZipFileAssociations.ps1') -Force

Copy-Item -LiteralPath (Join-Path $sevenZipDir '7z.exe') -Destination (Join-Path $stageDir '7z.exe') -Force
Copy-Item -LiteralPath (Join-Path $sevenZipDir '7z.dll') -Destination (Join-Path $stageDir '7z.dll') -Force

New-Item -ItemType Directory -Path (Join-Path $stageDir 'ThirdParty\7-Zip') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $sevenZipDir 'License.txt') -Destination (Join-Path $stageDir 'ThirdParty\7-Zip\License.txt') -Force
Copy-Item -LiteralPath (Join-Path $sevenZipDir 'History.txt') -Destination (Join-Path $stageDir 'ThirdParty\7-Zip\History.txt') -Force

$outputBaseFilename = "ZipZip-Setup-$($Version -replace '[^0-9A-Za-z._-]', '-')"
& $isccPath "/DStageDir=$stageDir" "/DOutputDir=$outputDir" "/DAppVersion=$Version" "/DOutputBaseFilename=$outputBaseFilename" $issPath
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup 컴파일이 실패했습니다. 종료 코드: $LASTEXITCODE"
}

$setupExe = Join-Path $outputDir "$outputBaseFilename.exe"
if (-not (Test-Path -LiteralPath $setupExe -PathType Leaf)) {
    throw "생성된 설치 파일을 찾을 수 없습니다: $setupExe"
}

if (-not [string]::IsNullOrWhiteSpace($SignThumbprint) -or -not [string]::IsNullOrWhiteSpace($ExportPublicCertPath)) {
    $signArguments = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $signScriptPath,
        '-FilePath', $setupExe
    )

    if (-not [string]::IsNullOrWhiteSpace($SignThumbprint)) {
        $signArguments += @('-Thumbprint', $SignThumbprint)
    }

    if (-not [string]::IsNullOrWhiteSpace($ExportPublicCertPath)) {
        $signArguments += @('-ExportPublicCertPath', $ExportPublicCertPath)
    }

    & powershell @signArguments
    if ($LASTEXITCODE -ne 0) {
        throw "설치 파일 서명이 실패했습니다. 종료 코드: $LASTEXITCODE"
    }
}

Write-Host ''
Write-Host '설치 파일 생성 완료:'
Get-ChildItem -LiteralPath $outputDir -Filter '*.exe' | Select-Object FullName, Length, LastWriteTime
