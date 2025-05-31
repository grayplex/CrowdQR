#!/bin/bash
# deploy/deploy-traefik.sh

set -e

echo "🚀 Deploying CrowdQR with Traefik + Cloudflare SSL"
echo "=================================================="

# Load environment variables
if [ -f .env ]; then
    source .env
    echo "✅ Loaded environment variables"
else
    echo "❌ .env file not found"
    exit 1
fi

# Validate required variables
REQUIRED_VARS=(
    "DOMAIN"
    "CLOUDFLARE_EMAIL"
    "POSTGRES_PASSWORD"
    "JWT_SECRET"
)

# Check for Cloudflare credentials
if [ -n "$CLOUDFLARE_ZONE_API_TOKEN" ]; then
    echo "✅ Using Cloudflare Zone API Token"
elif [ -n "$CLOUDFLARE_DNS_API_TOKEN" ] && [ -n "$CLOUDFLARE_EMAIL" ]; then
    echo "✅ Using Cloudflare Global API Key"
else
    echo "❌ Missing Cloudflare credentials"
    echo "Set either CLOUDFLARE_ZONE_API_TOKEN or both CLOUDFLARE_DNS_API_TOKEN and CLOUDFLARE_EMAIL"
    exit 1
fi

for var in "${REQUIRED_VARS[@]}"; do
    if [ -z "${!var}" ]; then
        echo "❌ Missing required variable: $var"
        exit 1
    fi
done

# Create necessary directories
echo "📁 Creating directories..."
mkdir -p letsencrypt
mkdir -p deploy/traefik

# Set proper permissions for Let's Encrypt storage
chmod 600 letsencrypt 2>/dev/null || true

# Validate docker-compose configuration
echo "🔍 Validating Docker Compose configuration..."
if ! docker-compose config >/dev/null; then
    echo "❌ Invalid Docker Compose configuration"
    exit 1
fi

# Stop any existing deployment
echo "🛑 Stopping existing services..."
docker-compose down --remove-orphans 2>/dev/null || true

# Pull latest images
echo "📥 Pulling latest images..."
docker-compose pull

# Build and start services
echo "🏗️ Building and starting services..."
docker-compose up -d --build

# Wait for services to be ready
echo "⏳ Waiting for services to be ready..."
sleep 45

# Check service health
echo "🩺 Checking service health..."

# Check Traefik
if docker-compose ps traefik | grep -q "Up"; then
    echo "✅ Traefik is running"
else
    echo "❌ Traefik failed to start"
    docker-compose logs traefik
    exit 1
fi

# Wait for certificates
echo "🔐 Waiting for SSL certificates..."
sleep 30

# Test endpoints
echo "🧪 Testing endpoints..."

# Test HTTP redirect
if curl -I "http://${DOMAIN}" 2>/dev/null | grep -q "301\|302"; then
    echo "✅ HTTP redirects to HTTPS"
else
    echo "⚠️ HTTP redirect not working"
fi

# Test HTTPS endpoints
if curl -f "https://${DOMAIN}/health" >/dev/null 2>&1; then
    echo "✅ Web application is accessible via HTTPS"
else
    echo "❌ Web application HTTPS failed"
    echo "Checking logs..."
    docker-compose logs web
fi

if curl -f "https://api.${DOMAIN}/health" >/dev/null 2>&1; then
    echo "✅ API is accessible via HTTPS"
else
    echo "❌ API HTTPS failed"
    echo "Checking logs..."
    docker-compose logs api
fi

# Show deployment summary
echo ""
echo "🎉 Deployment Summary"
echo "===================="
echo "🌐 Web Application: https://${DOMAIN}"
echo "🔧 API: https://api.${DOMAIN}"
echo "🔒 SSL: Let's Encrypt via Cloudflare DNS"
echo "🛡️ Reverse Proxy: Traefik"
echo ""
echo "📊 Service Status:"
docker-compose ps

echo ""
echo "📋 Next Steps:"
echo "1. Update your DNS records to point to this server"
echo "2. Test all functionality at https://${DOMAIN}"
echo "3. Monitor logs with: docker-compose logs -f"
echo "4. Check certificates with: docker-compose exec traefik cat /letsencrypt/acme.json"