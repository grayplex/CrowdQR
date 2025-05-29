#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs all tests for the CrowdQR API project with coverage reporting.

.DESCRIPTION
    This script runs unit tests, integration tests, and generates code coverage reports
    for the CrowdQR API project. It supports different configurations and output options.

.PARAMETER Configuration
    The build configuration to use (Debug or Release). Default is Debug.

.PARAMETER Coverage
    Whether to collect code coverage. Default is true.

.PARAMETER Output
    The output directory for test results and coverage reports. Default is ./TestResults.

.PARAMETER Framework
    The target framework to test. Default is net8.0.

.PARAMETER Verbosity
    The verbosity level for test output. Default is normal.

.PARAMETER Filter
    Test filter expression to run specific tests.

.EXAMPLE
    ./run-tests.ps1
    Runs all tests with default settings.

.EXAMPLE
    ./run-tests.ps1 -Configuration Release -Coverage $true -Output "./coverage-results"
    Runs tests in Release mode with coverage reporting to a custom directory.

.EXAMPLE  
    ./run-tests.ps1 -Filter "FullyQualifiedName~VoteController"
    Runs only tests containing "VoteController" in their name.
#>

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    
    [bool]$Coverage = $true,
    
    [string]$Output = './TestResults',
    
    [string]$Framework = 'net8.0',
    
    [ValidateSet('quiet', 'minimal', 'normal', 'detailed', 'diagnostic')]
    [string]$Verbosity = 'normal',
    
    [string]$Filter = ''
)

# Set error action preference
$ErrorActionPreference = 'Stop'

# Colors for output
$Green = "`e[32m"
$Red = "`e[31m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Reset = "`e[0m"

function Write-Info {
    param([string]$Message)
    Write-Host "${Blue}ℹ️  $Message${Reset}"
}

function Write-Success {
    param([string]$Message)
    Write-Host "${Green}✅ $Message${Reset}"
}

function Write-Warning {
    param([string]$Message)
    Write-Host "${Yellow}⚠️  $Message${Reset}"
}

function Write-Error {
    param([string]$Message)
    Write-Host "${Red}❌ $Message${Reset}"
}

function Write-Header {
    param([string]$Title)
    Write-Host ""
    Write-Host "${Blue}===========================================${Reset}"
    Write-Host "${Blue}  $Title${Reset}"
    Write-Host "${Blue}===========================================${Reset}"
    Write-Host ""
}

# Start script
Write-Header "CrowdQR API Test Runner"

Write-Info "Configuration: $Configuration"
Write-Info "Coverage Collection: $Coverage"
Write-Info "Output Directory: $Output"
Write-Info "Target Framework: $Framework"
Write-Info "Verbosity: $Verbosity"

if ($Filter) {
    Write-Info "Test Filter: $Filter"
}

# Clean up previous results
Write-Info "Cleaning up previous test results..."
if (Test-Path $Output) {
    Remove-Item -Recurse -Force $Output
    Write-Success "Cleaned up $Output"
}

# Restore packages
Write-Header "Restoring NuGet Packages"
try {
    dotnet restore
    Write-Success "Package restoration completed"
}
catch {
    Write-Error "Package restoration failed: $_"
    exit 1
}

# Build solution
Write-Header "Building Solution"
try {
    dotnet build --configuration $Configuration --no-restore --verbosity $Verbosity
    Write-Success "Build completed successfully"
}
catch {
    Write-Error "Build failed: $_"
    exit 1
}

# Prepare test command
$testArgs = @(
    'test'
    'tests/CrowdQR.API.Tests/CrowdQR.Api.Tests.csproj'
    '--configuration', $Configuration
    '--framework', $Framework
    '--no-build'
    '--verbosity', $Verbosity
    '--results-directory', $Output
    '--logger', 'trx'
    '--logger', 'console;verbosity=normal'
)

# Add test filter if specified
if ($Filter) {
    $testArgs += '--filter', $Filter
}

# Add coverage collection if enabled
if ($Coverage) {
    $testArgs += '--collect', 'XPlat Code Coverage'
    $testArgs += '--settings', 'tests/CrowdQR.API.Tests/coverlet.runsettings'
}

# Run tests
Write-Header "Running Tests"
try {
    Write-Info "Executing: dotnet $($testArgs -join ' ')"
    & dotnet @testArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "All tests passed!"
    } else {
        Write-Error "Some tests failed. Exit code: $LASTEXITCODE"
        # Don't exit here, we still want to generate coverage report
    }
}
catch {
    Write-Error "Test execution failed: $_"
    exit 1
}

# Generate coverage report if coverage was collected
if ($Coverage) {
    Write-Header "Generating Coverage Report"
    
    # Find coverage files
    $coverageFiles = Get-ChildItem -Path $Output -Filter "coverage.cobertura.xml" -Recurse
    
    if ($coverageFiles.Count -eq 0) {
        Write-Warning "No coverage files found"
    } else {
        Write-Info "Found $($coverageFiles.Count) coverage file(s)"
        
        # Check if reportgenerator is installed
        $reportGenerator = Get-Command 'reportgenerator' -ErrorAction SilentlyContinue
        
        if (-not $reportGenerator) {
            Write-Info "Installing ReportGenerator tool..."
            try {
                dotnet tool install --global dotnet-reportgenerator-globaltool
                Write-Success "ReportGenerator installed successfully"
            }
            catch {
                Write-Warning "Failed to install ReportGenerator: $_"
                Write-Warning "Install manually with: dotnet tool install --global dotnet-reportgenerator-globaltool"
            }
        }
        
        # Generate HTML report
        $reportDir = Join-Path $Output "coverage-report"
        $coverageFileArgs = ($coverageFiles | ForEach-Object { $_.FullName }) -join ';'

        try {
            Write-Info "Generating HTML coverage report..."
                reportgenerator -reports:$coverageFileArgs -targetdir:$reportDir -reporttypes:Html
            Write-Success "Coverage report generated at: $reportDir"
            Write-Info "Open $reportDir/index.html to view the coverage report"
        }
        catch {
            Write-Warning "Failed to generate coverage report: $_"
        }
        
        # Generate console summary
        try {
            Write-Info "Generating coverage summary..."
                reportgenerator -reports:$coverageFileArgs -targetdir:$reportDir -reporttypes:TextSummary
            
            $summaryFile = Join-Path $reportDir "Summary.txt"
            if (Test-Path $summaryFile) {
                Write-Header "Coverage Summary"
                Get-Content $summaryFile | Write-Host
            }
        }
        catch {
            Write-Warning "Failed to generate coverage summary: $_"
        }
    }
}

# Display test results summary
Write-Header "Test Results Summary"

$trxFiles = Get-ChildItem -Path $Output -Filter "*.trx" -Recurse
if ($trxFiles.Count -gt 0) {
    Write-Info "Test result files:"
    $trxFiles | ForEach-Object { Write-Info "  $($_.FullName)" }
} else {
    Write-Warning "No test result files found"
}

# Check for test failures and exit accordingly
if ($LASTEXITCODE -ne 0) {
    Write-Error "Tests completed with failures. Check the output above for details."
    exit 1
} else {
    Write-Success "All tests completed successfully!"
    
    if ($Coverage) {
        $reportPath = Join-Path $Output "coverage-report/index.html"
        if (Test-Path $reportPath) {
            Write-Info "📊 Coverage report: $reportPath"
        }
    }
    
    Write-Success "🎉 Test run completed successfully!"
}

exit 0