[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [int]$ProcessId
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$artifactsRoot = Join-Path $projectRoot "artifacts\dotnet-diagnostics"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outputDir = Join-Path $artifactsRoot "$timestamp-gcdump"
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$outputPath = Join-Path $outputDir "memory.gcdump"

Write-Host "Restoring local diagnostic tools..."
& dotnet tool restore | Out-Host
Write-Host "Collecting gcdump for PID $ProcessId ..."
& dotnet dotnet-gcdump collect -p $ProcessId -o $outputPath
Write-Host "gcdump 已写入：$outputPath"
