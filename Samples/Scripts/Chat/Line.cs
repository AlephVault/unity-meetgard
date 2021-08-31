using AlephVault.Unity.Binary;

namespace AlephVault.Unity.Meetgard.Samples
{
    namespace Chat
    {
        public class Echo : ISerializable
        {
            public string Content;

            public void Serialize(Serializer serializer)
            {
                serializer.Serialize(ref Content);
            }

            public override string ToString()
            {
                return Content;
            }
        }
    }
}