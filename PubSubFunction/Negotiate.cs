using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Communication.Identity;
using Azure.Messaging.WebPubSub;

namespace PubSubFunction
{
    public class Negotiate
    {
        private static readonly OnBehalfOfHelper _oboHelper = new OnBehalfOfHelper(
            Config.TenantId,
            Config.ClientId,
            Config.ClientSecret
        );

        private static readonly TokenValidator _tokenValidator = new TokenValidator(
            Config.TenantId,
            Config.ClientId
        );

        private static readonly CommunicationIdentityClient _acsClient = new CommunicationIdentityClient(
            Config.ACSConnectionString
        );

        private static readonly WebPubSubServiceClient _pubsubClient = new WebPubSubServiceClient(
            Config.WebPubSubConnectionString,
            Config.PubSubHubName
        );

        [FunctionName("negotiate")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Starting negotiate...");

            // Check if the user provided auth details
            if (!req.Headers.TryGetValue("authorization", out var authHeaders))
            {
                return new UnauthorizedResult();
            }

            string authHeaderValue = authHeaders.FirstOrDefault();
            if(string.IsNullOrWhiteSpace(authHeaderValue) ||
                !authHeaderValue.StartsWith("bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return new UnauthorizedResult();
            }

            // Remove "bearer " from the start
            string accessToken = authHeaderValue.Substring(7);

			bool includeACS = !string.IsNullOrWhiteSpace(req.Query["includeACS"]) &&
                                bool.TryParse(req.Query["includeACS"], out bool includeACSQuery) &&
                                includeACSQuery;

            // Validate the provided token
            var tokenValidation = await _tokenValidator.ValidateAccessTokenAsync(accessToken);

            if (!tokenValidation.TokenValid)
            {
                log.LogError("Invalid token: " + tokenValidation.Message);
                return new BadRequestResult();
            }
            log.LogInformation("Validated token for " + tokenValidation.UserId);

            // Generate an ACS token if requested
            string acsToken = null;
            if (includeACS)
            {
                string teamsAccessToken = await _oboHelper.GetTokenOnBehalfOfAsync(accessToken, Config.ACSScope);

                acsToken = (await _acsClient.GetTokenForTeamsUserAsync(teamsAccessToken)).Value.Token;
            }

            // Generate the pubsub endpoint
            Uri pubsubEndpoint = await _pubsubClient.GetClientAccessUriAsync(TimeSpan.FromHours(1), tokenValidation.UserId);

            // Return the result
            return new OkObjectResult(new {
                pubsubEndpoint = pubsubEndpoint.ToString(),
                acsToken = acsToken
            });
        }
    }
}
