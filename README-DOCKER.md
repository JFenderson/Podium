# Docker Setup Guide for Podium Application

## Overview

This guide provides comprehensive instructions for building, running, and deploying the Podium application using Docker and Docker Compose. The application consists of:

- **Backend API**: ASP.NET Core 8.0 application
- **Frontend**: Angular 21 application served with nginx
- **SQL Server**: Microsoft SQL Server 2022 for database
- **Azurite**: Azure Storage emulator for local development

The Docker setup supports both local development and production deployment scenarios with optimized multi-stage builds for minimal image sizes.

## Prerequisites

Before you begin, ensure you have the following installed:

- **Docker**: Version 20.10 or higher
- **Docker Compose**: Version 2.0 or higher
- **Git**: For cloning the repository

### Verify Installation

```bash
docker --version
docker-compose --version
```

## Building Images

### Build Backend Image

To build only the backend API image:

```bash
# From the repository root
docker build -t podium-backend:latest -f Backend/Podium/Podium.API/Dockerfile Backend/Podium
```

### Build Frontend Image

To build only the frontend image:

```bash
# From the repository root
docker build -t podium-frontend:latest -f Frontend/podium-frontend/Dockerfile Frontend/podium-frontend
```

### Build All Images via Docker Compose

The easiest way to build all images at once:

```bash
# Build all images defined in docker-compose.yml
docker-compose build

# Build with no cache (force rebuild)
docker-compose build --no-cache

# Build specific service
docker-compose build backend
docker-compose build frontend
```

## Running Locally

### Quick Start

1. **Copy the environment file**:
   ```bash
   cp .env.example .env
   ```

2. **Update the `.env` file** with your configuration (optional for local development - defaults work out of the box)

3. **Start all services**:
   ```bash
   docker-compose up
   ```

4. **Access the application**:
   - Frontend: http://localhost:4200
   - Backend API: http://localhost:5044
   - API Documentation (Swagger): http://localhost:5044/swagger
   - Hangfire Dashboard: http://localhost:5044/hangfire

### Detailed Commands

#### Start Services in Detached Mode

Run services in the background:

```bash
docker-compose up -d
```

#### View Logs

View logs from all services:

```bash
docker-compose logs

# Follow logs (live tail)
docker-compose logs -f

# View logs for specific service
docker-compose logs backend
docker-compose logs frontend
docker-compose logs sqlserver
docker-compose logs azurite

# Follow logs for specific service
docker-compose logs -f backend
```

#### Stop Services

```bash
# Stop all services (containers remain)
docker-compose stop

# Stop and remove containers, networks, and volumes
docker-compose down

# Stop and remove everything including volumes (WARNING: deletes database data)
docker-compose down -v
```

#### Restart Services

```bash
# Restart all services
docker-compose restart

# Restart specific service
docker-compose restart backend
```

#### Rebuild and Restart

If you've made code changes:

```bash
# Rebuild and restart specific service
docker-compose up -d --build backend

# Rebuild and restart all services
docker-compose up -d --build
```

## Environment Variables

### Complete Environment Variables Table

| Variable | Description | Default Value | Required | Used In |
|----------|-------------|---------------|----------|---------|
| `SQL_SA_PASSWORD` | SQL Server SA password | `YourStrong!Passw0rd` | Yes (Dev) | docker-compose.yml |
| `DATABASE_CONNECTION_STRING` | Production database connection | - | Yes (Prod) | docker-compose.prod.yml |
| `JWT_SECRET` | JWT signing secret (min 32 chars) | `YourSuperSecretKeyThatIsAtLeast32CharactersLong!` | Yes | Both |
| `JWT_ISSUER` | JWT token issuer | `PodiumAPI` | No | Both |
| `JWT_AUDIENCE` | JWT token audience | `PodiumClient` | No | Both |
| `JWT_EXPIRATION_MINUTES` | JWT token expiration time | `60` | No | Both |
| `AZURE_STORAGE_CONNECTION_STRING` | Azure Storage connection | (Azurite default) | Yes (Prod) | docker-compose.prod.yml |
| `AZURE_CONTAINER_NAME` | Blob container name | `podium-videos` | No | Both |
| `AWS_REGION` | AWS region (if using S3) | `us-east-1` | No | Both |
| `AWS_BUCKET_NAME` | S3 bucket name | `podium-videos-bucket` | No | Both |
| `AWS_ACCESS_KEY` | AWS access key | - | No | Both |
| `AWS_SECRET_KEY` | AWS secret key | - | No | Both |
| `VIDEO_TRANSCODING_SECRET` | Video transcoding secret | `YourSecureRandomStringHere123!` | Yes | Both |
| `VIDEO_MAX_FILE_SIZE_MB` | Max video file size | `500` | No | Both |
| `API_URL` | Backend API URL | `http://localhost:5044` | Yes | Both |
| `CLIENT_URL` | Frontend URL | `http://localhost:4200` | Yes | Both |
| `FRONTEND_URL_0` | Production frontend URL 1 | - | Yes (Prod) | docker-compose.prod.yml |
| `FRONTEND_URL_1` | Production frontend URL 2 | - | Yes (Prod) | docker-compose.prod.yml |
| `BACKEND_PORT` | Backend host port | `5044` | No | Both |
| `FRONTEND_PORT` | Frontend host port | `4200` (Dev), `80` (Prod) | No | Both |
| `SENDGRID_API_KEY` | SendGrid API key for emails | - | No | Both |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET environment | `Development` | No | Both |

### Example .env File

Create a `.env` file in the repository root:

```env
# Database
SQL_SA_PASSWORD=YourStrong!Passw0rd

# JWT
JWT_SECRET=YourSuperSecretKeyThatIsAtLeast32CharactersLong!
JWT_ISSUER=PodiumAPI
JWT_AUDIENCE=PodiumClient
JWT_EXPIRATION_MINUTES=60

# Azure Storage (uses Azurite in dev)
AZURE_CONTAINER_NAME=podium-videos

# Video Processing
VIDEO_TRANSCODING_SECRET=YourSecureRandomStringHere123!
VIDEO_MAX_FILE_SIZE_MB=500

# URLs
API_URL=http://localhost:5044
CLIENT_URL=http://localhost:4200

# Ports
BACKEND_PORT=5044
FRONTEND_PORT=4200

# Environment
ASPNETCORE_ENVIRONMENT=Development
```

## Pushing to Container Registry

### Azure Container Registry (ACR)

1. **Login to ACR**:
   ```bash
   az login
   az acr login --name yourregistryname
   ```

2. **Tag images**:
   ```bash
   docker tag podium-backend:latest yourregistryname.azurecr.io/podium-backend:latest
   docker tag podium-backend:latest yourregistryname.azurecr.io/podium-backend:v1.0.0
   
   docker tag podium-frontend:latest yourregistryname.azurecr.io/podium-frontend:latest
   docker tag podium-frontend:latest yourregistryname.azurecr.io/podium-frontend:v1.0.0
   ```

3. **Push images**:
   ```bash
   docker push yourregistryname.azurecr.io/podium-backend:latest
   docker push yourregistryname.azurecr.io/podium-backend:v1.0.0
   
   docker push yourregistryname.azurecr.io/podium-frontend:latest
   docker push yourregistryname.azurecr.io/podium-frontend:v1.0.0
   ```

### Docker Hub

1. **Login to Docker Hub**:
   ```bash
   docker login
   ```

2. **Tag images**:
   ```bash
   docker tag podium-backend:latest yourusername/podium-backend:latest
   docker tag podium-backend:latest yourusername/podium-backend:v1.0.0
   
   docker tag podium-frontend:latest yourusername/podium-frontend:latest
   docker tag podium-frontend:latest yourusername/podium-frontend:v1.0.0
   ```

3. **Push images**:
   ```bash
   docker push yourusername/podium-backend:latest
   docker push yourusername/podium-backend:v1.0.0
   
   docker push yourusername/podium-frontend:latest
   docker push yourusername/podium-frontend:v1.0.0
   ```

## Production Deployment

### Using docker-compose.prod.yml

1. **Create production environment file**:
   ```bash
   cp .env.example .env.prod
   ```

2. **Update `.env.prod` with production values**:
   ```env
   # Production Database (Azure SQL, AWS RDS, etc.)
   DATABASE_CONNECTION_STRING=Server=prod-server.database.windows.net;Database=PodiumDb;User Id=produser;Password=ProdPassword123!;TrustServerCertificate=True

   # Production Azure Storage
   AZURE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;AccountName=prodaccount;AccountKey=yourkey;EndpointSuffix=core.windows.net
   
   # JWT with strong secret
   JWT_SECRET=YourProductionSecretKeyThatIsVeryLongAndSecure123456!
   
   # Production URLs
   API_URL=https://api.yourproductiondomain.com
   CLIENT_URL=https://www.yourproductiondomain.com
   FRONTEND_URL_0=https://www.yourproductiondomain.com
   FRONTEND_URL_1=https://yourproductiondomain.com
   
   # Other production settings
   ASPNETCORE_ENVIRONMENT=Production
   ```

3. **Deploy using production compose file**:
   ```bash
   # Load production environment
   export $(cat .env.prod | xargs)
   
   # Start services with production configuration
   docker-compose -f docker-compose.prod.yml up -d
   
   # Or specify env file directly
   docker-compose -f docker-compose.prod.yml --env-file .env.prod up -d
   ```

### Database Migration Steps

The backend automatically runs migrations on startup. For manual migration control:

1. **Run migrations manually**:
   ```bash
   # Access the backend container
   docker-compose exec backend bash
   
   # Inside container, run migrations
   dotnet ef database update
   ```

2. **Create a new migration** (during development):
   ```bash
   # From host machine in Backend/Podium/Podium.API directory
   dotnet ef migrations add MigrationName --project ../Podium.Infrastructure
   ```

3. **Check migration status**:
   ```bash
   docker-compose exec backend dotnet ef migrations list
   ```

## Troubleshooting

### Common Issues and Solutions

#### Issue: Backend fails to connect to database

**Symptoms**: Backend logs show connection errors

**Solutions**:
1. Check SQL Server is healthy:
   ```bash
   docker-compose ps sqlserver
   docker-compose logs sqlserver
   ```

2. Verify connection string environment variable:
   ```bash
   docker-compose exec backend env | grep ConnectionStrings
   ```

3. Ensure SQL Server is accepting connections:
   ```bash
   # Try connecting directly
   docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong!Passw0rd" -Q "SELECT 1"
   ```

#### Issue: Frontend cannot reach backend API

**Symptoms**: Frontend shows API errors or CORS issues

**Solutions**:
1. Check backend is healthy:
   ```bash
   docker-compose ps backend
   curl http://localhost:5044/health
   ```

2. Verify CORS configuration in backend logs:
   ```bash
   docker-compose logs backend | grep CORS
   ```

3. Check environment variables:
   ```bash
   docker-compose exec frontend env | grep API_URL
   ```

#### Issue: Port conflicts

**Symptoms**: Error message about port already in use

**Solutions**:
1. Check which process is using the port:
   ```bash
   # On Linux/Mac
   lsof -i :5044
   lsof -i :4200
   lsof -i :1433
   
   # On Windows
   netstat -ano | findstr :5044
   ```

2. Stop conflicting service or change port in `.env`:
   ```env
   BACKEND_PORT=5045
   FRONTEND_PORT=4201
   ```

3. Restart docker-compose:
   ```bash
   docker-compose down
   docker-compose up -d
   ```

#### Issue: Container keeps restarting

**Symptoms**: Container shows status "Restarting"

**Solutions**:
1. Check container logs:
   ```bash
   docker-compose logs backend
   docker-compose logs frontend
   ```

2. Check health check status:
   ```bash
   docker inspect podium-backend | grep -A 10 Health
   ```

3. Disable health check temporarily to debug:
   Edit docker-compose.yml and comment out healthcheck section

#### Issue: Build fails

**Symptoms**: docker-compose build command fails

**Solutions**:
1. Clear Docker build cache:
   ```bash
   docker-compose build --no-cache
   ```

2. Remove old images:
   ```bash
   docker image prune -a
   ```

3. Check Dockerfile syntax and paths:
   ```bash
   # Verify paths exist
   ls Backend/Podium/Podium.API/Dockerfile
   ls Frontend/podium-frontend/Dockerfile
   ```

### How to Check Service Health

```bash
# Check all services status
docker-compose ps

# Check specific service health
docker inspect podium-backend --format='{{json .State.Health}}'

# Check backend health endpoint
curl http://localhost:5044/health

# Check frontend
curl http://localhost:4200
```

### How to Access Container Logs

```bash
# All logs
docker-compose logs

# Specific service
docker-compose logs backend
docker-compose logs frontend
docker-compose logs sqlserver

# Follow logs (live)
docker-compose logs -f backend

# Last N lines
docker-compose logs --tail=100 backend

# Logs with timestamps
docker-compose logs -t backend
```

### Database Connection Issues

1. **Verify SQL Server is running**:
   ```bash
   docker-compose ps sqlserver
   ```

2. **Test connection from backend container**:
   ```bash
   docker-compose exec backend bash
   # Inside container
   apt-get update && apt-get install -y telnet
   telnet sqlserver 1433
   ```

3. **Check database exists**:
   ```bash
   docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong!Passw0rd" -Q "SELECT name FROM sys.databases"
   ```

4. **View backend logs for EF errors**:
   ```bash
   docker-compose logs backend | grep -i "entity framework\|migration\|database"
   ```

## Development Tips

### Hot Reload Configuration

For development with hot reload (code changes reflected without rebuild):

1. **Backend hot reload** - Add volume mount to docker-compose.yml:
   ```yaml
   backend:
     # ... other configuration
     volumes:
       - ./Backend/Podium/Podium.API:/src/Podium.API:ro
       - ./Backend/Podium/Podium.Application:/src/Podium.Application:ro
       - ./Backend/Podium/Podium.Core:/src/Podium.Core:ro
       - ./Backend/Podium/Podium.Infrastructure:/src/Podium.Infrastructure:ro
   ```

2. **Frontend hot reload** - Run Angular dev server locally instead:
   ```bash
   # Don't use frontend container for development
   docker-compose up -d backend sqlserver azurite
   
   # Run Angular locally with hot reload
   cd Frontend/podium-frontend
   npm install
   npm start
   ```

### Debugging in Containers

#### Backend Debugging

1. **Enable debug mode**:
   Add to docker-compose.yml backend service:
   ```yaml
   environment:
     - ASPNETCORE_ENVIRONMENT=Development
   ports:
     - "5044:5044"
     - "5000:5000"  # Debug port
   ```

2. **View detailed logs**:
   ```bash
   docker-compose logs -f backend
   ```

3. **Access container shell**:
   ```bash
   docker-compose exec backend bash
   ```

#### Frontend Debugging

1. **Access container**:
   ```bash
   docker-compose exec frontend sh
   ```

2. **View nginx logs**:
   ```bash
   docker-compose exec frontend tail -f /var/log/nginx/access.log
   docker-compose exec frontend tail -f /var/log/nginx/error.log
   ```

### Running Individual Services

You can run services individually for targeted development:

```bash
# Only database
docker-compose up -d sqlserver

# Database and backend
docker-compose up -d sqlserver azurite backend

# All except frontend
docker-compose up -d sqlserver azurite backend
```

### Executing Commands in Running Containers

```bash
# Execute command in backend
docker-compose exec backend dotnet --version

# Execute command in frontend
docker-compose exec frontend nginx -t

# Access SQL Server
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong!Passw0rd"

# Access Azurite storage explorer (if installed)
docker-compose exec azurite ls /data
```

### Database Backup and Restore

#### Backup

```bash
# Backup database
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong!Passw0rd" -Q "BACKUP DATABASE PodiumDb TO DISK='/var/opt/mssql/backup/PodiumDb.bak'"

# Copy backup to host
docker cp podium-sqlserver:/var/opt/mssql/backup/PodiumDb.bak ./PodiumDb.bak
```

#### Restore

```bash
# Copy backup to container
docker cp ./PodiumDb.bak podium-sqlserver:/var/opt/mssql/backup/PodiumDb.bak

# Restore database
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong!Passw0rd" -Q "RESTORE DATABASE PodiumDb FROM DISK='/var/opt/mssql/backup/PodiumDb.bak' WITH REPLACE"
```

## Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [ASP.NET Core Docker Documentation](https://docs.microsoft.com/aspnet/core/host-and-deploy/docker/)
- [Angular Docker Deployment](https://angular.io/guide/deployment#docker)
- [SQL Server Docker Documentation](https://docs.microsoft.com/sql/linux/sql-server-linux-docker-container-deployment)

## Support

For issues specific to the Docker setup, please check:
1. This troubleshooting guide
2. Docker and container logs
3. Environment variable configuration
4. Network connectivity between services

For application-specific issues, refer to the main README.md and project documentation.
