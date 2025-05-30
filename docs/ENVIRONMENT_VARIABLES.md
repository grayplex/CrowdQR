# CrowdQR Environment Variables Reference

This document provides a comprehensive reference for all environment variables used in the CrowdQR application.

## Quick Start

1. Copy the example environment file:

   ```bash
   cp .env.example .env
   ```

2. Edit the .env file with your specific values:

    ```bash
    nano .env # Or your preferred editor
    ```

3. Important: Change these security-critical variables before production deployment:

    - `POSTGRES_PASSWORD`
    - `JWT_SECRET`
    - `CORS_ORIGINS`

## Required Variables

### Database Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `POSTGRES_USER` | PostgreSQL username | `crowdqr_prod` | ✅ |
| `POSTGRES_PASSWORD` | PostgreSQL password | `your_secure_password_here_change_this` | ✅ |
| `POSTGRES_DB` | PostgreSQL database name | `crowdqr_production` | ✅ |
| `DB_HOST` | Database host (use db for docker-compose)| `db` | ✅ |
| `DB_PORT` | Database port | `5432` | ✅ |

### Application Environment

| Variable | Description | Values | Required |
|----------|-------------|--------|----------|
| `ASPNETCORE_ENVIRONMENT` | Application environment| `Development`, `Production` | ✅ |

### Authentication & Security

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `JWT_SECRET` | JWT signing secret (32+ characters) | `your_jwt_secret_key_minimum_32_characters_long_change_this_in_production` | ✅ |
| `JWT_ISSUER` | JWT token issuer | `CrowdQR.Api` | ✅ |
| `JWT_AUDIENCE` | JWT token audience | `CrowdQR.Api` | ✅ |
| `JWT_EXPIRY_HOURS` | JWT token expiry in hours | `24` | ❌ |

## OptionalVariables

### Email Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `SMTP_HOST` | SMTP server host | `""` | ❌ |
| `SMTP_PORT` | SMTP server port | `587` | ❌ |
| `SMTP_USERNAME` | SMTP username | `""` | ❌ |
| `SMTP_PASSWORD` | SMTP password | `""` | ❌ |
| `SMTP_FROM_EMAIL` | Email sender address | `noreply@yourdomain.com` | ❌ |
| `SMTP_FROM_NAME` | Email sender name | `CrowdQR` | ❌ |

### Application URLs

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `API_PUBLIC_URL` | Public API URL | `https://your-api-domain.com` | ❌ |
| `WEB_PUBLIC_URL` | Public web URL | `https://your-web-domain.com` | ❌ |
| `CORS_ORIGINS` | Allowed CORS origins (comma-separated) | `https://your-web-domain.com`,`https://localhost:5000` | ❌ |

### Docker & Deployment

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `REGISTRY` | Docker registry | `ghcr.io` | ❌ |
| `REPOSITORY` | Docker repository | `grayplex/crowdqr` | ❌ |
| `CROWDQR_VERSION` | Image version tag | `latest` | ❌ |
| `DOCKER_API_PORT` | API external port | `5000` | ❌ |
| `DOCKER_WEB_PORT` | Web external port | `80` | ❌ |
| `DOCKER_DB_PORT` | Database external port | `5433` | ❌ |

### Performance & Monitoring

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `RATE_LIMIT_REQUESTS_PER_MINUTE` | Rate limit per IP | `100` | ❌ |
| `LOG_LEVEL` | Logging level | `Information` | ❌ |
| `HEALTH_CHECK_TIMEOUT` | Health check timeout in seconds | `30` | ❌ |

## Environment-Specific Examples

### Development Environment

```bash
ASPNETCORE_ENVIRONMENT=Development
POSTGRES_PASSWORD=postgres
JWT_SECRET=dev_secret_key_for_local_development_only_32_chars
DB_HOST=localhost
API_PUBLIC_URL=http://localhost:5071
WEB_PUBLIC_URL=http://localhost:5139
CORS_ORIGINS=http://localhost:5139,https://localhost:7159
LOG_LEVEL=Debug
```

### Production Environment

```bash
ASPNETCORE_ENVIRONMENT=Production
POSTGRES_PASSWORD=$(openssl rand -base64 32)
JWT_SECRET=$(openssl rand -base64 32)
DB_HOST=db
API_PUBLIC_URL=https://api.yourdomain.com
WEB_PUBLIC_URL=https://app.yourdomain.com
CORS_ORIGINS=https://app.yourdomain.com
LOG_LEVEL=Warning
SMTP_HOST=smtp.yourdomain.com
SMTP_USERNAME=noreply@yourdomain.com
SMTP_PASSWORD=your_smtp_password
```

## Security Best Practices

### Critical Security Variables

These variables **MUST** be changed before production deployment:

1. `POSTGRES_PASSWORD`: Use a strong, unique password

    ```bash
    # Generate a secure password
    openssl rand -base64 32
    ```

2. `JWT_SECRET`: Use a cryptographically secure random string (32+ characters)

    ```bash
    # Generate a secure JWT secret
    openssl rand -base64 32
    ```

3. `CORS_ORIGINS`: Restrict to your actual domain(s)

    ```bash
    CORS_ORIGINS=https://yourdomain.com,https://api.yourdomain.com
    ```

### Additional Security Measures

- Use Docker secrets for sensitive values in production
- Implement proper network segmentation
- Enable SSL/TLS for all external connections
- Regularly rotate passwords and secrets
- Monitor logs for suspicious activity

### Security Checklist

- [ ] Changed default `POSTGRES_PASSWORD`
- [ ] Generated a secure `JWT_SECRET`
- [ ] Configured proper `CORS_ORIGINS`
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Configured HTTPS for API and Web URLs
- [ ] Enabled email notifications (*Not yet implemented*)
- [ ] Set appropriate log levels
- [ ] Implemented rate limiting
- [ ] Implemented backup strategy

## Troubleshooting

### Common Issues

1. **Database Connection Errors**: 

    - Verify `DB_HOST`, `DB_PORT`, `DB_USER`, and `DB_PASSWORD`
    - Ensure database contianer is running
    - Check network connectivity

2. **Authentication Failures**:

    - Verify `JWT_SECRET` is consistent across services
    - Check `JWT_ISSUER` and `JWT_AUDIENCE` settings
    - Ensure token hasn't expired

3. **CORS Issues**:

    - Verify `CORS_ORIGINS` includes the web app URL
    - Check for trailing slashes in URLs
    - Ensure protocol (http/https) matches

4. **Email Sending Failures**:

    - Verify all SMTP settings
    - Test SMTP credentials separately
    - Check firewall rules for SMTP port

### Environment Variable Validation

Use this command to validate your environment variables:

```bash
# Check if all required variables are set
docker-compose config
```

For detailed troubleshooting, see the [main deployment documentation](../deploy/README.md).