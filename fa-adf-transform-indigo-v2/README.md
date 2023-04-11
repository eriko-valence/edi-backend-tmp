# OVERVIEW

This EMS Data & Integration (EDI) Azure Data Factory (ADF) transformation Azure function app transforms EMS file package content into curated output. 

# CONFIGURATION

- [ ]  Add these application settings
  - APPINSIGHTS_INSTRUMENTATIONKEY
    - Value: `*******`
  - APPLICATIONINSIGHTS_CONNECTION_STRING
    - Value: `InstrumentationKey=******`
  - AZURE_STORAGE_BLOB_CONTAINER_NAME_EMS_CONFIG
    - Value: `config-edi`
  - AZURE_STORAGE_BLOB_CONTAINER_NAME_INPUT_UNCOMPRESSED
    - Value: `staged-input`
  - AZURE_STORAGE_BLOB_CONTAINER_NAME_OUTPUT_PROCESSED
    - Value: `curated-output`
  - AZURE_STORAGE_INPUT_CONNECTION_STRING
    - Value: `DefaultEndpointsProtocol=https;AccountName=adlsedidev;AccountKey...`
  - AzureWebJobsStorage
    - Value: `DefaultEndpointsProtocol=https;AccountName=saoperationallogsdev`
  - EMS_JSON_SCHEMA_FILENAME
    - Value: `EMS_schema.json`
  
# DEPLOYMENT
- [ ]  Manual zip push
  - Download the publishing profile from the Azure portal
  - Open Visual Studio
  - Right click on function app project
  - Select 'Publish..."
  - Select the publishing profile
  - Select 'Publish' button
