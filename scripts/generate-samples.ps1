<#
.SYNOPSIS
    Regenerate samples/*.xml from FlowNode.Tools.SampleGenerator.
#>
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$toolsProj = Join-Path $repoRoot "FlowNode.Tools\FlowNode.Tools.csproj"

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild `
    -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1

& $msbuild $toolsProj /t:Restore,Build /p:Configuration=Debug /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$exe = Join-Path $repoRoot "FlowNode.Tools\bin\Debug\net472\FlowNode.Tools.exe"
& $exe $repoRoot
Write-Host "Samples written to $repoRoot\samples"
