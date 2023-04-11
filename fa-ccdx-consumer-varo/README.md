# OVERVIEW

This EMS Data & Integration (EDI) CCDX Consumer Azure function pulls file packages from CCDX and uploads to Azure blob storage container `raw-ccdx-consumer`. 

# CONFIGURATION

- [ ]  Add these application settings
  - APPINSIGHTS_INSTRUMENTATIONKEY
    - Value: `*******`
  - APPLICATIONINSIGHTS_CONNECTION_STRING
    - Value: `InstrumentationKey=******`
  - AzureWebJobsStorage
    - Value: `DefaultEndpointsProtocol=https;AccountName=saoperationallogsdev;...`
  - CCDX_AZURE_STORAGE_ACCOUNT_CONNECTION_STRING
    - Value: `DefaultEndpointsProtocol=https;AccountName=adlsedidev;...`
  - CCDX_AZURE_STORAGE_BLOB_CONTAINER_NAME
    - Value: `raw-ccdx-consumer`
  - CCDX_PUBLISHER_HEADER_CE_TYPE_CFD50
    - Value: `org.nhgh.cfd50.report.dev`
  - CCDX_PUBLISHER_HEADER_CE_TYPE_INDIGO_V2
    - Value: `org.nhgh.indigo_v2.report.dev`
  - CCDX_PUBLISHER_HEADER_CE_TYPE_USBDG
    - Value: `org.nhgh.usbdg.report.dev`
  - KAFKA_GROUP_ID
    - Value: `dx.consumer.edimailproc.dev`
  - KAFKA_TRIGGER_SASL_PASSWORD
    - Value: `*****************`
  - KAFKA_TRIGGER_SASL_USERNAME
    - Value: `************`
  - SUPPORTED_CCDX_CE_TYPE
    - Value: `org.nhgh.varo.report.dev`
  - SUPPORTED_CCDX_DX_EMAIL
    - Value: `dx.edi@2to8.cc`
  
# DEPLOYMENT
- [ ]  Manual zip push
  - Download the publishing profile from the Azure portal
  - Open Visual Studio
  - Right click on function app project
  - Select 'Publish..."
  - Select the publishing profile
  - Select 'Publish' button
