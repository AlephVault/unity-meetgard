using System;
using System.Threading.Tasks;
using AlephVault.Unity.Meetgard.Protocols;
using AlephVault.Unity.Support.Utils;

namespace AlephVault.Unity.Meetgard
{
    namespace Authoring
    {
        namespace Behaviours
        {
            namespace Client
            {
                /// <summary>
                ///   This is the client-side implementation of a mandatory
                ///   handshake protocol. In this case, the client-side
                ///   will have a Welcome and a Timeout event, which are
                ///   triggered on the respective messages.
                /// </summary>
                public class MandatoryHandshakeProtocolClientSide : ProtocolClientSide<MandatoryHandshakeProtocolDefinition>
                {
                    protected override void SetIncomingMessageHandlers()
                    {
                        AddIncomingMessageHandler("Welcome", async (proto) =>
                        {
                            // The OnWelcome event is triggered. The implementation
                            // must, as fast as it can, invoke athe expected method
                            // (i.e. handshake message) in this event handler.
                            await (OnWelcome?.InvokeAsync() ?? Task.CompletedTask);
                        });
                        AddIncomingMessageHandler("Timeout", async (proto) =>
                        {
                            // The OnTimeout event is triggered. This is weird to
                            // occur if the OnWelcome event implements the sending
                            // of the messages immediately. Nevertheless, the event
                            // exists because it is triggered. Expect a disconnection
                            // after this event triggers.
                            client.Close();
                            await (OnTimeout?.InvokeAsync() ?? Task.CompletedTask);
                        });
                    }
                    
                    /// <summary>
                    ///   Triggered when a Welcome message is received. This message
                    ///   is received after immediately connecting: it is the first
                    ///   message received from the server. This message should be
                    ///   listened to send the expected handshake message as fastest
                    ///   as possible.
                    /// </summary>
                    public event Func<Task> OnWelcome = null;

                    /// <summary>
                    ///   Triggered when the server determines the client did not
                    ///   send the handshake after a specified time.
                    /// </summary>
                    public event Func<Task> OnTimeout = null;
                }
            }
        }
    }
}
