# OVERVIEW

EDI configuration files.  

# FILES

- EMS_schema.JSON (

# UPLOAD

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
  - EMS_USBDG_METADATA_JSON_SCHEMA_FILENAME
    - Value: `USBDG_Metadata_schema.json`
  
# DEPLOYMENT
- [ ]  Manual zip push
  - Download the publishing profile from the Azure portal
  - Open Visual Studio
  - Right click on function app project
  - Select 'Publish..."
  - Select the publishing profile (e.g., fa-maint-dev - Zip Deploy.pubxml)
  - Select 'Publish' button
