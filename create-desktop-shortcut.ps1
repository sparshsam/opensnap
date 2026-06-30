# Creates an OpenSnap shortcut on the desktop.
$exePath = "C:\Users\spars\repos\opensnap\bin\Release\net8.0-windows10.0.19041.0\OpenSnap.exe"
$iconPath = "C:\Users\spars\repos\opensnap\Resources\app.ico"
$shortcutPath = [Environment]::GetFolderPath("Desktop") + "\OpenSnap.lnk"

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.Description = "OpenSnap - Screenshot widget"
$shortcut.IconLocation = "$iconPath, 0"
$shortcut.Save()

Write-Host "Shortcut created: $shortcutPath"
