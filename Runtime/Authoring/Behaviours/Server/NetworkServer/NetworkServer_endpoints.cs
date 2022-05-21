using AlephVault.Unity.Binary;
using AlephVault.Unity.Meetgard.Types;
using AlephVault.Unity.Support.Types;
using AlephVault.Unity.Support.Utils;
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
    namespace Authoring
    {
        namespace Behaviours
        {
            namespace Server
            {
                public partial class NetworkServer : MonoBehaviour
                {
                    // The endpoint id for the host.
                    public const ulong HostEndpointId = 0;

                    // The pool retrieves the next id to use, and also allows
                    // removing ids that are not used anymore, and optimize
                    // the memory for the allocated ids in order to get the
                    // next id in an optimal way.
                    private IdPool connectionIdPool = new IdPool();

                    // A mutex to interact with the connectionIdPool.
                    private SemaphoreSlim connectionIdPoolMutex = new SemaphoreSlim(1, 1);

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
                        return connectionIdPool.Next();
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

                    /// <summary>
                    ///   Returns the native remote endpoint of a connection.
                    ///   Since our connections are only IP, most of the data
                    ///   will be constant.
                    /// </summary>
                    /// <param name="connectionId">The id of the connection to get the endpoint for</param>
                    /// <returns>The native remote endpoint data</returns>
                    /// <exception cref="InvalidOperationException">The connection is invalid or not available</exception>
                    public IPEndPoint GetRemoteEndpoint(ulong connectionId)
                    {
                        try
                        {
                            return endpointById[connectionId].RemoteEndpoint;
                        }
                        catch (KeyNotFoundException e)
                        {
                            throw new InvalidOperationException($"Connection id {connectionId} is not " +
                                                                $"available. It might be already terminated " +
                                                                $"or an arbitrary value, not tested to exist " +
                                                                $"as a valid connection id");
                        }
                    }

                    // Creates a NetworkRemoteEndpoint for the given client
                    // socket (which is a just-accepted socket), and adds
                    // it to the registered endpoints. This is ran on the
                    // main server life-cycle.
                    private void AddNetworkClientEndpoint(TcpClient clientSocket)
                    {
                        ulong nextId = GetNextEndpointId();
                        NetworkEndpoint endpoint = new NetworkRemoteEndpoint(clientSocket, PrepareStream, NewMessageContainer, async () =>
                        {
                            XDebug debugger = new XDebug("Meetgard", this, $"AddNetworkClientEndpoint::OnConnected({nextId})", debug);
                            debugger.Start();
                            try
                            {
                                await TriggerOnConnected(nextId);
                            }
                            finally
                            {
                                debugger.End();
                            }
                        }, async (protocolId, messageTag, content) =>
                        {
                            await HandleMessage(nextId, protocolId, messageTag, content);
                        }, async (e) =>
                        {
                            XDebug debugger = new XDebug("Meetgard", this, $"AddNetworkClientEndpoint::OnDisconnected({nextId})", debug);
                            debugger.Start();
                            endpointById.TryRemove(nextId, out var endpoint);
                            endpointIds.TryRemove(endpoint, out var _);
                            try
                            {
                                debugger.Info("Acquiring ID Mutex");
                                await connectionIdPoolMutex.WaitAsync();
                                debugger.Info($"Releasing ID {nextId}");
                                connectionIdPool.Release(nextId);
                                debugger.Info("Triggering OnConnected");
                                await TriggerOnDisconnected(nextId, e);
                            }
                            finally
                            {
                                debugger.Info("Releasing ID Mutex");
                                connectionIdPoolMutex.Release();
                                debugger.End();
                            }
                        }, maxMessageSize, idleSleepTime, writeTimeout);
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
    }
}
