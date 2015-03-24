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
        public string ResourceId { get; set; }
        public string FileId { get; set; }

        public ActivationParameters(NameValueCollection activationParameters)
        {
            this.Client = activationParameters["client"];
            this.CultureName = activationParameters["cultureName"];
            this.FileGet = activationParameters["fileGet"];
            this.FilePut = activationParameters["filePut"];
            this.FileId = activationParameters["fileId"];
            this.ResourceId = activationParameters["resourceId"];

            if (string.IsNullOrEmpty(this.ResourceId) || string.IsNullOrEmpty(this.FilePut) || string.IsNullOrEmpty(this.FileGet))
            {
                throw new Exception("ResourceId and/or file locations are invalid - cannot get file.");
            }
        }


        public override String ToString()
        {
            String str = "";
            str += "Client: " + this.Client + "<br/>";
            str += "Culture Name: " + this.CultureName + "<br/>";
            str += "File GET: " + this.FileGet + "<br/>";
            str += "File PUT: " + this.FilePut + "<br/>";
            str += "ReourceId: " + this.ResourceId + "<br/>";
            str += "FileId: " + this.FileId + "<br/>";
            return str;
        }
    }
}