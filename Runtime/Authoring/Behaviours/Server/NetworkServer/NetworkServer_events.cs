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
    namespace Server
    {
        public partial class NetworkServer : MonoBehaviour
        {
            // Asynchronously triggers the OnServerStarted event.
            private async void DoTriggerOnServerStarted()
            {
                await TriggerOnServerStarted();
            }

            // Asynchronously triggers the OnServerStopped event, but after telling
            // all of the active sockets to close. The server stopped event may encounter
            // race conditions with the disconnection events (which become, in turn, calls
            // to TriggerOnClientDisconnected).
            private async void DoTriggerOnServerStopped(System.Exception e)
            {
                CloseAllEndpoints();
                await TriggerOnServerStopped(e);
            }
        }
    }
}
