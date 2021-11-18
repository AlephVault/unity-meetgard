using AlephVault.Unity.Support.Utils;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        using AlephVault.Unity.Binary;
        using System.Threading.Tasks;

        /// <summary>
        ///   <para>
        ///     A network endpoint serves for remote, non-host,
        ///     connections.
        ///   </para>
        ///   <para>
        ///     Endpoints can be told to be closed, and manage the
        ///     send and arrival of data. Sending the data can be
        ///     done in a buffered way (via "train buffers"). Most
        ///     of these operations are asynchronous in a way or
        ///     another, and event-driven. The asynchronous calls
        ///     are synchronized into the main Unity thread, however,
        ///     via the default async execution manager.
        ///   </para>
        /// </summary>
        public partial class NetworkRemoteEndpoint : NetworkEndpoint
        {
            // Whether to debug or not using XDebug.
            private bool debug = false;

            // Keeps a track of all the sockets that were used to
            // instantiate an endpoint. This does not guarantee
            // preventing the same socket to be used in a different
            // king of architecture.
            private static HashSet<TcpClient> endpointSocketsInUse = new HashSet<TcpClient>();

            // The socket, created in our life-cycle.
            private TcpClient remoteSocket = null;

            // The write timeout.
            private int remoteSocketWriteTimeout;

            // On each arriving message, this function will be invoked to get
            // the get an object of the appropriate type to deserialize the
            // message content into.
            private Func<ushort, ushort, ISerializable> protocolMessageFactory = null;

            public NetworkRemoteEndpoint(
                TcpClient endpointSocket, Func<ushort, ushort, ISerializable> protocolMessageFactory,
                Func<Task> onConnected, Func<ushort, ushort, ISerializable, Task> onArrival, Func<System.Exception, Task> onDisconnected,
                ushort maxMessageSize = 1024, float idleSleepTime = 0.01f, float writeTimeout = 15f
            ) {
                remoteSocketWriteTimeout = (int)(writeTimeout * 1000);
                if (endpointSocket == null || !endpointSocket.Connected || endpointSocketsInUse.Contains(endpointSocket))
                {
                    // This, however, does not prevent or detect the socket being used in different places.
                    // Ensure this socket is used only once, by your own means.
                    throw new ArgumentException("A unique connected socket must be passed to the endpoint construction");
                }
                if (onConnected.GetInvocationList().Length != 1)
                {
                    throw new ArgumentException("Only one handler for the onConnected event is allowed");
                }
                onConnectionStart += onConnected;
                if (onArrival.GetInvocationList().Length != 1)
                {
                    throw new ArgumentException("Only one handler for the onArrival event is allowed");
                }
                onMessage += onArrival;
                if (onDisconnected.GetInvocationList().Length != 1)
                {
                    throw new ArgumentException("Only one handler for the onDisconnected event is allowed");
                }
                onConnectionEnd += onDisconnected;

                // Prepare values related to the internal buffering.
                MaxMessageSize = Values.Clamp(512, maxMessageSize, 6144);
                IdleSleepTime = Values.Clamp(0.005f, idleSleepTime, 0.5f);

                // Prepare the settings for incoming messages.
                if (protocolMessageFactory.GetInvocationList().Length != 1)
                {
                    throw new ArgumentException("Only one handler for the getMessage callback is allowed");
                }
                this.protocolMessageFactory += protocolMessageFactory;

                // Mark the socket as in use, and also start the lifecycle.
                remoteSocket = endpointSocket;
                endpointSocketsInUse.Add(endpointSocket);
                StartLifeCycle();
            }

            ~NetworkRemoteEndpoint()
            {
                endpointSocketsInUse.Remove(remoteSocket);
            }

            // Related to connection's status.

            /// <summary>
            ///   Tells whether the life-cycle is active or not. While Active, another
            ///   life-cycle (e.g. a call to <see cref="Connect(IPAddress, int)"/> or
            ///   <see cref="Connect(string, int)"/>) cannot be done.
            /// </summary>
            public override bool IsActive { get { return lifeCycle != null && lifeCycle.IsAlive; } }

            /// <summary>
            ///   Tells whether the underlying socket is instantiated and connected.
            /// </summary>
            public override bool IsConnected { get { return remoteSocket.Connected; } }

            // Related to the available actions over a socket.

            /// <summary>
            ///   Closes the active connection, if any. This, actually,
            ///   tells the thread to close the connection.
            /// </summary>
            public override void Close()
            {
                if (!IsConnected)
                {
                    throw new InvalidOperationException("The socket is not connected - It cannot be closed");
                }

                remoteSocket.Close();
            }

            /// <summary>
            ///   Queues the message to be sent.
            /// </summary>
            /// <param name="protocolId">The id of protocol for this message</param>
            /// <param name="messageTag">The tag of the message being sent</param>
            /// <param name="data">The object to serialize and send</param>
            /// <returns>The task that can be waited for: when the message is done</returns>
            protected override Task DoSend(ushort protocolId, ushort messageTag, ISerializable data)
            {
                XDebug debugger = new XDebug("Meetgard", this, "DoSend(...)", debug);
                debugger.Start();

                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                debugger.Info($"Queuing the message to be sent in the endpoint lifecycle: ({protocolId}, {messageTag}, {data})");
                queuedOutgoingMessages.Enqueue(new Tuple<ushort, ushort, ISerializable, TaskCompletionSource<bool>>(protocolId, messageTag, data, tcs));
                Task result = tcs.Task;

                debugger.End();
                return result;
            }
        }
    }
}
