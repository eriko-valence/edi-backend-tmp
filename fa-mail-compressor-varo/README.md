# OVERVIEW

This EDI Azure function app packages and compresses Varo collected email attachments (base64 encoded strings in http request body). The compressed report package is included in the http response body as a base64 encoded string. This function app is integrated into the [Varo Mail Processor Logic Apps workflow](../logic-apps-varo-mail-processor/README.md). 

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
