param(
    [string]$OutputRoot = (Join-Path $PSScriptRoot '..\src\ZipZip.App\Assets'),
    [string]$SourceImage = (Join-Path $PSScriptRoot 'assets\ZipZip-master.png')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$source = @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

public static class ZipZipIconBuilder
{
    public static void GenerateSet(string sourcePath, string outputRoot, string fileName, string accentHex)
    {
        Directory.CreateDirectory(outputRoot);
        using (var source = new Bitmap(sourcePath))
        {
            if (source.Width != source.Height || source.GetPixel(0, 0).A != 0)
                throw new InvalidDataException("Icon master must be square with a transparent background.");
            // Retain the established file-type accent colors and the common silhouette.
            if (!string.IsNullOrEmpty(accentHex))
            {
                var accent = ColorTranslator.FromHtml(accentHex);
                for (var y = 0; y < source.Height; y++)
                for (var x = 0; x < source.Width; x++)
                {
                    var pixel = source.GetPixel(x, y);
                    if (pixel.A > 0 && pixel.B > 100 && pixel.B > pixel.R * 1.5 && pixel.B > pixel.G * 1.2)
                        source.SetPixel(x, y, Color.FromArgb(pixel.A, accent.R * pixel.B / 255, accent.G * pixel.B / 255, accent.B * pixel.B / 255));
                }
            }
            var frames = new Dictionary<int, byte[]>();
            foreach (var size in new[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 })
            {
                using (var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
                using (var graphics = Graphics.FromImage(bitmap))
                using (var attributes = new ImageAttributes())
                using (var stream = new MemoryStream())
                {
                    graphics.Clear(Color.Transparent);
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    attributes.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(source, new Rectangle(0, 0, size, size), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
                    bitmap.Save(stream, ImageFormat.Png);
                    frames[size] = stream.ToArray();
                }
            }
            File.WriteAllBytes(Path.Combine(outputRoot, fileName + ".png"), frames[256]);
            SaveIco(Path.Combine(outputRoot, fileName + ".ico"), frames);
        }
    }
    private static void SaveIco(string outputPath, Dictionary<int, byte[]> frames)
    {
        var orderedFrames = frames.OrderBy(pair => pair.Key).ToArray();
        using (var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)orderedFrames.Length);
            var offset = 6 + (16 * orderedFrames.Length);
            foreach (var frame in orderedFrames)
            {
                writer.Write((byte)(frame.Key >= 256 ? 0 : frame.Key));
                writer.Write((byte)(frame.Key >= 256 ? 0 : frame.Key));
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(frame.Value.Length);
                writer.Write(offset);
                offset += frame.Value.Length;
            }
            foreach (var frame in orderedFrames) writer.Write(frame.Value);
        }
    }
}
'@
Add-Type -TypeDefinition $source -ReferencedAssemblies 'System.Drawing'
[ZipZipIconBuilder]::GenerateSet($SourceImage, (Join-Path $OutputRoot 'App'), 'ZipZip', $null)
$palettes = [ordered]@{
    default = $null; zip = $null; '7z' = '#88C93D'; rar = '#9B82C5'
    tar = '#D88C34'; tgz = '#48C3CB'; bz2 = '#E2A53A'; xz = '#7B91C8'
    zst = '#F2B022'; cab = '#B8C0C8'; iso = '#7C9CC5'; alz = '#F0A428'
}
foreach ($palette in $palettes.GetEnumerator()) {
    [ZipZipIconBuilder]::GenerateSet($SourceImage, (Join-Path $OutputRoot 'FileTypes'), $palette.Key, $palette.Value)
}
Write-Output "Generated ZipZip PNG and nine-frame ICO assets at $OutputRoot"
