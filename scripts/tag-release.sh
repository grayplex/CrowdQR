#!/bin/bash

# CrowdQR Docker Image Tagging Script
# Handles semantic versioning for Docker images

set -e

# Configuration
REGISTRY="ghcr.io"
REPOSITORY="grayplex/crowdqr"
COMPOSE_FILE="docker-compose.yml"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

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

# Display usage information
usage() {
    echo "Usage: $0 [VERSION] [OPTIONS]"
    echo ""
    echo "VERSION:"
    echo "  Semantic version (e.g., 1.0.0, 1.2.3-beta.1)"
    echo "  If not provided, will auto-increment patch version"
    echo ""
    echo "OPTIONS:"
    echo "  -p, --push         Push images to registry after tagging"
    echo "  -l, --latest       Also tag as 'latest'"
    echo "  -r, --registry     Specify registry (default: $REGISTRY)"
    echo "  -n, --repo         Specify repository (default: $REPOSITORY)"
    echo "  --dry-run          Show what would be done without executing"
    echo "  -h, --help         Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 1.0.0 --push --latest"
    echo "  $0 1.2.3-beta.1 --push"
    echo "  $0 --dry-run"
    echo ""
}

# Get the latest version from git tags
get_latest_version() {
   local latest_tag
   latest_tag=$(git describe --tags --abbrev=0 2>/dev/null || echo "0.0.0")
   echo "${latest_tag#v}"  # Remove 'v' prefix if present
}

# Increment version number
increment_version() {
   local version=$1
   local IFS='.'
   read -ra parts <<< "$version"
   
   # Increment patch version
   parts[2]=$((parts[2] + 1))
   
   echo "${parts[0]}.${parts[1]}.${parts[2]}"
}

# Validate semantic version format
validate_version() {
   local version=$1
   local semver_regex='^([0-9]+)\.([0-9]+)\.([0-9]+)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?(\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$'
   
   if [[ ! $version =~ $semver_regex ]]; then
       log_error "Invalid semantic version format: $version"
       echo "Expected format: MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]"
       echo "Examples: 1.0.0, 1.2.3-beta.1, 1.0.0+build.1"
       return 1
   fi
   
   return 0
}

# Check if git working directory is clean
check_git_status() {
   if [ -n "$(git status --porcelain)" ]; then
       log_warning "Git working directory is not clean"
       echo "Uncommitted changes detected. Consider committing or stashing changes."
       read -p "Continue anyway? (y/N): " -n 1 -r
       echo
       if [[ ! $REPLY =~ ^[Yy]$ ]]; then
           log_error "Aborted by user"
           exit 1
       fi
   fi
}

# Build images with version tags
build_and_tag_images() {
   local version=$1
   local services=("api" "web")
   
   log_info "Building images with version $version..."
   
   # Build base images first
   if [ "$DRY_RUN" = true ]; then
       log_info "DRY RUN: Would build images with docker-compose"
   else
       export ASPNETCORE_ENVIRONMENT=Production
       docker-compose -f "$COMPOSE_FILE" build --no-cache
   fi
   
   # Tag images with version
   for service in "${services[@]}"; do
       local image_name="${REGISTRY}/${REPOSITORY}-${service}"
       local source_image="crowdqr-${service}"
       
       log_info "Tagging $service image..."
       
       if [ "$DRY_RUN" = true ]; then
           log_info "DRY RUN: Would tag $source_image as:"
           echo "  - ${image_name}:${version}"
           [ "$TAG_LATEST" = true ] && echo "  - ${image_name}:latest"
       else
           # Get the actual image ID from docker-compose
           local image_id
           image_id=$(docker images --format "table {{.Repository}}:{{.Tag}}\t{{.ID}}" | grep "crowdqr[_-]${service}" | head -1 | awk '{print $2}')
           
           if [ -z "$image_id" ]; then
               log_error "Could not find built image for $service"
               exit 1
           fi
           
           # Tag with version
           docker tag "$image_id" "${image_name}:${version}"
           log_success "Tagged ${image_name}:${version}"
           
           # Tag with latest if requested
           if [ "$TAG_LATEST" = true ]; then
               docker tag "$image_id" "${image_name}:latest"
               log_success "Tagged ${image_name}:latest"
           fi
       fi
   done
}

# Push images to registry
push_images() {
   local version=$1
   local services=("api" "web")
   
   if [ "$PUSH_IMAGES" != true ]; then
       return 0
   fi
   
   log_info "Pushing images to registry..."
   
   # Login check
   if [ "$DRY_RUN" != true ]; then
       if ! docker info | grep -q "Username:"; then
           log_warning "Not logged into Docker registry"
           log_info "Please login with: docker login $REGISTRY"
           return 1
       fi
   fi
   
   for service in "${services[@]}"; do
       local image_name="${REGISTRY}/${REPOSITORY}-${service}"
       
       if [ "$DRY_RUN" = true ]; then
           log_info "DRY RUN: Would push:"
           echo "  - ${image_name}:${version}"
           [ "$TAG_LATEST" = true ] && echo "  - ${image_name}:latest"
       else
           # Push version tag
           log_info "Pushing ${image_name}:${version}..."
           docker push "${image_name}:${version}"
           log_success "Pushed ${image_name}:${version}"
           
           # Push latest tag if requested
           if [ "$TAG_LATEST" = true ]; then
               log_info "Pushing ${image_name}:latest..."
               docker push "${image_name}:latest"
               log_success "Pushed ${image_name}:latest"
           fi
       fi
   done
}

# Create git tag
create_git_tag() {
   local version=$1
   local tag_name="v${version}"
   
   if [ "$DRY_RUN" = true ]; then
       log_info "DRY RUN: Would create git tag: $tag_name"
       return 0
   fi
   
   # Check if tag already exists
   if git tag -l | grep -q "^${tag_name}$"; then
       log_warning "Git tag $tag_name already exists"
       read -p "Overwrite existing tag? (y/N): " -n 1 -r
       echo
       if [[ $REPLY =~ ^[Yy]$ ]]; then
           git tag -d "$tag_name"
           log_info "Deleted existing tag $tag_name"
       else
           log_error "Aborted to avoid overwriting existing tag"
           exit 1
       fi
   fi
   
   # Create annotated tag
   git tag -a "$tag_name" -m "Release version $version"
   log_success "Created git tag: $tag_name"
   
   # Ask about pushing tag
   if [ "$PUSH_IMAGES" = true ]; then
       read -p "Push git tag to origin? (Y/n): " -n 1 -r
       echo
       if [[ ! $REPLY =~ ^[Nn]$ ]]; then
           git push origin "$tag_name"
           log_success "Pushed git tag: $tag_name"
       fi
   fi
}

# Generate release notes
generate_release_notes() {
   local version=$1
   local previous_version
   
   previous_version=$(get_latest_version)
   
   if [ "$DRY_RUN" = true ]; then
       log_info "DRY RUN: Would generate release notes for $version"
       return 0
   fi
   
   local release_notes_file="release-notes-${version}.md"
   
   cat > "$release_notes_file" << EOF
# CrowdQR Release ${version}

## Docker Images

- \`${REGISTRY}/${REPOSITORY}-api:${version}\`
- \`${REGISTRY}/${REPOSITORY}-web:${version}\`

## Quick Start

\`\`\`bash
# Pull images
docker pull ${REGISTRY}/${REPOSITORY}-api:${version}
docker pull ${REGISTRY}/${REPOSITORY}-web:${version}

# Run with docker-compose
export CROWDQR_VERSION=${version}
docker-compose up -d
\`\`\`

## Changes Since ${previous_version}

EOF
   
   # Add git log changes if previous version exists
   if [ "$previous_version" != "0.0.0" ]; then
       echo "### Commits" >> "$release_notes_file"
       git log --pretty=format:"- %s (%h)" "v${previous_version}..HEAD" >> "$release_notes_file" 2>/dev/null || true
       echo "" >> "$release_notes_file"
   fi
   
   cat >> "$release_notes_file" << EOF

## Verification

\`\`\`bash
# Verify image integrity
docker image inspect ${REGISTRY}/${REPOSITORY}-api:${version}
docker image inspect ${REGISTRY}/${REPOSITORY}-web:${version}

# Health checks
curl -f http://localhost:5000/health
curl -f http://localhost:8080/health
\`\`\`

---

**Full Changelog**: https://github.com/grayplex/crowdqr/compare/v${previous_version}...v${version}
EOF
   
   log_success "Generated release notes: $release_notes_file"
}

# Main execution
main() {
   local version=""
   local provided_version=false
   
   # Parse command line arguments
   while [[ $# -gt 0 ]]; do
       case $1 in
           -p|--push)
               PUSH_IMAGES=true
               shift
               ;;
           -l|--latest)
               TAG_LATEST=true
               shift
               ;;
           -r|--registry)
               REGISTRY="$2"
               shift 2
               ;;
           -n|--repo)
               REPOSITORY="$2"
               shift 2
               ;;
           --dry-run)
               DRY_RUN=true
               shift
               ;;
           -h|--help)
               usage
               exit 0
               ;;
           -*)
               log_error "Unknown option: $1"
               usage
               exit 1
               ;;
           *)
               if [ -z "$version" ]; then
                   version="$1"
                   provided_version=true
               else
                   log_error "Multiple versions provided"
                   usage
                   exit 1
               fi
               shift
               ;;
       esac
   done
   
   # Set defaults
   PUSH_IMAGES=${PUSH_IMAGES:-false}
   TAG_LATEST=${TAG_LATEST:-false}
   DRY_RUN=${DRY_RUN:-false}
   
   # Determine version
   if [ -z "$version" ]; then
       local latest_version
       latest_version=$(get_latest_version)
       version=$(increment_version "$latest_version")
       log_info "Auto-incrementing version from $latest_version to $version"
   fi
   
   # Validate version
   if ! validate_version "$version"; then
       exit 1
   fi
   
   # Show configuration
   log_info "Configuration:"
   echo "  Version: $version"
   echo "  Registry: $REGISTRY"
   echo "  Repository: $REPOSITORY"
   echo "  Push images: $PUSH_IMAGES"
   echo "  Tag latest: $TAG_LATEST"
   echo "  Dry run: $DRY_RUN"
   echo ""
   
   # Confirm if not dry run
   if [ "$DRY_RUN" != true ]; then
       read -p "Proceed with tagging version $version? (y/N): " -n 1 -r
       echo
       if [[ ! $REPLY =~ ^[Yy]$ ]]; then
           log_error "Aborted by user"
           exit 1
       fi
   fi
   
   # Check git status
   check_git_status
   
   # Execute tagging process
   build_and_tag_images "$version"
   push_images "$version"
   create_git_tag "$version"
   generate_release_notes "$version"
   
   # Show summary
   log_success "Successfully tagged version $version"
   echo ""
   log_info "Tagged images:"
   echo "  - ${REGISTRY}/${REPOSITORY}-api:${version}"
   echo "  - ${REGISTRY}/${REPOSITORY}-web:${version}"
   
   if [ "$TAG_LATEST" = true ]; then
       echo "  - ${REGISTRY}/${REPOSITORY}-api:latest"
       echo "  - ${REGISTRY}/${REPOSITORY}-web:latest"
   fi
   
   if [ "$PUSH_IMAGES" = true ] && [ "$DRY_RUN" != true ]; then
       echo ""
       log_info "Images pushed to registry and available for deployment"
   fi
}

# Check if running directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
   main "$@"
fi