# OVERVIEW

## Azure Sql Database

### Source Control

Schema changes are authored against the EDI Azure SQL development database. CI/CD is not currently used. Instead, the Schema Compare feature in Visual Studio is manually run to update changes into this `db-edi` Github repository and pushed to production. 

### Github Updates

Use these steps tach time changes are made to the EDI Azure SQL staging environment:

* Open `nhgh-edi-backend` in Visual Studio 
* Right click on the `db-edi` project and select 'Schema Compare...' 
* Select the development database (e.g., `dbsql-nhgh-ems-dev`) as the 'Source'
* Make sure `db-edi` is set as the 'Target'
* Select the 'Options' icon
* Select 'Application-scoped' under the 'Object types' tab
* Unselect the following:
  * Permissions
  * Role membership
  * Users
* Select the objects to push to Github
* Select the 'Update' button
* Open up your source control app and push these SQL changes to this Github repository, `db-edi`

### Production Deployments

CI/CD is not currently used. To manually push SQL schema changes to production:

* Open `nhgh-edi-backend` in Visual Studio 
* Right click on the `db-edi` project and select 'Schema Compare...' 
* Make sure `db-edi` is set as the 'Source'
* Select the production database as the 'Target'
* Select the 'Options' icon
* Select 'Application-scoped' under the 'Object types' tab
* Unselect the following:
  * Permissions
  * Role membership
  * Users
* Select the objects to push to Github
* Select the 'Update' button
