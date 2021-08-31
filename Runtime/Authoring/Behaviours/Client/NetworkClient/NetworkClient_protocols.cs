using System;
using UnityEngine;
using AlephVault.Unity.Binary;
using AlephVault.Unity.Meetgard.Types;
using AlephVault.Unity.Layout.Utils;
using System.Linq;
using System.Threading.Tasks;

namespace AlephVault.Unity.Meetgard
{
    namespace Client
    {
        public partial class NetworkClient : MonoBehaviour
        {
            // Protocols will exist by their id (0-based)
            private IProtocolClientSide[] protocols = null;

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
            private ushort GetProtocolId(IProtocolClientSide protocol)
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
            private async Task HandleMessage(ushort protocolId, ushort messageTag, ISerializable message)
            {
                // At this point, the protocolId exists. Also, the messageTag exists.
                // We get the client-side handler, and we invoke it.
                Func<ISerializable, Task> handler = protocols[protocolId].GetIncomingMessageHandler(messageTag);
                if (handler != null)
                {
                    await handler(message);
                }
                else
                {
                    Debug.LogWarning($"Message ({protocolId}, {messageTag}) does not have any handler!");
                }
            }

            // Enumerates all of the protocols in this connection.
            // This method will be invoked on Awake, to prepare
            // the list of protocols.
            private void SetupClientProtocols()
            {
                // The first thing is to detect the Zero protocol manually.
                // This is done because, otherwise, adding RequireComponent
                // would force the Zero protocol into a circular dependency
                // in the editor.
                ZeroProtocolClientSide zeroProtocol = GetComponent<ZeroProtocolClientSide>();
                if (zeroProtocol == null)
                {
                    Destroy(gameObject);
                    throw new MissingZeroProtocol("This NetworkClient does not have a ZeroProtocolClientSide protocol behaviour added - it must have one");
                }
                var protocolList = (from protocolClientSide in GetComponents<IProtocolClientSide>() select (Component)protocolClientSide).ToList();
                protocolList.Remove(zeroProtocol);
                Behaviours.SortByDependencies(protocolList.ToArray()).ToList();
                protocolList.Insert(0, zeroProtocol);
                protocols = (from protocolClientSide in protocolList select (IProtocolClientSide)protocolClientSide).ToArray();
            }

            // This function gets invoked when the network client
            // successfully connects to a server. It invokes all
            // of the OnConnected handlers on each protocol.
            private async Task TriggerOnConnected()
            {
                foreach(IProtocolClientSide protocol in protocols)
                {
                    try
                    {
                        await protocol.OnConnected();
                    }
                    catch(System.Exception e)
                    {
                        Debug.LogWarning("An exception was triggered. Ensure exceptions are captured and handled properly, " +
                                         "for this warning will not be available on deployed games");
                        Debug.LogException(e);
                    }
                }
            }

            // This function gets invoked when the network client
            // disconnects from a server, be it normally or not.
            // It invokes all of the OnDisconnected handlers on
            // each protocol.
            private async Task TriggerOnDisconnected(System.Exception reason)
            {
                foreach (IProtocolClientSide protocol in protocols)
                {
                    try
                    {
                        await protocol.OnDisconnected(reason);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("An exception was triggered. Ensure exceptions are captured and handled properly, " +
                                         "for this warning will not be available on deployed games");
                        Debug.LogException(e);
                    }
                }
            }
        }
    }
}
