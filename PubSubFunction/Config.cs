using System;

namespace PubSubFunction;

internal static class Config
{
    public const string PubSubHubName = "CallNotifications";
    public const string ACSScope = "https://auth.msft.communication.azure.com/Teams.ManageCalls";
    public const string JwtOidClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    public static string TenantId { get; } = Environment.GetEnvironmentVariable("AADTenantId");
    public static string ClientId { get; } = Environment.GetEnvironmentVariable("AADClientId");
    public static string ClientSecret { get; } = Environment.GetEnvironmentVariable("AADClientSecret");
    public static string ACSConnectionString { get; } = Environment.GetEnvironmentVariable("ACSConnectionString");
    public static string WebPubSubConnectionString { get; } = Environment.GetEnvironmentVariable("WebPubSubConnectionString");
    public static string Authority { get; } = Environment.GetEnvironmentVariable("WebPubSubConnectionString");
}