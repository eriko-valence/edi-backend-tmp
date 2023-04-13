# OVERVIEW

This EDI Azure function app receives base64 encoded content from an http request body and publishes to CCDX. It is integrated into the [Varo Mail Processor Logic Apps workflow](../logic-apps-varo-mail-processor/README.md). 

# CONFIGURATION

- [ ]  Add these application settings
  - APPINSIGHTS_INSTRUMENTATIONKEY
    - Value: `******`
  - APPLICATIONINSIGHTS_CONNECTION_STRING
    - Value: `******`
  - AzureWebJobsStorage
    - Value: `DefaultEndpointsProtocol=https;AccountName=adlsedidev;AccountKey=***`
  - CCDX_HEADERS:CE_SOURCE
    - Value: `urn://nhgh.varo`
  - CCDX_HEADERS:CE_SPECVERSION
    - Value: `1.0`
  - CCDX_HEADERS:CE_TYPE
    - Value: `org.nhgh.{0}.report.dev`
  - CCDX_HEADERS:DX_EMAIL
    - Value: `dx.edi@2to8.cc`
  - CCDX_HEADERS:DX_OWNER
    - Value: `urn://nhgh`
  - CCDX_HEADERS:DX_TOKEN
    - Value: `*******`
  - CCDX_HEADERS:MULTIPART_FORM_DATA_FILE_ENDPOINT
    - Value: `https://ix-publish.2to8.cc/publish/file`
  - EMD_TYPE
    - Value: `varo`
  
# DEPLOYMENT
- [ ]  Manual zip push
  - Download the publishing profile from the Azure portal
  - Open Visual Studio
  - Right click on function app project
  - Select 'Publish..."
  - Select the publishing profile
  - Select 'Publish' button
