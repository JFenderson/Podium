# Monitoring Alert Integration Guide

This document describes how to integrate Azure Monitor alerts with notification services (PagerDuty, Microsoft Teams, Email).

## Table of Contents

- [Azure Action Groups](#azure-action-groups)
- [Email Integration](#email-integration)
- [Microsoft Teams Integration](#microsoft-teams-integration)
- [PagerDuty Integration](#pagerduty-integration)
- [Opsgenie Integration](#opsgenie-integration)
- [Testing Alerts](#testing-alerts)

---

## Azure Action Groups

Action Groups define how to notify people when an alert fires.

### Create Action Group

**Via Azure Portal:**
1. Navigate to Azure Portal > Monitor > Alerts > Action groups
2. Click "+ Create"
3. Fill in details:
   - Subscription: Your subscription
   - Resource group: `podium-production-rg`
   - Action group name: `podium-production-alerts`
   - Display name: `Podium Prod`
4. Add notifications and actions
5. Review + create

**Via Azure CLI:**
```bash
az monitor action-group create \
  --resource-group podium-production-rg \
  --name podium-production-alerts \
  --short-name PodiumProd
```

---

## Email Integration

### Add Email Receivers

**Via Azure Portal:**
1. Action Group > Notifications
2. Add Email/SMS/Push/Voice
3. Enter:
   - Name: `DevOps Team`
   - Email: `devops@podiumapp.com`
4. Test notification
5. Save

**Via Azure CLI:**
```bash
az monitor action-group create \
  --resource-group podium-production-rg \
  --name podium-production-alerts \
  --short-name PodiumProd \
  --email-receiver \
    name="DevOps Team" \
    email=devops@podiumapp.com \
    use-common-alert-schema=true \
  --email-receiver \
    name="On-Call Engineer" \
    email=oncall@podiumapp.com \
    use-common-alert-schema=true
```

### Email Alert Format

Alerts use Common Alert Schema:
- **Subject:** `[Azure Monitor] Alert: {AlertName} - {Severity}`
- **Body:** JSON or HTML with alert details
- **Links:** Direct link to Azure Portal

---

## Microsoft Teams Integration

### Setup Incoming Webhook

1. **Create Teams Channel:**
   - Create channel: `#podium-alerts`
   - Or use existing operations channel

2. **Add Incoming Webhook Connector:**
   - Channel > More options (⋯) > Connectors
   - Search for "Incoming Webhook"
   - Configure
   - Name: `Podium Production Alerts`
   - Upload icon (optional)
   - Create
   - Copy webhook URL

3. **Add Webhook to Action Group:**

**Via Azure Portal:**
- Action Group > Actions > Add action
- Action type: Webhook
- Name: `Teams - Podium Alerts`
- URI: `https://outlook.office.com/webhook/...` (paste webhook URL)
- Enable common alert schema: Yes
- Save

**Via Azure CLI:**
```bash
az monitor action-group create \
  --resource-group podium-production-rg \
  --name podium-production-alerts \
  --short-name PodiumProd \
  --webhook-receiver \
    name="Teams Webhook" \
    service-uri="https://outlook.office.com/webhook/..." \
    use-common-alert-schema=true
```

### Teams Message Format

Teams messages will include:
- Alert severity and name
- Resource affected
- Alert description
- Time fired
- Link to Azure Portal

---

## PagerDuty Integration

### Setup Integration

1. **Create PagerDuty Service:**
   - Login to PagerDuty
   - Services > Service Directory
   - New Service
   - Name: `Podium Production`
   - Escalation policy: Select or create
   - Integration: Azure Monitor
   - Create Service
   - Copy integration key

2. **Add to Action Group:**

**Via Azure Portal:**
- Action Group > Actions > Add action
- Action type: ITSM
- Name: `PagerDuty - Production`
- ITSM Connection: Create new
- Connection name: `PagerDuty-Podium`
- Partner: PagerDuty
- Integration Key: [Paste from step 1]
- Save

**Alternative: Use Logic App**

For advanced routing:

```bash
# Create Logic App for PagerDuty integration
az logic workflow create \
  --resource-group podium-production-rg \
  --location eastus \
  --name pagerduty-alert-integration \
  --definition @pagerduty-logic-app.json
```

**Logic App Definition (pagerduty-logic-app.json):**
```json
{
  "definition": {
    "$schema": "https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#",
    "triggers": {
      "manual": {
        "type": "Request",
        "kind": "Http"
      }
    },
    "actions": {
      "Send_to_PagerDuty": {
        "type": "Http",
        "inputs": {
          "method": "POST",
          "uri": "https://events.pagerduty.com/v2/enqueue",
          "headers": {
            "Content-Type": "application/json"
          },
          "body": {
            "routing_key": "@parameters('PagerDutyIntegrationKey')",
            "event_action": "trigger",
            "payload": {
              "summary": "@{triggerBody()?['data']?['essentials']?['alertRule']}",
              "severity": "critical",
              "source": "Azure Monitor",
              "custom_details": "@triggerBody()"
            }
          }
        }
      }
    }
  }
}
```

3. **Link Logic App to Action Group:**

```bash
az monitor action-group create \
  --resource-group podium-production-rg \
  --name podium-production-alerts \
  --short-name PodiumProd \
  --logic-app-receiver \
    name="PagerDuty via Logic App" \
    resource-id="/subscriptions/{subscription}/resourceGroups/podium-production-rg/providers/Microsoft.Logic/workflows/pagerduty-alert-integration" \
    callback-url="https://..."
```

### PagerDuty Alert Features

- **Incident creation:** Automatic
- **Escalation:** Based on PagerDuty policy
- **Acknowledgement:** Syncs with Azure
- **Resolution:** Manual or automatic

---

## Opsgenie Integration

### Setup Integration

1. **Create Opsgenie Integration:**
   - Login to Opsgenie
   - Settings > Integrations
   - Add Integration > Azure Monitor
   - Name: `Podium Production`
   - Team: Select team
   - Copy API Key

2. **Add to Action Group:**

**Via Azure Portal:**
- Action Group > Actions > Add action
- Action type: Webhook
- Name: `Opsgenie`
- URI: `https://api.opsgenie.com/v1/json/azuremonitor?apiKey={YOUR_API_KEY}`
- Enable common alert schema: Yes
- Save

**Via Azure CLI:**
```bash
az monitor action-group create \
  --resource-group podium-production-rg \
  --name podium-production-alerts \
  --short-name PodiumProd \
  --webhook-receiver \
    name="Opsgenie" \
    service-uri="https://api.opsgenie.com/v1/json/azuremonitor?apiKey={API_KEY}" \
    use-common-alert-schema=true
```

---

## Testing Alerts

### Test Action Group

**Via Azure Portal:**
1. Action Group > Test action group
2. Select sample alert type
3. Select action types to test
4. Click Test
5. Verify notifications received

**Via Azure CLI:**
```bash
az monitor action-group test-notifications create \
  --resource-group podium-production-rg \
  --action-group-name podium-production-alerts \
  --notification-type Email \
  --receivers \
    name="DevOps Team"
```

### Manual Test Alert

Create a test alert to verify full pipeline:

```bash
# Trigger a metric alert manually (requires elevated metric value)
# Or use Alert Rules > Test alert feature in portal
```

### Verification Checklist

After setup, verify:
- [ ] Test email received
- [ ] Teams message appears in channel
- [ ] PagerDuty incident created
- [ ] Opsgenie alert created
- [ ] All team members can access alerts
- [ ] Links in alerts work correctly

---

## Alert Suppression Rules

To avoid alert fatigue during maintenance:

```bash
az monitor alert-processing-rule create \
  --resource-group podium-production-rg \
  --name maintenance-suppression \
  --enabled true \
  --scopes /subscriptions/{subscription}/resourceGroups/podium-production-rg \
  --action-groups-to-remove podium-production-alerts \
  --schedule-recurrence-type weekly \
  --schedule-recurrence-start-time "2026-01-20T02:00:00" \
  --schedule-recurrence-end-time "2026-01-20T06:00:00" \
  --schedule-time-zone "Eastern Standard Time"
```

---

## Best Practices

1. **Use Common Alert Schema** - Consistent format across integrations
2. **Set Severity Appropriately** - Not everything is critical
3. **Test Regularly** - Monthly verification of all integrations
4. **Document On-Call Procedures** - Clear escalation path
5. **Review Alert Frequency** - Tune to avoid alert fatigue
6. **Use Suppression During Maintenance** - Avoid false alerts
7. **Monitor Integration Health** - Ensure webhooks are reachable

---

*Last Updated: 2026-01-15*
*Version: 1.0*
