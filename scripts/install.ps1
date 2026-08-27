param(
    [string]$InstallDir = "$env:LOCALAPPDATA\AlarmProgram",
    [switch]$CreateDesktopShortcut
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\AlarmProgram.UI\AlarmProgram.UI.csproj"
$publishDir = Join-Path $repoRoot "artifacts\publish\ReleaseSingleFile"

Write-Host "Publishing Alarm Program..."
dotnet publish $project `
    -c Release `
    -p:PublishProfile=ReleaseSingleFile `
    -o $publishDir

if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir | Out-Null
}

Write-Host "Copying files to $InstallDir"
Copy-Item -Path (Join-Path $publishDir "*") -Destination $InstallDir -Recurse -Force

$quickStartSource = Join-Path $repoRoot "docs\quick-start.md"
$quickStartTarget = Join-Path $InstallDir "QUICKSTART.txt"
Copy-Item -Path $quickStartSource -Destination $quickStartTarget -Force

if ($CreateDesktopShortcut) {
    $exePath = Join-Path $InstallDir "AlarmProgram.UI.exe"
    $desktop = [Environment]::GetFolderPath("Desktop")
    $shortcutPath = Join-Path $desktop "Alarm Program.lnk"
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $exePath
    $shortcut.WorkingDirectory = $InstallDir
    $shortcut.Description = "Alarm Program"
    $shortcut.Save()
    Write-Host "Desktop shortcut created: $shortcutPath"
}

Write-Host "Install complete."
Write-Host "Run: $(Join-Path $InstallDir 'AlarmProgram.UI.exe')"
Write-Host "Quick start: $quickStartTarget"
