# EMS Data &amp; Integration (EDI)

## Terminology

* EMS - Equipment Monitoring System
* EMD - Equipment Monitoring Device
  * Examples: USBDG, Varo
* ABST - Absolute timestamp of EMD when the file is created on the EMD filesystem
  * Source: EMD Report Metadata Filenames
	* Varo example: 20230321T141414Z_03b00274630501120363837_reports.tar.gz
	* USBDG example: 40A36BCA7463_20230412T230639Z_002200265547501820383131_reports.tar.gz
  * Notes
    * EMD timestamp source for USBDG and CFD50 is whatever it thinks is the absolute time (usually determined using cellular, but could be wifi)
	* EMD timestamp source for Varo is whatever the phone thinks is absolute time

* EMD Report Metadata Filenames
  * Examples: 
    * Varo: 20230321T141414Z_03b00274630501120363837_reports.tar.gz
    * USBDG: 40A36BCA7463_20230412T230639Z_002200265547501820383131_reports.tar.gz
  * ABST – Absolute timestamp of EMD when the file is created on the EMD filesystem
    * EMD timestamp source for USBDG and CFD50 is whatever it thinks is the absolute time (usually determined using cellular, but could be wifi)
	* EMD timestamp source for Varo is whatever the phone thinks is absolute time
  * 

## VARO

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
