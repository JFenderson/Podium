# Production Readiness Checklist

This comprehensive checklist ensures the Podium application is ready for production deployment.

**Purpose:** Verify all systems, processes, and documentation are in place before going live.

**Owner:** Engineering Team

**Approval Required:** Engineering Manager, DevOps Lead, Security Team

---

## Table of Contents

- [Infrastructure & Configuration](#infrastructure--configuration)
- [Database & Data](#database--data)
- [Monitoring & Alerting](#monitoring--alerting)
- [Security](#security)
- [Testing & Quality](#testing--quality)
- [Documentation & Training](#documentation--training)
- [Support & Operations](#support--operations)
- [Final Sign-Off](#final-sign-off)

---

## Infrastructure & Configuration

### Azure Resources

- [ ] **All Azure resources provisioned via Bicep**
  - Template: `infrastructure/main.bicep` validated
  - Deployed successfully to production resource group
  - All outputs verified (URLs, connection strings, IDs)
  - No manual Azure resources exist

- [ ] **Resource naming conventions followed**
  - Format: `podium-{resource-type}-{environment}-{uniqueid}`
  - Examples verified in Azure Portal
  - Documentation matches actual names

- [ ] **Resource tags applied**
  - Environment, Application, Owner, CostCenter, Project
  - Tags visible in Azure Portal
  - Cost allocation working

- [ ] **App Service Plan properly configured**
  - SKU: P1v2 (production grade)
  - Linux operating system
  - Region: East US (or chosen region)
  - Always On: Enabled

- [ ] **Backend App Service configured**
  - Runtime: .NET 8.0
  - System-assigned managed identity: Enabled
  - HTTPS only: Enabled
  - HTTP 2.0: Enabled
  - Minimum TLS: 1.2
  - Health check path: `/health` configured

- [ ] **Frontend App Service configured**
  - Runtime: Node 20 LTS
  - System-assigned managed identity: Enabled
  - HTTPS only: Enabled
  - HTTP 2.0: Enabled
  - Health check path: `/health` configured

- [ ] **Azure SQL Database configured**
  - SKU: S2 Standard (or higher)
  - Geo-redundant backup: Enabled
  - Point-in-time restore: 35 days retention
  - Long-term retention: 7 years configured
  - Firewall: Azure services allowed

- [ ] **Blob Storage configured**
  - Redundancy: GRS (geo-redundant)
  - HTTPS only: Enforced
  - Public blob access: Disabled
  - Containers created: videos, thumbnails, assets

- [ ] **Key Vault configured**
  - Soft delete: Enabled (90 days)
  - Access policies for App Services configured
  - All secrets stored (no hardcoded values)

- [ ] **Application Insights configured**
  - Connected to Log Analytics workspace
  - Retention: 90 days
  - Sampling: Configured appropriately
  - Connection string in App Service settings

- [ ] **CDN configured**
  - Origin: Frontend App Service
  - Caching rules: Configured per specifications
  - Compression: Enabled
  - HTTPS only: Enabled
  - Custom domain (if applicable): Configured

- [ ] **Log Analytics workspace configured**
  - Retention: 90 days
  - Diagnostic settings enabled for all resources
  - Connected to Application Insights

### Secrets Management

- [ ] **All secrets in Key Vault (no hardcoded secrets)**
  - JWT secret
  - SQL connection string
  - Storage connection string
  - SendGrid API key
  - All environment-specific secrets

- [ ] **Key Vault references in App Service work**
  - Format: `@Microsoft.KeyVault(SecretUri=...)`
  - Test: Secrets loaded correctly
  - No "Key Vault reference" errors in logs

- [ ] **Managed identities configured**
  - Backend App Service identity created
  - Frontend App Service identity created
  - Key Vault access policies granted

### Auto-Scaling

- [ ] **Auto-scale rules configured and tested**
  - Scale out: CPU > 70% for 5 minutes
  - Scale in: CPU < 40% for 10 minutes
  - Min instances: 1, Max instances: 8
  - Tested under load

### SSL Certificates

- [ ] **SSL certificates valid and auto-renewal enabled**
  - Backend: App Service managed certificate
  - Frontend: App Service managed certificate
  - CDN: CDN managed certificate (if custom domain)
  - Expiry: Not within 30 days
  - Auto-renewal: Verified

### DNS Configuration

- [ ] **DNS configured with proper TTLs**
  - Production domain: Configured
  - TTL: 3600 seconds (1 hour) or as required
  - CNAME records: Verified
  - Propagation: Complete

### Network Configuration

- [ ] **CDN configured with caching rules**
  - Static assets: 7 days cache
  - HTML: 1 hour cache
  - API: No cache
  - Compression: Enabled
  - Query string behavior: Configured

---

## Database & Data

### Database Deployment

- [ ] **Database backups configured (automated + geo-redundant)**
  - Automated backups: Enabled
  - Backup schedule: Every 5 minutes (automated)
  - Retention: Short-term 35 days, long-term 7 years
  - Geo-redundancy: Enabled
  - Last backup: Verified within 24 hours

- [ ] **Database migrations tested on staging**
  - All migrations applied successfully
  - No data loss verified
  - Schema integrity validated
  - Rollback tested successfully

- [ ] **Point-in-time restore verified**
  - Test restore completed within last 7 days
  - Restore to test database successful
  - Data integrity confirmed
  - Documented in test log

### Data Quality

- [ ] **Connection pooling configured**
  - Max pool size: Set appropriately (100)
  - Min pool size: Set appropriately (10)
  - Connection timeout: 30 seconds
  - Verified in connection string

- [ ] **Database performance baselines established**
  - Average query time: < 50ms
  - P95 query time: < 200ms
  - DTU usage baseline: 20-30%
  - Slow query alerts: Configured

---

## Monitoring & Alerting

### Application Insights

- [ ] **Application Insights configured and sending telemetry**
  - Connection string in App Service
  - Telemetry visible in Azure Portal
  - Custom metrics tracked (if any)
  - Performance data flowing

- [ ] **Alert rules active and tested**
  - High CPU (>80%): Tested
  - High memory (>85%): Tested
  - High response time (>2s): Tested
  - High error rate (>5%): Tested
  - Failed login attempts (>20/min): Tested
  - Database connection failures (>5/min): Tested

- [ ] **Dashboard created and shared with team**
  - Azure Portal dashboard created
  - Metrics: Request rate, response time, errors, availability
  - Shared with: DevOps team, engineering team
  - URL documented and accessible

- [ ] **Log Analytics workspace configured**
  - Diagnostic settings enabled
  - Logs from all resources flowing
  - Retention: 90 days
  - Query access: Granted to team

### Health Checks

- [ ] **Health check endpoints implemented (/health)**
  - Backend `/health`: Returns 200 OK
  - Backend `/ready`: Returns 200 OK (includes DB check)
  - Frontend `/health`: Returns 200 OK
  - App Service health check: Configured

### Alerting

- [ ] **Alert integration configured**
  - Action group created
  - Email recipients: Configured
  - PagerDuty/Teams: Integrated (if applicable)
  - Test alert: Sent and received

---

## Security

### Security Scanning

- [ ] **Security scan passed (no critical vulnerabilities)**
  - CodeQL scan: Passed
  - Container scan: Passed
  - Dependency scan: Passed
  - No critical or high severity issues

- [ ] **OWASP Top 10 compliance verified**
  - A01: Broken Access Control ✓
  - A02: Cryptographic Failures ✓
  - A03: Injection ✓
  - A04: Insecure Design ✓
  - A05: Security Misconfiguration ✓
  - A06: Vulnerable Components ✓
  - A07: Authentication Failures ✓
  - A08: Software/Data Integrity ✓
  - A09: Logging/Monitoring ✓
  - A10: SSRF ✓

- [ ] **Penetration testing completed**
  - Test date: [Date]
  - Tester: [Company/Individual]
  - Report: Reviewed
  - Critical findings: Resolved
  - Medium findings: Addressed or accepted

### Security Configuration

- [ ] **DDoS protection enabled**
  - Azure DDoS Protection: Configured (Standard or Basic)
  - Rate limiting: Enabled
  - WAF rules: Configured (if using App Gateway/Front Door)

- [ ] **WAF rules configured (if applicable)**
  - OWASP Core Rule Set: Enabled
  - Custom rules: Configured
  - Prevention mode: Enabled
  - Tested: Blocking malicious requests

- [ ] **Authentication and authorization tested**
  - Login flow: Working
  - JWT tokens: Validated and expire correctly
  - Role-based access: Enforced
  - Unauthorized access: Blocked

### Security Headers

- [ ] **Security headers validated (CSP, HSTS, etc.)**
  - HSTS: Enabled (production)
  - CSP: Configured
  - X-Frame-Options: DENY
  - X-Content-Type-Options: nosniff
  - Test: https://securityheaders.com scan passed

---

## Testing & Quality

### Functional Testing

- [ ] **Load testing passed (expected peak traffic + 50%)**
  - Tool: k6, JMeter, or Azure Load Testing
  - Users simulated: 1,500 concurrent (if 1,000 expected)
  - Duration: 1 hour
  - Results: All scenarios passed
  - Errors: < 1%
  - Response time P95: < 2 seconds

- [ ] **Smoke tests automated and passing**
  - Health endpoints: ✓
  - Login flow: ✓
  - Student profile creation: ✓
  - Video upload: ✓
  - Search: ✓
  - All tests: Passing in CI/CD

- [ ] **End-to-end tests passing**
  - Critical user journeys tested
  - All tests passing in CI/CD
  - Test coverage: >70%
  - No flaky tests

- [ ] **Performance benchmarks met**
  - API response time: <200ms (average)
  - Page load time: <2 seconds
  - Database queries: <50ms (average)
  - Video upload: Functional

### Browser Compatibility

- [ ] **Browser compatibility verified**
  - Chrome (latest): ✓
  - Firefox (latest): ✓
  - Safari (latest): ✓
  - Edge (latest): ✓
  - Mobile browsers: ✓

---

## Documentation & Training

### Documentation

- [ ] **Deployment runbook complete (DEPLOYMENT.md)**
  - Pre-deployment checklist
  - Deployment steps
  - Rollback procedures
  - Post-deployment tasks
  - Troubleshooting guide
  - Reviewed and approved

- [ ] **Disaster recovery plan documented (DR-PLAN.md)**
  - RTO/RPO defined
  - Failover procedures
  - Communication plan
  - DR testing schedule
  - Reviewed and approved

- [ ] **Security documentation complete (SECURITY.md)**
  - Secrets rotation schedule
  - Access control (RBAC)
  - Incident response plan
  - Security audit checklist
  - OWASP compliance
  - Reviewed and approved

- [ ] **Infrastructure documentation (main.bicep, README)**
  - All resources documented
  - Configuration parameters explained
  - Deployment instructions clear
  - Reviewed by team

- [ ] **Environment configuration documented**
  - `.env.production.example` complete
  - All variables explained
  - Where to obtain values documented
  - Reviewed and validated

### Training

- [ ] **Team trained on deployment procedures**
  - Runbook reviewed with team
  - Dry-run completed
  - Questions answered
  - Team feels confident

- [ ] **Runbook dry-run completed**
  - Simulated deployment in staging
  - All steps executed successfully
  - Timing verified
  - Issues identified and resolved

### Knowledge Transfer

- [ ] **On-call rotation established**
  - Schedule created
  - Responsibilities defined
  - Contact information updated
  - PagerDuty/on-call tool configured

---

## Support & Operations

### Support Plan

- [ ] **Support plan established (Azure Support tier)**
  - Tier: Developer, Standard, or Professional Direct
  - Contact: Support portal access verified
  - SLA: Understood and documented
  - Escalation path: Defined

- [ ] **Incident response procedures documented**
  - Severity definitions
  - Response times
  - Escalation criteria
  - Communication templates
  - Reviewed with team

- [ ] **Monitoring dashboard accessible to ops team**
  - Azure Portal dashboard: Shared
  - Application Insights: Access granted
  - Alerts: Configured to notify ops
  - Documentation: Available

### Operational Readiness

- [ ] **Rollback procedures tested**
  - Database rollback: Tested on staging
  - Application rollback: Tested on staging
  - CDN cache purge: Tested
  - DNS update: Documented

- [ ] **Maintenance window scheduled and communicated**
  - Date/time: Scheduled
  - Duration: Estimated (90 minutes)
  - Stakeholders: Notified
  - Users: Notified (if applicable)

---

## Final Sign-Off

### Pre-Deployment Sign-Off

**Signature Required Before Production Deployment**

| Role | Name | Signature | Date |
|------|------|-----------|------|
| **Engineering Lead** | | | |
| **DevOps Lead** | | | |
| **Security Lead** | | | |
| **Product Owner** | | | |
| **Engineering Manager** | | | |

### Deployment Authorization

**I authorize the production deployment of Podium application version [VERSION]**

**Authorized By:** _________________________  
**Title:** _________________________  
**Date:** _________________________  
**Time:** _________________________

### Post-Deployment Sign-Off

**Completed After Successful Deployment**

| Task | Completed | Verified By | Date/Time |
|------|-----------|-------------|-----------|
| Deployment executed successfully | [ ] | | |
| All smoke tests passed | [ ] | | |
| Health checks passing | [ ] | | |
| No critical errors in logs | [ ] | | |
| Monitoring dashboard shows healthy | [ ] | | |
| Users can access application | [ ] | | |

**Deployment Status:** [ ] Success  [ ] Failed  [ ] Rolled Back

**Notes:**
_________________________________________
_________________________________________
_________________________________________

**Signed:** _________________________  
**Date:** _________________________

---

## Appendix: Quick Reference

### Critical URLs

- Production Frontend: https://www.podiumapp.com
- Production Backend: https://api.podiumapp.com
- Azure Portal: https://portal.azure.com
- Application Insights: [Link to dashboard]
- Status Page: [Link if applicable]

### Emergency Contacts

- On-Call Engineer: [Phone/Email]
- DevOps Team Lead: [Phone/Email]
- Engineering Manager: [Phone/Email]
- Azure Support: [Support plan details]

### Key Commands

```bash
# Health check
curl https://api.podiumapp.com/health

# View logs
az webapp log tail --resource-group podium-production-rg --name podium-backend-production

# Purge CDN
./cdn-purge.sh production

# Rollback
# See DEPLOYMENT.md Rollback Procedure section
```

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-15  
**Next Review:** After production deployment  
**Owner:** DevOps Team
