using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace PubSubFunction;

public class OnBehalfOfHelper
{
    private readonly IConfidentialClientApplication _clientApp;
    public OnBehalfOfHelper(string tenantId, string clientId, string clientSecret)
    {
        _clientApp = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithAuthority("https://login.microsoftonline.com/" + tenantId)
                .WithClientSecret(clientSecret)
                .Build();
    }
    public async Task<string> GetTokenOnBehalfOfAsync(string accessToken, string scope)
    {
        var res = await _clientApp.AcquireTokenOnBehalfOf(
                    new List<string>
                    {
                        scope
                    }, new UserAssertion(accessToken)).ExecuteAsync();
        return res.AccessToken;
    }
}