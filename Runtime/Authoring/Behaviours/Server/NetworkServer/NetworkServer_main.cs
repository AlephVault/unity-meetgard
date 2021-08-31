using AlephVault.Unity.Binary;
using AlephVault.Unity.Meetgard.Types;
using AlephVault.Unity.Support.Utils;
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
                public partial class NetworkServer : MonoBehaviour
                {
                    // The current listener.
                    private TcpListener listener = null;

                    /// <summary>
                    ///   Tells whether the life-cycle is active or not. While Active, another
                    ///   life-cycle (e.g. a call to <see cref="Listen(int)"/> or
                    ///   <see cref="Connect(string, int)"/>) cannot be done.
                    /// </summary>
                    public bool IsRunning { get { return lifeCycle != null && lifeCycle.IsAlive; } }

                    /// <summary>
                    ///   Tells whether the server is currently listening.
                    /// </summary>
                    public bool IsListening { get { return listener != null; } }

                    private void Awake()
                    {
                        maxMessageSize = Values.Clamp(512, maxMessageSize, 6144);
                        idleSleepTime = Values.Clamp(0.005f, idleSleepTime, 0.5f);
                        SetupServerProtocols();
                    }

                    /// <summary>
                    ///   Starts the server, if it is not already started, in all the
                    ///   available ip network interfaces.
                    /// </summary>
                    /// <param name="port">The port to listen at</param>
                    public void StartServer(int port)
                    {
                        StartServer(IPAddress.Any, port);
                    }

                    /// <summary>
                    ///   Starts the server, if it is not already started.
                    /// </summary>
                    /// <param name="adddress">The address to listen at</param>
                    /// <param name="port">The port to listen at</param>
                    public void StartServer(IPAddress address, int port)
                    {
                        if (IsRunning)
                        {
                            throw new InvalidOperationException("The server is already running");
                        }

                        listener = new TcpListener(address, port);
                        listener.Start();
                        StartLifeCycle();
                    }

                    /// <summary>
                    ///   Stops the server, if it is already started and listening.
                    ///   This will trigger an exception in the life-cycle which will
                    ///   be understood as a graceful closure.
                    /// </summary>
                    public void StopServer()
                    {
                        if (!IsListening)
                        {
                            throw new InvalidOperationException("The server is not listening");
                        }

                        listener.Stop();
                    }

                    /// <summary>
                    ///   Sends a message to a registered endpoint by its id.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <param name="clientId">The id of the client</param>
                    /// <param name="protocol">The protocol for this message. It must be an already attached component</param>
                    /// <param name="message">The message (as it was registered) being sent</param>
                    /// <param name="content">The message content</param>
                    /// <returns>A task, if the client exists, or null otherwise</returns>
                    public Task Send<T>(IProtocolServerSide protocol, string message, ulong clientId, T content) where T : ISerializable
                    {
                        if (protocol == null)
                        {
                            throw new ArgumentNullException("protocol");
                        }

                        if (!IsRunning)
                        {
                            throw new InvalidOperationException("The server is not running - cannot send any message");
                        }

                        ushort protocolId = GetProtocolId(protocol);
                        ushort messageTag;
                        Type expectedType;
                        try
                        {
                            messageTag = GetOutgoingMessageTag(protocolId, message);
                            expectedType = GetOutgoingMessageType(protocolId, messageTag);
                        }
                        catch (UnexpectedMessageException e)
                        {
                            // Reformatting the exception.
                            throw new UnexpectedMessageException($"Unexpected outgoing protocol/message: ({protocol.GetType().FullName}, {message})", e);
                        }

                        if (content.GetType() != expectedType)
                        {
                            throw new OutgoingMessageTypeMismatchException($"Outgoing message ({protocol.GetType().FullName}, {message}) was attempted with type {content.GetType().FullName} when {expectedType.FullName} was expected");
                        }

                        if (endpointById.TryGetValue(clientId, out NetworkEndpoint endpoint))
                        {
                            return endpoint.Send(protocolId, messageTag, content);
                        }
                        else
                        {
                            return null;
                        }
                    }

                    /// <summary>
                    ///   Sends a message to a registered endpoint by its id. The message
                    ///   does not have any body.
                    /// </summary>
                    /// <param name="clientId">The id of the client</param>
                    /// <param name="protocol">The protocol for this message. It must be an already attached component</param>
                    /// <param name="message">The message (as it was registered) being sent</param>
                    /// <returns>A task, if the client exists, or null otherwise</returns>
                    public Task Send(IProtocolServerSide protocol, string message, ulong clientId)
                    {
                        return Send(protocol, message, clientId, new Nothing());
                    }

                    /// <summary>
                    ///   Sends a message through the network.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <typeparam name="ProtocolType">The protocol type for this message. One instance of it must be an already attached component</param>
                    /// <param name="clientId">The id of the client</param>
                    /// <param name="message">The message (as it was registered) being sent</param>
                    /// <param name="content">The message content</param>
                    /// <returns>A task, if the client exists, or null otherwise</returns>
                    public Task Send<ProtocolType, T>(string message, ulong clientId, T content) where ProtocolType : IProtocolServerSide where T : ISerializable
                    {
                        ProtocolType protocol = GetComponent<ProtocolType>();
                        if (protocol == null)
                        {
                            throw new UnknownProtocolException($"This object does not have a protocol of type {protocol.GetType().FullName} attached to it");
                        }
                        else
                        {
                            return Send(protocol, message, clientId, content);
                        }
                    }

                    /// <summary>
                    ///   Sends a message through the network. The message does not
                    ///   have any body.
                    /// </summary>
                    /// <typeparam name="ProtocolType">The protocol type for this message. One instance of it must be an already attached component</param>
                    /// <param name="clientId">The id of the client</param>
                    /// <param name="message">The message (as it was registered) being sent</param>
                    /// <returns>A task, if the client exists, or null otherwise</returns>
                    public Task Send<ProtocolType>(string message, ulong clientId) where ProtocolType : IProtocolServerSide
                    {
                        return Send<ProtocolType, Nothing>(message, clientId, new Nothing());
                    }

                    /// <summary>
                    ///   Creates a sender shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <param name="protocol">The protocol for this message. It must be an already attached component</param>
                    /// <param name="message">The message (as it was registered) that this sender will send</param>
                    /// <returns>A function that takes the message to send, of the appropriate type, and sends it (asynchronously)</returns>
                    public Func<ulong, T, Task> MakeSender<T>(IProtocolServerSide protocol, string message) where T : ISerializable
                    {
                        if (protocol == null)
                        {
                            throw new ArgumentNullException("protocol");
                        }

                        ushort protocolId = GetProtocolId(protocol);
                        ushort messageTag;
                        Type expectedType;
                        try
                        {
                            messageTag = GetOutgoingMessageTag(protocolId, message);
                            expectedType = GetOutgoingMessageType(protocolId, messageTag);
                        }
                        catch (UnexpectedMessageException e)
                        {
                            // Reformatting the exception.
                            throw new UnexpectedMessageException($"Unexpected outgoing protocol/message: ({protocol.GetType().FullName}, {message})", e);
                        }

                        if (typeof(T) != expectedType)
                        {
                            throw new OutgoingMessageTypeMismatchException($"Message sender creation for protocol / message ({protocol.GetType().FullName}, {message}) was attempted with type {typeof(T).FullName} when {expectedType.FullName} was expected");
                        }

                        return (clientId, content) =>
                        {
                            if (!IsRunning)
                            {
                                throw new InvalidOperationException("The server is not running - cannot send any message");
                            }

                            if (content.GetType() != expectedType)
                            {
                                throw new OutgoingMessageTypeMismatchException($"Outgoing message ({protocol.GetType().FullName}, {message}) was attempted with type {content.GetType().FullName} when {expectedType.FullName} was expected");
                            }

                            if (endpointById.TryGetValue(clientId, out NetworkEndpoint endpoint))
                            {
                                return endpoint.Send(protocolId, messageTag, content);
                            }
                            else
                            {
                                return null;
                            }
                        };
                    }

                    /// <summary>
                    ///   Creates a sender shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once. The message to send does
                    ///   not have any body.
                    /// </summary>
                    /// <param name="protocol">The protocol for this message. It must be an already attached component</param>
                    /// <param name="message">The message (as it was registered) that this sender will send</param>
                    /// <returns>A function that takes the target client, and sends the message (asynchronously)</returns>
                    public Func<ulong, Task> MakeSender(IProtocolServerSide protocol, string message)
                    {
                        Func<ulong, Nothing, Task> sender = MakeSender<Nothing>(protocol, message);
                        return (clientId) => sender(clientId, new Nothing());
                    }

                    /// <summary>
                    ///   Creates a sender shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once.
                    /// </summary>
                    /// <typeparam name="ProtocolType">The protocol type for this message. One instance of it must be an already attached component</param>
                    /// <typeparam name="T">The type of the message this sender will send</typeparam>
                    /// <param name="message">The name of the message this sender will send</param>
                    /// <returns>A function that takes the message to send, of the appropriate type, and sends it (asynchronously)</returns>
                    public Func<ulong, T, Task> MakeSender<ProtocolType, T>(string message) where ProtocolType : IProtocolServerSide where T : ISerializable
                    {
                        ProtocolType protocol = GetComponent<ProtocolType>();
                        if (protocol == null)
                        {
                            throw new UnknownProtocolException($"This object does not have a protocol of type {protocol.GetType().FullName} attached to it");
                        }
                        else
                        {
                            return MakeSender<T>(protocol, message);
                        }
                    }

                    /// <summary>
                    ///   Creates a sender shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once. The message to send does
                    ///   not have any body.
                    /// </summary>
                    /// <typeparam name="ProtocolType">The protocol type for this message. One instance of it must be an already attached component</param>
                    /// <param name="message">The name of the message this sender will send</param>
                    /// <returns>A function that takes the target client, and sends the message (asynchronously)</returns>
                    public Func<ulong, Task> MakeSender<ProtocolType>(string message) where ProtocolType : IProtocolServerSide
                    {
                        Func<ulong, Nothing, Task> sender = MakeSender<ProtocolType, Nothing>(message);
                        return (clientId) => sender(clientId, new Nothing());
                    }

                    /// <summary>
                    ///   <para>
                    ///     Sends a message to many registered endpoints by their ids.
                    ///     All the endpoints that are not found, or throw an exception
                    ///     on send, are ignored and kept in an output bag of failed
                    ///     endpoints.
                    ///   </para>
                    ///   <para>
                    ///     Notes: use <code>null</code> as the first argument to notify
                    ///     to all the available registered endpoints.
                    ///   </para>
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <param name="protocol">The protocol for this message. It must be an already attached component</param>
                    /// <param name="message">The message (as it was registered) being sent</param>
                    /// <param name="clientIds">The ids to send the same message - use null to specify ALL the available ids</param>
                    /// <param name="content">The message content</param>
                    /// <returns>The send tasks for each endpoint that was iterated</returns>
                    public Dictionary<ulong, Task> Broadcast<T>(IProtocolServerSide protocol, string message, IEnumerable<ulong> clientIds, T content) where T : ISerializable
                    {
                        if (protocol == null)
                        {
                            throw new ArgumentNullException("protocol");
                        }

                        if (!IsRunning)
                        {
                            throw new InvalidOperationException("The server is not running - cannot send any message");
                        }

                        ushort protocolId = GetProtocolId(protocol);
                        ushort messageTag;
                        Type expectedType;
                        try
                        {
                            messageTag = GetOutgoingMessageTag(protocolId, message);
                            expectedType = GetOutgoingMessageType(protocolId, messageTag);
                        }
                        catch (UnexpectedMessageException e)
                        {
                            // Reformatting the exception.
                            throw new UnexpectedMessageException($"Unexpected outgoing protocol/message: ({protocol.GetType().FullName}, {message})", e);
                        }

                        if (content.GetType() != expectedType)
                        {
                            throw new OutgoingMessageTypeMismatchException($"Outgoing message ({protocol.GetType().FullName}, {message}) was attempted with type {content.GetType().FullName} when {expectedType.FullName} was expected");
                        }

                        // Now, with everything ready, the send can be done. 
                        return DoBroadcast(protocolId, messageTag, clientIds, content);
                    }

                    /// <summary>
                    ///   <para>
                    ///     Sends a message to many registered endpoints by their ids.
                    ///     All the endpoints that are not found, or throw an exception
                    ///     on send, are ignored and kept in an output bag of failed
                    ///     endpoints. The message to send does not have any body.
                    ///   </para>
                    ///   <para>
                    ///     Notes: use <code>null</code> as the first argument to notify
                    ///     to all the available registered endpoints.
                    ///   </para>
                    /// </summary>
                    /// <param name="protocol">The protocol for this message. It must be an already attached component</param>
                    /// <param name="message">The message (as it was registered) being sent</param>
                    /// <param name="clientIds">The ids to send the same message - use null to specify ALL the available ids</param>
                    /// <returns>The send tasks for each endpoint that was iterated</returns>
                    public Dictionary<ulong, Task> Broadcast(IProtocolServerSide protocol, string message, IEnumerable<ulong> clientIds)
                    {
                        return Broadcast(protocol, message, clientIds, new Nothing());
                    }

                    private Dictionary<ulong, Task> DoBroadcast<T>(ushort protocolId, ushort messageTag, IEnumerable<ulong> clientIds, T content) where T : ISerializable
                    {
                        Dictionary<ulong, Task> endpointTasks = new Dictionary<ulong, Task>();

                        if (clientIds != null)
                        {
                            // Only the specified endpoints will be iterated.
                            foreach (ulong clientId in clientIds)
                            {
                                if (endpointById.TryGetValue(clientId, out NetworkEndpoint endpoint))
                                {
                                    try
                                    {
                                        endpointTasks?.Add(clientId, endpoint.Send(protocolId, messageTag, content));
                                    }
                                    catch
                                    {
                                        endpointTasks?.Add(clientId, null);
                                    }
                                }
                                else
                                {
                                    endpointTasks?.Add(clientId, null);
                                }
                            }
                        }
                        else
                        {
                            // All of the endpoints will be iterated.
                            foreach (KeyValuePair<ulong, NetworkEndpoint> pair in endpointById.ToArray())
                            {
                                try
                                {
                                    endpointTasks?.Add(pair.Key, pair.Value.Send(protocolId, messageTag, content));
                                }
                                catch
                                {
                                    endpointTasks?.Add(pair.Key, null);
                                }
                            }
                        }

                        return endpointTasks;
                    }

                    /// <summary>
                    ///   <para>
                    ///     Sends a message to many registered endpoints by their ids.
                    ///     All the endpoints that are not found, or throw an exception
                    ///     on send, are ignored and kept in an output bag of failed
                    ///     endpoints.
                    ///   </para>
                    ///   <para>
                    ///     Notes: use <code>null</code> as the first argument to notify
                    ///     to all the available registered endpoints.
                    ///   </para>
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <typeparam name="ProtocolType">The protocol type for this message. One instance of it must be an already attached component</param>
                    /// <param name="message">The message (as it was registered) being sent</param>
                    /// <param name="clientIds">The ids to send the same message - use null to specify ALL the available ids</param>
                    /// <param name="content">The message content</param>
                    /// <returns>The send tasks for each endpoint that was iterated</returns>
                    public Dictionary<ulong, Task> Broadcast<ProtocolType, T>(string message, IEnumerable<ulong> clientIds, T content) where ProtocolType : IProtocolServerSide where T : ISerializable
                    {
                        ProtocolType protocol = GetComponent<ProtocolType>();
                        if (protocol == null)
                        {
                            throw new UnknownProtocolException($"This object does not have a protocol of type {protocol.GetType().FullName} attached to it");
                        }
                        else
                        {
                            return Broadcast(protocol, message, clientIds, content);
                        }
                    }

                    /// <summary>
                    ///   <para>
                    ///     Sends a message to many registered endpoints by their ids.
                    ///     All the endpoints that are not found, or throw an exception
                    ///     on send, are ignored and kept in an output bag of failed
                    ///     endpoints. The message to send does not have any body.
                    ///   </para>
                    ///   <para>
                    ///     Notes: use <code>null</code> as the first argument to notify
                    ///     to all the available registered endpoints.
                    ///   </para>
                    /// </summary>
                    /// <typeparam name="ProtocolType">The protocol type for this message. One instance of it must be an already attached component</param>
                    /// <param name="message">The message (as it was registered) being sent</param>
                    /// <param name="clientIds">The ids to send the same message - use null to specify ALL the available ids</param>
                    /// <returns>The send tasks for each endpoint that was iterated</returns>
                    public Dictionary<ulong, Task> Broadcast<ProtocolType>(string message, IEnumerable<ulong> clientIds) where ProtocolType : IProtocolServerSide
                    {
                        return Broadcast<ProtocolType, Nothing>(message, clientIds, new Nothing());
                    }

                    /// <summary>
                    ///   Creates a broadcaster shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once.
                    /// </summary>
                    /// <typeparam name="T">The type of the message this sender will send</typeparam>
                    /// <param name="protocol">The protocol for this message. It must be an already attached component</param>
                    /// <param name="message">The name of the message this sender will send</param>
                    /// <returns>A function that takes the list of clients and the message to send, of the appropriate type, and sends it (asynchronously)</returns>
                    public Func<IEnumerable<ulong>, T, Dictionary<ulong, Task>> MakeBroadcaster<T>(IProtocolServerSide protocol, string message) where T : ISerializable
                    {
                        if (protocol == null)
                        {
                            throw new ArgumentNullException("protocol");
                        }

                        ushort protocolId = GetProtocolId(protocol);
                        ushort messageTag;
                        Type expectedType;
                        try
                        {
                            messageTag = GetOutgoingMessageTag(protocolId, message);
                            expectedType = GetOutgoingMessageType(protocolId, messageTag);
                        }
                        catch (UnexpectedMessageException e)
                        {
                            // Reformatting the exception.
                            throw new UnexpectedMessageException($"Unexpected outgoing protocol/message: ({protocol.GetType().FullName}, {message})", e);
                        }

                        if (typeof(T) != expectedType)
                        {
                            throw new OutgoingMessageTypeMismatchException($"Message sender creation for protocol / message ({protocol.GetType().FullName}, {message}) was attempted with type {typeof(T).FullName} when {expectedType.FullName} was expected");
                        }

                        return (clientIds, content) =>
                        {
                            if (!IsRunning)
                            {
                                throw new InvalidOperationException("The endpoint is not running - No data can be sent");
                            }

                            if (content.GetType() != expectedType)
                            {
                                throw new OutgoingMessageTypeMismatchException($"Outgoing message ({protocol.GetType().FullName}, {message}) was attempted with type {content.GetType().FullName} when {expectedType.FullName} was expected");
                            }

                            return DoBroadcast(protocolId, messageTag, clientIds, content);
                        };
                    }

                    /// <summary>
                    ///   Creates a broadcaster shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once. The message to send does not
                    ///   have any body.
                    /// </summary>
                    /// <param name="protocol">The protocol for this message. It must be an already attached component</param>
                    /// <param name="message">The name of the message this sender will send</param>
                    /// <returns>A function that takes the list of clients and sends the message (asynchronously)</returns>
                    public Func<IEnumerable<ulong>, Dictionary<ulong, Task>> MakeBroadcaster(IProtocolServerSide protocol, string message)
                    {
                        Func<IEnumerable<ulong>, Nothing, Dictionary<ulong, Task>> broadcaster = MakeBroadcaster<Nothing>(protocol, message);
                        return (clientIds) => broadcaster(clientIds, new Nothing());
                    }

                    /// <summary>
                    ///   Creates a broadcaster shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once.
                    /// </summary>
                    /// <typeparam name="ProtocolType">The protocol type for this message. One instance of it must be an already attached component</param>
                    /// <typeparam name="T">The type of the message this sender will send</typeparam>
                    /// <param name="message">The name of the message this sender will send</param>
                    /// <returns>A function that takes the list of clients and the message to send, of the appropriate type, and sends it (asynchronously)</returns>
                    public Func<IEnumerable<ulong>, T, Dictionary<ulong, Task>> MakeBroadcaster<ProtocolType, T>(string message) where ProtocolType : IProtocolServerSide where T : ISerializable
                    {
                        ProtocolType protocol = GetComponent<ProtocolType>();
                        if (protocol == null)
                        {
                            throw new UnknownProtocolException($"This object does not have a protocol of type {protocol.GetType().FullName} attached to it");
                        }
                        else
                        {
                            return MakeBroadcaster<T>(protocol, message);
                        }
                    }

                    /// <summary>
                    ///   Creates a broadcaster shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once. The message to send does not
                    ///   have any body.
                    /// </summary>
                    /// <typeparam name="ProtocolType">The protocol type for this message. One instance of it must be an already attached component</param>
                    /// <param name="message">The name of the message this sender will send</param>
                    /// <returns>A function that takes the list of clients, and sends the message (asynchronously)</returns>
                    public Func<IEnumerable<ulong>, Dictionary<ulong, Task>> MakeBroadcaster<ProtocolType>(string message) where ProtocolType : IProtocolServerSide
                    {
                        Func<IEnumerable<ulong>, Nothing, Dictionary<ulong, Task>> broadcaster = MakeBroadcaster<ProtocolType, Nothing>(message);
                        return (clientIds) => broadcaster(clientIds, new Nothing());
                    }

                    /// <summary>
                    ///   Closes a registered endpoint by its id.
                    /// </summary>
                    /// <param name="clientId">The id of the client</param>
                    /// <returns>Whether the endpoint existed or not (if true, it was also closed)</returns>
                    public bool Close(ulong clientId)
                    {
                        if (!IsRunning)
                        {
                            throw new InvalidOperationException("The server is not running - cannot close any connection");
                        }

                        if (endpointById.TryGetValue(clientId, out NetworkEndpoint value))
                        {
                            value.Close();
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }
    }
}
