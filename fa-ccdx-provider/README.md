# OVERVIEW

This EMS Data & Integration (EDI) CCDX Provider Azure function publishes USBDG collected report packages in blob container `raw-ccdx-provider` to CCDX. 

# CONFIGURATION

- [ ]  Add these application settings
  - APPINSIGHTS_INSTRUMENTATIONKEY
    - Value: `******`
  - APPLICATIONINSIGHTS_CONNECTION_STRING
    - Value: `******`
  - AzureWebJobsStorage
    - Value: `DefaultEndpointsProtocol=https;AccountName=adlsedidev;AccountKey=***`
  - AZURE_STORAGE_BLOB_CONTAINER_NAME_CONFIG
    - Value: `config-edi`
  - AZURE_STORAGE_BLOB_CONTAINER_NAME_HOLDING
    - Value: `raw-ccdx-error`
  - AZURE_STORAGE_BLOB_CONTAINER_NAME_INPUT
    - Value: `raw-ccdx-provider`
  - CCDX_HTTP_MULTIPART_FORM_DATA_FILE_ENDPOINT
    - Value: `https://ix-publish.2to8.cc/publish/file`
  - CCDX_PUBLISHER_HEADER_CE_SOURCE
    - Value: `urn://nhgh.usbdg`
  - CCDX_PUBLISHER_HEADER_CE_SPECVERSION
    - Value: `1.0`
  - CCDX_PUBLISHER_HEADER_CE_TYPE
    - Value: `org.nhgh.{0}.report.dev`
  - CCDX_PUBLISHER_HEADER_DX_OWNER
    - Value: `urn://nhgh`
  - CCDX_PUBLISHER_HEADER_DX_TOKEN
    - Value: `806a0*****************`
  - CCDX_PUBLISHER_HEADER_SAMPLE_VALUES_FILENAME
    - Value: `ccdx_telemetry_provider_metadata_headers.json`
  - EMS_ERROR_CODES_FILENAME
    - Value: `edi_error_codes.json`
  - CCDX_HEADERS:CE_SOURCE
    - Value: `urn://nhgh.varo`
  
# DEPLOYMENT
- [ ]  Manual zip push
  - Download the publishing profile from the Azure portal
  - Open Visual Studio
  - Right click on function app project
  - Select 'Publish..."
  - Select the publishing profile
  - Select 'Publish' button





# CCDX ONBOARDING

- Account information: https://valencegroup.atlassian.net/browse/NHGH-670
- Data Interchange onboarding steps: https://www.cold-chain-data.com/resources/telemetry-provider
## STEPS
- Request configuration of new Telemetry provider account in Data Interchange
- Configure fa-ccdx-provider app to send data to the interchange
  - Add the following application settings provided from the Data Interchange adminstrator
    - CCDX_HTTP_MULTIPART_FORM_DATA_FILE_ENDPOINT (File Upload Endpoint)
	- CCDX_PUBLISHER_HEADER_CE_SOURCE (ce-source)
	- CCDX_PUBLISHER_HEADER_DX_OWNER (dx-owner)
	- CCDX_PUBLISHER_HEADER_DX_TOKEN (dx-token)
  - Add the following additional Data Interchange application settings
    - CCDX_PUBLISHER_HEADER_CE_TYPE (e.g., "org.nhgh.{0}.report.dev.local")
	- CCDX_PUBLISHER_HEADER_CE_SPECVERSION (e.g., "1.0")
  - Add the following blob container application settings
    - AZURE_STORAGE_BLOB_CONTAINER_NAME_INPUT (e.g., "raw-ccdx-provider")
	- AZURE_STORAGE_BLOB_CONTAINER_NAME_HOLDING (e.g., "raw-ccdx-error")
	- AZURE_STORAGE_BLOB_CONTAINER_NAME_CONFIG (e.g., "config-edi")