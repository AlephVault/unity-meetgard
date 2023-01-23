using System;
using System.Threading.Tasks;
using AlephVault.Unity.Meetgard.Authoring.Behaviours.Server;
using UnityEngine;

namespace AlephVault.Unity.Meetgard.Samples
{
    namespace Throttle
    {
        using Binary.Wrappers;
        
        [RequireComponent(typeof(ServerSideThrottler))]
        public class SampleThrottledProtocolServerSide : ProtocolServerSide<SampleThrottledProtocolDefinition>
        {
            private ServerSideThrottler throttler;
            
            protected override void Setup()
            {
                base.Setup();
                throttler = GetComponent<ServerSideThrottler>();
            }

            protected override void SetIncomingMessageHandlers()
            {
                AddIncomingMessageHandler("ClientThrottledCommand", async (proto, clientId) =>
                {
                    Debug.Log($"ClientThrottledCommand received. time={DateTime.Now}");
                });
                AddIncomingMessageHandler("ServerThrottledCommand", async (proto, clientId) =>
                {
                    await throttler.DoThrottled(clientId, async () =>
                    {
                        Debug.Log($"ServerThrottledCommand received. time={DateTime.Now}");
                    }, OnCommandThrottled);
                });
                AddIncomingMessageHandler<String>("ClientThrottledCommand2", async (proto, clientId, msg) =>
                {
                    Debug.Log($"ClientThrottledCommand2 received. text={msg} time={DateTime.Now}");
                });
                AddIncomingMessageHandler<String>("ServerThrottledCommand2", async (proto, clientId, msg) =>
                {
                    await throttler.DoThrottled(clientId, async () =>
                    {
                        Debug.Log($"ServerThrottledCommand2 received. text={msg} time={DateTime.Now}");
                    }, OnCommandThrottled);
                });
            }

            private async Task OnCommandThrottled(ulong connectionId, DateTime time, int throttlesCount)
            {
                Debug.Log($"The connection was throttled. Id={connectionId} time={time.ToString()} count={throttlesCount}");
            }

            public override async Task OnServerStarted()
            {
                throttler.Startup();
            }

            public override async Task OnConnected(ulong clientId)
            {
                throttler.TrackConnection(clientId);
            }

            public override async Task OnDisconnected(ulong clientId, Exception reason)
            {
                throttler.UntrackConnection(clientId);
            }

            public override async Task OnServerStopped(Exception e)
            {
                throttler.Teardown();
            }
        }
    }
}
