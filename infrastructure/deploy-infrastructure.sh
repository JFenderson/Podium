#!/bin/bash
# ============================================================================
# Podium Infrastructure Deployment Script
# ============================================================================
# This script validates and deploys Azure infrastructure using Bicep templates
# Usage: ./deploy-infrastructure.sh <environment> [resource-group]
# Example: ./deploy-infrastructure.sh staging podium-staging-rg
# ============================================================================

set -e # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# ============================================================================
# Functions
# ============================================================================

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if required tools are installed
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    if ! command -v az &> /dev/null; then
        log_error "Azure CLI is not installed. Please install from https://aka.ms/InstallAzureCLI"
        exit 1
    fi
    
    # Check if logged in
    if ! az account show &> /dev/null; then
        log_error "Not logged in to Azure. Please run 'az login'"
        exit 1
    fi
    
    # Check for bicep
    if ! az bicep version &> /dev/null; then
        log_info "Installing Bicep CLI..."
        az bicep install
    fi
    
    log_success "Prerequisites check passed"
}

# Validate parameters
validate_params() {
    if [ -z "$ENVIRONMENT" ]; then
        log_error "Environment parameter is required"
        echo "Usage: $0 <environment> [resource-group]"
        echo "Environments: staging, production"
        exit 1
    fi
    
    if [ "$ENVIRONMENT" != "staging" ] && [ "$ENVIRONMENT" != "production" ]; then
        log_error "Invalid environment: $ENVIRONMENT"
        echo "Valid environments are: staging, production"
        exit 1
    fi
    
    log_success "Parameters validated"
}

# Get script directory
get_script_dir() {
    SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
}

# Validate Bicep template
validate_template() {
    log_info "Validating Bicep template..."
    
    TEMPLATE_FILE="$SCRIPT_DIR/main.bicep"
    PARAMS_FILE="$SCRIPT_DIR/parameters.${ENVIRONMENT}.json"
    
    if [ ! -f "$TEMPLATE_FILE" ]; then
        log_error "Template file not found: $TEMPLATE_FILE"
        exit 1
    fi
    
    if [ ! -f "$PARAMS_FILE" ]; then
        log_error "Parameters file not found: $PARAMS_FILE"
        exit 1
    fi
    
    # Build the Bicep file to check for syntax errors
    if ! az bicep build --file "$TEMPLATE_FILE" --stdout > /dev/null 2>&1; then
        log_error "Bicep template validation failed"
        az bicep build --file "$TEMPLATE_FILE"
        exit 1
    fi
    
    log_success "Bicep template validation passed"
}

# Create or update resource group
ensure_resource_group() {
    log_info "Ensuring resource group exists: $RESOURCE_GROUP"
    
    # Default location
    LOCATION="eastus"
    
    if az group show --name "$RESOURCE_GROUP" &> /dev/null; then
        log_info "Resource group already exists"
    else
        log_info "Creating resource group..."
        az group create \
            --name "$RESOURCE_GROUP" \
            --location "$LOCATION" \
            --tags \
                Application=Podium \
                Environment="$ENVIRONMENT" \
                ManagedBy=Bicep
        log_success "Resource group created"
    fi
}

# Validate deployment (what-if)
preview_changes() {
    log_info "Previewing deployment changes..."
    
    az deployment group what-if \
        --resource-group "$RESOURCE_GROUP" \
        --template-file "$TEMPLATE_FILE" \
        --parameters "$PARAMS_FILE" \
        --no-pretty-print
    
    echo ""
    read -p "Do you want to proceed with the deployment? (yes/no): " CONFIRM
    
    if [ "$CONFIRM" != "yes" ]; then
        log_warning "Deployment cancelled by user"
        exit 0
    fi
}

# Deploy infrastructure
deploy_infrastructure() {
    log_info "Deploying infrastructure to $ENVIRONMENT environment..."
    
    DEPLOYMENT_NAME="podium-${ENVIRONMENT}-$(date +%Y%m%d-%H%M%S)"
    
    log_info "Deployment name: $DEPLOYMENT_NAME"
    
    # Deploy with detailed output
    az deployment group create \
        --name "$DEPLOYMENT_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --template-file "$TEMPLATE_FILE" \
        --parameters "$PARAMS_FILE" \
        --verbose
    
    DEPLOYMENT_STATUS=$?
    
    if [ $DEPLOYMENT_STATUS -eq 0 ]; then
        log_success "Infrastructure deployment completed successfully"
    else
        log_error "Infrastructure deployment failed with status code: $DEPLOYMENT_STATUS"
        exit $DEPLOYMENT_STATUS
    fi
}

# Get deployment outputs
get_outputs() {
    log_info "Retrieving deployment outputs..."
    
    OUTPUT_FILE="$SCRIPT_DIR/deployment-outputs-${ENVIRONMENT}.json"
    
    az deployment group show \
        --name "$DEPLOYMENT_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query properties.outputs \
        > "$OUTPUT_FILE"
    
    log_success "Outputs saved to: $OUTPUT_FILE"
    
    # Display key outputs
    echo ""
    log_info "=== Deployment Outputs ==="
    echo ""
    
    BACKEND_URL=$(az deployment group show --name "$DEPLOYMENT_NAME" --resource-group "$RESOURCE_GROUP" --query 'properties.outputs.backendUrl.value' -o tsv)
    FRONTEND_URL=$(az deployment group show --name "$DEPLOYMENT_NAME" --resource-group "$RESOURCE_GROUP" --query 'properties.outputs.frontendUrl.value' -o tsv)
    CDN_URL=$(az deployment group show --name "$DEPLOYMENT_NAME" --resource-group "$RESOURCE_GROUP" --query 'properties.outputs.cdnUrl.value' -o tsv)
    SQL_SERVER=$(az deployment group show --name "$DEPLOYMENT_NAME" --resource-group "$RESOURCE_GROUP" --query 'properties.outputs.sqlServerFqdn.value' -o tsv)
    KEY_VAULT_URI=$(az deployment group show --name "$DEPLOYMENT_NAME" --resource-group "$RESOURCE_GROUP" --query 'properties.outputs.keyVaultUri.value' -o tsv)
    
    echo "Backend URL:        $BACKEND_URL"
    echo "Frontend URL:       $FRONTEND_URL"
    echo "CDN URL:            $CDN_URL"
    echo "SQL Server:         $SQL_SERVER"
    echo "Key Vault URI:      $KEY_VAULT_URI"
    echo ""
    
    log_success "Deployment outputs retrieved"
}

# Display next steps
show_next_steps() {
    echo ""
    log_info "=== Next Steps ==="
    echo ""
    echo "1. Configure custom domain and SSL certificates"
    echo "2. Update DNS records to point to CDN endpoint"
    echo "3. Run database migrations using database/migrations/apply-migrations.sh"
    echo "4. Deploy application code to App Services"
    echo "5. Configure Application Insights alerts"
    echo "6. Test health endpoints:"
    echo "   - Backend: ${BACKEND_URL}/health"
    echo "   - Frontend: ${FRONTEND_URL}/health"
    echo ""
}

# Cleanup on error
cleanup_on_error() {
    log_error "Deployment failed. Check the error messages above."
    echo ""
    log_info "You can check deployment status in Azure Portal:"
    echo "https://portal.azure.com/#blade/HubsExtension/DeploymentDetailsBlade/id/%2Fsubscriptions%2F$(az account show --query id -o tsv)%2FresourceGroups%2F${RESOURCE_GROUP}%2Fproviders%2FMicrosoft.Resources%2Fdeployments%2F${DEPLOYMENT_NAME}"
}

# ============================================================================
# Main Script
# ============================================================================

main() {
    log_info "Starting Podium Infrastructure Deployment"
    echo "=========================================="
    echo ""
    
    # Get parameters
    ENVIRONMENT="$1"
    RESOURCE_GROUP="$2"
    
    # Set default resource group if not provided
    if [ -z "$RESOURCE_GROUP" ]; then
        RESOURCE_GROUP="podium-${ENVIRONMENT}-rg"
    fi
    
    log_info "Environment: $ENVIRONMENT"
    log_info "Resource Group: $RESOURCE_GROUP"
    echo ""
    
    # Get script directory
    get_script_dir
    
    # Run deployment steps
    check_prerequisites
    validate_params
    validate_template
    ensure_resource_group
    
    # Preview changes (optional for staging, required for production)
    if [ "$ENVIRONMENT" = "production" ]; then
        preview_changes
    else
        read -p "Do you want to preview changes before deployment? (yes/no): " PREVIEW
        if [ "$PREVIEW" = "yes" ]; then
            preview_changes
        fi
    fi
    
    # Deploy infrastructure
    deploy_infrastructure || cleanup_on_error
    
    # Get and display outputs
    get_outputs
    show_next_steps
    
    log_success "Deployment script completed successfully"
}

# Trap errors
trap cleanup_on_error ERR

# Run main function
main "$@"
