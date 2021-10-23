using AlephVault.Unity.Binary;
using AlephVault.Unity.Meetgard.Protocols;
using AlephVault.Unity.Meetgard.Types;
using AlephVault.Unity.Support.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                ///   <para>
                ///     A protocol server side is the implementation
                ///     for the servers using this protocol.
                ///   </para>
                ///   <para>
                ///     It is related to a particular protocol definition.
                ///   </para>
                /// </summary>
                [RequireComponent(typeof(NetworkServer))]
                [DisallowMultipleComponent]
                public abstract class ProtocolServerSide<Definition> : MonoBehaviour, IProtocolServerSide where Definition : ProtocolDefinition, new()
                {
                    // The related network server.
                    protected NetworkServer server;

                    // A handler for when an error occurs while sending
                    // a message (useful via send or broadcast).
                    protected Func<System.Exception, Task> OnSendError;

                    // The protocol definition instance is created on construction.
                    private Definition definition = new Definition();

                    // The handlers for this protocol. The action is already wrapped
                    // to refer the current protocol.
                    private Func<ulong, ISerializable, Task>[] incomingMessageHandlers = null;

                    /// <summary>
                    ///   See <see cref="NetworkServer.MaxMessageSize"/>.
                    /// </summary>
                    protected ushort MaxSocketMessageSize => server.MaxMessageSize;

                    // Initializes the handlers, according to its definition.
                    protected void Awake()
                    {
                        server = GetComponent<NetworkServer>();
                        incomingMessageHandlers = new Func<ulong, ISerializable, Task>[definition.ClientMessagesCount()];
                        try
                        {
                            Initialize();
                            SetIncomingMessageHandlers();
                        }
                        catch (System.Exception)
                        {
                            Destroy(gameObject);
                            throw;
                        }
                    }

                    /// <summary>
                    ///   Performs the initialization of the protocol server side.
                    /// </summary>
                    protected virtual void Initialize() {}

                    /// <summary>
                    ///   Implement this method with several calls to <see cref="AddIncomingMessageHandler{T}(string, Action{ProtocolServerSide{Definition}, ulong, T})"/>.
                    /// </summary>
                    protected abstract void SetIncomingMessageHandlers();

                    /// <summary>
                    ///   Adds a handler to a defined incoming message. The handler to
                    ///   add must also allow a reference to the protocol as a generic
                    ///   parent class reference.
                    /// </summary>
                    /// <typeparam name="T">The tpye of the message's content</typeparam>
                    /// <param name="message">The message name</param>
                    /// <param name="handler">The handler to register</param>
                    protected void AddIncomingMessageHandler<T>(string message, Func<ProtocolServerSide<Definition>, ulong, T, Task> handler) where T : ISerializable
                    {
                        if (message == null || message.Trim().Length == 0)
                        {
                            throw new ArgumentException("The message name must not be null or empty");
                        }

                        if (handler == null)
                        {
                            throw new ArgumentNullException("handler");
                        }

                        ushort incomingMessageTag;
                        Type expectedIncomingMessageType;
                        try
                        {
                            incomingMessageTag = definition.GetClientMessageTagByName(message);
                            expectedIncomingMessageType = definition.GetClientMessageTypeByName(message);
                        }
                        catch (KeyNotFoundException)
                        {
                            throw new UnexpectedMessageException($"The protocol definition of type {typeof(Definition).FullName} does not define a message: {message}");
                        }

                        if (expectedIncomingMessageType != typeof(T))
                        {
                            throw new IncomingMessageTypeMismatchException($"Incoming message ({message}) in protocol {GetType().FullName} was attempted to handle with type {typeof(T).FullName} when {expectedIncomingMessageType.FullName} was expected");
                        }

                        if (incomingMessageHandlers[incomingMessageTag] != null)
                        {
                            throw new HandlerAlreadyRegisteredException($"Incoming message ({message}) is already handled by {GetType().FullName} - cannot set an additional handler");
                        }
                        else
                        {
                            incomingMessageHandlers[incomingMessageTag] = (clientId, content) => handler(this, clientId, (T)content);
                        }
                    }

                    /// <summary>
                    ///   Adds a handler to a defined incoming message. The handler to
                    ///   add must also allow a reference to the protocol as a generic
                    ///   parent class reference. The handler is for a message without
                    ///   any body.
                    /// </summary>
                    /// <param name="message">The message name</param>
                    /// <param name="handler">The handler to register</param>
                    protected void AddIncomingMessageHandler(string message, Func<ProtocolServerSide<Definition>, ulong, Task> handler)
                    {
                        AddIncomingMessageHandler<Nothing>(message, (proto, clientIds, _) => handler(proto, clientIds));
                    }

                    /// <summary>
                    ///   Creates a sender shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once. Intended to be used on
                    ///   lazy initialization of senders, or eager initializationin some sort of
                    ///   extended <see cref="Awake"/> or similar method.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <param name="message">The message (as it was registered) that this sender will send</param>
                    protected Func<ulong, T, Task> MakeSender<T>(string message) where T : ISerializable
                    {
                        return server.MakeSender<T>(this, message);
                    }

                    /// <summary>
                    ///   Creates a sender shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once. Intended to be used on
                    ///   lazy initialization of senders, or eager initializationin some sort of
                    ///   extended <see cref="Awake"/> or similar method. The message to send
                    ///   does not have any body.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <param name="message">The message (as it was registered) that this sender will send</param>
                    /// <returns>A function that takes the client to send the message, and sends the message (asynchronously)</returns>
                    protected Func<ulong, Task> MakeSender(string message)
                    {
                        return server.MakeSender(this, message);
                    }

                    /// <summary>
                    ///   Creates a sender shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once. Intended to be used on
                    ///   lazy initialization of senders, or eager initializationin some sort of
                    ///   extended <see cref="Awake"/> or similar method.
                    /// </summary>
                    /// <typeparam name="ProtocolType">The protocol type for this message. One instance of it must be an already attached component</param>
                    /// <typeparam name="T">The type of the message this sender will send</typeparam>
                    /// <param name="message">The name of the message this sender will send</param>
                    /// <returns>A function that takes the message to send, of the appropriate type, and sends it (asynchronously)</returns>
                    protected Func<ulong, T, Task> MakeSender<ProtocolType, T>(string message) where ProtocolType : IProtocolServerSide where T : ISerializable
                    {
                        return server.MakeSender<ProtocolType, T>(message);
                    }

                    /// <summary>
                    ///   Creates a broadcaster shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once. Intended to be used on
                    ///   lazy initialization of senders, or eager initializationin some sort of
                    ///   extended <see cref="Awake"/> or similar method.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <param name="message">The message (as it was registered) that this sender will send</param>
                    protected Func<IEnumerable<ulong>, T, Dictionary<ulong, Task>> MakeBroadcaster<T>(string message) where T : ISerializable
                    {
                        return server.MakeBroadcaster<T>(this, message);
                    }

                    /// <summary>
                    ///   Creates a broadcaster shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once. Intended to be used on
                    ///   lazy initialization of senders, or eager initializationin some sort of
                    ///   extended <see cref="Awake"/> or similar method. The message to send does
                    ///   not have any body. The message to send does not have any body.
                    /// </summary>
                    /// <param name="message">The message (as it was registered) that this sender will send</param>
                    protected Func<IEnumerable<ulong>, Dictionary<ulong, Task>> MakeBroadcaster(string message)
                    {
                        return server.MakeBroadcaster(this, message);
                    }

                    /// <summary>
                    ///   Creates a broadcaster shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once. Intended to be used on
                    ///   lazy initialization of senders, or eager initializationin some sort of
                    ///   extended <see cref="Awake"/> or similar method.
                    /// </summary>
                    /// <typeparam name="ProtocolType">The protocol type for this message. One instance of it must be an already attached component</param>
                    /// <typeparam name="T">The type of the message this sender will send</typeparam>
                    /// <param name="message">The name of the message this sender will send</param>
                    /// <returns>A function that takes the message to send, of the appropriate type, and sends it (asynchronously)</returns>
                    protected Func<IEnumerable<ulong>, T, Dictionary<ulong, Task>> MakeBroadcaster<ProtocolType, T>(string message) where ProtocolType : IProtocolServerSide where T : ISerializable
                    {
                        return server.MakeBroadcaster<ProtocolType, T>(message);
                    }

                    /// <summary>
                    ///   Creates a message container for an incoming client message,
                    ///   with a particular inner message tag.
                    /// </summary>
                    /// <param name="tag">The message tag to get the container for</param>
                    /// <returns>The message container</returns>
                    public ISerializable NewMessageContainer(ushort tag)
                    {
                        try
                        {
                            Type messageType = definition.GetClientMessageTypeByTag(tag);
                            return (ISerializable)Activator.CreateInstance(messageType);
                        }
                        catch (IndexOutOfRangeException)
                        {
                            return null;
                        }
                    }

                    /// <summary>
                    ///   For a given message name, gets the tag it acquired when
                    ///   it was registered. Returns null if absent.
                    /// </summary>
                    /// <param name="message">The name of the message to get the tag for</param>
                    /// <returns>The tag (nullable)</returns>
                    public ushort? GetOutgoingMessageTag(string message)
                    {
                        try
                        {
                            return definition.GetServerMessageTagByName(message);
                        }
                        catch (KeyNotFoundException)
                        {
                            return null;
                        }
                    }

                    /// <summary>
                    ///   Gets the type of a particular outgoing message tag. Returns
                    ///   null if the tag is not valid.
                    /// </summary>
                    /// <param name="tag">The tag to get the type for</param>
                    /// <returns>The type for the given tag</returns>
                    public Type GetOutgoingMessageType(ushort tag)
                    {
                        try
                        {
                            return definition.GetServerMessageTypeByTag(tag);
                        }
                        catch (IndexOutOfRangeException)
                        {
                            return null;
                        }
                    }

                    /// <summary>
                    ///   Gets the handler for a given requested tag. The returned
                    ///   handler already wraps an original handler also referencing
                    ///   the current protocol.
                    /// </summary>
                    /// <param name="tag">The message tag to get the handler for</param>
                    /// <returns>The message container</returns>
                    public Func<ulong, ISerializable, Task> GetIncomingMessageHandler(ushort tag)
                    {
                        try
                        {
                            return incomingMessageHandlers[tag];
                        }
                        catch (IndexOutOfRangeException)
                        {
                            return null;
                        }
                    }

                    /// <summary>
                    ///   Sends a message using another protocol. The type must match
                    ///   whatever was used to register the message. Also, the protocol
                    ///   specified in the type must exist as a sibling component.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <param name="message">The name of the message being sent</param>
                    /// <param name="clientId">The id of the client to send the message to</param>
                    /// <param name="content">The content of the message being sent</param>
                    public Task Send<T>(ulong clientId, string message, T content) where T : ISerializable
                    {
                        return server.Send(this, message, clientId, content);
                    }

                    /// <summary>
                    ///   Sends a message using this protocol. The type must match
                    ///   whatever was used to register the message.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <param name="message">The name of the message being sent</param>
                    /// <param name="clientId">The id of the client to send the message to</param>
                    /// <param name="content">The content of the message being sent</param>
                    public Task Send<ProtocolType, T>(string message, ulong clientId, T content)
                        where ProtocolType : IProtocolServerSide
                        where T : ISerializable
                    {
                        return server.Send<ProtocolType, T>(message, clientId, content);
                    }

                    /// <summary>
                    ///   Broadcasts a message using another protocol. The type must match
                    ///   whatever was used to register the message. Also, the protocol
                    ///   specified in the type must exist as a sibling component.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <param name="message">The message (as it was registered) being sent</param>
                    /// <param name="clientIds">The ids to send the same message - use null to specify ALL the available ids</param>
                    /// <param name="content">The message content</param>
                    /// <returns>The send tasks for each endpoint that was iterated</returns>
                    public Dictionary<ulong, Task> Broadcast<T>(string message, IEnumerable<ulong> clientIds, T content) where T : ISerializable
                    {
                        return server.Broadcast(this, message, clientIds, content);
                    }

                    /// <summary>
                    ///   Broadcasts a message using this protocol. The type must match
                    ///   whatever was used to register the message.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <param name="message">The message (as it was registered) being sent</param>
                    /// <param name="clientIds">The ids to send the same message - use null to specify ALL the available ids</param>
                    /// <param name="content">The message content</param>
                    /// <returns>The send tasks for each endpoint that was iterated</returns>
                    public Dictionary<ulong, Task> Broadcast<ProtocolType, T>(string message, IEnumerable<ulong> clientIds, T content)
                        where ProtocolType : IProtocolServerSide
                        where T : ISerializable
                    {
                        return server.Broadcast<ProtocolType, T>(message, clientIds, content);
                    }

                    /// <summary>
                    ///   This task iterates over all of the broadcast tasks, awaiting to be done.
                    ///   Null ones are omitted, and errors are handled one by one by the internal
                    ///   event <see cref="OnSendError"/>.
                    /// </summary>
                    /// <param name="tasks">The dictionary clientId => (task?)</param>
                    protected Task UntilBroadcastIsDone(Dictionary<ulong, Task> tasks)
                    {
                        return Tasks.UntilAllDone(tasks.Values, OnSendError);
                    }

                    /// <summary>
                    ///   This task wraps another task (typically, a "send" one), awaiting to be done.
                    ///   If the task is null, it will be ignored. Any error while awaiting will be
                    ///   handled by the internal event <see cref="OnSendError"/>.
                    /// </summary>
                    /// <param name="task">The task (a possibly null one)</param>
                    protected Task UntilSendIsDone(Task task)
                    {
                        return Tasks.UntilDone(task, OnSendError);
                    }

                    /// <summary>
                    ///   <para>
                    ///     This is a callback that gets invoked when a client successfully
                    ///     established a connection to this server.
                    ///   </para>
                    ///   <para>
                    ///     Override it at need.
                    ///   </para>
                    /// </summary>
                    public virtual async Task OnConnected(ulong clientId)
                    {
                    }

                    /// <summary>
                    ///   <para>
                    ///     This is a callback that gets invoked when a client is disconnected
                    ///     from the server. This can happen gracefully locally, gracefully
                    ///     remotely, or abnormally.
                    ///   </para>
                    ///   <para>
                    ///     Override it at need.
                    ///   </para>
                    /// </summary>
                    /// <param name="reason">If not null, tells the abnormal reason of closure</param>
                    public virtual async Task OnDisconnected(ulong clientId, System.Exception reason)
                    {
                    }

                    /// <summary>
                    ///   This is a callback that gets invoked when the server has just started.
                    /// </summary>
                    public virtual async Task OnServerStarted()
                    {
                    }

                    /// <summary>
                    ///   This is a callback that gets invoked when the server is (and previously
                    ///   all the client connections are as well) told to stop.
                    /// </summary>
                    /// <param name="e"></param>
                    public virtual async Task OnServerStopped(System.Exception e)
                    {
                    }
                }
            }
        }
    }
}
