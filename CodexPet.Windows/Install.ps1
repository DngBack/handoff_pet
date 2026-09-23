$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$builtExe = & (Join-Path $projectDir 'Build.ps1')
$installDir = Join-Path $env:LOCALAPPDATA 'CodexPet'
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
$installedExe = Join-Path $installDir 'CodexPet.exe'
$running = Get-Process CodexPet -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $installedExe }
foreach ($process in $running) {
    $process.Kill()
    $process.WaitForExit(5000) | Out-Null
}
Copy-Item -LiteralPath $builtExe -Destination $installedExe -Force

# The application registers itself under HKCU\...\Run on first launch.
# It starts in the user's desktop session, where its popup can be shown.
Start-Process -FilePath $installedExe -ArgumentList '--show-now' -WindowStyle Normal
Write-Output "Installed and started: $installedExe"
