#!/bin/bash
# ============================================================================
# Podium Backend Startup Script for Azure App Service
# ============================================================================
# This script is executed when the backend container starts
# It sets up the environment and starts the application
# ============================================================================

set -e

echo "================================"
echo "Podium Backend Startup"
echo "================================"
echo "Environment: ${ASPNETCORE_ENVIRONMENT:-Production}"
echo "Start Time: $(date)"
echo "================================"

# ============================================================================
# Environment Variables Setup
# ============================================================================

echo "[INFO] Setting up environment variables..."

# Azure App Service automatically injects these from App Settings:
# - APPLICATIONINSIGHTS_CONNECTION_STRING
# - JWT__Secret (from Key Vault)
# - ConnectionStrings__DefaultConnection (from Key Vault)
# - AzureStorage__ConnectionString (from Key Vault)

# Verify critical environment variables
if [ -z "$ConnectionStrings__DefaultConnection" ]; then
    echo "[ERROR] Database connection string not set!"
    exit 1
fi

if [ -z "$JWT__Secret" ]; then
    echo "[ERROR] JWT secret not set!"
    exit 1
fi

echo "[INFO] Environment variables verified"

# ============================================================================
# Database Connectivity Check
# ============================================================================

echo "[INFO] Checking database connectivity..."

# Wait for database to be available (optional, useful for startup)
MAX_RETRIES=30
RETRY_COUNT=0

while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
    # Simple connectivity test using dotnet tool or custom check
    # This is a placeholder - actual implementation would use EF Core or sqlcmd
    echo "[INFO] Database check attempt $((RETRY_COUNT + 1))/$MAX_RETRIES"
    
    # For now, just verify connection string is set
    if [ -n "$ConnectionStrings__DefaultConnection" ]; then
        echo "[INFO] Database connection string is configured"
        break
    fi
    
    RETRY_COUNT=$((RETRY_COUNT + 1))
    sleep 2
done

if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
    echo "[WARNING] Could not verify database connectivity, proceeding anyway"
fi

# ============================================================================
# Application Insights Setup
# ============================================================================

if [ -n "$APPLICATIONINSIGHTS_CONNECTION_STRING" ]; then
    echo "[INFO] Application Insights enabled"
    export APPLICATIONINSIGHTS_ENABLE_LIVE_METRICS=true
else
    echo "[WARNING] Application Insights not configured"
fi

# ============================================================================
# Logging Configuration
# ============================================================================

# Ensure log directory exists
mkdir -p /home/LogFiles

# Set appropriate log levels for production
if [ "$ASPNETCORE_ENVIRONMENT" = "Production" ]; then
    export Serilog__MinimumLevel__Default=${Serilog__MinimumLevel__Default:-Warning}
else
    export Serilog__MinimumLevel__Default=${Serilog__MinimumLevel__Default:-Information}
fi

echo "[INFO] Log level: ${Serilog__MinimumLevel__Default}"

# ============================================================================
# Performance & Security Settings
# ============================================================================

# Enable Server GC for better throughput
export COMPlus_gcServer=1

# Set thread pool minimums for better startup performance
export DOTNET_ThreadPool_MinThreads=10

# Enable Ready To Run for faster startup
export DOTNET_ReadyToRun=1

# Enable tiered compilation
export DOTNET_TieredCompilation=1

echo "[INFO] Performance settings applied"

# ============================================================================
# Health Check Delay
# ============================================================================

# Give the application a few seconds to initialize before health checks
export HEALTHCHECK_DELAY_SECONDS=${HEALTHCHECK_DELAY_SECONDS:-10}

# ============================================================================
# Pre-Start Tasks
# ============================================================================

echo "[INFO] Running pre-start tasks..."

# Apply database migrations automatically on startup (optional, use with caution)
# Uncomment if you want auto-migrations (not recommended for production)
# if [ "$AUTO_MIGRATE_DATABASE" = "true" ]; then
#     echo "[INFO] Applying database migrations..."
#     dotnet ef database update --project /app/Podium.API.dll
# fi

# Warm up cache or perform other initialization tasks here

echo "[INFO] Pre-start tasks completed"

# ============================================================================
# Start Application
# ============================================================================

echo "[INFO] Starting Podium API..."
echo "================================"

# Change to application directory
cd /app

# Start the application
# The actual entry point is specified in the Dockerfile
exec dotnet Podium.API.dll
