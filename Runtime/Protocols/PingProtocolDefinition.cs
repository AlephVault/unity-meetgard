using AlephVault.Unity.Binary;
using System;
using System.Linq;
using System.Collections.Generic;
using AlephVault.Unity.Meetgard.Types;

namespace AlephVault.Unity.Meetgard
{
    namespace Protocols
    {
        /// <summary>
        ///   <para>
        ///     A ping-pong protocol to ensure connections are not idle
        ///     for a long time (e.g. to be considered as disconnected).
        ///   </para>
        /// </summary>
        public class PingProtocolDefinition : ProtocolDefinition
        {
            protected override void DefineMessages()
            {
                DefineServerMessage("Ping");
                DefineServerMessage("Timeout");
                DefineClientMessage("Pong");
            }
        }
    }
}
