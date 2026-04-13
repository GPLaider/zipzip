param(
    [string]$OutputRoot = (Join-Path $PSScriptRoot '..\src\ZipZip.App\Assets')
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$source = @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

public static class ZipZipIconBuilder
{
    public static void GenerateSet(string outputRoot, string fileName, string accentHex)
    {
        Directory.CreateDirectory(outputRoot);

        var frames = new Dictionary<int, byte[]>();
        foreach (var size in new[] { 16, 32, 48, 64, 256 })
        {
            using (var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var stream = new MemoryStream())
            {
                DrawIcon(graphics, size, ColorTranslator.FromHtml(accentHex));
                bitmap.Save(stream, ImageFormat.Png);
                frames[size] = stream.ToArray();
            }
        }

        File.WriteAllBytes(Path.Combine(outputRoot, fileName + ".png"), frames[256]);
        SaveIco(Path.Combine(outputRoot, fileName + ".ico"), frames);
    }

    private static void DrawIcon(Graphics graphics, int size, Color accent)
    {
        var scale = size / 256f;
        var outline = ColorTranslator.FromHtml("#0E2A43");
        var bodyFill = Color.White;
        var shadow = Color.FromArgb(28, outline);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);

        using (var shadowBrush = new SolidBrush(shadow))
        using (var chimneyPath = CreateRoundedRectangle(72f * scale, 66f * scale, 30f * scale, 38f * scale, 8f * scale))
        using (var bodyPath = CreateRoundedRectangle(74f * scale, 118f * scale, 108f * scale, 107f * scale, 18f * scale))
        using (var chimneyBrush = new SolidBrush(accent))
        using (var bodyBrush = new SolidBrush(bodyFill))
        using (var accentBrush = new SolidBrush(accent))
        using (var outlineBrush = new SolidBrush(outline))
        using (var outlinePen = new Pen(outline, 12f * scale))
        {
            outlinePen.LineJoin = LineJoin.Round;
            graphics.FillEllipse(shadowBrush, 54f * scale, 222f * scale, 148f * scale, 14f * scale);
            graphics.FillPath(chimneyBrush, chimneyPath);
            graphics.DrawPath(outlinePen, chimneyPath);

            using (var roofOutlinePen = new Pen(outline, 40f * scale))
            using (var roofAccentPen = new Pen(accent, 24f * scale))
            {
                roofOutlinePen.StartCap = LineCap.Round;
                roofOutlinePen.EndCap = LineCap.Round;
                roofOutlinePen.LineJoin = LineJoin.Round;
                roofAccentPen.StartCap = LineCap.Round;
                roofAccentPen.EndCap = LineCap.Round;
                roofAccentPen.LineJoin = LineJoin.Round;

                var roofPoints = new[]
                {
                    new PointF(46f * scale, 118f * scale),
                    new PointF(128f * scale, 48f * scale),
                    new PointF(210f * scale, 118f * scale),
                };

                graphics.DrawLines(roofOutlinePen, roofPoints);
                graphics.DrawLines(roofAccentPen, roofPoints);
            }

            graphics.FillPath(bodyBrush, bodyPath);
            graphics.DrawPath(outlinePen, bodyPath);

            var state = graphics.Save();
            graphics.SetClip(bodyPath);
            graphics.FillRectangle(accentBrush, 76f * scale, 194f * scale, 104f * scale, 30f * scale);
            graphics.Restore(state);

            var zipperTop = new[]
            {
                new PointF(128f * scale, 73f * scale),
                new PointF(114f * scale, 145f * scale),
                new PointF(118f * scale, 184f * scale),
                new PointF(138f * scale, 184f * scale),
                new PointF(142f * scale, 145f * scale),
            };
            graphics.FillPolygon(outlineBrush, zipperTop);

            using (var pullPath = new GraphicsPath())
            using (var pullPen = new Pen(outline, 6f * scale))
            using (var pullBrush = new SolidBrush(bodyFill))
            using (var stemPath = CreateRoundedRectangle(121f * scale, 184f * scale, 14f * scale, 40f * scale, 4f * scale))
            using (var toothBrush = new SolidBrush(bodyFill))
            {
                pullPen.LineJoin = LineJoin.Round;
                pullPath.AddPolygon(new[]
                {
                    new PointF(128f * scale, 150f * scale),
                    new PointF(114f * scale, 190f * scale),
                    new PointF(119f * scale, 205f * scale),
                    new PointF(137f * scale, 205f * scale),
                    new PointF(142f * scale, 190f * scale),
                });

                graphics.FillPath(pullBrush, pullPath);
                graphics.DrawPath(pullPen, pullPath);

                graphics.FillPath(outlineBrush, stemPath);
                graphics.FillRectangle(toothBrush, 123f * scale, 192f * scale, 10f * scale, 11f * scale);
                graphics.FillRectangle(toothBrush, 123f * scale, 208f * scale, 10f * scale, 11f * scale);
            }
        }
    }

    private static GraphicsPath CreateRoundedRectangle(float x, float y, float width, float height, float radius)
    {
        var diameter = radius * 2f;
        var path = new GraphicsPath();
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
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
                var size = frame.Key;
                var bytes = frame.Value;

                writer.Write((byte)(size >= 256 ? 0 : size));
                writer.Write((byte)(size >= 256 ? 0 : size));
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(bytes.Length);
                writer.Write(offset);

                offset += bytes.Length;
            }

            foreach (var frame in orderedFrames)
            {
                writer.Write(frame.Value);
            }
        }
    }
}
"@

Add-Type -TypeDefinition $source -ReferencedAssemblies 'System.Drawing'

$appRoot = Join-Path $OutputRoot 'App'
$fileTypeRoot = Join-Path $OutputRoot 'FileTypes'

$palettes = @(
    @{ Name = 'ZipZip'; Folder = $appRoot; Color = '#5B9EF1' },
    @{ Name = 'default'; Folder = $fileTypeRoot; Color = '#5B9EF1' },
    @{ Name = 'zip'; Folder = $fileTypeRoot; Color = '#5B9EF1' },
    @{ Name = '7z'; Folder = $fileTypeRoot; Color = '#88C93D' },
    @{ Name = 'rar'; Folder = $fileTypeRoot; Color = '#9B82C5' },
    @{ Name = 'tar'; Folder = $fileTypeRoot; Color = '#D88C34' },
    @{ Name = 'tgz'; Folder = $fileTypeRoot; Color = '#48C3CB' },
    @{ Name = 'bz2'; Folder = $fileTypeRoot; Color = '#E2A53A' },
    @{ Name = 'xz'; Folder = $fileTypeRoot; Color = '#7B91C8' },
    @{ Name = 'zst'; Folder = $fileTypeRoot; Color = '#F2B022' },
    @{ Name = 'cab'; Folder = $fileTypeRoot; Color = '#B8C0C8' },
    @{ Name = 'iso'; Folder = $fileTypeRoot; Color = '#7C9CC5' },
    @{ Name = 'alz'; Folder = $fileTypeRoot; Color = '#F0A428' }
)

foreach ($palette in $palettes) {
    [ZipZipIconBuilder]::GenerateSet($palette.Folder, $palette.Name, $palette.Color)
}

Write-Output "Generated ZipZip icons at $OutputRoot"
