using AlephVault.Unity.Binary;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Protocols
    {
        /// <summary>
        ///   Tracks a version as manor.minor.revision-releaseType.
        /// </summary>
        [Serializable]
        public class Version : ISerializable
        {
            public enum VersionReleaseType
            {
                Stable, RC, Beta, Alpha, PreAlpha
            }

            public byte Major;
            public byte Minor;
            public byte Revision;
            public VersionReleaseType ReleaseType;

            public Version() {}

            public void Serialize(Serializer serializer)
            {
                serializer.Serialize(ref Major);
                serializer.Serialize(ref Minor);
                serializer.Serialize(ref Revision);
                serializer.Serialize(ref ReleaseType);
            }

            public bool Equals(Version other)
            {
                return Major == other.Major && Minor == other.Minor;
            }
        }
    }
}
