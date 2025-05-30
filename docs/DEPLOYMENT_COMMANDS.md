# CrowdQR Deployment Commands Reference

This document provides a comprehensive reference for all deployment-related commands.

## Quick Reference

| Command | Description |
|---------|-------------|
| `docker-compose up -d` | Start all services in detached mode |
| `docker-compose down` | Stop and remove all services |
| `docker-compose logs -f` | Follow logs from all services |
| `docker-compose ps` | Show running containers status |
| `docker system prune -f` | Clean up unused Docker resources |

## Build Commands

### Docker Compose Build

```bash
# Build all services (recommended)
docker-compose build

# Build with no cache (clean build)
docker-compose build --no-cache

# Build specific service only
docker-compose build api
docker-compose build web

# Build in production mode
export ASPNETCORE_ENVIRONMENT=Production
docker-compose build --no-cache
```

### Plain Docker Build

```bash
# Build API image
cd src
docker build -f CrowdQR.Api/Dockerfile -t crowdqr-api:latest .

# Build Web image  
docker build -f CrowdQR.Web/Dockerfile -t crowdqr-web:latest .

# Build with specific tag
docker build -f CrowdQR.Api/Dockerfile -t crowdqr-api:v1.0.0 .
```

### .NET Build (Development)

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build --configuration Release

# Build specific project
dotnet build src/CrowdQR.Api/CrowdQR.Api.csproj --configuration Release
```

## Run Commands

### Docker Compose (Recommended)

```bash
# Start all services
docker-compose up

# Start in detached mode (background)
docker-compose up -d

# Start with specific environment file
docker-compose --env-file .env.production up -d

# Start specific services only
docker-compose up db api

# Recreate containers (useful after config changes)
docker-compose up -d --force-recreate
```

### Plain Docker Run

```bash
# Run PostgreSQL database
docker run -d \
  --name crowdqr-db \
  -e POSTGRES_USER=crowdqr_prod \
  -e POSTGRES_PASSWORD=your_password \
  -e POSTGRES_DB=crowdqr_production \
  -p 5433:5432 \
  -v crowdqr_db_data:/var/lib/postgresql/data \
  postgres:17

# Run API (after database is running)
docker run -d \
  --name crowdqr-api \
  --link crowdqr-db:db \
  -e DB_HOST=db \
  -e DB_USER=crowdqr_prod \
  -e DB_PASSWORD=your_password \
  -e DB_NAME=crowdqr_production \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -p 5000:5000 \
  crowdqr-api:latest

# Run Web app (after API is running)
docker run -d \
  --name crowdqr-web \
  --link crowdqr-api:api \
  -e ApiSettings__BaseUrl=http://api:5000 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -p 8080:80 \
  crowdqr-web:latest
```

### Local Development

```bash
# Run database only (for local development)
docker-compose up -d db

# Run API locally
cd src/CrowdQR.Api
dotnet run

# Run Web app locally (in another terminal)
cd src/CrowdQR.Web  
dotnet run
```

## Stop Commands

### Docker Compose

```bash
# Stop all services (keeps containers)
docker-compose stop

# Stop and remove containers
docker-compose down

# Stop, remove containers, and remove volumes (WARNING: deletes data)
docker-compose down --volumes

# Stop and remove everything including images
docker-compose down --rmi all --volumes --remove-orphans
```

### Plain Docker

```bash

# Stop containers
docker stop crowdqr-web crowdqr-api crowdqr-db

# Remove containers
docker rm crowdqr-web crowdqr-api crowdqr-db

# Remove volumes (WARNING: deletes data)
docker volume rm crowdqr_db_data
```

## Monitoring & Health Check Commands

### Service Status

```bash
# Check container status
docker-compose ps

# Check detailed container info
docker-compose ps --services
docker-compose ps --format table

# Check resource usage
docker stats $(docker-compose ps -q)
```

### Health Checks

```bash
# Check API health
curl -f http://localhost:5000/health

# Check Web health
curl -f http://localhost:8080/health

# Check database connectivity
docker-compose exec db pg_isready -U crowdqr_prod -d crowdqr_production

# Detailed health check with JSON output
curl -s http://localhost:5000/health | jq .
curl -s http://localhost:8080/health | jq .
```

### Log Commands

```bash
# View all logs
docker-compose logs

# Follow logs in real-time
docker-compose logs -f

# View logs for specific service
docker-compose logs api
docker-compose logs web
docker-compose logs db

# View last N lines
docker-compose logs --tail=50 api

# View logs with timestamps
docker-compose logs -t api

# View logs for specific time range
docker-compose logs --since="2024-01-01T00:00:00" --until="2024-01-01T23:59:59"
```

## Troubleshooting Commands

## Database Issues

```bash
# Connect to database
docker-compose exec db psql -U crowdqr_prod -d crowdqr_production

# Check database connection
docker-compose exec db pg_isready -U crowdqr_prod

# View database logs
docker-compose logs db

# Reset database (WARNING: deletes all data)
docker-compose down
docker volume rm crowdqr_db_data
docker-compose up -d db
```

### Application Issues

```bash
# Check application logs
docker-compose logs api
docker-compose logs web

# Access container shell
docker-compose exec api /bin/sh
docker-compose exec web /bin/sh

# Check environment variables inside container
docker-compose exec api printenv
docker-compose exec web printenv

# Test API endpoints manually
docker-compose exec api curl http://localhost:5000/health
```

### Network Issues

```bash
# List Docker networks
docker network ls

# Inspect network
docker network inspect crowdqr_crowdqr-network

# Test connectivity between containers
docker-compose exec web ping api
docker-compose exec api ping db

# Check port bindings
netstat -tulpn | grep :5000
netstat -tulpn | grep :8080
```

### Performance Issues

```bash
# Check resource usage
docker stats

# Check disk usage
docker system df

# Check container resource limits
docker-compose exec api cat /sys/fs/cgroup/memory/memory.limit_in_bytes

# Monitor in real-time
watch 'docker stats --no-stream'
```

## Maintenance Commands

### Updates

```bash
# Pull latest images
docker-compose pull

# Update and restart services
docker-compose pull && docker-compose up -d

# Update specific service
docker-compose pull api && docker-compose up -d api
```

### Cleanup

```bash
# Remove unused images
docker image prune -f

# Remove unused volumes
docker volume prune -f

# Remove unused networks
docker network prune -f

# Complete cleanup (BE CAREFUL)
docker system prune -a -f

# Clean up only CrowdQR resources
docker-compose down --rmi all --volumes --remove-orphans
```

### Backup

```bash
# Backup database
docker-compose exec db pg_dump -U crowdqr_prod crowdqr_production > backup_$(date +%Y%m%d_%H%M%S).sql

# Backup with compression
docker-compose exec db pg_dump -U crowdqr_prod crowdqr_production | gzip > backup_$(date +%Y%m%d_%H%M%S).sql.gz

# Restore database
docker-compose exec -T db psql -U crowdqr_prod crowdqr_production < backup_20240101_120000.sql
```

## Automated Deployment Scripts

### Production Test

```bash
# Run complete deployment test  
chmod +x deploy/production-test.sh
./deploy/production-test.sh

# Run with custom timeout
./deploy/production-test.sh --timeout 600

# PowerShell version (Windows)
./deploy/production-test.ps1
```

### Release Tagging

```bash
# Tag and build release
chmod +x scripts/tag-release.sh
./scripts/tag-release.sh 1.0.0 --push --latest

# PowerShell version (Windows)
./scripts/tag-release.ps1 -Version "1.0.0" -Push -Latest
```

## CI/CD Integration

### GitHub Actions

```bash
# Trigger release workflow
gh workflow run release.yml --field version=v1.0.0

# Check workflow status
gh run list --workflow=ci.yml
```

### :pca; CO Testomg

```bash
# Run the same tests as CI
dotnet test --configuration Release --verbosity normal

# Run with coverage
dotnet test --collect:"XPlat Code Coverage" --settings tests/CrowdQR.Api.Tests/coverlet.runsettings
```

## Emergency Procedures

### Complete System Reset

```bash
#!/bin/bash
# WARNING: This deletes ALL data
echo "This will delete ALL CrowdQR data. Are you sure? (type 'DELETE' to confirm)"
read confirmation
if [ "$confirmation" = "DELETE" ]; then
    docker-compose down --volumes --rmi all --remove-orphans
    docker system prune -a -f
    echo "System reset complete. Run 'docker-compose up -d' to restart."
else
    echo "Reset cancelled."
fi
```

### Service Recovery

```bash
# Restart specific service
docker-compose restart api

# Force recreate service  
docker-compose up -d --force-recreate api

# Scale service (if needed)
docker-compose up -d --scale api=2
```

### Data Recovery

```bash
# Check if volumes exist
docker volume ls | grep crowdqr

# Inspect volume
docker volume inspect crowdqr_db_data

# Create emergency backup before recovery
docker run --rm -v crowdqr_db_data:/data -v $(pwd):/backup alpine tar czf /backup/emergency_backup.tar.gz /data
```

For more detailed troubleshooting, see the [Troubleshooting Guide](docs/TROUBLESHOOTING.md).