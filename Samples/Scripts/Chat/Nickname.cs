using AlephVault.Unity.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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