# Disaster Recovery Plan

This document outlines the disaster recovery (DR) procedures for the Podium application.

## Table of Contents

- [Overview](#overview)
- [Recovery Objectives](#recovery-objectives)
- [Backup Locations](#backup-locations)
- [Disaster Scenarios](#disaster-scenarios)
- [Failover Procedures](#failover-procedures)
- [Communication Plan](#communication-plan)
- [DR Testing Schedule](#dr-testing-schedule)

---

## Overview

**Purpose:** Ensure business continuity in the event of a disaster affecting the Podium application.

**Scope:** Production environment infrastructure, data, and application services.

**Owner:** DevOps Team

**Last Tested:** [To be filled after first DR test]

**Next Test:** [Schedule quarterly]

---

## Recovery Objectives

### RTO (Recovery Time Objective)

**Target: 4 hours**

Maximum acceptable downtime from disaster declaration to service restoration.

**Breakdown by Component:**
| Component | RTO | Notes |
|-----------|-----|-------|
| Application (read-only) | 1 hour | Failover to secondary region |
| Application (full functionality) | 2 hours | Database restored |
| Complete system | 4 hours | All services operational |

### RPO (Recovery Point Objective)

**Target: 1 hour**

Maximum acceptable data loss measured in time.

**Data Protection:**
- **Database:** Point-in-time restore available (5-minute granularity)
- **Blob Storage:** Geo-redundant storage (GRS) with automatic replication
- **Application Code:** GitHub repository (no data loss)
- **Infrastructure:** Bicep templates in source control (no data loss)

**Actual RPO by Component:**
| Component | RPO | Method |
|-----------|-----|--------|
| Database | 5 minutes | SQL automated backups + point-in-time restore |
| File Storage | 0 (near real-time) | GRS automatic replication |
| Application Configuration | 0 | Stored in Key Vault (geo-replicated) |
| Infrastructure | 0 | Infrastructure as Code (Bicep) |

---

## Backup Locations

### Primary Backup Storage

**Azure SQL Database Automated Backups:**
- Location: Azure-managed (East US and paired region)
- Type: Full, differential, and transaction log backups
- Retention: 35 days short-term, 7 years long-term
- Redundancy: Geo-redundant (automatically replicated to paired region)
- Access: Azure Portal > SQL Database > Restore

**Azure Blob Storage:**
- Type: Geo-redundant storage (GRS)
- Primary Region: East US
- Secondary Region: West US (read-access enabled)
- Replication: Automatic and continuous
- Access: Failover initiated via Azure Portal or CLI

### Secondary Backup Storage

**Manual Database Backups:**
- Location: Separate storage account (`podiumbackups{environment}`)
- Type: Full database exports (.bacpac)
- Schedule: Weekly
- Retention: 90 days
- Redundancy: LRS (within single region for cost)

**Infrastructure Backups:**
- Location: GitHub repository
- Type: Bicep templates, scripts, documentation
- Version Control: Git
- Access: All team members with repository access

### Application Code

**Repository:** `https://github.com/JFenderson/Podium`
- Branches: Protected with required reviews
- Docker Images: GitHub Container Registry (GHCR)
- Tags: Semantic versioning (e.g., v2.5.0)
- Retention: Indefinite for tagged releases

---

## Disaster Scenarios

### Scenario 1: Single Service Failure

**Example:** App Service becomes unresponsive

**Impact:** High (application unavailable)

**RTO:** 30 minutes

**Recovery Steps:**
1. Verify service status in Azure Portal
2. Review Application Insights for errors
3. Attempt service restart
4. If restart fails, deploy to secondary instance
5. Update DNS if needed

### Scenario 2: Regional Outage

**Example:** Azure East US region completely unavailable

**Impact:** Critical (all services unavailable)

**RTO:** 2-4 hours

**Recovery Steps:** See [Failover Procedures](#failover-procedures)

### Scenario 3: Database Corruption

**Example:** Data corruption or accidental deletion

**Impact:** Critical (data integrity compromised)

**RTO:** 2-3 hours

**Recovery Steps:**
1. Declare incident
2. Isolate affected database
3. Assess extent of corruption
4. Identify recovery point (last known good state)
5. Restore from point-in-time backup
6. Validate data integrity
7. Reconnect application

### Scenario 4: Ransomware/Security Breach

**Example:** Unauthorized access and data encryption

**Impact:** Critical (data security compromised)

**RTO:** 4-6 hours

**Recovery Steps:**
1. Immediately revoke all access
2. Isolate compromised resources
3. Engage security team and law enforcement
4. Assess breach scope
5. Restore from clean backups (verified pre-breach)
6. Rotate all secrets and credentials
7. Conduct security audit before re-enabling

### Scenario 5: Accidental Resource Deletion

**Example:** Critical Azure resource deleted

**Impact:** High to Critical

**RTO:** 1-2 hours

**Recovery Steps:**
1. Check Azure Activity Log for deletion details
2. Attempt resource recovery (some resources can be undeleted)
3. If unrecoverable, redeploy from Bicep templates
4. Restore data from backups
5. Update DNS and configurations
6. Validate functionality

---

## Failover Procedures

### Prerequisites

- [ ] Azure CLI installed and configured
- [ ] Access to Azure Portal (Contributor role)
- [ ] Access to GitHub repository
- [ ] DNS management access
- [ ] Incident commander assigned

### Phase 1: Assessment (0-15 minutes)

**Objective:** Understand the disaster scope and declare incident

**Actions:**
1. Verify primary region is unavailable:
   ```bash
   az resource list --location eastus --query "[?resourceGroup=='podium-production-rg']" --output table
   ```

2. Check Azure Status:
   - https://status.azure.com
   - Look for outages in East US

3. Assess impact:
   - Are users completely blocked?
   - Is data at risk?
   - What services are affected?

4. **Decision Point:** Proceed with failover?
   - If YES: Declare disaster and continue
   - If NO: Attempt standard troubleshooting

5. Notify stakeholders (see Communication Plan)

### Phase 2: Initiate Database Failover (15-45 minutes)

**Objective:** Restore database in secondary region

**Option A: Geo-Restore (if geo-replication not configured)**

```bash
# Restore database to West US from geo-redundant backup
az sql db restore \
  --resource-group podium-dr-rg \
  --server podium-sql-dr \
  --name PodiumDb \
  --dest-name PodiumDb \
  --source-database /subscriptions/{subscription}/resourceGroups/podium-production-rg/providers/Microsoft.Sql/servers/podium-sql-production/databases/PodiumDb \
  --time "2026-01-15T12:00:00Z"  # Last known good time
```

Wait for restore completion (typically 15-30 minutes).

**Option B: Failover Group (if configured - recommended for future)**

```bash
# Initiate failover to secondary
az sql failover-group set-primary \
  --resource-group podium-production-rg \
  --server podium-sql-production \
  --name podium-fog \
  --force-fail-over
```

This is faster (2-5 minutes) but requires pre-configuration.

### Phase 3: Deploy Application to Secondary Region (30-60 minutes)

**Objective:** Bring up application in West US

1. **Create resource group (if not exists):**
   ```bash
   az group create \
     --name podium-dr-rg \
     --location westus
   ```

2. **Deploy infrastructure from Bicep:**
   ```bash
   cd infrastructure
   ./deploy-infrastructure.sh dr podium-dr-rg
   ```

3. **Update configuration:**
   - Point to restored database
   - Update Key Vault secrets if needed
   - Configure App Service settings

4. **Deploy application:**
   ```bash
   # Backend
   az webapp config container set \
     --resource-group podium-dr-rg \
     --name podium-backend-dr \
     --docker-custom-image-name ghcr.io/jfenderson/podium/podium-backend:latest

   # Frontend
   az webapp config container set \
     --resource-group podium-dr-rg \
     --name podium-frontend-dr \
     --docker-custom-image-name ghcr.io/jfenderson/podium/podium-frontend:latest
   ```

### Phase 4: Storage Failover (30-45 minutes)

**Objective:** Access data in secondary region

**Initiate storage failover:**
```bash
az storage account failover \
  --resource-group podium-production-rg \
  --name podiumstorageproduction
```

**Note:** This makes West US the primary. Takes 30-45 minutes. During failover, storage is read-only.

### Phase 5: Update DNS (5-10 minutes)

**Objective:** Route traffic to DR environment

1. Update DNS records to point to West US endpoints:
   ```
   www.podiumapp.com → podium-frontend-dr.azurewebsites.net (or CDN)
   api.podiumapp.com → podium-backend-dr.azurewebsites.net
   ```

2. Reduce TTL to 60 seconds for faster updates

3. Wait for DNS propagation (5-15 minutes)

### Phase 6: Verify Functionality (15-30 minutes)

**Objective:** Ensure DR environment is operational

**Smoke Tests:**
```bash
# Health checks
curl https://podium-backend-dr.azurewebsites.net/health
curl https://podium-backend-dr.azurewebsites.net/ready

# Test critical flows
# - Login
# - View student profile
# - Search functionality
# - Video playback (if storage failover complete)
```

**Validation Checklist:**
- [ ] Application loads
- [ ] Users can authenticate
- [ ] Database queries work
- [ ] File uploads/downloads work
- [ ] No critical errors in Application Insights
- [ ] Performance acceptable

### Phase 7: Monitor Closely (Ongoing)

**Objective:** Watch for issues in DR environment

**Monitor:**
- Application Insights for errors
- Azure Monitor for resource health
- User reports and support tickets
- Database performance metrics

**Be Ready To:**
- Scale resources if needed
- Fix configuration issues
- Roll back if critical issues found

---

## Communication Plan

### Stakeholder Notification

#### Internal Stakeholders

| Role | Contact | Notification Method | Timing |
|------|---------|-------------------|--------|
| **Incident Commander** | [Name/Contact] | Phone | Immediate |
| **Engineering Manager** | [Name/Contact] | Phone + Slack | Within 5 min |
| **DevOps Team** | [Team Channel] | Slack | Within 5 min |
| **Customer Support** | [Team Email] | Email + Slack | Within 15 min |
| **Executive Team** | [Distribution List] | Email | Within 30 min |
| **All Staff** | [Company-wide] | Email | Within 1 hour |

#### External Communication

| Audience | Channel | Message Template | Timing |
|----------|---------|-----------------|--------|
| **All Users** | Status page | "We are experiencing technical difficulties..." | Within 15 min |
| **Premium Users** | Email | Detailed incident notification | Within 30 min |
| **Partners** | Email | Impact assessment | Within 1 hour |
| **Public** | Social media | Brief status update | Within 1 hour |

### Message Templates

#### Initial Notification (Internal)

```
INCIDENT ALERT

Severity: [P0/P1/P2]
Type: [Disaster Recovery / Regional Outage / etc.]
Impact: [Service completely unavailable / Degraded performance]
Affected Users: [All / Subset]
ETR: [4 hours / TBD]

Current Status:
- [Brief description]

Actions Taken:
- [What we're doing]

Incident Commander: [Name]
War Room: [Slack channel / Teams meeting]

Updates: Every 30 minutes or as status changes
```

#### User Notification (External)

```
Subject: Service Disruption - Podium Application

Dear Podium Users,

We are currently experiencing technical difficulties with the Podium application. Our team is working to restore service as quickly as possible.

Impact:
- Application may be unavailable or slow
- Some features may not work as expected

Current Status: [Investigating / Restoring / Recovering]

Estimated Resolution Time: [4 hours / As soon as possible]

We apologize for the inconvenience and will provide updates every hour until resolved.

For urgent inquiries, please contact: support@podiumapp.com

Thank you for your patience.

The Podium Team
```

### Status Page Updates

**Update Frequency:**
- Every 30 minutes during incident
- When major milestone reached
- When service restored

**Example Updates:**
```
[12:00 PM] Investigating: We are investigating reports of application unavailability.

[12:30 PM] Identified: We have identified a regional outage in our primary datacenter and are initiating disaster recovery procedures.

[1:00 PM] Monitoring: We have successfully failed over to our backup datacenter and are monitoring system stability.

[2:00 PM] Resolved: All systems have been restored and are operating normally. We will continue to monitor closely.

[3:00 PM] Postmortem: A detailed incident report will be published within 48 hours.
```

---

## DR Testing Schedule

### Monthly

- [ ] Backup restore test (development environment)
- [ ] Review and update DR documentation
- [ ] Verify backup retention policies

**Owner:** DevOps Engineer  
**Duration:** 2 hours

### Quarterly

- [ ] **Full DR Drill** (simulated disaster)
- [ ] Test database failover to secondary region
- [ ] Test application deployment in DR region
- [ ] Verify DNS update procedures
- [ ] Test communication plan
- [ ] Document lessons learned
- [ ] Update DR procedures based on findings

**Owner:** DevOps Team Lead  
**Duration:** 4-6 hours  
**Participants:** All DevOps team, Engineering Manager

### Annually

- [ ] **Comprehensive DR Exercise** (full failover)
- [ ] Execute complete failover during maintenance window
- [ ] Operate from DR region for 24 hours
- [ ] Test failback procedures
- [ ] Involve all stakeholders
- [ ] Conduct post-exercise review
- [ ] Update RTO/RPO based on actual performance

**Owner:** Engineering Manager  
**Duration:** 2 days (including prep and review)  
**Participants:** Full engineering team, management

### DR Drill Checklist

**Pre-Drill:**
- [ ] Schedule maintenance window
- [ ] Notify team members
- [ ] Prepare test scenarios
- [ ] Review current documentation
- [ ] Verify backup availability
- [ ] Set up monitoring

**During Drill:**
- [ ] Declare simulated incident
- [ ] Execute failover procedures
- [ ] Document timing of each step
- [ ] Note any issues or blockers
- [ ] Test communication plan
- [ ] Verify application functionality

**Post-Drill:**
- [ ] Document actual RTO/RPO achieved
- [ ] Identify gaps in procedures
- [ ] Update documentation
- [ ] Create improvement action items
- [ ] Share findings with team
- [ ] Schedule follow-up for action items

### Post-Incident Review

After any real disaster or DR activation:

1. **Schedule post-mortem within 48 hours**
2. **Participants:** All involved parties
3. **Agenda:**
   - Timeline of events
   - What went well
   - What could be improved
   - Action items with owners and deadlines
4. **Deliverable:** Written post-mortem report
5. **Follow-up:** Track action items to completion

---

## Recovery Validation

### Validation Checklist

After failover, verify:

**Application:**
- [ ] All web pages load correctly
- [ ] Authentication working
- [ ] API endpoints responding
- [ ] Real-time features operational
- [ ] No JavaScript errors

**Data:**
- [ ] Database accessible
- [ ] Data integrity verified (spot checks)
- [ ] No data loss beyond RPO
- [ ] Recent transactions visible

**Storage:**
- [ ] File uploads working
- [ ] File downloads working
- [ ] Videos playable
- [ ] Thumbnails loading

**Integrations:**
- [ ] Email delivery working
- [ ] External API calls successful
- [ ] Monitoring and logging active

**Performance:**
- [ ] Response times acceptable (< 2s)
- [ ] No errors in Application Insights
- [ ] Resource utilization normal

---

## Failback Procedures

When primary region is restored:

1. **Verify primary region health**
2. **Test primary region with canary deployment**
3. **Sync any data from DR to primary**
4. **Schedule maintenance window**
5. **Update DNS back to primary**
6. **Monitor closely for 24 hours**
7. **Decommission DR resources (optional)**

**Decision criteria for failback:**
- Primary region stable for 24+ hours
- Azure confirms issue resolved
- Performance metrics normal
- No active alerts

---

*Last Updated: 2026-01-15*
*Version: 1.0*
*Next Review: After first DR drill*
