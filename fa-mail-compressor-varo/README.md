# OVERVIEW

This EMS Data & Integration (EDI) Azure function compresses Varo collected email report attachments and returns in a http response body as base64 encoded string.

# CONFIGURATION

- [ ]  Add these application settings
  - APPINSIGHTS_INSTRUMENTATIONKEY
    - Value: `******`
  - APPLICATIONINSIGHTS_CONNECTION_STRING
    - Value: `******`
  - AzureWebJobsStorage
    - Value: `DefaultEndpointsProtocol=https;AccountName=adlsedidev;AccountKey=****`
  
# DEPLOYMENT
- [ ]  Manual zip push
  - Download the publishing profile from the Azure portal
  - Open Visual Studio
  - Right click on function app project
  - Select 'Publish..."
  - Select the publishing profile
  - Select 'Publish' button
