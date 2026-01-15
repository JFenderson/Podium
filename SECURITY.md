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

## Contact

For security concerns, contact: **[security@podiumapp.com]** *(replace with actual contact)*

For general issues and questions, use GitHub Issues.
