[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Version,

    [Parameter(Position = 1)]
    [string]$Changelog,

    [Parameter()]
    [string]$ChangelogFile,

    [Parameter()]
    [string]$Token,

    [switch]$Draft,
    [switch]$Prerelease,
    [switch]$SkipGitChecks,
    [switch]$NoPush
)

# Enable TLS 1.2 for modern GitHub API compatibility on PowerShell 5.1
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12 -bor [System.Net.ServicePointManager]::SecurityProtocol

$RepoRoot = $PSScriptRoot

Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "         ValheimPipes GitHub Release Automation        " -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan

# -------------------------------------------------------------
# 1. Resolve Git Remote & Repository
# -------------------------------------------------------------
$remoteUrl = git config --get remote.origin.url
$Owner = "Faryzal2020"
$Repo = "ValheimPipes"

if ($remoteUrl -and $remoteUrl -match 'github\.com[:/](?<owner>[^/]+)/(?<repo>[^/]+?)(\.git)?$') {
    $Owner = $Matches['owner']
    $Repo = $Matches['repo']
}
Write-Host "Target Repository : $Owner/$Repo" -ForegroundColor Gray

# -------------------------------------------------------------
# 2. Pre-flight Git Checks
# -------------------------------------------------------------
$currentBranch = (git rev-parse --abbrev-ref HEAD 2>$null).Trim()
Write-Host "Current Branch    : $currentBranch" -ForegroundColor Gray

if ($currentBranch -ne "master" -and -not $SkipGitChecks) {
    Write-Warning "You are on branch '$currentBranch'. Releases are normally published from 'master' after merging dev."
    $confirmBranch = Read-Host "Do you want to proceed releasing from '$currentBranch'? (y/N)"
    if ($confirmBranch -notmatch '^[yY]') {
        Write-Host "Aborted. Please checkout 'master' first." -ForegroundColor Yellow
        exit 0
    }
}

$dirtyFiles = git status --porcelain
if ($dirtyFiles -and -not $SkipGitChecks) {
    Write-Warning "Working tree has uncommitted or unstaged changes:"
    $dirtyFiles | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    $confirmDirty = Read-Host "Do you want to proceed anyway? (y/N)"
    if ($confirmDirty -notmatch '^[yY]') {
        Write-Host "Aborted. Please commit or stash your changes before releasing." -ForegroundColor Yellow
        exit 1
    }
}

# -------------------------------------------------------------
# 3. Resolve GitHub Personal Access Token (PAT)
# -------------------------------------------------------------
if (-not $Token) {
    if ($env:GITHUB_TOKEN) {
        $Token = $env:GITHUB_TOKEN
        Write-Host "Using token from `$env:GITHUB_TOKEN" -ForegroundColor Gray
    } elseif (Test-Path "$RepoRoot/.github_token") {
        $Token = (Get-Content "$RepoRoot/.github_token" -Raw).Trim()
        Write-Host "Using token from .github_token file" -ForegroundColor Gray
    } elseif (Test-Path "$RepoRoot/.env") {
        foreach ($line in (Get-Content "$RepoRoot/.env")) {
            if ($line -match '^\s*GITHUB_TOKEN\s*=\s*(.+)$') {
                $Token = $Matches[1].Trim().Trim('"').Trim("'")
                Write-Host "Using token from .env file" -ForegroundColor Gray
                break
            }
        }
    }
}

if (-not $Token) {
    Write-Host "`nGitHub Personal Access Token (PAT) required." -ForegroundColor Yellow
    Write-Host "Permissions needed: 'Contents: Read & Write' (fine-grained) or 'repo' scope (classic)." -ForegroundColor Gray
    Write-Host "Create one at: https://github.com/settings/tokens" -ForegroundColor Gray
    $secureToken = Read-Host "Enter GitHub PAT" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken)
    $Token = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
}

if (-not $Token -or -not $Token.Trim()) {
    Write-Error "No GitHub token provided. Cannot authenticate with GitHub API."
    exit 1
}

# -------------------------------------------------------------
# 4. Resolve Version
# -------------------------------------------------------------
$manifestPath = "$RepoRoot/manifest.json"
$currentVersion = "1.0.0"
if (Test-Path $manifestPath) {
    try {
        $manifestJson = Get-Content $manifestPath -Raw | ConvertFrom-Json
        if ($manifestJson.version_number) { $currentVersion = $manifestJson.version_number }
    } catch { }
}

if (-not $Version) {
    $enteredVersion = Read-Host "`nEnter release version [default: $currentVersion]"
    if ($enteredVersion -and $enteredVersion.Trim()) {
        $Version = $enteredVersion.Trim()
    } else {
        $Version = $currentVersion
    }
}

# Normalize: strip leading 'v' for file sync, keep 'v' for git tag
if ($Version.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) {
    $Version = $Version.Substring(1)
}
$TagName = "v$Version"
Write-Host "Release Tag       : $TagName" -ForegroundColor Green

# -------------------------------------------------------------
# 5. Resolve Changelog / Description
# -------------------------------------------------------------
if ($ChangelogFile -and (Test-Path $ChangelogFile)) {
    $Changelog = (Get-Content $ChangelogFile -Raw).Trim()
    Write-Host "Loaded changelog from $ChangelogFile" -ForegroundColor Gray
} elseif (-not $Changelog -or -not $Changelog.Trim()) {
    Write-Host "`nEnter changelog notes (type END on a blank line when done):" -ForegroundColor Cyan
    $lines = @()
    while ($true) {
        $inputLine = [System.Console]::ReadLine()
        if ($inputLine -eq "END") { break }
        $lines += $inputLine
    }
    $Changelog = ($lines -join "`n").Trim()
    if (-not $Changelog) {
        $Changelog = "Release $TagName"
    }
}

# -------------------------------------------------------------
# 6. Synchronize Version Across Project Files
# -------------------------------------------------------------
Write-Host "`n--- Synchronizing Version to $Version ---" -ForegroundColor Cyan

# 6a. manifest.json
if (Test-Path $manifestPath) {
    $content = Get-Content $manifestPath -Raw
    $updated = $content -replace '("version_number"\s*:\s*")[^"]+(")', "`$1$Version`$2"
    [System.IO.File]::WriteAllText($manifestPath, $updated, [System.Text.Encoding]::UTF8)
    Write-Host "[OK] Updated manifest.json" -ForegroundColor Green
}

# 6b. ValheimPipes.csproj
$csprojPath = "$RepoRoot/ValheimPipes/ValheimPipes.csproj"
if (Test-Path $csprojPath) {
    $content = Get-Content $csprojPath -Raw
    $updated = $content -replace '(<Version>)[^<]+(</Version>)', "`$1$Version`$2"
    [System.IO.File]::WriteAllText($csprojPath, $updated, [System.Text.Encoding]::UTF8)
    Write-Host "[OK] Updated ValheimPipes.csproj" -ForegroundColor Green
}

# 6c. Plugin.cs
$pluginPath = "$RepoRoot/ValheimPipes/Plugin.cs"
if (Test-Path $pluginPath) {
    $content = Get-Content $pluginPath -Raw
    $updated = $content -replace '(public const string ModVersion = ")[^"]+(";)', "`$1$Version`$2"
    [System.IO.File]::WriteAllText($pluginPath, $updated, [System.Text.Encoding]::UTF8)
    Write-Host "[OK] Updated Plugin.cs ModVersion" -ForegroundColor Green
}

# 6d. CHANGELOG.md
$changelogPath = "$RepoRoot/CHANGELOG.md"
if (Test-Path $changelogPath) {
    $clContent = Get-Content $changelogPath -Raw
    if ($clContent -notmatch "(?m)^$([regex]::Escape($Version))\b") {
        $newSection = "$Version`n$Changelog`n`n"
        if ($clContent -match "(?m)^# Changelog\s*") {
            $updatedCl = $clContent -replace "(?m)^# Changelog\s*", "# Changelog`n`n$newSection"
        } else {
            $updatedCl = "# Changelog`n`n$newSection$clContent"
        }
        [System.IO.File]::WriteAllText($changelogPath, $updatedCl, [System.Text.Encoding]::UTF8)
        Write-Host "[OK] Updated CHANGELOG.md" -ForegroundColor Green
    }
}

# -------------------------------------------------------------
# 7. Build Project and Package Release Zip
# -------------------------------------------------------------
Write-Host "`n--- Compiling Project ---" -ForegroundColor Cyan
& dotnet build "$RepoRoot/ValheimPipes/ValheimPipes.csproj" -c Debug
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet build failed with code $LASTEXITCODE. Release aborted."
    exit $LASTEXITCODE
}

Write-Host "`n--- Packaging Distribution Files (deploy.ps1) ---" -ForegroundColor Cyan
& powershell -ExecutionPolicy Bypass -File "$RepoRoot/deploy.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Error "deploy.ps1 failed with code $LASTEXITCODE. Release aborted."
    exit $LASTEXITCODE
}

$DistZip = "$RepoRoot/dist/ValheimPipes.zip"
if (-not (Test-Path $DistZip)) {
    Write-Error "Expected release asset not found: $DistZip"
    exit 1
}

$zipInfo = Get-Item $DistZip
Write-Host "[OK] Verified asset: $($zipInfo.Name) ($([math]::Round($zipInfo.Length / 1KB, 1)) KB)" -ForegroundColor Green

# -------------------------------------------------------------
# 8. Git Commit, Tag & Push
# -------------------------------------------------------------
if (-not $NoPush) {
    Write-Host "`n--- Git Commit & Tag ---" -ForegroundColor Cyan
    git add manifest.json ValheimPipes/ValheimPipes.csproj ValheimPipes/Plugin.cs CHANGELOG.md .gitignore
    
    # Check if there are staged changes
    git diff --cached --quiet
    if ($LASTEXITCODE -ne 0) {
        git commit -m "Release $TagName"
        Write-Host "[OK] Committed version bump to $TagName" -ForegroundColor Green
    } else {
        Write-Host "No version changes needed committing." -ForegroundColor Gray
    }

    # Tagging
    git rev-parse -q --verify "refs/tags/$TagName" > $null
    if ($LASTEXITCODE -ne 0) {
        git tag -a "$TagName" -m "Release $TagName"
        Write-Host "[OK] Created git tag $TagName" -ForegroundColor Green
    } else {
        Write-Host "Git tag $TagName already exists locally." -ForegroundColor Yellow
    }

    Write-Host "Pushing commit and tag $TagName to origin..." -ForegroundColor Cyan
    git push origin HEAD
    git push origin "$TagName"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "git push failed with code $LASTEXITCODE. Aborting GitHub Release creation."
        exit $LASTEXITCODE
    }
}

# -------------------------------------------------------------
# 9. Create or Update GitHub Release via REST API
# -------------------------------------------------------------
Write-Host "`n--- Creating GitHub Release ---" -ForegroundColor Cyan

$apiBase = "https://api.github.com/repos/$Owner/$Repo"
$apiHeaders = @{
    "Authorization"        = "Bearer $Token"
    "Accept"               = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
    "User-Agent"           = "ValheimPipes-Release-Script"
}

# Helper to read error responses
function Get-ApiErrorMessage($ex) {
    if ($ex.Response) {
        try {
            $stream = $ex.Response.GetResponseStream()
            if ($stream) {
                $reader = [System.IO.StreamReader]::new($stream)
                return $reader.ReadToEnd()
            }
        } catch { }
    }
    return $ex.Message
}

$release = $null

# Check if release already exists
try {
    $existing = Invoke-RestMethod -Uri "$apiBase/releases/tags/$TagName" -Method Get -Headers $apiHeaders -ErrorAction Stop
    if ($existing -and $existing.id) {
        Write-Warning "A release with tag '$TagName' already exists on GitHub ($($existing.html_url))."
        $confirmReplace = Read-Host "Do you want to re-upload asset to this existing release? (y/N)"
        if ($confirmReplace -notmatch '^[yY]') {
            Write-Host "Aborted." -ForegroundColor Yellow
            exit 0
        }
        $release = $existing

        # If asset already exists, delete it so we can re-upload fresh
        if ($release.assets) {
            foreach ($asset in $release.assets) {
                if ($asset.name -eq $zipInfo.Name) {
                    Write-Host "Removing previous asset '$($zipInfo.Name)' from release..." -ForegroundColor Yellow
                    Invoke-RestMethod -Uri "$apiBase/releases/assets/$($asset.id)" -Method Delete -Headers $apiHeaders
                    break
                }
            }
        }
    }
} catch {
    # 404 is expected when release doesn't exist yet
}

if (-not $release) {
    $releasePayload = @{
        tag_name         = $TagName
        target_commitish = $currentBranch
        name             = $TagName
        body             = $Changelog
        draft            = [bool]$Draft
        prerelease       = [bool]$Prerelease
    } | ConvertTo-Json -Depth 5

    try {
        $release = Invoke-RestMethod -Uri "$apiBase/releases" -Method Post -Headers $apiHeaders -Body $releasePayload -ContentType "application/json; charset=utf-8"
        Write-Host "[OK] Release created: $($release.html_url)" -ForegroundColor Green
    } catch {
        $errMsg = Get-ApiErrorMessage $_.Exception
        Write-Error "Failed to create GitHub release: $errMsg"
        exit 1
    }
}

# -------------------------------------------------------------
# 10. Upload dist/ValheimPipes.zip to Release
# -------------------------------------------------------------
Write-Host "`n--- Uploading Asset ($($zipInfo.Name)) ---" -ForegroundColor Cyan

$uploadTemplate = $release.upload_url
$uploadUrl = ($uploadTemplate -replace '\{\?name,label\}', '') + "?name=$($zipInfo.Name)"

$uploadHeaders = @{
    "Authorization"        = "Bearer $Token"
    "Accept"               = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
    "User-Agent"           = "ValheimPipes-Release-Script"
    "Content-Type"         = "application/zip"
}

try {
    $uploadedAsset = Invoke-RestMethod -Uri $uploadUrl -Method Post -Headers $uploadHeaders -InFile $DistZip
    Write-Host "[OK] Asset uploaded successfully!" -ForegroundColor Green
    Write-Host "Asset Download URL: $($uploadedAsset.browser_download_url)" -ForegroundColor Gray
} catch {
    $errMsg = Get-ApiErrorMessage $_.Exception
    Write-Error "Failed to upload asset: $errMsg"
    exit 1
}

# -------------------------------------------------------------
# Summary
# -------------------------------------------------------------
Write-Host "`n=======================================================" -ForegroundColor Green
Write-Host "       Release $TagName successfully published!         " -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green
Write-Host "Release URL : $($release.html_url)" -ForegroundColor Cyan
Write-Host "Asset       : $($uploadedAsset.browser_download_url)" -ForegroundColor Cyan
Write-Host "Version     : $Version" -ForegroundColor Gray
Write-Host "Tag         : $TagName" -ForegroundColor Gray
