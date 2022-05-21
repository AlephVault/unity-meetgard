using AlephVault.Unity.Binary;
using AlephVault.Unity.Meetgard.Protocols;
using AlephVault.Unity.Meetgard.Types;
using AlephVault.Unity.Support.Authoring.Behaviours;
using AlephVault.Unity.Support.Utils;
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
            namespace Client
            {
                /// <summary>
                ///   <para>
                ///     A protocol client side is the implementation
                ///     for the clients using this protocol.
                ///   </para>
                ///   <para>
                ///     It is related to a particular protocol definition.
                ///   </para>
                /// </summary>
                [RequireComponent(typeof(NetworkClient))]
                // TODO Later, ensure NetworkClient is abstract and we have
                // TODO both NetworkRemoteClient and NetworkLocalClient.
                [DisallowMultipleComponent]
                public abstract class ProtocolClientSide<Definition> : MonoBehaviour, IProtocolClientSide where Definition : ProtocolDefinition, new()
                {
                    // The related network client.
                    protected NetworkClient client;

                    /// <summary>
                    ///   The related queue manager.
                    /// </summary>
                    public AsyncQueueManager QueueManager => client.QueueManager;

                    // A handler for when an error occurs while sending
                    // a message (useful via send only).
                    protected Func<System.Exception, Task> OnSendError;

                    // The protocol definition instance is created on construction.
                    private Definition definition = new Definition();

                    // The handlers for this protocol. The action is already wrapped
                    // to refer the current protocol.
                    private Func<ISerializable, Task>[] incomingMessageHandlers = null;

                    /// <summary>
                    ///   See <see cref="NetworkClient.MaxMessageSize"/>.
                    /// </summary>
                    protected ushort MaxSocketMessageSize => client.MaxMessageSize;

                    // Initializes the handlers, according to its definition.
                    protected void Awake()
                    {
                        client = GetComponent<NetworkClient>();
                        incomingMessageHandlers = new Func<ISerializable, Task>[definition.ServerMessagesCount()];
                        Setup();
                    }

                    /// <summary>
                    ///   An after-awake setup.
                    /// </summary>
                    protected virtual void Setup()
                    {
                    }

                    private void Start()
                    {
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
                    ///   Implement this method with several calls to <see cref="AddIncomingMessageHandler{T}(string, Action{ProtocolClientSide{Definition}, T})"/>.
                    /// </summary>
                    protected virtual void SetIncomingMessageHandlers() {}

                    /// <summary>
                    ///   Adds a handler to a defined incoming message. The handler to
                    ///   add must also allow a reference to the protocol as a generic
                    ///   parent class reference.
                    /// </summary>
                    /// <typeparam name="T">The tpye of the message's content</typeparam>
                    /// <param name="message">The message name</param>
                    /// <param name="handler">The handler to register</param>
                    protected void AddIncomingMessageHandler<T>(string message, Func<ProtocolClientSide<Definition>, T, Task> handler) where T : ISerializable
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
                            incomingMessageTag = definition.GetServerMessageTagByName(message);
                            expectedIncomingMessageType = definition.GetServerMessageTypeByName(message);
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
                            incomingMessageHandlers[incomingMessageTag] = (content) => handler(this, (T)content);
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
                    protected void AddIncomingMessageHandler(string message, Func<ProtocolClientSide<Definition>, Task> handler)
                    {
                        AddIncomingMessageHandler<Nothing>(message, (proto, _) => handler(proto));
                    }

                    /// <summary>
                    ///   Creates a sender shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once. Intended to be used on
                    ///   lazy initialization of senders, or eager initializationin some sort of
                    ///   extended <see cref="Awake"/> or similar method.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <param name="message">The message (as it was registered) that this sender will send</param>
                    protected Func<T, Task> MakeSender<T>(string message) where T : ISerializable
                    {
                        return client.MakeSender<T>(this, message);
                    }

                    /// <summary>
                    ///   Creates a sender shortcut, intended to send the message multiple times
                    ///   and spend time on message mapping only once. Intended to be used on
                    ///   lazy initialization of senders, or eager initializationin some sort of
                    ///   extended <see cref="Awake"/> or similar method. The message does not have
                    ///   any body.
                    /// </summary>
                    /// <param name="message">The message (as it was registered) that this sender will send</param>
                    protected Func<Task> MakeSender(string message)
                    {
                        Func<Nothing, Task> sender = MakeSender<Nothing>(message);
                        return () => sender(Nothing.Instance);
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
                    protected Func<T, Task> MakeSender<ProtocolType, T>(string message) where ProtocolType : IProtocolClientSide where T : ISerializable
                    {
                        return client.MakeSender<ProtocolType, T>(message);
                    }

                    /// <summary>
                    ///   Creates a message container for an incoming server message,
                    ///   with a particular inner message tag.
                    /// </summary>
                    /// <param name="tag">The message tag to get the container for</param>
                    /// <returns>The message container</returns>
                    public ISerializable NewMessageContainer(ushort tag)
                    {
                        try
                        {
                            Type messageType = definition.GetServerMessageTypeByTag(tag);
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
                            return definition.GetClientMessageTagByName(message);
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
                            return definition.GetClientMessageTypeByTag(tag);
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
                    public Func<ISerializable, Task> GetIncomingMessageHandler(ushort tag)
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
                    /// <param name="content">The content of the message being sent</param>
                    public Task Send<T>(string message, T content) where T : ISerializable
                    {
                        return client.Send(this, message, content);
                    }

                    /// <summary>
                    ///   Sends a message using this protocol. The type must match
                    ///   whatever was used to register the message.
                    /// </summary>
                    /// <typeparam name="T">The type of the message being sent</typeparam>
                    /// <param name="message">The name of the message being sent</param>
                    /// <param name="content">The content of the message being sent</param>
                    public Task Send<ProtocolType, T>(string message, T content)
                        where ProtocolType : IProtocolClientSide
                        where T : ISerializable
                    {
                        return client.Send<ProtocolType, T>(message, content);
                    }

                    /// <summary>
                    ///   Runs an action in the main unity thread.
                    /// </summary>
                    /// <param name="action">The action to run</param>
                    /// <returns>A task to wait for</returns>
                    public Task RunInMainThread(Action action)
                    {
                        return client.QueueManager.Queue(action);
                    }

                    /// <summary>
                    ///   Runs an async action in the main unity thread.
                    /// </summary>
                    /// <param name="action">The action to run</param>
                    /// <returns>A task to wait for</returns>
                    public Task RunInMainThread(Func<Task> task)
                    {
                        return client.QueueManager.Queue(task);
                    }

                    /// <summary>
                    ///   Runs a typed async action in the main unity thread.
                    /// </summary>
                    /// <param name="action">The typed action to run</param>
                    /// <returns>A typed task to wait for</returns>
                    public Task<T> RunInMainThread<T>(Func<Task<T>> task)
                    {
                        return client.QueueManager.Queue(task);
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
                    ///     This is a callback that gets invoked when the client successfully
                    ///     established a connection to a server.
                    ///   </para>
                    ///   <para>
                    ///     Override it at need.
                    ///   </para>
                    /// </summary>
                    public virtual async Task OnConnected()
                    {
                    }

                    /// <summary>
                    ///   <para>
                    ///     This is a callback that gets invoked when the client is disconnected
                    ///     from the server. This can happen gracefully locally, gracefully remotely,
                    ///     or abnormally.
                    ///   </para>
                    ///   <para>
                    ///     Override it at need.
                    ///   </para>
                    /// </summary>
                    /// <param name="reason">If not null, tells the abnormal reason of closure</param>
                    public virtual async Task OnDisconnected(System.Exception reason)
                    {
                    }
                }
            }
        }
    }
}