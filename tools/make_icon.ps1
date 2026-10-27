# Writes native\TheNorns.ico (16, 24, 32, 48, 64, 128, 256 px) and tools\icon_preview.png: ノ, the first letter of
# ノルン, in Zarame Gothic (by _mokuzai_, SIL OFL 1.1) printed on a piece of the body - the steel-blue hull with its satin
# light and lit edge, and the accent lamp the body has next to its name. Own graphics only (no EVE / CCP material).
#   powershell -ExecutionPolicy Bypass -File tools\make_icon.ps1 -Font "<path to Zarame Gothic.ttf>"
param([Parameter(Mandatory = $true)][string]$Font)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$pfc = New-Object System.Drawing.Text.PrivateFontCollection
$pfc.AddFontFile((Resolve-Path $Font).Path)
$family = $pfc.Families[0]

function Rounded([System.Drawing.RectangleF]$r, [float]$rad) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = 2 * $rad
    $p.AddArc($r.X, $r.Y, $d, $d, 180, 90); $p.AddArc($r.Right - $d, $r.Y, $d, $d, 270, 90)
    $p.AddArc($r.Right - $d, $r.Bottom - $d, $d, $d, 0, 90); $p.AddArc($r.X, $r.Bottom - $d, $d, $d, 90, 90)
    $p.CloseFigure(); return $p
}

# one size, drawn 4x larger and scaled down (smooth edges at 16 px too)
function Render([int]$size) {
    $S = $size * 4
    $big = New-Object System.Drawing.Bitmap $S, $S, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($big)
    $g.SmoothingMode = "AntiAlias"; $g.PixelOffsetMode = "HighQuality"
    $m = $S * 0.035
    $r = New-Object System.Drawing.RectangleF $m, $m, ($S - 2 * $m), ($S - 2 * $m)
    $body = Rounded $r ($S * 0.2)
    $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(78, 87, 104))), $body)
    # satin light from the top left, darker towards the bottom (as the body, Body.cs DrawShell)
    $lg = New-Object System.Drawing.Drawing2D.LinearGradientBrush $r, ([System.Drawing.Color]::White), ([System.Drawing.Color]::Black), 80
    $cb = New-Object System.Drawing.Drawing2D.ColorBlend 3
    $cb.Colors = @([System.Drawing.Color]::FromArgb(46, 255, 255, 255), [System.Drawing.Color]::FromArgb(6, 255, 255, 255), [System.Drawing.Color]::FromArgb(80, 0, 0, 0))
    $cb.Positions = @([float]0, [float]0.45, [float]1)
    $lg.InterpolationColors = $cb
    $g.FillPath($lg, $body)
    $edge = New-Object System.Drawing.Drawing2D.LinearGradientBrush $r, ([System.Drawing.Color]::FromArgb(90, 255, 255, 255)), ([System.Drawing.Color]::FromArgb(130, 0, 0, 0)), 90
    $g.DrawPath((New-Object System.Drawing.Pen $edge, ([Math]::Max(4, $S / 64))), $body)

    # ノ, centred on its ink, a soft shadow under it
    $glyph = New-Object System.Drawing.Drawing2D.GraphicsPath
    $glyph.AddString([string][char]0x30CE, $family, 0, 100, (New-Object System.Drawing.PointF 0, 0), [System.Drawing.StringFormat]::GenericTypographic)
    $b = $glyph.GetBounds()
    $k = ($S * 0.6) / [Math]::Max($b.Width, $b.Height)
    $mx = New-Object System.Drawing.Drawing2D.Matrix
    $mx.Translate($S / 2 - ($b.X + $b.Width / 2) * $k - $S * 0.02, $S / 2 - ($b.Y + $b.Height / 2) * $k + $S * 0.02)
    $mx.Scale($k, $k)
    $glyph.Transform($mx)
    $sh = $glyph.Clone(); $mo = New-Object System.Drawing.Drawing2D.Matrix; $mo.Translate(0, $S * 0.018); $sh.Transform($mo)
    $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(90, 0, 0, 0))), $sh)
    $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(244, 245, 247))), $glyph)

    # the accent lamp (#F7FF14) in the free bottom right corner, with a soft halo
    $c = New-Object System.Drawing.PointF ($S * 0.765), ($S * 0.765)
    $lr = [Math]::Max($S * 0.056, 6)
    $halo = New-Object System.Drawing.Drawing2D.GraphicsPath
    $hr = $lr * 2.3; $halo.AddEllipse($c.X - $hr, $c.Y - $hr, 2 * $hr, 2 * $hr)
    $pb = New-Object System.Drawing.Drawing2D.PathGradientBrush $halo
    $pb.CenterColor = [System.Drawing.Color]::FromArgb(110, 247, 255, 20); $pb.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 247, 255, 20))
    $pb.SetSigmaBellShape(1, 1)
    $g.FillPath($pb, $halo)
    $lamp = New-Object System.Drawing.Drawing2D.GraphicsPath
    $lamp.AddEllipse($c.X - $lr, $c.Y - $lr, 2 * $lr, 2 * $lr)
    $lb = New-Object System.Drawing.Drawing2D.PathGradientBrush $lamp
    $lb.CenterColor = [System.Drawing.Color]::FromArgb(255, 255, 255, 200); $lb.SurroundColors = @([System.Drawing.Color]::FromArgb(255, 225, 240, 0))
    $g.FillPath($lb, $lamp)
    $g.Dispose()

    $out = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g2 = [System.Drawing.Graphics]::FromImage($out)
    $g2.InterpolationMode = "HighQualityBicubic"; $g2.PixelOffsetMode = "HighQuality"; $g2.CompositingQuality = "HighQuality"
    $g2.DrawImage($big, 0, 0, $size, $size)
    $g2.Dispose(); $big.Dispose()
    return $out
}

# a 32-bit BMP icon image (BITMAPINFOHEADER, bottom-up BGRA, empty AND mask) for the small sizes, PNG for 128 / 256
function IconImage([System.Drawing.Bitmap]$bmp) {
    $ms = New-Object System.IO.MemoryStream
    if ($bmp.Width -ge 128) { $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png); return , $ms.ToArray() }
    $w = New-Object System.IO.BinaryWriter $ms
    $n = $bmp.Width; $maskRow = [int]([Math]::Ceiling($n / 32.0) * 4)
    $w.Write([int]40); $w.Write([int]$n); $w.Write([int]($n * 2)); $w.Write([int16]1); $w.Write([int16]32)
    $w.Write([int]0); $w.Write([int]($n * $n * 4 + $maskRow * $n)); $w.Write([int]0); $w.Write([int]0); $w.Write([int]0); $w.Write([int]0)
    for ($y = $n - 1; $y -ge 0; $y--) { for ($x = 0; $x -lt $n; $x++) { $p = $bmp.GetPixel($x, $y); $w.Write([byte]$p.B); $w.Write([byte]$p.G); $w.Write([byte]$p.R); $w.Write([byte]$p.A) } }
    $w.Write((New-Object byte[] ($maskRow * $n)))
    $w.Flush(); return , $ms.ToArray()
}

$sizes = 16, 24, 32, 48, 64, 128, 256
$images = @(); $bitmaps = @{}
foreach ($s in $sizes) { $bm = Render $s; $bitmaps[$s] = $bm; $images += , (IconImage $bm) }
$ico = Join-Path $root "native\TheNorns.ico"
$fs = [System.IO.File]::Create($ico); $w = New-Object System.IO.BinaryWriter $fs
$w.Write([int16]0); $w.Write([int16]1); $w.Write([int16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $dim = if ($s -ge 256) { 0 } else { $s }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([int16]1); $w.Write([int16]32); $w.Write([int]$images[$i].Length); $w.Write([int]$offset)
    $offset += $images[$i].Length
}
foreach ($img in $images) { $w.Write($img) }
$w.Close()

# preview: every size on a dark and a light strip
$pv = New-Object System.Drawing.Bitmap 760, 600
$g = [System.Drawing.Graphics]::FromImage($pv)
$g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(24, 24, 27))), 0, 0, 760, 300)
$g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(236, 236, 240))), 0, 300, 760, 300)
foreach ($band in 0, 300) {
    $x = 20
    foreach ($s in $sizes) { $g.DrawImageUnscaled($bitmaps[$s], $x, $band + 150 - [int]($s / 2)); $x += $s + 20 }
}
$g.Dispose()
$pv.Save((Join-Path $root "tools\icon_preview.png"), [System.Drawing.Imaging.ImageFormat]::Png)
"{0}: {1} sizes, {2} bytes" -f $ico, $sizes.Count, (Get-Item $ico).Length
