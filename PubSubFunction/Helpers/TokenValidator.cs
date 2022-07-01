using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace PubSubFunction;

public record AccessTokenResult(bool TokenValid, string UserId, string Message);
public class TokenValidator
{
    private readonly string[] _validAudiences;
    private readonly string[] _validIssuers;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _managedOpenIdConfig;
    public TokenValidator(string tenantId, string clientId)
    {
        _validAudiences = new[] { "api://" + clientId };
        _validIssuers = new[] { $"https://sts.windows.net/{tenantId}/" };
        _managedOpenIdConfig = new ConfigurationManager<OpenIdConnectConfiguration>(
                                    $"https://sts.windows.net/{tenantId}/.well-known/openid-configuration",
                                    new OpenIdConnectConfigurationRetriever(),
                                    new HttpDocumentRetriever()
                                );
    }
    public async Task<AccessTokenResult> ValidateAccessTokenAsync(string accessToken)
    {
            // This could be cached as an optimization
            var config = await _managedOpenIdConfig.GetConfigurationAsync();

            var parameteres = new TokenValidationParameters()
            {
                RequireSignedTokens = true,
                RequireAudience = true,
                RequireExpirationTime = true,
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                ValidAudiences = _validAudiences,
                ValidIssuers = _validIssuers, 
                IssuerSigningKeys = config.SigningKeys
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            try 
            {
                var claimPrincipal = tokenHandler.ValidateToken(accessToken, parameteres, out _);

                string oid = claimPrincipal.FindFirst(Config.JwtOidClaim).Value;

                return new AccessTokenResult(true, oid, string.Empty);
            }
            catch (Exception e)
            {
                return new AccessTokenResult(false, string.Empty, e.Message);
            }
        }
}