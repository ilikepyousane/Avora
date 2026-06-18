Add-Type -AssemblyName System.Drawing

$pngPath = "C:\Users\MECHREVO\Desktop\Avora\Logo\black.png"
$icoPath = "C:\Users\MECHREVO\Desktop\Avora\Avora\Assets\icon.ico"
$icoPath2 = "C:\Users\MECHREVO\Desktop\Avora\Avora\icon.ico"

$img = [System.Drawing.Image]::FromFile($pngPath)
$bmp = New-Object System.Drawing.Bitmap($img, 256, 256)
$hIcon = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)

$fs = [System.IO.File]::Create($icoPath)
$icon.Save($fs)
$fs.Close()

Copy-Item $icoPath $icoPath2 -Force

Write-Host "ICO created successfully"
$icon.Dispose()
$bmp.Dispose()
$img.Dispose()
