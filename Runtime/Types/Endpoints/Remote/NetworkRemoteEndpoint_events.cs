using System;

namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        using AlephVault.Unity.Binary;
        using System.Threading.Tasks;

        /// <summary>
        ///   <para>
        ///     A network endpoint serves for remote, non-host,
        ///     connections.
        ///   </para>
        ///   <para>
        ///     Endpoints can be told to be closed, and manage the
        ///     send and arrival of data. Sending the data can be
        ///     done in a buffered way (via "train buffers"). Most
        ///     of these operations are asynchronous in a way or
        ///     another, and event-driven. The asynchronous calls
        ///     are synchronized into the main Unity thread, however,
        ///     via the default async execution manager.
        ///   </para>
        /// </summary>
        public partial class NetworkRemoteEndpoint : NetworkEndpoint
        {
            // When a connection is established, this callback is processed.
            private Func<Task> onConnectionStart = null;

            // When a message is received, this callback is processed, passing
            // a protocol ID, a message tag, and a reader for the incoming buffer.
            private Func<ushort, ushort, ISerializable, Task> onMessage = null;

            // When a connection is terminated, this callback is processed.
            // If the termination was not graceful, the exception that caused
            // the termination will be given. Otherwise, it will be null.
            private Func<System.Exception, Task> onConnectionEnd = null;

            // Invokes the method DoTriggerOnConnectionStart, which is
            // asynchronous in nature.
            private async void TriggerOnConnectionStart()
            {
                await (onConnectionStart?.Invoke() ?? Task.CompletedTask);
            }

            // Invokes the method DoTriggerOnConnectionEnd, which is asynchronous
            // in nature.
            private async void TriggerOnConnectionEnd(System.Exception exception)
            {
                await (onConnectionEnd?.Invoke(exception) ?? Task.CompletedTask);
            }

            // Invokes the method DoTriggerOnMessageEvent, which is asynchronous
            // in nature.
            private async void TriggerOnMessageEvent()
            {
                if (queuedIncomingMessages.TryDequeue(out var result))
                {
                    await (onMessage?.Invoke(result.Item1, result.Item2, result.Item3) ?? Task.CompletedTask);
                }
            }
        }
    }
}
