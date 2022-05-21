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
        ///     A mandatory handshake protocol definition defines messages
        ///     for mandatory handshakes: How to react when the server
        ///     waits for a specific handshake type from the clients. The
        ///     servers must determine when a received message was appropriate
        ///     and mark the connection as not handshake-pending anymore.
        ///   </para>
        /// </summary>
        public class MandatoryHandshakeProtocolDefinition : ProtocolDefinition
        {
            protected override void DefineMessages()
            {
                DefineServerMessage("Welcome");
                DefineServerMessage("Timeout");
            }
        }
    }
}
