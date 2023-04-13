# OVERVIEW

## EDI Job Import

EDI components emit job telemetry to Azure Log Analytics Workspace (LAW) (via App Insights). Telemetry comes from Azure function (app events and traces), Azure blob (report package put events), and Azure Data Factory pipeline execution status events). This EDI Job Import function queries this telemetry from LAW and inserts into Azure SQL. 

## EDI Job Import Monitor

The EDI Job Import job (defined above) emits high level telemetry results at completion. This high level telemetry includes event counts loaded into sql, exceptions thrown while loading this telemetry, etc. The EDI Job Import Monitor loads only this high level telemetry from LAW into Azure SQL. 

## EDI Email Report

Sends daily EDI job execution results (using telemetry imported by the EDI Job Import function defined above) using SendGrid. 

# HOW TO CONFIGURE
- [ ]  Enable managed identify for function app
  - Login to Azure AD
  - Select Azure function app (e.g., "fa-maint-dev")
  - Select 'Identity'
  - Select 'On' on 'Status' toggle
- [ ]  Grant function app permission to the log analytics workspace
  - Login to Azure AD
  - Select Azure log analytics (e.g., "law-edi-dev")
  - Select 'Access control (IAM)'
  - Select '+Add'
  - Select 'Add role assignment'
  - Select role `Log Analytics Reader`
  - Select 'Next'
  - Select 'Managed identity' from 'Assign access to'
  - Select '+Select members'
  - Select subscription (e.g., "NHGH Development")
  - Select 'Managed identity' under 'Function App'
  - Select the function app name (e.g., "fa-maint-dev")
  - Select 'Select'
  - Select 'Review + assign' (may need to select this button twice)
  
# HOW TO DEPLOY
- [ ]  Manual zip push
  - Open Visual Studio
  - Right click on function app project
  - Select 'Publish..."
  - Select the publishing profile (e.g., fa-maint-dev - Zip Deploy.pubxml)
  - Select 'Publish' button
