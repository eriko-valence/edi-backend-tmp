# fa-ccdx-consumer
## Onboarding
- https://valencegroup.atlassian.net/browse/NHGH-703
### Production
- Account information: https://valencegroup.atlassian.net/browse/NHGH-703
- Data Interchange onboarding steps: https://www.cold-chain-data.com/resources/telemetry-consumer
### Steps
- Request the creation of a new Telemetry Consumer
- Configure the fa-ccdx-consumer app to consume telemetry data from the Data Interchange
  - Update the following constant variables in Consumer.cs (provided from the Data Interchange adminstrator)
    - Broker 
	- Topic 
  - Add the following application settings (provided from the Data Interchange adminstrator)
    - KAFKA_TRIGGER_SASL_USERNAME (key)
	- KAFKA_TRIGGER_SASL_PASSWORD (secret)
  - Add the following application settings (provided from the Data Interchange adminstrator) - these app settings are only used for documentation purposes (these values are currently pulled from the aforementioned constant variables)
	- KAFKA_BROKER (dev and prod use the same broker endpoint) 
	- KAFKA_TOPIC (topic)
  - Add the following blob container application settings
    - CCDX_AZURE_STORAGE_BLOB_CONTAINER_NAME (e.g., "raw-ccdx-consumer")
  - Add the supported log types application settings
    - CCDX_PUBLISHER_HEADER_CE_TYPE_CFD50 (e.g., "org.nhgh.cfd50.report.dev.local")
	- CCDX_PUBLISHER_HEADER_CE_TYPE_INDIGO_V2 (e.g., "org.nhgh.indigo_v2.report.dev.local")