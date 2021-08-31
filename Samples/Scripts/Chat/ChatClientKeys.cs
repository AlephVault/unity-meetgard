using AlephVault.Unity.Meetgard.Authoring.Behaviours.Client;
using UnityEngine;

namespace AlephVault.Unity.Meetgard.Samples
{
    namespace Chat
    {
        [RequireComponent(typeof(ChatProtocolClientSide))]
        public class ChatClientKeys : MonoBehaviour
        {
            [SerializeField]
            private KeyCode connectKey;

            [SerializeField]
            private KeyCode disconnectKey;

            [SerializeField]
            private KeyCode dumbMessageSendKey;

            private ChatProtocolClientSide protocol;
            private NetworkClient client;

            // Start is called before the first frame update
            void Awake()
            {
                protocol = GetComponent<ChatProtocolClientSide>();
                client = GetComponent<NetworkClient>();
            }

            // Update is called once per frame
            void Update()
            {
                if (Input.GetKeyDown(connectKey) && !client.IsConnected)
                {
                    client.Connect("127.0.0.1", 6666);
                }
                else if (Input.GetKeyDown(disconnectKey) && client.IsConnected)
                {
                    client.Close();
                }
                else if (Input.GetKeyDown(dumbMessageSendKey) && client.IsConnected)
                {
                    protocol.Say("Lorem ipsum dolor sit amet");
                }
            }
        }
    }
}
