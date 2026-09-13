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
    # MSBuild files have a namespace, so we use GetElementsByTagName for simplicity
    $ValheimInstall = $props.GetElementsByTagName("VALHEIM_INSTALL")[0].InnerText
    $R2ModmanInstall = $props.GetElementsByTagName("R2MODMAN_INSTALL")[0].InnerText
    if ($props.GetElementsByTagName("USE_R2MODMAN_AS_DEPLOY_FOLDER")[0].InnerText -eq "true") {
        $UseR2ModmanPath = $true
    }
}

# set BepInExFolder
$BepInExFolder = $null
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
    if (Test-Path "$OutputDir/$ModName.pdb") { Copy-Item "$OutputDir/$ModName.pdb" $ModDir -Force }
    if (Test-Path "$OutputDir/$ModName.dll.mdb") { Copy-Item "$OutputDir/$ModName.dll.mdb" $ModDir -Force }
    Copy-Item "$PSScriptRoot/README.md" $ModDir -Force
    Copy-Item "$PSScriptRoot/CHANGELOG.md" $ModDir -Force
    Copy-Item "$PSScriptRoot/manifest.json" $ModDir -Force
    Copy-Item "$PSScriptRoot/icon.png" $ModDir -Force
}

# Prepare the Unity Project context
Write-Host "Copying dependencies to $ModNameUnity/Assets/Assemblies" -ForegroundColor Cyan
$UnityAssemblies = "$PSScriptRoot/$ModNameUnity/Assets/Assemblies"
$UnityBundles = "$PSScriptRoot/$ModNameUnity/AssetBundles/StandaloneWindows"

if (-not (Test-Path $UnityAssemblies)) { New-Item -ItemType Directory -Path $UnityAssemblies -Force }
if (-not (Test-Path $UnityBundles)) { New-Item -ItemType Directory -Path $UnityBundles -Force }

# 1. Copy our mod's DLL
Copy-Item "$OutputDir/$ModName.dll" $UnityAssemblies -Force

# 2. Copy BepInEx core DLLs (matching deploy.sh)
if ($BepInExFolder -and (Test-Path "$BepInExFolder/core")) {
    $CoreLibs = @("BepInEx.dll", "0Harmony.dll", "Mono.Cecil.dll", "MonoMod.Utils.dll", "MonoMod.RuntimeDetour.dll")
    foreach ($lib in $CoreLibs) {
        if (Test-Path "$BepInExFolder/core/$lib") {
            Copy-Item "$BepInExFolder/core/$lib" $UnityAssemblies -Force
        }
    }
}

# 3. Copy common plugins (matching deploy.sh fallbacks)
if ($PluginFolder) {
    $PluginPaths = @(
        "MSchmoecker-MultiUserChest/MultiUserChest.dll",
        "MultiUserChest/MultiUserChest.dll",
        "ValheimModding-Jotunn/Jotunn.dll",
        "Jotunn/Jotunn.dll"
    )
    foreach ($p in $PluginPaths) {
        if (Test-Path "$PluginFolder/$p") {
            Copy-Item "$PluginFolder/$p" $UnityAssemblies -Force
        }
    }
}

# 4. Copy Valheim managed assemblies (matching deploy.sh)
if ($ValheimInstall -and (Test-Path "$ValheimInstall/valheim_Data/Managed")) {
    $ValheimLibs = @(
        "assembly_valheim.dll", "assembly_utils.dll", "assembly_postprocessing.dll",
        "assembly_sunshafts.dll", "assembly_guiutils.dll", "assembly_googleanalytics.dll",
        "PlayFab.dll", "PlayFabParty.dll", "Splatform.dll", "Splatform.Steam.dll",
        "gui_framework.dll", "com.rlabrecque.steamworks.net.dll", "SoftReferenceableAssets.dll"
    )
    foreach ($lib in $ValheimLibs) {
        if (Test-Path "$ValheimInstall/valheim_Data/Managed/$lib") {
            Copy-Item "$ValheimInstall/valheim_Data/Managed/$lib" $UnityAssemblies -Force
        }
    }
}

# Clean up older debugging artifacts like .mdb if needed (sh prefers mdb, modern prefers pdb)
# if (Test-Path "$UnityAssemblies/$ModName.dll.mdb") { Remove-Item "$UnityAssemblies/$ModName.dll.mdb" -Force }

# Make release zip files 
Write-Host "Creating release zip files..." -ForegroundColor Cyan

$DistDir = "$PSScriptRoot/dist"
if (Test-Path $DistDir) { Remove-Item $DistDir -Recurse -Force }
New-Item -ItemType Directory -Path $DistDir -Force

# We use a temporary staging area to structure zips
$StagingDir = "$DistDir/staging"
New-Item -ItemType Directory -Path $StagingDir -Force

# Copy loose DLL to dist root for convenience
Copy-Item "$OutputDir/$ModName.dll" $DistDir -Force

# Standard Thunderstore format zip
Copy-Item "$OutputDir/$ModName.dll" $StagingDir -Force
if (Test-Path "$OutputDir/$ModName.pdb") { Copy-Item "$OutputDir/$ModName.pdb" $StagingDir -Force }
if (Test-Path "$OutputDir/$ModName.dll.mdb") { Copy-Item "$OutputDir/$ModName.dll.mdb" $StagingDir -Force }
Copy-Item "$PSScriptRoot/README.md" $StagingDir -Force
Copy-Item "$PSScriptRoot/CHANGELOG.md" $StagingDir -Force
Copy-Item "$PSScriptRoot/manifest.json" $StagingDir -Force
Copy-Item "$PSScriptRoot/icon.png" $StagingDir -Force

Compress-Archive -Path "$StagingDir/*" -DestinationPath "$DistDir/$ModName.zip" -Force

# Nexus format zip (contains a plugins/ subfolder)
$NexusStaging = "$DistDir/nexus_staging"
$NexusPlugins = "$NexusStaging/plugins"
New-Item -ItemType Directory -Path $NexusPlugins -Force

Copy-Item "$OutputDir/$ModName.dll" $NexusPlugins -Force
if (Test-Path "$OutputDir/$ModName.pdb") { Copy-Item "$OutputDir/$ModName.pdb" $NexusPlugins -Force }
if (Test-Path "$OutputDir/$ModName.dll.mdb") { Copy-Item "$OutputDir/$ModName.dll.mdb" $NexusPlugins -Force }

Compress-Archive -Path "$NexusStaging/*" -DestinationPath "$DistDir/$ModName-Nexus.zip" -Force

# Cleanup staging
Remove-Item $StagingDir -Recurse -Force
Remove-Item $NexusStaging -Recurse -Force

Write-Host "Done! Zip files are located in the dist/ folder." -ForegroundColor Green
