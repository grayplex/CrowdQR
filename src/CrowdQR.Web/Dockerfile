# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files
COPY CrowdQR.Shared/CrowdQR.Shared.csproj ./CrowdQR.Shared/
COPY CrowdQR.Web/CrowdQR.Web.csproj ./CrowdQR.Web/

# Restore dependencies
RUN dotnet restore ./CrowdQR.Web/CrowdQR.Web.csproj

# Copy source code
COPY CrowdQR.Shared/ ./CrowdQR.Shared/
COPY CrowdQR.Web/ ./CrowdQR.Web/

# Build and publish
WORKDIR /app/CrowdQR.Web
RUN dotnet publish -c Release -o /app/out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "CrowdQR.Web.dll"] 