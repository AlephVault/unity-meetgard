using System.Security.Authentication;
using AlephVault.Unity.Support.Authoring.Behaviours;
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
                ///     Network servers are behaviours that spawn an additional
                ///     thread to listen for connections. Each connection is
                ///     accepted and, for each one, a new thread is spawned to
                ///     handle it. Each server can listen in one address:port
                ///     at once, but many different servers can be instantiated
                ///     in the same scene.
                ///   </para>
                /// </summary>
                [RequireComponent(typeof(AsyncQueueManager))]
                public partial class NetworkServer : MonoBehaviour
                {
                    /// <summary>
                    ///   <para>
                    ///     The time to sleep, on each iteration, when no data to
                    ///     read or write is present in the socket on a given
                    ///     iteration.
                    ///   </para>
                    ///   <para>
                    ///     This setting should match whatever is set in the clients
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
                    ///     This setting should match whatever is set in the clients
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
                    ///   Tells that this server will not be destroyed when a new scene
                    ///   is loaded in normal mode.
                    /// </summary>
                    [SerializeField]
                    private bool DontDestroy;

                    /// <summary>
                    ///   Tells whether the server uses a secure protocol (SSL, TLS)
                    ///   for the whole connection lifecycle.
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
                    ///   The path to the certificate to use. When using the secure
                    ///   mode, this field is mandatory. The file will be attempted
                    ///   as a .pfx file (containing both the certificate and the
                    ///   private key) if the value of <see cref="privateKeyPath"/>
                    ///   is empty. Otherwise, this filepath will be understood as
                    ///   a .pem certificate file, and also the key will be attempted
                    ///   as that format. Additionally, if there is a non-blank value
                    ///   at <see cref="privateKeyPassphraseEnvVar"/>, then the key
                    ///   will be attempted as an encrypted private key, regardless
                    ///   the whole load being .pfx or .pem. Both the certificate
                    ///   path and the private key path (if specified) must be either
                    ///   absolute paths or somehow reachable paths from the server.
                    /// </summary>
                    [SerializeField]
                    private string certificatePath;
                    
                    /// <summary>
                    ///   See <see cref="certificatePath"/> for details on how to use
                    ///   this field.
                    /// </summary>
                    [SerializeField]
                    private string privateKeyPath;

                    /// <summary>
                    ///   See <see cref="certificatePath"/> for details on how to use
                    ///   this field.
                    /// </summary>
                    [SerializeField]
                    private string privateKeyPassphraseEnvVar;
                }
            }
        }
    }
}
