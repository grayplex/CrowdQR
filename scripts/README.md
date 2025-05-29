# CrowdQR Release Scripts

This directory contains scripts for managing CrowdQR releases and Docker image versioning.

## Tag Release Script

The `tag-release.sh` script automates the process of building, tagging, and releasing CrowdQR Docker images with semantic versioning.

### Usage

```bash
# Make script executable
chmod +x scripts/tag-release.sh

# Tag a specific version
./scripts/tag-release.sh 1.0.0

# Tag and push to registry
./scripts/tag-release.sh 1.0.0 --push

# Tag, push, and mark as latest
./scripts/tag-release.sh 1.0.0 --push --latest

# Auto-increment patch version
./scripts/tag-release.sh --push

# Dry run to see what would happen
./scripts/tag-release.sh 1.0.0 --dry-run

# Use custom registry
./scripts/tag-release.sh 1.0.0 --registry myregistry.com --repo myorg/crowdqr
```

### Version Format

The script supports semantic versioning (SemVer):

- Major.Minor.Path (e.g., `1.0.0`)
- Pre-release (e.g., `1.0.0-beta.1`, `1.0.0-alpha.2`)
- Build metadata (e.g., `1.0.0+build.1`)

### What the script does

1. Validates the version format.
2. Builds Docker images in production mode
3. Tags images with the specified version
4. Optionally tags as `latest`
5. Pushes to registry (if requested)
6. Creates git tag
7. Generates release notes

## Examples

### Release 1.0.0

```bash
# Create and push release 1.0.0
./scripts/tag-release.sh 1.0.0 --push --latest

# This creates:
# - ghcr.io/crowdqr/crowdqr-api:1.0.0
# - ghcr.io/crowdqr/crowdqr-web:1.0.0
# - ghcr.io/crowdqr/crowdqr-api:latest
# - ghcr.io/crowdqr/crowdqr-web:latest
# - Git tag v1.0.0
```

### Beta Release

```bash
# Create beta release
./scripts/tag-release.sh 1.1.0-beta.1 --push

# This creates:
# - ghcr.io/grayplex/crowdqr-api:1.1.0-beta.1
# - ghcr.io/grayplex/crowdqr-web:1.1.0-beta.1
# - Git tag: v1.1.0-beta.1
```

### Auto-increment

```bash
# Auto-increment from latest git tag
./scripts/tag-release.sh --push

# If latest tag is v1.0.0, creates v1.0.1
```

## Registry Authentication

Before pushing images, ensure you're logged into the registry:

```bash
# Github Container registry
echo $GITHUB_TOKEN | docker login ghcr.io -u USERNAME --password-stdin

# Docker Hub
docker login
```

## Generated Files

The script generates:

- Git tags (e.g., `v1.0.0`)
- Release notes (`release-notes-1.0.0.md`)
- Tagged Docker images

## CI/CD Integration

The script can be integrated with GitHub Actions or other CI/CD systems:

```yaml
# Example GitHub Actions step
- name: Tag and push release
  run: |
    chmod +x scripts/tag-release.sh
    ./scripts/tag-release.sh ${{ github.event.inputs.version }} --push --latest
```

## Troubleshooting

### Common Issues

1. Docker not logged in
    - `docker login ghcr.io`
2. Git tag already exists
    - The script will prompt to overwrite
    - Or delete manually: `git tag -d v1.0.0`
3. Image build fails
    - Check Docker daemon is running
    - Ensure docker-compose.yml is valid
    - Verify source code compiles
4. Push fails
    - Check registry authentication
    - Verify repository permissions
    - Check network connectivity

### Debug Mode

Use `--dry-run` to see what the script would do without executing:

```bash
./scripts/tag-release.sh 1.0.0 --push --latest --dry-run
```

## Best Practices

1. Always test with `--dry-run` first
2. Use semantic versioning consistently
3. Tag stable releases as `latest`
4. Keep git history clean before tagging
5. Test images before pushing to production
