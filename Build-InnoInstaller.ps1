param(
    [string]$Platform = "x64",
    [string]$AppVersion = "0.1.0"
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message" -ForegroundColor Cyan
}

$rootDir = $PSScriptRoot
$projectPath = Join-Path $rootDir "AudioSwap.csproj"
$innoScript = Join-Path $rootDir "installer\AudioSwapInstaller.iss"
$publishDir = Join-Path $rootDir "dist\publish\win-$Platform"
$isccCandidates = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

if (-not (Test-Path $projectPath)) {
    throw "Could not find project file at $projectPath"
}

if (-not (Test-Path $innoScript)) {
    throw "Could not find Inno Setup script at $innoScript"
}

$runtimeIdentifier = switch ($Platform.ToLowerInvariant()) {
    "x64" { "win-x64" }
    "x86" { "win-x86" }
    "arm64" { "win-arm64" }
    default { throw "Unsupported platform: $Platform" }
}

$isccPath = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($isccPath)) {
    throw "Could not find ISCC.exe. Install Inno Setup 6 first."
}

Write-Step "Publishing AudioSwap ($runtimeIdentifier)..."
dotnet publish $projectPath `
    -c Release `
    -r $runtimeIdentifier `
    --self-contained false `
    -p:PublishSingleFile=false `
    -p:WindowsAppSDKSelfContained=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$exePath = Join-Path $publishDir "AudioSwap.exe"
if (-not (Test-Path $exePath)) {
    throw "Published executable not found at $exePath"
}

$env:AUDIOSWAP_PUBLISH_DIR = $publishDir
$env:AUDIOSWAP_APP_VERSION = $AppVersion

Write-Step "Compiling Inno Setup installer..."
Push-Location (Join-Path $rootDir "installer")
try {
    & $isccPath $innoScript
    if ($LASTEXITCODE -ne 0) {
        throw "ISCC failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

$installerOutputDir = Join-Path $rootDir "dist\inno"
$installerPath = Get-ChildItem -Path $installerOutputDir -Filter *.exe | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($null -eq $installerPath) {
    throw "Could not find compiled installer in $installerOutputDir"
}

Write-Step "Installer ready"
Write-Host $installerPath.FullName
