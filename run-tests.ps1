#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs unit tests for the CrowdQR API project with coverage reporting.

.DESCRIPTION
    This script runs all unit tests in the CrowdQR.API.Tests project and generates
    code coverage reports. It can be used locally or in CI/CD pipelines.

.PARAMETER Configuration
    The build configuration to use (Debug or Release). Default is Debug.

.PARAMETER Coverage
    Whether to collect code coverage. Default is true.

.PARAMETER Output
    Output directory for test results and coverage reports.

.EXAMPLE
    ./run-tests.ps1
    
.EXAMPLE
    ./run-tests.ps1 -Configuration Release -Coverage $true

.EXAMPLE
    ./run-tests.ps1 -Output "./test-results"
#>

param(
    [Parameter()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    
    [Parameter()]
    [bool]$Coverage = $true,
    
    [Parameter()]
    [string]$Output = "./TestResults"
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get the script directory and project root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$TestProject = Join-Path $ProjectRoot "tests/CrowdQR.API.Tests/CrowdQR.Api.Tests.csproj"

Write-Host "🧪 Running CrowdQR API Tests" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Coverage: $Coverage" -ForegroundColor Gray
Write-Host "Output: $Output" -ForegroundColor Gray
Write-Host ""

# Ensure output directory exists
if (-not (Test-Path $Output)) {
    New-Item -ItemType Directory -Path $Output -Force | Out-Null
}

try {
    # Restore dependencies
    Write-Host "📦 Restoring dependencies..." -ForegroundColor Yellow
    dotnet restore $ProjectRoot
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore dependencies"
    }

    # Build the test project
    Write-Host "🔨 Building test project..." -ForegroundColor Yellow
    dotnet build $TestProject --configuration $Configuration --no-restore
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build test project"
    }

    # Prepare test command
    $TestCommand = @(
        "test"
        $TestProject
        "--configuration", $Configuration
        "--no-build"
        "--verbosity", "normal"
        "--results-directory", $Output
        "--logger", "trx"
        "--logger", "console;verbosity=detailed"
    )

    # Add coverage collection if requested
    if ($Coverage) {
        Write-Host "📊 Code coverage enabled" -ForegroundColor Yellow
        $TestCommand += @(
            "--collect", "XPlat Code Coverage"
            "--settings", (Join-Path $ProjectRoot "tests/CrowdQR.API.Tests/coverlet.runsettings")
        )
    }

    # Run the tests
    Write-Host "🏃 Running tests..." -ForegroundColor Yellow
    Write-Host "Command: dotnet $($TestCommand -join ' ')" -ForegroundColor Gray
    Write-Host ""
    
    & dotnet @TestCommand
    
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed with exit code $LASTEXITCODE"
    }

    Write-Host ""
    Write-Host "✅ All tests passed!" -ForegroundColor Green

    # Generate coverage report if coverage was collected
    if ($Coverage) {
        Write-Host ""
        Write-Host "📈 Generating coverage report..." -ForegroundColor Yellow
        
        # Find the coverage file
        $CoverageFiles = Get-ChildItem -Path $Output -Recurse -Filter "coverage.cobertura.xml"
        
        if ($CoverageFiles.Count -gt 0) {
            $CoverageFile = $CoverageFiles[0].FullName
            Write-Host "Coverage file: $CoverageFile" -ForegroundColor Gray
            
            # Install reportgenerator tool if not present
            $ReportGeneratorVersion = "5.1.26"
            try {
                dotnet tool install --global dotnet-reportgenerator-globaltool --version $ReportGeneratorVersion 2>$null
            } catch {
                # Tool might already be installed
            }
            
            # Generate HTML report
            $ReportOutput = Join-Path $Output "coverage-report"
            dotnet reportgenerator -reports:$CoverageFile -targetdir:$ReportOutput -reporttypes:"Html;TextSummary"
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "📄 Coverage report generated: $ReportOutput/index.html" -ForegroundColor Green
                
                # Display text summary
                $SummaryFile = Join-Path $ReportOutput "Summary.txt"
                if (Test-Path $SummaryFile) {
                    Write-Host ""
                    Write-Host "📊 Coverage Summary:" -ForegroundColor Cyan
                    Get-Content $SummaryFile | Write-Host
                }
            } else {
                Write-Warning "Failed to generate coverage report"
            }
        } else {
            Write-Warning "No coverage files found"
        }
    }

    # Display test results summary
    Write-Host ""
    Write-Host "📋 Test Results:" -ForegroundColor Cyan
    
    $TrxFiles = Get-ChildItem -Path $Output -Filter "*.trx" -Recurse
    foreach ($TrxFile in $TrxFiles) {
        Write-Host "  - $($TrxFile.Name)" -ForegroundColor Gray
    }

    Write-Host ""
    Write-Host "🎉 Test run completed successfully!" -ForegroundColor Green

} catch {
    Write-Host ""
    Write-Host "❌ Test run failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}