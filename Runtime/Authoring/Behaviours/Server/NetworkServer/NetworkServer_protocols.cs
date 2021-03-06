using AlephVault.Unity.Binary;
using AlephVault.Unity.Meetgard.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
                using AlephVault.Unity.Layout.Utils;
                using AlephVault.Unity.Support.Utils;

                public partial class NetworkServer : MonoBehaviour
                {
                    // Protocols will exist by their id (0-based)
                    private IProtocolServerSide[] protocols = null;

                    // Returns an object to serve as the receiver of specific
                    // message data. This must be implemented with the protocol.
                    private ISerializable NewMessageContainer(ushort protocolId, ushort messageTag)
                    {
                        if (protocolId >= protocols.Length)
                        {
                            throw new UnexpectedMessageException($"Unexpected incoming message protocol/tag: ({protocolId}, {messageTag})");
                        }
                        ISerializable result = protocols[protocolId].NewMessageContainer(messageTag);
                        if (result == null)
                        {
                            throw new UnexpectedMessageException($"Unexpected outgoing message protocol/tag: ({protocolId}, {messageTag})");
                        }
                        else
                        {
                            return result;
                        }
                    }

                    // Returns the expected type for a message to be sent.
                    private Type GetOutgoingMessageType(ushort protocolId, ushort messageTag)
                    {
                        if (protocolId >= protocols.Length)
                        {
                            throw new UnexpectedMessageException($"Unexpected outgoing message protocol/tag: ({protocolId}, {messageTag})");
                        }
                        Type result = protocols[protocolId].GetOutgoingMessageType(messageTag);
                        if (result == null)
                        {
                            throw new UnexpectedMessageException($"Unexpected outgoing message protocol/tag: ({protocolId}, {messageTag})");
                        }
                        else
                        {
                            return result;
                        }
                    }

                    // Returns the index for a given protocol id.
                    private ushort GetProtocolId(IProtocolServerSide protocol)
                    {
                        int index = Array.IndexOf(protocols, protocol);
                        if (index == protocols.GetLowerBound(0) - 1)
                        {
                            throw new UnknownProtocolException($"The given instance of {protocol.GetType().FullName} is not a component on this object");
                        }
                        else
                        {
                            return (ushort)index;
                        }
                    }

                    // Returns the message tag for the given protocol and message name.
                    private ushort GetOutgoingMessageTag(ushort protocolId, string messageName)
                    {
                        if (protocolId >= protocols.Length)
                        {
                            throw new UnexpectedMessageException($"Unexpected outgoing message protocol/name: ({protocolId}, {messageName})");
                        }
                        ushort? tag = protocols[protocolId].GetOutgoingMessageTag(messageName);
                        if (tag == null)
                        {
                            throw new UnexpectedMessageException($"Unexpected outgoing message protocol/name: ({protocolId}, {messageName})");
                        }
                        else
                        {
                            return tag.Value;
                        }
                    }

                    // Handles a received message. The received message will be
                    // handled by the underlying protocol handler.
                    private async Task HandleMessage(ulong clientId, ushort protocolId, ushort messageTag, ISerializable message)
                    {
                        XDebug debugger = new XDebug("Meetgard", this, "HandleMessage", debug);
                        debugger.Start();
                        ZeroProtocolServerSide zeroProtocol = (ZeroProtocolServerSide)protocols[0];
                        if (protocolId != 0 && !zeroProtocol.Ready(clientId))
                        {
                            await zeroProtocol.SendNotReady(clientId);
                            return;
                        }

                        // At this point, the protocolId exists. Also, the messageTag exists.
                        // Also, the client is ready to interact freely with the server. We
                        // get the client-side handler, and we invoke it.
                        Func<ulong, ISerializable, Task> handler = protocols[protocolId].GetIncomingMessageHandler(messageTag);
                        if (handler != null)
                        {
                            debugger.Info($"Message ({protocolId}, {messageTag}) being handled");
                            await handler(clientId, message);
                        }
                        else
                        {
                            debugger.Warning($"Message ({protocolId}, {messageTag}) does not have any handler!");
                        }
                        debugger.End();
                    }

                    // Enumerates all of the protocols in this connection.
                    // This method will be invoked on Awake, to prepare
                    // the list of protocols.
                    private void SetupServerProtocols()
                    {
                        // The first thing is to detect the Zero protocol manually.
                        // This is done because, otherwise, adding RequireComponent
                        // would force the Zero protocol into a circular dependency
                        // in the editor.
                        ZeroProtocolServerSide zeroProtocol = GetComponent<ZeroProtocolServerSide>();
                        if (zeroProtocol == null)
                        {
                            Destroy(gameObject);
                            throw new MissingZeroProtocol("This NetworkServer does not have a ZeroProtocolServerSide protocol behaviour added - it must have one");
                        }
                        var protocolList = (from protocolServerSide in GetComponents<IProtocolServerSide>() select (Component)protocolServerSide).ToList();
                        protocolList.Remove(zeroProtocol);
                        Behaviours.SortByDependencies(protocolList.ToArray()).ToList();
                        protocolList.Insert(0, zeroProtocol);
                        protocols = (from protocolServerSide in protocolList select (IProtocolServerSide)protocolServerSide).ToArray();
                        zeroProtocol.OnReady += async (clientId) =>
                        {
                            XDebug debugger = new XDebug("Meetgard", this, $"ZeroProtocol.[Base OnReady]({clientId})", debug);
                            debugger.Start();
                            for (int i = 1; i < protocols.Length; i++)
                            {
                                IProtocolServerSide protocol = protocols[i];
                                try
                                {
                                    debugger.Info($"{protocol.GetType().FullName}.OnConnected (migth be async)");
                                    await protocol.OnConnected(clientId);
                                }
                                catch (System.Exception e)
                                {
                                    debugger.Exception(e);
                                }
                            }
                            debugger.End();
                        };
                        zeroProtocol.OnReadyClosing += async (clientId, reason) =>
                        {
                            XDebug debugger = new XDebug("Meetgard", this, $"ZeroProtocol.[Base OnReadyClosing]({clientId})", debug);
                            debugger.Start();
                            for (int i = 1; i < protocols.Length; i++)
                            {
                                IProtocolServerSide protocol = protocols[i];
                                try
                                {
                                    debugger.Info($"{protocol.GetType().FullName}.OnDisonnected (might be async)");
                                    await protocol.OnDisconnected(clientId, reason);
                                }
                                catch (System.Exception e)
                                {
                                    debugger.Exception(e);
                                }
                            }
                            debugger.End();
                        };
                    }

                    // This function gets invoked when the network server
                    // started. It invokes all of the OnServerStarted
                    // handlers on each protocol.
                    private async Task TriggerOnServerStarted()
                    {
                        XDebug debugger = new XDebug("Meetgard", this, $"TriggerOnServerStarted()", debug);
                        debugger.Start();
                        foreach (IProtocolServerSide protocol in protocols)
                        {
                            try
                            {
                                debugger.Info($"{protocol.GetType().FullName}.OnServerStarted (might be async)");
                                await protocol.OnServerStarted();
                            }
                            catch (System.Exception e)
                            {
                                debugger.Exception(e);
                            }
                        }
                        debugger.End();
                    }

                    // This function gets invoked when a network client
                    // successfully connects to this server. It invokes
                    // the OnConnected only in the Zero protocol. The
                    // zero protocol, at a different moment, will trigger
                    // the OnConnected method in the other protocols.
                    private async Task TriggerOnConnected(ulong clientId)
                    {
                        XDebug debugger = new XDebug("Meetgard", this, $"TriggerOnConnected({clientId})", debug);
                        debugger.Start();
                        await protocols[0].OnConnected(clientId);
                        debugger.End();
                    }

                    // This function gets invoked when a network client
                    // disconnects from this server, be it normally or
                    // not. It invokes the OnDisconnected handler only
                    // in the zero protocol.
                    private async Task TriggerOnDisconnected(ulong clientId, System.Exception reason)
                    {
                        XDebug debugger = new XDebug("Meetgard", this, $"TriggerOnDisconnected({clientId})", debug);
                        debugger.Start();
                        await protocols[0].OnDisconnected(clientId, reason);
                        debugger.End();
                    }

                    // This function gets invoked when the network server
                    // stopped. It invokes all of the OnServerStopped
                    // handlers on each protocol.
                    private async Task TriggerOnServerStopped(System.Exception reason)
                    {
                        XDebug debugger = new XDebug("Meetgard", this, $"TriggerOnServerStopped()", debug);
                        debugger.Start();
                        foreach (IProtocolServerSide protocol in protocols)
                        {
                            try
                            {
                                debugger.Info($"{protocol.GetType().FullName}.OnServerStopped (might be async)");
                                await protocol.OnServerStopped(reason);
                            }
                            catch (System.Exception e)
                            {
                                debugger.Exception(e);
                            }
                        }
                        debugger.End();
                    }
                }
            }
        }
    }
}
