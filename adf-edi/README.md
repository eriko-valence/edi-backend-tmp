# OVERVIEW

## Azure Data Factory

### Source Control

By default, Azure Data Factory is setup to author directly against the ADF service rather than a Github repository.  This is how the EDI ADF JSON objects are authored. There are a few limitations to this approach:

* https://learn.microsoft.com/en-us/azure/data-factory/source-control 

As CI/CD is not setup for EDI ADF, the scope of this Github repository folder is to store manual backups. See "Manual Backups" below. Code deployments are also manually done. See "Manual Deployments". 

### Manual Backups

Use these steps tach time changes are made to the ADF EDI staging environment:

* Launch Azure Data Factory Studio from the Azure portal
* Select the Author icon (pencil) on the left navigation panel
* Navigate to the Orchestraiton folder
* Select the Actions button (...) for the `pl_orch_report`
* Select Download support files
* Uncompress these files and upload to this `adf-edi' github repository folder

### Manual Deployments

CI/CD is not setup for EDI ADF. To move pipeline changes from the ADF staging environment to production:

* Launch Azure Data Factory Studio from the Azure portal (production environment)
* Open the JSON representation of each ADF object by
  * Selecting the ADF object 
  * Selecting the {} link
  * Manually merging the JSON deltas from the staging environment to the production environment
