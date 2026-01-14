# ADR-014: Cost Alerts at 50%, 83%, 100%

**Date**: January 13, 2026
**Status**: Accepted

## Context

Need to monitor Azure costs to stay under $150/month budget. Options:

1. **Azure Cost Management** budgets with email alerts
2. **Azure Monitor** action groups with Logic Apps
3. **Third-party tools** (CloudHealth, CloudCheckr)
4. **Manual checks** (Azure Portal daily review)

## Decision

Use Azure Cost Management budgets with tiered email alerts at $75, $125, $150/month thresholds (50%, 83%, 100% of budget).

## Consequences

**Pros**:

- **Free**: Built into Azure
- **Proactive**: Alerts before exceeding budget
- **Multiple warning levels**: 50%, 83%, 100% provide escalating awareness
- **Configurable**: Can adjust thresholds anytime
- **Integrated**: Same portal as other Azure resources
- **Bicep deployable**: Can define budgets in infrastructure code

**Cons**:

- Alerts are reactive (can't automatically shut down resources)
- 8-24 hour delay in cost reporting
- Email fatigue if costs spike repeatedly

**Alert Configuration**:

- **50% ($75)**: Info alert, review spending patterns
- **83% ($125)**: Warning alert, investigate high costs
- **100% ($150)**: Critical alert, immediate action required

**Action Items on Alerts**:

1. Check Azure Cost Analysis for top consumers
2. Review Application Insights for unusual traffic
3. Scale down App Service tier if needed (B1 → Free)
4. Pause PostgreSQL database during low-usage periods

**Future Enhancement**:

- Consider adding Logic App (~$0.50/month) in Phase 7+ to automatically deallocate resources if projected costs exceed $150/month
- Requires manual approval to restart services
- Provides safety net against runaway costs
