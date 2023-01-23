using AlephVault.Unity.Meetgard.Authoring.Behaviours.Client;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace AlephVault.Unity.Meetgard.Samples
{
    using Binary.Wrappers;
    
    namespace Throttle
    {
        [RequireComponent(typeof(ClientSideThrottler))]
        public class SampleThrottledProtocolClientSide : ProtocolClientSide<SampleThrottledProtocolDefinition>
        {
            private Func<Task> SendClientThrottledCommand;
            private Func<Task> SendServerThrottledCommand;
            private Func<String, Task> SendClientThrottledCommand2;
            private Func<String, Task> SendServerThrottledCommand2;

            private ClientSideThrottler throttler;
            
            protected override void Setup()
            {
                base.Setup();
                throttler = GetComponent<ClientSideThrottler>();
            }
            
            protected override void Initialize()
            {
                SendClientThrottledCommand = throttler.MakeThrottledSender(MakeSender("ClientThrottledCommand"));
                SendServerThrottledCommand = MakeSender("ServerThrottledCommand");
                SendClientThrottledCommand2 = throttler.MakeThrottledSender(MakeSender<String>("ClientThrottledCommand2"));
                SendServerThrottledCommand2 = MakeSender<String>("ServerThrottledCommand2");
            }
            
            public async void ClientThrottled()
            {
                await (SendClientThrottledCommand() ?? Task.CompletedTask);
            }

            public async void ClientThrottled2(string value)
            {
                await (SendClientThrottledCommand2((String)"CustomMessage") ?? Task.CompletedTask);
            }

            public async void ServerThrottled()
            {
                await SendServerThrottledCommand();
            }

            public async void ServerThrottled2(string value)
            {
                await SendServerThrottledCommand2((String)"CustomMessage");
            }
        }
    }
}
