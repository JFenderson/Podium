#!/bin/bash
# ============================================================================
# Database Migration Application Script
# ============================================================================
# This script applies pending database migrations to the specified environment
# Usage: ./apply-migrations.sh <environment> [connection-string]
# Example: ./apply-migrations.sh staging
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

MIGRATION_LOG_DIR="/var/log/podium-migrations"
MIGRATION_LOG_FILE="$MIGRATION_LOG_DIR/migrations-$(date +%Y%m%d-%H%M%S).log"

# ============================================================================
# Functions
# ============================================================================

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1" | tee -a "$MIGRATION_LOG_FILE"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1" | tee -a "$MIGRATION_LOG_FILE"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1" | tee -a "$MIGRATION_LOG_FILE"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1" | tee -a "$MIGRATION_LOG_FILE"
}

# Setup logging directory
setup_logging() {
    if [ ! -d "$MIGRATION_LOG_DIR" ]; then
        mkdir -p "$MIGRATION_LOG_DIR" 2>/dev/null || {
            MIGRATION_LOG_DIR="/tmp/podium-migrations"
            mkdir -p "$MIGRATION_LOG_DIR"
            MIGRATION_LOG_FILE="$MIGRATION_LOG_DIR/migrations-$(date +%Y%m%d-%H%M%S).log"
        }
    fi
    
    log_info "Migration started at $(date)"
    log_info "Environment: $ENVIRONMENT"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check if dotnet is installed
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK is not installed. Please install from https://dotnet.microsoft.com/download"
        exit 1
    fi
    
    # Check dotnet version
    DOTNET_VERSION=$(dotnet --version)
    log_info "Using .NET SDK version: $DOTNET_VERSION"
    
    # Check if dotnet-ef tool is installed
    if ! dotnet tool list -g | grep -q dotnet-ef; then
        log_warning "dotnet-ef tool not found. Installing..."
        dotnet tool install --global dotnet-ef
    fi
    
    log_success "Prerequisites check passed"
}

# Get connection string
get_connection_string() {
    if [ -n "$CONNECTION_STRING_PARAM" ]; then
        CONNECTION_STRING="$CONNECTION_STRING_PARAM"
        log_info "Using connection string from parameter"
    elif [ -n "$DATABASE_CONNECTION_STRING" ]; then
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
            echo "Please provide connection string via parameter or DATABASE_CONNECTION_STRING environment variable"
            exit 1
        fi
    fi
}

# Get current database version
get_current_version() {
    log_info "Checking current database version..."
    
    # Navigate to API project directory
    SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
    PROJECT_DIR="$SCRIPT_DIR/../../Backend/Podium/Podium.API"
    
    if [ ! -d "$PROJECT_DIR" ]; then
        log_error "Project directory not found: $PROJECT_DIR"
        exit 1
    fi
    
    cd "$PROJECT_DIR"
    
    # List applied migrations
    log_info "Applied migrations:"
    dotnet ef migrations list --no-build --connection "$CONNECTION_STRING" 2>&1 | tee -a "$MIGRATION_LOG_FILE" || {
        log_warning "Could not list migrations (database might not exist yet)"
    }
}

# Create database backup before migration
create_backup() {
    log_info "Creating database backup before migration..."
    
    BACKUP_SCRIPT="$SCRIPT_DIR/backup-database.sh"
    
    if [ -f "$BACKUP_SCRIPT" ]; then
        bash "$BACKUP_SCRIPT" "$ENVIRONMENT" "pre-migration-$(date +%Y%m%d-%H%M%S)" || {
            log_warning "Backup failed, but continuing with migration"
        }
    else
        log_warning "Backup script not found at $BACKUP_SCRIPT"
        
        if [ "$ENVIRONMENT" = "production" ]; then
            read -p "Continue without backup? (yes/no): " CONTINUE
            if [ "$CONTINUE" != "yes" ]; then
                log_info "Migration cancelled by user"
                exit 0
            fi
        fi
    fi
}

# List pending migrations
list_pending_migrations() {
    log_info "Checking for pending migrations..."
    
    PENDING_MIGRATIONS=$(dotnet ef migrations list --no-build --connection "$CONNECTION_STRING" 2>&1 | grep "Pending" || echo "")
    
    if [ -z "$PENDING_MIGRATIONS" ]; then
        log_info "No pending migrations found. Database is up to date."
        return 1
    fi
    
    log_info "Pending migrations:"
    echo "$PENDING_MIGRATIONS" | tee -a "$MIGRATION_LOG_FILE"
    
    return 0
}

# Apply migrations
apply_migrations() {
    log_info "Applying database migrations..."
    
    # Build the project first
    log_info "Building project..."
    dotnet build --configuration Release 2>&1 | tee -a "$MIGRATION_LOG_FILE"
    
    # Apply migrations with detailed output
    log_info "Running EF Core migrations..."
    
    if dotnet ef database update --connection "$CONNECTION_STRING" --verbose 2>&1 | tee -a "$MIGRATION_LOG_FILE"; then
        log_success "Migrations applied successfully"
        return 0
    else
        log_error "Migration failed"
        return 1
    fi
}

# Verify migration success
verify_migration() {
    log_info "Verifying migration..."
    
    # Check that all migrations are applied
    dotnet ef migrations list --no-build --connection "$CONNECTION_STRING" 2>&1 | tee -a "$MIGRATION_LOG_FILE"
    
    # Check if there are any pending migrations left
    if dotnet ef migrations list --no-build --connection "$CONNECTION_STRING" 2>&1 | grep -q "Pending"; then
        log_warning "Some migrations are still pending"
        return 1
    else
        log_success "All migrations have been applied"
        return 0
    fi
}

# Rollback on failure
rollback_on_failure() {
    log_error "Migration failed. Initiating rollback..."
    
    ROLLBACK_SCRIPT="$SCRIPT_DIR/rollback-migration.sh"
    
    if [ -f "$ROLLBACK_SCRIPT" ]; then
        log_info "Running rollback script..."
        bash "$ROLLBACK_SCRIPT" "$ENVIRONMENT" --auto-confirm || {
            log_error "Rollback also failed. Manual intervention required!"
            exit 1
        }
    else
        log_error "Rollback script not found. Manual rollback required!"
        exit 1
    fi
}

# Send notification
send_notification() {
    local STATUS=$1
    local MESSAGE=$2
    
    log_info "Sending notification: $MESSAGE"
    
    # This is a placeholder for notification integration
    # Integrate with your notification service (email, Slack, Teams, etc.)
    # Example: curl -X POST your-webhook-url -d "{\"text\": \"$MESSAGE\"}"
}

# ============================================================================
# Main Script
# ============================================================================

main() {
    echo "============================================"
    log_info "Podium Database Migration Script"
    echo "============================================"
    echo ""
    
    # Get parameters
    ENVIRONMENT="$1"
    CONNECTION_STRING_PARAM="$2"
    
    # Validate environment
    if [ -z "$ENVIRONMENT" ]; then
        log_error "Environment parameter is required"
        echo "Usage: $0 <environment> [connection-string]"
        echo "Environments: development, staging, production"
        exit 1
    fi
    
    # Setup logging
    setup_logging
    
    # Check prerequisites
    check_prerequisites
    
    # Get connection string
    get_connection_string
    
    # Get current version
    get_current_version
    
    # Check for pending migrations
    if ! list_pending_migrations; then
        log_success "Database is already up to date. No action needed."
        exit 0
    fi
    
    # Confirmation for production
    if [ "$ENVIRONMENT" = "production" ]; then
        echo ""
        log_warning "You are about to apply migrations to PRODUCTION database"
        read -p "Type 'APPLY PRODUCTION MIGRATIONS' to confirm: " CONFIRM
        
        if [ "$CONFIRM" != "APPLY PRODUCTION MIGRATIONS" ]; then
            log_info "Migration cancelled by user"
            exit 0
        fi
    fi
    
    # Create backup
    create_backup
    
    # Apply migrations
    if apply_migrations; then
        # Verify migration
        if verify_migration; then
            log_success "Migration completed successfully at $(date)"
            send_notification "SUCCESS" "Database migrations applied successfully to $ENVIRONMENT"
            exit 0
        else
            log_warning "Migration applied but verification failed"
            send_notification "WARNING" "Database migrations applied to $ENVIRONMENT but verification failed"
            exit 1
        fi
    else
        log_error "Migration failed at $(date)"
        send_notification "ERROR" "Database migration failed for $ENVIRONMENT"
        rollback_on_failure
        exit 1
    fi
}

# Trap errors
trap 'log_error "Script terminated unexpectedly"' ERR

# Run main function
main "$@"
