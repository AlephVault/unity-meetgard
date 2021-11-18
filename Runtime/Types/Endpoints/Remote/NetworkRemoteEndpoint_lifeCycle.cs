using System;
using System.Net.Sockets;
using System.Threading;

namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        using AlephVault.Unity.Binary;
        using AlephVault.Unity.Support.Utils;

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
            /// <summary>
            ///   The time to sleep, on each iteration, when no data to
            ///   read or write is present in the socket on a given
            ///   iteration.
            /// </summary>
            public readonly float IdleSleepTime;

            // A life-cycle thread for our socket.
            private Thread lifeCycle = null;

            // Starts the life-cycle.
            private void StartLifeCycle()
            {
                lifeCycle = new Thread(new ThreadStart(LifeCycle));
                lifeCycle.IsBackground = true;
                lifeCycle.Start();
            }

            // The full socket lifecycle goes here.
            private void LifeCycle()
            {
                XDebug debugger = new XDebug("Meetgard", this, "LifeCycle()", debug);
                debugger.Start();

                System.Exception lifeCycleException = null;
                byte[] outgoingMessageArray = new byte[MaxMessageSize];
                byte[] incomingMessageArray = new byte[MaxMessageSize];
                try
                {
                    lifeCycleException = null;
                    // So far, remoteSocket WILL be connected.
                    TriggerOnConnectionStart();
                    // We get the stream once.
                    NetworkStream stream = remoteSocket.GetStream();
                    stream.WriteTimeout = remoteSocketWriteTimeout;
                    while (true)
                    {
                        try
                        {
                            bool inactive = true;
                            if (!remoteSocket.Connected)
                            {
                                // Close, if the socket is not connected.
                                return;
                            }
                            if (stream.CanRead && stream.DataAvailable)
                            {
                                Tuple<MessageHeader, ISerializable> result;
                                // protocolMessageFactory must throw an exception when
                                // the message is not understood. Such exception will
                                // blindly close the connection.
                                result = MessageUtils.ReadMessage(stream, protocolMessageFactory, outgoingMessageArray);
                                debugger.Info($"Receiving message: ({result.Item1.ProtocolId}, {result.Item1.MessageTag}, {result.Item2})");
                                queuedIncomingMessages.Enqueue(new Tuple<ushort, ushort, ISerializable>(result.Item1.ProtocolId, result.Item1.MessageTag, result.Item2));
                                TriggerOnMessageEvent();
                                inactive = false;
                            }
                            if (stream.CanWrite && !queuedOutgoingMessages.IsEmpty)
                            {
                                while (queuedOutgoingMessages.TryDequeue(out var result))
                                {
                                    debugger.Info($"Taking queued message: {result}");
                                    try
                                    {
                                        debugger.Info($"Writing it to the stream");
                                        MessageUtils.WriteMessage(stream, result.Item1, result.Item2, result.Item3, outgoingMessageArray);
                                        // The task is marked as complete.
                                        debugger.Info($"Write success. Marking task as completed");
                                        result.Item4.TrySetResult(true);
                                        debugger.Info($"Task marked");
                                    }
                                    catch (Exception e)
                                    {
                                        debugger.Exception(e);
                                        result.Item4.TrySetException(e);
                                        throw;
                                    }
                                }
                                inactive = false;
                            }
                            if (inactive)
                            {
                                // On inactivity we sleep a while, to not hog
                                // the processor.
                                Thread.Sleep((int)(IdleSleepTime * 1000));
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // Simply return, for the socket is closed.
                            // This happened, probably, gracefully. The
                            // `finally` block will still do the cleanup.
                            return;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    // Keep the exception, and return from the
                    // whole thread execution.
                    lifeCycleException = e;
                }
                finally
                {
                    // On closure, if the socket is connected,
                    // it will be closed. Then, it will be
                    // disposed and unassigned.
                    if (remoteSocket != null)
                    {
                        if (remoteSocket.Connected) remoteSocket.Close();
                        remoteSocket.Dispose();
                    }
                    // Also, clear the thread reference.
                    lifeCycle = null;
                    // Finally, trigger the disconnected event.
                    TriggerOnConnectionEnd(lifeCycleException);
                    debugger.End();
                }
            }
        }
    }
}
