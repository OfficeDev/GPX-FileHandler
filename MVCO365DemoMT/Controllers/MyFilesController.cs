using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Office365.Discovery;
using Microsoft.Office365.SharePoint.CoreServices;
using Microsoft.Office365.SharePoint.FileServices;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;
using MVCO365Demo.Models;
using MVCO365Demo.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MVCO365Demo.Controllers
{
    [Authorize]
    public class MyFilesController : Controller
    {
        // GET: MyFiles
        public async Task<ActionResult> Index()
        {
            List<MyFile> myFiles = new List<MyFile>();

            var signInUserId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            var userObjectId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            var tenantId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            
            AuthenticationContext authContext = new AuthenticationContext(string.Format("{0}/{1}", AADAppSettings.AuthorizationUri, tenantId), new NaiveSessionCache(signInUserId));

            try
            {
                DiscoveryClient discClient = new DiscoveryClient(AADAppSettings.DiscoveryServiceEndpointUri,
                    async () =>
                    {
                        var authResult = await authContext.AcquireTokenSilentAsync(AADAppSettings.DiscoveryServiceResourceId, new ClientCredential(AADAppSettings.ClientId, AADAppSettings.AppKey), new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));

                        return authResult.AccessToken;
                    });

                var dcr = await discClient.DiscoverCapabilityAsync("MyFiles");

                ViewBag.ResourceId = dcr.ServiceResourceId;

                SharePointClient spClient = new SharePointClient(dcr.ServiceEndpointUri,
                    async () =>
                    {
                        var authResult = await authContext.AcquireTokenSilentAsync(dcr.ServiceResourceId, new ClientCredential(AADAppSettings.ClientId, AADAppSettings.AppKey), new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));

                        return authResult.AccessToken;
                    });

                var filesResult = await spClient.Files.ExecuteAsync();

                do
                {
                    var files = filesResult.CurrentPage.OfType<File>();

                    foreach (var file in files)
                    {
                        myFiles.Add(new MyFile { Name = file.Name });
                    }

                    filesResult = await filesResult.GetNextPageAsync();

                } while (filesResult != null);
            }
            catch (AdalException exception)
            {
                //handle token acquisition failure
                if (exception.ErrorCode == AdalError.FailedToAcquireTokenSilently)
                {
                    authContext.TokenCache.Clear();

                    ViewBag.ErrorMessage = "AuthorizationRequired";
                }
            }

            return View(myFiles);
        }
    }
}