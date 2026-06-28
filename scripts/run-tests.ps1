<#
.SYNOPSIS
    Build and run FlowNode.Tests unit tests (covering the pure-logic node/ layer).

.DESCRIPTION
    - FlowNode targets .NET Framework 4.7.2, so Visual Studio's MSBuild is required
      (the .NET SDK MSBuild cannot process the legacy .resx files).
    - Tests are written with NUnit [Test]/Assert but executed by the project's built-in
      lightweight runner, to avoid a known crash in the NUnit3 engine when it enumerates
      .NET 7 runtime directories on this machine.
    - The script points the NuGet global packages folder back to
      %USERPROFILE%\.nuget\packages so restore works offline.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/run-tests.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$testProj = Join-Path $repoRoot "FlowNode.Tests\FlowNode.Tests.csproj"

# Offline restore: use the real user-level global packages folder
# (some environments redirect NUGET_PACKAGES to a temporary cache).
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

& $msbuild $testProj /t:Restore,Build /p:Configuration=$Configuration /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit code $LASTEXITCODE)." }

$testExe = Join-Path $repoRoot "FlowNode.Tests\bin\$Configuration\net472\FlowNode.Tests.exe"
if (-not (Test-Path $testExe)) { throw "Test executable not found: $testExe" }

Write-Host "`nRunning tests..." -ForegroundColor Cyan
& $testExe
exit $LASTEXITCODE
