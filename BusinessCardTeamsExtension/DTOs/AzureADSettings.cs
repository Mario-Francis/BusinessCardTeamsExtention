using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessCardTeamsExtension.DTOs
{
    public class AzureADSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        public string Instance { get; set; }
        public string GraphResource { get; set; }
        public string GraphResourceEndPoint { get; set; }
        public string GraphAPIEndPoint { get
            {
                return $"{GraphResource}{GraphResourceEndPoint}";
            }
        }
        public string Authority { get
            {
                return $"{Instance}{TenantId}";
            }
        }
    }
}
