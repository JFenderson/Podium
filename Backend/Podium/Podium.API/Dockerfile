# ============================================
# Stage 1: Build Stage
# ============================================
# Use the official .NET 8.0 SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory inside the container
WORKDIR /src

# Copy solution file first to leverage Docker layer caching
COPY ["Podium.sln", "./"]

# Copy all project files to enable dependency restoration
# This is done separately to optimize layer caching - if .csproj files don't change,
# this layer can be reused even if source code changes
COPY ["Podium.API/Podium.API.csproj", "Podium.API/"]
COPY ["Podium.Application/Podium.Application.csproj", "Podium.Application/"]
COPY ["Podium.Core/Podium.Core.csproj", "Podium.Core/"]
COPY ["Podium.Infrastructure/Podium.Infrastructure.csproj", "Podium.Infrastructure/"]
COPY ["Podium.Tests/Podium.Tests.csproj", "Podium.Tests/"]

# Restore NuGet packages for all projects
RUN dotnet restore "Podium.API/Podium.API.csproj"

# Copy the rest of the application source code
COPY . .

# Set working directory to the API project
WORKDIR "/src/Podium.API"

# Build the application in Release mode
RUN dotnet build "Podium.API.csproj" -c Release -o /app/build

# ============================================
# Stage 2: Publish Stage
# ============================================
# Continue from the build stage
FROM build AS publish

# Publish the application to /app/publish directory
# This creates a deployment-ready version of the application
RUN dotnet publish "Podium.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ============================================
# Stage 3: Runtime Stage
# ============================================
# Use the official .NET 8.0 ASP.NET runtime image (much smaller than SDK)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory for the runtime
WORKDIR /app

# Define build argument for API port (can be overridden at build time)
ARG API_PORT=5044

# Install curl for health checks (needed before switching to non-root user)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security best practices
# Running as non-root reduces security risks
RUN groupadd -r podiumuser && useradd -r -g podiumuser podiumuser

# Copy the published application from the publish stage
COPY --from=publish /app/publish .

# Change ownership of the application files to the non-root user
RUN chown -R podiumuser:podiumuser /app

# Switch to the non-root user
USER podiumuser

# Expose the API port
EXPOSE ${API_PORT}

# Configure environment variables
# These can be overridden at runtime via docker-compose or docker run
ENV ASPNETCORE_URLS=http://+:${API_PORT}
ENV ASPNETCORE_ENVIRONMENT=Production

# Add health check to monitor container health
# Uses the same port as configured in ASPNETCORE_URLS
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl --fail http://localhost:${API_PORT}/health || exit 1

# Set the entry point to run the application
ENTRYPOINT ["dotnet", "Podium.API.dll"]
