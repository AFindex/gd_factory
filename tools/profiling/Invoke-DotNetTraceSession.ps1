[CmdletBinding()]
param(
    [ValidateSet(
        "launcher",
        "factory-demo",
        "mobile-demo",
        "mobile-large",
        "factory-smoke",
        "mobile-smoke",
        "mobile-large-smoke",
        "map-validate"
    )]
    [string]$Preset = "factory-demo",
    [ValidateSet("NetTrace", "Speedscope", "Chromium")]
    [string]$Format = "Speedscope",
    [string]$Profile = "dotnet-common,dotnet-sampled-thread-time",
    [string]$Duration = "00:00:10",
    [int]$WarmupSeconds = 4,
    [string]$GodotExe,
    [string]$Configuration = "Debug",
    [switch]$SkipBuild,
    [switch]$NoOpenOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-ExistingPath {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Candidates
    )

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

function Quote-Argument {
    param([Parameter(Mandatory = $true)][string]$Value)

    if ($Value -notmatch '[\s"]') {
        return $Value
    }

    return '"' + ($Value -replace '"', '\"') + '"'
}

function Join-ArgumentList {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    return (($Arguments | ForEach-Object { Quote-Argument $_ }) -join ' ').Trim()
}

function New-PresetCatalog {
    return @{
        "launcher" = @{
            Name = "Launcher"
            Scene = $null
            UserArgs = @()
            Headless = $false
        }
        "factory-demo" = @{
            Name = "Factory Sandbox"
            Scene = "res://scenes/factory_demo.tscn"
            UserArgs = @()
            Headless = $false
        }
        "mobile-demo" = @{
            Name = "Focused Mobile Factory"
            Scene = "res://scenes/mobile_factory_demo.tscn"
            UserArgs = @()
            Headless = $false
        }
        "mobile-large" = @{
            Name = "Large Mobile Scenario"
            Scene = "res://scenes/mobile_factory_test_scenario.tscn"
            UserArgs = @()
            Headless = $false
        }
        "factory-smoke" = @{
            Name = "Factory Smoke"
            Scene = $null
            UserArgs = @("--factory-smoke-test")
            Headless = $true
        }
        "mobile-smoke" = @{
            Name = "Mobile Smoke"
            Scene = "res://scenes/mobile_factory_demo.tscn"
            UserArgs = @("--mobile-factory-smoke-test")
            Headless = $true
        }
        "mobile-large-smoke" = @{
            Name = "Large Mobile Smoke"
            Scene = "res://scenes/mobile_factory_test_scenario.tscn"
            UserArgs = @("--mobile-factory-large-smoke-test")
            Headless = $true
        }
        "map-validate" = @{
            Name = "Map Validate"
            Scene = $null
            UserArgs = @("--factory-map-validate")
            Headless = $true
        }
    }
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$projectFile = Join-Path $projectRoot "net_factory.csproj"
$artifactsRoot = Join-Path $projectRoot "artifacts\dotnet-diagnostics"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$presetCatalog = New-PresetCatalog
$presetConfig = $presetCatalog[$Preset]

$resolvedGodotConsoleExe = Resolve-ExistingPath -Candidates (@(
    $env:GODOT_MONO_CONSOLE_EXE,
    "D:\Godot\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe",
    "D:\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64_console.exe",
    "D:\Godot\Godot_v4.3-stable_mono_win64\Godot_v4.3-stable_mono_win64\Godot_v4.3-stable_mono_win64_console.exe"
) | Where-Object { $null -ne $_ })

$resolvedGodotGuiExe = if ($PSBoundParameters.ContainsKey("GodotExe")) {
    Resolve-ExistingPath -Candidates (@($GodotExe) | Where-Object { $null -ne $_ })
} elseif ($env:GODOT_MONO_EXE -and (Test-Path -LiteralPath $env:GODOT_MONO_EXE)) {
    Resolve-ExistingPath -Candidates (@($env:GODOT_MONO_EXE) | Where-Object { $null -ne $_ })
} else {
    $derivedGuiExe = $resolvedGodotConsoleExe -replace "_console(?=\.exe$)", ""
    Resolve-ExistingPath -Candidates (@($derivedGuiExe) | Where-Object { $null -ne $_ })
}

if (-not $SkipBuild) {
    Write-Host "Building $projectFile ($Configuration)..."
    & dotnet build $projectFile -c $Configuration | Out-Host
}

Write-Host "Restoring local diagnostic tools..."
& dotnet tool restore | Out-Host

$sessionDir = Join-Path $artifactsRoot "$timestamp-$Preset-trace"
New-Item -ItemType Directory -Path $sessionDir -Force | Out-Null

$godotExecutable = if ($presetConfig.Headless) { $resolvedGodotConsoleExe } else { $resolvedGodotGuiExe }
$godotArguments = [System.Collections.Generic.List[string]]::new()

if ($presetConfig.Headless) {
    $null = $godotArguments.Add("--headless")
}

$null = $godotArguments.Add("--path")
$null = $godotArguments.Add($projectRoot)

if ($presetConfig.Scene) {
    $null = $godotArguments.Add($presetConfig.Scene)
}

if ($presetConfig.UserArgs.Count -gt 0) {
    $null = $godotArguments.Add("--")
    foreach ($userArg in $presetConfig.UserArgs) {
        $null = $godotArguments.Add($userArg)
    }
}

$tracePath = Join-Path $sessionDir "trace.nettrace"
$godotSummary = (Join-ArgumentList -Arguments (@($godotExecutable) + $godotArguments.ToArray()))
$launchArguments = $godotArguments.ToArray()
$process = Start-Process -FilePath $godotExecutable -ArgumentList $launchArguments -PassThru
Start-Sleep -Seconds $WarmupSeconds

$collectArguments = [System.Collections.Generic.List[string]]::new()
$null = $collectArguments.Add("dotnet-trace")
$null = $collectArguments.Add("collect")
$null = $collectArguments.Add("--process-id")
$null = $collectArguments.Add($process.Id.ToString())
$null = $collectArguments.Add("--output")
$null = $collectArguments.Add($tracePath)
$null = $collectArguments.Add("--format")
$null = $collectArguments.Add($Format)
$null = $collectArguments.Add("--profile")
$null = $collectArguments.Add($Profile)
$null = $collectArguments.Add("--duration")
$null = $collectArguments.Add($Duration)

if ($presetConfig.Headless) {
    $null = $collectArguments.Add("--clrevents")
    $null = $collectArguments.Add("gc+gchandle")
}

$collectCommand = "dotnet " + (Join-ArgumentList -Arguments $collectArguments.ToArray())
$readmePath = Join-Path $sessionDir "trace-session.txt"
$launchPath = Join-Path $sessionDir "launch-trace.ps1"

$readme = @"
dotnet-trace 会话：$($presetConfig.Name)

说明
- 这是官方 .NET 诊断链路，不依赖 CodeTrack。
- 默认 profile：$Profile
- 输出格式：$Format
- 产物目录：$sessionDir

本次 Godot 启动命令
$godotSummary

本次 trace 命令
$collectCommand

使用方式
1. 脚本会先启动 Godot。
2. 等待 $WarmupSeconds 秒后，再按 PID 附加 trace。
3. 默认采样时长是 $Duration，到时会自动结束。
4. 结果会写到：
   $tracePath

分析建议
- .nettrace 可用 Visual Studio 或 PerfView 打开。
- 如果选择 Speedscope/Chromium，脚本还会额外生成对应格式文件，适合火焰图查看。
"@

$readme | Set-Content -LiteralPath $readmePath -Encoding UTF8

$launchScript = @"
[CmdletBinding()]
param()
& dotnet $(Join-ArgumentList -Arguments $collectArguments.ToArray())
"@

$launchScript | Set-Content -LiteralPath $launchPath -Encoding UTF8

Write-Host ""
Write-Host "dotnet-trace 会话已准备好：$($presetConfig.Name)"
Write-Host "输出目录：$sessionDir"
Write-Host "PID：$($process.Id)"
Write-Host "Trace 命令：$collectCommand"
Write-Host "Godot 已启动，等待 $WarmupSeconds 秒后开始采样，采样时长 $Duration。"

if (-not $NoOpenOutput) {
    Start-Process -FilePath $sessionDir
}

& dotnet @($collectArguments.ToArray())
