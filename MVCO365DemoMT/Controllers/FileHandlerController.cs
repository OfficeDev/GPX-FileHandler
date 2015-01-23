using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security.OpenIdConnect;
using MVCO365Demo.Models;
using MVCO365Demo.Utils;

namespace MVCO365Demo.Controllers
{
    [Authorize]
    public class FileHandlerController : Controller
    {
        private GPXHelper gpxUtils = new GPXHelper();
        public static readonly string DocumentKey = "XML_DOCUMENT_KEY";

        public async Task<ActionResult> Preview()
        {
            var signInUserId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            var userObjectId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            var tenantId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            String token = null;
            AuthenticationContext authContext = new AuthenticationContext(string.Format(AADAppSettings.AuthorizationUri, tenantId), new ADALTokenCache(signInUserId));
            AuthenticationResult authResult = null;

            try
            {
                ActivationParameters parameters = this.LoadActivationParameters();
                authResult = await authContext.AcquireTokenSilentAsync(parameters.Tenant, new ClientCredential(AADAppSettings.ClientId, AADAppSettings.AppKey), new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));
                token = authResult.AccessToken;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(parameters.FileGet);
                request.Headers.Add("Authorization: bearer " + token);
                Stream responseStream = request.GetResponse().GetResponseStream();

                bool complete = gpxUtils.LoadGPXFromStream(responseStream);
                if (complete)
                {
                    ViewBag.Coordinates = gpxUtils.getPointsFromGPX();
                    return View();
                }
            }
            catch (AdalException exception)
            {
                //handle token acquisition failure
                if (exception.ErrorCode == AdalError.FailedToAcquireTokenSilently)
                {
                    authContext.TokenCache.Clear();
                    return Content(exception.Message);
                }
            }

            return Content("GPX file could not be loaded.");

        }

        public async Task<ActionResult> Open()
        {
            ActivationParameters parameters = this.LoadActivationParameters();

            string test = HttpContext.User.Identity.Name;
            await HttpContext.GetOwinContext().Authentication.AuthenticateAsync(OpenIdConnectAuthenticationDefaults.AuthenticationType);

            // Sign in to the app
            // HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/FileHandler/Open"}, OpenIdConnectAuthenticationDefaults.AuthenticationType);

            //load activation parameters and all the stuff you need to get a token
            var signInUserId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            var userObjectId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            var tenantId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            String token = null;
            AuthenticationContext authContext = new AuthenticationContext(string.Format(AADAppSettings.AuthorizationUri, tenantId), new ADALTokenCache(signInUserId));
            AuthenticationResult authResult = null;
            Session[AADAppSettings.SavedFormDataName] = parameters;

            try
            {
                //grab the token
                authResult = await authContext.AcquireTokenSilentAsync(parameters.Tenant, new ClientCredential(AADAppSettings.ClientId, AADAppSettings.AppKey), new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));
                token = authResult.AccessToken;

                //assemble request to get the file
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(parameters.FileGet.Replace("''", "'"));
                request.Headers.Add("Authorization: bearer " + token);
                Stream responseStream = request.GetResponse().GetResponseStream();

                //get the file & load it into a gpx file
                bool complete = gpxUtils.LoadGPXFromStream(responseStream);
                Session[DocumentKey] = gpxUtils;
                if (complete)
                {
                    //set the name of the user, the different coordinates to map out, and the title of the track
                    ViewBag.Name = ClaimsPrincipal.Current.FindFirst(ClaimTypes.GivenName).Value + " " + ClaimsPrincipal.Current.FindFirst(ClaimTypes.Surname).Value;
                    ViewBag.Coordinates = gpxUtils.getPointsFromGPX();
                    ViewBag.Title = gpxUtils.getTitle();
                    return View();
                }
            }
            catch (AdalException exception)
            {
                //handle token acquisition failure
                if (exception.ErrorCode == AdalError.FailedToAcquireTokenSilently)
                {
                    authContext.TokenCache.Clear();
                    return Content(exception.Message);
                }
            }
            catch (Exception exception)
            {
                authContext.TokenCache.Clear();
                return Content(exception.Message);
            }

            return Content("GPX file could not be loaded.<br/>Token: " + token + "<br/> FileGet URL: " + parameters.FileGet);
        }

        public async Task<ActionResult> Save(string newName)
        {
            //grab all the stuff you need to get a token
            var signInUserId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            var userObjectId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            var tenantId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            String token = null;
            AuthenticationContext authContext = new AuthenticationContext(string.Format(AADAppSettings.AuthorizationUri, tenantId), new ADALTokenCache(signInUserId));
            AuthenticationResult authResult = null;
            Stream fileStream = null;

            //change the name in the file
            gpxUtils = Session[DocumentKey] as GPXHelper;
            gpxUtils.setTitle(newName);

            try
            {
                //grab activation parameters (this was set in Open controller)
                ActivationParameters parameters = Session[AADAppSettings.SavedFormDataName] as ActivationParameters;
                //grab token
                authResult = await authContext.AcquireTokenSilentAsync(parameters.Tenant, new ClientCredential(AADAppSettings.ClientId, AADAppSettings.AppKey), new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));
                token = authResult.AccessToken;

                //create request to write file back to server
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(parameters.FilePut);
                request.Headers.Add("Authorization: bearer " + token);
                request.Method = "PUT";

                //write bytes to stream
                byte[] xmlByteArray = Encoding.Default.GetBytes(gpxUtils.doc.OuterXml);
                fileStream = request.GetRequestStream();
                fileStream.Write(xmlByteArray, 0, xmlByteArray.Length);

                //send file over & respond as the workload does
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                return new HttpStatusCodeResult(response.StatusCode);
            }
            catch (AdalException exception)
            {
                //handle token acquisition failure
                if (exception.ErrorCode == AdalError.FailedToAcquireTokenSilently)
                {
                    authContext.TokenCache.Clear();
                }
                return new HttpStatusCodeResult(HttpStatusCode.ExpectationFailed);
            }
            catch (WebException webException)
            {
                //something funky happened in the web response - return as needed
                HttpWebResponse response = (HttpWebResponse)webException.Response;
                return new HttpStatusCodeResult(response.StatusCode);
            }
            finally
            {
                //close the file stream
                if (fileStream != null)
                {
                    fileStream.Close();
                }
            }
        }

        private ActivationParameters LoadActivationParameters()
        {
            ActivationParameters parameters;

            FormDataCookie cookie = new FormDataCookie(AADAppSettings.SavedFormDataName);
            if (Request.Form != null && Request.Form.AllKeys.Count<string>() != 0)
            {
                // get from current request's form data
                parameters = new ActivationParameters(Request.Form);
            }
            else if (cookie.Load() && cookie.IsLoaded && cookie.FormData.AllKeys.Count<string>() > 0)
            {
                // if form data does not exist, it must be because of the sign in redirection, at the time form data is saved in the cookie 
                parameters = new ActivationParameters(cookie.FormData);
                // clear the cookie after using it
                cookie.Clear();
            }
            else
            {
                parameters = (ActivationParameters)Session[AADAppSettings.SavedFormDataName];
            }
            return parameters;
        }
    }
}