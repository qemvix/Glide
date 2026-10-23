param(
    [string]$Output = (Join-Path $PSScriptRoot '..\bin\Glide.exe')
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskSource = Join-Path $taskRoot 'src\Glide'
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $taskCompiler)) {
    $taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (!(Test-Path -LiteralPath $taskCompiler)) {
    throw 'The .NET Framework C# compiler was not found. Install .NET Framework 4.8 development tools.'
}
$taskOutput = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Force -Path (Split-Path $taskOutput -Parent) | Out-Null
$taskFiles = @('Native.cs','Settings.cs','ScrollEngine.cs','App.cs','Diagnostics.cs','Touchpad.cs','Tests.cs')
$taskSources = $taskFiles | ForEach-Object { Join-Path $taskSource $_ }
& $taskCompiler /nologo /target:winexe /platform:anycpu /optimize+ "/out:$taskOutput" "/win32manifest:$taskSource\app.manifest" "/win32icon:$taskSource\Glide.ico" "/resource:$taskSource\Glide.ico,Glide.ico" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll $taskSources
if ($LASTEXITCODE -ne 0) { throw 'Glide build failed.' }
Write-Output "Built $taskOutput"
