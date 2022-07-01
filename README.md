# TeamsCallNotificationPubSub
A small Teams notification and dial demo using Azure Web PubSub.

_Detailed instructions to be updated!_

## Getting Started - Draft

1. Create an Azure AD Application Registration with the following settings:
   - Single tenant - This organization directory only
   - Authentication: Add a "Single-page application" platform with a redirect (for example: `http://localhost:1234/blank.html`)
   - Secrets: Create a new Client Secret and take note of the value
   - API Permissions: Add the "Azure Communication Services" "Teams.ManageCalls" delegated permission, and grant admin consent for your directory
   - Expose an API: Set an application ID that matches the Client ID of the application. Eg: `api://12300123-1234-1234-1234-123646123646`. Add a scope named `access_as_user` and allow Admins and users to consent. (Display name can be the same)
2. Create an Azure Function, and Azure Communication Service, & Azure Web PubSub resource within your Azure Subscription (template to be added)
3. Update `TeamsCallRemote\Program.cs` and `TeamsCallRemoteJS\app.js` with the values you created in Step 1. The `negotiateEndpoint` should point towards the Azure Function you created in step 2.
4. Configure the Azure Function with the following Application Settings:
   - `AADClientId` - Client ID from Step 1
   - `AADTenantId` - Tenant ID from Step 1
   - `AADClientSecret` - Client Secret from Step 1
   - `ACSConnectionString` - Connection string from your ACS resource deployed in Step 2
   - `WebPubSubConnectionString` - Connection string from your Azure Web PubSub resource deployed in Step 2
5. Publish the Azure Function within the `PubSubFunction` folder using the `func azure functionapp publish <functionAppName>` command
6. Start the `TeamsCallRemote` project using `dotnet run` or via Visual Studio. You will need to authenticate as the application starts (to find which user to connect as), but this process could be replaced.
7. Start the `TeamsCallRemoteJS` site using `npx parcel index.html` or the `run.cmd` file. _If it is your first time running this, you may first need to run `npm install`_
8. Navigate to the URL shown in the `TeamsCallRemoteJS` command prompt, you will be asked to log in (as the same user as `TeamsCallRemote`)
9. Entering a number and pressing enter on the `TeamsCallRemote` console will send a message via Web PubSub to `TeamsCallRemoteJS`, triggering a Microsoft Teams Deep Link to call the provided number.
10. If you recieve an inbound call whilst the browser is open with the `TeamsCallRemoteJS` site, a message will be sent via Web PubSub to the `TeamsCallRemote` console.