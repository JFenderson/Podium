# Podium - College Band Recruitment Platform

[![Backend CI](https://github.com/JFenderson/Podium/actions/workflows/backend-ci.yml/badge.svg)](https://github.com/JFenderson/Podium/actions/workflows/backend-ci.yml)
[![Frontend CI](https://github.com/JFenderson/Podium/actions/workflows/frontend-ci.yml/badge.svg)](https://github.com/JFenderson/Podium/actions/workflows/frontend-ci.yml)
[![Deploy to Staging](https://github.com/JFenderson/Podium/actions/workflows/deploy-staging.yml/badge.svg)](https://github.com/JFenderson/Podium/actions/workflows/deploy-staging.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Podium is a comprehensive web application designed to streamline the college band recruitment process, connecting high school students with college band programs.

## Table of Contents

- [Features](#features)
- [Prerequisites](#prerequisites)
- [Technology Stack](#technology-stack)
- [Getting Started](#getting-started)
  - [Environment Setup](#environment-setup)
  - [Local Development](#local-development)
  - [Docker Deployment](#docker-deployment)
- [Configuration](#configuration)
- [Health Checks](#health-checks)
- [Security](#security)
- [API Documentation](#api-documentation)
- [Contributing](#contributing)
- [License](#license)

## Features

- **Student Profiles**: High school students can create comprehensive profiles with performance videos
- **Band Program Management**: College bands can showcase their programs
- **Recruitment Tools**: Advanced search and filtering for band directors
- **Video Uploads**: Support for performance video uploads with transcoding
- **Scholarship Management**: Track and manage scholarship offers
- **Real-time Notifications**: SignalR-based notifications
- **Event Management**: Coordinate auditions and recruitment events
- **Guardian Access**: Parents/guardians can manage student profiles

## CI/CD Pipeline

Podium uses GitHub Actions for continuous integration and deployment. The pipeline ensures code quality, runs automated tests, builds Docker images, and deploys to staging and production environments.

### Workflow Overview

```mermaid
graph LR
    A[Code Push] --> B[Backend CI]
    A --> C[Frontend CI]
    B --> D[Build Docker Images]
    C --> D
    D --> E[Push to GHCR]
    E --> F[Deploy to Staging]
    F --> G{Manual Approval}
    G -->|Approved| H[Deploy to Production]
    G -->|Rejected| I[End]
    H --> J[Health Checks]
    J --> K[Deployment Complete]
```

### Workflows

#### Backend CI (`backend-ci.yml`)
- **Triggers**: Push to `main`, Pull Requests, Manual dispatch
- **Jobs**:
  - Run unit tests with coverage reporting
  - Build .NET application with NuGet caching
  - Build and push Docker image to GitHub Container Registry
- **Features**: Test result annotations, code coverage reports in PRs

#### Frontend CI (`frontend-ci.yml`)
- **Triggers**: Push to `main`, Pull Requests, Manual dispatch
- **Jobs**:
  - Lint Angular codebase with ESLint
  - Run unit tests with coverage
  - Build production bundle with size checks
  - Build and push Docker image to GitHub Container Registry
- **Features**: Bundle size warnings (>2MB), artifact uploads

#### Staging Deployment (`deploy-staging.yml`)
- **Triggers**: Push to `main`, Manual dispatch
- **Jobs**:
  - Pull latest Docker images from GHCR
  - Run database migrations
  - Deploy to staging environment
  - Execute smoke tests
  - Send deployment notifications
- **Features**: Automatic rollback on failure

#### Production Deployment (`deploy-production.yml`)
- **Triggers**: Manual dispatch only
- **Jobs**:
  - Pre-deployment checks (image availability, configuration validation)
  - Deploy with manual approval gate
  - Database migrations with automatic rollback
  - Post-deployment health checks
  - Cache warming and monitoring updates
- **Features**: Manual approval required, comprehensive error handling

### Docker Images

All Docker images are stored in GitHub Container Registry (GHCR) and tagged with:
- `latest` - Latest build from main branch
- `sha-{commit}` - Specific commit SHA
- `pr-{number}` - Pull request builds

**Image Locations:**
- Backend: `ghcr.io/jfenderson/podium/podium-backend`
- Frontend: `ghcr.io/jfenderson/podium/podium-frontend`

### Manual Deployments

To trigger a manual deployment:

1. **Staging Deployment**:
   - Go to Actions → Deploy to Staging
   - Click "Run workflow"
   - Select branch (typically `main`)
   - Click "Run workflow"

2. **Production Deployment**:
   - Go to Actions → Deploy to Production
   - Click "Run workflow"
   - Enter the Docker image tag (e.g., `latest` or `sha-abc1234`)
   - Optionally skip health checks
   - Click "Run workflow"
   - Approve the deployment in the Environments section

### Required Secrets

See [.github/GITHUB-SECRETS.md](.github/GITHUB-SECRETS.md) for detailed information on all required secrets and how to configure them.

**Key Secrets:**
- `DATABASE_CONNECTION_STRING` - Database connection
- `JWT_SECRET` - JWT signing key (32+ characters)
- `AZURE_STORAGE_CONNECTION_STRING` - File storage
- `GITHUB_TOKEN` - Automatically provided for GHCR access

### Caching Strategy

- **Backend**: NuGet packages cached by `packages.lock.json` hash
- **Frontend**: `node_modules` cached by `package-lock.json` hash
- **Docker**: Build cache using GitHub Actions cache

### Monitoring

- Workflow status badges displayed at the top of this README
- Deployment summaries posted to GitHub Actions
- Health check endpoints:
  - Backend: `/health` and `/ready`
  - Frontend: `/health`

## Prerequisites

### For Local Development

- **[.NET SDK 8.0+](https://dotnet.microsoft.com/download/dotnet/8.0)**: Required for backend development
- **[Node.js 20+](https://nodejs.org/)** and **npm 10+**: Required for frontend development
- **[SQL Server 2022](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)** or **LocalDB**: Database server
- **[Git](https://git-scm.com/)**: Version control

### For Docker Deployment

- **[Docker 20.10+](https://docs.docker.com/get-docker/)**: Container runtime
- **[Docker Compose 2.0+](https://docs.docker.com/compose/install/)**: Multi-container orchestration

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 8.0
- **ORM**: Entity Framework Core 8.0
- **Database**: SQL Server 2022
- **Authentication**: JWT (JSON Web Tokens)
- **Background Jobs**: Hangfire
- **Logging**: Serilog
- **API Documentation**: Swagger/OpenAPI

### Frontend
- **Framework**: Angular 21
- **UI Library**: Tailwind CSS
- **Real-time**: SignalR client
- **Charts**: Chart.js

### Infrastructure
- **Containerization**: Docker
- **Orchestration**: Docker Compose
- **Web Server (Production)**: Nginx (for frontend)
- **Storage**: Azure Blob Storage or AWS S3

## Getting Started

### Environment Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/JFenderson/Podium.git
   cd Podium
   ```

2. **Create environment file:**
   ```bash
   cp .env.example .env
   ```

3. **Configure environment variables:**
   Edit the `.env` file and set your configuration values:
   ```env
   # Database
   ConnectionStrings__DefaultConnection=Server=localhost;Database=PodiumDb;Trusted_Connection=true;MultipleActiveResultSets=true
   
   # JWT
   JWT__Secret=your-super-secret-key-at-least-32-characters-long
   JWT__Issuer=PodiumAPI
   JWT__Audience=PodiumClient
   
   # Storage (choose Azure or AWS)
   StorageProvider=Azure
   AzureStorage__ConnectionString=your-azure-connection-string
   
   # Email
   SendGrid__ApiKey=your-sendgrid-api-key
   ```

   > **Important**: Never commit the `.env` file to version control!

### Local Development

#### Backend

1. **Navigate to the Backend directory:**
   ```bash
   cd Backend/Podium
   ```

2. **Restore NuGet packages:**
   ```bash
   dotnet restore
   ```

3. **Update database:**
   ```bash
   cd Podium.API
   dotnet ef database update
   ```

4. **Run the application:**
   ```bash
   dotnet run
   ```

   The API will be available at: `https://localhost:5001` or `http://localhost:5000`

#### Frontend

1. **Navigate to the Frontend directory:**
   ```bash
   cd Frontend/podium-frontend
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Start the development server:**
   ```bash
   npm start
   ```

   The application will be available at: `http://localhost:4200`

### Docker Deployment

#### Using Docker Compose (Recommended)

1. **Ensure `.env` file is configured** (see [Environment Setup](#environment-setup))

2. **Build and start all services:**
   ```bash
   docker-compose up -d
   ```

   This will start:
   - SQL Server database on port `1433`
   - Backend API on port `5000`
   - Frontend on port `80`

3. **View logs:**
   ```bash
   docker-compose logs -f
   ```

4. **Stop all services:**
   ```bash
   docker-compose down
   ```

5. **Stop and remove volumes (⚠️ deletes data):**
   ```bash
   docker-compose down -v
   ```

#### Building Individual Images

**Backend:**
```bash
cd Backend
docker build -t podium-backend .
docker run -p 5000:5000 --env-file ../.env podium-backend
```

**Frontend:**
```bash
cd Frontend
docker build -t podium-frontend .
docker run -p 80:80 podium-frontend
```

## Configuration

### Environment Variables

See [`.env.example`](.env.example) for a complete list of environment variables.

#### Required Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | Database connection string | `Server=db;Database=PodiumDb;User Id=sa;Password=YourPass123!;TrustServerCertificate=true` |
| `JWT__Secret` | JWT signing key (min 32 chars) | `your-super-secret-key-min-32-characters` |
| `AzureStorage__ConnectionString` or `AWS__AccessKey` | Storage provider credentials | (depends on chosen provider) |

#### Optional Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `JWT__ExpirationMinutes` | JWT token lifetime | `60` |
| `Video__MaxFileSizeMB` | Max video upload size | `500` |
| `RateLimit__PermitLimit` | API rate limit per window | `10` for auth, `60` for general |
| `SecurityHeaders__EnableHSTS` | Enable HSTS header | `false` (dev), `true` (prod) |
| `Serilog__MinimumLevel__Default` | Logging level | `Information` |

### Rate Limiting

Rate limiting is configured in `appsettings.json`:

```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "HttpStatusCode": 429,
    "IpRateLimitPolicies": {
      "IpRules": [
        {
          "Endpoint": "*/api/auth/login",
          "Period": "1m",
          "Limit": 10
        }
      ]
    }
  }
}
```

**Endpoint Limits:**
- Authentication endpoints (`/api/auth/*`): 10 requests/minute
- General API endpoints: 60 requests/minute
- Health check endpoints: Unlimited (whitelisted)

### Security Headers

Security headers are automatically applied to all responses:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Strict-Transport-Security` (production only)
- `Content-Security-Policy`
- `Referrer-Policy: strict-origin-when-cross-origin`

Configure via environment variables:
```env
SecurityHeaders__EnableHSTS=true
SecurityHeaders__HSTSMaxAge=31536000
SecurityHeaders__EnableCSP=true
```

### Logging

Structured logging is implemented with Serilog:

**Console Output:**
- Format: `[HH:mm:ss Level] Message`
- Outputs to Docker logs

**File Output:**
- Location: `logs/podium-{Date}.log`
- Rolling: Daily
- Retention: 7 days

**Configuration:**
```env
Serilog__MinimumLevel__Default=Information
Serilog__MinimumLevel__Override__Microsoft=Warning
```

**Log Levels:**
- `Verbose`: Most detailed
- `Debug`: Internal system events
- `Information`: General application flow (default)
- `Warning`: Abnormal or unexpected events
- `Error`: Errors and exceptions
- `Fatal`: Critical errors causing shutdown

## Health Checks

The application provides health check endpoints for monitoring:

### `/health` - Basic Health Check
Returns `200 OK` if the application is running.

**Response:**
```json
{
  "status": "Healthy",
  "timestamp": "2026-01-15T08:44:13Z",
  "service": "Podium API"
}
```

**Usage:**
```bash
curl http://localhost:5000/health
```

### `/ready` - Readiness Check
Returns `200 OK` if the application is ready to serve traffic (checks database connectivity).

**Response (Healthy):**
```json
{
  "status": "Ready",
  "timestamp": "2026-01-15T08:44:13Z",
  "service": "Podium API",
  "checks": {
    "database": "Connected"
  }
}
```

**Response (Unhealthy):**
```json
{
  "status": "NotReady",
  "timestamp": "2026-01-15T08:44:13Z",
  "service": "Podium API",
  "checks": {
    "database": "Disconnected"
  },
  "error": "Unable to connect to database"
}
```
Returns `503 Service Unavailable` if dependencies are not available.

**Usage:**
```bash
curl http://localhost:5000/ready
```

### Docker Health Checks

Health checks are automatically configured in Docker:
- Backend: Checks `/health` every 30 seconds
- Frontend: Checks nginx `/health` endpoint every 30 seconds
- Database: Checks SQL Server connectivity every 30 seconds

View health status:
```bash
docker-compose ps
```

## Security

Security is a top priority. See [SECURITY.md](SECURITY.md) for:
- Vulnerability reporting process
- Security best practices
- Authentication & authorization details
- Secrets management guidelines
- Dependency management

**Key Security Features:**
- JWT-based authentication
- Role-based access control (RBAC)
- Rate limiting on API endpoints
- Security headers (CSP, HSTS, etc.)
- Encrypted database connections
- Secrets stored in environment variables
- Regular dependency updates
- Structured audit logging

## API Documentation

### Swagger UI

Interactive API documentation is available in development mode:
- URL: `http://localhost:5000/swagger`
- Provides interactive API testing
- Shows all available endpoints, request/response schemas

### Authentication

Most API endpoints require authentication using JWT tokens.

**1. Register a new user:**
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Student"
}
```

**2. Login:**
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "def50200a1b2c3d4...",
  "expiresAt": "2026-01-15T09:44:13Z",
  "userId": "abc-123-def",
  "email": "user@example.com"
}
```

**3. Use the token:**
```http
GET /api/students
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### Key Endpoints

- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Authenticate user
- `GET /api/students` - List students (Band Staff/Director)
- `POST /api/videos/upload` - Upload performance video
- `GET /api/scholarships` - List scholarship offers
- `GET /api/bands` - List college bands
- `POST /api/events` - Create recruitment event

## Deployment Considerations

### Production Checklist

- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Generate strong `JWT__Secret` (32+ characters)
- [ ] Configure HTTPS/TLS certificates
- [ ] Enable HSTS (`SecurityHeaders__EnableHSTS=true`)
- [ ] Set up production database with backups
- [ ] Configure production storage (Azure/AWS)
- [ ] Set up email service (SendGrid)
- [ ] Configure production `AllowedOrigins` for CORS
- [ ] Set up monitoring and alerting
- [ ] Configure log aggregation
- [ ] Review and tighten security headers
- [ ] Perform security scan
- [ ] Load test the application
- [ ] Set up CI/CD pipeline

### Recommended Infrastructure

- **Web Server**: Nginx or IIS as reverse proxy
- **SSL/TLS**: Let's Encrypt or commercial certificate
- **Database**: Azure SQL Database or AWS RDS
- **Storage**: Azure Blob Storage or AWS S3
- **Monitoring**: Application Insights, CloudWatch, or Datadog
- **Container Orchestration**: Kubernetes or Azure Container Apps (for scale)

### Scaling Considerations

- Use managed database services for reliability
- Implement caching (Redis) for frequently accessed data
- Use CDN for static assets
- Configure auto-scaling for containers
- Separate read and write database operations
- Implement queue-based video processing

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow C# and Angular coding conventions
- Write unit tests for new features
- Update documentation as needed
- Ensure all tests pass before submitting PR
- Keep commits atomic and well-described

## License

[Add your license information here]

## Support

- **Issues**: [GitHub Issues](https://github.com/JFenderson/Podium/issues)
- **Security**: See [SECURITY.md](SECURITY.md)
- **Email**: [support@podiumapp.com]

---

Made with ❤️ for the college band community
