# EMS Data &amp; Integration (EDI)

A data platform that supports data ingestion, transformation, storage/loading, and visualization for a variety of data sets, including:

* New Horizons USB Datagrabber (USBDG) device metadata
* EMS-like loggers. Examples: New Horizons Indigo v2 logger, New Horizons Stationary Logger

## Terminology

### General

* EMS - Equipment Monitoring System
* EMD - Equipment Monitoring Device

### Timestamps

* Source: EMD Report Metadata Filenames
  * Examples:
    * Varo: 03b00274630501120363837_20230321T141414Z.json
    * USBDG: 40A36BCA7463_20230413T160744Z_report.json
  * Definitions
    * **ABST** - Absolute timestamp of EMD when the file is created on the EMD filesystem
  * Notes:
    * EMD timestamp source for USBDG and CFD50 is whatever it thinks is the absolute time (usually determined using cellular, but could be wifi)
    * EMD timestamp source for Varo is whatever the phone thinks is absolute time
* Source: USBDG Report Metadata Records Array
  * Examples
    ```
    "records": [
		{
		  "ABST": "20230413T160509Z",
		  "RELT": "P173DT10H16M12S",
		  "RTCW": "PT0S"
		}
	]
    ```
  * Definitions
    * **ABST** - Absolute timestamp determined by the EMD device at the point when a logger is mounted via USB
	* **RELT** - Represents relative time (ISO 8601 duration format) determined by the logger at the point when it is mounted by an EMD device via USB. The relative time/duration value is the time elapsed since the logger was manufactured and activated/commissioned (likely at the factory).
  * Notes
    * The ABST and RELT values from the metadata can be used as a “point in time reference” association between the two timestamps for transformation purposes if needed.
	* The logger has a 7 to 10 year battery backup for the real time clock. 
	* A USBDG EMD uses a local state file to read from to populate the USBDG metadata file. For brand new USBDG EMDs (at the first logger connection time), this local state file does not exist. In this case null values will be present in the metadata file and ABST and RELT are pulled from the SYNC file name in the cloud for transformation purposes.
  
## Varo

### Azure Functions

* [Varo CCDX Provider](fa-ccdx-provider-varo/README.md)
* [Varo CCDX Consumer](fa-ccdx-consumer-varo/README.md)
* [Varo Mail Compressor](fa-mail-compressor-varo/README.md)
* [Varo Transformer](fa-adf-transform-varo/README.md)

### Logic App Workflows

* [Varo Mail Processor](logic-apps-varo-mail-processor/README.md)

## USBDG

### Azure Functions

* [USBDG CCDX Provider](fa-ccdx-provider/README.md)
* [USBDG CCDX Consumer](fa-ccdx-consumer/README.md)
* [USBDG Transformer](fa-adf-transform-indigo-v2-varo/README.md)

## Shared

### Azure Functions

* [EDI Maintenance](fa-maint/README.md)

### Databases

* [EDI Database](db-edi/README.md)

### Libraries

* [EDI Libraries](lib-edi/README.md)

### Azure Data Factories

* [EDI ADF](adf-edi/README.md)
