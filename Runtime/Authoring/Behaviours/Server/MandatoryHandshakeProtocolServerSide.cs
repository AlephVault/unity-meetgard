using AlephVault.Unity.Meetgard.Authoring.Behaviours.Server;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Protocols
    {
        namespace Simple
        {
            /// <summary>
            ///   This is the server-side implementation of the
            ///   mandatory handshake protocol. Once a connection
            ///   is established, it is accounted to be "pending".
            ///   If the connection remains pending for a long
            ///   time, it will be Timeout-kicked. The server
            ///   must determine when a handshake is successfully
            ///   completed and remove the connection from being
            ///   handshake-pending, so timeouts do not occur.
            /// </summary>
            /// <typeparam name="Definition">A subclass of <see cref="MandatoryHandshakeProtocolDefinition"/></typeparam>
            public abstract class MandatoryHandshakeProtocolServerSide<Definition> : ProtocolServerSide<Definition>
                where Definition : MandatoryHandshakeProtocolDefinition, new()
            {
                /// <summary>
                ///   The timeout to kick a connection that did
                ///   not send a handshake message appropriately.
                /// </summary>
                [SerializeField]
                private float handshakeTimeout = 5f;

                // This tracks the handshake-pending connections.
                private Coroutine handshakeTimeoutCoroutine;

                // This is a dict that will be used to track
                // the timeout of handshake pending connections.
                private ConcurrentDictionary<ulong, float> pendingHandshakeConnections = new ConcurrentDictionary<ulong, float>();

                /// <summary>
                ///   This is a sender for the Welcome message.
                /// </summary>
                private Func<ulong, Task> SendWelcome;

                /// <summary>
                ///   This is a sender for the Timeout message.
                /// </summary>
                private Func<ulong, Task> SendTimeout;
                
                /// <summary>
                ///   Typically, in this Start callback function
                ///   all the Send* shortcuts will be instantiated.
                ///   This time, also the timeout coroutine is
                ///   spawned immediately.
                /// </summary>
                protected override void Initialize()
                {
                    SendWelcome = MakeSender("Welcome");
                    SendTimeout = MakeSender("Timeout");
                    handshakeTimeoutCoroutine = StartCoroutine(HandshakeTimeoutCoroutine());
                }

                private void OnDestroy()
                {
                    if (handshakeTimeoutCoroutine != null) StopCoroutine(handshakeTimeoutCoroutine);
                    handshakeTimeoutCoroutine = null;
                }

                // Every second, it updates the handshake-pending timers.
                private IEnumerator HandshakeTimeoutCoroutine()
                {
                    while(true)
                    {
                        yield return new WaitForSeconds(1f);
                        // Yes: it triggers an async function on each frame.
                        // Checks every 1s that there are no pending connections.
                        UpdateHandshakePendingTimers(1f);
                    }
                }
                
                /// <summary>
                ///   Sets up the connection to be handhake pending.
                ///   Also greets the client.
                /// </summary>
                /// <param name="clientId">The just-connected client id</param>
                public override async Task OnConnected(ulong clientId)
                {
                    AddHandshakePending(clientId);
                    _ = SendWelcome(clientId);
                }

                /// <summary>
                ///   Removes the connection from pending handshake
                ///   and also removes the session, if any. Only one
                ///   of them will, in practice, be executed.
                /// </summary>
                /// <param name="clientId">The just-disconnected client id</param>
                /// <param name="reason">The exception which is the disconnection reason, if abrupt</param>
                public override async Task OnDisconnected(ulong clientId, Exception reason)
                {
                    RemoveHandshakePending(clientId);
                }
                
                /// <summary>
                ///   Adds a connection to the pool of pending connections.
                /// </summary>
                /// <param name="connection">The connection to add</param>
                /// <returns>Whether the connection was added or already existed</returns>
                protected bool AddHandshakePending(ulong connection)
                {
                    return pendingHandshakeConnections.TryAdd(connection, 0);
                }

                /// <summary>
                ///   Removes a connection from the pool of pending connections.
                /// </summary>
                /// <param name="connection">The connection to remove</param>
                /// <returns>Whether the connection was removed or was never there</returns>
                protected bool RemoveHandshakePending(ulong connection)
                {
                    return pendingHandshakeConnections.TryRemove(connection, out _);
                }

                // Updates all of the pending connections.
                private async void UpdateHandshakePendingTimers(float delta)
                {
                    await Exclusive(async () =>
                    {
                        foreach (var pair in pendingHandshakeConnections.ToArray())
                        {
                            pendingHandshakeConnections.TryUpdate(pair.Key, pair.Value + delta, pair.Value);
                            if (pendingHandshakeConnections.TryGetValue(pair.Key, out float value) && value >= handshakeTimeout)
                            {
                                _ = SendTimeout(pair.Key);
                            }
                        }
                    });
                }
            }
        }
    }
}