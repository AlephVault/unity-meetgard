using System.Net.Sockets;
using System.Threading;
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
                    // The current server life-cycle.
                    private Thread lifeCycle = null;

                    // Starts a new life-cycle for this server.
                    private void StartLifeCycle()
                    {
                        lifeCycle = new Thread(new ThreadStart(LifeCycle));
                        lifeCycle.IsBackground = true;
                        lifeCycle.Start();
                    }

                    // The full server life-cycle goes here.
                    private void LifeCycle()
                    {
                        System.Exception lifeCycleException = null;
                        try
                        {
                            // The server is considered connected right now.
                            DoTriggerOnServerStarted();
                            PrepareCertificate();
                            // Accepts all of the incoming connections, ad eternum.
                            while (true) try
                            {
                                TcpClient newClient = listener.AcceptTcpClient();
                                connectionIdPoolMutex.Wait();
                                AddNetworkClientEndpoint(newClient);
                            }
                            finally
                            {
                                connectionIdPoolMutex.Release();
                            }

                        }
                        catch (SocketException e)
                        {
                            // If the error code is SocketError.Interrupted, this close reason is
                            // graceful in this context. Otherwise, it is abnormal.
                            if (e.SocketErrorCode != SocketError.Interrupted) lifeCycleException = e;
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogException(e);
                            lifeCycleException = e;
                        }
                        finally
                        {
                            if (listener != null)
                            {
                                listener.Stop();
                                listener = null;
                            }
                            DoTriggerOnServerStopped(lifeCycleException);
                        }
                    }
                }
            }
        }
    }
}
