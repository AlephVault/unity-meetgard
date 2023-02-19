using AlephVault.Unity.Meetgard.Protocols;
using AlephVault.Unity.Support.Utils;
using System;
using System.Threading.Tasks;


namespace AlephVault.Unity.Meetgard
{
    namespace Authoring
    {
        namespace Behaviours
        {
            namespace Client
            {
                /// <summary>
                ///   Client-side implementation for the "ping" protocol.
                ///   It answers with a "pong" message any arriving "ping"
                ///   message from the server.
                /// </summary>
                public class PingProtocolClientSide : ProtocolClientSide<PingProtocolDefinition>
                {
                    private Func<Task> SendPong;

                    protected override void Initialize()
                    {
                        SendPong = MakeSender("Pong");
                    }

                    protected override void SetIncomingMessageHandlers()
                    {
                        AddIncomingMessageHandler("Ping", async (proto) =>
                        {
                            await SendPong();
                        });
                        AddIncomingMessageHandler("Timeout", async (proto) =>
                        {
                            client.Close();
                            await (OnTimeout?.InvokeAsync(Tasks.DefaultOnError) ?? Task.CompletedTask);
                        });
                    }

                    /// <summary>
                    ///   Triggered when the client received the notification that the
                    ///   ping did not occur after a tolerance time, perhaps due to
                    ///   networking problems. Expect a sudden yet graceful disconnection
                    ///   after this message.
                    /// </summary>
                    public event Func<Task> OnTimeout = null;
                }
            }
        }
    }
}