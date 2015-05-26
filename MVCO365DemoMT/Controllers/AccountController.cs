//----------------------------------------------------------------------------------------------
//    Copyright 2014 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//----------------------------------------------------------------------------------------------

using System;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using MVCO365Demo.Utils;

namespace MVCO365Demo.Controllers
{
    public class AccountController : Controller
    {
        public void SignIn()
        {
            if (!Request.IsAuthenticated)
            {
                HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/" }, OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
        }
        public void SignOut()
        {
            string callbackUrl = Url.Action("SignOutCallback", "Account", routeValues: null, protocol: Request.Url.Scheme);

            HttpContext.GetOwinContext().Authentication.SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                OpenIdConnectAuthenticationDefaults.AuthenticationType, CookieAuthenticationDefaults.AuthenticationType);
        }

        public ActionResult SignOutCallback()
        {
            if (Request.IsAuthenticated)
            {
                // Redirect to home page if the user is authenticated.
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        public ActionResult ConsentApp()
        {
            string strResource = Request.QueryString["resource"];
            string strRedirectController = Request.QueryString["redirect"];

            string authorizationRequest = String.Format(
                AADAppSettings.ConsentUri,
                    Uri.EscapeDataString(AADAppSettings.ClientId),
                    Uri.EscapeDataString(strResource),
                    Uri.EscapeDataString(String.Format("{0}/{1}", this.Request.Url.GetLeftPart(UriPartial.Authority).ToString(), strRedirectController))
                    );

            return new RedirectResult(authorizationRequest);
        }

        [Authorize]
        public ActionResult AdminConsentApp()
        {
            // if login has expired, redirect so that user can login
            if (ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier) == null)
            {
                RedirectToAction("Index");
            }

            string strResource = Request.QueryString["resource"];
            if (string.IsNullOrEmpty(strResource))
            {
                string signInUserId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Upn).Value;
                strResource = signInUserId.Substring(signInUserId.IndexOf('@') + 1);
                if (strResource.ToUpper() == "MICROSOFT.COM")
                {
                    if (AADAppSettings.Authority.Contains("-ppe"))
                        strResource = "msft.spoppe.com";
                    else
                        strResource = "Microsoft.SharePoint.com";
                }
                strResource = "https://" + strResource;
            }
            string strRedirectController = Request.QueryString["redirect"];
            if (string.IsNullOrEmpty(strRedirectController))
            {
                strRedirectController = "Home/Index";
            }

            string authorizationRequest = String.Format(
                AADAppSettings.AdminConsentUri,
                    Uri.EscapeDataString(AADAppSettings.ClientId),
                    Uri.EscapeDataString(strResource),
                    Uri.EscapeDataString(String.Format("{0}/{1}", this.Request.Url.GetLeftPart(UriPartial.Authority).ToString(), strRedirectController)),
                    Uri.EscapeDataString("admin_consent")
                    );

            return new RedirectResult(authorizationRequest);
        }

        public void RefreshSession()
        {
            string strRedirectController = Request.QueryString["redirect"];

            HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = String.Format("/{0}", strRedirectController) }, OpenIdConnectAuthenticationDefaults.AuthenticationType);
        }
    }
}