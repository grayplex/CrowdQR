# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

# Build argument to handle different project structures
ARG PROJECT_ROOT=.
WORKDIR /app

# Copy solution and project files with dynamic paths
COPY ${PROJECT_ROOT}/CrowdQR.Shared/CrowdQR.Shared.csproj ./CrowdQR.Shared/
COPY ${PROJECT_ROOT}/CrowdQR.Web/CrowdQR.Web.csproj ./CrowdQR.Web/

# Restore dependencies
RUN dotnet restore ./CrowdQR.Web/CrowdQR.Web.csproj

# Copy source code with dynamic paths
COPY ${PROJECT_ROOT}/CrowdQR.Shared/ ./CrowdQR.Shared/
COPY ${PROJECT_ROOT}/CrowdQR.Web/ ./CrowdQR.Web/

# Build and publish
WORKDIR /app/CrowdQR.Web
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

# Switch to non-root user
USER crowdqr

# Configure ASP.NET Core
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

EXPOSE 80

# Add healthcheck
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "CrowdQR.Web.dll"]