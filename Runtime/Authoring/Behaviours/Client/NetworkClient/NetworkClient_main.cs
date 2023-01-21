using AlephVault.Unity.Support.Utils;
using System;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Authoring
    {
        namespace Behaviours
        {
            namespace Client
            {
                using AlephVault.Unity.Binary;
                using AlephVault.Unity.Meetgard.Types;
                using AlephVault.Unity.Support.Authoring.Behaviours;
                using System.Net;
                using System.Net.Sockets;
                using System.Threading.Tasks;

                public partial class NetworkClient : MonoBehaviour
                {
                    // The underlying network endpoint, or null if the connection
                    // is not established.
                    private NetworkRemoteEndpoint endpoint = null;

                    /// <summary>
                    ///   The related queue manager.
                    /// </summary>
                    public AsyncQueueManager QueueManager { get; private set; }

                    /// <summary>
                    ///   Tells whether the endpoint is active or not. While Active, another
                    ///   life-cycle (e.g. a call to <see cref="Connect(IPAddress, int)"/> or
                    ///   <see cref="Connect(string, int)"/>) cannot be done.
                    /// </summary>
                    public bool IsRunning { get { return endpoint != null && endpoint.IsActive; } }

                    /// <summary>
                    ///   Tells whether the underlying socket is instantiated and connected.
                    /// </summary>
                    public bool IsConnected { get { return endpoint != null && endpoint.IsConnected; } }

                    // The target host this client is being connected against. Only available
                    // while connected AND not using a bare IP.
                    private string targetHostName = null;

                    private void Awake()
                    {
                        QueueManager = GetComponent<AsyncQueueManager>();
                        maxMessageSize = Values.Clamp(512, maxMessageSize, 6144);
                        idleSleepTime = Values.Clamp(0.005f, idleSleepTime, 0.5f);
                        writeTimeout = Values.Max(15f, writeTimeout);
                        if (DontDestroy) DontDestroyOnLoad(gameObject);
                        SetupClientProtocols();
                    }

                    private void OnDestroy()
                    {
                        if (IsConnected) Close();
                    }

                    /// <summary>
                    ///   Connects to a specific address/port pair.
                    /// </summary>
                    /// <param name="address">Any IPv4 or IPv6 valid address</param>
                    /// <param name="port">Any port nuber (in the TCP range)</param>
                    public void Connect(IPAddress address, int port)
                    {
                        Connect(address.ToString(), port);
                    }

                    /// <summary>
                    ///   Connects to a specific address/port pair.
                    /// </summary>
                    /// <param name="address">Any IPv4 or IPv6 valid address</param>
                    /// <param name="port">Any port nuber (in the TCP range)</param>
                    public void Connect(string address, int port)
                    {
                        if (IsRunning)
                        {
                            throw new InvalidOperationException("The socket is already connected - It cannot be connected again");
                        }

                        try
                        {
                            _ = IPAddress.Parse(address);
                            if (secure) throw new InvalidOperationException("Cannot connect to a bare IP " +
                                                                            "address since this client is set " +
                                                                            "to use a secure connection");
                            targetHostName = null;
                        }
                        catch (FormatException)
                        {
                            targetHostName = address;
                        }
                        
                        // Connects to a given address. Throws any exception
                        // that socket connection throws.
                        TcpClient client = new TcpClient();
                        client.Connect(address, port);
                        endpoint = new NetworkRemoteEndpoint(
                            client, PrepareStream, NewMessageContainer, TriggerOnConnected, HandleMessage, TriggerOnDisconnected,
                            maxMessageSize, idleSleepTime, writeTimeout
                        );
                    }

                    /// <summary>
                    ///   Sends a message through the network. This function is asynchronous
                    ///   and will wait until no other messages are pending to be sent.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <param name="protocol">The protocol for this message. It must be an already attached component</param>
                    /// <param name="message">The message (as it was registered) being sent</param>
                    /// <param name="content">The message content</param>
                    public Task Send<T>(IProtocolClientSide protocol, string message, T content) where T : ISerializable
                    {
                        if (protocol == null)
                        {
                            throw new ArgumentNullException("protocol");
                        }

                        if (!IsRunning)
                        {
                            throw new InvalidOperationException("The endpoint is not running - No data can be sent");
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

                        return endpoint.Send(protocolId, messageTag, content);
                    }

                    /// <summary>
                    ///   Sends a message through the network. This function is asynchronous
                    ///   and will wait until no other messages are pending to be sent.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <param name="protocol">The protocol for this message. It must be an already attached component</param>
                    /// <param name="message">The message (as it was registered) being sent</param>
                    /// <param name="content">The message content</param>
                    public Task Send(IProtocolClientSide protocol, string message)
                    {
                        return Send(protocol, message, Nothing.Instance);
                    }

                    /// <summary>
                    ///   Sends a message through the network. This function is asynchronous
                    ///   and will wait until no other messages are pending to be sent.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <typeparam name="ProtocolType">The protocol type for this message. One instance of it must be an already attached component</param>
                    /// <param name="message">The message (as it was registered) being sent</param>
                    /// <param name="content">The message content</param>
                    public Task Send<ProtocolType, T>(string message, T content) where ProtocolType : IProtocolClientSide where T : ISerializable
                    {
                        ProtocolType protocol = GetComponent<ProtocolType>();
                        if (protocol == null)
                        {
                            throw new UnknownProtocolException($"This object does not have a protocol of type {protocol.GetType().FullName} attached to it");
                        }
                        else
                        {
                            return Send(protocol, message, content);
                        }
                    }

                    /// <summary>
                    ///   Sends a message through the network. This function is asynchronous
                    ///   and will wait until no other messages are pending to be sent. The
                    ///   message has no content (i.e. it is of <see cref="Nothing"/> type).
                    /// </summary>
                    /// <typeparam name="ProtocolType">The protocol type for this message. One instance of it must be an already attached component</param>
                    /// <param name="message">The message (as it was registered) being sent</param>
                    public Task Send<ProtocolType>(string message) where ProtocolType : IProtocolClientSide
                    {
                        return Send<ProtocolType, Nothing>(message, Nothing.Instance);
                    }

                    /// <summary>
                    ///   Creates a sender shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <param name="protocol">The protocol to use in the returned sender function</param>
                    /// <param name="message">The message (as it was registered) that this sender will send</param>
                    public Func<T, Task> MakeSender<T>(IProtocolClientSide protocol, string message) where T : ISerializable
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

                        return (content) =>
                        {
                            if (!IsRunning)
                            {
                                throw new InvalidOperationException("The endpoint is not running - No data can be sent");
                            }

                            if (content.GetType() != expectedType)
                            {
                                throw new OutgoingMessageTypeMismatchException($"Outgoing message ({protocol.GetType().FullName}, {message}) was attempted with type {content.GetType().FullName} when {expectedType.FullName} was expected");
                            }

                            return endpoint.Send(protocolId, messageTag, content);
                        };
                    }

                    /// <summary>
                    ///   Creates a sender shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once. The message does not have
                    ///   any body.
                    /// </summary>
                    /// <param name="protocol">The protocol to use in the returned sender function</param>
                    /// <param name="message">The message (as it was registered) that this sender will send</param>
                    public Func<Task> MakeSender(IProtocolClientSide protocol, string message)
                    {
                        Func<Nothing, Task> sender = MakeSender<Nothing>(protocol, message);
                        return () => sender(Nothing.Instance);
                    }

                    /// <summary>
                    ///   Creates a sender shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once.
                    /// </summary>
                    /// <typeparam name="ProtocolType">The protocol type for this message. One instance of it must be an already attached component</param>
                    /// <typeparam name="T">The type of the message this sender will send</typeparam>
                    /// <param name="message">The name of the message this sender will send</param>
                    /// <returns>A function that takes the message to send, of the appropriate type, and sends it (asynchronously)</returns>
                    public Func<T, Task> MakeSender<ProtocolType, T>(string message) where ProtocolType : IProtocolClientSide where T : ISerializable
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
                    ///   and spend time on message mapping only once. The message does not have
                    ///   any body.
                    /// </summary>
                    /// <typeparam name="ProtocolType">The protocol type for this message. One instance of it must be an already attached component</param>
                    /// <typeparam name="T">The type of the message this sender will send</typeparam>
                    /// <param name="message">The name of the message this sender will send</param>
                    /// <returns>A function that takes the message to send, of the appropriate type, and sends it (asynchronously)</returns>
                    public Func<Task> MakeSender<ProtocolType>(string message) where ProtocolType : IProtocolClientSide
                    {
                        Func<Nothing, Task> sender = MakeSender<ProtocolType, Nothing>(message);
                        return () => sender(Nothing.Instance);
                    }

                    /// <summary>
                    ///   Closes the active connection, if any. This, actually,
                    ///   tells the thread to close the connection.
                    /// </summary>
                    public void Close()
                    {
                        if (!IsRunning)
                        {
                            throw new InvalidOperationException("The socket is not connected - It cannot be closed");
                        }

                        endpoint.Close();
                    }

                    /// <summary>
                    ///   An event to attend when this client established a connection (at UI level, typically).
                    /// </summary>
                    public event Action OnConnected;
                    
                    /// <summary>
                    ///   An event to attend when this client ends its connection (at UI level, typically).
                    ///   It is NOT triggered when the client fails to connect.
                    /// </summary>
                    public event Action OnDisconnected;
                }
            }
        }
    }
}
