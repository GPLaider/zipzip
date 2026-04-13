param(
    [Parameter(Mandatory = $true)]
    [string]$FilePath,

    [string]$Thumbprint = '',

    [string]$SignToolPath = 'C:\Program Files (x86)\Windows Kits\10\App Certification Kit\signtool.exe',

    [string]$TimestampUrl = '',

    [string]$ExportPublicCertPath = ''
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $FilePath -PathType Leaf)) {
    throw "서명할 파일을 찾을 수 없습니다: $FilePath"
}

if (-not (Test-Path -LiteralPath $SignToolPath -PathType Leaf)) {
    throw "signtool.exe를 찾을 수 없습니다: $SignToolPath"
}

if ([string]::IsNullOrWhiteSpace($Thumbprint)) {
    $cert = Get-ChildItem Cert:\CurrentUser\My |
        Where-Object {
            $_.Subject -eq 'CN=ZipZip Test Signing' -and
            ($_.EnhancedKeyUsageList.ObjectId -contains '1.3.6.1.5.5.7.3.3')
        } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

    if (-not $cert) {
        throw 'CN=ZipZip Test Signing 코드 서명 인증서를 찾을 수 없습니다.'
    }

    $Thumbprint = $cert.Thumbprint
}
else {
    $cert = Get-ChildItem Cert:\CurrentUser\My |
        Where-Object { $_.Thumbprint -eq $Thumbprint } |
        Select-Object -First 1

    if (-not $cert) {
        throw "지정한 인증서를 CurrentUser\\My에서 찾을 수 없습니다: $Thumbprint"
    }
}

if (-not [string]::IsNullOrWhiteSpace($ExportPublicCertPath)) {
    $exportDir = Split-Path -Path $ExportPublicCertPath -Parent
    if ($exportDir) {
        New-Item -ItemType Directory -Path $exportDir -Force | Out-Null
    }

    Export-Certificate -Cert $cert -FilePath $ExportPublicCertPath -Force | Out-Null
}

$arguments = @(
    'sign',
    '/fd', 'SHA256',
    '/sha1', $Thumbprint
)

if (-not [string]::IsNullOrWhiteSpace($TimestampUrl)) {
    $arguments += @('/tr', $TimestampUrl, '/td', 'SHA256')
}

$arguments += $FilePath

& $SignToolPath @arguments
if ($LASTEXITCODE -ne 0) {
    throw "signtool 서명이 실패했습니다. 종료 코드: $LASTEXITCODE"
}

& $SignToolPath 'verify' '/pa' '/v' $FilePath
if ($LASTEXITCODE -ne 0) {
    throw "signtool 서명 검증이 실패했습니다. 종료 코드: $LASTEXITCODE"
}

Write-Host ''
Write-Host '서명 완료:'
Write-Host "파일: $FilePath"
Write-Host "인증서: $($cert.Subject)"
Write-Host "Thumbprint: $Thumbprint"
