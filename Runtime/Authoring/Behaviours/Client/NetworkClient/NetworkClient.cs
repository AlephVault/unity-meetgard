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
                    ///   Tells that this client will not be destroyed when a new scene
                    ///   is loaded in normal mode.
                    /// </summary>
                    [SerializeField]
                    private bool DontDestroy;
                }
            }
        }
    }
}
