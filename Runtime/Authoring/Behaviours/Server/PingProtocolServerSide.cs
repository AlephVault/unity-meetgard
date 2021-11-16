using AlephVault.Unity.Meetgard.Protocols;
using AlephVault.Unity.Support.Utils;
using AlephVault.Unity.Support.Authoring.Behaviours;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace AlephVault.Unity.Meetgard
{
    namespace Authoring
    {
        namespace Behaviours
        {
            namespace Server
            {
                /// <summary>
                ///   Server-side implementation for the "ping" protocol.
                ///   This one tracks every connection that did not send
                ///   a message for so long, and sends a -probing- "ping"
                ///   message. After sending the message, it initiates a
                ///   countdown for the client to reply that message with
                ///   a "pong" message.
                /// </summary>
                public class PingProtocolServerSide : ProtocolServerSide<PingProtocolDefinition>
                {
                    /// <summary>
                    ///   The time to wait since the last pong message -or connection start-
                    ///   to wait until there is need to send a ping message.
                    /// </summary>
                    [SerializeField]
                    private float trustTime = 50f;

                    /// <summary>
                    ///   The timeout to wait for a Pong message since a Ping was sent.
                    /// </summary>
                    [SerializeField]
                    private float patience = 10f;

                    // Tracks a connection and its current countdown.
                    private class PingTracking
                    {
                        /// <summary>
                        ///   The current connection status: whether it
                        ///   is trusted (i.e. no need to send a ping)
                        ///   or not trusted (i.e. a ping was sent and
                        ///   no pong is still received).
                        /// </summary>
                        public bool Trusted = true;

                        /// <summary>
                        ///   The id of the connection being tracked.
                        /// </summary>
                        public ulong ConnectionId;

                        /// <summary>
                        ///   The countdown for that connection, either
                        ///   until a ping command is sent or a timeout
                        ///   is inferred and processed.
                        /// </summary>
                        public float Countdown;
                    }

                    // Tracks the current idle status for each connection.
                    private Dictionary<ulong, PingTracking> tracking = new Dictionary<ulong, PingTracking>();

                    // A sender for the Ping message.
                    private Func<ulong, Task> SendPing;

                    // A sender for the Timeout message.
                    private Func<ulong, Task> SendTimeout;

                    protected override void Setup()
                    {
                        patience = Values.Max(10f, patience);
                        trustTime = Values.Max(50f, trustTime);
                    }

                    protected override void Initialize()
                    {
                        SendPing = MakeSender("Ping");
                        SendTimeout = MakeSender("Timeout");
                    }

                    private void Update()
                    {
                        float delta = Time.unscaledDeltaTime;
                        foreach(PingTracking pingTracking in tracking.Values)
                        {
                            pingTracking.Countdown -= delta;
                            if (pingTracking.Countdown <= 0)
                            {
                                if (pingTracking.Trusted)
                                {
                                    pingTracking.Trusted = false;
                                    pingTracking.Countdown += patience;
                                    Debug.Log($"PingProtocolServerSide::Update()::Sending ping to {pingTracking.ConnectionId}");
                                    SendPing(pingTracking.ConnectionId);
                                }
                                else
                                {
                                    Debug.Log($"PingProtocolServerSide::Update()::Sending timeout to {pingTracking.ConnectionId}");
                                    SendTimeout(pingTracking.ConnectionId);
                                    // We have to be explicit here, or the extension mechanism
                                    // will not recognize our overload of the Invoke function
                                    // since an Invoke is already defined.
                                    this.Invoke(() => { server.Close(pingTracking.ConnectionId); }, 0.5f);
                                }
                            }
                        }
                    }

                    public override async Task OnConnected(ulong clientId)
                    {
                        tracking.Add(clientId, new PingTracking() {
                            Trusted = true,
                            ConnectionId = clientId,
                            Countdown = trustTime
                        });
                    }

                    public override async Task OnDisconnected(ulong clientId, Exception reason)
                    {
                        tracking.Remove(clientId);
                    }

                    protected override void SetIncomingMessageHandlers()
                    {
                        AddIncomingMessageHandler("Pong", async (proto, clientId) =>
                        {
                            // Trust is restored for this connection.
                            Debug.Log($"PingProtocolServerSide::Incoming pong message from {clientId}");
                            tracking[clientId].Countdown = trustTime;
                            tracking[clientId].Trusted = true;
                        });
                    }
                }
            }
        }
    }
}