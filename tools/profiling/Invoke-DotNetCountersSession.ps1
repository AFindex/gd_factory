[CmdletBinding()]
param(
    [ValidateSet("factory-demo", "mobile-demo", "mobile-large", "map-validate")]
    [string]$Preset = "factory-demo",
    [string]$Counters = "System.Runtime",
    [int]$RefreshInterval = 1,
    [string]$GodotExe
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-ExistingPath {
    param([Parameter(Mandatory = $true)][object[]]$Candidates)

    foreach ($candidate in $Candidates) {
        $candidateText = [string]$candidate
        if ([string]::IsNullOrWhiteSpace($candidateText)) {
            continue
        }

        if (Test-Path -LiteralPath $candidateText) {
            return (Resolve-Path -LiteralPath $candidateText).Path
        }
    }

    throw "Could not resolve any candidate path. Checked: $($Candidates -join '; ')"
}

function New-PresetCatalog {
    return @{
        "factory-demo" = @{ Name = "Factory Sandbox"; Scene = "res://scenes/factory_demo.tscn"; Headless = $false; UserArgs = @() }
        "mobile-demo" = @{ Name = "Focused Mobile Factory"; Scene = "res://scenes/mobile_factory_demo.tscn"; Headless = $false; UserArgs = @() }
        "mobile-large" = @{ Name = "Large Mobile Scenario"; Scene = "res://scenes/mobile_factory_test_scenario.tscn"; Headless = $false; UserArgs = @() }
        "map-validate" = @{ Name = "Map Validate"; Scene = $null; Headless = $true; UserArgs = @("--factory-map-validate") }
    }
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$projectFile = Join-Path $projectRoot "net_factory.csproj"
$presetConfig = (New-PresetCatalog)[$Preset]

$resolvedGodotConsoleExe = Resolve-ExistingPath -Candidates (@(
    $env:GODOT_MONO_CONSOLE_EXE,
    "D:\Godot\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe"
) | Where-Object { $null -ne $_ })

$resolvedGodotGuiExe = if ($PSBoundParameters.ContainsKey("GodotExe")) {
    Resolve-ExistingPath -Candidates (@($GodotExe) | Where-Object { $null -ne $_ })
} elseif ($env:GODOT_MONO_EXE -and (Test-Path -LiteralPath $env:GODOT_MONO_EXE)) {
    Resolve-ExistingPath -Candidates (@($env:GODOT_MONO_EXE) | Where-Object { $null -ne $_ })
} else {
    $derivedGuiExe = $resolvedGodotConsoleExe -replace "_console(?=\.exe$)", ""
    Resolve-ExistingPath -Candidates (@($derivedGuiExe) | Where-Object { $null -ne $_ })
}

Write-Host "Building $projectFile (Debug)..."
& dotnet build $projectFile -c Debug | Out-Host
Write-Host "Restoring local diagnostic tools..."
& dotnet tool restore | Out-Host

$godotExecutable = if ($presetConfig.Headless) { $resolvedGodotConsoleExe } else { $resolvedGodotGuiExe }
$godotArgs = [System.Collections.Generic.List[string]]::new()
if ($presetConfig.Headless) {
    $null = $godotArgs.Add("--headless")
}

$null = $godotArgs.Add("--path")
$null = $godotArgs.Add($projectRoot)

if ($presetConfig.Scene) {
    $null = $godotArgs.Add($presetConfig.Scene)
}

if ($presetConfig.UserArgs.Count -gt 0) {
    $null = $godotArgs.Add("--")
    foreach ($userArg in $presetConfig.UserArgs) {
        $null = $godotArgs.Add($userArg)
    }
}

$process = Start-Process -FilePath $godotExecutable -ArgumentList $godotArgs.ToArray() -PassThru
$args = @(
    "dotnet-counters",
    "monitor",
    "--process-id",
    $process.Id.ToString(),
    "--counters",
    $Counters,
    "--refresh-interval",
    $RefreshInterval.ToString()
)

Write-Host ""
Write-Host "dotnet-counters 正在启动：$($presetConfig.Name)"
Write-Host "Counters: $Counters"
Write-Host "PID: $($process.Id)"
& dotnet @args
