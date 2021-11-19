using System.Net.Security;
using System.Security.Authentication;
using AlephVault.Unity.Support.Authoring.Behaviours;
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
                ///     Network clients are behaviours that spawn an additional
                ///     thread to interact with a server. They can be connected
                ///     to only one server at once, but many clients can be
                ///     instantiated in the same scene.
                ///   </para>
                /// </summary>
                [RequireComponent(typeof(AsyncQueueManager))]
                public partial class NetworkClient : MonoBehaviour
                {
                    /// <summary>
                    ///   <para>
                    ///     The time to sleep, on each iteration, when no data to
                    ///     read or write is present in the socket on a given
                    ///     iteration.
                    ///   </para>
                    ///   <para>
                    ///     This setting should match whatever is set in the server
                    ///     and supported by the protocols to use.
                    ///   </para>
                    /// </summary>
                    [SerializeField]
                    private float idleSleepTime = 0.01f;

                    /// <summary>
                    ///   <para>
                    ///     The maximum size of each individual message to be sent.
                    ///   </para>
                    ///   <para>
                    ///     This setting should match whatever is set in the server
                    ///     and supported by the protocols to use.
                    ///   </para>
                    /// </summary>
                    [SerializeField]
                    private ushort maxMessageSize = 1024;

                    /// <summary>
                    ///   See <see cref="maxMessageSize"/>.
                    /// </summary>
                    public ushort MaxMessageSize => maxMessageSize;

                    /// <summary>
                    ///   The timeout, in seconds, to wait until a message is written
                    ///   and sent through the network. The connection will be closed
                    ///   after this timeout occurs.
                    /// </summary>
                    [SerializeField]
                    private float writeTimeout = 15f;

                    /// <summary>
                    ///   Tells that this client will not be destroyed when a new scene
                    ///   is loaded in normal mode.
                    /// </summary>
                    [SerializeField]
                    private bool DontDestroy;
                    
                    /// <summary>
                    ///   Tells whether the server uses a secure protocol (SSL, TLS).
                    ///   This client will attempt connecting using SSL handshakes and
                    ///   security for the whole connection lifecycle.
                    /// </summary>
                    [SerializeField]
                    private bool Secure;
                    
                    // TODO Add a custom editor later. All the following properties will
                    // TODO be hidden in the inspector if Secure == false.

                    /// <summary>
                    ///   The SSL/TLS protocol to use. By default, the system guesses
                    ///   which one to use, appropriately.
                    /// </summary>
                    [SerializeField]
                    private SslProtocols sslProtocols = SslProtocols.Default;

                    /// <summary>
                    ///   Checks whether the server certificate is revoked or not.
                    /// </summary>
                    [SerializeField]
                    private bool checkCertificateRevocation;
                }
            }
        }
    }
}
