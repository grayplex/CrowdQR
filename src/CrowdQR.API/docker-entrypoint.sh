#!/bin/bash
set -e

# Wait for the database to be ready
echo "Waiting for PostgreSQL..."
# Check for the correct DLL file name
if [ -f "/app/CrowdQR.Api.dll" ]; then
    DLL_NAME="CrowdQR.Api.dll"
elif [ -f "/app/CrowdQR.API.dll" ]; then
    DLL_NAME="CrowdQR.API.dll"
else
    echo "Error: Could not find the application DLL"
    exit 1
fi

until dotnet exec /app/$DLL_NAME -- --wait-for-db; do
    echo "PostgreSQL is unavailable - sleeping"
    sleep 1
done

echo "PostgreSQL is up - executing command"
exec dotnet $DLL_NAME