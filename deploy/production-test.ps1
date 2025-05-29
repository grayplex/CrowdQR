<#
.SYNOPSIS
    Production Deployment Test Script for CrowdQR
    This script tests a complete deployment from scratch

.DESCRIPTION
    Tests a complete CrowdQR deployment including building images, starting services,
    waiting for health checks, and validating basic functionality.

.PARAMETER CleanUp
    Whether to clean up existing deployment before starting. Default is true.

.PARAMETER Timeout
    Timeout in seconds for health checks. Default is 300 (5 minutes).

.PARAMETER SkipBuild
    Skip building images and use existing ones. Default is false.

.EXAMPLE
    ./production-test.ps1
    Runs the complete deployment test with default settings.

.EXAMPLE
    ./production-test.ps1 -Timeout 600 -SkipBuild
    Runs with 10-minute timeout and skips building images.
#>

param(
    [bool]$CleanUp = $true,
    [int]$Timeout = 300,
    [bool]$SkipBuild = $false
)

# Set error action preference
$ErrorActionPreference = 'Stop'

Write-Host "🚀 Starting CrowdQR Production Deployment Test" -ForegroundColor Blue
Write-Host "==============================================" -ForegroundColor Blue

# Configuration
$ProjectName = "crowdqr"
$ComposeFile = "docker-compose.yml"
$EnvFile = ".env"
$TestTimeout = $Timeout

# Colors for output
$Colors = @{
    Red = "Red"
    Green = "Green"
    Yellow = "Yellow"
    Blue = "Blue"
    Cyan = "Cyan"
}

# Utility functions
function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor $Colors.Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor $Colors.Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor $Colors.Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor $Colors.Red
}

# Check prerequisites
function Test-Prerequisites {
    Write-Info "Checking prerequisites..."
    
    # Check Docker
    try {
        $null = Get-Command docker -ErrorAction Stop
    }
    catch {
        Write-Error "Docker is not installed or not in PATH"
        exit 1
    }
    
    # Check Docker Compose (try both docker-compose and docker compose)
    $dockerComposeAvailable = $false
    try {
        $null = Get-Command docker-compose -ErrorAction Stop
        $dockerComposeAvailable = $true
    }
    catch {
        try {
            docker compose version *>$null
            $dockerComposeAvailable = $true
        }
        catch {
            # Will check later
        }
    }
    
    if (-not $dockerComposeAvailable) {
        Write-Error "Docker Compose is not installed or not available"
        exit 1
    }
    
    # Check curl (optional, will use Invoke-WebRequest as fallback)
    try {
        $null = Get-Command curl -ErrorAction Stop
        $script:UseCurl = $true
    }
    catch {
        Write-Warning "curl not found, will use Invoke-WebRequest"
        $script:UseCurl = $false
    }
    
    Write-Success "Prerequisites check passed"
}

# Test URL availability
function Test-Url {
    param([string]$Url)
    
    if ($script:UseCurl) {
        try {
            curl -f $Url 2>$null | Out-Null
            return $true
        }
        catch {
            return $false
        }
    }
    else {
        try {
            $response = Invoke-WebRequest -Uri $Url -Method Get -UseBasicParsing -ErrorAction Stop
            return $response.StatusCode -eq 200
        }
        catch {
            return $false
        }
    }
}

# Clean up any existing deployment
function Remove-ExistingDeployment {
    if (-not $CleanUp) {
        Write-Info "Skipping cleanup as requested"
        return
    }
    
    Write-Info "Cleaning up any existing deployment..."
    
    try {
        docker-compose -f $ComposeFile down --volumes --remove-orphans 2>$null
    }
    catch {
        # Ignore errors during cleanup
    }
    
    try {
        docker system prune -f 2>$null
    }
    catch {
        # Ignore errors during cleanup
    }
    
    Write-Success "Cleanup completed"
}

# Validate configuration files
function Test-ConfigurationFiles {
    Write-Info "Validating configuration files..."
    
    if (-not (Test-Path $ComposeFile)) {
        Write-Error "docker-compose.yml not found"
        exit 1
    }
    
    if (-not (Test-Path $EnvFile)) {
        Write-Error "$EnvFile not found"
        exit 1
    }
    
    # Validate docker-compose syntax
    try {
        docker-compose -f $ComposeFile config 2>$null | Out-Null
    }
    catch {
        Write-Error "Invalid docker-compose.yml syntax"
        exit 1
    }
    
    Write-Success "Configuration validation passed"
}

# Build production images
function Build-ProductionImages {
    if ($SkipBuild) {
        Write-Info "Skipping image build as requested"
        return
    }
    
    Write-Info "Building production images..."
    
    $env:ASPNETCORE_ENVIRONMENT = "Production"
    
    try {
        docker-compose -f $ComposeFile --env-file $EnvFile build --no-cache
        if ($LASTEXITCODE -ne 0) {
            throw "Docker build failed with exit code $LASTEXITCODE"
        }
    }
    catch {
        Write-Error "Image build failed: $_"
        exit 1
    }
    
    Write-Success "Images built successfully"
}

# Start services
function Start-Services {
    Write-Info "Starting services..."
    
    $env:ASPNETCORE_ENVIRONMENT = "Production"
    
    try {
        docker-compose -f $ComposeFile --env-file $EnvFile up -d
        if ($LASTEXITCODE -ne 0) {
            throw "Docker compose up failed with exit code $LASTEXITCODE"
        }
    }
    catch {
        Write-Error "Failed to start services: $_"
        exit 1
    }
    
    Write-Success "Services started"
}

# Wait for services to be healthy
function Wait-ForHealthyServices {
    Write-Info "Waiting for services to become healthy..."
    
    $startTime = Get-Date
    
    # Wait for database health
    Write-Info "Waiting for database to be ready..."
    while ($true) {
        $elapsed = (Get-Date) - $startTime
        
        if ($elapsed.TotalSeconds -gt $TestTimeout) {
            Write-Error "Health check timeout after $($TestTimeout)s"
            Show-ServiceLogs
            exit 1
        }
        
        try {
            docker-compose -f $ComposeFile exec -T db pg_isready -U $env:POSTGRES_USER -d $env:POSTGRES_DB 2>$null | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Database is healthy"
                break
            }
        }
        catch {
            # Continue waiting
        }
        
        Write-Host "." -NoNewline
        Start-Sleep -Seconds 5
    }
    
    # Wait for API health
    Write-Info "Waiting for API to be healthy..."
    $startTime = Get-Date
    
    while ($true) {
        $elapsed = (Get-Date) - $startTime
        
        if ($elapsed.TotalSeconds -gt $TestTimeout) {
            Write-Error "API health check timeout after $($TestTimeout)s"
            Show-ServiceLogs
            exit 1
        }
        
        if (Test-Url "http://localhost:5000/health") {
            Write-Success "API is healthy"
            break
        }
        
        Write-Host "." -NoNewline
        Start-Sleep -Seconds 5
    }
    
    # Wait for Web health
    Write-Info "Waiting for Web app to be healthy..."
    $startTime = Get-Date
    
    while ($true) {
        $elapsed = (Get-Date) - $startTime
        
        if ($elapsed.TotalSeconds -gt $TestTimeout) {
            Write-Error "Web app health check timeout after $($TestTimeout)s"
            Show-ServiceLogs
            exit 1
        }
        
        if (Test-Url "http://localhost:8080/health") {
            Write-Success "Web app is healthy"
            break
        }
        
        Write-Host "." -NoNewline
        Start-Sleep -Seconds 5
    }
}

# Test basic functionality
function Test-BasicFunctionality {
    Write-Info "Testing basic functionality..."
    
    # Test API endpoints
    Write-Info "Testing API endpoints..."
    
    if (-not (Test-Url "http://localhost:5000/health")) {
        Write-Error "API health endpoint failed"
        exit 1
    }
    
    if (-not (Test-Url "http://localhost:5000/api/event")) {
        Write-Error "API events endpoint failed"
        exit 1
    }
    
    # Test Web app
    Write-Info "Testing Web application..."
    
    if (-not (Test-Url "http://localhost:8080")) {
        Write-Error "Web app root endpoint failed"
        exit 1
    }
    
    if (-not (Test-Url "http://localhost:8080/health")) {
        Write-Error "Web app health endpoint failed"
        exit 1
    }
    
    Write-Success "Basic functionality tests passed"
}

# Show service logs for debugging
function Show-ServiceLogs {
    Write-Info "Showing service logs for debugging..."
    
    Write-Host "=== DATABASE LOGS ===" -ForegroundColor $Colors.Cyan
    docker-compose -f $ComposeFile logs --tail=50 db
    
    Write-Host "=== API LOGS ===" -ForegroundColor $Colors.Cyan
    docker-compose -f $ComposeFile logs --tail=50 api
    
    Write-Host "=== WEB LOGS ===" -ForegroundColor $Colors.Cyan
    docker-compose -f $ComposeFile logs --tail=50 web
}

# Show deployment summary
function Show-DeploymentSummary {
    Write-Info "Deployment Summary"
    Write-Host "=================="
    
    Write-Host "Services Status:"
    docker-compose -f $ComposeFile ps
    
    Write-Host ""
    Write-Host "Service URLs:"
    Write-Host "  🌐 Web Application: http://localhost:8080"
    Write-Host "  🔧 API: http://localhost:5000"
    Write-Host "  💾 Database: localhost:5432"
    
    Write-Host ""
    Write-Host "Health Check URLs:"
    Write-Host "  📊 API Health: http://localhost:5000/health"
    Write-Host "  📊 Web Health: http://localhost:8080/health"
    
    Write-Host ""
    Write-Host "Container Images:"
    $crowdqrImages = docker images | Select-String "crowdqr"
    if ($crowdqrImages) {
        $crowdqrImages
    }
    else {
        Write-Host "  No CrowdQR images found"
    }
}

# Cleanup function for script exit
function Invoke-CleanupOnExit {
    param([int]$ExitCode)
    
    if ($ExitCode -ne 0) {
        Write-Error "Deployment test failed!"
        Show-ServiceLogs
    }
}

# Main execution function
function Invoke-Main {
    try {
        Write-Info "Starting CrowdQR Production Deployment Test"
        
        Test-Prerequisites
        Remove-ExistingDeployment
        Test-ConfigurationFiles
        Build-ProductionImages
        Start-Services
        Wait-ForHealthyServices
        Test-BasicFunctionality
        Show-DeploymentSummary
        
        Write-Success "🎉 Deployment test completed successfully!"
        Write-Host ""
        Write-Info "To stop the deployment, run: docker-compose -f $ComposeFile down"
        Write-Info "To view logs, run: docker-compose -f $ComposeFile logs -f"
        
        return 0
    }
    catch {
        Write-Error "Deployment test failed: $_"
        Show-ServiceLogs
        return 1
    }
}

# Load environment variables from .env file if it exists
if (Test-Path $EnvFile) {
    Get-Content $EnvFile | ForEach-Object {
        if ($_ -match '^([^#][^=]+)=(.*)$') {
            [System.Environment]::SetEnvironmentVariable($matches[1], $matches[2])
        }
    }
}

# Execute main function and capture exit code
$exitCode = Invoke-Main

# Cleanup on exit
Invoke-CleanupOnExit -ExitCode $exitCode

# Exit with the appropriate code
exit $exitCode