using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.WebPubSub;
using Microsoft.Azure.WebPubSub.Common;

namespace PubSubFunction;

public class Message
{
    // Very basic message event, will re-send the event to all users with the same UserId
    [FunctionName("message")]
    public static async Task Run(
        [WebPubSubTrigger(Config.PubSubHubName, WebPubSubEventType.User, "message")] UserEventRequest request,
        BinaryData data,
        WebPubSubDataType dataType,
        [WebPubSub(Hub = Config.PubSubHubName)] IAsyncCollector<WebPubSubAction> actions)
    {
        await actions.AddAsync(WebPubSubAction.CreateSendToUserAction(
                request.ConnectionContext.UserId,
                BinaryData.FromString($"!{data.ToString()}"),
                dataType));
    }
}
