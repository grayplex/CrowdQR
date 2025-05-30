# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app

# Copy solution and project files
COPY CrowdQR.Shared/CrowdQR.Shared.csproj ./CrowdQR.Shared/
COPY CrowdQR.Api/CrowdQR.Api.csproj ./CrowdQR.Api/

# Restore dependencies
RUN dotnet restore ./CrowdQR.Api/CrowdQR.Api.csproj

# Copy source code
COPY CrowdQR.Shared/ ./CrowdQR.Shared/
COPY CrowdQR.Api/ ./CrowdQR.Api/

# Build and publish
WORKDIR /app/CrowdQR.Api
RUN dotnet publish -c Release -o /app/out --no-restore \
    --self-contained false \
    --verbosity minimal \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

# Create non-root user for security
RUN addgroup -g 1001 -S crowdqr && \
    adduser -S crowdqr -u 1001 -G crowdqr

# Install curl for healthchecks
RUN apk add --no-cache curl

WORKDIR /app

# Copy the published output
COPY --from=build --chown=crowdqr:crowdqr /app/out .

# Copy and set up the entrypoint script
COPY --chown=crowdqr:crowdqr CrowdQR.Api/docker-entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh && \
    dos2unix /app/entrypoint.sh 2>/dev/null || true

# Switch to non-root user
USER crowdqr

# Configure ASP.NET Core
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

EXPOSE 5000

# Add healthcheck
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["/bin/sh", "/app/entrypoint.sh"]