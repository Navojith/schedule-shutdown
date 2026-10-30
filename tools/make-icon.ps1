# Generates the app icon: a white power symbol on a blue rounded square.
#   src/ScheduleShutdown.App/Assets/AppIcon.ico  (16-64 px as 32-bit DIBs, 256 px as PNG)
#   src/ScheduleShutdown.App/Assets/AppIcon.png  (256 px, used for toasts and the title bar)
# Run from the repo root in Windows PowerShell:  powershell -File tools/make-icon.ps1
Add-Type -AssemblyName System.Drawing

$assets = Join-Path $PSScriptRoot '..\src\ScheduleShutdown.App\Assets'
$sizes = 16, 20, 24, 32, 40, 48, 64, 256

function New-IconBitmap([int]$size) {
    $bitmap = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    # Rounded square, Fluent blue, lighter at the top.
    $radius = [Math]::Max(2, $size * 0.22)
    $d = $radius * 2
    $max = $size - 1
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($max - $d, 0, $d, $d, 270, 90)
    $path.AddArc($max - $d, $max - $d, $d, $d, 0, 90)
    $path.AddArc(0, $max - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $rect = New-Object System.Drawing.RectangleF 0, 0, $size, $size
    $top = [System.Drawing.Color]::FromArgb(255, 0x2B, 0x88, 0xD8)
    $bottom = [System.Drawing.Color]::FromArgb(255, 0x0F, 0x5C, 0xA8)
    $fill = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, $top, $bottom, 90
    $g.FillPath($fill, $path)

    # Power symbol: an arc open at the top and a vertical stroke. Small sizes get relatively thicker lines.
    $stroke = [Math]::Max($size * 0.085, 1.6)
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), $stroke
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $inset = if ($size -le 20) { $size * 0.2 } else { $size * 0.25 }
    $center = $size / 2
    $arcSize = $size - 2 * $inset
    $g.DrawArc($pen, [single]$inset, [single]($inset + $size * 0.03), [single]$arcSize, [single]$arcSize, -55, 290)
    $g.DrawLine($pen, [single]$center, [single]($inset - $size * 0.04), [single]$center, [single]($center - $size * 0.02))

    $pen.Dispose(); $fill.Dispose(); $path.Dispose(); $g.Dispose()
    $bitmap
}

function Get-PngBytes($bitmap) {
    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $stream.ToArray()
    $stream.Dispose()
    , $bytes
}

# 32-bit DIB as stored in an ICO: BITMAPINFOHEADER (double height), bottom-up BGRA rows, then an
# all-zero AND mask (alpha does the masking). Small sizes use this: PNG entries below 256 px aren't
# understood by every consumer (System.Drawing.Icon shows them as noise).
function Get-DibBytes($bitmap) {
    $size = $bitmap.Width
    $stream = New-Object System.IO.MemoryStream
    $w = New-Object System.IO.BinaryWriter $stream
    $w.Write([uint32]40); $w.Write([int32]$size); $w.Write([int32]($size * 2))
    $w.Write([uint16]1); $w.Write([uint16]32); $w.Write([uint32]0)
    $w.Write([uint32]0); $w.Write([int32]0); $w.Write([int32]0); $w.Write([uint32]0); $w.Write([uint32]0)
    for ($y = $size - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $size; $x++) {
            $c = $bitmap.GetPixel($x, $y)
            $w.Write([byte]$c.B); $w.Write([byte]$c.G); $w.Write([byte]$c.R); $w.Write([byte]$c.A)
        }
    }
    $maskRow = [int]([Math]::Ceiling($size / 32) * 4)
    $w.Write((New-Object byte[] ($maskRow * $size)))
    $w.Flush()
    $bytes = $stream.ToArray()
    $w.Dispose()
    , $bytes
}

$images = foreach ($size in $sizes) {
    $bitmap = New-IconBitmap $size
    if ($size -eq 256) {
        $bitmap.Save((Join-Path $assets 'AppIcon.png'), [System.Drawing.Imaging.ImageFormat]::Png)
        $bytes = Get-PngBytes $bitmap
    }
    else {
        $bytes = Get-DibBytes $bitmap
    }
    [pscustomobject]@{ Size = $size; Bytes = $bytes }
    $bitmap.Dispose()
}

# ICO: 6-byte header, 16-byte directory entry per image, then the PNG data.
$out = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter $out
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$images.Count)
$offset = 6 + 16 * $images.Count
foreach ($image in $images) {
    $dimension = if ($image.Size -ge 256) { 0 } else { $image.Size }
    $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
    $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32)
    $writer.Write([uint32]$image.Bytes.Length); $writer.Write([uint32]$offset)
    $offset += $image.Bytes.Length
}
foreach ($image in $images) { $writer.Write($image.Bytes) }
$writer.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $assets 'AppIcon.ico'), $out.ToArray())
$writer.Dispose()

"Wrote AppIcon.ico ($($sizes -join ', ') px) and AppIcon.png to $((Resolve-Path $assets).Path)"
