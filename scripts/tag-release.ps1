param (
    [string]$Version,
    [switch]$Push,
    [switch]$Latest,
    [string]$Registry = "ghcr.io",
    [string]$Repo = "grayplex/crowdqr",
    [switch]$DryRun,
    [switch]$Help
)

# Output styling
function Log-Info { Write-Host "ℹ️  $args" -ForegroundColor Cyan }
function Log-Success { Write-Host "✅ $args" -ForegroundColor Green }
function Log-Warning { Write-Host "⚠️  $args" -ForegroundColor Yellow }
function Log-Error { Write-Host "❌ $args" -ForegroundColor Red }

function Show-Usage {
    Write-Host @"
Usage: .\tag-docker.ps1 -Version <semver> [OPTIONS]

VERSION:
  Semantic version (e.g., 1.0.0, 1.2.3-beta.1)
  If not provided, auto-increments patch

OPTIONS:
  -Push             Push images to registry
  -Latest           Tag as 'latest'
  -Registry         Override default registry ($Registry)
  -Repo             Override default repository ($Repo)
  -DryRun           Only show actions, don't execute
  -Help             Show this message
"@
    exit 0
}

if ($Help) { Show-Usage }

function Get-LatestVersion {
    try {
        git describe --tags --abbrev=0 | ForEach-Object { $_ -replace '^v', '' }
    } catch {
        return "0.0.0"
    }
}

function Increment-Version($v) {
    $parts = $v.Split(".")
    $parts[2] = [int]$parts[2] + 1
    return "$($parts[0]).$($parts[1]).$($parts[2])"
}

function Validate-Version($v) {
    if ($v -notmatch '^(\d+)\.(\d+)\.(\d+)(-[\w.-]+)?(\+[\w.-]+)?$') {
        Log-Error "Invalid semantic version format: $v"
        return $false
    }
    return $true
}

function Check-GitStatus {
    $dirty = git status --porcelain
    if ($dirty) {
        Log-Warning "Git working directory is not clean."
        $resp = Read-Host "Continue anyway? (y/N)"
        if ($resp -notmatch '^[Yy]$') {
            Log-Error "Aborted by user."
            exit 1
        }
    }
}

function Build-And-TagImages($ver) {
    $services = @("api", "web")

    Log-Info "Building images with version $ver..."
    if ($DryRun) {
        Log-Info "DRY RUN: Would run docker-compose build"
    } else {
        $env:ASPNETCORE_ENVIRONMENT = "Production"
        docker-compose -f "docker-compose.yml" build --no-cache
    }

    foreach ($s in $services) {
        $imgName = "$Registry/$Repo-$s"
        $srcImage = "crowdqr-$s"

        if ($DryRun) {
            Log-Info "DRY RUN: Would tag $srcImage as ${imgName}:$ver"
            if ($Latest) { Log-Info "DRY RUN: Would also tag as latest" }
        } else {
            $imgID = docker images --format "{{.Repository}}:{{.Tag}}`t{{.ID}}" |
                Select-String "crowdqr[-_]$s" | Select-Object -First 1 |
                ForEach-Object { ($_ -split "`t")[1] }

            if (-not $imgID) {
                Log-Error "Could not find built image for $s"
                exit 1
            }

            docker tag $imgID "${imgName}:$ver"
            Log-Success "Tagged ${imgName}:$ver"

            if ($Latest) {
                docker tag $imgID "${imgName}:latest"
                Log-Success "Tagged ${imgName}:latest"
            }
        }
    }
}

function Push-Images($ver) {
    if (-not $Push) { return }

    $services = @("api", "web")
    Log-Info "Pushing images..."

    foreach ($s in $services) {
        $img = "$Registry/$Repo-$s"

        if ($DryRun) {
            Log-Info "DRY RUN: Would push ${img}:$ver"
            if ($Latest) { Log-Info "DRY RUN: Would push $img:latest" }
        } else {
            docker push "${img}:$ver"
            Log-Success "Pushed ${img}:$ver"
            if ($Latest) {
                docker push "${img}:latest"
                Log-Success "Pushed ${img}:latest"
            }
        }
    }
}

function Create-GitTag($ver) {
    $tag = "v$ver"

    if ($DryRun) {
        Log-Info "DRY RUN: Would tag Git as $tag"
        return
    }

    if (git tag -l | Select-String "^$tag$") {
        Log-Warning "Git tag $tag already exists."
        $resp = Read-Host "Overwrite? (y/N)"
        if ($resp -match '^[Yy]$') {
            git tag -d $tag
            Log-Info "Deleted existing tag $tag"
        } else {
            Log-Error "Aborted to avoid overwriting"
            exit 1
        }
    }

    git tag -a $tag -m "Release version $ver"
    Log-Success "Created git tag $tag"

    if ($Push) {
        $resp = Read-Host "Push tag to origin? (Y/n)"
        if ($resp -notmatch '^[Nn]$') {
            git push origin $tag
            Log-Success "Pushed tag $tag"
        }
    }
}

function Generate-ReleaseNotes($ver) {
    $prev = Get-LatestVersion
    $file = "release-notes-$ver.md"

    $header = @"
# CrowdQR Release $ver

## Docker Images
- \`$Registry/$Repo-api:$ver\`
- \`$Registry/$Repo-web:$ver\`

## Quick Start
\`\`\`bash
docker pull $Registry/$Repo-api:$ver
docker pull $Registry/$Repo-web:$ver
export CROWDQR_VERSION=$ver
docker-compose up -d
\`\`\`

## Changes Since $prev

"@

    Set-Content -Path $file -Value $header

    if ($prev -ne "0.0.0") {
        Add-Content -Path $file -Value "### Commits"
        git log --pretty=format:"- %s (%h)" "v$prev..HEAD" | Add-Content $file
        Add-Content -Path $file -Value ""
    }

    Add-Content -Path $file -Value @"
## Verification

\`\`\`bash
docker image inspect $Registry/$Repo-api:$ver
docker image inspect $Registry/$Repo-web:$ver
curl -f http://localhost:5000/health
curl -f http://localhost:8080/health
\`\`\`

---

**Full Changelog**: https://github.com/grayplex/crowdqr/compare/v$prev...v$ver
"@

    Log-Success "Generated release notes: $file"
}

# Main logic
if (-not $Version) {
    $Version = Increment-Version (Get-LatestVersion)
    Log-Info "Auto-incremented version to $Version"
}

if (-not (Validate-Version $Version)) {
    exit 1
}

Log-Info "Configuration:"
Write-Host "  Version: $Version"
Write-Host "  Registry: $Registry"
Write-Host "  Repository: $Repo"
Write-Host "  Push: $Push"
Write-Host "  Latest: $Latest"
Write-Host "  DryRun: $DryRun"
Write-Host ""

if (-not $DryRun) {
    $resp = Read-Host "Proceed with tagging $Version? (y/N)"
    if ($resp -notmatch '^[Yy]$') {
        Log-Error "Aborted by user."
        exit 1
    }
}

Check-GitStatus
Build-And-TagImages $Version
Push-Images $Version
Create-GitTag $Version
Generate-ReleaseNotes $Version

Log-Success "Tagged version $Version"
