# Writes native\res\name_path.txt: the outlines of the body's name "ノルン" in Zarame Gothic (by _mokuzai_, SIL OFL 1.1,
# booth.pm/items/7724393). The whole font is 11 MB for three glyphs, so the app embeds only these outlines and draws them
# as a vector path (Body.cs). Run again after changing the name or the font:
#   powershell -ExecutionPolicy Bypass -File tools\make_name_path.ps1 -Font "<path to Zarame Gothic.ttf>"
param([Parameter(Mandatory = $true)][string]$Font, [string]$Text = "ノルン")
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$pfc = New-Object System.Drawing.Text.PrivateFontCollection
$pfc.AddFontFile((Resolve-Path $Font).Path)
$family = $pfc.Families[0]
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
# em size 100: the app scales the path to the height it needs
$path.AddString($Text, $family, [int][System.Drawing.FontStyle]::Regular, 100, (New-Object System.Drawing.PointF 0, 0), [System.Drawing.StringFormat]::GenericTypographic)
$b = $path.GetBounds()
$inv = [System.Globalization.CultureInfo]::InvariantCulture
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# " + $Text + " in " + $family.Name + " (SIL OFL 1.1), em 100; x y type per point, origin at the ink's top left")
$lines.Add([string]::Format($inv, "{0:0.###} {1:0.###}", $b.Width, $b.Height))
$pts = $path.PathPoints; $types = $path.PathTypes
for ($i = 0; $i -lt $pts.Length; $i++) { $lines.Add([string]::Format($inv, "{0:0.###} {1:0.###} {2}", $pts[$i].X - $b.X, $pts[$i].Y - $b.Y, $types[$i])) }
$out = Join-Path $root "native\res\name_path.txt"
[System.IO.File]::WriteAllLines($out, $lines, (New-Object System.Text.UTF8Encoding $false))
"{0}: {1} points, {2:0.0} x {3:0.0}" -f $out, $pts.Length, $b.Width, $b.Height
