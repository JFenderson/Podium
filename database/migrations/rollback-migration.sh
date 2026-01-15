#!/bin/bash
# ============================================================================
# Database Migration Rollback Script
# ============================================================================
# This script rolls back the last applied database migration
# Usage: ./rollback-migration.sh <environment> [--auto-confirm]
# Example: ./rollback-migration.sh staging
# ============================================================================

set -e # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# ============================================================================
# Configuration
# ============================================================================

ROLLBACK_LOG_DIR="/var/log/podium-migrations"
ROLLBACK_LOG_FILE="$ROLLBACK_LOG_DIR/rollback-$(date +%Y%m%d-%H%M%S).log"

# ============================================================================
# Functions
# ============================================================================

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1" | tee -a "$ROLLBACK_LOG_FILE"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1" | tee -a "$ROLLBACK_LOG_FILE"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1" | tee -a "$ROLLBACK_LOG_FILE"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1" | tee -a "$ROLLBACK_LOG_FILE"
}

# Setup logging directory
setup_logging() {
    if [ ! -d "$ROLLBACK_LOG_DIR" ]; then
        mkdir -p "$ROLLBACK_LOG_DIR" 2>/dev/null || {
            ROLLBACK_LOG_DIR="/tmp/podium-migrations"
            mkdir -p "$ROLLBACK_LOG_DIR"
            ROLLBACK_LOG_FILE="$ROLLBACK_LOG_DIR/rollback-$(date +%Y%m%d-%H%M%S).log"
        }
    fi
    
    log_info "Rollback started at $(date)"
    log_info "Environment: $ENVIRONMENT"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check if dotnet is installed
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK is not installed"
        exit 1
    fi
    
    # Check if dotnet-ef tool is installed
    if ! dotnet tool list -g | grep -q dotnet-ef; then
        log_error "dotnet-ef tool not found. Installing..."
        dotnet tool install --global dotnet-ef
    fi
    
    log_success "Prerequisites check passed"
}

# Get connection string
get_connection_string() {
    if [ -n "$DATABASE_CONNECTION_STRING" ]; then
        CONNECTION_STRING="$DATABASE_CONNECTION_STRING"
        log_info "Using connection string from environment variable"
    else
        # Try to get from Azure Key Vault
        if [ "$ENVIRONMENT" = "production" ] || [ "$ENVIRONMENT" = "staging" ]; then
            log_info "Attempting to retrieve connection string from Azure Key Vault..."
            
            if command -v az &> /dev/null && az account show &> /dev/null; then
                VAULT_NAME="podium-kv-${ENVIRONMENT}"
                CONNECTION_STRING=$(az keyvault secret show --vault-name "$VAULT_NAME" --name "sql-connection-string" --query value -o tsv 2>/dev/null) || {
                    log_error "Failed to retrieve connection string from Key Vault"
                    exit 1
                }
                log_success "Connection string retrieved from Key Vault"
            else
                log_error "Azure CLI not available or not logged in"
                exit 1
            fi
        else
            log_error "No connection string provided"
            echo "Please set DATABASE_CONNECTION_STRING environment variable"
            exit 1
        fi
    fi
}

# Verify backup exists
verify_backup() {
    log_info "Verifying backup before rollback..."
    
    # Check if recent backup exists
    BACKUP_DIR="/var/backups/podium-database"
    
    if [ -d "$BACKUP_DIR" ]; then
        LATEST_BACKUP=$(ls -t "$BACKUP_DIR"/*.bak 2>/dev/null | head -1)
        
        if [ -n "$LATEST_BACKUP" ]; then
            BACKUP_AGE=$(( ($(date +%s) - $(stat -c %Y "$LATEST_BACKUP")) / 3600 ))
            log_info "Latest backup found: $LATEST_BACKUP (${BACKUP_AGE} hours old)"
            
            if [ $BACKUP_AGE -gt 24 ]; then
                log_warning "Backup is more than 24 hours old"
            fi
        else
            log_warning "No backup files found in $BACKUP_DIR"
        fi
    else
        log_warning "Backup directory not found: $BACKUP_DIR"
    fi
    
    if [ "$ENVIRONMENT" = "production" ] && [ -z "$AUTO_CONFIRM" ]; then
        read -p "Continue without recent backup verification? (yes/no): " CONTINUE
        if [ "$CONTINUE" != "yes" ]; then
            log_info "Rollback cancelled by user"
            exit 0
        fi
    fi
}

# Get current migration state
get_current_state() {
    log_info "Getting current migration state..."
    
    # Navigate to API project directory
    SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
    PROJECT_DIR="$SCRIPT_DIR/../../Backend/Podium/Podium.API"
    
    if [ ! -d "$PROJECT_DIR" ]; then
        log_error "Project directory not found: $PROJECT_DIR"
        exit 1
    fi
    
    cd "$PROJECT_DIR"
    
    # Get list of applied migrations
    log_info "Applied migrations:"
    APPLIED_MIGRATIONS=$(dotnet ef migrations list --no-build --connection "$CONNECTION_STRING" 2>&1 | grep "Applied" || echo "")
    
    if [ -z "$APPLIED_MIGRATIONS" ]; then
        log_error "No applied migrations found. Nothing to rollback."
        exit 1
    fi
    
    echo "$APPLIED_MIGRATIONS" | tee -a "$ROLLBACK_LOG_FILE"
    
    # Get the last applied migration
    LAST_MIGRATION=$(echo "$APPLIED_MIGRATIONS" | tail -1 | awk '{print $2}')
    log_info "Last applied migration: $LAST_MIGRATION"
    
    # Get the migration before last (target for rollback)
    PREVIOUS_MIGRATION=$(echo "$APPLIED_MIGRATIONS" | tail -2 | head -1 | awk '{print $2}')
    
    if [ -z "$PREVIOUS_MIGRATION" ]; then
        TARGET_MIGRATION="0"
        log_warning "This will rollback to empty database (no migrations)"
    else
        TARGET_MIGRATION="$PREVIOUS_MIGRATION"
        log_info "Target migration (after rollback): $TARGET_MIGRATION"
    fi
}

# Confirm rollback
confirm_rollback() {
    if [ -n "$AUTO_CONFIRM" ]; then
        log_info "Auto-confirm enabled, skipping confirmation"
        return 0
    fi
    
    echo ""
    log_warning "===== ROLLBACK CONFIRMATION ====="
    echo "Environment: $ENVIRONMENT"
    echo "Current Migration: $LAST_MIGRATION"
    echo "Target Migration: $TARGET_MIGRATION"
    echo ""
    
    if [ "$ENVIRONMENT" = "production" ]; then
        log_warning "You are about to rollback PRODUCTION database"
        read -p "Type 'ROLLBACK PRODUCTION' to confirm: " CONFIRM
        
        if [ "$CONFIRM" != "ROLLBACK PRODUCTION" ]; then
            log_info "Rollback cancelled by user"
            exit 0
        fi
    else
        read -p "Do you want to proceed with rollback? (yes/no): " CONFIRM
        
        if [ "$CONFIRM" != "yes" ]; then
            log_info "Rollback cancelled by user"
            exit 0
        fi
    fi
}

# Execute rollback
execute_rollback() {
    log_info "Executing rollback..."
    
    # Build the project first
    log_info "Building project..."
    dotnet build --configuration Release 2>&1 | tee -a "$ROLLBACK_LOG_FILE"
    
    # Rollback to previous migration
    log_info "Rolling back to migration: $TARGET_MIGRATION"
    
    if dotnet ef database update "$TARGET_MIGRATION" --connection "$CONNECTION_STRING" --verbose 2>&1 | tee -a "$ROLLBACK_LOG_FILE"; then
        log_success "Rollback completed successfully"
        return 0
    else
        log_error "Rollback failed"
        return 1
    fi
}

# Verify rollback
verify_rollback() {
    log_info "Verifying rollback..."
    
    # List current migrations
    CURRENT_STATE=$(dotnet ef migrations list --no-build --connection "$CONNECTION_STRING" 2>&1 | tee -a "$ROLLBACK_LOG_FILE")
    
    # Check that the target migration is now the last applied
    if [ "$TARGET_MIGRATION" = "0" ]; then
        if echo "$CURRENT_STATE" | grep -q "Applied"; then
            log_error "Rollback verification failed: migrations still applied"
            return 1
        else
            log_success "Rollback verified: database is now empty"
            return 0
        fi
    else
        if echo "$CURRENT_STATE" | grep -q "$TARGET_MIGRATION" | grep -q "Applied"; then
            log_success "Rollback verified: now at migration $TARGET_MIGRATION"
            return 0
        else
            log_warning "Could not verify rollback state"
            return 1
        fi
    fi
}

# Log incident
log_incident() {
    log_info "Logging rollback incident..."
    
    INCIDENT_LOG="$ROLLBACK_LOG_DIR/incidents.log"
    
    {
        echo "=========================="
        echo "Rollback Incident Report"
        echo "=========================="
        echo "Date: $(date)"
        echo "Environment: $ENVIRONMENT"
        echo "Rolled back from: $LAST_MIGRATION"
        echo "Rolled back to: $TARGET_MIGRATION"
        echo "Initiated by: $(whoami)"
        echo "Log file: $ROLLBACK_LOG_FILE"
        echo ""
    } >> "$INCIDENT_LOG"
    
    log_info "Incident logged to: $INCIDENT_LOG"
}

# ============================================================================
# Main Script
# ============================================================================

main() {
    echo "============================================"
    log_info "Podium Database Rollback Script"
    echo "============================================"
    echo ""
    
    # Get parameters
    ENVIRONMENT="$1"
    AUTO_CONFIRM=""
    
    if [ "$2" = "--auto-confirm" ]; then
        AUTO_CONFIRM="true"
    fi
    
    # Validate environment
    if [ -z "$ENVIRONMENT" ]; then
        log_error "Environment parameter is required"
        echo "Usage: $0 <environment> [--auto-confirm]"
        echo "Environments: development, staging, production"
        exit 1
    fi
    
    # Setup logging
    setup_logging
    
    # Check prerequisites
    check_prerequisites
    
    # Get connection string
    get_connection_string
    
    # Verify backup exists
    verify_backup
    
    # Get current state
    get_current_state
    
    # Confirm rollback
    confirm_rollback
    
    # Execute rollback
    if execute_rollback; then
        # Verify rollback
        if verify_rollback; then
            log_success "Rollback completed and verified at $(date)"
            log_incident
            exit 0
        else
            log_warning "Rollback completed but verification failed"
            log_incident
            exit 1
        fi
    else
        log_error "Rollback failed at $(date)"
        log_incident
        exit 1
    fi
}

# Trap errors
trap 'log_error "Script terminated unexpectedly"' ERR

# Run main function
main "$@"
