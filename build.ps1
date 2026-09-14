# Build script for ValheimPipes
# Copies the Unity AssetBundle into the C# project folder and runs dotnet build

$assetBundleSource = Join-Path $PSScriptRoot "UnityValheimPipes\AssetBundles\StandaloneWindows\valheimpipes_assetbundle"
$assetBundleDest = Join-Path $PSScriptRoot "ValheimPipes\valheimpipes_assetbundle"

Write-Host "Step 1: Copying ValheimPipes AssetBundle..." -ForegroundColor Cyan
if (Test-Path $assetBundleSource) {
    Copy-Item -Path $assetBundleSource -Destination $assetBundleDest -Force
    Write-Host "Done! AssetBundle copied to: $assetBundleDest" -ForegroundColor Green
} else {
    Write-Error "Source AssetBundle not found at $assetBundleSource. Please ensure it has been built in Unity."
    exit 1
}

Write-Host "`nStep 2: Running dotnet build..." -ForegroundColor Cyan
dotnet build $PSScriptRoot

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild Successful!" -ForegroundColor Green
} else {
    Write-Host "`nBuild Failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}
