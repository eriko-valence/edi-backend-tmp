# OVERVIEW

This EMS Data & Integration (EDI) CCDX Consumer Azure function pulls USBDG file packages from CCDX and uploads to Azure blob storage container `raw-ccdx-consumer`. 

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
    - Value: `dx.consumer.example.valence-dev`
  - KAFKA_TRIGGER_SASL_PASSWORD
    - Value: `BXp***********`
  - KAFKA_TRIGGER_SASL_USERNAME
    - Value: `DZ24**********`
  
# DEPLOYMENT
- [ ]  Manual zip push
  - Download the publishing profile from the Azure portal
  - Open Visual Studio
  - Right click on function app project
  - Select 'Publish..."
  - Select the publishing profile
  - Select 'Publish' button

# CCDX ONBOARDING
- https://valencegroup.atlassian.net/browse/NHGH-703
- https://www.cold-chain-data.com/resources/telemetry-consumer

## Steps
- Request the creation of a new Telemetry Consumer
- Configure the fa-ccdx-consumer app to consume telemetry data from the Data Interchange
  - Update the following constant variables in Consumer.cs (provided from the Data Interchange adminstrator)
    - Broker 
	- Topic 
  - Add the following application settings (provided from the Data Interchange adminstrator)
    - KAFKA_TRIGGER_SASL_USERNAME (key)
	- KAFKA_TRIGGER_SASL_PASSWORD (secret)
	- KAFKA_GROUP_ID (e.g., "dx.consumer.edidata.valence-prod")
	  - Note: The Data Interchange adminstrator provides the group prefix (e.g., "dx.consumer.edidata.")
  - Add the following application settings (provided from the Data Interchange adminstrator) - these app settings are only used for documentation purposes (these values are currently pulled from the aforementioned constant variables)
	- KAFKA_BROKER (dev and prod use the same broker endpoint) 
	- KAFKA_TOPIC (topic) (e.g., "dx.destination.edidata")
  - Add the following blob container application settings
    - CCDX_AZURE_STORAGE_BLOB_CONTAINER_NAME (e.g., "raw-ccdx-consumer")