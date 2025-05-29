#!/bin/bash

# Production Deployment Test Script for CrowdQR
# This script tests a complete deployment from scratch

set -e

echo "🚀 Starting CrowdQR Production Deployment Test"
echo "=============================================="

# Configuration
PROJECT_NAME="crowdqr"
COMPOSE_FILE="docker-compose.yml"
ENV_FILE=".env"
TEST_TIMEOUT=300  # 5 minutes

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Utility functions
log_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

log_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

log_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

log_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed"
        exit 1
    fi
    
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        log_error "Docker Compose is not installed"
        exit 1
    fi
    
    if ! command -v curl &> /dev/null; then
        log_error "curl is not installed"
        exit 1
    fi
    
    log_success "Prerequisites check passed"
}

# Clean up any existing deployment
cleanup_existing() {
    log_info "Cleaning up any existing deployment..."
    
    docker-compose -f $COMPOSE_FILE down --volumes --remove-orphans 2>/dev/null || true
    docker system prune -f 2>/dev/null || true
    
    log_success "Cleanup completed"
}

# Validate configuration files
validate_config() {
    log_info "Validating configuration files..."
    
    if [ ! -f "$COMPOSE_FILE" ]; then
        log_error "docker-compose.yml not found"
        exit 1
    fi
    
    if [ ! -f "$ENV_FILE" ]; then
        log_error "$ENV_FILE not found"
        exit 1
    fi
    
    # Validate docker-compose syntax
    if ! docker-compose -f $COMPOSE_FILE config >/dev/null 2>&1; then
        log_error "Invalid docker-compose.yml syntax"
        exit 1
    fi
    
    log_success "Configuration validation passed"
}

# Build production images
build_images() {
    log_info "Building production images..."
    
    export ASPNETCORE_ENVIRONMENT=Production
    
    if ! docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE build --no-cache; then
        log_error "Image build failed"
        exit 1
    fi
    
    log_success "Images built successfully"
}

# Start services
start_services() {
    log_info "Starting services..."
    
    export ASPNETCORE_ENVIRONMENT=Production
    
    if ! docker-compose -f $COMPOSE_FILE --env-file $ENV_FILE up -d; then
        log_error "Failed to start services"
        exit 1
    fi
    
    log_success "Services started"
}

# Wait for services to be healthy
wait_for_health() {
    log_info "Waiting for services to become healthy..."
    
    local start_time=$(date +%s)
    local timeout=$TEST_TIMEOUT
    
    while true; do
        local current_time=$(date +%s)
        local elapsed=$((current_time - start_time))
        
        if [ $elapsed -gt $timeout ]; then
            log_error "Health check timeout after ${timeout}s"
            show_logs
            exit 1
        fi
        
        # Check database health
        if docker-compose -f $COMPOSE_FILE exec -T db pg_isready -U crowdqr_prod -d crowdqr_production >/dev/null 2>&1; then
            log_success "Database is healthy"
            break
        fi
        
        echo -n "."
        sleep 5
    done
    
    # Wait for API health
    log_info "Waiting for API to be healthy..."
    start_time=$(date +%s)
    
    while true; do
        local current_time=$(date +%s)
        local elapsed=$((current_time - start_time))
        
        if [ $elapsed -gt $timeout ]; then
            log_error "API health check timeout after ${timeout}s"
            show_logs
            exit 1
        fi
        
        if curl -f http://localhost:5000/health >/dev/null 2>&1; then
            log_success "API is healthy"
            break
        fi
        
        echo -n "."
        sleep 5
    done
    
    # Wait for Web health
    log_info "Waiting for Web app to be healthy..."
    start_time=$(date +%s)
    
    while true; do
        local current_time=$(date +%s)
        local elapsed=$((current_time - start_time))
        
        if [ $elapsed -gt $timeout ]; then
            log_error "Web app health check timeout after ${timeout}s"
            show_logs
            exit 1
        fi
        
        if curl -f http://localhost:8080/health >/dev/null 2>&1; then
            log_success "Web app is healthy"
            break
        fi
        
        echo -n "."
        sleep 5
    done
}

# Test basic functionality
test_functionality() {
    log_info "Testing basic functionality..."
    
    # Test API endpoints
    log_info "Testing API endpoints..."
    
    if ! curl -f http://localhost:5000/health >/dev/null 2>&1; then
        log_error "API health endpoint failed"
        exit 1
    fi
    
    if ! curl -f http://localhost:5000/api/event >/dev/null 2>&1; then
        log_error "API events endpoint failed"
        exit 1
    fi
    
    # Test Web app
    log_info "Testing Web application..."
    
    if ! curl -f http://localhost:8080 >/dev/null 2>&1; then
        log_error "Web app root endpoint failed"
        exit 1
    fi
    
    if ! curl -f http://localhost:8080/health >/dev/null 2>&1; then
        log_error "Web app health endpoint failed"
        exit 1
    fi
    
    log_success "Basic functionality tests passed"
}

# Show service logs for debugging
show_logs() {
    log_info "Showing service logs for debugging..."
    echo "=== DATABASE LOGS ==="
    docker-compose -f $COMPOSE_FILE logs --tail=50 db
    echo "=== API LOGS ==="
    docker-compose -f $COMPOSE_FILE logs --tail=50 api
    echo "=== WEB LOGS ==="
    docker-compose -f $COMPOSE_FILE logs --tail=50 web
}

# Show deployment summary
show_summary() {
    log_info "Deployment Summary"
    echo "=================="
    
    echo "Services Status:"
    docker-compose -f $COMPOSE_FILE ps
    
    echo ""
    echo "Service URLs:"
    echo "  🌐 Web Application: http://localhost:8080"
    echo "  🔧 API: http://localhost:5000"
    echo "  💾 Database: localhost:5432"
    
    echo ""
    echo "Health Check URLs:"
    echo "  📊 API Health: http://localhost:5000/health"
    echo "  📊 Web Health: http://localhost:8080/health"
    
    echo ""
    echo "Container Images:"
    docker images | grep crowdqr || echo "  No CrowdQR images found"
}

# Cleanup function for script exit
cleanup_on_exit() {
    if [ "$1" != "0" ]; then
        log_error "Deployment test failed!"
        show_logs
    fi
}

# Set trap for cleanup
trap 'cleanup_on_exit $?' EXIT

# Main execution
main() {
    log_info "Starting CrowdQR Production Deployment Test"
    
    check_prerequisites
    cleanup_existing
    validate_config
    build_images
    start_services
    wait_for_health
    test_functionality
    show_summary
    
    log_success "🎉 Deployment test completed successfully!"
    echo ""
    log_info "To stop the deployment, run: docker-compose -f $COMPOSE_FILE down"
    log_info "To view logs, run: docker-compose -f $COMPOSE_FILE logs -f"
}

# Execute main function
main "$@"