using AlephVault.Unity.Meetgard.Protocols;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlephVault.Unity.Meetgard.Samples
{
    namespace Chat
    {
        public class ChatProtocolDefinition : ProtocolDefinition
        {
            protected override void DefineMessages()
            {
                DefineServerMessage("WhoAreYou");
                DefineClientMessage<Nickname>("Nickname");
                DefineServerMessage("Nickname:OK");
                DefineServerMessage("Nickname:Duplicated");
                DefineServerMessage("Nickname:AlreadyIntroduced");
                DefineServerMessage<Nickname>("Nickname:Joined");
                DefineServerMessage<Nickname>("Nickname:Left");
                DefineClientMessage<Line>("Say");
                DefineServerMessage<Said>("Say:Said");
                DefineServerMessage("Say:OK");
                DefineServerMessage("Say:NotIntroduced");
                DefineServerMessage<Echo>("Ping");
                DefineClientMessage<Echo>("Ping:Pong");
                DefineServerMessage("Ping:Timeout");
            }
        }
    }
}
