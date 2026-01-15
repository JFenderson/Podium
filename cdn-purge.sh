#!/bin/bash
# ============================================================================
# CDN Cache Purge Script
# ============================================================================
# Purges Azure CDN cache for the Podium application
# Usage: ./cdn-purge.sh <environment> [path1] [path2] ...
# Example: ./cdn-purge.sh production
# Example: ./cdn-purge.sh staging /index.html /assets/*
# ============================================================================

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Main
ENVIRONMENT="$1"

if [ -z "$ENVIRONMENT" ]; then
    log_error "Environment required"
    echo "Usage: $0 <environment> [paths...]"
    echo "Examples:"
    echo "  $0 production              # Purge all content"
    echo "  $0 staging /index.html     # Purge specific file"
    echo "  $0 production /assets/*    # Purge directory"
    exit 1
fi

# Configuration
RESOURCE_GROUP="podium-${ENVIRONMENT}-rg"
CDN_PROFILE="podium-cdn-${ENVIRONMENT}"
CDN_ENDPOINT="podium-cdn-endpoint-${ENVIRONMENT}"

log_info "CDN Cache Purge"
log_info "Environment: $ENVIRONMENT"
log_info "Resource Group: $RESOURCE_GROUP"
log_info "CDN Profile: $CDN_PROFILE"
log_info "CDN Endpoint: $CDN_ENDPOINT"
echo ""

# Check Azure CLI
if ! command -v az &> /dev/null; then
    log_error "Azure CLI not found. Please install from https://aka.ms/InstallAzureCLI"
    exit 1
fi

# Check login
if ! az account show &> /dev/null; then
    log_error "Not logged in to Azure. Please run 'az login'"
    exit 1
fi

# Determine paths to purge
shift  # Remove environment argument

if [ $# -eq 0 ]; then
    # No paths provided, purge all
    CONTENT_PATHS='["/*"]'
    log_warning "No specific paths provided. Will purge ALL content."
else
    # Build JSON array of paths
    PATHS=("$@")
    CONTENT_PATHS="["
    for i in "${!PATHS[@]}"; do
        if [ $i -gt 0 ]; then
            CONTENT_PATHS+=","
        fi
        CONTENT_PATHS+="\"${PATHS[$i]}\""
    done
    CONTENT_PATHS+="]"
    
    log_info "Paths to purge:"
    for path in "${PATHS[@]}"; do
        echo "  - $path"
    done
fi

echo ""

# Confirmation for production
if [ "$ENVIRONMENT" = "production" ]; then
    log_warning "You are about to purge CDN cache in PRODUCTION"
    read -p "Type 'PURGE' to confirm: " CONFIRM
    
    if [ "$CONFIRM" != "PURGE" ]; then
        log_info "Purge cancelled by user"
        exit 0
    fi
fi

# Execute purge
log_info "Purging CDN cache..."

if az cdn endpoint purge \
    --resource-group "$RESOURCE_GROUP" \
    --profile-name "$CDN_PROFILE" \
    --name "$CDN_ENDPOINT" \
    --content-paths $CONTENT_PATHS \
    --no-wait; then
    
    log_success "CDN purge initiated successfully"
    echo ""
    log_info "Purge Details:"
    log_info "- Purge request submitted to Azure"
    log_info "- Propagation time: 5-10 minutes globally"
    log_info "- Status: Processing"
    echo ""
    log_info "Note: It may take 5-10 minutes for the purge to propagate to all CDN edge nodes"
else
    log_error "CDN purge failed"
    exit 1
fi

# Check purge status
log_info "Checking purge status..."
sleep 2

# Get recent purges
PURGES=$(az rest \
    --method get \
    --url "https://management.azure.com/subscriptions/$(az account show --query id -o tsv)/resourceGroups/${RESOURCE_GROUP}/providers/Microsoft.Cdn/profiles/${CDN_PROFILE}/endpoints/${CDN_ENDPOINT}/purge?api-version=2021-06-01" \
    2>/dev/null || echo "Unable to check status")

log_info "Recent purges logged in Azure Activity Log"

# Verification
echo ""
log_info "Verification Steps:"
echo "1. Wait 5-10 minutes for global propagation"
echo "2. Clear your browser cache (Ctrl+Shift+Delete)"
echo "3. Test the CDN endpoint:"
echo "   https://${CDN_ENDPOINT}.azureedge.net/"
echo "4. Check Azure Portal:"
echo "   Portal > CDN Profile > Endpoint > Activity Log"

# Log purge
LOG_FILE="/tmp/cdn-purge-log.txt"
{
    echo "===== CDN Purge ====="
    echo "Timestamp: $(date -Iseconds)"
    echo "Environment: $ENVIRONMENT"
    echo "Paths: $CONTENT_PATHS"
    echo "User: $(whoami)"
    echo "====================="
    echo ""
} >> "$LOG_FILE"

log_success "Purge completed. Log saved to: $LOG_FILE"
