Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap(1,1)
$bmp.MakeTransparent()
$hicon = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hicon)
$fs = [System.IO.File]::Create('C:\Users\MECHREVO\Desktop\Avora\Avora\Assets\empty.ico')
$icon.Save($fs)
$fs.Close()
Write-Host "Done"
