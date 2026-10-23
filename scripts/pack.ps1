param(
    [string]$Executable,
    [string]$Version,
    [string]$ExpectedSha256
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
if (!$Version) { $Version = (Get-Content -LiteralPath (Join-Path $taskRoot 'VERSION') -Raw).Trim() }
if ($Version -notmatch '\A\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?\z') { throw 'Provide a semantic version without the v prefix.' }
if (!$Executable) {
    & (Join-Path $PSScriptRoot 'build.ps1')
    $Executable = Join-Path $taskRoot 'bin\Glide.exe'
}
$taskExecutable = [IO.Path]::GetFullPath($Executable)
$taskOriginalHash = (Get-FileHash -LiteralPath $taskExecutable -Algorithm SHA256).Hash
if ($ExpectedSha256 -and $taskOriginalHash -ne $ExpectedSha256) { throw 'The executable does not match the expected confirmed build.' }
$taskDist = Join-Path $taskRoot 'dist'
$taskStaging = Join-Path $taskDist ('.staging-' + [Guid]::NewGuid().ToString('N'))
$taskPortable = Join-Path $taskStaging 'Glide'
New-Item -ItemType Directory -Force -Path $taskPortable | Out-Null
Copy-Item -LiteralPath $taskExecutable -Destination (Join-Path $taskPortable 'Glide.exe')
Copy-Item -LiteralPath (Join-Path $taskRoot 'docs\PORTABLE.txt') -Destination (Join-Path $taskPortable 'README.txt')
$taskStandalone = Join-Path $taskDist 'Glide.exe'
Copy-Item -LiteralPath $taskExecutable -Destination $taskStandalone -Force
$taskArchive = Join-Path $taskDist "Glide-$Version-windows-portable.zip"
Compress-Archive -LiteralPath $taskPortable -DestinationPath $taskArchive -Force
$taskLines = foreach ($taskAsset in @($taskStandalone,$taskArchive)) {
    $taskHash = (Get-FileHash -LiteralPath $taskAsset -Algorithm SHA256).Hash.ToLowerInvariant()
    $taskHash + '  ' + [IO.Path]::GetFileName($taskAsset)
}
$taskLines | Set-Content -LiteralPath (Join-Path $taskDist 'SHA256SUMS.txt') -Encoding ascii
if ((Get-FileHash -LiteralPath $taskStandalone -Algorithm SHA256).Hash -ne $taskOriginalHash) { throw 'The preserved executable changed during packaging.' }
Write-Output "Packaged Glide $Version in $taskDist"
