#!/bin/bash
# ============================================================================
# Environment Variables Validation Script
# ============================================================================
# This script validates that all required environment variables are set
# and have valid formats before deployment
# Usage: ./validate-env.sh [environment]
# Example: ./validate-env.sh production
# ============================================================================

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Counters
ERRORS=0
WARNINGS=0
PASSED=0

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[PASS]${NC} $1"
    ((PASSED++))
}

log_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
    ((WARNINGS++))
}

log_error() {
    echo -e "${RED}[FAIL]${NC} $1"
    ((ERRORS++))
}

# Validate required variable is set
validate_required() {
    local var_name="$1"
    local var_value="${!var_name}"
    local description="$2"
    
    if [ -z "$var_value" ]; then
        log_error "$var_name is required but not set - $description"
        return 1
    else
        log_success "$var_name is set"
        return 0
    fi
}

# Validate optional variable
validate_optional() {
    local var_name="$1"
    local var_value="${!var_name}"
    local description="$2"
    
    if [ -z "$var_value" ]; then
        log_warning "$var_name is not set (optional) - $description"
        return 0
    else
        log_success "$var_name is set"
        return 0
    fi
}

# Validate connection string format
validate_connection_string() {
    local var_name="$1"
    local var_value="${!var_name}"
    
    if [ -z "$var_value" ]; then
        return 0 # Already validated as required/optional
    fi
    
    # Check for SQL Server connection string pattern
    if [[ "$var_value" =~ Server=.+Database=.+ ]]; then
        log_success "$var_name has valid SQL Server connection string format"
        return 0
    # Check for Key Vault reference
    elif [[ "$var_value" =~ @Microsoft\.KeyVault ]]; then
        log_success "$var_name uses Key Vault reference"
        return 0
    else
        log_warning "$var_name may have invalid connection string format"
        return 0
    fi
}

# Validate URL format
validate_url() {
    local var_name="$1"
    local var_value="${!var_name}"
    
    if [ -z "$var_value" ]; then
        return 0
    fi
    
    if [[ "$var_value" =~ ^https?:// ]]; then
        log_success "$var_name has valid URL format"
        return 0
    else
        log_error "$var_name has invalid URL format: $var_value"
        return 1
    fi
}

# Validate minimum length
validate_min_length() {
    local var_name="$1"
    local var_value="${!var_name}"
    local min_length="$2"
    local description="$3"
    
    if [ -z "$var_value" ]; then
        return 0
    fi
    
    # Skip if it's a Key Vault reference
    if [[ "$var_value" =~ @Microsoft\.KeyVault ]]; then
        log_success "$var_name uses Key Vault reference"
        return 0
    fi
    
    if [ ${#var_value} -ge $min_length ]; then
        log_success "$var_name meets minimum length ($min_length characters)"
        return 0
    else
        log_error "$var_name is too short (min $min_length characters) - $description"
        return 1
    fi
}

# Main validation
main() {
    echo "============================================"
    log_info "Podium Environment Validation"
    echo "============================================"
    echo ""
    
    ENVIRONMENT="${1:-development}"
    log_info "Validating environment: $ENVIRONMENT"
    echo ""
    
    # Load environment variables if .env file exists
    if [ -f ".env" ]; then
        log_info "Loading .env file..."
        set -a
        source .env
        set +a
    fi
    
    echo "=== Database Configuration ==="
    validate_required "ConnectionStrings__DefaultConnection" "Database connection string"
    validate_connection_string "ConnectionStrings__DefaultConnection"
    
    if [ "$ENVIRONMENT" = "development" ]; then
        validate_optional "DB_SA_PASSWORD" "SQL Server SA password for Docker"
    fi
    echo ""
    
    echo "=== Authentication & Security ==="
    validate_required "JWT__Secret" "JWT signing secret"
    validate_min_length "JWT__Secret" 32 "JWT secret must be at least 32 characters"
    validate_optional "JWT__Issuer" "JWT issuer"
    validate_optional "JWT__Audience" "JWT audience"
    validate_optional "Video__TranscodingSecret" "Video transcoding secret"
    echo ""
    
    echo "=== Storage Configuration ==="
    validate_required "StorageProvider" "Storage provider (Azure or AWS)"
    
    if [ "$StorageProvider" = "Azure" ]; then
        validate_required "AzureStorage__ConnectionString" "Azure Storage connection string"
        validate_optional "AzureStorage__ContainerName" "Azure Storage container name"
    elif [ "$StorageProvider" = "AWS" ]; then
        validate_required "AWS__AccessKey" "AWS access key"
        validate_required "AWS__SecretKey" "AWS secret key"
        validate_required "AWS__BucketName" "AWS S3 bucket name"
        validate_optional "AWS__Region" "AWS region"
    fi
    echo ""
    
    echo "=== Email Configuration ==="
    if [ "$ENVIRONMENT" = "production" ] || [ "$ENVIRONMENT" = "staging" ]; then
        validate_required "SendGrid__ApiKey" "SendGrid API key for emails"
    else
        validate_optional "SendGrid__ApiKey" "SendGrid API key for emails"
    fi
    validate_optional "SendGrid__FromEmail" "Sender email address"
    echo ""
    
    echo "=== CORS & URLs ==="
    validate_required "AllowedOrigins__0" "At least one allowed origin for CORS"
    validate_url "AllowedOrigins__0"
    validate_optional "AllowedOrigins__1" "Additional allowed origin"
    validate_optional "App__ClientUrl" "Frontend application URL"
    validate_url "App__ClientUrl"
    echo ""
    
    echo "=== Monitoring ==="
    if [ "$ENVIRONMENT" = "production" ] || [ "$ENVIRONMENT" = "staging" ]; then
        validate_required "APPLICATIONINSIGHTS_CONNECTION_STRING" "Application Insights connection"
        validate_required "ENABLE_APPLICATION_INSIGHTS" "Application Insights enable flag"
    else
        validate_optional "APPLICATIONINSIGHTS_CONNECTION_STRING" "Application Insights connection"
        validate_optional "ENABLE_APPLICATION_INSIGHTS" "Application Insights enable flag"
    fi
    echo ""
    
    echo "=== Security Headers ==="
    if [ "$ENVIRONMENT" = "production" ]; then
        if [ "$SecurityHeaders__EnableHSTS" != "true" ]; then
            log_warning "SecurityHeaders__EnableHSTS should be 'true' in production"
        else
            log_success "HSTS is enabled"
        fi
    fi
    
    if [ "$ENVIRONMENT" = "production" ] && [ "$ASPNETCORE_ENVIRONMENT" != "Production" ]; then
        log_error "ASPNETCORE_ENVIRONMENT must be 'Production' in production"
    fi
    echo ""
    
    echo "=== Environment Settings ==="
    validate_required "ASPNETCORE_ENVIRONMENT" "ASP.NET Core environment"
    validate_optional "ASPNETCORE_URLS" "Application URLs"
    echo ""
    
    # Summary
    echo "============================================"
    echo "Validation Summary"
    echo "============================================"
    echo -e "${GREEN}Passed:${NC}   $PASSED"
    echo -e "${YELLOW}Warnings:${NC} $WARNINGS"
    echo -e "${RED}Errors:${NC}   $ERRORS"
    echo ""
    
    if [ $ERRORS -gt 0 ]; then
        log_error "Validation failed with $ERRORS errors"
        echo ""
        echo "Please fix the errors above before deploying"
        exit 1
    elif [ $WARNINGS -gt 0 ]; then
        log_warning "Validation passed with $WARNINGS warnings"
        echo ""
        echo "Review warnings before deploying to production"
        exit 0
    else
        log_success "All validations passed!"
        echo ""
        echo "Environment is ready for deployment"
        exit 0
    fi
}

# Run main
main "$@"
