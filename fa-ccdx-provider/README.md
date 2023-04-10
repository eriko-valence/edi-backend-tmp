# fa-ccdx-provider
## Onboarding
### Production
- Account information: https://valencegroup.atlassian.net/browse/NHGH-670
- Data Interchange onboarding steps: https://www.cold-chain-data.com/resources/telemetry-provider
### Steps
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