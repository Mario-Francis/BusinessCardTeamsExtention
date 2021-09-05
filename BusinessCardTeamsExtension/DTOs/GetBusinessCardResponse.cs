using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessCardTeamsExtension.DTOs
{
    public class GetBusinessCardResponse:ResponseBase
    {
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("url_without_mobile")]
        public string UrlWithoutMobile { get; set; } 
        [JsonProperty("myContacts_url")]
        public string MyContactsUrl { get; set; }

    }
}
