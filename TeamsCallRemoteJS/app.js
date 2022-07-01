import { CallClient, CallAgent } from "@azure/communication-calling";
import { AzureCommunicationTokenCredential } from "@azure/communication-common";
import * as msal from "@azure/msal-browser";

// Replace these settings with your enviroment settings
const clientId = "00000000-0000-0000-0000-000000000000";
const tenantId = "00000000-0000-0000-0000-000000000000";
const redirectUri = "http://localhost:1234/blank.html";
const negotiateEndpoint = "https://<appsvcname>.azurewebsites.net/api/negotiate?includeACS=true";

let callAgent;

// Log into Azure AD
const msalAuth = async () => {
    const msalConfig = {
        auth: {
            clientId: clientId,
            authority: "https://login.microsoftonline.com/" + tenantId
        }
    };

    const msalInstance = new msal.PublicClientApplication(msalConfig);

    const auth = await msalInstance.loginPopup({
        redirectUri: redirectUri,
        scopes: [
            "api://" + clientId + "/access_as_user"
        ]
    });

    return auth.accessToken;
}

// Negotiate the credentials for ACS & Azure Web PubSub
const negotiateConnection = async (accessToken) => {
    let res = await fetch(negotiateEndpoint, {
        method: "POST",
        headers: {
            "authorization": "bearer " + accessToken,
        }
    })

    let data = await res.json();

    return {
        acsToken: data.acsToken,
        pubsubEndpoint: data.pubsubEndpoint
    };
}

// Connect to ACS and hook up to incoming calls
const acsConnect = async (acsToken, onCall) => {
    const callClient = new CallClient();
    const tokenCredential = new AzureCommunicationTokenCredential(acsToken);
    callAgent = await callClient.createCallAgent(tokenCredential);
    callAgent.on("incomingCall", x => {
        console.log("IncomingCall", x.incomingCall.callerInfo);
        onCall(JSON.stringify(x.incomingCall.callerInfo));
    });
    console.log("ACS Connected!");
}

// Connect to Azure Web PubSub
const pubsubConnect = async (endpoint, onMessage) => {
    let ws = new WebSocket(endpoint);
    ws.onopen = () => console.log("PubSub Connected!");
    ws.onmessage = event => onMessage(event.data);
    return ws;
}

// Callback for when a dial message is recieved
const dialNumber = message => {
    let data = message.split("!DIAL|");
    if (data.length === 2 && /^[0-9\-+]+$/.test(data[1])) {
        console.log("Dialing", data[1]);
        window.open("https://teams.microsoft.com/l/call/0/0?users=4:" + data[1], "_blank");
    }
}

// Kick off the connection process
(async () => {
    const accessToken = await msalAuth();
    const negotiate = await negotiateConnection(accessToken);
    const pubsub = await pubsubConnect(negotiate.pubsubEndpoint, dialNumber);
    acsConnect(negotiate.acsToken, x => pubsub.send("INCOMING|" + x));
})();