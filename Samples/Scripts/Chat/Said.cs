using AlephVault.Unity.Binary;

namespace AlephVault.Unity.Meetgard.Samples
{
    namespace Chat
    {
        public class Said : ISerializable
        {
            public string Nickname;
            public string Content;
            public string When;

            public void Serialize(Serializer serializer)
            {
                serializer.Serialize(ref Nickname);
                serializer.Serialize(ref Content);
                serializer.Serialize(ref When);
            }

            public override string ToString()
            {
                return $"{When} {Nickname}: {Content}";
            }
        }
    }
}