param()

$ErrorActionPreference = 'Stop'

$allExtensions = @(
    '.zip', '.7z', '.rar', '.cab', '.arj', '.lzh',
    '.tar', '.gz', '.tgz', '.bz2', '.tbz', '.tbz2', '.xz', '.txz', '.zst', '.lz', '.lzma', '.z', '.cpio',
    '.iso', '.wim'
)

function Get-ProgId([string]$extension) {
    return "ZipZip.Assoc.$($extension.TrimStart('.').ToUpperInvariant())"
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

$registered = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Software\RegisteredApplications')
try {
    $registered.DeleteValue('ZipZip', $false)
}
finally {
    if ($null -ne $registered) { $registered.Dispose() }
}

[Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree('Software\ZipZip\Capabilities', $false)
[Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree('Software\Classes\Applications\ZipZip.App.exe', $false)

foreach ($extension in $allExtensions) {
    $progId = Get-ProgId $extension

    $extensionKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey("Software\Classes\$extension")
    $openWithKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey("Software\Classes\$extension\OpenWithProgids")

    try {
        $openWithKey.DeleteValue($progId, $false)

        if (-not (Test-HasExplicitUserChoice $extension)) {
            $currentDefault = $extensionKey.GetValue('')
            if ($currentDefault -is [string] -and $currentDefault -eq $progId) {
                $extensionKey.DeleteValue('', $false)
            }
        }
    }
    finally {
        if ($null -ne $extensionKey) { $extensionKey.Dispose() }
        if ($null -ne $openWithKey) { $openWithKey.Dispose() }
    }

    [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree("Software\Classes\$progId", $false)
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
