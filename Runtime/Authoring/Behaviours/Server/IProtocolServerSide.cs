using AlephVault.Unity.Binary;
using AlephVault.Unity.Meetgard.Protocols;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Server
    {
        /// <summary>
        ///   <para>
        ///     A contract for all the protocol server sides.
        ///     They serve the purpose of enumerating and
        ///     accesing all of the handlers.
        ///   </para>
        /// </summary>
        public interface IProtocolServerSide
        {
            /// <summary>
            ///   For a given message name, gets the tag it acquired when
            ///   it was registered. Returns null if absent.
            /// </summary>
            /// <param name="message">The name of the message to get the tag for</param>
            /// <returns>The tag (nullable)</returns>
            public ushort? GetOutgoingMessageTag(string message);

            /// <summary>
            ///   Gets the type of a particular outgoing message tag. Returns
            ///   null if the tag is not valid.
            /// </summary>
            /// <param name="tag">The tag to get the type for</param>
            /// <returns>The type for the given tag</returns>
            public Type GetOutgoingMessageType(ushort tag);

            /// <summary>
            ///   Creates a message container for an incoming client message,
            ///   with a particular inner message tag.
            /// </summary>
            /// <param name="tag">The message tag to get the container for</param>
            /// <returns>The message container</returns>
            public ISerializable NewMessageContainer(ushort tag);

            /// <summary>
            ///   Gets the handler for a given requested tag. The returned
            ///   handler already wraps an original handler also referencing
            ///   the current protocol.
            /// </summary>
            /// <param name="tag">The message tag to get the handler for</param>
            /// <returns>The message handler</returns>
            public Func<ulong, ISerializable, Task> GetIncomingMessageHandler(ushort tag);

            /// <summary>
            ///   This is a callback that gets invoked when the server starts.
            /// </summary>
            public Task OnServerStarted();

            /// <summary>
            ///   This is a callback that gets invoked when a client successfully
            ///   established a connection to this server.
            /// </summary>
            public Task OnConnected(ulong clientId);

            /// <summary>
            ///   This is a callback that gets invoked when a client is disconnected from
            ///   this server. This can happen gracefully locally, gracefully remotely. or
            ///   abnormally.
            /// </summary>
            /// <param name="reason">If not null, tells the abnormal reason of closure</param>
            public Task OnDisconnected(ulong clientId, Exception reason);

            /// <summary>
            ///   This is a callback that gets invoked when the server is stopped.
            /// </summary>
            public Task OnServerStopped(Exception e);
        }
    }
}