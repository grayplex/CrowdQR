# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files
COPY CrowdQR.Shared/CrowdQR.Shared.csproj ./CrowdQR.Shared/
COPY CrowdQR.API/CrowdQR.API.csproj ./CrowdQR.API/

# Restore dependencies
RUN dotnet restore ./CrowdQR.API/CrowdQR.API.csproj

# Copy source code
COPY CrowdQR.Shared/ ./CrowdQR.Shared/
COPY CrowdQR.API/ ./CrowdQR.API/

# Build and publish
WORKDIR /app/CrowdQR.API
RUN dotnet publish -c Release -o /app/out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy the published output
COPY --from=build /app/out .

# Copy and set up the entrypoint script
COPY CrowdQR.API/docker-entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh && \
    sed -i 's/\r$//' /app/entrypoint.sh

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["/bin/bash", "/app/entrypoint.sh"]