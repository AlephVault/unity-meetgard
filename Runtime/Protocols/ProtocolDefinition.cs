using AlephVault.Unity.Binary;
using System;
using System.Linq;
using System.Collections.Generic;
using AlephVault.Unity.Meetgard.Types;

namespace AlephVault.Unity.Meetgard
{
    namespace Protocols
    {
        /// <summary>
        ///   <para>
        ///     A protocol has a list of messages that can
        ///     be sent from client to server and vice versa.
        ///     They are different pairs of dictionaries, as
        ///     they are messages in opposite directions.
        ///   </para>
        ///   <para>
        ///     Reverse definitions to get an integer tag
        ///     by the message name, and vice versal will
        ///     be available as well.
        ///   </para>
        ///   <para>
        ///     This all makes this class suitable to be
        ///     distributed with both the server and the
        ///     client projects. It will be also required,
        ///     since the protocol implementations (which
        ///     may belong each to a different project)
        ///     need to make use of the definition to be
        ///     implemented.
        ///   </para>
        /// </summary>
        public abstract class ProtocolDefinition
        {
            // While the protocol will not be defined the
            // handling in this class, the message types
            // will be defined here.

            // The client messages are those that will be
            // sent by the client and handled by the server.
            // While the messages' implementations will not
            // be defined here, their types will.
            private SortedDictionary<string, Type> registeredClientMessageTypeByName = new SortedDictionary<string, Type>();

            // Each client message name will be mapped against
            // the tag it will have. These tags are known in
            // the object's construction, right after the
            // messages are defined and it is locked from any
            // further definition.
            private Dictionary<string, ushort> registeredClientMessageTagByName = new Dictionary<string, ushort>();

            // Each client message type will be mapped from
            // the tag it will have. These tags are known in
            // the object's construction, right ater the
            // messages are defined and it is locked from any
            // further definition.
            private Type[] registeredClientMessageTypeByTag = null;

            // Each client message name will be mapped from
            // the tag it will have. These tags are known in
            // the object's construction, right after the
            // messages are defined and it is locked from any
            // further definition.
            private string[] registeredClientMessageNameByTag = null;

            // The server messages are those that will be
            // sent by the server and handled by the client.
            // While the messages' implementations will not
            // be defined here, their types will.
            private SortedDictionary<string, Type> registeredServerMessageTypeByName = new SortedDictionary<string, Type>();

            // Each server message name will be mapped against
            // the tag it will have. These tags are known in
            // the object's construction, right after the
            // messages are defined and it is locked from any
            // further definition.
            private Dictionary<string, ushort> registeredServerMessageTagByName = new Dictionary<string, ushort>();

            // Each server message type will be mapped from
            // the tag it will have. These tags are known in
            // the object's construction, right after the
            // messages are defined and it is locked from any
            // further definition.
            private Type[] registeredServerMessageTypeByTag = null;

            // Each server message name will be mapped from
            // the tag it will have. These tags are known in
            // the object's construction, right after the
            // messages are defined and it is locked from any
            // further definition.
            private string[] registeredServerMessageNameByTag = null;

            // Tells whether the definition is already done.
            // This flag is true after the object is fully
            // constructed.
            private bool isDefined = false;

            public ProtocolDefinition()
            {
                DefineMessages();
                isDefined = true;
                registeredClientMessageNameByTag = registeredClientMessageTypeByName.Keys.ToArray();
                registeredClientMessageTypeByTag = registeredClientMessageTypeByName.Values.ToArray();
                for (ushort i = 0; i < registeredClientMessageNameByTag.Length; i++)
                {
                    registeredClientMessageTagByName.Add(registeredClientMessageNameByTag[i], i);
                }
                registeredServerMessageNameByTag = registeredServerMessageTypeByName.Keys.ToArray();
                registeredServerMessageTypeByTag = registeredServerMessageTypeByName.Values.ToArray();
                for (ushort i = 0; i < registeredServerMessageNameByTag.Length; i++)
                {
                    registeredServerMessageTagByName.Add(registeredServerMessageNameByTag[i], i);
                }
            }

            // This function checks via reflection whether a given type:
            // 1. Is not null.
            // 2. Respects the setting: ISerializable, new().
            // It explodes with an error otherwise.
            private static void CheckValidType(Type type)
            {
                if (type == null) throw new ArgumentNullException("type");

                if (!typeof(ISerializable).IsAssignableFrom(type) || (!type.IsValueType && type.GetConstructor(Type.EmptyTypes) == null))
                {
                    throw new ArgumentException("The given type does not implement ISerializable or does not have an empty-args constructor");
                }
            }

            /// <summary>
            ///   Implement this method with several calls
            ///   to <see cref="DefineServerMessage{T}(string)"/>
            ///   and <see cref="DefineClientMessage{T}(string)"/>.
            /// </summary>
            protected abstract void DefineMessages();

            /// <summary>
            ///   Registers a client message using a particular
            ///   serializable type.
            /// </summary>
            /// <typeparam name="T">The type of the message's content</typeparam>
            /// <param name="messageKey">The message's key</param>
            protected void DefineClientMessage<T>(string messageKey) where T : ISerializable, new()
            {
                DefineMessage(messageKey, "client", registeredClientMessageTypeByName, typeof(T));
            }

            /// <summary>
            ///   Registers a client message using a particular
            ///   serializable type.
            /// </summary>
            /// <param name="messageKey">The message's key</param>
            /// <param name="messageType">The type of the message's content</param>
            protected void DefineClientMessage(string messageKey, Type messageType)
            {
                CheckValidType(messageType);
                DefineMessage(messageKey, "client", registeredClientMessageTypeByName, messageType);
            }

            /// <summary>
            ///   Registers a client message without body (i.e.
            ///   using the special <see cref="Nothing"/> type
            ///   as serializable type).
            /// </summary>
            /// <param name="messageKey">The message's key</param>
            protected void DefineClientMessage(string messageKey)
            {
                DefineClientMessage<Nothing>(messageKey);
            }

            /// <summary>
            ///   Registers a server message using a particular
            ///   serializable type.
            /// </summary>
            /// <typeparam name="T">The type of the message's content</typeparam>
            /// <param name="messageKey">The message's key</param>
            protected void DefineServerMessage<T>(string messageKey) where T : ISerializable, new()
            {
                DefineMessage(messageKey, "server", registeredServerMessageTypeByName, typeof(T));
            }

            /// <summary>
            ///   Registers a server message using a particular
            ///   serializable type.
            /// </summary>
            /// <param name="messageKey">The message's key</param>
            /// <param name="messageType">The type of the message's content</param>
            protected void DefineServerMessage(string messageKey, Type messageType)
            {
                CheckValidType(messageType);
                DefineMessage(messageKey, "server", registeredServerMessageTypeByName, messageType);
            }

            /// <summary>
            ///   Registers a server message without body (i.e.
            ///   using the special <see cref="Nothing"/> type
            ///   as serializable type).
            /// </summary>
            /// <param name="messageKey">The message's key</param>
            protected void DefineServerMessage(string messageKey)
            {
                DefineServerMessage<Nothing>(messageKey);
            }

            // Registers a message using a particular serializable
            // type, and a particular context.
            private void DefineMessage(string messageKey, string scope, SortedDictionary<string, Type> messages, Type messageType)
            {
                if (isDefined)
                {
                    throw new InvalidOperationException("Messages cannot defined outside of the protocol definition constructor");
                }

                if (messages.Count >= ushort.MaxValue)
                {
                    throw new InvalidOperationException($"This protocol has already {ushort.MaxValue} values registered in the {scope} side. No more messages can be registered there");
                }

                if (messageKey == null)
                {
                    throw new ArgumentNullException("messageKey");
                }

                messageKey = messageKey.Trim();
                if (messageKey == "")
                {
                    throw new ArgumentException("Message key is empty");
                }

                if (messages.ContainsKey(messageKey))
                {
                    throw new ArgumentException($"Message key already registered as a {scope} message: {messageKey}");
                }
                messages[messageKey] = messageType;
            }

            /// <summary>
            ///   Gets a registered client message's type. The type
            ///   will be an ISerializable implementor.
            /// </summary>
            /// <param name="messageKey">The message's key</param>
            /// <returns>The type of the message's content</returns>
            public Type GetClientMessageTypeByName(string messageKey)
            {
                return GetMessageTypeByName(messageKey, registeredClientMessageTypeByName);
            }

            /// <summary>
            ///   Gets a registered server message's type. The type
            ///   will be an ISerializable implementor.
            /// </summary>
            /// <param name="messageKey">The message's key</param>
            /// <returns>The type of the message's content</returns>
            public Type GetServerMessageTypeByName(string messageKey)
            {
                return GetMessageTypeByName(messageKey, registeredServerMessageTypeByName);
            }

            // Gets a registered message's type. The type will be
            // an ISerializable implementor.
            private Type GetMessageTypeByName(string messageKey, SortedDictionary<string, Type> messages)
            {
                return messages[messageKey];
            }

            /// <summary>
            ///   Returns a key-value pair over all of the registered client messages.
            /// </summary>
            /// <returns>An enumerator over all of the client message types</returns>
            public SortedDictionary<string, Type>.Enumerator GetClientMessageTypes()
            {
                return registeredClientMessageTypeByName.GetEnumerator();
            }

            /// <summary>
            ///   Returns a key-value pair over all of the registered server messages.
            /// </summary>
            /// <returns>An enumerator over all of the server message types</returns>
            public SortedDictionary<string, Type>.Enumerator GetServerMessageTypes()
            {
                return registeredServerMessageTypeByName.GetEnumerator();
            }

            /// <summary>
            ///   Gets the corresponding tag for a server message.
            ///   This is needed both when sending a message and
            ///   when installing a handler for an incoming message
            ///   (this is specified by its tag).
            /// </summary>
            /// <param name="messageKey">The key of the message of our interest</param>
            /// <returns>The tag that will be sent or mapped</returns>
            public ushort GetServerMessageTagByName(string messageKey)
            {
                return registeredServerMessageTagByName[messageKey];
            }

            /// <summary>
            ///   Gets the corresponding tag for a client message.
            ///   This is needed both when sending a message and
            ///   when installing a handler for an incoming message
            ///   (this is specified by its tag).
            /// </summary>
            /// <param name="messageKey">The key of the message of our interest</param>
            /// <returns>The tag that will be sent or mapped</returns>
            public ushort GetClientMessageTagByName(string messageKey)
            {
                return registeredClientMessageTagByName[messageKey];
            }

            /// <summary>
            ///   Gets the corresponding type for a server message tag.
            /// </summary>
            /// <param name="tag">The message tag to get the underlying type for</param>
            /// <returns>The message type corresponding to that tag</returns>
            public Type GetServerMessageTypeByTag(ushort tag)
            {
                return registeredServerMessageTypeByTag[tag];
            }

            /// <summary>
            ///   Gets the corresponding type for a client message tag.
            /// </summary>
            /// <param name="tag">The message tag to get the underlying type for</param>
            /// <returns>The message type corresponding to that tag</returns>
            public Type GetClientMessageTypeByTag(ushort tag)
            {
                return registeredClientMessageTypeByTag[tag];
            }

            /// <summary>
            ///   Gets the corresponding name for a server message tag.
            /// </summary>
            /// <param name="tag">The message tag to get the underlying type for</param>
            /// <returns>The message name corresponding to that tag</returns>
            public string GetServerMessageNameByTag(ushort tag)
            {
                return registeredServerMessageNameByTag[tag];
            }

            /// <summary>
            ///   Gets the corresponding name for a client message tag.
            /// </summary>
            /// <param name="tag">The message tag to get the underlying type for</param>
            /// <returns>The message name corresponding to that tag</returns>
            public string GetClientMessageNameByTag(ushort tag)
            {
                return registeredClientMessageNameByTag[tag];
            }

            /// <summary>
            ///   <para>
            ///     Given a tag, it retrieves the type of server message
            ///     corresponding to it. That type is instantiated. On
            ///     key not found (unknown tag) the result will be null.
            ///   </para>
            ///   <para>
            ///     Meant to be used by clients when receiving server messages.
            ///   </para>
            /// </summary>
            /// <param name="tag">The tag to spawn a message for</param>
            /// <returns>A new <see cref="ISerializable"/> instance, or null on unknown tag</returns>
            public ISerializable NewServerMessageContainer(ushort tag)
            {
                return NewMessageContainer(tag, registeredServerMessageTypeByTag);
            }

            /// <summary>
            ///   <para>
            ///     Given a tag, it retrieves the type of client message
            ///     corresponding to it. That type is instantiated. On
            ///     key not found (unknown tag) the result will be null.
            ///   </para>
            ///   <para>
            ///     Meant to be used by servers when receiving server messages.
            ///   </para>
            /// </summary>
            /// <param name="tag">The tag to spawn a message for</param>
            /// <returns>A new <see cref="ISerializable"/> instance, or null on unknown tag</returns>
            public ISerializable NewClientMessageContainer(ushort tag)
            {
                return NewMessageContainer(tag, registeredClientMessageTypeByTag);
            }

            // Instantiates an ISerializable object according to its tag.
            private ISerializable NewMessageContainer(ushort tag, Type[] typeByTag)
            {
                if (tag >= typeByTag.Length)
                {
                    return null;
                }
                else
                {
                    return (ISerializable)Activator.CreateInstance(typeByTag[tag]);
                }
            }

            /// <summary>
            ///   Gets the count of registered client messages.
            /// </summary>
            public int ClientMessagesCount()
            {
                return registeredClientMessageTypeByName.Count;
            }

            /// <summary>
            ///   Gets the count of registered server messages.
            /// </summary>
            public int ServerMessagesCount()
            {
                return registeredServerMessageTypeByName.Count;
            }
        }
    }
}
