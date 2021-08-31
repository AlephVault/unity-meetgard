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
                }
            }
        }
    }
}
