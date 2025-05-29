#!/bin/sh
set -e

# Wait for the database to be ready
echo "Waiting for PostgreSQL..."

until dotnet exec $DLL_NAME -- --wait-for-db; do
    echo "PostgreSQL is unavailable - sleeping"
    sleep 1
done

echo "PostgreSQL is up - executing command"
exec dotnet $DLL_NAME