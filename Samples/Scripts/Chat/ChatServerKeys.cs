using AlephVault.Unity.Meetgard.Server;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace AlephVault.Unity.Meetgard.Samples
{
    namespace Chat
    {
        [RequireComponent(typeof(ChatProtocolServerSide))]
        public class ChatServerKeys : MonoBehaviour
        {
            [SerializeField]
            private KeyCode startKey;

            [SerializeField]
            private KeyCode stopKey;

            private NetworkServer server;

            // Start is called before the first frame update
            void Awake()
            {
                server = GetComponent<NetworkServer>();
            }

            // Update is called once per frame
            void Update()
            {
                if (Input.GetKeyDown(startKey) && !server.IsListening)
                {
                    server.StartServer(IPAddress.Any, 6666);
                }
                else if (Input.GetKeyDown(stopKey) && server.IsListening)
                {
                    server.StopServer();
                }
            }
        }
    }
}
