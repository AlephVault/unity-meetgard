using AlephVault.Unity.Binary;
using AlephVault.Unity.Meetgard.Types;
using System;
using System.Collections;
using System.Collections.Concurrent;
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
    namespace Server
    {
        public partial class NetworkServer : MonoBehaviour
        {
            // The endpoint id for the host.
            public const ulong HostEndpointId = 0;

            // The next id to use, when a new connection is spawned.
            // Please note: id=0 is reserved for a single network
            // endpoint of type NetworkHostEndpoint (i.e. the host
            // connection for non-dedicated games).
            private ulong nextEndpointId = 1;

            // A mapping of the connections currently established. Each
            // connection is mapped against a generated id for them.
            private ConcurrentDictionary<NetworkEndpoint, ulong> endpointIds = new ConcurrentDictionary<NetworkEndpoint, ulong>();

            // A mapping of the connections by their ids.
            private ConcurrentDictionary<ulong, NetworkEndpoint> endpointById = new ConcurrentDictionary<ulong, NetworkEndpoint>();

            // Gets the next id to use. If the next endpoint id is the
            // maximum value, it tries searching a free id among the
            // mapping keys. Otherwise, it just returns the value and
            // then increments.
            private ulong GetNextEndpointId()
            {
                if (nextEndpointId < ulong.MaxValue)
                {
                    return nextEndpointId++;
                }
                else
                {
                    ulong testId = 1;
                    while(true)
                    {
                        if (testId == ulong.MaxValue)
                        {
                            throw new Types.Exception("Connections exhausted! The server is insanely and improbably full");
                        }
                        if (!endpointById.ContainsKey(testId)) return testId;
                        testId++;
                    }
                }
            }

            // Removes the host endpoint. It will emulate disconnection
            // events as if it were a remote endpoint.
            private async void RemoveHostEndpoint()
            {
                if (endpointById.TryRemove(HostEndpointId, out NetworkEndpoint endpoint))
                {
                    endpointIds.TryRemove(endpoint, out var _);
                    await TriggerOnDisconnected(HostEndpointId, null);
                }
            }

            // Creates a NetworkRemoteEndpoint for the given client
            // socket (which is a just-accepted socket), and adds
            // it to the registered endpoints. This is ran on the
            // main server life-cycle.
            private void AddNetworkClientEndpoint(TcpClient clientSocket)
            {
                ulong nextId = GetNextEndpointId();
                NetworkEndpoint endpoint = new NetworkRemoteEndpoint(clientSocket, NewMessageContainer, async () =>
                {
                    await TriggerOnConnected(nextId);
                }, async (protocolId, messageTag, content) =>
                {
                    await HandleMessage(nextId, protocolId, messageTag, content);
                }, async (e) =>
                {
                    endpointById.TryRemove(nextId, out var endpoint);
                    endpointIds.TryRemove(endpoint, out var _);
                    await TriggerOnDisconnected(nextId, e);
                }, maxMessageSize, idleSleepTime);
                endpointById.TryAdd(nextId, endpoint);
                endpointIds.TryAdd(endpoint, nextId);
            }

            // Creates a NetworkLocalEndpoint, on request, and adds it
            // to the registered endpoints. This is ran on demand.
            private void AddNetworkHostEndpoint()
            {
                /**
                 * TODO implement this... later.
                 * 
                NetworkLocalEndpoint endpoint = new NetworkLocalEndpoint(() =>
                {
                    TriggerOnClientConnected(HostEndpointId);
                }, (protocolId, messageTag, reader) =>
                {
                    TriggerOnMessage(HostEndpointId, protocolId, messageTag, reader);
                }, () =>
                {
                    TriggerOnClientDisconnected(HostEndpointId, null);
                });
                 */
            }

            /// <summary>
            ///   Starts a host endpoint (only allowed on an already running server).
            /// </summary>
            public void StartHostEndpoint()
            {
                if (!IsListening)
                {
                    throw new InvalidOperationException("The server is not listening - host endpoint cannot be created");
                }

                AddNetworkHostEndpoint();
            }

            /// <summary>
            ///   Checks whether an endpoint with the given is registered.
            /// </summary>
            /// <param name="clientId">The id to check</param>
            /// <returns>Whether the endpoint exists</returns>
            public bool EndpointExists(ulong clientId)
            {
                return endpointById.ContainsKey(clientId);
            }

            // Closes all the remaining living endpoints.
            private void CloseAllEndpoints()
            {
                ulong[] keys = endpointById.Keys.ToArray();
                foreach (ulong key in keys)
                {
                    if (endpointById.TryGetValue(key, out NetworkEndpoint value)) value.Close();
                }
            }
        }
    }
}
