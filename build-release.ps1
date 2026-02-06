# OneClick Release Build Script
# Usage: .\build-release.ps1 -Version "1.0.0"

param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  OneClick Release Build v$Version" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Validate version format (semantic versioning)
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "ERROR: Version must be in format X.Y.Z (e.g., 1.0.0)" -ForegroundColor Red
    exit 1
}

# Step 1: Clean previous builds
Write-Host "[1/4] Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean OneClick.Client/OneClick.Client.csproj -c Release
if (Test-Path "./publish") {
    Remove-Item -Path "./publish" -Recurse -Force
}
if (Test-Path "./releases") {
    Remove-Item -Path "./releases" -Recurse -Force
}
Write-Host "  ✓ Clean completed" -ForegroundColor Green
Write-Host ""

# Step 2: Restore dependencies
Write-Host "[2/4] Restoring dependencies..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Restore failed" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Restore completed" -ForegroundColor Green
Write-Host ""

# Step 3: Publish application
Write-Host "[3/4] Publishing application..." -ForegroundColor Yellow
dotnet publish OneClick.Client/OneClick.Client.csproj `
    -c Release `
    -o ./publish `
    --self-contained true `
    -r win-x64 `
    /p:PublishSingleFile=false `
    /p:Version=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Publish failed" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Publish completed" -ForegroundColor Green
Write-Host ""

# Step 4: Create Velopack release
Write-Host "[4/4] Creating Velopack release..." -ForegroundColor Yellow

# Check if vpk is installed
$vpkInstalled = Get-Command vpk -ErrorAction SilentlyContinue
if (-not $vpkInstalled) {
    Write-Host "  ! Velopack CLI (vpk) not found. Installing..." -ForegroundColor Yellow
    dotnet tool install -g vpk
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ✗ Failed to install vpk" -ForegroundColor Red
        exit 1
    }
    Write-Host "  ✓ vpk installed" -ForegroundColor Green
}

vpk pack `
    --packId "OneClick" `
    --packVersion $Version `
    --packDir ./publish `
    --mainExe "OneClick.Client.exe" `
    --outputDir ./releases

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Velopack packaging failed" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Velopack packaging completed" -ForegroundColor Green
Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Build Completed Successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output directory: ./releases" -ForegroundColor Cyan
Write-Host ""
Write-Host "Generated files:" -ForegroundColor White
Get-ChildItem -Path "./releases" | ForEach-Object {
    $size = if ($_.PSIsContainer) { "DIR" } else { "{0:N2} MB" -f ($_.Length / 1MB) }
    Write-Host "  - $($_.Name) ($size)" -ForegroundColor Gray
}
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Test the setup: ./releases/OneClick-Setup.exe" -ForegroundColor White
Write-Host "  2. Upload to GitHub Releases" -ForegroundColor White
Write-Host ""
