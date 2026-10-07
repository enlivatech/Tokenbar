# Renders the tray meter at every tray size, zoomed, on dark and light taskbar colours.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root 'src\Tokenbar\bin\x64\Debug\net8.0-windows10.0.19041.0\Tokenbar.exe'
$dump = Join-Path $root 'src\Tokenbar\obj\icon-dump'
$out = Join-Path $root 'docs\screenshots\tray-icons.png'

Start-Process $exe -ArgumentList '--dump-icons', "`"$dump`"" -Wait
Add-Type -AssemblyName System.Drawing

$zoom = 8; $gap = 16
$sizes = 16, 20, 24, 32
$kinds = 'full', 'mixed', 'low', 'stale'
$cell = 32 * $zoom
$width = $gap + ($sizes | ForEach-Object { $_ * $zoom + $gap } | Measure-Object -Sum).Sum
$rowH = $cell + $gap
$height = 2 * $kinds.Count * $rowH

$sheet = New-Object System.Drawing.Bitmap ([int]$width), ([int]$height)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(255, 32, 32, 32))
$g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 238, 238, 238))), 0, $height / 2, $width, $height / 2)

for ($pass = 0; $pass -lt 2; $pass++) {
    $shade = if ($pass -eq 0) { 255 } else { 0 }
    $y0 = $pass * $height / 2 + $gap / 2
    foreach ($k in $kinds) {
        $x0 = $gap
        foreach ($s in $sizes) {
            $bytes = [IO.File]::ReadAllBytes((Join-Path $dump "icon-$s-$k.raw"))
            $yOff = $y0 + ($cell - $s * $zoom) / 2
            for ($i = 0; $i -lt $bytes.Length; $i++) {
                $a = $bytes[$i]
                if ($a -eq 0) { continue }
                $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb($a, $shade, $shade, $shade))
                $g.FillRectangle($brush, $x0 + ($i % $s) * $zoom, $yOff + [math]::Floor($i / $s) * $zoom, $zoom, $zoom)
                $brush.Dispose()
            }
            $x0 += $s * $zoom + $gap
        }
        $y0 += $rowH
    }
}
$sheet.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $sheet.Dispose()
Write-Output $out
