# Configuration
$ModName = "ValheimPipes"
$ModNameUnity = "UnityValheimPipes"
$Configuration = "Debug"
$TargetFramework = "net472"
$OutputDir = "$PSScriptRoot/$ModName/bin/$Configuration/$TargetFramework"

# Stop early if build output is missing
if (-not (Test-Path "$OutputDir/$ModName.dll")) {
    Write-Error "Output DLL not found. Run 'dotnet build' before deploying."
    exit 1
}

# Optional: Read install folder from environment (for local deployment)
$ValheimInstall = $null
$R2ModmanInstall = $null
$UseR2ModmanPath = $false

if (Test-Path "$PSScriptRoot/Environment.props") {
    [xml]$props = Get-Content "$PSScriptRoot/Environment.props"
    $ValheimInstall = $props.Project.PropertyGroup.VALHEIM_INSTALL
    $R2ModmanInstall = $props.Project.PropertyGroup.R2MODMAN_INSTALL
    if ($props.Project.PropertyGroup.USE_R2MODMAN_AS_DEPLOY_FOLDER -eq "true") {
        $UseR2ModmanPath = $true
    }
}

# Deploy to local game folder if configuration exists
if ($ValheimInstall -or $R2ModmanInstall) {
    if ($UseR2ModmanPath -and $R2ModmanInstall) {
        $BepInExFolder = "$R2ModmanInstall/BepInEx"
    } elseif ($ValheimInstall) {
        $BepInExFolder = "$ValheimInstall/BepInEx"
    }

    if ($BepInExFolder) {
        $PluginFolder = "$BepInExFolder/plugins"
        $ModDir = "$PluginFolder/$ModName"

        Write-Host "Deploying to local game folder: $ModDir" -ForegroundColor Cyan
        if (-not (Test-Path $ModDir)) { New-Item -ItemType Directory -Path $ModDir -Force }
        Copy-Item "$OutputDir/$ModName.dll" $ModDir -Force
        Copy-Item "$OutputDir/$ModName.pdb" $ModDir -Force
        Copy-Item "$PSScriptRoot/README.md" $ModDir -Force
        Copy-Item "$PSScriptRoot/CHANGELOG.md" $ModDir -Force
        Copy-Item "$PSScriptRoot/manifest.json" $ModDir -Force
        Copy-Item "$PSScriptRoot/icon.png" $ModDir -Force
    }
}

# Prepare the Unity Project context
Write-Host "Copying dependencies to $ModNameUnity/Assets/Assemblies" -ForegroundColor Cyan
$UnityAssemblies = "$PSScriptRoot/$ModNameUnity/Assets/Assemblies"
$UnityBundles = "$PSScriptRoot/$ModNameUnity/AssetBundles/StandaloneWindows"

if (-not (Test-Path $UnityAssemblies)) { New-Item -ItemType Directory -Path $UnityAssemblies -Force }
if (-not (Test-Path $UnityBundles)) { New-Item -ItemType Directory -Path $UnityBundles -Force }

# Copy all resolved dependencies from the project's build output to the Unity Asset folder
Copy-Item "$OutputDir/*.dll" $UnityAssemblies -Force -ErrorAction SilentlyContinue

# Clean up older debugging artifacts like .mdb
if (Test-Path "$UnityAssemblies/$ModName.dll.mdb") { Remove-Item "$UnityAssemblies/$ModName.dll.mdb" -Force }

# Make release zip files 
Write-Host "Creating release zip files..." -ForegroundColor Cyan

$DistDir = "$PSScriptRoot/dist"
$PackageDir = "$DistDir/$ModName"
$NexusDir = "$PackageDir/plugins"

if (Test-Path $DistDir) { Remove-Item $DistDir -Recurse -Force }
New-Item -ItemType Directory -Path $PackageDir -Force
New-Item -ItemType Directory -Path $NexusDir -Force

# Package standard zip files
Copy-Item "$OutputDir/$ModName.dll" $PackageDir -Force
Copy-Item "$OutputDir/$ModName.pdb" $PackageDir -Force
Copy-Item "$PSScriptRoot/README.md" $PackageDir -Force
Copy-Item "$PSScriptRoot/CHANGELOG.md" $PackageDir -Force
Copy-Item "$PSScriptRoot/manifest.json" $PackageDir -Force
Copy-Item "$PSScriptRoot/icon.png" $PackageDir -Force

# Create the Nexus specific folder struct
Copy-Item "$OutputDir/$ModName.dll" $NexusDir -Force
Copy-Item "$OutputDir/$ModName.pdb" $NexusDir -Force

# Zip the contents
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($PackageDir, "$DistDir/$ModName.zip")

# Note: Nexus one is usually just the plugins folder contents
# Zip.exe is usually not on windows by default, so we use PowerShell's Compress-Archive
Compress-Archive -Path "$PackageDir/*" -DestinationPath "$DistDir/$ModName.zip" -Force
Compress-Archive -Path "$PackageDir/plugins" -DestinationPath "$DistDir/$ModName-Nexus.zip" -Force

Remove-Item $PackageDir -Recurse -Force

Write-Host "Done! Zip files are located in the dist/ folder." -ForegroundColor Green
