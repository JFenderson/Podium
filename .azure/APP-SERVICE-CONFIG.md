# Azure App Service Configuration Guide

This document describes the Azure App Service configuration for the Podium application.

## Table of Contents

- [Backend App Service Configuration](#backend-app-service-configuration)
- [Frontend App Service Configuration](#frontend-app-service-configuration)
- [Configuration Steps](#configuration-steps)
- [Managed Identity Setup](#managed-identity-setup)
- [Auto-Scaling Rules](#auto-scaling-rules)
- [Health Checks](#health-checks)
- [Deployment Slots](#deployment-slots)

---

## Backend App Service Configuration

### Basic Settings

| Setting | Value | Description |
|---------|-------|-------------|
| **Name** | `podium-backend-{environment}` | App Service name |
| **Runtime** | .NET 8.0 (Linux) | Application runtime |
| **Region** | East US | Azure region |
| **Service Plan** | P1v2 Premium | Production-grade plan |
| **Operating System** | Linux | Container host OS |
| **Always On** | Enabled | Keeps app loaded |
| **HTTP Version** | 2.0 | HTTP/2 support |
| **Minimum TLS Version** | 1.2 | Security requirement |
| **HTTPS Only** | Enabled | Force HTTPS |
| **FTP State** | Disabled | Security best practice |

### Application Settings

Configure these in Azure Portal > App Service > Configuration > Application Settings:

```json
{
  "ASPNETCORE_ENVIRONMENT": "Production",
  "ASPNETCORE_URLS": "http://+:5000",
  "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=...;IngestionEndpoint=https://...",
  "ENABLE_APPLICATION_INSIGHTS": "true",
  "LOG_LEVEL": "Warning",
  
  "JWT__Issuer": "PodiumAPI",
  "JWT__Audience": "PodiumClient",
  "JWT__ExpirationMinutes": "60",
  "JWT__Secret": "@Microsoft.KeyVault(SecretUri=https://podium-kv-production.vault.azure.net/secrets/jwt-secret/)",
  
  "AzureStorage__ContainerName": "podium-videos",
  "AzureStorage__ConnectionString": "@Microsoft.KeyVault(SecretUri=https://podium-kv-production.vault.azure.net/secrets/storage-connection-string/)",
  
  "SendGrid__FromEmail": "noreply@podiumapp.com",
  "SendGrid__FromName": "Podium",
  "SendGrid__ApiKey": "@Microsoft.KeyVault(SecretUri=https://podium-kv-production.vault.azure.net/secrets/sendgrid-api-key/)",
  
  "AllowedOrigins__0": "https://www.podiumapp.com",
  "AllowedOrigins__1": "https://app.podiumapp.com",
  "AllowedOrigins__2": "https://podium-cdn.azureedge.net",
  
  "App__ClientUrl": "https://www.podiumapp.com",
  
  "RateLimit__EnableRateLimiting": "true",
  "RateLimit__PermitLimit": "60",
  "RateLimit__WindowSeconds": "60",
  
  "SecurityHeaders__EnableHSTS": "true",
  "SecurityHeaders__HSTSMaxAge": "31536000",
  "SecurityHeaders__EnableCSP": "true",
  
  "Video__MaxFileSizeMB": "500",
  "Video__TranscodingEnabled": "true",
  
  "Serilog__MinimumLevel__Default": "Warning",
  "Serilog__MinimumLevel__Override__Microsoft": "Error"
}
```

### Connection Strings

Configure in Azure Portal > App Service > Configuration > Connection Strings:

| Name | Value | Type |
|------|-------|------|
| **DefaultConnection** | `@Microsoft.KeyVault(SecretUri=https://podium-kv-production.vault.azure.net/secrets/sql-connection-string/)` | SQLAzure |

### Container Settings

| Setting | Value |
|---------|-------|
| **Image Source** | Docker Hub or other registry |
| **Registry URL** | `https://ghcr.io` |
| **Image and Tag** | `ghcr.io/jfenderson/podium/podium-backend:latest` |
| **Registry Username** | GitHub username |
| **Registry Password** | GitHub Personal Access Token (PAT) |
| **Continuous Deployment** | Enabled (webhook) |
| **Startup Command** | `/azure/startup.sh` (if custom startup needed) |

---

## Frontend App Service Configuration

### Basic Settings

| Setting | Value | Description |
|---------|-------|-------------|
| **Name** | `podium-frontend-{environment}` | App Service name |
| **Runtime** | Node 20 LTS (Linux) | Application runtime |
| **Region** | East US | Azure region |
| **Service Plan** | P1v2 Premium (shared with backend) | Production-grade plan |
| **Operating System** | Linux | Container host OS |
| **Always On** | Enabled | Keeps app loaded |
| **HTTP Version** | 2.0 | HTTP/2 support |
| **Minimum TLS Version** | 1.2 | Security requirement |
| **HTTPS Only** | Enabled | Force HTTPS |
| **FTP State** | Disabled | Security best practice |

### Application Settings

```json
{
  "API_URL": "https://podium-backend-production.azurewebsites.net",
  "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=...;IngestionEndpoint=https://...",
  "NODE_ENV": "production"
}
```

### Container Settings

| Setting | Value |
|---------|-------|
| **Image Source** | Docker Hub or other registry |
| **Registry URL** | `https://ghcr.io` |
| **Image and Tag** | `ghcr.io/jfenderson/podium/podium-frontend:latest` |
| **Continuous Deployment** | Enabled |

---

## Configuration Steps

### Step 1: Create App Service via Azure Portal

1. Navigate to Azure Portal
2. Create Resource > Web App
3. Fill in basic information:
   - Subscription: Select your subscription
   - Resource Group: `podium-production-rg`
   - Name: `podium-backend-production`
   - Publish: Container
   - Operating System: Linux
   - Region: East US
4. Select App Service Plan: P1v2 (or create new)
5. Review and Create

### Step 2: Configure Container

1. Go to App Service > Deployment Center
2. Select Container settings:
   - Source: Docker Hub or Other registries
   - Registry: `https://ghcr.io`
   - Image: `ghcr.io/jfenderson/podium/podium-backend`
   - Tag: `latest` or specific SHA
3. Configure registry credentials
4. Enable Continuous Deployment webhook
5. Save changes

### Step 3: Configure Application Settings

1. Go to App Service > Configuration > Application Settings
2. Add each setting from the table above
3. Use Key Vault references for secrets:
   - Format: `@Microsoft.KeyVault(SecretUri=https://vault-name.vault.azure.net/secrets/secret-name/)`
4. Save changes (will restart app)

### Step 4: Configure Connection Strings

1. Go to App Service > Configuration > Connection Strings
2. Add connection string:
   - Name: `DefaultConnection`
   - Value: Key Vault reference
   - Type: `SQLAzure`
3. Save changes

### Step 5: Enable Managed Identity

1. Go to App Service > Identity
2. System assigned tab
3. Status: On
4. Save
5. Copy Object (principal) ID for Key Vault access policy

### Step 6: Grant Key Vault Access

1. Go to Key Vault > Access policies
2. Add Access Policy:
   - Secret permissions: Get, List
   - Select principal: Use Object ID from Step 5
3. Save changes

### Step 7: Configure Auto-Scaling

See [Auto-Scaling Rules](#auto-scaling-rules) section below

### Step 8: Configure Health Checks

See [Health Checks](#health-checks) section below

---

## Managed Identity Setup

### Enable System-Assigned Identity

**Via Azure Portal:**
1. App Service > Identity > System assigned
2. Status: On
3. Save

**Via Azure CLI:**
```bash
az webapp identity assign \
  --resource-group podium-production-rg \
  --name podium-backend-production
```

### Configure Key Vault Access Policy

```bash
# Get the principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --resource-group podium-production-rg \
  --name podium-backend-production \
  --query principalId -o tsv)

# Grant Key Vault access
az keyvault set-policy \
  --name podium-kv-production \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

### Using Managed Identity in Application

The application automatically uses managed identity to access Key Vault when using Key Vault references in app settings. No code changes needed.

---

## Auto-Scaling Rules

### Configure via Azure Portal

1. Go to App Service Plan > Scale out (App Service plan)
2. Enable autoscale
3. Add rules:

**Scale Out Rule (CPU):**
- Metric: CPU Percentage
- Operator: Greater than
- Threshold: 70%
- Duration: 5 minutes
- Action: Increase count by 1
- Cool down: 5 minutes

**Scale In Rule (CPU):**
- Metric: CPU Percentage
- Operator: Less than
- Threshold: 40%
- Duration: 10 minutes
- Action: Decrease count by 1
- Cool down: 10 minutes

**Instance Limits:**
- Minimum: 1
- Maximum: 8
- Default: 1

### Configure via Azure CLI

```bash
# Create autoscale setting
az monitor autoscale create \
  --resource-group podium-production-rg \
  --name podium-autoscale \
  --resource /subscriptions/{subscription-id}/resourceGroups/podium-production-rg/providers/Microsoft.Web/serverfarms/podium-asp-production \
  --min-count 1 \
  --max-count 8 \
  --count 1

# Add scale out rule
az monitor autoscale rule create \
  --resource-group podium-production-rg \
  --autoscale-name podium-autoscale \
  --condition "CpuPercentage > 70 avg 5m" \
  --scale out 1

# Add scale in rule
az monitor autoscale rule create \
  --resource-group podium-production-rg \
  --autoscale-name podium-autoscale \
  --condition "CpuPercentage < 40 avg 10m" \
  --scale in 1
```

### Memory-Based Scaling (Optional)

Add memory-based rules similarly:
- Scale out when Memory > 85%
- Scale in when Memory < 60%

---

## Health Checks

### Configure Health Check Path

1. Go to App Service > Health check
2. Enable: Yes
3. Path: `/health`
4. Load balancing: Enable

**Health Check Configuration:**
- Interval: 30 seconds
- Unhealthy threshold: 3 failed requests
- Timeout: 5 seconds

### Via Azure CLI

```bash
az webapp config set \
  --resource-group podium-production-rg \
  --name podium-backend-production \
  --health-check-path "/health"
```

### Health Check Endpoints

**Backend:**
- `/health` - Basic liveness check (200 OK if running)
- `/ready` - Readiness check (200 OK if ready to serve traffic, includes DB check)

**Frontend:**
- `/health` - Basic check (served by nginx)

### Monitoring Health Status

View health status in:
- Azure Portal > App Service > Health check
- Application Insights > Availability
- Azure Monitor > Metrics

---

## Deployment Slots

### Create Staging Slot

Deployment slots allow zero-downtime deployments with swap functionality.

**Via Azure Portal:**
1. Go to App Service > Deployment slots
2. Add Slot
3. Name: `staging`
4. Clone settings from: Production
5. Create

**Via Azure CLI:**
```bash
az webapp deployment slot create \
  --resource-group podium-production-rg \
  --name podium-backend-production \
  --slot staging \
  --configuration-source podium-backend-production
```

### Deploy to Staging Slot

```bash
# Deploy container to staging slot
az webapp config container set \
  --resource-group podium-production-rg \
  --name podium-backend-production \
  --slot staging \
  --docker-custom-image-name ghcr.io/jfenderson/podium/podium-backend:sha-abc1234

# Restart staging slot
az webapp restart \
  --resource-group podium-production-rg \
  --name podium-backend-production \
  --slot staging
```

### Swap Slots (Zero-Downtime Deployment)

After validating staging:

```bash
# Swap staging to production
az webapp deployment slot swap \
  --resource-group podium-production-rg \
  --name podium-backend-production \
  --slot staging \
  --target-slot production
```

### Slot-Specific Settings

Mark settings as "slot settings" to prevent them from swapping:
- Database connection strings (if different per slot)
- API keys specific to staging/production
- Feature flags

**Via Portal:**
1. Configuration > Application settings
2. Check "Deployment slot setting" for specific settings

---

## Troubleshooting

### App Won't Start

**Check:**
1. Application logs: `az webapp log tail`
2. Container logs: Deployment Center > Logs
3. Key Vault access: Verify managed identity has permissions
4. Startup command: Check if custom startup script is working

### Key Vault References Not Working

**Verify:**
1. Managed identity is enabled
2. Key Vault access policy includes App Service identity
3. Secret name in reference is correct
4. Key Vault URI is correct

### High CPU/Memory Usage

**Actions:**
1. Check Application Insights for slow endpoints
2. Review auto-scaling rules
3. Consider scaling up to higher tier (P2v2, P3v2)
4. Optimize application code

---

*Last Updated: 2026-01-15*
*Version: 1.0*
