using AlephVault.Unity.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        /// <summary>
        ///   A message header with the 3 fields:
        ///   protocol id, message tag, message size.
        ///   It also has the ability to check the
        ///   size of the message against a maximum
        ///   provided size.
        /// </summary>
        public class MessageHeader : ISerializable
        {
            public ushort ProtocolId;
            public ushort MessageTag;
            public ushort MessageSize;

            public void Serialize(Serializer serializer)
            {
                if (serializer.IsReading)
                {
                    ProtocolId = serializer.Reader.ReadUInt16();
                    MessageTag = serializer.Reader.ReadUInt16();
                    MessageSize = serializer.Reader.ReadUInt16();
                }
                else
                {
                    serializer.Writer.WriteUInt16(ProtocolId);
                    serializer.Writer.WriteUInt16(MessageTag);
                    serializer.Writer.WriteUInt16(MessageSize);
                }
            }

            public void CheckSize(long maxMessageSize)
            {
                if (MessageSize > maxMessageSize)
                {
                    throw new MessageOverflowException($"The message's size ({MessageSize}) is greater than the maximum allowed size ({maxMessageSize})");
                }
            }
        }
    }
}
