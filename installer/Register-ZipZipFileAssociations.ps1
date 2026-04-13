param(
    [Parameter(Mandatory = $true)]
    [string]$AppExecutablePath
)

$ErrorActionPreference = 'Stop'

$appExecutable = [System.IO.Path]::GetFullPath($AppExecutablePath)
if (-not (Test-Path -LiteralPath $appExecutable -PathType Leaf)) {
    throw "ZipZip 실행 파일을 찾을 수 없습니다: $appExecutable"
}

$recommendedExtensions = @(
    '.zip', '.7z', '.rar', '.tar', '.gz', '.bz2', '.xz', '.zst',
    '.tgz', '.tbz', '.tbz2', '.txz', '.cab', '.iso', '.wim'
)

$labels = @{
    '.zip'  = 'ZIP'
    '.7z'   = '7Z'
    '.rar'  = 'RAR'
    '.tar'  = 'TAR'
    '.gz'   = 'GZ'
    '.bz2'  = 'BZ2'
    '.xz'   = 'XZ'
    '.zst'  = 'ZST'
    '.tgz'  = 'TGZ'
    '.tbz'  = 'TBZ'
    '.tbz2' = 'TBZ2'
    '.txz'  = 'TXZ'
    '.cab'  = 'CAB'
    '.iso'  = 'ISO'
    '.wim'  = 'WIM'
}

function Get-ProgId([string]$extension) {
    return "ZipZip.Assoc.$($extension.TrimStart('.').ToUpperInvariant())"
}

function Get-AssetKey([string]$extension) {
    $value = $extension.Trim().ToLowerInvariant()

    if ($value -in @('.tgz', '.gz')) { return 'tgz' }
    if ($value -in @('.tbz', '.tbz2', '.bz2')) { return 'bz2' }
    if ($value -in @('.txz', '.xz')) { return 'xz' }
    if ($value -eq '.zst') { return 'zst' }

    switch ($value) {
        '.zip' { return 'zip' }
        '.7z'  { return '7z' }
        '.rar' { return 'rar' }
        '.tar' { return 'tar' }
        '.iso' { return 'iso' }
        default { return 'cab' }
    }
}

function Get-IconPath([string]$extension) {
    $baseDirectory = Split-Path -Path $appExecutable -Parent
    $iconPath = Join-Path $baseDirectory "Assets\FileTypes\$(Get-AssetKey $extension).ico"
    if (Test-Path -LiteralPath $iconPath -PathType Leaf) {
        return $iconPath
    }

    return (Join-Path $baseDirectory 'Assets\App\ZipZip.ico')
}

function Test-HasExplicitUserChoice([string]$extension) {
    $userChoice = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey(
        "Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\$extension\UserChoice")
    try {
        return ($null -ne $userChoice) -and -not [string]::IsNullOrWhiteSpace($userChoice.GetValue('ProgId'))
    }
    finally {
        if ($null -ne $userChoice) { $userChoice.Dispose() }
    }
}

function Set-DefaultValue($key, [string]$value) {
    $key.SetValue('', $value)
}

function Register-Capabilities {
    $registered = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Software\RegisteredApplications')
    $capabilities = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Software\ZipZip\Capabilities')
    $fileAssociationsPath = 'Software\ZipZip\Capabilities\FileAssociations'

    try {
        $registered.SetValue('ZipZip', 'Software\ZipZip\Capabilities')
        $capabilities.SetValue('ApplicationName', 'ZipZip')
        $capabilities.SetValue('ApplicationDescription', '한국 사용자를 위한 Windows 압축 파일 도구')

        [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree($fileAssociationsPath, $false)
        $fileAssociations = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($fileAssociationsPath)
        try {
            foreach ($extension in $recommendedExtensions) {
                $fileAssociations.SetValue($extension, (Get-ProgId $extension))
            }
        }
        finally {
            if ($null -ne $fileAssociations) { $fileAssociations.Dispose() }
        }

        [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree('Software\Classes\Applications\ZipZip.App.exe\SupportedTypes', $false)
        $applicationKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Software\Classes\Applications\ZipZip.App.exe')
        $supportedTypes = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Software\Classes\Applications\ZipZip.App.exe\SupportedTypes')
        $commandKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Software\Classes\Applications\ZipZip.App.exe\shell\open\command')
        try {
            $applicationKey.SetValue('FriendlyAppName', 'ZipZip')
            foreach ($extension in $recommendedExtensions) {
                $supportedTypes.SetValue($extension, '')
            }
            $commandKey.SetValue('', "`"$appExecutable`" `"%1`"")
        }
        finally {
            if ($null -ne $applicationKey) { $applicationKey.Dispose() }
            if ($null -ne $supportedTypes) { $supportedTypes.Dispose() }
            if ($null -ne $commandKey) { $commandKey.Dispose() }
        }
    }
    finally {
        if ($null -ne $registered) { $registered.Dispose() }
        if ($null -ne $capabilities) { $capabilities.Dispose() }
    }
}

Register-Capabilities

foreach ($extension in $recommendedExtensions) {
    $progId = Get-ProgId $extension
    $progIdKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey("Software\Classes\$progId")
    $iconKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey("Software\Classes\$progId\DefaultIcon")
    $commandKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey("Software\Classes\$progId\shell\open\command")
    $extensionKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey("Software\Classes\$extension")
    $openWithKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey("Software\Classes\$extension\OpenWithProgids")

    try {
        $label = $labels[$extension]
        Set-DefaultValue $progIdKey "ZipZip $label 파일"
        $progIdKey.SetValue('FriendlyTypeName', "ZipZip $label 파일")
        Set-DefaultValue $iconKey (Get-IconPath $extension)
        Set-DefaultValue $commandKey "`"$appExecutable`" `"%1`""
        $openWithKey.SetValue($progId, '')

        if (-not (Test-HasExplicitUserChoice $extension)) {
            Set-DefaultValue $extensionKey $progId
        }
    }
    finally {
        if ($null -ne $progIdKey) { $progIdKey.Dispose() }
        if ($null -ne $iconKey) { $iconKey.Dispose() }
        if ($null -ne $commandKey) { $commandKey.Dispose() }
        if ($null -ne $extensionKey) { $extensionKey.Dispose() }
        if ($null -ne $openWithKey) { $openWithKey.Dispose() }
    }
}

Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class ZipZipShellNotify {
    [DllImport("shell32.dll")]
    public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
'@

[ZipZipShellNotify]::SHChangeNotify(0x08000000, 0x0000, [IntPtr]::Zero, [IntPtr]::Zero)
