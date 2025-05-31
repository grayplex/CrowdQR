#!/bin/bash
# scripts/check-ssl.sh

set -e

DOMAIN=${1:-$(grep DOMAIN .env | cut -d'=' -f2)}

if [ -z "$DOMAIN" ]; then
    echo "❌ Domain not specified"
    echo "Usage: $0 [domain]"
    exit 1
fi

echo "🔍 SSL Certificate Check for $DOMAIN"
echo "===================================="

# Check certificate details
echo "📜 Certificate Information:"
echo | openssl s_client -servername $DOMAIN -connect $DOMAIN:443 2>/dev/null | openssl x509 -noout -text | grep -E "(Subject|Issuer|Not After)"

# Check SSL Labs rating (optional)
echo ""
echo "🔗 For detailed SSL analysis, visit:"
echo "https://www.ssllabs.com/ssltest/analyze.html?d=$DOMAIN"

# Check security headers
echo ""
echo "🛡️ Security Headers:"
curl -I "https://$DOMAIN" 2>/dev/null | grep -i -E "(strict-transport-security|content-security-policy|x-frame-options|x-content-type-options)"

# Check certificate expiration
echo ""
echo "📅 Certificate Expiration:"
echo | openssl s_client -servername $DOMAIN -connect $DOMAIN:443 2>/dev/null | openssl x509 -noout -dates

echo ""
echo "✅ SSL check complete!"