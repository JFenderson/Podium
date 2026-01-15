# Podium - Deployment & Operations Guide

This repository contains comprehensive production deployment configuration for the Podium application. This guide provides an overview and quick links to all deployment resources.

## 📚 Documentation Index

### Quick Start
- **[Production Readiness Checklist](PRODUCTION-READINESS.md)** - Complete verification before going live
- **[Deployment Runbook](DEPLOYMENT.md)** - Step-by-step deployment procedures
- **[Disaster Recovery Plan](DR-PLAN.md)** - RTO/RPO objectives and failover procedures

### Infrastructure
- **[Azure Infrastructure (Bicep)](infrastructure/main.bicep)** - Infrastructure as Code template
- **[Infrastructure Deployment Script](infrastructure/deploy-infrastructure.sh)** - Automated deployment
- **[Azure App Service Configuration](/.azure/APP-SERVICE-CONFIG.md)** - App Service setup guide

### Security
- **[Security Policy](SECURITY.md)** - Comprehensive security documentation including:
  - Secrets rotation schedule
  - Access control (RBAC)
  - Incident response plan
  - Security audit checklist
  - OWASP Top 10 compliance

### Database
- **[Apply Migrations](database/migrations/apply-migrations.sh)** - Automated migration script
- **[Rollback Migrations](database/migrations/rollback-migration.sh)** - Safe rollback procedures
- **[Verify Migrations](database/migrations/verify-migrations.sh)** - Migration validation
- **[Backup Database](database/migrations/backup-database.sh)** - Manual backup creation
- **[Restore Test](database/migrations/restore-test.sh)** - Backup restoration verification

### Configuration
- **[Environment Variables - Development](environments/.env.development.example)** - Local dev setup
- **[Environment Variables - Staging](environments/.env.staging.example)** - Staging configuration
- **[Environment Variables - Production](environments/.env.production.example)** - Production settings
- **[Validate Environment](environments/validate-env.sh)** - Environment validation script

### Monitoring & Alerts
- **[Alerting Rules](monitoring/alerting-rules.json)** - 11 Azure Monitor alert rules
- **[Dashboard Configuration](monitoring/dashboard.json)** - Azure Portal dashboard
- **[Alert Integration Guide](monitoring/ALERT-INTEGRATION.md)** - PagerDuty, Teams, Email setup

### CDN & Performance
- **[CDN Configuration](cdn-config.md)** - Azure CDN setup and optimization
- **[CDN Purge Script](cdn-purge.sh)** - Cache purging automation

### Cost Management
- **[Cost Estimation](cost-estimation.md)** - Detailed cost breakdown and projections

---

## 🚀 Quick Start Deployment

### Prerequisites
```bash
# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLI | sudo bash

# Login to Azure
az login

# Install Bicep
az bicep install
```

### Deploy Infrastructure
```bash
cd infrastructure
./deploy-infrastructure.sh production podium-production-rg
```

### Apply Database Migrations
```bash
cd database/migrations
./apply-migrations.sh production
```

### Deploy Application
See [DEPLOYMENT.md](DEPLOYMENT.md) for complete deployment steps.

---

## 📋 Pre-Deployment Checklist

Before deploying to production, complete all items in:
- ✅ [PRODUCTION-READINESS.md](PRODUCTION-READINESS.md)

**Key sections:**
1. Infrastructure & Configuration (17 items)
2. Database & Data (5 items)
3. Monitoring & Alerting (5 items)
4. Security (9 items)
5. Testing & Quality (5 items)
6. Documentation & Training (6 items)
7. Support & Operations (5 items)

---

## 🏗️ Architecture Overview

### Azure Resources Provisioned

| Resource | SKU/Tier | Purpose |
|----------|----------|---------|
| **App Service Plan** | P1v2 Premium | Hosts backend and frontend |
| **Backend App Service** | .NET 8.0 Linux | API and business logic |
| **Frontend App Service** | Node 20 LTS | Angular application |
| **Azure SQL Database** | S2 Standard | Relational data storage |
| **Blob Storage** | Standard GRS | Video and file storage |
| **Application Insights** | Pay-as-you-go | Monitoring and telemetry |
| **Key Vault** | Standard | Secrets management |
| **CDN** | Standard Microsoft | Content delivery |
| **Log Analytics** | Pay-as-you-go | Centralized logging |

### Estimated Monthly Cost
- **Base:** $350-400/month
- **10x scale:** $800-1,000/month
- **100x scale:** $2,500-3,000/month

See [cost-estimation.md](cost-estimation.md) for detailed breakdown.

---

## 🔐 Security Features

- ✅ All secrets stored in Azure Key Vault
- ✅ Managed identities for service-to-service auth
- ✅ HTTPS enforced with HSTS
- ✅ TLS 1.2 minimum
- ✅ Security headers configured (CSP, X-Frame-Options, etc.)
- ✅ SQL injection protection (parameterized queries)
- ✅ OWASP Top 10 compliance verified
- ✅ Rate limiting on authentication endpoints
- ✅ Automated secrets rotation schedule

---

## 📊 Monitoring & Alerts

### Configured Alerts

1. **Performance:**
   - CPU usage > 80% for 5 minutes
   - Memory usage > 85% for 5 minutes
   - Response time > 2 seconds (P95)

2. **Errors:**
   - HTTP 5xx rate > 5% for 5 minutes
   - Failed login attempts > 20 per minute
   - Unhandled exceptions > 10 per minute

3. **Resources:**
   - Database DTU > 80% for 10 minutes
   - Storage availability < 99.9%
   - Instance count > 8 (cost alert)

### Dashboard Metrics
- Request rate and latency
- Error rate
- CPU and memory utilization
- Database performance
- Active users

---

## 🔄 Database Management

### Migration Scripts

All scripts support staging and production environments:

```bash
# Apply migrations
./database/migrations/apply-migrations.sh production

# Rollback last migration
./database/migrations/rollback-migration.sh production

# Verify migrations on test database
./database/migrations/verify-migrations.sh production

# Create manual backup
./database/migrations/backup-database.sh production monthly-backup

# Test backup restoration
./database/migrations/restore-test.sh production
```

### Backup Strategy
- **Automated:** Every 5 minutes (point-in-time restore)
- **Retention:** 35 days short-term, 7 years long-term
- **Redundancy:** Geo-redundant (primary + paired region)
- **Testing:** Monthly restore verification

---

## 🛡️ Disaster Recovery

### Recovery Objectives
- **RTO (Recovery Time Objective):** 4 hours
- **RPO (Recovery Point Objective):** 1 hour

### Disaster Scenarios Covered
1. Single service failure
2. Regional outage
3. Database corruption
4. Security breach / ransomware
5. Accidental resource deletion

### Failover Procedures
Complete step-by-step procedures in [DR-PLAN.md](DR-PLAN.md)

### Testing Schedule
- **Monthly:** Backup restore test
- **Quarterly:** Full DR drill
- **Annually:** Comprehensive exercise with failback

---

## 💰 Cost Optimization

### Immediate Savings (30-50% reduction)
1. **Azure Reserved Instances** - Save $50-100/month
2. **Right-size resources** - Save $30-50/month
3. **Optimize storage** - Save $10-20/month
4. **Review log retention** - Save $5-10/month

**Total potential savings: $115-210/month**

### Resource Tagging
All resources tagged with:
- Environment (Production/Staging/Development)
- Application (Podium)
- Owner (DevOps Team)
- CostCenter (Billing code)
- Project (Podium)

### Budget Alerts
- **80% ($400):** Warning email
- **90% ($450):** Escalation
- **100% ($500):** Emergency review

---

## 📞 Support & Contact

### Emergency Contacts
- **On-Call Engineer:** [Configure in PagerDuty]
- **DevOps Team Lead:** [Contact info]
- **Engineering Manager:** [Contact info]

### Support Channels
- **Monitoring:** Application Insights dashboard
- **Alerts:** PagerDuty, Microsoft Teams, Email
- **Documentation:** This repository
- **Azure Support:** [Support plan tier]

---

## 🔧 Tools & Scripts

### Infrastructure
```bash
infrastructure/deploy-infrastructure.sh <environment> [resource-group]
```

### Database
```bash
database/migrations/apply-migrations.sh <environment> [connection-string]
database/migrations/rollback-migration.sh <environment> [--auto-confirm]
database/migrations/verify-migrations.sh <environment>
database/migrations/backup-database.sh <environment> [label]
database/migrations/restore-test.sh <environment> [backup-name]
```

### Environment
```bash
environments/validate-env.sh [environment]
```

### CDN
```bash
./cdn-purge.sh <environment> [paths...]
```

---

## 📖 Additional Resources

### Microsoft Documentation
- [Azure App Service](https://docs.microsoft.com/azure/app-service/)
- [Azure SQL Database](https://docs.microsoft.com/azure/azure-sql/)
- [Azure Bicep](https://docs.microsoft.com/azure/azure-resource-manager/bicep/)
- [Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)

### Best Practices
- [Azure Well-Architected Framework](https://docs.microsoft.com/azure/architecture/framework/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Twelve-Factor App](https://12factor.net/)

---

## 📝 Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-15 | Initial production deployment configuration |

---

## ✅ Status

**Production Ready:** All components implemented and documented.

**Next Steps:**
1. Complete [PRODUCTION-READINESS.md](PRODUCTION-READINESS.md) checklist
2. Schedule deployment window
3. Execute [DEPLOYMENT.md](DEPLOYMENT.md) runbook
4. Monitor using dashboard and alerts

---

*Last Updated: 2026-01-15*
*Maintained by: DevOps Team*
