$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    throw '.NET Framework C# compiler was not found on this Windows installation.'
}

$distDir = Join-Path $projectDir 'dist'
New-Item -ItemType Directory -Path $distDir -Force | Out-Null
$output = Join-Path $distDir 'CodexPet.exe'
$sources = @(Get-ChildItem -LiteralPath $projectDir -Filter '*.cs' -File | ForEach-Object { $_.FullName })

& $compiler /nologo /target:winexe /platform:anycpu /codepage:65001 "/out:$output" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'CodexPet compilation failed.' }
Write-Output $output
