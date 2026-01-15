# Security Policy

## Reporting Security Vulnerabilities

We take the security of Podium seriously. If you discover a security vulnerability, please follow these steps:

### How to Report

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report them via email to: **[security@podiumapp.com]** *(replace with actual security email)*

Include the following information:
- Description of the vulnerability
- Steps to reproduce the issue
- Potential impact
- Suggested fix (if any)

You should receive a response within 48 hours. If the issue is confirmed, we will:
- Work on a fix and release a security patch
- Credit you for the discovery (unless you prefer to remain anonymous)
- Keep you informed of the progress

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| Latest  | :white_check_mark: |
| < Latest| :x:                |

We only support the latest version with security updates. Please ensure you're running the most recent version.

## Security Best Practices

### Authentication & Authorization

Podium uses **JWT (JSON Web Tokens)** for authentication:
- Tokens expire after a configurable period (default: 60 minutes)
- Tokens must be transmitted over HTTPS only in production
- Refresh tokens are used for extended sessions
- Role-based access control (RBAC) is implemented for authorization

**User Roles:**
- `Student`: Access to student-specific features
- `Guardian`: Access to manage student accounts they're linked to
- `BandStaff`: Access to recruitment and event management
- `Director`: Full administrative access

### Secrets Management

**Never commit secrets to version control.**

All sensitive configuration must be stored in environment variables:
- Database connection strings
- JWT secrets
- API keys (SendGrid, AWS, Azure, etc.)
- Third-party service credentials

Use the provided `.env.example` file as a template and create a `.env` file locally (which is git-ignored).

**Production Deployment:**
- Use a secure secrets management system (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, etc.)
- Rotate secrets regularly
- Use different secrets for each environment (dev, staging, production)

---

## Secrets Rotation Schedule

Regular rotation of secrets and credentials is essential for maintaining security.

### Rotation Frequencies

| Secret Type | Rotation Frequency | Owner | Method |
|-------------|-------------------|-------|--------|
| **Database Passwords** | Every 90 days | DevOps Team | Azure Key Vault + SQL Server |
| **JWT Signing Key** | Every 180 days | Security Team | Azure Key Vault |
| **API Keys (SendGrid, etc.)** | Every 180 days | DevOps Team | Provider Dashboard + Key Vault |
| **Storage Account Keys** | Every 90 days | DevOps Team | Azure Portal + Key Vault |
| **Service Principal Secrets** | Every 365 days | DevOps Team | Azure AD + Key Vault |
| **SSL/TLS Certificates** | Auto-renewed 30 days before expiry | Automated | Azure App Service |

### Rotation Procedure

1. **Pre-Rotation:**
   - Schedule rotation during low-traffic period
   - Notify team of upcoming rotation
   - Verify backup access to all systems
   - Document current secret versions

2. **Generate New Secret:**
   - Use secure random generation (e.g., `openssl rand -base64 48`)
   - Follow complexity requirements
   - Store securely in temporary location

3. **Update Key Vault:**
   - Add new secret version to Azure Key Vault
   - Test access from non-production environment
   - Verify Key Vault references work

4. **Deploy Application Update:**
   - Update App Service configuration (auto-uses latest Key Vault version)
   - Restart application
   - Verify health checks pass
   - Monitor Application Insights for errors

5. **Verify Functionality:**
   - Test authentication
   - Test database connectivity
   - Test external API calls
   - Verify no logged errors

6. **Cleanup Old Secret:**
   - Wait 7 days for rollback window
   - Disable old secret version in Key Vault
   - After 30 days, delete old version
   - Update documentation

7. **Document Rotation:**
   - Log rotation in audit trail
   - Update next rotation date
   - Note any issues encountered

### Emergency Rotation

If a secret is compromised:
1. **Immediately** generate and deploy new secret
2. Revoke old secret across all systems
3. Review access logs for unauthorized use
4. Conduct security assessment
5. File incident report

---

## Access Control (RBAC)

### Azure Role Assignments

Production environment uses Azure Role-Based Access Control (RBAC):

| Principal | Resource | Role | Justification |
|-----------|----------|------|---------------|
| DevOps Team | Resource Group | Contributor | Deploy and manage resources |
| DevOps Team | Key Vault | Key Vault Administrator | Manage secrets |
| App Service (Backend) | Key Vault | Key Vault Secrets User | Read secrets |
| App Service (Frontend) | Key Vault | Key Vault Secrets User | Read secrets |
| App Service (Backend) | SQL Database | Contributor | Optional: AAD auth |
| App Service (Backend) | Storage Account | Storage Blob Data Contributor | Optional: Managed identity access |
| Security Team | All Resources | Security Reader | Security audits |
| Monitoring Service | All Resources | Monitoring Reader | Metrics and logs |

### Managed Identities

All service-to-service authentication uses Azure Managed Identities:

**System-Assigned Identities:**
- Backend App Service → Key Vault
- Frontend App Service → Key Vault
- Backend App Service → SQL Database (optional)
- Backend App Service → Storage Account (optional)

**Benefits:**
- No credentials in code or configuration
- Automatic credential rotation
- Audit trail in Azure AD
- Reduced attack surface

### Key Vault Access Policies

```json
{
  "accessPolicies": [
    {
      "tenantId": "{tenant-id}",
      "objectId": "{backend-app-service-identity}",
      "permissions": {
        "secrets": ["get", "list"]
      }
    },
    {
      "tenantId": "{tenant-id}",
      "objectId": "{frontend-app-service-identity}",
      "permissions": {
        "secrets": ["get", "list"]
      }
    },
    {
      "tenantId": "{tenant-id}",
      "objectId": "{devops-team-group}",
      "permissions": {
        "secrets": ["get", "list", "set", "delete", "backup", "restore"]
      }
    }
  ]
}
```

### Least Privilege Principle

All access follows the principle of least privilege:
- Applications only have access to secrets they need
- No wildcards in permission grants
- Regular review of access policies (quarterly)
- Removal of unused or expired access

---

## Incident Response Plan

### Severity Levels

| Level | Definition | Response Time | Escalation |
|-------|------------|---------------|------------|
| **P0 - Critical** | Complete outage, data breach | Immediate | All hands on deck |
| **P1 - High** | Major functionality broken | < 1 hour | On-call engineer + manager |
| **P2 - Medium** | Degraded performance | < 4 hours | On-call engineer |
| **P3 - Low** | Minor issues, no user impact | < 24 hours | Regular workflow |

### Incident Response Workflow

#### 1. Detection and Alerting

**Detection Methods:**
- Application Insights alerts
- Azure Monitor alerts
- User reports
- Security scanning tools
- Penetration testing findings

**Alerting Channels:**
- PagerDuty/Opsgenie
- Microsoft Teams channel
- Email notifications
- SMS for critical incidents

#### 2. Initial Assessment (5-15 minutes)

**Actions:**
- Acknowledge alert
- Assess severity level
- Identify affected systems
- Determine user impact
- Estimate blast radius
- Form response team

**Decision:** Continue investigation or escalate?

#### 3. Containment (15-30 minutes)

**Actions for Security Incidents:**
- Block malicious IPs at firewall/WAF
- Revoke compromised credentials
- Isolate affected systems
- Enable DDoS protection
- Take snapshots for forensics

**Actions for Availability Incidents:**
- Redirect traffic if needed
- Scale resources
- Enable maintenance mode
- Communicate status

#### 4. Eradication and Recovery (1-4 hours)

**Security Incidents:**
- Remove malicious code/access
- Patch vulnerabilities
- Rotate all potentially compromised secrets
- Restore from clean backups if needed
- Apply security hardening

**Availability Incidents:**
- Deploy fix or rollback
- Restore from backup if needed
- Verify system functionality
- Monitor for recurrence

#### 5. Post-Incident Review (24-48 hours after)

**Required Documentation:**
- Incident timeline
- Root cause analysis
- Impact assessment
- Response effectiveness
- Lessons learned
- Action items

**Meeting Agenda:**
- What happened? (facts)
- Why did it happen? (root cause)
- How did we respond? (effectiveness)
- What will we do differently? (improvements)
- Who owns action items? (accountability)

### Communication Plan

#### Internal Communication

**Stakeholders to Notify:**
- Engineering team (immediate)
- Management (within 15 minutes for P0/P1)
- Customer support (as needed)
- Legal/Compliance (for data breaches)

**Communication Channels:**
- Slack/Teams incident channel
- Email distribution list
- Phone tree for P0 incidents

#### External Communication

**For Security Incidents:**
- Prepare customer notification (if data affected)
- Update status page
- Coordinate with PR team
- Follow legal/regulatory requirements

**Notification Templates:**
- Initial notification (incident occurred)
- Progress update (every 2 hours)
- Resolution notification
- Post-mortem summary (optional)

### Incident Commander Role

For P0/P1 incidents, designate an Incident Commander:

**Responsibilities:**
- Lead response efforts
- Make key decisions
- Coordinate team members
- Communicate with stakeholders
- Ensure documentation
- Declare incident resolved

**Authority:**
- Make architectural decisions
- Allocate resources
- Authorize emergency changes
- Skip approval processes

### Rate Limiting

Rate limiting is implemented to prevent abuse:
- **Authentication endpoints** (login, register): 10 requests per minute per IP
- **General API endpoints**: 60 requests per minute per IP
- Rate limits are configurable via environment variables

Configuration:
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "HttpStatusCode": 429
  }
}
```

### Security Headers

The application implements security headers to protect against common vulnerabilities:

| Header | Value | Purpose |
|--------|-------|---------|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME type sniffing |
| `X-Frame-Options` | `DENY` | Prevents clickjacking attacks |
| `X-XSS-Protection` | `1; mode=block` | Enables XSS protection |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | Forces HTTPS (production only) |
| `Content-Security-Policy` | (configured) | Prevents injection attacks |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Controls referrer information |

### HTTPS Requirements

**Production deployments MUST use HTTPS:**
- All API endpoints must be served over HTTPS
- HTTP Strict Transport Security (HSTS) is enabled in production
- Certificates should be obtained from a trusted Certificate Authority
- Configure your reverse proxy/load balancer to handle TLS termination

### CORS (Cross-Origin Resource Sharing)

CORS is configured to allow requests only from trusted origins:
- Configure `AllowedOrigins` in environment variables
- Never use `*` (allow all) in production
- Be specific about allowed origins

Example configuration:
```json
{
  "AllowedOrigins": [
    "https://your-production-domain.com",
    "https://www.your-production-domain.com"
  ]
}
```

### Database Security

**Connection Security:**
- Use encrypted connections to the database (TLS/SSL)
- Database credentials must be stored in environment variables
- Use strong passwords (minimum 16 characters, mix of uppercase, lowercase, numbers, symbols)
- Apply principle of least privilege - application user should only have necessary permissions

**SQL Injection Prevention:**
- Entity Framework Core is used with parameterized queries
- Never construct SQL queries with string concatenation
- Input validation is performed on all user inputs

### Dependencies Management

**Keeping Dependencies Updated:**
- Regularly update NuGet packages and npm dependencies
- Monitor security advisories for used packages
- Use `dotnet list package --vulnerable` to check for vulnerabilities
- Use `npm audit` for frontend dependencies

**Current Major Dependencies:**
- .NET 8.0
- Entity Framework Core 8.0
- Angular 21
- SQL Server 2022

### Password Security

**Password Requirements:**
- Minimum 8 characters
- Must contain uppercase and lowercase letters
- Must contain at least one digit
- Must contain at least one special character
- Passwords are hashed using ASP.NET Core Identity (PBKDF2 with HMAC-SHA256)

**Password Storage:**
- Passwords are never stored in plain text
- Salted and hashed using industry-standard algorithms
- Password reset tokens expire after a set period

### Video Upload Security

**File Upload Validation:**
- Maximum file size: 500MB (configurable)
- Allowed file types are validated
- Files are scanned for malware (implementation dependent on storage provider)
- Uploaded files are stored in isolated storage (Azure Blob Storage or AWS S3)

### Audit Logging

Security-relevant events are logged:
- Authentication attempts (success and failure)
- Authorization failures
- User account changes
- Administrative actions
- Data access for sensitive records

**Log Security:**
- Logs do not contain sensitive information (passwords, tokens, etc.)
- Structured logging with Serilog for easy parsing
- Log retention: 7 days locally, longer in production logging systems

### Security Update Policy

- **Critical vulnerabilities**: Patched within 24-48 hours
- **High-severity vulnerabilities**: Patched within 1 week
- **Medium/Low severity**: Patched in the next regular release

Security patches are released as soon as they're ready and tested.

### Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security Best Practices](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [Angular Security Guide](https://angular.io/guide/security)

## Compliance

This application follows security best practices aligned with:
- OWASP Top 10 guidelines
- CWE/SANS Top 25 Most Dangerous Software Errors
- NIST Cybersecurity Framework principles

---

## Security Audit Checklist

Use this checklist to verify security compliance before production deployment:

### Application Security

- [ ] **All secrets in Key Vault (no hardcoded secrets)**
  - Check: Search codebase for connection strings, API keys, passwords
  - Verify: All sensitive configs use Key Vault references
  
- [ ] **HTTPS enforced on all endpoints**
  - Check: `HTTPS Only` enabled in App Service
  - Check: HSTS header configured
  - Verify: HTTP redirects to HTTPS

- [ ] **SQL injection protection verified**
  - Check: All database queries use Entity Framework or parameterized queries
  - Check: No string concatenation in SQL queries
  - Test: Attempt SQL injection in all input fields

- [ ] **XSS protection headers configured**
  - Check: `X-XSS-Protection` header present
  - Check: `X-Content-Type-Options: nosniff` header present
  - Check: Content Security Policy configured
  - Test: Attempt XSS in all input fields

- [ ] **CORS properly configured**
  - Check: `AllowedOrigins` lists only trusted domains
  - Check: No `*` wildcard in production
  - Test: Verify CORS from unauthorized origin fails

- [ ] **Authentication and authorization tested**
  - Test: Unauthenticated access blocked
  - Test: Role-based access working correctly
  - Test: JWT token expiration enforced
  - Test: Password requirements enforced

- [ ] **DDoS protection enabled**
  - Check: Azure DDoS Protection Standard (if using VNet)
  - Check: Rate limiting configured
  - Check: WAF rules active (if using App Gateway/Front Door)

- [ ] **Security headers validated**
  - Check: All required headers present (see table below)
  - Test: https://securityheaders.com scan passes

### Infrastructure Security

- [ ] **Network security configured**
  - Check: SQL Server firewall allows only Azure services
  - Check: Storage account disables public blob access
  - Check: Key Vault network restrictions (if applicable)

- [ ] **Managed identities configured**
  - Check: System-assigned identity enabled on App Services
  - Check: Key Vault access policies grant minimum required permissions
  - Check: No connection strings with passwords (use managed identity)

- [ ] **Backup and recovery tested**
  - Check: Automated backups configured (SQL, Storage)
  - Test: Restore from backup successful
  - Check: Backup retention meets requirements

- [ ] **Logging and monitoring active**
  - Check: Application Insights receiving telemetry
  - Check: Diagnostic settings enabled
  - Check: Security alerts configured
  - Check: Log retention policy set

### Data Security

- [ ] **Data encryption at rest**
  - Check: SQL Database encryption enabled (TDE)
  - Check: Storage account encryption enabled
  - Check: Disk encryption enabled (if applicable)

- [ ] **Data encryption in transit**
  - Check: TLS 1.2 minimum enforced
  - Check: Database connections use encryption
  - Check: Storage connections use HTTPS only

- [ ] **Personal data handling compliant**
  - Check: Password hashing implemented
  - Check: Sensitive data not logged
  - Check: PII handling follows regulations

### Dependency Security

- [ ] **No known vulnerabilities**
  - Run: `dotnet list package --vulnerable`
  - Run: `npm audit`
  - Check: All critical/high vulnerabilities resolved

- [ ] **Dependencies up to date**
  - Check: .NET SDK is latest LTS version
  - Check: NuGet packages updated (last 90 days)
  - Check: npm packages updated (last 90 days)

### Compliance Verification

- [ ] **OWASP Top 10 compliance** (see checklist below)
- [ ] **Security scanning passed**
  - Run: CodeQL analysis
  - Run: Container vulnerability scan
  - Run: Dependency check
- [ ] **Penetration testing completed** (for major releases)
- [ ] **Security review approved** (for production)

---

## OWASP Top 10 Compliance Checklist

Based on [OWASP Top 10 2021](https://owasp.org/www-project-top-ten/)

### A01:2021 – Broken Access Control

- [ ] All API endpoints require authentication (except public endpoints)
- [ ] Role-based authorization enforced on sensitive operations
- [ ] User can only access their own data (unless admin)
- [ ] Direct object references validated (no IDOR vulnerabilities)
- [ ] CORS configured properly (no wildcard origins in production)
- [ ] Test: Attempt to access other users' data

**Mitigations Implemented:**
- JWT-based authentication
- `[Authorize]` attributes on controllers
- User ID validation in service layer
- CORS middleware with explicit origins

### A02:2021 – Cryptographic Failures

- [ ] All secrets stored securely (Azure Key Vault)
- [ ] Passwords hashed using strong algorithm (PBKDF2, bcrypt, or Argon2)
- [ ] Sensitive data encrypted in transit (HTTPS/TLS)
- [ ] Sensitive data encrypted at rest (TDE for SQL, Storage encryption)
- [ ] No sensitive data in logs or error messages
- [ ] Strong TLS configuration (TLS 1.2+, secure cipher suites)

**Mitigations Implemented:**
- ASP.NET Core Identity for password hashing
- HTTPS enforced
- TLS 1.2 minimum
- Azure-managed encryption at rest

### A03:2021 – Injection

- [ ] All database queries use ORM (Entity Framework Core)
- [ ] No raw SQL queries with string concatenation
- [ ] Input validation on all user inputs
- [ ] Output encoding for HTML rendering
- [ ] Command injection prevented (no `Process.Start` with user input)
- [ ] Test: SQL injection attempts fail

**Mitigations Implemented:**
- Entity Framework Core with parameterized queries
- Input validation attributes
- Angular automatic encoding
- No dynamic SQL construction

### A04:2021 – Insecure Design

- [ ] Threat modeling completed for core features
- [ ] Security requirements defined
- [ ] Rate limiting on authentication endpoints
- [ ] Account lockout after failed login attempts
- [ ] Business logic validation (e.g., can't award negative scholarships)
- [ ] Secure default configuration

**Mitigations Implemented:**
- Rate limiting middleware
- Account lockout policy
- Business rule validation in domain layer
- Secure defaults in appsettings

### A05:2021 – Security Misconfiguration

- [ ] No default credentials used anywhere
- [ ] Unnecessary features disabled (FTP, remote access, etc.)
- [ ] Error messages don't reveal sensitive information
- [ ] Security headers configured properly
- [ ] HTTPS enforced (HTTP redirects)
- [ ] Swagger/API documentation disabled in production
- [ ] Detailed error pages disabled in production

**Mitigations Implemented:**
- `ASPNETCORE_ENVIRONMENT=Production`
- Custom error pages
- Security headers middleware
- Swagger only in Development

### A06:2021 – Vulnerable and Outdated Components

- [ ] All dependencies up to date
- [ ] Vulnerability scanning in CI/CD pipeline
- [ ] Regular dependency updates (at least quarterly)
- [ ] No deprecated or unsupported packages
- [ ] Software Bill of Materials (SBOM) available

**Mitigations Implemented:**
- `dotnet list package --vulnerable` in CI
- `npm audit` in CI
- Dependabot alerts enabled
- Regular update schedule

### A07:2021 – Identification and Authentication Failures

- [ ] Multi-factor authentication available (optional for now)
- [ ] Strong password requirements enforced
- [ ] Account lockout after failed attempts
- [ ] Secure session management (JWT expiration)
- [ ] No credential stuffing vulnerability
- [ ] Password reset secure (token expiration, email verification)

**Mitigations Implemented:**
- ASP.NET Core Identity password policy
- JWT with expiration
- Rate limiting on auth endpoints
- Secure password reset flow

### A08:2021 – Software and Data Integrity Failures

- [ ] Code signing implemented (for releases)
- [ ] Docker images from trusted registries only
- [ ] CI/CD pipeline secured (no secrets in logs)
- [ ] Dependency integrity verified (package lock files)
- [ ] No untrusted deserialization
- [ ] Backups validated regularly

**Mitigations Implemented:**
- GitHub Actions from verified marketplace
- Container image signing (optional)
- Package lock files committed
- Regular backup testing

### A09:2021 – Security Logging and Monitoring Failures

- [ ] Security events logged (login, failed auth, authorization failures)
- [ ] Logs sent to centralized logging (Application Insights)
- [ ] Alerts configured for suspicious activity
- [ ] Log integrity protected
- [ ] No sensitive data in logs
- [ ] Incident response plan documented

**Mitigations Implemented:**
- Serilog structured logging
- Application Insights integration
- Azure Monitor alerts
- SECURITY.md incident response plan

### A10:2021 – Server-Side Request Forgery (SSRF)

- [ ] User-supplied URLs validated before fetching
- [ ] Network segmentation (if applicable)
- [ ] Internal endpoints not accessible from API
- [ ] URL validation whitelist approach
- [ ] No user control over redirect destinations

**Mitigations Implemented:**
- No user-supplied URL fetching in current implementation
- Azure networking controls
- Input validation on all URL parameters

---

## Security Metrics

Track these metrics to maintain security posture:

| Metric | Target | Frequency | Owner |
|--------|--------|-----------|-------|
| Time to patch critical vulnerabilities | < 48 hours | Continuous | Security Team |
| Failed login attempts | < 1% of total | Daily | Security Team |
| Secrets rotation compliance | 100% | Monthly | DevOps Team |
| Security scan pass rate | 100% (no critical) | Per deployment | DevOps Team |
| Mean time to detect (MTTD) | < 15 minutes | Per incident | Security Team |
| Mean time to respond (MTTR) | < 1 hour | Per incident | On-call Team |

---

## Contact

For security concerns, contact: **[security@podiumapp.com]** *(replace with actual contact)*

For general issues and questions, use GitHub Issues.

---

*Last Updated: 2026-01-15*
*Version: 2.0*
*Owner: Security Team*
