# OVERVIEW

This EDI Azure function app pulls Varo collected file packages from CCDX and uploads to an Azure blob storage container (`raw-ccdx-consumer`). 

# CONFIGURATION

- [ ] Two configuration variables are hard coded:

  - const string Broker = "pkc-41973.westus2.azure.confluent.cloud:9092";
  - const string Topic = "dx.destination.example";

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
