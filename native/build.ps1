# Builds TheNorns.exe with the Roslyn compiler from VS Build Tools 2022 against the .NET Framework 4.x
# assemblies shipped with Windows. No downloads.
#   .\build.ps1                 -> native\bin\TheNorns.exe
#   .\build.ps1 -Out <folder>   -> a scratch build elsewhere (native\bin stays untouched)
#   .\build.ps1 -Setup          -> also TheNorns-Setup-<version>.exe (native\setup\Setup.cs + the app's files as
#                                  an embedded zip) and a portable zip, next to the exe
param([string]$Out = "", [switch]$Setup)
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $here
$csc = Get-ChildItem "${env:ProgramFiles(x86)}\Microsoft Visual Studio\*\*\MSBuild\Current\Bin\Roslyn\csc.exe" |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $csc) { throw "Roslyn csc.exe not found (install Visual Studio Build Tools)" }
$fw = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$out = if ($Out) { $Out } else { Join-Path $here "bin" }
New-Item -ItemType Directory -Force -Path $out | Out-Null
# the version lives in one place: AppInfo.Version / Channel (Core.cs)
$info = Get-Content (Join-Path $here "Core.cs") -Raw
$version = [regex]::Match($info, 'Version = "([^"]+)"').Groups[1].Value
$channel = [regex]::Match($info, 'Channel = "([^"]*)"').Groups[1].Value
$tag = if ($channel) { "$version-$channel" } else { $version }   # 1.0.0 / 1.1.0-beta
$refs = "System.dll", "System.Core.dll", "System.Drawing.dll", "System.Windows.Forms.dll", "System.Web.Extensions.dll" |
    ForEach-Object { "/reference:$fw\$_" }
# every *.cs of native\ (the installer's own file is in native\setup\); icon native\TheNorns.ico;
# manifest: no elevation, per-monitor DPI
$src = Get-ChildItem $here -File | Where-Object { $_.Extension -eq ".cs" } | ForEach-Object { $_.FullName }
$common = "/nologo", "/target:winexe", "/platform:x64", "/optimize+", "/unsafe", "/langversion:7.3",
    "/win32icon:$here\TheNorns.ico", "/win32manifest:$here\app.manifest"
# the app's languages (Loc.cs): native\lang\<code>.json -> embedded as TheNorns.lang.<code>.json
$common += Get-ChildItem "$here\lang" -Filter *.json | ForEach-Object { "/resource:$($_.FullName),TheNorns.lang.$($_.BaseName).json" }
# the body's fonts (OFL) and the power display's glass texture (Body.cs)
$common += "/resource:$here\res\fonts\Orbitron.ttf,TheNorns.font.Orbitron.ttf", "/resource:$here\res\fonts\SpaceGrotesk.ttf,TheNorns.font.SpaceGrotesk.ttf",
    "/resource:$here\res\power_glass.png,TheNorns.power_glass.png", "/resource:$here\res\spawn_alert.wav,TheNorns.spawn_alert.wav", "/resource:$here\res\name_path.txt,TheNorns.name_path.txt", "/resource:$root\CHANGELOG.md,TheNorns.CHANGELOG.md"
& $csc @common "/out:$out\TheNorns.exe" "/main:TheNorns.Program" @refs @src
if ($LASTEXITCODE -ne 0) { throw "build failed" }
Get-Item "$out\TheNorns.exe" | Select-Object FullName, Length, LastWriteTime

if ($Setup) {
    # what gets installed: the exe, the license, readme and changelog (those that exist)
    $stage = Join-Path ([IO.Path]::GetTempPath()) ("norns_stage_" + [guid]::NewGuid().ToString("N").Substring(0, 8))
    New-Item -ItemType Directory -Force -Path $stage | Out-Null
    Copy-Item "$out\TheNorns.exe" $stage
    if (-not (Test-Path "$root\LICENSE")) { throw "missing LICENSE in $root" }
    foreach ($f in "LICENSE", "README.md", "CHANGELOG.md") { if (Test-Path "$root\$f") { Copy-Item "$root\$f" $stage } }
    # third-party licenses of what the exe embeds: the fonts (OFL) and the loadersz animation (MIT)
    New-Item -ItemType Directory -Force -Path "$stage\licenses" | Out-Null
    Copy-Item "$here\res\fonts\*-OFL.txt", "$here\res\loadersz-LICENSE.txt" "$stage\licenses"
    $payload = Join-Path $stage "..\$(Split-Path $stage -Leaf).zip"
    # zip with "/" in entry names (Compress-Archive of Windows PowerShell 5 writes "\")
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    if (Test-Path $payload) { Remove-Item -Force $payload }
    $zip = [System.IO.Compression.ZipFile]::Open($payload, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        Get-ChildItem $stage -Recurse -File | ForEach-Object {
            $name = $_.FullName.Substring($stage.Length + 1).Replace('\', '/')
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $name, [System.IO.Compression.CompressionLevel]::Optimal)
        }
    } finally { $zip.Dispose() }
    $refsSetup = $refs + "/reference:$fw\System.IO.Compression.dll", "/reference:$fw\System.IO.Compression.FileSystem.dll"
    $setupExe = Join-Path $out "TheNorns-Setup-$tag.exe"
    & $csc @common "/out:$setupExe" "/main:TheNorns.SetupProgram" "/resource:$payload,TheNorns.payload.zip" @refsSetup @src "$here\setup\Setup.cs"
    if ($LASTEXITCODE -ne 0) { throw "setup build failed" }
    # portable zip: the same files, unpack and run
    $portable = Join-Path $out "TheNorns-$tag-portable.zip"
    Copy-Item -Force $payload $portable
    Remove-Item -Recurse -Force $stage, $payload
    Get-Item $setupExe, $portable | Select-Object FullName, Length, LastWriteTime
    Get-FileHash $setupExe, $portable -Algorithm SHA256 | Select-Object Hash, Path
}
