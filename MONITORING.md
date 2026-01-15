# Podium Application Monitoring and Observability Guide

This guide provides comprehensive instructions for setting up and using monitoring, logging, and observability features in the Podium application.

## Table of Contents
1. [Application Insights Setup](#application-insights-setup)
2. [Dashboard Configuration](#dashboard-configuration)
3. [Alert Setup](#alert-setup)
4. [Log Query Examples (KQL)](#log-query-examples-kql)
5. [Health Checks](#health-checks)
6. [Troubleshooting Runbook](#troubleshooting-runbook)
7. [Log Retention Policy](#log-retention-policy)
8. [Local Development](#local-development)

---

## 1. Application Insights Setup

### Prerequisites
- Active Azure subscription
- Azure CLI or access to Azure Portal

### Step 1: Create Application Insights Resource

#### Using Azure Portal:
1. Navigate to [Azure Portal](https://portal.azure.com)
2. Click **Create a resource** → Search for "Application Insights"
3. Fill in the required fields:
   - **Resource Group**: Select or create a new resource group
   - **Name**: `podium-app-insights` (or your preferred name)
   - **Region**: Choose a region close to your deployment
   - **Resource Mode**: Workspace-based (recommended)
4. Click **Review + Create** → **Create**

#### Using Azure CLI:
```bash
# Create resource group if needed
az group create --name podium-rg --location eastus

# Create Application Insights resource
az monitor app-insights component create \
  --app podium-app-insights \
  --location eastus \
  --resource-group podium-rg \
  --workspace /subscriptions/{subscription-id}/resourcegroups/{rg-name}/providers/microsoft.operationalinsights/workspaces/{workspace-name}
```

### Step 2: Get Connection String

#### Via Azure Portal:
1. Navigate to your Application Insights resource
2. In the left menu, click **Overview**
3. Copy the **Connection String** (not the Instrumentation Key)
   - Format: `InstrumentationKey=xxx;IngestionEndpoint=https://...`

#### Via Azure CLI:
```bash
az monitor app-insights component show \
  --app podium-app-insights \
  --resource-group podium-rg \
  --query connectionString --output tsv
```

### Step 3: Configure Environment Variable

Add the connection string to your environment:

#### For Development (.env file):
```bash
APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=xxx;IngestionEndpoint=https://..."
ENABLE_APPLICATION_INSIGHTS=true
```

#### For Production (Azure App Service):
1. Navigate to your App Service in Azure Portal
2. Go to **Configuration** → **Application settings**
3. Add new application setting:
   - **Name**: `APPLICATIONINSIGHTS_CONNECTION_STRING`
   - **Value**: Your connection string
4. Click **Save**

#### For Docker/Kubernetes:
Add to your deployment configuration:
```yaml
env:
  - name: APPLICATIONINSIGHTS_CONNECTION_STRING
    value: "InstrumentationKey=xxx;IngestionEndpoint=https://..."
  - name: ENABLE_APPLICATION_INSIGHTS
    value: "true"
```

### Step 4: Verify Data Flow

After deploying your application:
1. Go to Application Insights in Azure Portal
2. Click **Live Metrics** (should show real-time data within 1-2 minutes)
3. Navigate to **Logs** and run a simple query:
   ```kql
   requests
   | where timestamp > ago(1h)
   | take 10
   ```
4. If no data appears after 5 minutes, check the [Troubleshooting](#troubleshooting-runbook) section

---

## 2. Dashboard Configuration

### Import Pre-built Dashboard

The repository includes a pre-configured dashboard JSON file: `Backend/Podium/Podium.API/ApplicationInsightsDashboard.json`

#### Steps to Import:
1. Navigate to Azure Portal → Application Insights
2. Click **Workbooks** (in the left menu)
3. Click **+ New** → **Advanced Editor** (</> icon in toolbar)
4. Replace the default JSON with the content from `ApplicationInsightsDashboard.json`
5. Click **Apply** → **Done Editing**
6. Click **Save** and give your workbook a name (e.g., "Podium Application Dashboard")
7. Choose:
   - **Title**: Podium Application Dashboard
   - **Subscription**: Your subscription
   - **Resource Group**: Same as Application Insights
   - **Location**: Same as Application Insights

### Customize Tiles

To modify dashboard tiles:
1. Open the workbook
2. Click **Edit** mode
3. Click on any tile to edit its query
4. Modify the KQL query as needed
5. Click **Done Editing** → **Save**

### Share Dashboard with Team

1. Open your workbook
2. Click **Share** button
3. Options:
   - **Publish to gallery**: Share with your organization
   - **Get link**: Share specific link with read or edit permissions
   - **Export**: Download as JSON to share via email/Git

### Key Dashboard Tiles Explained

| Tile Name | Purpose | Alert Threshold |
|-----------|---------|----------------|
| Request Rate | Monitor traffic volume | N/A |
| Average Response Time | Track API performance | >2000ms |
| Failed Requests | Monitor error rates | >5% |
| Active Users | Track concurrent users | N/A |
| Video Upload Success Rate | Monitor upload health | <80% |
| Database Query Performance | Identify slow queries | >500ms |
| Exception Count | Track application errors | >20/5min |

---

## 3. Alert Setup

### Configure Action Groups

Action groups define who gets notified when alerts trigger.

#### Create Email Action Group:
1. Azure Portal → **Monitor** → **Alerts** → **Action groups**
2. Click **+ Create**
3. Fill in details:
   - **Action group name**: `podium-email-alerts`
   - **Display name**: Podium Alerts
4. Under **Actions** tab:
   - **Action type**: Email/SMS/Push/Voice
   - **Name**: Email Admins
   - **Email**: admin@podiumapp.com
5. Click **Review + create** → **Create**

#### Create SMS Action Group:
Repeat above steps but choose SMS as action type for critical alerts.

### Configure Alert Rules

The repository includes alert definitions in `Backend/Podium/Podium.API/AlertRules.json`. Configure each manually:

#### Example: High Error Rate Alert

1. Navigate to Application Insights → **Alerts** → **+ Create** → **Alert rule**
2. **Scope**: Select your Application Insights resource
3. **Condition**:
   - Signal: **Custom log search**
   - Query:
     ```kql
     requests
     | where timestamp > ago(5m)
     | summarize FailureRate = (countif(success == false) * 100.0) / count()
     | where FailureRate > 5
     ```
   - Threshold: Greater than 5
   - Evaluation frequency: 1 minute
   - Look back period: 5 minutes
4. **Actions**: Select `podium-email-alerts` action group
5. **Alert rule details**:
   - **Alert rule name**: High Error Rate
   - **Severity**: Critical (Sev 0)
   - **Description**: Error rate exceeds 5% over 5 minutes
6. Click **Create alert rule**

### Recommended Alert Configuration

Refer to `AlertRules.json` for complete alert definitions. Key alerts to configure:

| Alert | Query Signal | Threshold | Severity |
|-------|-------------|-----------|----------|
| High Error Rate | Custom log | >5% in 5min | Critical |
| Slow API Responses | Average duration | >2000ms | Warning |
| Failed Auth Spike | Custom events | >10 in 1min | Critical |
| Database Failures | Dependencies failed | >0 in 1min | Critical |
| High Memory Usage | Custom metrics | >80% | Warning |

### Alert Tuning Tips

- **Start conservatively**: Begin with higher thresholds, then lower based on baseline
- **Use staging**: Test alerts in a staging environment first
- **Avoid alert fatigue**: Don't create too many noisy alerts
- **Review weekly**: Check alert history and adjust thresholds
- **Document suppressions**: If you suppress an alert, document why

---

## 4. Log Query Examples (KQL)

Kusto Query Language (KQL) is used to query Application Insights data.

### Basic Queries

#### All Failed Authentication Attempts (Last 24h)
```kql
customEvents
| where name == "AuthenticationFailure"
| where timestamp > ago(24h)
| project timestamp, 
          userId = tostring(customDimensions.UserId),
          reason = tostring(customDimensions.Reason),
          ipAddress = tostring(customDimensions.IpAddress)
| order by timestamp desc
```

#### Slowest API Endpoints
```kql
requests
| where timestamp > ago(1h)
| summarize 
    AvgDuration = avg(duration),
    P95Duration = percentile(duration, 95),
    RequestCount = count()
    by name
| where AvgDuration > 500  // Only show endpoints >500ms
| order by AvgDuration desc
| take 20
```

#### Trace Request by Correlation ID
```kql
let correlationId = "YOUR_CORRELATION_ID_HERE";
union requests, dependencies, exceptions, traces
| where operation_Id == correlationId or 
        customDimensions.RequestId == correlationId or
        customDimensions.CorrelationId == correlationId
| project timestamp, itemType, name, message, 
          duration, success, resultCode
| order by timestamp asc
```

#### Exceptions Grouped by Type
```kql
exceptions
| where timestamp > ago(7d)
| summarize 
    Count = count(),
    Sample = any(outerMessage),
    FirstSeen = min(timestamp),
    LastSeen = max(timestamp)
    by type, outerMessage
| order by Count desc
| take 50
```

#### Video Upload Performance Analysis
```kql
customEvents
| where name in ("VideoUploadSuccess", "VideoUploadFailure")
| where timestamp > ago(24h)
| extend 
    fileSizeBytes = tolong(customDimensions.FileSizeBytes),
    durationMs = todouble(customDimensions.DurationMs),
    success = tobool(customDimensions.Success)
| summarize 
    TotalUploads = count(),
    SuccessRate = countif(success) * 100.0 / count(),
    AvgFileSize = avg(fileSizeBytes) / 1024 / 1024,  // MB
    AvgDuration = avg(durationMs) / 1000,  // seconds
    P95Duration = percentile(durationMs, 95) / 1000
| project 
    TotalUploads,
    SuccessRate = round(SuccessRate, 2),
    AvgFileSizeMB = round(AvgFileSize, 2),
    AvgDurationSec = round(AvgDuration, 2),
    P95DurationSec = round(P95Duration, 2)
```

### Advanced Queries

#### User Journey Analysis
```kql
let userId = "USER_ID_HERE";
customEvents
| where timestamp > ago(1d)
| where customDimensions.UserId == userId or 
        customDimensions.AuthenticatedUserId == userId
| project timestamp, name, customDimensions
| order by timestamp asc
| take 100
```

#### Database Query Performance
```kql
dependencies
| where type == "SQL"
| where timestamp > ago(1h)
| summarize 
    Count = count(),
    AvgDuration = avg(duration),
    P95Duration = percentile(duration, 95),
    FailureCount = countif(success == false)
    by name
| extend FailureRate = (FailureCount * 100.0) / Count
| order by AvgDuration desc
| take 50
```

---

## 5. Health Checks

The application exposes three health check endpoints:

### Endpoints

| Endpoint | Purpose | Use Case |
|----------|---------|----------|
| `/health` | Full health status with details | General monitoring, dashboards |
| `/health/ready` | Readiness check (all dependencies) | Kubernetes readiness probe |
| `/health/live` | Liveness check (basic health) | Kubernetes liveness probe |

### Health Check Components

| Component | What It Checks | Healthy | Degraded | Unhealthy |
|-----------|---------------|---------|----------|-----------|
| Database | SQL Server connectivity & query time | <500ms | 500-1000ms | No connection |
| BlobStorage | Azure Storage access | Accessible | N/A | Access denied |
| Hangfire | Background jobs status | <10 failures | 10-50 failures | >50 failures |
| Memory | System memory usage | <80% | 80-90% | >90% |
| DiskSpace | Available disk space | >10GB | 5-10GB | <5GB |

### Sample Health Check Response

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "database": {
      "status": "Healthy",
      "description": "Database is responsive",
      "duration": "00:00:00.0450000",
      "data": {
        "DurationMs": 45
      }
    },
    "memory": {
      "status": "Healthy",
      "description": "Memory usage is normal",
      "data": {
        "AllocatedMemoryMB": 256,
        "TotalMemoryMB": 2048,
        "UsagePercentage": 12.5
      }
    }
  }
}
```

### Kubernetes Integration

#### Deployment YAML Example:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: podium-api
spec:
  template:
    spec:
      containers:
      - name: podium-api
        image: podium-api:latest
        livenessProbe:
          httpGet:
            path: /health/live
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 5000
          initialDelaySeconds: 15
          periodSeconds: 5
          timeoutSeconds: 5
          failureThreshold: 3
```

### Health Check Intervals

Configure check frequency via environment variable:
```bash
HEALTH_CHECK_INTERVAL_SECONDS=30  # Default: 30 seconds
```

### Monitoring Health Checks

1. **Azure Application Insights**: Health check failures are logged as traces
2. **Health Checks UI**: Access at `/healthchecks-ui` for visual dashboard
3. **Prometheus**: Health check metrics can be exported (requires additional setup)

---

## 6. Troubleshooting Runbook

### Common Issues and Solutions

#### Issue: Application Insights Not Receiving Data

**Symptoms:**
- Live Metrics shows no data
- No requests/dependencies in logs after 5 minutes
- Dashboard tiles are empty

**Solutions:**
1. Verify connection string is set correctly:
   ```bash
   # Check environment variable
   echo $APPLICATIONINSIGHTS_CONNECTION_STRING
   ```
2. Ensure `ENABLE_APPLICATION_INSIGHTS=true`
3. Check application logs for initialization errors:
   ```bash
   # View logs
   tail -f logs/podium-*.log | grep -i "application insights"
   ```
4. Verify network connectivity to Azure:
   ```bash
   # Test ingestion endpoint
   curl -I https://YOUR-REGION.in.applicationinsights.azure.com
   ```
5. Check firewall rules allow outbound HTTPS to *.applicationinsights.azure.com

**Still not working?**
- Restart the application
- Check Azure Application Insights status page
- Contact Azure support if Azure service is degraded

---

#### Issue: Log Files Growing Too Large

**Symptoms:**
- Disk space health check reports degraded/unhealthy
- Log directory exceeds expected size
- Application performance degradation

**Solutions:**
1. Check current log file size:
   ```bash
   du -sh logs/
   ls -lh logs/ | tail -20
   ```
2. Verify retention policy in `appsettings.json`:
   ```json
   "retainedFileCountLimit": 30
   ```
3. Manually clean old logs:
   ```bash
   # Delete logs older than 30 days
   find logs/ -name "*.log" -mtime +30 -delete
   ```
4. Adjust log level to reduce verbosity:
   ```json
   "MinimumLevel": {
     "Default": "Warning"  // Change from Information
   }
   ```
5. Consider using Application Insights instead of file logging in production

---

#### Issue: Health Checks Failing

**Symptoms:**
- `/health` endpoint returns Unhealthy status
- Kubernetes pods are not ready
- Health Checks UI shows red status

**Diagnosis:**
1. Check which component is failing:
   ```bash
   curl http://localhost:5000/health | jq
   ```
2. Review component-specific data in response

**Per-Component Solutions:**

| Component | Solution |
|-----------|----------|
| Database | Check connection string, verify SQL Server is running, check network |
| BlobStorage | Verify Azure Storage connection string and container exists |
| Hangfire | Check Hangfire dashboard at `/hangfire`, restart failed jobs |
| Memory | Restart application, investigate memory leaks, scale up resources |
| DiskSpace | Clean logs, expand disk, move data to different volume |

---

#### Issue: High Memory Usage Alerts

**Symptoms:**
- Memory health check reports degraded
- Application becomes slow or unresponsive
- Out of Memory exceptions

**Solutions:**
1. Check current memory usage:
   ```bash
   # Linux
   free -m
   
   # View process memory
   ps aux --sort=-%mem | head -10
   ```
2. Review memory metrics in Application Insights:
   ```kql
   customMetrics
   | where name == "MemoryUsage"
   | where timestamp > ago(24h)
   | render timechart
   ```
3. Investigate memory leaks:
   - Enable memory profiling
   - Check for unclosed database connections
   - Review large object allocations
4. Short-term fix: Restart application
5. Long-term fix:
   - Scale up (more memory)
   - Fix memory leaks in code
   - Implement caching strategies

---

#### Issue: Slow Request Performance

**Symptoms:**
- Average response time >2000ms
- Users complain about slow page loads
- Performance tracking middleware logs warnings

**Diagnosis:**
1. Identify slow endpoints:
   ```kql
   requests
   | where timestamp > ago(1h)
   | summarize avg(duration) by name
   | where avg_duration > 2000
   | order by avg_duration desc
   ```
2. Check slow database queries:
   ```kql
   dependencies
   | where type == "SQL"
   | where duration > 500
   | order by timestamp desc
   | take 50
   ```

**Solutions:**
1. **Database optimization:**
   - Add missing indexes
   - Optimize queries
   - Enable query caching
2. **API optimization:**
   - Implement response caching
   - Use async/await properly
   - Reduce payload sizes
3. **Infrastructure:**
   - Scale out (add more instances)
   - Scale up (more CPU/memory)
   - Add CDN for static assets

---

### Performance Optimization Tips

1. **Caching:**
   ```csharp
   // Add caching to frequently accessed data
   services.AddMemoryCache();
   services.AddResponseCaching();
   ```

2. **Database Connection Pooling:**
   ```
   Server=...;Database=...;Min Pool Size=10;Max Pool Size=100
   ```

3. **Asynchronous Operations:**
   ```csharp
   // Use async methods
   await _repository.GetDataAsync();
   ```

4. **Compression:**
   ```csharp
   services.AddResponseCompression();
   ```

---

## 7. Log Retention Policy

### Environment-Specific Policies

| Environment | File Logs | Application Insights | Notes |
|-------------|-----------|---------------------|-------|
| Development | 7 days | Not enabled | Local files only |
| Staging | 30 days | 30 days | Test alert configuration |
| Production | 90 days | 90 days | Full observability |

### Configure Retention

#### File Logs (appsettings.json):
```json
{
  "Serilog": {
    "WriteTo": [{
      "Name": "File",
      "Args": {
        "retainedFileCountLimit": 30  // Number of daily files to keep
      }
    }]
  }
}
```

#### Application Insights:
1. Azure Portal → Application Insights → **Usage and estimated costs**
2. Click **Data Retention**
3. Set retention period (30-730 days)
4. Click **Apply**

**Note:** Longer retention = higher costs. Archive old data to Azure Storage for cost savings.

### Data Export for Compliance

For compliance/audit requirements, export Application Insights data:

1. **Continuous Export** (recommended):
   - Azure Portal → Application Insights → **Configure** → **Continuous export**
   - Export to Azure Storage Account
   - Set up retention policy on Storage Account

2. **Manual Export:**
   ```kql
   // Run query and export results
   requests
   | where timestamp between (datetime(2024-01-01) .. datetime(2024-01-31))
   | project timestamp, name, duration, resultCode
   ```
   - Click **Export** → **To CSV/Excel**

---

## 8. Local Development

### Disable Application Insights Locally

To avoid sending telemetry during development:

**.env file:**
```bash
ENABLE_APPLICATION_INSIGHTS=false
```

Or leave connection string empty:
```bash
APPLICATIONINSIGHTS_CONNECTION_STRING=
```

### View Logs in Development

#### Console Output:
Logs are printed to console with colorized output from Serilog.

#### File Logs:
```bash
# Tail logs in real-time
tail -f logs/podium-$(date +%Y%m%d).log

# Search logs
grep "ERROR" logs/*.log
grep "AuthenticationFailure" logs/*.log
```

#### Structured Log Querying:
Use tools like [jq](https://stedolan.github.io/jq/) for JSON logs:
```bash
# If using JSON output format
cat logs/podium-*.log | jq 'select(.Level == "Error")'
```

### Test Health Checks Locally

```bash
# Basic health check
curl http://localhost:5000/health | jq

# Readiness check (all dependencies)
curl http://localhost:5000/health/ready | jq

# Liveness check
curl http://localhost:5000/health/live | jq

# Health Checks UI
open http://localhost:5000/healthchecks-ui
```

### Local Application Insights Testing

For local testing with Application Insights enabled:

1. Create a separate Application Insights resource for development
2. Use a different connection string for local testing
3. Add tag to distinguish development telemetry:
   ```csharp
   telemetry.Context.Properties["Environment"] = "Development";
   ```

### Mock Services for Testing

To avoid external dependencies during local development:

```csharp
// In Program.cs or ServiceExtensions.cs
if (builder.Environment.IsDevelopment())
{
    // Use mock services
    services.AddScoped<ITelemetryService, MockTelemetryService>();
}
```

---

## Additional Resources

- [Application Insights Documentation](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [KQL Reference](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/)
- [Serilog Documentation](https://serilog.net/)
- [ASP.NET Core Health Checks](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)

---

## Support

For issues or questions:
- **Internal Support**: Contact DevOps team
- **Azure Issues**: Create support ticket in Azure Portal
- **Application Bugs**: Create issue in GitHub repository

---

*Last Updated: January 2026*
