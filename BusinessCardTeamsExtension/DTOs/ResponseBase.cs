using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessCardTeamsExtension.DTOs
{
    public class ResponseBase
    {
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        public bool IsSuccess
        {
            get
            {
                return Status == "success";
            }
        }
    }
}

