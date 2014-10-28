using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MVCO365Demo.Models
{
    public class ActivationParameters
    {
        public string Client { get; set; }
        public string CultureName { get; set; }
        public string FileGet { get; set; }
        public string FilePut { get; set; }
        public string Tenant { get; set; }
        public string ViewportHeight { get; set; }
        public string ViewportWidth { get; set; }

        public ActivationParameters(NameValueCollection activationParameters)
        {
            this.Client = activationParameters["client"];
            this.CultureName = activationParameters["cultureName"];
            this.FileGet = activationParameters["fileGet"];
            this.FilePut = activationParameters["filePut"];
            this.Tenant = activationParameters["tenant"];
            this.ViewportHeight = activationParameters["viewportheight"];
            this.ViewportWidth = activationParameters["viewportwidth"];

            if (this.Tenant == null || this.FilePut == null || this.FileGet == null)
            {
                throw new Exception("Tenant and/or file locations are invalid - cannot get file.");
            }
        }

        
        public String ToString()
        {
            String str = "";
            str += "Client: " + this.Client + "<br/>";
            str += "Culture Name: " + this.CultureName + "<br/>";
            str += "File GET: " + this.FileGet + "<br/>";
            str += "File PUT: " + this.FilePut + "<br/>";
            str += "Tenant: " + this.Tenant + "<br/>";
            str += "ViewPort Height: " + this.ViewportHeight + "<br/>";
            str += "ViewPort Width: " + this.ViewportWidth + "<br/>";
            return str;
        }


    }
}