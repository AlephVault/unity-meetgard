using System;
using AlephVault.Unity.Meetgard.Authoring.Behaviours.Client;
using AlephVault.Unity.Meetgard.Samples.Throttle;
using UnityEngine;

namespace AlephVault.Unity.Meetgard.Samples
{
    namespace Throttled
    {
        [RequireComponent(typeof(SampleThrottledProtocolClientSide))]
        public class SampleThrottledClientControlKeys : MonoBehaviour
        {
            private SampleThrottledProtocolClientSide clientProtocol;
            private NetworkClient client;
            
            [SerializeField]
            private KeyCode connectKey = KeyCode.Q;

            [SerializeField]
            private KeyCode disconnectKey = KeyCode.W;
        
            [SerializeField]
            private KeyCode clientThrottled1Key = KeyCode.E;
        
            [SerializeField]
            private KeyCode clientThrottled2Key = KeyCode.R;
        
            [SerializeField]
            private KeyCode serverThrottled1Key = KeyCode.T;
        
            [SerializeField]
            private KeyCode serverThrottled2Key = KeyCode.Y;

            private void Awake()
            {
                clientProtocol = GetComponent<SampleThrottledProtocolClientSide>();
                client = GetComponent<NetworkClient>();
            }

            private void Update()
            {
                if (Input.GetKeyDown(connectKey))
                {
                    Debug.Log("Connecting client...");
                    client.Connect("localhost", 7777);
                }
                else if (Input.GetKeyDown(disconnectKey))
                {
                    Debug.Log("Stopping client...");
                    client.Close();
                }
                else if (Input.GetKeyDown(clientThrottled1Key))
                {
                    Debug.Log("Sending client throttled 1...");
                    clientProtocol.ClientThrottled();                    
                }
                else if (Input.GetKeyDown(clientThrottled2Key))
                {
                    Debug.Log("Sending client throttled 2...");
                    clientProtocol.ClientThrottled2("Hello");
                }
                else if (Input.GetKeyDown(serverThrottled1Key))
                {
                    Debug.Log("Sending server throttled 1...");
                    clientProtocol.ServerThrottled();                    
                }
                else if (Input.GetKeyDown(serverThrottled2Key))
                {
                    Debug.Log("Sending server throttled 2...");
                    clientProtocol.ServerThrottled2("Hello");
                }
            }
        }
    }
}
