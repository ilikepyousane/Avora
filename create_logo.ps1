Add-Type -AssemblyName System.Drawing

$size = 256
$bmp = New-Object System.Drawing.Bitmap($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
$g.Clear([System.Drawing.Color]::FromArgb(255, 0, 0, 0))

$font = New-Object System.Drawing.Font("Segoe UI", 190, [System.Drawing.FontStyle]::Bold)
$font2 = New-Object System.Drawing.Font("Segoe UI", 182, [System.Drawing.FontStyle]::Bold)
$format = [System.Drawing.StringFormat]::new()
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center

$rect = [System.Drawing.RectangleF]::new(0, -15, 256, 270)

$g.DrawString("A", $font, [System.Drawing.Brushes]::White, $rect, $format)
$g.DrawString("A", $font2, [System.Drawing.Brushes]::Black, $rect, $format)

$savePath = "C:\Users\MECHREVO\Desktop\Music-M-master\Avora\Assets\AvoraLogo.png"
$bmp.Save($savePath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "Logo saved to: $savePath"
Write-Host "File size: $((Get-Item $savePath).Length) bytes"

$font.Dispose()
$font2.Dispose()
$format.Dispose()
$g.Dispose()
$bmp.Dispose()
