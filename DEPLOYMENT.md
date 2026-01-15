# Podium Deployment Runbook

This document provides step-by-step instructions for deploying the Podium application to staging and production environments.

## Table of Contents

- [Pre-Deployment Checklist](#pre-deployment-checklist)
- [Deployment Steps](#deployment-steps)
- [Rollback Procedure](#rollback-procedure)
- [Post-Deployment](#post-deployment)
- [Troubleshooting](#troubleshooting)

---

## Pre-Deployment Checklist

Complete this checklist before every production deployment. For staging deployments, items marked with ⭐ are required.

### Infrastructure & Configuration ⭐

- [ ] All Azure resources are healthy and running
- [ ] Resource group exists for target environment
- [ ] All required secrets are present in Azure Key Vault
- [ ] Key Vault access policies are configured for App Service managed identities
- [ ] SSL certificates are valid (not expiring within 30 days)
- [ ] DNS configuration is correct

### Database ⭐

- [ ] **Database backup created and verified** (CRITICAL)
  ```bash
  cd database/migrations
  ./backup-database.sh production
  ./restore-test.sh production  # Verify backup is restorable
  ```
- [ ] Database migrations tested successfully on staging
  ```bash
  ./verify-migrations.sh staging
  ```
- [ ] Migration scripts reviewed and approved by team
- [ ] Database connection strings validated
- [ ] Point-in-time restore tested within last 7 days

### Application Code

- [ ] All tests passing in CI/CD pipeline
- [ ] Code review completed and approved
- [ ] Docker images built and pushed to registry
  - Backend: `ghcr.io/jfenderson/podium/podium-backend:latest`
  - Frontend: `ghcr.io/jfenderson/podium/podium-frontend:latest`
- [ ] Image SHA verified and recorded
- [ ] Security scan passed (no critical vulnerabilities)
- [ ] Performance testing completed (if significant changes)

### Monitoring & Alerting ⭐

- [ ] Application Insights is receiving telemetry
- [ ] Alert rules are active and tested
- [ ] Monitoring dashboard accessible
- [ ] PagerDuty/alerting integration tested
- [ ] On-call engineer identified and available

### Communication

- [ ] Team notified of deployment window (minimum 24 hours notice for production)
- [ ] Stakeholders informed (for major releases)
- [ ] Maintenance page ready (if downtime expected)
- [ ] Rollback plan documented and reviewed
- [ ] Post-deployment verification plan prepared

### Environment Validation ⭐

- [ ] Environment variables validated
  ```bash
  cd environments
  ./validate-env.sh production
  ```
- [ ] Secrets rotation schedule reviewed
- [ ] No expired credentials or certificates

---

## Deployment Steps

### Step 1: Enable Maintenance Mode (Optional)

If expecting significant downtime (>5 minutes):

```bash
# Display maintenance page
# Update App Service configuration or use Azure Traffic Manager
az webapp config appsettings set \
  --resource-group podium-production-rg \
  --name podium-frontend-production \
  --settings MAINTENANCE_MODE=true
```

**Estimated time:** 2 minutes

### Step 2: Deploy Infrastructure Changes

If infrastructure updates are needed:

```bash
cd infrastructure

# Validate Bicep template
az bicep build --file main.bicep

# Deploy to environment
./deploy-infrastructure.sh production podium-production-rg

# Review outputs and save connection strings
```

**Estimated time:** 10-15 minutes

**Verification:**
- [ ] All resources show as "Succeeded" in Azure Portal
- [ ] Key Vault secrets accessible by App Service
- [ ] Database connection successful

### Step 3: Run Database Migrations

Apply database schema changes:

```bash
cd database/migrations

# Set connection string from Key Vault or environment
export DATABASE_CONNECTION_STRING="Server=tcp:podium-sql-production.database.windows.net,1433;..."

# Apply migrations
./apply-migrations.sh production

# Review migration log
cat /var/log/podium-migrations/migrations-*.log
```

**Estimated time:** 5-10 minutes

**Verification:**
- [ ] All migrations applied successfully
- [ ] No error messages in migration log
- [ ] Database schema version matches expected version

**Rollback on failure:**
If migration fails, automatic rollback is triggered. Review logs and fix issues before retrying.

### Step 4: Deploy Backend Application

Deploy backend Docker image to App Service:

```bash
# Set the Docker image
az webapp config container set \
  --resource-group podium-production-rg \
  --name podium-backend-production \
  --docker-custom-image-name ghcr.io/jfenderson/podium/podium-backend:sha-abc1234 \
  --docker-registry-server-url https://ghcr.io \
  --docker-registry-server-user $GITHUB_USERNAME \
  --docker-registry-server-password $GITHUB_TOKEN

# Restart the app service
az webapp restart \
  --resource-group podium-production-rg \
  --name podium-backend-production

# Stream logs to verify startup
az webapp log tail \
  --resource-group podium-production-rg \
  --name podium-backend-production
```

**Estimated time:** 3-5 minutes

**Verification:**
- [ ] App Service shows "Running" status
- [ ] Logs show successful startup
- [ ] No error messages in application logs

### Step 5: Deploy Frontend Application

Deploy frontend assets:

```bash
# Set the Docker image
az webapp config container set \
  --resource-group podium-production-rg \
  --name podium-frontend-production \
  --docker-custom-image-name ghcr.io/jfenderson/podium/podium-frontend:sha-abc1234 \
  --docker-registry-server-url https://ghcr.io \
  --docker-registry-server-user $GITHUB_USERNAME \
  --docker-registry-server-password $GITHUB_TOKEN

# Restart the app service
az webapp restart \
  --resource-group podium-production-rg \
  --name podium-frontend-production
```

**Estimated time:** 3-5 minutes

### Step 6: Purge CDN Cache

Clear CDN cache to serve new frontend assets:

```bash
cd /path/to/podium

# Purge all CDN content
az cdn endpoint purge \
  --resource-group podium-production-rg \
  --profile-name podium-cdn-production \
  --name podium-cdn-endpoint-production \
  --content-paths "/*"

# Or use the purge script if created
# ./cdn-purge.sh production
```

**Estimated time:** 2-3 minutes

### Step 7: Run Automated Smoke Tests

Execute smoke tests to verify core functionality:

```bash
# Health check - Backend
curl https://podium-backend-production.azurewebsites.net/health
# Expected: 200 OK with {"status":"Healthy"}

# Health check - Frontend
curl https://podium-frontend-production.azurewebsites.net/health
# Expected: 200 OK

# Ready check - Backend (includes database connectivity)
curl https://podium-backend-production.azurewebsites.net/ready
# Expected: 200 OK with {"status":"Ready","checks":{"database":"Connected"}}

# API endpoint test
curl https://podium-backend-production.azurewebsites.net/api/health
# Expected: 200 OK
```

**Estimated time:** 2 minutes

**Verification:**
- [ ] All health endpoints return 200 OK
- [ ] Backend can connect to database
- [ ] Backend can access blob storage
- [ ] Frontend loads successfully

### Step 8: Disable Maintenance Mode

Remove maintenance page:

```bash
az webapp config appsettings delete \
  --resource-group podium-production-rg \
  --name podium-frontend-production \
  --setting-names MAINTENANCE_MODE
```

**Estimated time:** 1 minute

### Step 9: Monitor Application (15 minutes)

Monitor the application closely after deployment:

```bash
# View Application Insights Live Metrics
# Open Azure Portal > Application Insights > Live Metrics

# View application logs
az webapp log tail \
  --resource-group podium-production-rg \
  --name podium-backend-production

# Check error rate
az monitor metrics list \
  --resource /subscriptions/{subscription-id}/resourceGroups/podium-production-rg/providers/Microsoft.Web/sites/podium-backend-production \
  --metric "Http5xx" \
  --interval PT1M
```

**Watch for:**
- [ ] Error rate < 1%
- [ ] Response time < 2 seconds (P95)
- [ ] No exceptions in logs
- [ ] CPU and memory within normal ranges

### Step 10: Verify Critical User Flows

Manually test critical paths:

**Test Scenarios:**
1. **User Authentication**
   - [ ] Login with valid credentials
   - [ ] Logout successfully
   - [ ] Password reset flow works

2. **Student Profile**
   - [ ] Create new student profile
   - [ ] Upload video (end-to-end)
   - [ ] View student profile

3. **Search & Discovery**
   - [ ] Search for students by instrument
   - [ ] Filter results by location
   - [ ] View band program details

4. **Scholarship Management**
   - [ ] Create scholarship offer
   - [ ] View scholarship list
   - [ ] Update scholarship status

**Estimated time:** 10 minutes

---

## Rollback Procedure

If deployment fails or critical issues are discovered:

### Step 1: Assess the Situation

- Determine the severity of the issue
- Identify affected components (backend, frontend, database)
- Review error logs and metrics
- Estimate user impact

### Step 2: Decision to Rollback

**Rollback immediately if:**
- Application is completely unavailable
- Data integrity is compromised
- Critical security vulnerability introduced
- Error rate > 10%
- Unable to fix forward within 15 minutes

**Decision makers:** Engineering Lead, On-call Engineer

### Step 3: Execute Database Rollback (if needed)

If database migrations are the issue:

```bash
cd database/migrations

# Rollback to previous migration
./rollback-migration.sh production

# Verify rollback
./verify-migrations.sh production
```

**Estimated time:** 5-10 minutes

### Step 4: Revert Application Deployments

Revert to previous Docker images:

```bash
# Find previous deployment tags
az webapp config container show \
  --resource-group podium-production-rg \
  --name podium-backend-production

# Revert backend
PREVIOUS_BACKEND_IMAGE="ghcr.io/jfenderson/podium/podium-backend:sha-xyz7890"
az webapp config container set \
  --resource-group podium-production-rg \
  --name podium-backend-production \
  --docker-custom-image-name $PREVIOUS_BACKEND_IMAGE

az webapp restart \
  --resource-group podium-production-rg \
  --name podium-backend-production

# Revert frontend
PREVIOUS_FRONTEND_IMAGE="ghcr.io/jfenderson/podium/podium-frontend:sha-xyz7890"
az webapp config container set \
  --resource-group podium-production-rg \
  --name podium-frontend-production \
  --docker-custom-image-name $PREVIOUS_FRONTEND_IMAGE

az webapp restart \
  --resource-group podium-production-rg \
  --name podium-frontend-production
```

**Estimated time:** 5 minutes

### Step 5: Clear CDN Cache

```bash
az cdn endpoint purge \
  --resource-group podium-production-rg \
  --profile-name podium-cdn-production \
  --name podium-cdn-endpoint-production \
  --content-paths "/*"
```

**Estimated time:** 2 minutes

### Step 6: Verify Rollback

Run smoke tests again:

```bash
curl https://podium-backend-production.azurewebsites.net/health
curl https://podium-backend-production.azurewebsites.net/ready
curl https://podium-frontend-production.azurewebsites.net/health
```

**Verification:**
- [ ] All health checks passing
- [ ] Error rate returned to normal
- [ ] Critical user flows working
- [ ] Monitoring dashboard shows recovery

### Step 7: Document Incident

Create incident report:
- Time of deployment
- Issue description
- Root cause (if known)
- Actions taken
- Rollback completion time
- Lessons learned

### Step 8: Post-Mortem

Schedule post-mortem meeting within 48 hours to:
- Analyze root cause
- Identify process improvements
- Update deployment procedures
- Implement preventive measures

---

## Post-Deployment

### Monitor for 30 Minutes

After deployment or rollback, monitor closely:

**Application Insights Dashboard:**
- Check request rate and response times
- Monitor exception rate
- Review dependency failures
- Check availability tests

**Key Metrics to Watch:**
- Request rate: Should match historical patterns
- Response time (P95): < 2 seconds
- Error rate: < 1%
- CPU utilization: < 70%
- Memory utilization: < 85%
- Database DTU: < 80%

### Update Deployment Log

Document deployment in tracking system:

```
Deployment Log Entry:
- Date/Time: 2026-01-15 14:30 UTC
- Environment: Production
- Version: v2.5.0
- Backend Image: ghcr.io/jfenderson/podium/podium-backend:sha-abc1234
- Frontend Image: ghcr.io/jfenderson/podium/podium-frontend:sha-abc1234
- Database Migrations: Applied (20260115_AddVideoThumbnails)
- Deployed by: [Engineer Name]
- Duration: 45 minutes
- Issues: None
- Rollback: Not required
```

### Send Completion Notification

Notify team and stakeholders:

**Slack/Teams Message Template:**
```
✅ Production Deployment Completed

Environment: Production
Version: v2.5.0
Status: Success
Duration: 45 minutes
Health: All systems operational

Deployed:
- Backend: sha-abc1234
- Frontend: sha-abc1234
- Database: Migration 20260115 applied

Monitoring: All metrics normal
Next Steps: Continued monitoring for 24 hours

Dashboard: [link to Application Insights]
```

### Schedule Follow-up Review

- Review Application Insights for 24 hours
- Check for any unusual patterns or errors
- Gather user feedback
- Document any issues for next deployment

---

## Troubleshooting

### Common Issues

#### App Service Won't Start

**Symptoms:** App Service shows "Starting" but never reaches "Running"

**Troubleshooting:**
```bash
# Check logs
az webapp log tail --resource-group podium-production-rg --name podium-backend-production

# Check app settings
az webapp config appsettings list --resource-group podium-production-rg --name podium-backend-production

# Verify Key Vault access
az webapp identity show --resource-group podium-production-rg --name podium-backend-production

# Test Key Vault connection
az keyvault secret show --vault-name podium-kv-production --name jwt-secret
```

**Common Causes:**
- Missing or incorrect Key Vault references
- Managed identity not configured
- Invalid connection strings
- Missing environment variables

#### Database Connection Failures

**Symptoms:** `/ready` endpoint returns 503, logs show SQL connection errors

**Troubleshooting:**
```bash
# Verify SQL Server is accessible
az sql server show --resource-group podium-production-rg --name podium-sql-production

# Check firewall rules
az sql server firewall-rule list --resource-group podium-production-rg --server podium-sql-production

# Test connection from App Service
# Use Kudu console or SSH
```

**Common Causes:**
- SQL Server firewall blocking App Service
- Incorrect connection string
- Database not ready after migration
- Network connectivity issues

#### Frontend Not Loading

**Symptoms:** Frontend shows errors or blank page

**Troubleshooting:**
```bash
# Check CDN status
az cdn endpoint show --resource-group podium-production-rg --profile-name podium-cdn-production --name podium-cdn-endpoint-production

# Purge CDN cache again
az cdn endpoint purge --resource-group podium-production-rg --profile-name podium-cdn-production --name podium-cdn-endpoint-production --content-paths "/*"

# Check frontend logs
az webapp log tail --resource-group podium-production-rg --name podium-frontend-production

# Verify API URL configuration
az webapp config appsettings show --resource-group podium-production-rg --name podium-frontend-production
```

**Common Causes:**
- CDN cache serving old content
- Incorrect API URL in frontend configuration
- CORS configuration issues
- Frontend build errors

#### High Error Rate After Deployment

**Symptoms:** Error rate > 5% in Application Insights

**Immediate Actions:**
1. Check Application Insights for error details
2. Review application logs
3. If critical, initiate rollback
4. If non-critical, attempt quick fix

**Investigation:**
```bash
# Check recent exceptions
# Azure Portal > Application Insights > Failures

# Check specific endpoint errors
# Application Insights > Logs
# Run query:
# requests | where timestamp > ago(30m) and success == false | summarize count() by name
```

### Emergency Contacts

**On-Call Engineer:** [Primary contact]
**Backup Engineer:** [Secondary contact]
**Engineering Manager:** [Manager contact]
**Azure Support:** [Support plan details]

### Support Resources

- Azure Portal: https://portal.azure.com
- Application Insights Dashboard: [link]
- Runbook Wiki: [link]
- Team Slack Channel: #podium-ops
- Azure Status: https://status.azure.com

---

## Deployment Timeline

**Typical Production Deployment (no infrastructure changes):**
- Pre-deployment checks: 15 minutes
- Database migrations: 5-10 minutes
- Backend deployment: 5 minutes
- Frontend deployment: 5 minutes
- CDN purge: 3 minutes
- Smoke tests: 2 minutes
- Initial monitoring: 15 minutes
- **Total: 50-60 minutes**

**With Infrastructure Changes:**
- Add 15 minutes for infrastructure deployment
- **Total: 65-75 minutes**

**Plan for:** 90-minute deployment window with buffer

---

*Last Updated: 2026-01-15*
*Version: 1.0*
*Owner: DevOps Team*
