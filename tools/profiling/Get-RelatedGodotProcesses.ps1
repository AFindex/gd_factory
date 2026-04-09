[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$normalizedProjectRootPattern = [Regex]::Escape($projectRoot).Replace("\\", "[\\/]")
$now = Get-Date

$processes = Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -like "Godot*.exe" -and
        $_.CommandLine -and
        $_.CommandLine -match $normalizedProjectRootPattern
    } |
    Sort-Object CreationDate -Descending |
    ForEach-Object {
        $age = $now - $_.CreationDate
        [ordered]@{
            process_id = $_.ProcessId
            name = $_.Name
            command_line = $_.CommandLine
            created_at = $_.CreationDate.ToString("o")
            age_seconds = [int][Math]::Floor($age.TotalSeconds)
            is_editor = ($_.CommandLine -match "(^|\s)-e(\s|$)" -or $_.CommandLine -match "--editor")
            is_editor_launched_game = ($_.CommandLine -match "--editor-pid")
            is_headless = ($_.CommandLine -match "--headless")
        }
    }

$processes | ConvertTo-Json -Depth 4
