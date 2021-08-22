using AlephVault.Unity.Support.Utils;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        using AlephVault.Unity.Binary;
        using System.Collections.Concurrent;
        using System.Threading.Tasks;

        public partial class NetworkRemoteEndpoint : NetworkEndpoint
        {
            /// <summary>
            ///   The maximum size of each individual message to be sent.
            /// </summary>
            public readonly ushort MaxMessageSize;

            // The list of queued outgoing messages. They include a
            // task that will be resolved when the message is actually
            // having been sent.
            private ConcurrentQueue<Tuple<ushort, ushort, ISerializable, TaskCompletionSource<bool>>> queuedOutgoingMessages = new ConcurrentQueue<Tuple<ushort, ushort, ISerializable, TaskCompletionSource<bool>>>();

            // The list of queued incoming messages.
            private ConcurrentQueue<Tuple<ushort, ushort, ISerializable>> queuedIncomingMessages = new ConcurrentQueue<Tuple<ushort, ushort, ISerializable>>();
        }
    }
}
