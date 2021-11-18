using AlephVault.Unity.Binary;
using AlephVault.Unity.Support.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        /// <summary>
        ///   <para>
        ///     A network endpoint is an endpoint which can send
        ///     and receive data, and have their own concepts
        ///     of connected/active and events for data arrival.
        ///   </para>
        ///   <para>
        ///     There are two types of network endpoints: standard
        ///     (remote) ones, and host (local) ones. While the
        ///     inmense majority of the endpoints are remote, one
        ///     local endpoint may exist in the server (and WILL
        ///     exist in the server for host/symmetric games).
        ///   </para>
        ///   <para>
        ///     Implementation details: A network endpoint must
        ///     notify, somehow, about the following events:
        ///     connected, disconnected, and message arrival.
        ///   </para>
        /// </summary>
        public abstract class NetworkEndpoint
        {
            // Whether to debug or not using XDebug.
            private bool debug = false;

            /// <summary>
            ///   Tells whether the endpoint is active (i.e.
            ///   running some sort of life-cycle).
            /// </summary>
            public abstract bool IsActive { get; }

            /// <summary>
            ///   Tells whether the endpoint is connected (i.e.
            ///   its socket is connected).
            /// </summary>
            public abstract bool IsConnected { get; }

            /// <summary>
            ///   Closes the connection.
            /// </summary>
            public abstract void Close();

            /// <summary>
            ///   Sends a message, specifying metadata for
            ///   it (protocol id and message tag) and also
            ///   the data to serialize.
            /// </summary>
            /// <param name="protocolId">The id of protocol for this message</param>
            /// <param name="messageTag">The tag of the message being sent</param>
            /// <param name="data">The object to serialize and send</param>
            public Task Send(ushort protocolId, ushort messageTag, ISerializable data)
            {
                XDebug debugger = new XDebug("Meetgard", this, "Send(...)", debug);
                debugger.Start();

                debugger.Info("Checking parameters and status");
                if (!IsConnected)
                {
                    throw new InvalidOperationException("The socket is not connected - No data can be sent");
                }

                if (data == null)
                {
                    throw new ArgumentNullException("data");
                }

                debugger.Info($"Doing the actual send (data: {data})");
                Task result = DoSend(protocolId, messageTag, data);
                debugger.End();
                return result;
            }

            /// <summary>
            ///   Queues the message to be sent.
            /// </summary>
            /// <param name="protocolId">The id of protocol for this message</param>
            /// <param name="messageTag">The tag of the message being sent</param>
            /// <param name="data">The object to serialize and send</param>
            protected abstract Task DoSend(ushort protocolId, ushort messageTag, ISerializable data);
        }
    }
}
