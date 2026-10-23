param(
    [switch]$NativeInput,
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskBinary = Join-Path $taskRoot 'bin\Glide.exe'
$taskResults = Join-Path $taskRoot 'bin\test-results'
if (!$SkipBuild) { & (Join-Path $PSScriptRoot 'build.ps1') }
if (!(Test-Path -LiteralPath $taskBinary)) { throw 'Build Glide before using -SkipBuild.' }
New-Item -ItemType Directory -Force -Path $taskResults | Out-Null
$taskReport = Join-Path $taskResults 'core.txt'
$taskProcess = Start-Process -FilePath $taskBinary -ArgumentList @('--self-test',('"' + $taskReport + '"')) -WindowStyle Hidden -Wait -PassThru
Get-Content -LiteralPath $taskReport
if ($taskProcess.ExitCode -ne 0) { throw 'Core regression checks failed.' }

if ($NativeInput) {
    $taskRunning = Get-Process Glide -ErrorAction SilentlyContinue
    if ($taskRunning) { throw 'Quit Glide before running native input checks.' }
    $taskSource = Join-Path $taskRoot 'src\Glide'
    $taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (!(Test-Path -LiteralPath $taskCompiler)) { throw 'The native receiver requires the 64-bit .NET Framework compiler.' }
    $taskReceiver = Join-Path $taskRoot 'bin\touchpad-test.exe'
    $taskFiles = @('Native.cs','Settings.cs','ScrollEngine.cs','Touchpad.cs')
    $taskSources = @($taskFiles | ForEach-Object { Join-Path $taskSource $_ }) + @(Join-Path $taskRoot 'tests\TouchpadTest.cs')
    & $taskCompiler /nologo /target:winexe /platform:x64 "/out:$taskReceiver" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll $taskSources
    if ($LASTEXITCODE -ne 0) { throw 'Native receiver build failed.' }
    foreach ($taskMode in @('direct','engine')) {
        $taskReport = Join-Path $taskResults ("native-$taskMode.txt")
        $taskArguments = @('"' + $taskReport + '"')
        if ($taskMode -eq 'engine') { $taskArguments += '--engine' }
        $taskProcess = Start-Process -FilePath $taskReceiver -ArgumentList $taskArguments -WindowStyle Hidden -Wait -PassThru
        Get-Content -LiteralPath $taskReport
        if ($taskProcess.ExitCode -ne 0) { throw "Native $taskMode check failed or was interrupted." }
    }
}
