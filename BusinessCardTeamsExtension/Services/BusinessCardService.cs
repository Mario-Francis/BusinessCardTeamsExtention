using BusinessCardTeamsExtension.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BusinessCardTeamsExtension.Services
{
    public class BusinessCardService: IBusinessCardService
    {
        private readonly IConfiguration config;
        private readonly IHttpClientFactory clientFactory;
        private readonly ILogger<BusinessCardService> logger;

        public BusinessCardService(IConfiguration config, IHttpClientFactory clientFactory, ILogger<BusinessCardService> logger)
        {
            this.config = config;
            this.clientFactory = clientFactory;
            this.logger = logger;
        }

        public async Task<GetBusinessCardResponse> GetUserBusinessCard(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    throw new Exception("User ID is required");
                }
                var url = $"{config["CardAPIBaseUrl"]}/getCardLink/{userId}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                var client = clientFactory.CreateClient();
                var response = await client.SendAsync(request);

                var resContent = await response.Content.ReadAsStringAsync();

                // log request and response
                var req = new { method = "GET", url = url, headers = request.Headers, response = resContent };
                logger.LogInformation(JsonConvert.SerializeObject(req));

                if (response.IsSuccessStatusCode)
                {
                    var res = JsonConvert.DeserializeObject<GetBusinessCardResponse>(resContent);
                    if (res.IsSuccess)
                    {
                        res.Message = "SUCCESS";
                    }
                    return res;
                }
                else
                {
                    return new GetBusinessCardResponse
                    {
                        Status = "failure",
                        Message = "Something went wrong! Kindly check app logs for more details"
                    };
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return new GetBusinessCardResponse
                {
                    Status = "failure",
                    Message = $"Something went wrong! Error: {ex.Message}"
                };
            }
        }

        public async Task<GetUserIdResponse> GetUserId(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    throw new Exception("Email is required");
                }
                var url = $"{config["CardAPIBaseUrl"]}/getuserId/{email}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                var client = clientFactory.CreateClient();
                var response = await client.SendAsync(request);

                var resContent = await response.Content.ReadAsStringAsync();

                // log request and response
                var req = new { method = "GET", url = url, headers = request.Headers, response = resContent };
                logger.LogInformation(JsonConvert.SerializeObject(req));

                if (response.IsSuccessStatusCode)
                {
                    var res = JsonConvert.DeserializeObject<GetUserIdResponse>(resContent);
                    if (res.IsSuccess)
                    {
                        res.Message = "SUCCESS";
                    }
                    return res;
                }
                else
                {
                    return new GetUserIdResponse
                    {
                        Status = "failure",
                        Message = "Something went wrong! Kindly check app logs for more details"
                    };
                }
            }
            catch(Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return new GetUserIdResponse
                {
                    Status = "failure",
                    Message = $"Something went wrong! Error: {ex.Message}"
                };
            }

        }
    }
}
