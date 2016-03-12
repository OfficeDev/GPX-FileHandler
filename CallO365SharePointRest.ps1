<#
	.SYNOPSIS
		Calls a given SharePoint REST endpoint given proper credentials for the tenant.

	.PARAMETER Username
	The user from which the REST APIs should be called on behalf of. 

	.PARAMETER API
	The REST API that the user wishes to call. Must be https.

	.PARAMETER HTTPVerb
	Specifies how the REST API should be called. Default is GET.

	.PARAMETER BodyFile 
	Specifies the path and name of the file that contains the body of the request.

	.EXAMPLE
		CallO365SharePointRest.ps1 -api "https://contoso.sharepoint.com/_api/lists" -username "admin@contoso.onmicrosoft.com"
		Gets data on the tenant's lists.
#>

# Declare params
Param(
	[Parameter(Mandatory=$True)]
	[String]$API,
	 
	[Parameter(Mandatory=$False)]
	[Microsoft.PowerShell.Commands.WebRequestMethod]$HTTPVerb = [Microsoft.PowerShell.Commands.WebRequestMethod]::Get,
	 
	[Parameter(Mandatory=$True)]
	[String]$Username,

 	[Parameter(Mandatory=$False)]
	[String]$BodyFile
)

# Add all the right SharePoint DLLs so we can create our account credentials later on.
Add-Type -Path "C:\Program Files\Common Files\microsoft shared\Web Server Extensions\15\ISAPI\Microsoft.SharePoint.Client.dll" 
Add-Type -Path "C:\Program Files\Common Files\microsoft shared\Web Server Extensions\15\ISAPI\Microsoft.SharePoint.Client.Runtime.dll"

# Get the user's password as a secure string.
$secPass = Read-Host -Prompt "Enter your Office 365 account password" -AsSecureString 

# Create the SharePoint Online credentials so we can call the APIs.
$creds= New-Object Microsoft.SharePoint.Client.SharePointOnlineCredentials($Username, $secPass)

# Once the credentials are assembled, create the web request to call the REST APIs.
$request = [System.Net.WebRequest]::Create($API)
$request.Headers.Add("x-forms_based_auth_accepted", "f") 
$request.Accept = "application/json;odata=verbose"
$request.Credentials = $creds 

# Set the verb (and the verb header, if needed).
if (($HTTPVerb -ne [Microsoft.PowerShell.Commands.WebRequestMethod]::Get) -and ($HTTPVerb -ne [Microsoft.PowerShell.Commands.WebRequestMethod]::Post) -and ($HTTPVerb -ne [Microsoft.PowerShell.Commands.WebRequestMethod]::Delete))
{
    # PUT, PATCH, etc. have to use a X-HTTP-Method header, 
    # with POST as the real verb.
    $request.Method = [Microsoft.PowerShell.Commands.WebRequestMethod]::Post
    $request.Headers.Add("X-HTTP-Method", $HTTPVerb)
}
else 
{
    $request.Method=$HTTPVerb 
}

# Get the request digest, if needed.
if ($HTTPVerb -ne [Microsoft.PowerShell.Commands.WebRequestMethod]::Get)
{
    # Create the contextinfo URL.
    [string[]] $splitURL = $API.Split("_")
    $domain = $splitURL[0]
    $contextInfoURL = $domain + "_api/contextinfo" 

    # Create the digest request
    $digestRequest = [System.Net.WebRequest]::Create($contextInfoURL)
    $digestRequest.Headers.Add("x-forms_based_auth_accepted", "f") 
    $digestRequest.Accept = "application/xml"
    $digestRequest.Method= [Microsoft.PowerShell.Commands.WebRequestMethod]::Post
    $digestRequest.Credentials = $creds
    $digestRequest.ContentLength = 0

    # Get the digest from the response
    $response = $digestRequest.GetResponse()
    $stream = $response.GetResponseStream()
    $readStream = New-Object System.IO.StreamReader $stream
    $responseData=$readStream.ReadToEnd()
    $namespace = @{d="http://schemas.microsoft.com/ado/2007/08/dataservices"}
    $digest = ($responseData | Select-Xml -Namespace $namespace -XPath "//d:FormDigestValue")
    
    # Add digest to header of main request.
    $request.Headers["X-RequestDigest"] = $digest.node.innerxml
}

# Add the body, if there is one. 
if ($BodyFile.Length -ne 0)
{
    $body = Get-Content $BodyFile  
    $encoding = New-Object System.Text.ASCIIEncoding
    [byte[]] $bodyByteArray = $encoding.GetBytes($body)
    $request.ContentLength = $bodyByteArray.Length
    $requestStream = $request.GetRequestStream()
    $requestStream.Write($bodyByteArray, 0, $bodyByteArray.Length)
    $request.ContentType = "application/json"
}

# Execute the response and see what happens.
$response = $request.GetResponse()
$stream = $response.GetResponseStream()
$readStream = New-Object System.IO.StreamReader $stream
$responseData=$readStream.ReadToEnd()

# Return the response as JSON
($responseData | ConvertFrom-JSON).d
