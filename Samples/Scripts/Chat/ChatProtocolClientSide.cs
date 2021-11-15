using AlephVault.Unity.Meetgard.Authoring.Behaviours.Client;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace AlephVault.Unity.Meetgard.Samples
{
    namespace Chat
    {
        public class ChatProtocolClientSide : ProtocolClientSide<ChatProtocolDefinition>
        {
            [SerializeField]
            private string Nickname;

            private Func<Nickname, Task> SendNickname;
            private Func<Line, Task> SendLine;
            private Func<Echo, Task> SendPong;

            protected override void Initialize()
            {
                SendNickname = MakeSender<Nickname>("Nickname");
                SendLine = MakeSender<Line>("Say");
                SendPong = MakeSender<Echo>("Ping:Pong");
            }

            protected override void SetIncomingMessageHandlers()
            {
                AddIncomingMessageHandler("WhoAreYou", async (proto) =>
                {
                    Debug.Log($"client({Nickname}) :: server >>> WhoAreYou");
                    _ = SendNickname(new Nickname() { Nick = Nickname });
                    Debug.Log($"client({Nickname}) :: me >>> Nickname {Nickname}");
                });
                AddIncomingMessageHandler<Said>("Say:Said", async (proto, content) =>
                {
                    Debug.Log($"client({Nickname}) :: server >>> Said: {content}");
                });
                AddIncomingMessageHandler("Nickname:OK", async (proto) =>
                {
                    Debug.Log($"client({Nickname}) :: server >>> Nickname is OK");
                });
                AddIncomingMessageHandler("Nickname:Duplicated", async (proto) =>
                {
                    Debug.Log($"client({Nickname}) :: server >>> Nickname is duplicated");
                });
                AddIncomingMessageHandler("Nickname:AlreadyIntroduced", async (proto) =>
                {
                    Debug.Log($"client({Nickname}) :: server >>> Already introduced");
                });
                AddIncomingMessageHandler("Say:OK", async (proto) =>
                {
                    Debug.Log($"client({Nickname}) :: server >>> Successfully saying");
                });
                AddIncomingMessageHandler("Say:NotIntroduced", async (proto) =>
                {
                    Debug.Log($"client({Nickname}) :: server >>> Error on saying: Please introduce yourself first");
                });
                AddIncomingMessageHandler<Nickname>("Nickname:Joined", async (proto, nick) =>
                {
                    Debug.Log($"client({Nickname}) :: server >>> Joined: {nick}");
                });
                AddIncomingMessageHandler<Nickname>("Nickname:Left", async (proto, nick) =>
                {
                    Debug.Log($"client({Nickname}) :: server >>> Left: {nick}");
                });
                AddIncomingMessageHandler<Echo>("Ping", async (proto, echo) =>
                {
                    Debug.Log($"client({Nickname} :: server >>> Ping: {echo}");
                    _ = SendPong(echo);
                    Debug.Log($"client({Nickname}) :: me >>> Pong {echo}");
                });
                AddIncomingMessageHandler("Ping:Timeout", async (proto) =>
                {
                    Debug.Log($"client({Nickname} :: server >>> Ping:Timeout");
                });
            }

            public Task Say(string text)
            {
                Task task = SendLine(new Line() { Content = text });
                Debug.Log($"client({Nickname}) :: me >>> Say {text}");
                return task;
            }

            public override async Task OnConnected()
            {
                Debug.Log($"client({Nickname}) :: me >>> Connected");
            }

            public override async Task OnDisconnected(Exception reason)
            {
                Debug.Log($"client({Nickname}) :: me >>> Disconnected because: {reason}");
            }
        }
    }
}