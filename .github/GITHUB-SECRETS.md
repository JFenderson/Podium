# GitHub Secrets and Variables Configuration

This document describes all the secrets and variables required for the Podium application's CI/CD workflows.

## Table of Contents
- [Overview](#overview)
- [Required Secrets](#required-secrets)
- [Environment-Specific Secrets](#environment-specific-secrets)
- [How to Add Secrets](#how-to-add-secrets)
- [Security Best Practices](#security-best-practices)

## Overview

GitHub Secrets are encrypted environment variables used to store sensitive information needed by the CI/CD workflows. These secrets are never exposed in logs and are only accessible to the workflows.

## Required Secrets

### Repository-Level Secrets

These secrets should be added at the repository level (available to all workflows):

#### `GITHUB_TOKEN`
- **Description**: Automatically provided by GitHub Actions
- **Purpose**: Authenticate with GitHub Container Registry (GHCR) and perform API operations
- **How to obtain**: Automatically available - no setup needed
- **Permissions needed**: `write:packages` for pushing Docker images
- **Note**: This is automatically provided by GitHub Actions - you don't need to create it

#### `DATABASE_CONNECTION_STRING`
- **Description**: Connection string for the production database
- **Purpose**: Database access for deployments and migrations
- **How to obtain**: From your database provider (Azure SQL, AWS RDS, etc.)
- **Example Format**:
  ```
  Server=your-server.database.windows.net;Database=PodiumDb;User Id=your-user;Password=your-password;Encrypt=True;TrustServerCertificate=False
  ```
- **Required for**: Staging and Production deployments

#### `JWT_SECRET`
- **Description**: Secret key used to sign JWT authentication tokens
- **Purpose**: Secure authentication token generation and validation
- **How to obtain**: Generate a strong random string (minimum 32 characters)
- **Example generation**:
  ```bash
  openssl rand -base64 32
  ```
  or
  ```bash
  node -e "console.log(require('crypto').randomBytes(32).toString('hex'))"
  ```
- **⚠️ CRITICAL**: Must be the same across all environments for the same database
- **Required for**: All environments (Staging, Production)

#### `AZURE_STORAGE_CONNECTION_STRING`
- **Description**: Connection string for Azure Blob Storage
- **Purpose**: File and video storage
- **How to obtain**: 
  1. Go to Azure Portal
  2. Navigate to your Storage Account
  3. Go to "Access keys"
  4. Copy one of the connection strings
- **Example Format**:
  ```
  DefaultEndpointsProtocol=https;AccountName=youraccountname;AccountKey=youraccountkey;EndpointSuffix=core.windows.net
  ```
- **Required for**: Staging and Production deployments
- **Alternative**: If using AWS S3, you'll need `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` instead

### Optional Secrets

#### `AZURE_WEBAPP_NAME`
- **Description**: Azure Web App name (if deploying to Azure App Service)
- **Purpose**: Identify the Azure Web App for deployment
- **How to obtain**: From Azure Portal when you create a Web App
- **Required for**: Azure App Service deployments only

#### `SENDGRID_API_KEY`
- **Description**: SendGrid API key for email services
- **Purpose**: Send transactional emails (password resets, notifications)
- **How to obtain**:
  1. Sign up at https://sendgrid.com
  2. Go to Settings → API Keys
  3. Create a new API key with "Mail Send" permissions
- **Required for**: Production environment (optional for staging)

## Environment-Specific Secrets

GitHub Environments allow you to set secrets specific to `staging` or `production` environments.

### Staging Environment

Create a `staging` environment with these secrets:

| Secret Name | Description | Example |
|------------|-------------|---------|
| `DATABASE_CONNECTION_STRING` | Staging database connection | `Server=staging-db.database.windows.net;...` |
| `JWT_SECRET` | JWT signing key (can be different from prod) | `staging-jwt-secret-32-chars-min` |
| `AZURE_STORAGE_CONNECTION_STRING` | Staging storage account | `DefaultEndpointsProtocol=https;...` |

### Production Environment

Create a `production` environment with these secrets and enable protection rules:

| Secret Name | Description | Example |
|------------|-------------|---------|
| `DATABASE_CONNECTION_STRING` | Production database connection | `Server=prod-db.database.windows.net;...` |
| `JWT_SECRET` | JWT signing key (strong, unique) | `prod-jwt-secret-32-chars-minimum` |
| `AZURE_STORAGE_CONNECTION_STRING` | Production storage account | `DefaultEndpointsProtocol=https;...` |

**Protection Rules for Production:**
- ✅ Required reviewers: 1-6 reviewers
- ✅ Wait timer: 0-43,200 minutes (optional)
- ✅ Deployment branches: Only `main` branch

## How to Add Secrets

### Repository Secrets

1. **Navigate to your repository on GitHub**
2. **Go to Settings**
3. **In the left sidebar, click "Secrets and variables" → "Actions"**
4. **Click "New repository secret"**
5. **Enter the secret name** (e.g., `DATABASE_CONNECTION_STRING`)
6. **Enter the secret value**
7. **Click "Add secret"**

### Environment Secrets

1. **Navigate to your repository on GitHub**
2. **Go to Settings**
3. **In the left sidebar, click "Environments"**
4. **Click "New environment"** (if not created yet)
5. **Enter environment name** (`staging` or `production`)
6. **Configure protection rules** (especially for production)
7. **Scroll down to "Environment secrets"**
8. **Click "Add secret"**
9. **Enter the secret name and value**
10. **Click "Add secret"**

## Security Best Practices

### General Guidelines

1. **Never commit secrets to source code**
   - Always use GitHub Secrets
   - Check `.gitignore` includes `.env`, `.env.local`, etc.

2. **Use strong, unique secrets**
   - Generate cryptographically secure random values
   - Minimum 32 characters for JWT secrets
   - Use different secrets for staging and production

3. **Rotate secrets regularly**
   - Change production secrets quarterly
   - Rotate immediately if compromised
   - Update in both GitHub and deployment environment

4. **Limit secret access**
   - Use environment-specific secrets
   - Enable branch protection rules
   - Require approvals for production deployments

5. **Monitor secret usage**
   - Review workflow runs regularly
   - Check for any unauthorized access
   - Use GitHub's audit log

### Secret Rotation Process

When rotating secrets:

1. **Generate new secret value**
2. **Update in deployment environment first** (database, storage, etc.)
3. **Update GitHub secret**
4. **Deploy and verify**
5. **Deactivate old secret**
6. **Document the rotation** (date, reason)

### What to Do If a Secret is Compromised

1. **Immediately rotate the secret**
2. **Update in all environments**
3. **Review recent workflow runs**
4. **Check for unauthorized access**
5. **Consider rotating dependent secrets**
6. **Notify security team if applicable**

## Verification Checklist

Before deploying, ensure:

- [ ] All required secrets are set
- [ ] Secrets are set in correct environments
- [ ] Production environment has protection rules enabled
- [ ] JWT secrets are strong (32+ characters)
- [ ] Database connection strings are correct
- [ ] Storage connection strings are valid
- [ ] Secrets are not exposed in logs
- [ ] Team members with access are documented

## Additional Resources

- [GitHub Encrypted Secrets Documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [GitHub Environments Documentation](https://docs.github.com/en/actions/deployment/targeting-different-environments/using-environments-for-deployment)
- [Azure Key Vault](https://azure.microsoft.com/en-us/services/key-vault/) - For additional secret management
- [AWS Secrets Manager](https://aws.amazon.com/secrets-manager/) - Alternative secret management

## Support

For questions about secrets configuration:
- **Security concerns**: Contact the security team
- **Deployment issues**: Create an issue in the repository
- **Azure/AWS configuration**: Refer to provider documentation

---

**Last Updated**: 2026-01-15  
**Maintained by**: DevOps Team
