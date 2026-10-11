# Generates the custom app.ico (camera-with-slash icon) for the build.
# Run this before build.bat if you want the custom icon embedded.
# Output: src\app.ico

Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot "app.ico"

$bmp = New-Object System.Drawing.Bitmap 32, 32
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# dark circular background
$bg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 34, 45, 65))
$g.FillEllipse($bg, 1, 1, 30, 30)

# camera body
$cam = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 235, 235, 240))
$g.FillRectangle($cam, 7, 12, 13, 9)
$g.FillRectangle($cam, 20, 15, 3, 3)

# lens
$lens = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 60, 72, 95))
$g.FillEllipse($lens, 10, 14, 6, 5)

# red diagonal slash (blocked)
$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 235, 55, 55)), 3
$pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawLine($pen, 5, 26, 27, 6)

$icon = [System.Drawing.Icon]::FromHandle($bmp.GetHicon())
$fs = [System.IO.File]::Create($out)
$icon.Save($fs)
$fs.Close()

$pen.Dispose(); $lens.Dispose(); $cam.Dispose(); $bg.Dispose()
$g.Dispose(); $bmp.Dispose(); $icon.Dispose()

Write-Host "Icon written to: $out"
