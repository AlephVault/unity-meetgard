using AlephVault.Unity.Binary;

namespace AlephVault.Unity.Meetgard.Samples
{
    namespace Chat
    {
        public class Nickname : ISerializable
        {
            public string Nick;

            public void Serialize(Serializer serializer)
            {
                serializer.Serialize(ref Nick);
            }

            public override string ToString()
            {
                return Nick;
            }
        }
    }
}