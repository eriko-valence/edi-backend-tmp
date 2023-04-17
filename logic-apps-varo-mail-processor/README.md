# Varo DX Mail Processor

This mail processor moves Varo collected files from a gmail inbox to CCDX

# Overview

## Azure Functions

This project contains two Azure function apps

* [varo mail compressor](../fa-mail-compressor-varo/README.md)
  * Scope:
	* Builds and compresses tarball containing Varo collected files (gmail attachments)
	* Returns base64 encoded tarball (report package) in http response body
* [varo ccdx provider](../fa-ccdx-provider-varo/README.md)
  * Scope:
	* Receives base64 encoded tarball (report package) in http request body
	* Publishes report package to CCDX using the data streaming endpoint

## Logic Apps Solutions

This Logic Apps solution moves Varo collected log files from Gmail to CCDX using the two Azure Functions defined above

* [logic-apps-varo-mail-processor](README.md)
  * Scope:
    * New Gmail message (Varo generated report) triggers this solution
	* Varo collected report files (gmail attachments) are packaged into a compressed tarball
	* Receives base64 encoded tarball (report package) in http request body
	* Publishes report package tarball to CCDX using the data streaming endpoint

# Build Azure Resources

## Create CCDX Provider Azure Function app (.NET)
  * Login to the Azure portal
  * Create a new Azure Function app (ccdx provider)
    * Function app name: `fa-ccdx-provider-varo-dev`
    * Runtime stack: `.NET`
    * Version: `6`
    * Region: `US West 2`
    * Operating System: `Windows`
    * App Service Plan: `asp-edi-dev (B1: 1)`
	
## Create Mail Compressor Azure Function app (.NET)
  * Login to the Azure portal
  * Create a new Azure Function app (ccdx provider)
    * Function app name: `fa-mail-compressor-varo-dev`
    * Runtime stack: `.NET`
    * Version: `6`
    * Region: `US West 2`
    * Operating System: `Windows`
    * App Service Plan: `asp-edi-dev (B1: 1)`

## Create Azure Logic Apps Solution
  * Login to the Azure portal
  * Create a new Azure Logics app
    * Logic app name: `la-dx-varo-mail-proc-dev`
    * Region: `US West 2`
    * Plan type: `Consumption`
	* Enable log analytics: Yes
    * Log App Name: `law-edi-dev`
    * Zone redundancy: `Disabled`
  * Create the Gmail connection ([Use default shared application](#create-the-gmail-connection-use-default-shared-application))

# Deploy Code to Azure Resources

## Azure Functions

For each Azure Function... 

- [ ]  Manual zip push
  - Download the publishing profile from the Azure portal
  - Open Visual Studio
  - Right click on function app project
  - Select 'Publish..."
  - Select the publishing profile
  - Select 'Publish' button
  
## Azure Logic Apps Workflow

Note: This section needs to be ironed out. 

  * Login to the Azure portal
  * Select the newly created Azure Logics app
  * Select Logic app code view
  * Copy in the json from logic_app_code.json

# Gmail Connection Types

## Create the Gmail connection (Use default shared application)

Important: This connection type ONLY works with Gmail workspace accounts. Withou it, the Gmail connection cannot be linked Logic App Azure Function operations. 

  * Configure Gmail connection
  * Connection name: `gmail`
  * Authentication type: `Use default shared application`
  * Select the Gmail workspace account to login. 
  * Give Azure AppService Logic Apps permissions to 'Read, compose, send, and permanently delete all your email from Gmail'

## Create the Gmail connection (Bring your own application)

Note: This is not recommended as it requires a Google security review. Without the Google security review, the Gmail refresh tokens expires in seven days. 

  * Configure Gmail connection
  * Connection name: `gmail`
  * Authentication type: `Bring your own application`
    * Client ID: (see "Create an OAuth Client Application in Google")
    * Client Secret: (see "Create an OAuth Client Application in Google")
  * Select Sign In
    * Select Continue at the Google hasn't verified your app screen
    * Select the Gmail permissions box
  * Login to the Azure portal
  * Select the newly created Azure Logics app
  * Select Logic app designer
  * Select Blank Logic App template
  * Search for the Gmail connector
  * Select Gmail
  * Select the When new email arrives trigger
  * Select INBOX label
  * Select Yes for Include Attachments
  * Configure check interval to be 15 seconds

  * Note: The new Gmail connection will show up under API Connections.

# Google Oauth Client App Setup

Note: This step is ONLY required if using the [Bring your own application](#create-the-gmail-connection-use-default-shared-application) Gmail connection type

## Create an OAuth Client Application in Google

* Login to the Google Cloud Platform (https://console.cloud.google.com/)
* Create a new project
  * Name: `DX Mail Processor - Dev`
* Select the Dashboard link
* Select Go to APIs overview
* Select Credentials
* Select Configure Consent Screen
  * User Type: `External`
  * App Name: `DX Mail Processor - Dev`
  * Authorized domain: `azure-apim.net`
  * Select Add or Remove Scopes
    * Add this scope under Manually add scopes: `https://mail.google.com/`
      * Select ADD TO TABLE
      * Select UPDATE
      * Select SAVE AND CONTINUE
  * Add the applicable test users
  * Select BACK TO DASHBOARD from the Summary screen
* Select Credentials under APIs and Services
  * Select +CREATE CREDENTIALS
    * Select OAuth client ID
      * Application type: `Web application`
      * Name: `Web Client - DX Mail Processor - Dev`
      * Authorized redirect URIs: `https://global.consent.azure-apim.net/redirect/gmail`
      * Select CREATE
* Select Enabled APIs and Servivces under APIs and Services
  * Select +ENABLED APIS AND SERVICES
    * Search for Gmail API
    * Select the Gmail API
    * Select Enable

## Gmail Connector Errors

  * "Failed to save logic app logic-app-ccdx-mail-processor. The operation on workflow 'logic-app-ccdx-mail-processor' cannot be completed because it contains operations 'Function' which are not compatible with the Gmail connector. Please see https://aka.ms/la-gmaildocs for more information."
    * Cause: The Logic Apps Gmail connector uses the authentication type `Use default shared application`
    * Resolution: Use the `Bring your own application` authentication type. This requires creating an Google OAuth client app (see above)
  * "azure-apim.net has not completed the Google verification process. The app is currently being tested, and can only be accessed by developer-approved testers. If you think you should have access, contact the developer.
If you are a developer of azure-apim.net, see error details.
Error 403: access_denied"
    * Cause: tester gmail account is not approved
    * Resolution: 
	  * 1.) add tester gmail account (see "Create an OAuth Client Application in Google" above) OR
	  * 2.) use a Gmail workspace account (this is the preferred option)
  * "Please check your account info and/or permissions and try again. Details: Gmail API has not been used in project 616947808018 before or it is disabled. Enable it by visiting https://console.developers.google.com/apis/api/gmail.googleapis.com/overview?project=616947808018 then retry. If you enabled this API recently, wait a few minutes for the action to propagate to our systems and retry. clientRequestId: f8ad9b3e-6002-4698-903f-e60d448aa15c More diagnostic information: x-ms-client-request-id is '8638DD25-097F-4820-AB5A-BEAD762D8D5C'."
    * Cause: Gmail API not enabled
    * Resolution: Enable Gmail API (see "Create an OAuth Client Application in Google" above)
  * "Error 400: redirect_uri_mismatch. You can't sign in to this app because it doesn't comply with Google's Oauth 2.0 policy. If you're the app developer, register the redirect URI in the Google Cloud Console. Request details: redirect_uri=https://global.consent.azure-apim.net/redirect/gmail"
    * Cause: Missing redirect URI
    * Resolution: Add the redirect URI (see "Create an OAuth Client Application in Google" above)











# OVERVIEW

EDI Varo mail processing logic apps solution

# FILES

- logic_app_code.JSON

# DEPLOYMENT

- Login to the Azure portal
- Navigate to 'Logic Apps'
- Select `la-dx-varo-mail-proc-dev`
- 