using BusinessCardTeamsExtension.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace BusinessCardTeamsExtension.Services
{
    public class MicrosoftGraphService: IMicrosoftGraphService
    {
        private readonly AzureADSettings azureADSettings;
        public MicrosoftGraphService(IOptions<AzureADSettings> azureADSettings)
        {
            this.azureADSettings = azureADSettings.Value;
        }

        private  async Task<GraphServiceClient> GetGraphServiceClient()
        {
            // Get Access Token and Microsoft Graph Client using access token and microsoft graph v1.0 endpoint
            var delegateAuthProvider = await GetAuthProvider();
            // Initializing the GraphServiceClient
            var graphClient = new GraphServiceClient(azureADSettings.GraphAPIEndPoint, delegateAuthProvider);

            return graphClient;
        }


        private async Task<IAuthenticationProvider> GetAuthProvider()
        {
            AuthenticationContext authenticationContext = new AuthenticationContext(azureADSettings.Authority);
            ClientCredential clientCred = new ClientCredential(azureADSettings.ClientId, azureADSettings.ClientSecret);

            // ADAL includes an in memory cache, so this call will only send a message to the server if the cached token is expired.
            AuthenticationResult authenticationResult = await authenticationContext.AcquireTokenAsync(azureADSettings.GraphResource, clientCred);
            var token = authenticationResult.AccessToken;

            var delegateAuthProvider = new DelegateAuthenticationProvider((requestMessage) =>
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token.ToString());
                return Task.FromResult(0);
            });

            return delegateAuthProvider;
        }

        // public methods
        public async Task<ADUser> GetUser(string userId)
        {
            var client = await GetGraphServiceClient();
            var user = await client.Users[userId].Request().GetAsync();
            return new ADUser
            {
                Id = userId,
                Email = user.Mail,
                GivenName = user.GivenName,
                Surname = user.Surname
            };
        }
    }
}
