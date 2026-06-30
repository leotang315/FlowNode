<#
.SYNOPSIS
    Build and run FlowNode unit tests (Core + Editor).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/run-tests.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$sln = Join-Path $repoRoot "FlowNode.sln"

$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE ".nuget\packages"

function Resolve-MSBuild {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $path = & $vswhere -latest -requires Microsoft.Component.MSBuild `
            -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
        if ($path -and (Test-Path $path)) { return $path }
    }
    $fallback = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    if (Test-Path $fallback) { return $fallback }
    throw "Visual Studio MSBuild.exe not found. Install VS 2022 with the .NET desktop workload."
}

$msbuild = Resolve-MSBuild
Write-Host "Using MSBuild: $msbuild"

& $msbuild $sln /t:Restore,Build /p:Configuration=$Configuration /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit code $LASTEXITCODE)." }

$coreTests = Join-Path $repoRoot "FlowNode.Tests\bin\$Configuration\net472\FlowNode.Tests.exe"
$editorTests = Join-Path $repoRoot "FlowNode.Tests.Editor\bin\$Configuration\net472\FlowNode.Tests.Editor.exe"

foreach ($exe in @($coreTests, $editorTests)) {
    if (-not (Test-Path $exe)) { throw "Test executable not found: $exe" }
}

Write-Host "`nRunning FlowNode.Tests (Core)..." -ForegroundColor Cyan
& $coreTests
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`nRunning FlowNode.Tests.Editor..." -ForegroundColor Cyan
& $editorTests
exit $LASTEXITCODE
