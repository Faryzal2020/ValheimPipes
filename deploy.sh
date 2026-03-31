#!/bin/bash

# Configuration
ModName="ValheimHopper"
ModNameUnity="UnityValheimHopper"
Configuration="Debug"
TargetFramework="net472"
OutputDir="$ModName/bin/$Configuration/$TargetFramework"

# Stop early if build output is missing
if [ ! -f "$OutputDir/$ModName.dll" ]; then
    echo "Output DLL not found. Run 'dotnet build' before deploying."
    exit 1
fi

# Function for XML reading
read_dom () {
    local IFS=\>
    read -d \< ENTITY CONTENT
}

# Optional: Read install folder from environment (for local deployment)
if [ -f Environment.props ]; then
    while read_dom; do
        if [[ $ENTITY = "VALHEIM_INSTALL" ]]; then
            VALHEIM_INSTALL=$CONTENT
        fi
        if [[ $ENTITY = "R2MODMAN_INSTALL" ]]; then
            R2MODMAN_INSTALL=$CONTENT
        fi
        if [[ $ENTITY = "USE_R2MODMAN_AS_DEPLOY_FOLDER" ]]; then
            USE_R2MODMAN_AS_DEPLOY_FOLDER=$CONTENT
        fi
    done < Environment.props
fi

# Deploy to local game folder if configuration exists
if [[ -n "$VALHEIM_INSTALL" ]] || [[ -n "$R2MODMAN_INSTALL" ]]; then
    if [[ "$USE_R2MODMAN_AS_DEPLOY_FOLDER" == "true" ]]; then
      BepInExFolder="$R2MODMAN_INSTALL/BepInEx"
    else
        BepInExFolder="$VALHEIM_INSTALL/BepInEx"
    fi

    PluginFolder="$BepInExFolder/plugins"
    ModDir="$PluginFolder/$ModName"

    echo "Deploying to local game folder: $ModDir"
    mkdir -p "$ModDir"
    cp "$OutputDir/$ModName.dll" "$ModDir"
    cp "$OutputDir/$ModName.pdb" "$ModDir"
    cp README.md "$ModDir"
    cp CHANGELOG.md "$ModDir"
    cp manifest.json "$ModDir"
    cp icon.png "$ModDir"
fi

# Prepare the Unity Project context
echo "Copying dependencies to $ModNameUnity/Assets/Assemblies"
mkdir -p "$ModNameUnity/Assets/Assemblies"
mkdir -p "$ModNameUnity/AssetBundles/StandaloneWindows"

# Copy all resolved dependencies from the project's build output to the Unity Asset folder
# This completely replaces the old hardcoded VALHEIM_INSTALL copies since NuGet handles it
cp -r $OutputDir/*.dll "$ModNameUnity/Assets/Assemblies/" 2>/dev/null

# Clean up older debugging artifacts like .mdb
[ -f "$ModNameUnity/Assets/Assemblies/$ModName.dll.mdb" ] && rm "$ModNameUnity/Assets/Assemblies/$ModName.dll.mdb"

# Make release zip files 
echo "Creating release zip files..."

DistDir="dist"
mkdir -p "$DistDir/$ModName/plugins"

# Package standard zip files
cp "$OutputDir/$ModName.dll" "$DistDir/$ModName"
cp "$OutputDir/$ModName.pdb" "$DistDir/$ModName"
cp README.md "$DistDir/$ModName"
cp CHANGELOG.md "$DistDir/$ModName"
cp manifest.json "$DistDir/$ModName"
cp icon.png "$DistDir/$ModName"

# Create the Nexus specific folder struct
cp "$OutputDir/$ModName.dll" "$DistDir/$ModName/plugins"
cp "$OutputDir/$ModName.pdb" "$DistDir/$ModName/plugins"

cd "$DistDir/$ModName" || exit

zip "../$ModName.zip" "$ModName.dll" "$ModName.pdb" README.md CHANGELOG.md manifest.json icon.png
zip -r "../$ModName-Nexus.zip" plugins

cd ../../
rm -r "$DistDir/$ModName"

echo "Done! Zip files are located in the dist/ folder."
