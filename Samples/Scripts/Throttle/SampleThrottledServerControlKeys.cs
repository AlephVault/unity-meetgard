using System;
using AlephVault.Unity.Meetgard.Authoring.Behaviours.Server;
using AlephVault.Unity.Meetgard.Samples.Throttle;
using UnityEngine;

namespace AlephVault.Unity.Meetgard.Samples
{
    namespace Throttled
    {
        [RequireComponent(typeof(SampleThrottledProtocolServerSide))]
        public class SampleThrottledServerControlKeys : MonoBehaviour
        {
            private SampleThrottledProtocolServerSide serverProtocol;
            private NetworkServer server;
            
            [SerializeField]
            private KeyCode startKey = KeyCode.Z;

            [SerializeField]
            private KeyCode stopKey = KeyCode.X;
            
            private void Awake()
            {
                serverProtocol = GetComponent<SampleThrottledProtocolServerSide>();
                server = GetComponent<NetworkServer>();
            }

            private void Update()
            {
                if (Input.GetKeyDown(startKey))
                {
                    Debug.Log("Starting server...");
                    server.StartServer(7777);
                }
                else if (Input.GetKeyDown(stopKey))
                {
                    Debug.Log("Stopping server...");
                    server.StopServer();
                }
            }
        }
    }
}
