using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;

// Fill in these details to match your function & application id
const string NegotiateUrl = "https://<appsvcname>.azurewebsites.net/api/negotiate";
const string TenantId = "00000000-0000-0000-0000-000000000000"; // The Tenant ID of Azure AD
const string ApplicationId = "00000000-0000-0000-0000-000000000000";
const string RedirectUri = "http://localhost";

const string Authority = "https://login.microsoftonline.com/" + TenantId;
const string Scope = "api://" + ApplicationId + "/access_as_user";

using CancellationTokenSource cts = new();
Console.CancelKeyPress += (s, e) => cts.Cancel();

// Log in to Azure AD
var aadClient = PublicClientApplicationBuilder
							.Create(ApplicationId)
							.WithAuthority(Authority)
							.WithRedirectUri(RedirectUri)
							.Build();

var aadAuth = await aadClient
						.AcquireTokenInteractive(new string[] { Scope })
						.ExecuteAsync();

var aadAccessToken = aadAuth.AccessToken;


// Negotiate the websocket connection credentials
using HttpClient hc = new();
hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", aadAccessToken);
using var resp = await hc.PostAsync(NegotiateUrl, new StringContent(string.Empty), cts.Token);
resp.EnsureSuccessStatusCode();
NegotiateResponse res = await resp.Content.ReadFromJsonAsync<NegotiateResponse>()
							?? throw new Exception("Bad negotiate");


// Connect to the websocket
using ClientWebSocket ws = new();
await ws.ConnectAsync(new Uri(res.pubsubEndpoint), cts.Token);
Console.WriteLine("Connected");

await Task.WhenAny(
	RecieveAsync(ws, cts.Token),
	SendAsync(ws, cts.Token)
);
cts.Cancel();


static async Task RecieveAsync(ClientWebSocket ws, CancellationToken ct)
{
	try
	{
		byte[] buffer = new byte[4096];
		while (!ct.IsCancellationRequested)
		{
			var res = await ws.ReceiveAsync(buffer, ct);
			if (res != null && res.Count > 0)
			{
				string message = Encoding.UTF8.GetString(buffer, 0, res.Count);
				if (!message.StartsWith("DIAL|")) Console.WriteLine(message);
			}
		}
	}
	catch (Exception e)
	{
		Console.WriteLine("Error recieving: " + e);
	}
}

static async Task SendAsync(ClientWebSocket ws, CancellationToken ct)
{
	try
	{
		while (!ct.IsCancellationRequested)
		{
			string? input = await Console.In.ReadLineAsync().WaitAsync(ct);
			if (!string.IsNullOrWhiteSpace(input))
			{
				await ws.SendAsync(Encoding.UTF8.GetBytes("DIAL|" + input), WebSocketMessageType.Text, true, ct);
			}
		}
	}
	catch (Exception e)
	{
		Console.WriteLine("Error sending: " + e);
	}
}

record NegotiateResponse(string pubsubEndpoint);