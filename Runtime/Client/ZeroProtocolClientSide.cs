using AlephVault.Unity.Meetgard.Protocols;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Client
    {
        /// <summary>
        ///   Client-side implementation for the "zero" protocol.
        /// </summary>
        public class ZeroProtocolClientSide : ProtocolClientSide<ZeroProtocolDefinition>
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
            ///   Tells whether this client is ready or not (i.e.
            ///   whether it passed the version check for the current
            ///   connection, or not).
            /// </summary>
            public bool Ready { get; private set; }

            private Func<Protocols.Version, Task> SendMyVersion;

            protected void Start()
            {
                SendMyVersion = MakeSender<Protocols.Version>("MyVersion");
            }

            public override async Task OnConnected()
            {
                Ready = false;
            }

            public override async Task OnDisconnected(System.Exception reason)
            {
                Ready = false;
            }

            protected override void SetIncomingMessageHandlers()
            {
                AddIncomingMessageHandler("LetsAgree", async (proto) =>
                {
                    await SendMyVersion(Version);
                    // This will be invoked after the client repied with MyVersion
                    // message. This means: after the handshake started in client
                    // (protocol-wise) side.
                    await (OnZeroHandshakeStarted?.Invoke() ?? Task.CompletedTask);
                });
                AddIncomingMessageHandler("Timeout", async (proto) =>
                {
                    // This may be invoked regardless the LetsAgree being received
                    // or the MyVersion message being sent. This is due to the
                    // client taking too long to respond to LetsAgree message.
                    // Expect a disconnection after this message.
                    await (OnTimeout?.Invoke() ?? Task.CompletedTask);
                });
                AddIncomingMessageHandler("VersionMatch", async (proto) =>
                {
                    // The version was matched. Don't worry: we will seldom make
                    // use of this event, since typically other protocols will
                    // in turn initialize on their own for this client and send
                    // their own messages. But it is available anyway.
                    Ready = true;
                    await (OnVersionMatch?.Invoke() ?? Task.CompletedTask);
                });
                AddIncomingMessageHandler("VersionMismatch", async (proto) =>
                {
                    // This message is received when there is a mismatch between
                    // the server version and the client version. After receiving
                    // this message, expect a sudden graceful disconnection.
                    await (OnVersionMismatch?.Invoke() ?? Task.CompletedTask);
                });
                AddIncomingMessageHandler("NotReady", async (proto) =>
                {
                    // This is a debug message. Typically, it involves rejecting
                    // any message other than MyVersion, since the protocols are
                    // not ready for this client (being ready occurs after
                    // agreeing with this zero protocol).
                    await (OnNotReadyError?.Invoke() ?? Task.CompletedTask);
                });
                AddIncomingMessageHandler("AlreadyDone", async (proto) =>
                {
                    // This is a debug message. Typically, it will never occur.
                    // It involved rejecting a MyVersion message because the
                    // handshake is already done. This message is harmless.
                    await (OnAlreadyDoneError?.Invoke() ?? Task.CompletedTask);
                });
            }

            /// <summary>
            ///   Triggered when the client received a LetsAgree message and replied
            ///   with MyVersion message.
            /// </summary>
            public event Func<Task> OnZeroHandshakeStarted = null;

            /// <summary>
            ///   Triggered when the client received the notification that the
            ///   version handshake was correct.
            /// </summary>
            public event Func<Task> OnVersionMatch = null;

            /// <summary>
            ///   Triggered when the client received the notification that the
            ///   version handshake was incorrect. Expect a sudden yet graceful
            ///   disconnection after this message.
            /// </summary>
            public event Func<Task> OnVersionMismatch = null;

            /// <summary>
            ///   Triggered when the client attempted any message other than
            ///   MyVersion message while the handshake is still not successfully
            ///   completed in either side.
            /// </summary>
            public event Func<Task> OnNotReadyError = null;

            /// <summary>
            ///   Triggered when the client sent another MyVersion message,
            ///   but the server had previously approved the handhske for
            ///   this client connection.
            /// </summary>
            public event Func<Task> OnAlreadyDoneError = null;

            /// <summary>
            ///   Triggered when the client received the notification that the
            ///   version handshake did not occur after a tolerance time, perhaps
            ///   due to malicius attempts or networking problems. Expect a sudden
            ///   yet graceful disconnection after this message.
            /// </summary>
            public event Func<Task> OnTimeout = null;
        }
    }
}