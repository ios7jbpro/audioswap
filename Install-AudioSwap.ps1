[CmdletBinding()]
param(
    [switch]$SkipPublish,
    [switch]$SkipRuntimeInstall,
    [switch]$NoDesktopShortcut,
    [switch]$NoStartMenuShortcut,
    [switch]$NoLaunch
)

$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function New-Shortcut {
    param(
        [Parameter(Mandatory = $true)][string]$ShortcutPath,
        [Parameter(Mandatory = $true)][string]$TargetPath,
        [string]$WorkingDirectory,
        [string]$Description
    )

    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $TargetPath
    $shortcut.WorkingDirectory = $WorkingDirectory
    $shortcut.Description = $Description
    $shortcut.Save()
}

function Get-PlatformInfo {
    $platform = switch ($env:PROCESSOR_ARCHITECTURE.ToUpperInvariant()) {
        'AMD64' { 'x64' }
        'X86' { 'x86' }
        'ARM64' { 'arm64' }
        default { throw "Unsupported processor architecture: $env:PROCESSOR_ARCHITECTURE" }
    }

    $runtimeIdentifier = switch ($platform) {
        'x64' { 'win-x64' }
        'x86' { 'win-x86' }
        'arm64' { 'win-arm64' }
        default { throw "Unsupported platform: $platform" }
    }

    return @{
        Platform = $platform
        RuntimeIdentifier = $runtimeIdentifier
    }
}

function Ensure-WindowsAppRuntime {
    param(
        [Parameter(Mandatory = $true)][string]$Platform
    )

    $existing = Get-AppxPackage | Where-Object {
        $_.Name -like '*WindowsAppRuntime*1.6*' -or
        $_.Name -like '*WinAppRuntime*1.6*' -or
        $_.PackageFullName -like '*WindowsAppRuntime*1.6*' -or
        $_.PackageFullName -like '*WinAppRuntime*1.6*'
    } | Select-Object -First 1

    if ($existing) {
        Write-Step "Windows App Runtime 1.6 already present ($($existing.Version))."
        return
    }

    $runtimeMsix = Join-Path $env:USERPROFILE ".nuget\packages\microsoft.windowsappsdk\1.6.250205002\tools\MSIX\win10-$Platform\Microsoft.WindowsAppRuntime.1.6.msix"
    if (-not (Test-Path $runtimeMsix)) {
        throw "Windows App Runtime installer payload not found at $runtimeMsix"
    }

    Write-Step 'Installing Windows App Runtime 1.6 dependency...'
    try {
        Add-AppxPackage -Path $runtimeMsix -ForceApplicationShutdown
    }
    catch {
        if ($_.Exception.Message -like '*higher version of this package is already installed*') {
            Write-Step 'A newer compatible Windows App Runtime 1.6 package is already installed.'
            return
        }

        throw
    }
}

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $repoRoot 'AudioSwap.csproj'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\AudioSwap'
$desktopShortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) 'AudioSwap.lnk'
$startMenuDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$startMenuShortcut = Join-Path $startMenuDir 'AudioSwap.lnk'
$platformInfo = Get-PlatformInfo
$publishDir = Join-Path $repoRoot ("publish\" + $platformInfo.RuntimeIdentifier)

if (-not (Test-Path $projectPath)) {
    throw "Project file not found at $projectPath"
}

if (-not $SkipRuntimeInstall) {
    Ensure-WindowsAppRuntime -Platform $platformInfo.Platform
}
else {
    Write-Step 'Skipping Windows App Runtime installation check...'
}

if (-not $SkipPublish) {
    Write-Step "Publishing AudioSwap (Release, $($platformInfo.RuntimeIdentifier))..."
    dotnet publish $projectPath `
        -c Release `
        -r $platformInfo.RuntimeIdentifier `
        --self-contained false `
        -p:PublishSingleFile=false `
        -p:WindowsAppSDKSelfContained=true `
        -o $publishDir

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}
else {
    Write-Step 'Skipping publish and using the existing publish output...'
}

if (-not (Test-Path $publishDir)) {
    throw "Publish output not found at $publishDir"
}

$exePath = Join-Path $publishDir 'AudioSwap.exe'
if (-not (Test-Path $exePath)) {
    throw "Published executable not found at $exePath"
}

$runningProcess = Get-Process -Name 'AudioSwap' -ErrorAction SilentlyContinue
if ($runningProcess) {
    Write-Step 'Stopping running AudioSwap instances...'
    $runningProcess | Stop-Process -Force
}

Write-Step "Installing to $installDir"
if (Test-Path $installDir) {
    Remove-Item -LiteralPath $installDir -Recurse -Force
}

New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Copy-Item -Path (Join-Path $publishDir '*') -Destination $installDir -Recurse -Force

$installedExe = Join-Path $installDir 'AudioSwap.exe'

if (-not $NoDesktopShortcut) {
    Write-Step 'Creating desktop shortcut...'
    New-Shortcut `
        -ShortcutPath $desktopShortcut `
        -TargetPath $installedExe `
        -WorkingDirectory $installDir `
        -Description 'AudioSwap'
}

if (-not $NoStartMenuShortcut) {
    Write-Step 'Creating Start menu shortcut...'
    if (-not (Test-Path $startMenuDir)) {
        New-Item -ItemType Directory -Path $startMenuDir -Force | Out-Null
    }

    New-Shortcut `
        -ShortcutPath $startMenuShortcut `
        -TargetPath $installedExe `
        -WorkingDirectory $installDir `
        -Description 'AudioSwap'
}

if (-not $NoLaunch) {
    Write-Step 'Launching AudioSwap...'
    Start-Process -FilePath $installedExe -WorkingDirectory $installDir
}

Write-Step 'AudioSwap install complete.'
Write-Host "Installed executable: $installedExe"
