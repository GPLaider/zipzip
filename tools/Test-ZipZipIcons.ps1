param([string]$AssetsRoot = (Join-Path $PSScriptRoot '..\src\ZipZip.App\Assets'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$expected = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$files = @(Get-ChildItem -LiteralPath $AssetsRoot -Filter '*.ico' -Recurse)
if ($files.Count -ne 13) { throw 'Expected one app icon and twelve file-type icons.' }
foreach ($file in $files) {
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    if ([BitConverter]::ToUInt16($bytes, 2) -ne 1 -or [BitConverter]::ToUInt16($bytes, 4) -ne $expected.Count) { throw "Invalid ICO header: $file" }
    for ($index = 0; $index -lt $expected.Count; $index++) {
        $entry = 6 + 16 * $index
        $size = [int]$bytes[$entry]
        if ($size -eq 0) { $size = 256 }
        $length = [BitConverter]::ToInt32($bytes, $entry + 8)
        $offset = [BitConverter]::ToInt32($bytes, $entry + 12)
        if ($size -ne $expected[$index] -or $offset + $length -gt $bytes.Length) { throw "Invalid ICO frame: $file" }
        $stream = New-Object IO.MemoryStream(,$bytes[$offset..($offset + $length - 1)])
        $bitmap = [Drawing.Bitmap]::FromStream($stream)
        try {
            if ($bitmap.Width -ne $size -or $bitmap.Height -ne $size -or $bitmap.GetPixel(0, 0).A -ne 0) { throw "Invalid dimensions or transparency: $file / $size" }
        } finally { $bitmap.Dispose(); $stream.Dispose() }
    }
    $png = [IO.Path]::ChangeExtension($file.FullName, '.png')
    $pngBytes = [IO.File]::ReadAllBytes($png)
    if ([Convert]::ToBase64String($pngBytes) -ne [Convert]::ToBase64String($bytes, $offset, $length)) { throw "PNG differs from the 256px ICO frame: $file" }
}
Write-Output 'PASS: 13 icon sets, 117 ICO frames, dimensions, transparent corners and matching PNGs.'
