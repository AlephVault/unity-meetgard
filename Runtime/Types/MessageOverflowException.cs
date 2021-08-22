using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        /// <summary>
        ///   Triggered when a message is received with a size bigger
        ///   than the allowed one. This goes both for servers and
        ///   clients, but mainly intended for servers receiving
        ///   client messages.
        /// </summary>
        public class MessageOverflowException : Exception
        {
            public MessageOverflowException() : base() {}
            public MessageOverflowException(string message) : base(message) {}
            public MessageOverflowException(string message, System.Exception inner) : base(message, inner) {}
        }
    }
}
