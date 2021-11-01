
using AlephVault.Unity.Meetgard.Protocols;
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
                ///   Server-side implementation for the "zero" protocol.
                /// </summary>
                public class ZeroProtocolServerSide : ProtocolServerSide<ZeroProtocolDefinition>
                {
                    /// <summary>
                    ///   A value telling the version of the current protocol
                    ///   set in this network server. This must be changed as
                    ///   per deployment, since certain game changes are meant
                    ///   to be not retro-compatible and thus the version must
                    ///   be marked as mismatching.
                    /// </summary>
                    [SerializeField]
                    private Protocols.Version Version;

                    /// <summary>
                    ///   The timeout to wait for a MyVersion message.
                    /// </summary>
                    [SerializeField]
                    private float timeout = 3f;

                    // Tells the connections that are ready to interact (i.e.
                    // have their version handshake completed and approved).
                    private HashSet<ulong> readyConnections = new HashSet<ulong>();

                    private Func<ulong, Task> SendLetsAgree;
                    private Func<ulong, Task> SendTimeout;
                    private Func<ulong, Task> SendVersionMatch;
                    private Func<ulong, Task> SendVersionMismatch;
                    private Func<ulong, Task> SendAlreadyDone;

                    /// <summary>
                    ///   Sends a NotReady message to a client.
                    /// </summary>
                    public Func<ulong, Task> SendNotReady { get; private set; }

                    protected override void Initialize()
                    {
                        SendLetsAgree = MakeSender("LetsAgree");
                        SendTimeout = MakeSender("Timeout");
                        SendVersionMatch = MakeSender("VersionMatch");
                        SendVersionMismatch = MakeSender("VersionMismatch");
                        SendNotReady = MakeSender("NotReady");
                        SendAlreadyDone = MakeSender("AlreadyDone");
                    }

                    /// <summary>
                    ///   Triggered when a connection is ready (i.e. it
                    ///   passed a version check).
                    /// </summary>
                    public event Func<ulong, Task> OnReady = null;

                    /// <summary>
                    ///   Triggered when a connection that was ready (i.e.
                    ///   it passed a version check) is now closing.
                    /// </summary>
                    public event Func<ulong, Exception, Task> OnReadyClosing = null;

                    /// <summary>
                    ///   Tells whether a particular client id is "ready" or
                    ///   not (i.e. still connected and in the set of ready
                    ///   connections: with its version handshake approved).
                    /// </summary>
                    /// <param name="clientId">The client id to check</param>
                    /// <returns>Whether it is ready or not</returns>
                    public bool Ready(ulong clientId)
                    {
                        return readyConnections.Contains(clientId);
                    }

                    public override async Task OnConnected(ulong clientId)
                    {
                        readyConnections.Remove(clientId);
                        Debug.Log("ZeroProtocol::OnConnected::Sending zero-greeting...");
                        await UntilSendIsDone(SendLetsAgree(clientId));
                        Debug.Log("ZeroProtocol::OnConnected::Zero-greeting sent.");
                        StartTimeout(clientId);
                    }

                    // This is intentionally intended to be a separate task.
                    private async void StartTimeout(ulong clientId)
                    {
                        await Task.Delay((int)(1000 * timeout));
                        if (!Ready(clientId))
                        {
                            await UntilSendIsDone(SendTimeout(clientId));
                            server.Close(clientId);
                        }
                    }

                    public override async Task OnDisconnected(ulong clientId, System.Exception reason)
                    {
                        if (readyConnections.Remove(clientId))
                        {
                            await OnReadyClosing(clientId, reason);
                        }
                    }

                    protected override void SetIncomingMessageHandlers()
                    {
                        AddIncomingMessageHandler<Protocols.Version>("MyVersion", async (proto, clientId, version) =>
                        {
                            Debug.Log("Zero ProtocolServerSide::Handling version...");
                            if (Ready(clientId))
                            {
                                Debug.Log("Zero ProtocolServerSide::Sending AlreadyDone...");
                                await UntilSendIsDone(SendAlreadyDone(clientId));
                                Debug.Log("Zero ProtocolServerSide::AlreadyDone sent.");
                            }
                            else if (version.Equals(Version))
                            {
                                Debug.Log("Zero ProtocolServerSide::Sending Match...");
                                await UntilSendIsDone(SendVersionMatch(clientId));
                                readyConnections.Add(clientId);
                                Debug.Log("Zero ProtocolServerSide::Triggering OnReady...");
                                await OnReady(clientId);
                                Debug.Log("Zero ProtocolServerSide::Connection ready.");
                            }
                            else
                            {
                                Debug.Log("Zero ProtocolServerSide::Sending Mismatch...");
                                await UntilSendIsDone(SendVersionMismatch(clientId));
                                Debug.Log("Zero ProtocolServerSide::Closing connection...");
                                server.Close(clientId);
                                Debug.Log("Zero ProtocolServerSide::Connection closed.");
                            }
                        });
                    }
                }
            }
        }
    }
}