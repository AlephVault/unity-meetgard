using AlephVault.Unity.Binary;
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        /// <summary>
        ///   Utils related to reading and writing
        ///   messages into arrays.
        /// </summary>
        public static class MessageUtils
        {
            /// <summary>
            ///   Reads a message from a stream. Instantiates an
            ///   object of the appropriate type to receive the
            ///   message, and returns both the header of the
            ///   message and the deserialized object as a result.
            /// </summary>
            /// <param name="input">The input stream to read from</param>
            /// <param name="content">A function that guesses the target to serialize the data into</param>
            /// <param name="tempArray">The array that will serve as temporary buffer for the input data</param>
            /// <returns>The header of the just-deserialized message</returns>
            public static Tuple<MessageHeader, ISerializable> ReadMessage(Stream input, Func<ushort, ushort, ISerializable> factory, byte[] tempArray)
            {
                if (factory == null)
                {
                    throw new ArgumentNullException("content");
                }

                if (tempArray == null || tempArray.Length < 6)
                {
                    throw new ArgumentException("The temporary array must not be null, and must be at least 6 bytes long");
                }

                MessageHeader header = new MessageHeader();
                ISerializable content;
                try
                {
                    // Reading and validating the header.
                    BinaryUtils.ReadUntil(input, tempArray, 0, 6);
                    BinaryUtils.Load(header, tempArray);
                    header.CheckSize(tempArray.Length);
                    content = factory(header.ProtocolId, header.MessageTag);
                    // Reading the message body.
                    BinaryUtils.ReadUntil(input, tempArray, 0, header.MessageSize);
                    BinaryUtils.Load(content, tempArray);
                }
                catch(NotSupportedException)
                {
                    throw new MessageOverflowException("The serialization consumed more bytes than declared in the header message. This is most likely due to a corrupted message");
                }
                // Returning the header.
                return new Tuple<MessageHeader, ISerializable>(header, content);
            }

            /// <summary>
            ///   De-serializes an object and sends it to
            ///   an output stream, along a corresponding
            ///   message header.
            /// </summary>
            /// <param name="output">The stream to write the message into</param>
            /// <param name="protocolId">The protocol id for this message</param>
            /// <param name="messageTag">The tag for this message</param>
            /// <param name="content">The content to serialize</param>
            /// <param name="tempArray">The array that will serve as temporary buffer for the output data</param>
            public static void WriteMessage(Stream output, ushort protocolId, ushort messageTag, ISerializable content, byte[] tempArray)
            {
                if (content == null)
                {
                    throw new ArgumentNullException("content");
                }

                if (tempArray == null || tempArray.Length < 6)
                {
                    throw new ArgumentNullException("tempArray");
                }

                long length;
                try
                {
                    length = BinaryUtils.Dump(content, tempArray);
                }
                catch (NotSupportedException)
                {
                    throw new MessageOverflowException($"The content to serialize requires more bytes than the allocated in the temporary array ({tempArray.Length})");
                }

                if (length > ushort.MaxValue)
                {
                    throw new MessageOverflowException($"The final length of the message body ({length}) must not be above the supported limit ({ushort.MaxValue})");
                }
                // Serializing both the header and the message body.
                MessageHeader header = new MessageHeader() { ProtocolId = protocolId, MessageTag = messageTag, MessageSize = (ushort)length };
                // No need to dump the header in the intermediary array.
                // Just send the header through the stream, and then the content
                // (which is, yes, previously dumped into array)
                header.Serialize(new Serializer(new Writer(output)));
                output.Write(tempArray, 0, (int)length);
            }
        }
    }
}
