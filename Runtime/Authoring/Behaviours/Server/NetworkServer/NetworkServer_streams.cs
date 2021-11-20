using AlephVault.Unity.Binary;
using AlephVault.Unity.Meetgard.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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
                    private Stream PrepareStream(NetworkStream stream)
                    {
                        if (!secure)
                        {
                            return stream;
                        }
                        else
                        {
                            
                        }
                    }
                }
            }
        }
    }
}
