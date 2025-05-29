# CrowdQR Production Deployment Guide

This directory contains scripts and documentation for deploying CrowdQR in production environments.

## Quick Start

1. **Prepare Environment**

   ```bash
   # Copy production environment template
   cp .env.example .env
   
   # Edit production settings
   nano .env
   ```

2. **Run Deployment Test**

    ```bash
    chmod +x deploy/production-test.sh
    ./deploy/production-test.sh
    ```

3. **Access Application**

    - Web Application: <http://localhost:8080>
    - API: <http://localhost:5000>
    - Health Checks: <http://localhost:8080/health>

## Manual Deployment Steps

### Prerequisites

- Docker 20.10+
- Docker Compose 2.0+
- 2GB+ RAM available
- Ports 5000, 8080, 5443 available

### Step-by-Step Guide

1. **Clone Repository**

   ```bash
    git clone https://github.com/grayplex/crowdqr.git
    cd crowdqr
    ```

2. **Configure Environment**

    ```bash
    cp .env.example .env

    # Edit these critical settings:
    # - POSTGRES_PASSWORD (use a strong password)
    # - ASPNETCORE_ENVIRONMENT=Production
    ```

3. **Build and Start Services**

    ```bash
    export ASPNETCORE_ENVIRONMENT=Production
    docker-compose --env-file .env up -d --build
    ```

4. **Verify Deployment**

    ```bash
    # Check service status
    docker-compose ps

    # Check health
    curl http://localhost:5000/health
    curl http://localhost:8080/health
    ```

## Production Considerations

### Security

- Change default PostgreSQL password
- Use environment variables for secrets
- Consider reverse proxy (Nginx, Traefik) for SSL termination
- Enable firewall rules for required ports only

### Performance

- Allocate sufficient resources (2GB+ RAM)
- Consder using external PostgreSQL database for scalability
- Monitor container resource usage
- Set up log rotation

### Monitoring

- Health check endpoints available at `/health`
- Container logs via `docker-compose logs`
- Database connection monitoring via health checks

### Backup

- Database: Regular PostgreSQL backups via `pg_dump`
- Application: Version-controlled source code
- Configuration: Backup `.env` securely

## Troubleshooting

### Common Issues

1. **Port Conflicts**

    ```bash
    # Check port usage
    netstat -tulpn | grep :8080

    # Modify ports in docker-compose.yml if needed
    ```

2. **Database Connection Errors**

    ```bash
    # Check database logs
    docker-compose logs db

    # Test database connection
    docker-compose exec db psql -U crowdqr_prod -d crowdqr_production
    ```

3. **Application Not Responding**

    ```bash
    # Check application logs
    docker-compose logs api
    docker-compose logs web

    # Verify environment variables
    docker-compose exec api printenv
    ```

### Log Locations

- API Logs: `docker-compose logs api`
- Web Logs: `docker-compose logs web`
- Database Logs: `docker-compose logs db`

### Performance Monitoring

```bash
# Monitor resource usage
docker stats

# Check health status
curl -s http://localhost:5000/health | jq
curl -s http://localhost:8080/health | jq
```

## Updating Deployment

1. **Pull Latest Changes**

   ```bash
   git pull origin main
   ```

2. **Rebuild and Restart Services**

   ```bash
    docker-compose --env-file .env down
    docker-compose --env-file .env up -d --build
    ```

3. **Verify Update**

    ```bash
    docker-compose ps
    curl http://localhost:8080/health
    ```
