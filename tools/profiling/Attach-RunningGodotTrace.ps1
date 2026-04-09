[CmdletBinding()]
param(
    [string]$Duration = "00:00:10",
    [string]$Profile = "dotnet-common,dotnet-sampled-thread-time",
    [ValidateSet("NetTrace", "Speedscope", "Chromium")]
    [string]$Format = "Speedscope",
    [int]$WaitSeconds = 8,
    [int]$ProcessId = 0,
    [string]$StatusFile = "",
    [switch]$NoOpenOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-StatusFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [hashtable]$Payload
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $Payload | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Resolve-RunningGameProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectRoot,
        [int]$WaitSeconds,
        [int]$RequestedProcessId
    )

    if ($RequestedProcessId -gt 0) {
        $process = Get-CimInstance Win32_Process -Filter "ProcessId = $RequestedProcessId" -ErrorAction SilentlyContinue
        if ($null -eq $process) {
            throw "找不到 PID=$RequestedProcessId 对应的进程。"
        }

        return $process
    }

    $deadline = (Get-Date).AddSeconds($WaitSeconds)
    $normalizedProjectRootPattern = [Regex]::Escape($ProjectRoot).Replace("\\", "[\\/]")

    while ((Get-Date) -lt $deadline) {
        $candidates = Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
            Where-Object {
                $_.Name -like "Godot*.exe" -and
                $_.CommandLine -and
                $_.CommandLine -match $normalizedProjectRootPattern -and
                $_.CommandLine -notmatch "(^|\s)-e(\s|$)" -and
                $_.CommandLine -notmatch "--editor"
            }

        $candidate = $candidates |
            Where-Object { $_.CommandLine -match "--editor-pid" } |
            Sort-Object CreationDate -Descending |
            Select-Object -First 1

        if ($null -eq $candidate) {
            $candidate = $candidates |
                Where-Object { $_.CommandLine -notmatch "--headless" } |
                Sort-Object CreationDate -Descending |
                Select-Object -First 1
        }

        if ($null -eq $candidate) {
            $candidate = $candidates |
            Sort-Object CreationDate -Descending |
            Select-Object -First 1
        }

        if ($null -ne $candidate) {
            return $candidate
        }

        Start-Sleep -Milliseconds 500
    }

    throw "在 $WaitSeconds 秒内没有发现当前项目对应的运行中 Godot 游戏进程。请先在编辑器里点运行，再触发 trace。"
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$artifactsRoot = Join-Path $projectRoot "artifacts\dotnet-diagnostics"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$sessionDir = Join-Path $artifactsRoot "$timestamp-editor-attach-trace"
$tracePath = Join-Path $sessionDir "trace.nettrace"
$readmePath = Join-Path $sessionDir "trace-session.txt"

New-Item -ItemType Directory -Path $sessionDir -Force | Out-Null

Write-StatusFile -Path $StatusFile -Payload @{
    state = "running"
    message = "正在查找运行中的 Godot 游戏进程并准备附加 trace。"
    session_dir = $sessionDir
    trace_path = $tracePath
    created_at = (Get-Date).ToString("o")
}

try {
    Write-Host "Restoring local diagnostic tools..."
    & dotnet tool restore | Out-Host

    $targetProcess = Resolve-RunningGameProcess -ProjectRoot $projectRoot -WaitSeconds $WaitSeconds -RequestedProcessId $ProcessId
    $collectArguments = @(
        "dotnet-trace",
        "collect",
        "--process-id",
        $targetProcess.ProcessId.ToString(),
        "--output",
        $tracePath,
        "--format",
        $Format,
        "--profile",
        $Profile,
        "--duration",
        $Duration
    )

    $commandText = "dotnet " + (($collectArguments | ForEach-Object {
        if ($_ -match '[\s"]') { '"' + ($_ -replace '"', '\"') + '"' } else { $_ }
    }) -join " ")

    $readme = @"
编辑器附加 trace 会话

目标进程
- PID: $($targetProcess.ProcessId)
- Name: $($targetProcess.Name)
- CommandLine: $($targetProcess.CommandLine)

采样设置
- Duration: $Duration
- Profile: $Profile
- Format: $Format

trace 命令
$commandText

输出文件
$tracePath
"@

    $readme | Set-Content -LiteralPath $readmePath -Encoding UTF8

    Write-Host ""
    Write-Host "已附加到运行中的 Godot 游戏进程。"
    Write-Host "PID：$($targetProcess.ProcessId)"
    Write-Host "输出目录：$sessionDir"
    Write-Host "采样时长：$Duration"

    if (-not $NoOpenOutput) {
        Start-Process -FilePath $sessionDir
    }

    & dotnet @collectArguments

    $speedscopePath = [System.IO.Path]::ChangeExtension($tracePath, "speedscope.json")
    $traceSize = if (Test-Path -LiteralPath $tracePath) { (Get-Item -LiteralPath $tracePath).Length } else { 0 }
    $speedscopeSize = if (Test-Path -LiteralPath $speedscopePath) { (Get-Item -LiteralPath $speedscopePath).Length } else { 0 }
    $completedState = "completed"
    $completedMessage = if (Test-Path -LiteralPath $speedscopePath) {
        "trace 已完成，并生成了 nettrace 与 speedscope 文件。"
    } else {
        "trace 已完成。"
    }

    if ($speedscopeSize -gt 0 -and $speedscopeSize -lt 2048) {
        $completedState = "completed_with_warning"
        $completedMessage = "trace 已完成，但 speedscope 文件异常小，通常表示附加到了错误进程，或当前采样窗口里几乎没有托管样本。"
    }

    Write-StatusFile -Path $StatusFile -Payload @{
        state = $completedState
        message = $completedMessage
        session_dir = $sessionDir
        trace_path = $tracePath
        speedscope_path = $speedscopePath
        trace_size = $traceSize
        speedscope_size = $speedscopeSize
        process_id = $targetProcess.ProcessId
        command_line = $targetProcess.CommandLine
        completed_at = (Get-Date).ToString("o")
    }
}
catch {
    $errorMessage = $_.Exception.Message
    Write-StatusFile -Path $StatusFile -Payload @{
        state = "failed"
        message = $errorMessage
        session_dir = $sessionDir
        trace_path = $tracePath
        failed_at = (Get-Date).ToString("o")
    }

    throw
}
