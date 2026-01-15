# Azure CDN Configuration Guide

This document describes the Azure CDN setup for the Podium application frontend.

## Table of Contents

- [Overview](#overview)
- [CDN Endpoint Configuration](#cdn-endpoint-configuration)
- [Caching Rules](#caching-rules)
- [Custom Domain Setup](#custom-domain-setup)
- [SSL/TLS Configuration](#ssltls-configuration)
- [Optimization Settings](#optimization-settings)
- [Purging CDN Cache](#purging-cdn-cache)
- [Monitoring](#monitoring)

---

## Overview

Azure CDN (Content Delivery Network) is used to:
- Deliver frontend assets (HTML, CSS, JavaScript, images) with low latency
- Reduce load on origin App Service
- Improve global performance
- Provide HTTPS delivery
- Cache static content closer to users

**CDN Profile:** Standard Microsoft tier
**Origin:** Frontend App Service

---

## CDN Endpoint Configuration

### Basic Settings

| Setting | Value | Description |
|---------|-------|-------------|
| **Profile Name** | `podium-cdn-{environment}` | CDN profile name |
| **Endpoint Name** | `podium-cdn-endpoint-{environment}` | CDN endpoint |
| **Origin Type** | Azure App Service | Source of content |
| **Origin Hostname** | `podium-frontend-{environment}.azurewebsites.net` | App Service URL |
| **Origin Path** | `/` | Root path |
| **Protocol** | HTTPS only | Secure delivery |
| **Origin Host Header** | Same as origin hostname | Forwarded host header |
| **HTTP Port** | 80 | Not used (HTTPS only) |
| **HTTPS Port** | 443 | Secure port |

### Create via Azure Portal

1. Navigate to Azure Portal
2. Create Resource > CDN
3. Select Profile:
   - Subscription: Your subscription
   - Resource Group: `podium-production-rg`
   - Name: `podium-cdn-production`
   - Pricing tier: Standard Microsoft
   - Location: Global
4. Create Endpoint:
   - Name: `podium-cdn-endpoint-production`
   - Origin type: App Service
   - Origin hostname: Select your frontend App Service
5. Review and Create

### Create via Azure CLI

```bash
# Create CDN profile
az cdn profile create \
  --resource-group podium-production-rg \
  --name podium-cdn-production \
  --sku Standard_Microsoft \
  --location global

# Create CDN endpoint
az cdn endpoint create \
  --resource-group podium-production-rg \
  --profile-name podium-cdn-production \
  --name podium-cdn-endpoint-production \
  --origin podium-frontend-production.azurewebsites.net \
  --origin-host-header podium-frontend-production.azurewebsites.net \
  --enable-compression true \
  --query-string-caching-behavior IgnoreQueryString
```

---

## Caching Rules

Caching rules optimize delivery and reduce origin load.

### Static Assets (Long Cache)

**Files:** `.js`, `.css`, `.png`, `.jpg`, `.jpeg`, `.gif`, `.svg`, `.ico`, `.woff`, `.woff2`

**Cache Duration:** 7 days (168 hours)

**Reason:** These files rarely change and have content hashes in filenames

**Configuration:**
```json
{
  "name": "StaticAssetsCaching",
  "order": 1,
  "conditions": [
    {
      "name": "UrlFileExtension",
      "parameters": {
        "operator": "Equal",
        "matchValues": ["js", "css", "png", "jpg", "jpeg", "gif", "svg", "ico", "woff", "woff2"]
      }
    }
  ],
  "actions": [
    {
      "name": "CacheExpiration",
      "parameters": {
        "cacheBehavior": "Override",
        "cacheType": "All",
        "cacheDuration": "7.00:00:00"
      }
    }
  ]
}
```

### Video Thumbnails (Medium Cache)

**Files:** Thumbnail images from blob storage

**Cache Duration:** 30 days (720 hours)

**Reason:** Thumbnails rarely change once created

**Configuration:**
```json
{
  "name": "ThumbnailCaching",
  "order": 2,
  "conditions": [
    {
      "name": "UrlPath",
      "parameters": {
        "operator": "Contains",
        "matchValues": ["/thumbnails/"]
      }
    }
  ],
  "actions": [
    {
      "name": "CacheExpiration",
      "parameters": {
        "cacheBehavior": "Override",
        "cacheType": "All",
        "cacheDuration": "30.00:00:00"
      }
    }
  ]
}
```

### HTML Pages (Short Cache)

**Files:** `.html`, `/index.html`

**Cache Duration:** 1 hour

**Reason:** May be updated during deployments

**Configuration:**
```json
{
  "name": "HTMLCaching",
  "order": 3,
  "conditions": [
    {
      "name": "UrlFileExtension",
      "parameters": {
        "operator": "Equal",
        "matchValues": ["html"]
      }
    }
  ],
  "actions": [
    {
      "name": "CacheExpiration",
      "parameters": {
        "cacheBehavior": "Override",
        "cacheType": "All",
        "cacheDuration": "01:00:00"
      }
    }
  ]
}
```

### API Responses (No Cache)

**Paths:** `/api/*`

**Cache Duration:** None

**Reason:** API responses are dynamic

**Configuration:**
```json
{
  "name": "NoCacheAPI",
  "order": 4,
  "conditions": [
    {
      "name": "UrlPath",
      "parameters": {
        "operator": "BeginsWith",
        "matchValues": ["/api/"]
      }
    }
  ],
  "actions": [
    {
      "name": "CacheExpiration",
      "parameters": {
        "cacheBehavior": "BypassCache"
      }
    }
  ]
}
```

### Configure Rules via Azure CLI

```bash
# Update endpoint with caching rules
az cdn endpoint update \
  --resource-group podium-production-rg \
  --profile-name podium-cdn-production \
  --name podium-cdn-endpoint-production \
  --set deliveryPolicy.rules='[...]'  # JSON from above
```

---

## Custom Domain Setup

### Prerequisites

- Domain name (e.g., `www.podiumapp.com`)
- Access to DNS management
- SSL certificate (managed by Azure or custom)

### Steps

#### 1. Add Custom Domain

**Via Azure Portal:**
1. CDN Endpoint > Custom domains
2. Add custom domain
3. Enter: `www.podiumapp.com`
4. Validate

**Via Azure CLI:**
```bash
az cdn custom-domain create \
  --resource-group podium-production-rg \
  --profile-name podium-cdn-production \
  --endpoint-name podium-cdn-endpoint-production \
  --name www-podiumapp-com \
  --hostname www.podiumapp.com
```

#### 2. Configure DNS

Add CNAME record in your DNS provider:

| Type | Name | Value | TTL |
|------|------|-------|-----|
| CNAME | www | podium-cdn-endpoint-production.azureedge.net | 3600 |

Wait for DNS propagation (up to 48 hours, usually 15-30 minutes).

#### 3. Enable HTTPS

**Via Azure Portal:**
1. Custom Domain > HTTPS
2. Select: CDN managed certificate (recommended)
3. Minimum TLS version: 1.2
4. Enable

**Via Azure CLI:**
```bash
az cdn custom-domain enable-https \
  --resource-group podium-production-rg \
  --profile-name podium-cdn-production \
  --endpoint-name podium-cdn-endpoint-production \
  --name www-podiumapp-com \
  --min-tls-version 1.2
```

Certificate provisioning takes 6-8 hours.

---

## SSL/TLS Configuration

### CDN Managed Certificate (Recommended)

**Advantages:**
- Automatic provisioning
- Automatic renewal
- No cost
- DigiCert certificate authority

**Limitations:**
- No wildcard support (need separate cert per subdomain)
- Limited to Standard/Premium CDN tiers

### Custom Certificate

If you need a wildcard or specific CA:

```bash
# Upload certificate to Key Vault first
az keyvault certificate import \
  --vault-name podium-kv-production \
  --name podium-ssl-cert \
  --file certificate.pfx \
  --password "cert-password"

# Link to CDN
az cdn custom-domain enable-https \
  --resource-group podium-production-rg \
  --profile-name podium-cdn-production \
  --endpoint-name podium-cdn-endpoint-production \
  --name www-podiumapp-com \
  --user-cert-group-name podium-production-rg \
  --user-cert-secret-name podium-ssl-cert \
  --user-cert-vault-name podium-kv-production
```

### HTTP to HTTPS Redirect

Force HTTPS:

```bash
az cdn endpoint rule add \
  --resource-group podium-production-rg \
  --profile-name podium-cdn-production \
  --name podium-cdn-endpoint-production \
  --order 1 \
  --rule-name HttpsRedirect \
  --match-variable RequestScheme \
  --operator Equal \
  --match-values HTTP \
  --action-name UrlRedirect \
  --redirect-protocol Https \
  --redirect-type Found
```

---

## Optimization Settings

### Compression

Enable compression for text-based files:

**Enabled for:**
- `text/plain`
- `text/html`
- `text/css`
- `application/javascript`
- `text/javascript`
- `application/json`
- `application/xml`

**Configuration:**
```bash
az cdn endpoint update \
  --resource-group podium-production-rg \
  --profile-name podium-cdn-production \
  --name podium-cdn-endpoint-production \
  --enable-compression true \
  --content-types-to-compress \
    "text/plain" \
    "text/html" \
    "text/css" \
    "application/x-javascript" \
    "text/javascript" \
    "application/javascript" \
    "application/json" \
    "application/xml"
```

### Query String Caching

**Setting:** Ignore query strings

**Reason:** Most assets don't vary by query string

```bash
az cdn endpoint update \
  --resource-group podium-production-rg \
  --profile-name podium-cdn-production \
  --name podium-cdn-endpoint-production \
  --query-string-caching-behavior IgnoreQueryString
```

**Alternative:** Use query strings for cache busting (e.g., `?v=123`)

---

## Purging CDN Cache

### When to Purge

Purge cache after:
- Deployment of new frontend version
- Critical bug fixes
- Content updates
- Configuration changes

### Purge Methods

#### Purge All Content

```bash
az cdn endpoint purge \
  --resource-group podium-production-rg \
  --profile-name podium-cdn-production \
  --name podium-cdn-endpoint-production \
  --content-paths "/*"
```

#### Purge Specific Paths

```bash
az cdn endpoint purge \
  --resource-group podium-production-rg \
  --profile-name podium-cdn-production \
  --name podium-cdn-endpoint-production \
  --content-paths \
    "/index.html" \
    "/assets/main.*.js" \
    "/assets/styles.*.css"
```

#### Purge Script

Create `cdn-purge.sh` in project root:

```bash
#!/bin/bash
# CDN Cache Purge Script

ENVIRONMENT="${1:-production}"
RESOURCE_GROUP="podium-${ENVIRONMENT}-rg"
CDN_PROFILE="podium-cdn-${ENVIRONMENT}"
CDN_ENDPOINT="podium-cdn-endpoint-${ENVIRONMENT}"

echo "Purging CDN cache for ${ENVIRONMENT}..."

az cdn endpoint purge \
  --resource-group "$RESOURCE_GROUP" \
  --profile-name "$CDN_PROFILE" \
  --name "$CDN_ENDPOINT" \
  --content-paths "/*"

echo "CDN cache purged successfully"
echo "Note: Purge propagation takes 5-10 minutes"
```

Make executable:
```bash
chmod +x cdn-purge.sh
```

Usage:
```bash
./cdn-purge.sh production
```

### Purge via Azure Portal

1. Navigate to CDN Endpoint
2. Click "Purge"
3. Enter paths:
   - `/*` for all content
   - Specific paths for selective purge
4. Click Purge

**Note:** Purge propagation takes 5-10 minutes globally.

---

## Monitoring

### Metrics to Track

| Metric | Description | Threshold | Action |
|--------|-------------|-----------|--------|
| **Cache Hit Ratio** | % of requests served from cache | > 80% | Optimize caching rules if lower |
| **Bandwidth** | Data transfer | Monitor costs | Review content optimization |
| **Request Count** | Total requests | Baseline | Track growth |
| **4XX Error Rate** | Client errors | < 5% | Investigate broken links |
| **5XX Error Rate** | Origin errors | < 1% | Check origin health |
| **Origin Latency** | Response time from origin | < 500ms | Optimize origin if higher |

### View Metrics

**Via Azure Portal:**
1. CDN Endpoint > Monitoring > Metrics
2. Select metric
3. Apply filters
4. Set time range

**Via Azure CLI:**
```bash
az monitor metrics list \
  --resource /subscriptions/{subscription}/resourceGroups/podium-production-rg/providers/Microsoft.Cdn/profiles/podium-cdn-production/endpoints/podium-cdn-endpoint-production \
  --metric "Percentage4XX" \
  --start-time 2026-01-15T00:00:00Z \
  --end-time 2026-01-15T23:59:59Z \
  --interval PT1H
```

### Diagnostic Logs

Enable diagnostic settings:

```bash
az monitor diagnostic-settings create \
  --resource /subscriptions/{subscription}/resourceGroups/podium-production-rg/providers/Microsoft.Cdn/profiles/podium-cdn-production/endpoints/podium-cdn-endpoint-production \
  --name cdn-diagnostics \
  --logs '[{"category":"CoreAnalytics","enabled":true}]' \
  --workspace /subscriptions/{subscription}/resourceGroups/podium-production-rg/providers/Microsoft.OperationalInsights/workspaces/podium-logs-production
```

---

## Best Practices

### Cache Strategy

1. **Aggressive caching for static assets** - Long TTL (7+ days)
2. **Short caching for dynamic content** - Short TTL (1 hour)
3. **No caching for APIs** - Bypass cache
4. **Use versioned filenames** - Allows long TTL without stale content

### Performance

1. **Enable compression** - Reduces bandwidth and load time
2. **Optimize images** - Use WebP format, proper sizing
3. **Minimize cache misses** - Consistent URL patterns
4. **Monitor cache hit ratio** - Target > 80%

### Security

1. **HTTPS only** - Force HTTPS redirect
2. **TLS 1.2 minimum** - Disable older protocols
3. **Secure origin** - Ensure App Service uses HTTPS
4. **Access restrictions** - Consider WAF for sensitive content

### Cost Optimization

1. **Monitor bandwidth usage** - Primary cost driver
2. **Optimize content size** - Compression, minification
3. **Review caching rules** - Maximize cache hits
4. **Consider reserved capacity** - For predictable workloads

---

## Troubleshooting

### CDN Not Serving Content

**Check:**
1. Origin is accessible (App Service running)
2. DNS propagation complete
3. Firewall allows CDN IP ranges
4. Origin returns 200 status codes

### Stale Content After Deployment

**Solution:**
1. Purge CDN cache
2. Wait 5-10 minutes for propagation
3. Use versioned asset names (e.g., `main.abc123.js`)
4. Check browser cache (Ctrl+F5)

### SSL Certificate Issues

**Check:**
1. Certificate provisioning status
2. DNS CNAME record correct
3. Wait full 8 hours for provisioning
4. Ensure no conflicting DNS records

### High Latency

**Check:**
1. Cache hit ratio (should be > 80%)
2. Origin response time
3. CDN POP locations
4. Content optimization (compression, size)

---

*Last Updated: 2026-01-15*
*Version: 1.0*
