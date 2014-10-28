GPX-FileHandler
===============
This is a version of [a O365 multi-tenant webapp](https://github.com/OfficeDev/O365-WebApp-MultiTenant) modified to work as a File Handler extension for GPX files.

To run this on your machine after cloning:

1. Open the project in Visual Studio 2013. You must have the most recent version of the update (the October VSIX) in order for the app to function properly.
2. In the web.config & manifest.json, replace the app client ID and secret placeholder values.
3. Right Click -> Publish the app to the web.
4. Call the [O365 SP REST Powershell script](https://github.com/OfficeDev/call-spo-rest) to install your app using the File Handler install APIs:

```
Add-Type -Path "C:\Program Files\Common Files\microsoft shared\Web Server Extensions\15\ISAPI\Microsoft.SharePoint.Client.dll" 
Add-Type -Path "C:\Program Files\Common Files\microsoft shared\Web Server Extensions\15\ISAPI\Microsoft.SharePoint.Client.Runtime.dll"
./CallO365SharePointRest.ps1 -API "https://[tenant].sharepoint.com/_api/apps/" -Username [admin email] -HTTPVerb POST -BodyFile ".\MVCO365DemoMT\install_manifest.json" 
```
