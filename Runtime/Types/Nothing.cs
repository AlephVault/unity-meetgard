using AlephVault.Unity.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        /// <summary>
        ///   This is an empty message, to not cause
        ///   overhead at all when no body is needed.
        /// </summary>
        public class Nothing : ISerializable
        {
            /// <summary>
            ///   An instance to use everywhere without having to
            ///   create one everywhere.
            /// </summary>
            public static Nothing Instance = new Nothing();

            public void Serialize(Serializer serializer) {}
        }
    }
}
