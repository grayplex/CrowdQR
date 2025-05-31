#!/bin/sh
set -e

# Set the DLL name explicitly
DLL_NAME="CrowdQR.Api.dll"

# Wait for the database to be ready
echo "Waiting for PostgreSQL..."

until dotnet "$DLL_NAME" --wait-for-db; do
    echo "PostgreSQL is unavailable - sleeping"
    sleep 1
done

echo "PostgreSQL is up - executing command"
exec dotnet "$DLL_NAME"